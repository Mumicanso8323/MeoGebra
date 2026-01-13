using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeoGebra.Models;
using MeoGebra.Services.Expression;

namespace MeoGebra.Services.Evaluation;

public sealed class EvaluationPipeline {
    private readonly IExpressionSampler _sampler;
    private readonly TimeSpan _debounce;
    private CancellationTokenSource? _cts;
    private long _generation;

    public EvaluationPipeline(IExpressionSampler sampler, TimeSpan? debounce = null) {
        _sampler = sampler;
        _debounce = debounce ?? TimeSpan.FromMilliseconds(150);
    }

    public Task RequestEvaluationAsync(Document document, int samples, Action<EvaluationResult> onCompleted) {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var generation = Interlocked.Increment(ref _generation);

        return Task.Run(async () => {
            try {
                await Task.Delay(_debounce, token);
                token.ThrowIfCancellationRequested();
                var result = await EvaluateAsync(document, samples, token).ConfigureAwait(false);
                if (generation == _generation && !token.IsCancellationRequested) {
                    onCompleted(result);
                }
            } catch (OperationCanceledException) {
            }
        }, token);
    }

    public async Task<EvaluationResult> EvaluateAsync(Document document, int samples, CancellationToken token) {
        var parseResults = new Dictionary<Guid, BoundFunction?>();
        var diagnostics = new Dictionary<Guid, List<Diagnostic>>();
        SurfaceRenderCache? surfaceCache = null;

        document.Symbols.Restore(document.Functions);

        foreach (var function in document.Functions) {
            var functionDiagnostics = new List<Diagnostic>();
            diagnostics[function.Id] = functionDiagnostics;
            if (string.IsNullOrWhiteSpace(function.ExpressionText)) {
                functionDiagnostics.Add(new Diagnostic(DiagnosticCategory.Parse, "Expression is empty."));
                parseResults[function.Id] = null;
                continue;
            }
            var parser = new Parser(function.ExpressionText);
            ExpressionInput input;
            try {
                input = parser.ParseExpressionInput();
            } catch (Exception ex) {
                functionDiagnostics.Add(new Diagnostic(DiagnosticCategory.Parse, ex.Message));
                parseResults[function.Id] = null;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(input.DefinedName) && !string.Equals(input.DefinedName, function.Name, StringComparison.OrdinalIgnoreCase)) {
                function.Name = input.DefinedName!;
                document.Symbols.SetName(function.Name, function.Id);
            }

            if (input.Parameters.Count > 0) {
                function.Parameters = input.Parameters.ToArray();
            }
            if (function.Parameters.Length == 0) {
                function.Parameters = new[] { "x" };
            }

            var binder = new Binder(document.Symbols, function.Parameters);
            var boundResult = binder.Bind(input.Body);
            functionDiagnostics.AddRange(boundResult.Diagnostics.Items);

            if (boundResult.Expression is null) {
                parseResults[function.Id] = null;
                continue;
            }

            parseResults[function.Id] = new BoundFunction(
                function.Id,
                function.Name,
                boundResult.Expression,
                function.XMin,
                function.XMax,
                function.PaletteIndex);
        }

        var order = TopologicalSort(parseResults, diagnostics);
        var sampledValues = new Dictionary<Guid, double[]>();
        var renderCaches = new Dictionary<Guid, FunctionRenderCache>();

        foreach (var functionId in order) {
            token.ThrowIfCancellationRequested();

            var function = document.FindFunction(functionId);
            if (function is null) {
                continue;
            }

            if (document.PlotMode == PlotMode.TwoD && function.Parameters.Length != 1) {
                diagnostics[functionId].Add(new Diagnostic(
                    DiagnosticCategory.Bind,
                    "2D plot mode requires a single parameter (x)."));
                continue;
            }

            if (!parseResults.TryGetValue(functionId, out var bound) || bound is null) {
                continue;
            }

            if (document.PlotMode == PlotMode.ThreeD && function.Parameters.Length != 2) {
                diagnostics[functionId].Add(new Diagnostic(
                    DiagnosticCategory.Bind,
                    "3D plot mode requires two parameters (x, y)."));
                continue;
            }

            var context = new EvaluationContext(
                document.Viewport.CenterX - document.Viewport.ScaleX,
                document.Viewport.CenterX + document.Viewport.ScaleX,
                samples,
                document.AngleMode,
                document.Viewport,
                sampledValues);

            var cache = await _sampler.SampleAsync(bound, context, token).ConfigureAwait(false);
            renderCaches[functionId] = cache;

            var values = new double[samples];
            var step = (context.X1 - context.X0) / (samples - 1);
            for (var i = 0; i < samples; i++) {
                var x = context.X0 + step * i;
                values[i] = ExpressionEvaluator.Evaluate(bound.Expression, x, 0, document.AngleMode, sampledValues, i);
            }
            sampledValues[functionId] = values;

            if (cache.Segments.Count == 0) {
                diagnostics[functionId].Add(new Diagnostic(DiagnosticCategory.Overflow, "No drawable points in current viewport."));
            }
        }

        if (document.PlotMode == PlotMode.ThreeD) {
            var surfaceTarget = FindSurfaceTarget(document, parseResults);
            if (surfaceTarget is not null) {
                var boundSurface = parseResults[surfaceTarget.Id];
                if (boundSurface is not null) {
                    surfaceCache = await SurfaceSampler.SampleAsync(
                        boundSurface,
                        document.Viewport,
                        document.AngleMode,
                        token).ConfigureAwait(false);
                }
            }
        }

        return new EvaluationResult(renderCaches, diagnostics, surfaceCache);
    }

    private static FunctionObject? FindSurfaceTarget(Document document, Dictionary<Guid, BoundFunction?> parseResults) {
        if (document.SelectedSurfaceFunctionId.HasValue) {
            var selected = document.FindFunction(document.SelectedSurfaceFunctionId.Value);
            if (selected is not null && selected.Parameters.Length == 2 && parseResults.ContainsKey(selected.Id)) {
                return selected;
            }
        }
        return document.Functions.FirstOrDefault(f => f.Parameters.Length == 2 && parseResults.ContainsKey(f.Id));
    }

    private static List<Guid> TopologicalSort(Dictionary<Guid, BoundFunction?> functions, Dictionary<Guid, List<Diagnostic>> diagnostics) {
        var graph = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var (id, bound) in functions) {
            var deps = bound?.Expression.EnumerateDependencies().ToHashSet() ?? new HashSet<Guid>();
            graph[id] = deps;
        }

        var result = new List<Guid>();
        var state = new Dictionary<Guid, int>();
        var invalid = new HashSet<Guid>();

        foreach (var id in graph.Keys) {
            Visit(id);
        }

        return result;

        void Visit(Guid id) {
            if (state.TryGetValue(id, out var existingState)) {
                if (existingState == 1) {
                    diagnostics[id].Add(new Diagnostic(DiagnosticCategory.Bind, "Cyclic dependency detected."));
                    invalid.Add(id);
                }
                return;
            }
            state[id] = 1;
            foreach (var dep in graph[id]) {
                if (graph.ContainsKey(dep)) {
                    Visit(dep);
                    if (invalid.Contains(dep)) {
                        diagnostics[id].Add(new Diagnostic(DiagnosticCategory.Bind, "Depends on a cyclic function."));
                        invalid.Add(id);
                    }
                }
            }
            state[id] = 2;
            if (!invalid.Contains(id)) {
                result.Add(id);
            }
        }
    }
}

public sealed record EvaluationResult(Dictionary<Guid, FunctionRenderCache> RenderCaches, Dictionary<Guid, List<Diagnostic>> Diagnostics, SurfaceRenderCache? SurfaceCache);

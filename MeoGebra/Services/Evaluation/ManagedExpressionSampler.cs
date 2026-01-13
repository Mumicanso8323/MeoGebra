using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MeoGebra.Models;
using MeoGebra.Services.Expression;

namespace MeoGebra.Services.Evaluation;

public sealed class ManagedExpressionSampler : IExpressionSampler {
    public Task<FunctionRenderCache> SampleAsync(BoundFunction function, EvaluationContext context, CancellationToken token) {
        return Task.Run(() => Sample(function, context, token), token);
    }

    private static FunctionRenderCache Sample(BoundFunction function, EvaluationContext context, CancellationToken token) {
        var segments = new List<FunctionSegment>();
        var current = new List<SamplePoint>();
        var step = (context.X1 - context.X0) / (context.Samples - 1);

        for (var i = 0; i < context.Samples; i++) {
            token.ThrowIfCancellationRequested();
            var x = context.X0 + step * i;
            if (function.XMin.HasValue && x < function.XMin.Value) {
                Flush();
                continue;
            }
            if (function.XMax.HasValue && x > function.XMax.Value) {
                Flush();
                continue;
            }
            var y = Evaluate(function.Expression, x, context.AngleMode, context.Dependencies, i);
            if (double.IsNaN(y) || double.IsInfinity(y)) {
                Flush();
                continue;
            }
            current.Add(new SamplePoint(x, y));
        }
        Flush();

        return new FunctionRenderCache(segments);

        void Flush() {
            if (current.Count > 1) {
                segments.Add(new FunctionSegment(current));
            }
            current = new List<SamplePoint>();
        }
    }

    private static double Evaluate(BoundExpression expression, double x, AngleMode angleMode, IReadOnlyDictionary<Guid, double[]> deps, int index) {
        return ExpressionEvaluator.Evaluate(expression, x, angleMode, deps, index);
    }
}

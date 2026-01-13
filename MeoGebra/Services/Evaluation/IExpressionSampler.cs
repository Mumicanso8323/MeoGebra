using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MeoGebra.Models;
using MeoGebra.Services.Expression;

namespace MeoGebra.Services.Evaluation;

public interface IExpressionSampler {
    Task<FunctionRenderCache> SampleAsync(BoundFunction function, EvaluationContext context, CancellationToken token);
}

public sealed class EvaluationContext {
    public EvaluationContext(double x0, double x1, int samples, AngleMode angleMode, IReadOnlyDictionary<Guid, double[]> dependencies) {
        X0 = x0;
        X1 = x1;
        Samples = samples;
        AngleMode = angleMode;
        Dependencies = dependencies;
    }

    public double X0 { get; }
    public double X1 { get; }
    public int Samples { get; }
    public AngleMode AngleMode { get; }
    public IReadOnlyDictionary<Guid, double[]> Dependencies { get; }
}

public sealed record BoundFunction(Guid Id, string Name, BoundExpression Expression, double? XMin, double? XMax, int PaletteIndex);

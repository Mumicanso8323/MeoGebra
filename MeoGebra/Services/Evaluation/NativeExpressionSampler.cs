using System;
using System.Threading;
using System.Threading.Tasks;
using MeoGebra.Models;

namespace MeoGebra.Services.Evaluation;

public sealed class NativeExpressionSampler : IExpressionSampler {
    public Task<FunctionRenderCache> SampleAsync(BoundFunction function, EvaluationContext context, CancellationToken token) {
        throw new NotSupportedException("Native sampler is not implemented yet.");
    }
}

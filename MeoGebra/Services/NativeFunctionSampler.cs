using System;
using MeoGebra.NativeInterop;

namespace MeoGebra.Services;

public sealed class NativeFunctionSampler : IFunctionSampler {
    public unsafe void Sample(FunctionKind kind, double x0, double x1, FunctionParams p, PointD[] destination) {
        if (destination is null || destination.Length == 0)
            throw new ArgumentException("destination is empty");

        fixed (PointD* ptr = destination) {
            int written = NativeMethods.SampleFunction(kind, x0, x1, destination.Length, p, (IntPtr)ptr);
            if (written != destination.Length)
                throw new InvalidOperationException($"Native wrote {written} points, expected {destination.Length}");
        }
    }
}

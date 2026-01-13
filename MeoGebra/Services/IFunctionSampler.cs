using MeoGebra.NativeInterop;

namespace MeoGebra.Services;

public interface IFunctionSampler {
    void Sample(FunctionKind kind, double x0, double x1, FunctionParams p, PointD[] destination);
}

using System.Runtime.InteropServices;

namespace MeoGebra.NativeInterop;

public enum FunctionKind : int {
    Sin = 0,
    // 将来ここに追加（Poly, Exp, etc）
}

[StructLayout(LayoutKind.Sequential)]
public struct FunctionParams {
    public double A;
    public double B;
}

[StructLayout(LayoutKind.Sequential)]
public struct PointD {
    public double X;
    public double Y;
}

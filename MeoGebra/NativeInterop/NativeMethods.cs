using System;
using System.Runtime.InteropServices;

namespace MeoGebra.NativeInterop;

internal static class NativeMethods {
    private const string DllName = "NativeMath"; // NativeMath.dll

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int SampleFunction(
        FunctionKind kind,
        double x0,
        double x1,
        int n,
        FunctionParams p,
        IntPtr outPoints // PointD*
    );
}

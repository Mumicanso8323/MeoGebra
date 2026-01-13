#include "pch.h"
#include "native_math.h"
#include <cmath>

static double eval(FunctionKind kind, double x, const FunctionParams& p) {
    switch (kind) {
    case FunctionKind::Sin:
        return p.A * std::sin(p.B * x);
    default:
        return 0.0;
    }
}

extern "C" API int SampleFunction(
    FunctionKind kind,
    double x0,
    double x1,
    int n,
    FunctionParams p,
    PointD* outPoints) {
    if (!outPoints || n <= 0) return 0;

    const double dx = (n == 1) ? 0.0 : (x1 - x0) / (n - 1);

    for (int i = 0; i < n; ++i) {
        const double x = x0 + dx * i;
        outPoints[i].X = x;
        outPoints[i].Y = eval(kind, x, p);
    }
    return n;
}

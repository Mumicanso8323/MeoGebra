#include "pch.h"
#include "native_math.h"
#include "ExpReal.h"
#include <cmath>

static double eval(FunctionKind kind, double x, const FunctionParams& p) {
    switch (kind) {
    case FunctionKind::Sin:
        return p.A * std::sin(p.B * x);
    default:
        return 0.0;
    }
}

static ExpReal evalExp(FunctionKind kind, double x) {
    switch (kind) {
    case FunctionKind::ExpCancel: {
        auto xVal = ExpReal::FromDouble(x);
        auto xSquared = ExpReal::Pow(xVal, 2.0);
        auto numerator = ExpReal::Exp(xSquared.ToDouble());
        auto minusOne = ExpReal::Sub(xSquared, ExpReal::FromDouble(1.0));
        auto denominator = ExpReal::Exp(minusOne.ToDouble());
        return ExpReal::Div(numerator, denominator);
    }
    default:
        return ExpReal();
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

extern "C" API int SampleFunctionExp(
    FunctionKind kind,
    double x0,
    double x1,
    int n,
    FunctionParams p,
    double yLimit,
    SampleResult* outPoints) {
    (void)p;
    if (!outPoints || n <= 0) return 0;

    const double dx = (n == 1) ? 0.0 : (x1 - x0) / (n - 1);

    for (int i = 0; i < n; ++i) {
        const double x = x0 + dx * i;
        auto y = evalExp(kind, x);
        outPoints[i].X = x;
        if (!y.IsValid() || !y.IsWithin(yLimit)) {
            outPoints[i].Y = 0.0;
            outPoints[i].Valid = 0;
        } else {
            outPoints[i].Y = y.ToDouble();
            outPoints[i].Valid = 1;
        }
    }
    return n;
}
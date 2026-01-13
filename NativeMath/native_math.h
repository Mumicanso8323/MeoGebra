#pragma once
#include <cstdint>

#ifdef _WIN32
#define API __declspec(dllexport)
#else
#define API
#endif

extern "C" {

    enum class FunctionKind : int32_t {
        Sin = 0,
    };

    struct FunctionParams {
        double A;
        double B;
    };

    struct PointD {
        double X;
        double Y;
    };

    API int SampleFunction(
        FunctionKind kind,
        double x0,
        double x1,
        int n,
        FunctionParams p,
        PointD* outPoints
    );

} // extern "C"

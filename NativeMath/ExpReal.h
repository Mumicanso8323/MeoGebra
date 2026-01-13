#pragma once
#include <cmath>

struct ExpReal {
    int sign = 1;
    double mantissa = 0.0;
    int exponent10 = 0;
    bool isZero = true;
    bool isNaN = false;

    static ExpReal FromDouble(double value);
    static ExpReal Exp(double value);

    static ExpReal Mul(const ExpReal& a, const ExpReal& b);
    static ExpReal Div(const ExpReal& a, const ExpReal& b);
    static ExpReal Add(const ExpReal& a, const ExpReal& b);
    static ExpReal Sub(const ExpReal& a, const ExpReal& b);
    static ExpReal Pow(const ExpReal& a, double power);

    bool IsValid() const { return !isNaN; }
    bool IsZero() const { return isZero && !isNaN; }
    double ToDouble() const;
    bool IsWithin(double limit) const;

private:
    static ExpReal Normalize(int sign, double mantissa, int exponent10);
};
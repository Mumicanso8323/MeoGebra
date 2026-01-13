#include "pch.h"
#include "ExpReal.h"
#include <algorithm>
#include <limits>

ExpReal ExpReal::FromDouble(double value) {
    if (std::isnan(value)) {
        ExpReal out;
        out.isNaN = true;
        return out;
    }
    if (value == 0.0) {
        return ExpReal();
    }
    int sign = value < 0.0 ? -1 : 1;
    double absValue = std::abs(value);
    double log10Value = std::log10(absValue);
    int exponent = static_cast<int>(std::floor(log10Value));
    double mantissa = absValue / std::pow(10.0, exponent);
    return Normalize(sign, mantissa, exponent);
}

ExpReal ExpReal::Exp(double value) {
    if (std::isnan(value)) {
        ExpReal out;
        out.isNaN = true;
        return out;
    }
    double log10Value = value / std::log(10.0);
    int exponent = static_cast<int>(std::floor(log10Value));
    double mantissa = std::pow(10.0, log10Value - exponent);
    return Normalize(1, mantissa, exponent);
}

ExpReal ExpReal::Mul(const ExpReal& a, const ExpReal& b) {
    if (a.isNaN || b.isNaN) {
        ExpReal out;
        out.isNaN = true;
        return out;
    }
    if (a.IsZero() || b.IsZero()) {
        return ExpReal();
    }
    int sign = a.sign * b.sign;
    double mantissa = a.mantissa * b.mantissa;
    int exponent = a.exponent10 + b.exponent10;
    return Normalize(sign, mantissa, exponent);
}

ExpReal ExpReal::Div(const ExpReal& a, const ExpReal& b) {
    if (a.isNaN || b.isNaN || b.IsZero()) {
        ExpReal out;
        out.isNaN = true;
        return out;
    }
    if (a.IsZero()) {
        return ExpReal();
    }
    int sign = a.sign * b.sign;
    double mantissa = a.mantissa / b.mantissa;
    int exponent = a.exponent10 - b.exponent10;
    return Normalize(sign, mantissa, exponent);
}

ExpReal ExpReal::Add(const ExpReal& a, const ExpReal& b) {
    if (a.isNaN || b.isNaN) {
        ExpReal out;
        out.isNaN = true;
        return out;
    }
    if (a.IsZero()) {
        return b;
    }
    if (b.IsZero()) {
        return a;
    }

    int diff = a.exponent10 - b.exponent10;
    if (std::abs(diff) > 16) {
        return diff > 0 ? a : b;
    }

    double aValue = a.sign * a.mantissa * std::pow(10.0, diff > 0 ? 0 : diff);
    double bValue = b.sign * b.mantissa * std::pow(10.0, diff > 0 ? -diff : 0);
    double sum = aValue + bValue;
    return Normalize(sum < 0.0 ? -1 : 1, std::abs(sum), diff > 0 ? a.exponent10 : b.exponent10);
}

ExpReal ExpReal::Sub(const ExpReal& a, const ExpReal& b) {
    ExpReal neg = b;
    neg.sign *= -1;
    return Add(a, neg);
}

ExpReal ExpReal::Pow(const ExpReal& a, double power) {
    if (a.isNaN) {
        ExpReal out;
        out.isNaN = true;
        return out;
    }
    if (a.IsZero()) {
        return ExpReal();
    }
    if (a.sign < 0.0 && std::abs(power - std::round(power)) > 1e-9) {
        ExpReal out;
        out.isNaN = true;
        return out;
    }
    double log10Value = std::log10(a.mantissa) + a.exponent10;
    double resultLog10 = log10Value * power;
    int exponent = static_cast<int>(std::floor(resultLog10));
    double mantissa = std::pow(10.0, resultLog10 - exponent);
    int sign = (a.sign < 0.0 && static_cast<int>(std::round(power)) % 2 != 0) ? -1 : 1;
    return Normalize(sign, mantissa, exponent);
}

ExpReal ExpReal::Normalize(int sign, double mantissa, int exponent10) {
    ExpReal out;
    if (mantissa == 0.0) {
        return out;
    }
    double absMantissa = std::abs(mantissa);
    int exponent = exponent10;
    while (absMantissa >= 10.0) {
        absMantissa /= 10.0;
        exponent++;
    }
    while (absMantissa > 0.0 && absMantissa < 1.0) {
        absMantissa *= 10.0;
        exponent--;
    }
    out.sign = sign < 0 ? -1 : 1;
    out.mantissa = absMantissa;
    out.exponent10 = exponent;
    out.isZero = false;
    return out;
}

bool ExpReal::IsWithin(double limit) const {
    if (isNaN) {
        return false;
    }
    if (IsZero()) {
        return true;
    }
    double absLimit = std::abs(limit);
    if (absLimit <= 0.0) {
        return false;
    }
    double log10Limit = std::log10(absLimit);
    double log10Value = std::log10(mantissa) + exponent10;
    return log10Value <= log10Limit;
}

double ExpReal::ToDouble() const {
    if (isNaN) {
        return std::numeric_limits<double>::quiet_NaN();
    }
    if (IsZero()) {
        return 0.0;
    }
    return sign * mantissa * std::pow(10.0, exponent10);
}
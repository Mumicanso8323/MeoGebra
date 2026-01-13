using System;
using System.Collections.Generic;
using MeoGebra.Models;
using MeoGebra.Services.Expression;

namespace MeoGebra.Services.Evaluation;

public static class ExpressionEvaluator {
    public static double Evaluate(BoundExpression expression, double x, double y, AngleMode angleMode, IReadOnlyDictionary<Guid, double[]> deps, int index) {
        return expression switch {
            BoundConstant c => c.Value,
            BoundVariable variable => variable.Kind == VariableKind.X ? x : y,
            BoundUnary u => EvaluateUnary(u, x, y, angleMode, deps, index),
            BoundBinary b => EvaluateBinary(b, x, y, angleMode, deps, index),
            BoundBuiltinCall call => EvaluateBuiltin(call, x, y, angleMode, deps, index),
            BoundFunctionCall call => deps.TryGetValue(call.FunctionId, out var values) && index < values.Length ? values[index] : double.NaN,
            BoundConditional conditional => EvaluateConditional(conditional, x, y, angleMode, deps, index),
            _ => double.NaN
        };
    }

    public static double EvaluateSurface(BoundExpression expression, double x, double y, AngleMode angleMode) {
        return Evaluate(expression, x, y, angleMode, new Dictionary<Guid, double[]>(), 0);
    }

    private static double EvaluateUnary(BoundUnary unary, double x, double y, AngleMode mode, IReadOnlyDictionary<Guid, double[]> deps, int index) {
        var value = Evaluate(unary.Operand, x, y, mode, deps, index);
        return unary.Operator switch {
            TokenKind.Minus => -value,
            TokenKind.Plus => value,
            TokenKind.Bang => IsTrue(value) ? 0 : 1,
            _ => value
        };
    }

    private static double EvaluateBinary(BoundBinary binary, double x, double y, AngleMode mode, IReadOnlyDictionary<Guid, double[]> deps, int index) {
        var left = Evaluate(binary.Left, x, y, mode, deps, index);
        var right = Evaluate(binary.Right, x, y, mode, deps, index);
        return binary.Operator switch {
            TokenKind.Plus => left + right,
            TokenKind.Minus => left - right,
            TokenKind.Star => left * right,
            TokenKind.Slash => right == 0 ? double.NaN : left / right,
            TokenKind.Caret => Math.Pow(left, right),
            TokenKind.Less => IsTrue(left < right),
            TokenKind.LessEquals => IsTrue(left <= right),
            TokenKind.Greater => IsTrue(left > right),
            TokenKind.GreaterEquals => IsTrue(left >= right),
            TokenKind.DoubleEquals => IsTrue(Math.Abs(left - right) < 1e-9),
            TokenKind.NotEquals => IsTrue(Math.Abs(left - right) >= 1e-9),
            TokenKind.AndAnd => IsTrue(IsTrue(left) && IsTrue(right)),
            TokenKind.OrOr => IsTrue(IsTrue(left) || IsTrue(right)),
            _ => double.NaN
        };
    }

    private static double EvaluateBuiltin(BoundBuiltinCall call, double x, double y, AngleMode mode, IReadOnlyDictionary<Guid, double[]> deps, int index) {
        var arg = Evaluate(call.Argument, x, y, mode, deps, index);
        var radians = mode == AngleMode.Degrees ? arg * Math.PI / 180.0 : arg;
        return call.Function switch {
            BuiltinFunction.Sin => Math.Sin(radians),
            BuiltinFunction.Cos => Math.Cos(radians),
            BuiltinFunction.Tan => Math.Tan(radians),
            BuiltinFunction.Log => Math.Log10(arg),
            BuiltinFunction.Ln => Math.Log(arg),
            BuiltinFunction.Exp => Math.Exp(arg),
            BuiltinFunction.Sqrt => arg < 0 ? double.NaN : Math.Sqrt(arg),
            BuiltinFunction.Abs => Math.Abs(arg),
            _ => double.NaN
        };
    }

    private static double EvaluateConditional(BoundConditional conditional, double x, double y, AngleMode mode, IReadOnlyDictionary<Guid, double[]> deps, int index) {
        var condition = Evaluate(conditional.Condition, x, y, mode, deps, index);
        return IsTrue(condition)
            ? Evaluate(conditional.WhenTrue, x, y, mode, deps, index)
            : Evaluate(conditional.WhenFalse, x, y, mode, deps, index);
    }

    private static bool IsTrue(double value) => !double.IsNaN(value) && Math.Abs(value) > 1e-9;
    private static double IsTrue(bool value) => value ? 1.0 : 0.0;
}

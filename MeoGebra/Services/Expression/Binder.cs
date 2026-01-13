using System;
using System.Collections.Generic;
using MeoGebra.Models;

namespace MeoGebra.Services.Expression;

public sealed class Binder {
    private readonly SymbolTable _symbols;
    private readonly DiagnosticBag _diagnostics = new();

    public Binder(SymbolTable symbols) {
        _symbols = symbols;
    }

    public BoundResult Bind(ExpressionNode node) {
        var expr = BindExpression(node);
        return new BoundResult(expr, _diagnostics);
    }

    private BoundExpression BindExpression(ExpressionNode node) => node switch {
        NumberNode number => new BoundConstant(number.Value),
        IdentifierNode identifier => BindIdentifier(identifier),
        UnaryNode unary => new BoundUnary(unary.Operator, BindExpression(unary.Operand)),
        BinaryNode binary => new BoundBinary(BindExpression(binary.Left), binary.Operator, BindExpression(binary.Right)),
        CallNode call => BindCall(call),
        ConditionalNode conditional => new BoundConditional(
            BindExpression(conditional.Condition),
            BindExpression(conditional.WhenTrue),
            BindExpression(conditional.WhenFalse)),
        _ => new BoundConstant(0)
    };

    private BoundExpression BindIdentifier(IdentifierNode identifier) {
        var name = identifier.Name;
        if (string.Equals(name, "x", StringComparison.OrdinalIgnoreCase)) {
            return new BoundVariable();
        }
        if (string.Equals(name, "pi", StringComparison.OrdinalIgnoreCase)) {
            return new BoundConstant(Math.PI);
        }
        if (string.Equals(name, "e", StringComparison.OrdinalIgnoreCase)) {
            return new BoundConstant(Math.E);
        }
        _diagnostics.Add(DiagnosticCategory.Bind, $"Unknown identifier '{name}'.");
        return new BoundConstant(0);
    }

    private BoundExpression BindCall(CallNode call) {
        if (call.Arguments.Count != 1) {
            _diagnostics.Add(DiagnosticCategory.Bind, $"Function '{call.FunctionName}' expects 1 argument.");
            return new BoundConstant(0);
        }

        var arg = BindExpression(call.Arguments[0]);
        if (TryBindBuiltin(call.FunctionName, out var builtin)) {
            return new BoundBuiltinCall(builtin, arg);
        }

        if (_symbols.TryGetId(call.FunctionName, out var id)) {
            return new BoundFunctionCall(id, arg);
        }

        _diagnostics.Add(DiagnosticCategory.Bind, $"Unknown function '{call.FunctionName}'.");
        return new BoundConstant(0);
    }

    private static bool TryBindBuiltin(string name, out BuiltinFunction builtin) {
        if (string.Equals(name, "sin", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Sin;
            return true;
        }
        if (string.Equals(name, "cos", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Cos;
            return true;
        }
        if (string.Equals(name, "tan", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Tan;
            return true;
        }
        if (string.Equals(name, "log", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Log;
            return true;
        }
        if (string.Equals(name, "ln", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Ln;
            return true;
        }
        if (string.Equals(name, "exp", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Exp;
            return true;
        }
        if (string.Equals(name, "sqrt", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Sqrt;
            return true;
        }
        if (string.Equals(name, "abs", StringComparison.OrdinalIgnoreCase)) {
            builtin = BuiltinFunction.Abs;
            return true;
        }
        builtin = default;
        return false;
    }
}

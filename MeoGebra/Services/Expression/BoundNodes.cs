using System;
using System.Collections.Generic;
using MeoGebra.Models;

namespace MeoGebra.Services.Expression;

public abstract record BoundExpression {
    public abstract IEnumerable<Guid> EnumerateDependencies();
}

public sealed record BoundConstant(double Value) : BoundExpression {
    public override IEnumerable<Guid> EnumerateDependencies() => Array.Empty<Guid>();
}

public enum VariableKind {
    X,
    Y
}

public sealed record BoundVariable(VariableKind Kind) : BoundExpression {
    public override IEnumerable<Guid> EnumerateDependencies() => Array.Empty<Guid>();
}

public sealed record BoundUnary(TokenKind Operator, BoundExpression Operand) : BoundExpression {
    public override IEnumerable<Guid> EnumerateDependencies() => Operand.EnumerateDependencies();
}

public sealed record BoundBinary(BoundExpression Left, TokenKind Operator, BoundExpression Right) : BoundExpression {
    public override IEnumerable<Guid> EnumerateDependencies() {
        foreach (var id in Left.EnumerateDependencies()) {
            yield return id;
        }
        foreach (var id in Right.EnumerateDependencies()) {
            yield return id;
        }
    }
}

public enum BuiltinFunction {
    Sin,
    Cos,
    Tan,
    Log,
    Ln,
    Exp,
    Sqrt,
    Abs
}

public sealed record BoundBuiltinCall(BuiltinFunction Function, BoundExpression Argument) : BoundExpression {
    public override IEnumerable<Guid> EnumerateDependencies() => Argument.EnumerateDependencies();
}

public sealed record BoundFunctionCall(Guid FunctionId, BoundExpression Argument) : BoundExpression {
    public override IEnumerable<Guid> EnumerateDependencies() {
        yield return FunctionId;
        foreach (var id in Argument.EnumerateDependencies()) {
            yield return id;
        }
    }
}

public sealed record BoundConditional(BoundExpression Condition, BoundExpression WhenTrue, BoundExpression WhenFalse) : BoundExpression {
    public override IEnumerable<Guid> EnumerateDependencies() {
        foreach (var id in Condition.EnumerateDependencies()) {
            yield return id;
        }
        foreach (var id in WhenTrue.EnumerateDependencies()) {
            yield return id;
        }
        foreach (var id in WhenFalse.EnumerateDependencies()) {
            yield return id;
        }
    }
}

public sealed record BoundResult(BoundExpression? Expression, DiagnosticBag Diagnostics);

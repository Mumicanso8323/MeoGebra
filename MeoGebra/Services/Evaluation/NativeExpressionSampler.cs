using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MeoGebra.Models;
using MeoGebra.NativeInterop;
using MeoGebra.Services.Expression;

namespace MeoGebra.Services.Evaluation;

public sealed class NativeExpressionSampler : IExpressionSampler {
    private readonly ManagedExpressionSampler _fallback = new();

    public Task<FunctionRenderCache> SampleAsync(BoundFunction function, EvaluationContext context, CancellationToken token) {
        if (!TryGetNativeKind(function.Expression, out var kind)) {
            return _fallback.SampleAsync(function, context, token);
        }
        return Task.Run(() => SampleNative(function, context, kind), token);
    }

    private static bool TryGetNativeKind(BoundExpression expression, out FunctionKind kind) {
        kind = FunctionKind.ExpCancel;
        return IsExpCancelPattern(expression);
    }

    private static bool IsExpCancelPattern(BoundExpression expression) {
        if (expression is not BoundBinary binary || binary.Operator != TokenKind.Slash) {
            return false;
        }
        return IsExpOfXSquare(binary.Left) && IsExpOfXSquareMinusOne(binary.Right);
    }

    private static bool IsExpOfXSquare(BoundExpression expression) {
        return expression is BoundBuiltinCall { Function: BuiltinFunction.Exp, Argument: BoundBinary { Operator: TokenKind.Caret } } expCall
               && expCall.Argument is BoundBinary power
               && IsX(power.Left)
               && IsNumber(power.Right, 2);
    }

    private static bool IsExpOfXSquareMinusOne(BoundExpression expression) {
        if (expression is not BoundBuiltinCall { Function: BuiltinFunction.Exp } expCall) {
            return false;
        }
        if (expCall.Argument is not BoundBinary { Operator: TokenKind.Minus } minus) {
            return false;
        }
        return IsXSquare(minus.Left) && IsNumber(minus.Right, 1);
    }

    private static bool IsXSquare(BoundExpression? expression) {
        return expression is BoundBinary { Operator: TokenKind.Caret } power
               && IsX(power.Left)
               && IsNumber(power.Right, 2);
    }

    private static bool IsX(BoundExpression? expression) => expression is BoundVariable { Kind: VariableKind.X };

    private static bool IsNumber(BoundExpression? expression, double value) => expression is BoundConstant constant && Math.Abs(constant.Value - value) < 1e-9;

    private static FunctionRenderCache SampleNative(BoundFunction function, EvaluationContext context, FunctionKind kind) {
        var segments = new List<FunctionSegment>();
        var current = new List<SamplePoint>();
        var step = (context.X1 - context.X0) / (context.Samples - 1);
        var yLimit = Math.Max(context.Viewport.ScaleY * 5.0, 1.0);

        var results = new SampleResult[context.Samples];
        var handle = GCHandle.Alloc(results, GCHandleType.Pinned);
        try {
            NativeMethods.SampleFunctionExp(
                kind,
                context.X0,
                context.X1,
                context.Samples,
                new FunctionParams { A = 1, B = 1 },
                yLimit,
                handle.AddrOfPinnedObject());
        } finally {
            handle.Free();
        }

        for (var i = 0; i < context.Samples; i++) {
            var x = context.X0 + step * i;
            if (function.XMin.HasValue && x < function.XMin.Value) {
                Flush();
                continue;
            }
            if (function.XMax.HasValue && x > function.XMax.Value) {
                Flush();
                continue;
            }

            var result = results[i];
            if (result.Valid == 0 || double.IsNaN(result.Y) || double.IsInfinity(result.Y)) {
                Flush();
                continue;
            }
            current.Add(new SamplePoint(result.X, result.Y));
        }
        Flush();

        return new FunctionRenderCache(segments);

        void Flush() {
            if (current.Count > 1) {
                segments.Add(new FunctionSegment(current));
            }
            current = new List<SamplePoint>();
        }
    }
}

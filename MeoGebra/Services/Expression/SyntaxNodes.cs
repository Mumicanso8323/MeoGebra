using System.Collections.Generic;

namespace MeoGebra.Services.Expression;

public abstract record ExpressionNode;

public sealed record NumberNode(double Value) : ExpressionNode;
public sealed record IdentifierNode(string Name) : ExpressionNode;
public sealed record UnaryNode(TokenKind Operator, ExpressionNode Operand) : ExpressionNode;
public sealed record BinaryNode(ExpressionNode Left, TokenKind Operator, ExpressionNode Right) : ExpressionNode;
public sealed record CallNode(string FunctionName, IReadOnlyList<ExpressionNode> Arguments) : ExpressionNode;
public sealed record ConditionalNode(ExpressionNode Condition, ExpressionNode WhenTrue, ExpressionNode WhenFalse) : ExpressionNode;

public sealed record ExpressionInput(string? DefinedName, ExpressionNode Body);

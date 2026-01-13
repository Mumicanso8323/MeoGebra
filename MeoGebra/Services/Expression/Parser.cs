using System.Collections.Generic;

namespace MeoGebra.Services.Expression;

public sealed class Parser {
    private readonly List<Token> _tokens;
    private int _index;

    public Parser(string text) {
        _tokens = new Lexer(text).Lex();
    }

    public ExpressionInput ParseExpressionInput() {
        if (TryParseFunctionHeader(out var name, out var parameters)) {
            var body = ParseExpression();
            return new ExpressionInput(name, parameters, body);
        }

        return new ExpressionInput(null, new List<string>(), ParseExpression());
    }

    private bool TryParseFunctionHeader(out string name, out List<string> parameters) {
        name = string.Empty;
        parameters = new List<string>();
        if (Peek().Kind != TokenKind.Identifier) {
            return false;
        }
        if (Peek(1).Kind != TokenKind.LParen) {
            return false;
        }
        var tempIndex = _index;
        _ = NextToken(); // name
        _ = Match(TokenKind.LParen);
        if (!Check(TokenKind.RParen)) {
            do {
                if (!Match(TokenKind.Identifier)) {
                    _index = tempIndex;
                    return false;
                }
                parameters.Add(Previous().Text);
            } while (Match(TokenKind.Comma));
        }
        if (!Match(TokenKind.RParen)) {
            _index = tempIndex;
            return false;
        }
        if (!Match(TokenKind.Equals)) {
            _index = tempIndex;
            return false;
        }
        name = _tokens[tempIndex].Text;
        return true;
    }

    private ExpressionNode ParseExpression() => ParseConditional();

    private ExpressionNode ParseConditional() {
        var condition = ParseLogicalOr();
        if (Match(TokenKind.Question)) {
            var whenTrue = ParseExpression();
            _ = Match(TokenKind.Colon);
            var whenFalse = ParseExpression();
            return new ConditionalNode(condition, whenTrue, whenFalse);
        }
        return condition;
    }

    private ExpressionNode ParseLogicalOr() {
        var left = ParseLogicalAnd();
        while (Match(TokenKind.OrOr)) {
            var op = Previous().Kind;
            var right = ParseLogicalAnd();
            left = new BinaryNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParseLogicalAnd() {
        var left = ParseEquality();
        while (Match(TokenKind.AndAnd)) {
            var op = Previous().Kind;
            var right = ParseEquality();
            left = new BinaryNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParseEquality() {
        var left = ParseComparison();
        while (Match(TokenKind.DoubleEquals) || Match(TokenKind.NotEquals)) {
            var op = Previous().Kind;
            var right = ParseComparison();
            left = new BinaryNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParseComparison() {
        var left = ParseTerm();
        while (Match(TokenKind.Less) || Match(TokenKind.LessEquals) || Match(TokenKind.Greater) || Match(TokenKind.GreaterEquals)) {
            var op = Previous().Kind;
            var right = ParseTerm();
            left = new BinaryNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParseTerm() {
        var left = ParseFactor();
        while (Match(TokenKind.Plus) || Match(TokenKind.Minus)) {
            var op = Previous().Kind;
            var right = ParseFactor();
            left = new BinaryNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParseFactor() {
        var left = ParsePower();
        while (Match(TokenKind.Star) || Match(TokenKind.Slash)) {
            var op = Previous().Kind;
            var right = ParsePower();
            left = new BinaryNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParsePower() {
        var left = ParseUnary();
        if (Match(TokenKind.Caret)) {
            var op = Previous().Kind;
            var right = ParsePower();
            return new BinaryNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParseUnary() {
        if (Match(TokenKind.Minus) || Match(TokenKind.Plus) || Match(TokenKind.Bang)) {
            var op = Previous().Kind;
            var right = ParseUnary();
            return new UnaryNode(op, right);
        }
        return ParsePrimary();
    }

    private ExpressionNode ParsePrimary() {
        if (Match(TokenKind.Number)) {
            return new NumberNode(Previous().NumberValue ?? 0);
        }

        if (Match(TokenKind.Identifier)) {
            var name = Previous().Text;
            if (Match(TokenKind.LParen)) {
                var args = new List<ExpressionNode>();
                if (!Check(TokenKind.RParen)) {
                    do {
                        args.Add(ParseExpression());
                    } while (Match(TokenKind.Comma));
                }
                _ = Match(TokenKind.RParen);
                return new CallNode(name, args);
            }
            return new IdentifierNode(name);
        }

        if (Match(TokenKind.LParen)) {
            var expr = ParseExpression();
            _ = Match(TokenKind.RParen);
            return expr;
        }

        return new NumberNode(0);
    }

    private bool Match(TokenKind kind) {
        if (Check(kind)) {
            _index++;
            return true;
        }
        return false;
    }

    private bool Check(TokenKind kind) => Peek().Kind == kind;

    private Token NextToken() => _tokens[_index++];

    private Token Peek(int offset = 0) {
        var idx = _index + offset;
        if (idx >= _tokens.Count) {
            return _tokens[^1];
        }
        return _tokens[idx];
    }

    private Token Previous() => _tokens[_index - 1];
}

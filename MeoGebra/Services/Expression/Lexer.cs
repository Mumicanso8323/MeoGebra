using System;
using System.Collections.Generic;
using System.Globalization;

namespace MeoGebra.Services.Expression;

public enum TokenKind {
    End,
    Number,
    Identifier,
    Plus,
    Minus,
    Star,
    Slash,
    Caret,
    LParen,
    RParen,
    Comma,
    Question,
    Colon,
    Equals,
    DoubleEquals,
    NotEquals,
    Less,
    LessEquals,
    Greater,
    GreaterEquals,
    AndAnd,
    OrOr,
    Bang
}

public readonly record struct Token(TokenKind Kind, string Text, double? NumberValue = null);

public sealed class Lexer {
    private readonly string _text;
    private int _index;

    public Lexer(string text) {
        _text = text ?? string.Empty;
    }

    public List<Token> Lex() {
        var tokens = new List<Token>();
        Token token;
        do {
            token = NextToken();
            tokens.Add(token);
        } while (token.Kind != TokenKind.End);
        return tokens;
    }

    private Token NextToken() {
        SkipWhitespace();
        if (_index >= _text.Length) {
            return new Token(TokenKind.End, string.Empty);
        }

        var c = _text[_index];
        if (char.IsDigit(c) || c == '.') {
            return ReadNumber();
        }

        if (char.IsLetter(c) || c == '_') {
            return ReadIdentifier();
        }

        _index++;
        return c switch {
            '+' => new Token(TokenKind.Plus, "+"),
            '-' => new Token(TokenKind.Minus, "-"),
            '*' => new Token(TokenKind.Star, "*"),
            '/' => new Token(TokenKind.Slash, "/"),
            '^' => new Token(TokenKind.Caret, "^"),
            '(' => new Token(TokenKind.LParen, "("),
            ')' => new Token(TokenKind.RParen, ")"),
            ',' => new Token(TokenKind.Comma, ","),
            '?' => new Token(TokenKind.Question, "?"),
            ':' => new Token(TokenKind.Colon, ":"),
            '!' => Match('=') ? new Token(TokenKind.NotEquals, "!=") : new Token(TokenKind.Bang, "!"),
            '=' => Match('=') ? new Token(TokenKind.DoubleEquals, "==") : new Token(TokenKind.Equals, "="),
            '<' => Match('=') ? new Token(TokenKind.LessEquals, "<=") : new Token(TokenKind.Less, "<"),
            '>' => Match('=') ? new Token(TokenKind.GreaterEquals, ">=") : new Token(TokenKind.Greater, ">"),
            '&' => Match('&') ? new Token(TokenKind.AndAnd, "&&") : new Token(TokenKind.End, string.Empty),
            '|' => Match('|') ? new Token(TokenKind.OrOr, "||") : new Token(TokenKind.End, string.Empty),
            _ => new Token(TokenKind.End, string.Empty)
        };
    }

    private Token ReadNumber() {
        var start = _index;
        var hasDot = false;
        while (_index < _text.Length) {
            var c = _text[_index];
            if (char.IsDigit(c)) {
                _index++;
                continue;
            }
            if (c == '.' && !hasDot) {
                hasDot = true;
                _index++;
                continue;
            }
            break;
        }
        var slice = _text[start.._index];
        if (double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
            return new Token(TokenKind.Number, slice, value);
        }
        return new Token(TokenKind.Number, slice, 0);
    }

    private Token ReadIdentifier() {
        var start = _index;
        while (_index < _text.Length) {
            var c = _text[_index];
            if (char.IsLetterOrDigit(c) || c == '_') {
                _index++;
            } else {
                break;
            }
        }
        var text = _text[start.._index];
        return new Token(TokenKind.Identifier, text);
    }

    private bool Match(char expected) {
        if (_index >= _text.Length) {
            return false;
        }
        if (_text[_index] != expected) {
            return false;
        }
        _index++;
        return true;
    }

    private void SkipWhitespace() {
        while (_index < _text.Length && char.IsWhiteSpace(_text[_index])) {
            _index++;
        }
    }
}

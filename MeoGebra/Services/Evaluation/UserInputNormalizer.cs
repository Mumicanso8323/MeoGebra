using System;
using System.Text.RegularExpressions;

namespace MeoGebra.Services.Expression;

public static class UserInputNormalizer {
    public static string NormalizeUserExpression(string raw) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return string.Empty;
        }

        var normalized = raw.Trim();
        var lastEquals = normalized.LastIndexOf('=');
        if (lastEquals >= 0 && lastEquals + 1 < normalized.Length) {
            normalized = normalized[(lastEquals + 1)..];
        }

        normalized = normalized.Replace("\\left", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\\right", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\\sin", "sin", StringComparison.OrdinalIgnoreCase)
            .Replace("\\cos", "cos", StringComparison.OrdinalIgnoreCase)
            .Replace("\\tan", "tan", StringComparison.OrdinalIgnoreCase)
            .Replace("\\ln", "log", StringComparison.OrdinalIgnoreCase);

        normalized = Regex.Replace(normalized, @"\\sqrt\s*\{([^}]*)\}", "sqrt($1)");

        return normalized.Trim();
    }
}
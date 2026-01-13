using System.Collections.Generic;

namespace MeoGebra.Models;

public enum DiagnosticCategory {
    Parse,
    Bind,
    Domain,
    Overflow,
    Timeout
}

public sealed class Diagnostic {
    public Diagnostic(DiagnosticCategory category, string message) {
        Category = category;
        Message = message;
    }

    public DiagnosticCategory Category { get; }
    public string Message { get; }

    public override string ToString() => $"[{Category}] {Message}";
}

public sealed class DiagnosticBag {
    private readonly List<Diagnostic> _items = new();

    public IReadOnlyList<Diagnostic> Items => _items;

    public void Add(DiagnosticCategory category, string message) {
        _items.Add(new Diagnostic(category, message));
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics) {
        _items.AddRange(diagnostics);
    }
}

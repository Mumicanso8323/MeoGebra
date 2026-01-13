using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MeoGebra.Models;

public enum AngleMode {
    Degrees,
    Radians
}

public enum PlotMode {
    TwoD,
    ThreeD
}

public sealed class Document {
    public Guid DocumentId { get; set; } = Guid.NewGuid();
    public AngleMode AngleMode { get; set; } = AngleMode.Radians;
    public ViewportState Viewport { get; set; } = new(0, 0, 10, 10);
    public PlotMode PlotMode { get; set; } = PlotMode.TwoD;
    public Guid? SelectedSurfaceFunctionId { get; set; }
    public List<FunctionObject> Functions { get; set; } = new();
    [JsonPropertyName("UserMarkdownNotes")]
    public string UserNotes { get; set; } = string.Empty;
    [JsonIgnore]
    public SymbolTable Symbols { get; } = new();

    public Document Clone() {
        var clone = new Document {
            DocumentId = DocumentId,
            AngleMode = AngleMode,
            Viewport = Viewport,
            PlotMode = PlotMode,
            SelectedSurfaceFunctionId = SelectedSurfaceFunctionId,
            UserNotes = UserNotes
        };
        foreach (var function in Functions) {
            clone.Functions.Add(new FunctionObject {
                Id = function.Id,
                Name = function.Name,
                ExpressionText = function.ExpressionText,
                Parameters = function.Parameters.ToArray(),
                IsVisible = function.IsVisible,
                PaletteIndex = function.PaletteIndex,
                XMin = function.XMin,
                XMax = function.XMax
            });
        }
        clone.Symbols.Restore(Functions);
        return clone;
    }

    public FunctionObject? FindFunction(Guid id) => Functions.FirstOrDefault(f => f.Id == id);
}

public readonly record struct ViewportState(double CenterX, double CenterY, double ScaleX, double ScaleY);

public sealed class SymbolTable {
    private readonly Dictionary<string, Guid> _nameToId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, HashSet<string>> _aliases = new();

    public IReadOnlyDictionary<string, Guid> NameToId => _nameToId;

    public void Restore(IEnumerable<FunctionObject> functions) {
        _nameToId.Clear();
        _aliases.Clear();
        foreach (var function in functions) {
            if (!string.IsNullOrWhiteSpace(function.Name)) {
                SetName(function.Name, function.Id, keepOldAliases: false);
            }
        }
    }

    public bool TryGetId(string name, out Guid id) => _nameToId.TryGetValue(name, out id);

    public void SetName(string name, Guid id, bool keepOldAliases = true) {
        if (!_aliases.TryGetValue(id, out var aliasSet)) {
            aliasSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _aliases[id] = aliasSet;
        }
        aliasSet.Add(name);
        _nameToId[name] = id;
        if (!keepOldAliases) {
            var keys = new List<string>(aliasSet);
            foreach (var key in keys) {
                if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) {
                    _nameToId.Remove(key);
                    aliasSet.Remove(key);
                }
            }
        }
    }

    public void Remove(Guid id) {
        if (_aliases.TryGetValue(id, out var aliasSet)) {
            foreach (var key in aliasSet) {
                _nameToId.Remove(key);
            }
            _aliases.Remove(id);
        }
    }
}

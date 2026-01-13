using System;
using MeoGebra.Models;

namespace MeoGebra.Services.History;

public sealed class AddFunctionCommand : IHistoryCommand {
    private readonly Document _document;
    private readonly FunctionObject _function;

    public AddFunctionCommand(Document document, FunctionObject function) {
        _document = document;
        _function = function;
    }

    public string Description => "Add function";

    public void Execute() {
        _document.Functions.Add(_function);
        _document.Symbols.SetName(_function.Name, _function.Id);
    }

    public void Undo() {
        _document.Functions.Remove(_function);
        _document.Symbols.Remove(_function.Id);
    }
}

public sealed class RemoveFunctionCommand : IHistoryCommand {
    private readonly Document _document;
    private readonly FunctionObject _function;
    private int _index;

    public RemoveFunctionCommand(Document document, FunctionObject function) {
        _document = document;
        _function = function;
    }

    public string Description => "Remove function";

    public void Execute() {
        _index = _document.Functions.IndexOf(_function);
        _document.Functions.Remove(_function);
        _document.Symbols.Remove(_function.Id);
    }

    public void Undo() {
        if (_index < 0 || _index > _document.Functions.Count) {
            _document.Functions.Add(_function);
        } else {
            _document.Functions.Insert(_index, _function);
        }
        _document.Symbols.SetName(_function.Name, _function.Id);
    }
}

public sealed class RenameFunctionCommand : IHistoryCommand {
    private readonly Document _document;
    private readonly FunctionObject _function;
    private readonly string _newName;
    private string _oldName;

    public RenameFunctionCommand(Document document, FunctionObject function, string newName) {
        _document = document;
        _function = function;
        _newName = newName;
        _oldName = function.Name;
    }

    public string Description => "Rename function";

    public void Execute() {
        _oldName = _function.Name;
        _function.Name = _newName;
        _document.Symbols.SetName(_newName, _function.Id);
    }

    public void Undo() {
        _function.Name = _oldName;
        _document.Symbols.SetName(_oldName, _function.Id);
    }
}

public sealed class EditExpressionCommand : IHistoryCommand {
    private readonly FunctionObject _function;
    private readonly string _newText;
    private string _oldText;

    public EditExpressionCommand(FunctionObject function, string newText) {
        _function = function;
        _newText = newText;
        _oldText = function.ExpressionText;
    }

    public string Description => "Edit expression";

    public void Execute() {
        _oldText = _function.ExpressionText;
        _function.ExpressionText = _newText;
    }

    public void Undo() {
        _function.ExpressionText = _oldText;
    }
}

public sealed class ToggleVisibleCommand : IHistoryCommand {
    private readonly FunctionObject _function;
    private bool _oldValue;

    public ToggleVisibleCommand(FunctionObject function, bool newValue) {
        _function = function;
        _oldValue = function.IsVisible;
        NewValue = newValue;
    }

    public string Description => "Toggle visibility";
    public bool NewValue { get; }

    public void Execute() {
        _oldValue = _function.IsVisible;
        _function.IsVisible = NewValue;
    }

    public void Undo() {
        _function.IsVisible = _oldValue;
    }
}

public sealed class ChangeColorCommand : IHistoryCommand {
    private readonly FunctionObject _function;
    private readonly int _newIndex;
    private int _oldIndex;

    public ChangeColorCommand(FunctionObject function, int newIndex) {
        _function = function;
        _newIndex = newIndex;
        _oldIndex = function.PaletteIndex;
    }

    public string Description => "Change color";

    public void Execute() {
        _oldIndex = _function.PaletteIndex;
        _function.PaletteIndex = _newIndex;
    }

    public void Undo() {
        _function.PaletteIndex = _oldIndex;
    }
}

public sealed class AutoFitCommand : IHistoryCommand {
    private readonly Document _document;
    private readonly ViewportState _newViewport;
    private ViewportState _oldViewport;

    public AutoFitCommand(Document document, ViewportState newViewport) {
        _document = document;
        _newViewport = newViewport;
        _oldViewport = document.Viewport;
    }

    public string Description => "Auto-fit";

    public void Execute() {
        _oldViewport = _document.Viewport;
        _document.Viewport = _newViewport;
    }

    public void Undo() {
        _document.Viewport = _oldViewport;
    }
}

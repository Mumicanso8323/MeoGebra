using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MeoGebra.Models;
using MeoGebra.Services.History;

namespace MeoGebra.ViewModels;

public partial class FunctionRowViewModel : ObservableObject {
    private readonly FunctionObject _function;
    private readonly Document _document;
    private readonly HistoryService _history;
    private readonly Action _onEdited;
    private string _committedExpression;

    public FunctionRowViewModel(FunctionObject function, Document document, HistoryService history, Action onEdited) {
        _function = function;
        _document = document;
        _history = history;
        _onEdited = onEdited;
        _committedExpression = function.ExpressionText;
    }

    public Guid Id => _function.Id;
    public string[] Parameters => _function.Parameters;

    public string Name {
        get => _function.Name;
        set {
            if (_function.Name == value) {
                return;
            }
            _history.Execute(new RenameFunctionCommand(_document, _function, value), _document);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
            _onEdited();
        }
    }

    public string ExpressionText {
        get => _function.ExpressionText;
        set {
            if (_committedExpression == value) {
                return;
            }
            _history.Execute(new EditExpressionCommand(_function, _committedExpression, value), _document);
            _committedExpression = value;
            OnPropertyChanged();
            _onEdited();
        }
    }

    public string PreviewExpressionText {
        get => _function.ExpressionText;
        set {
            if (_function.ExpressionText == value) {
                return;
            }
            _function.ExpressionText = value;
            OnPropertyChanged(nameof(ExpressionText));
            _onEdited();
        }
    }

    public bool IsVisible {
        get => _function.IsVisible;
        set {
            if (_function.IsVisible == value) {
                return;
            }
            _history.Execute(new ToggleVisibleCommand(_function, value), _document);
            OnPropertyChanged();
            _onEdited();
        }
    }

    public int PaletteIndex {
        get => _function.PaletteIndex;
        set {
            if (_function.PaletteIndex == value) {
                return;
            }
            _history.Execute(new ChangeColorCommand(_function, value), _document);
            OnPropertyChanged();
            _onEdited();
        }
    }

    public bool HasDiagnostics => _function.HasDiagnostics;
    public string DiagnosticSummary => string.Join("; ", _function.Diagnostics.Select(d => d.ToString()));

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name;
    
    public void Refresh() {
        _committedExpression = _function.ExpressionText;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(HasDiagnostics));
        OnPropertyChanged(nameof(DiagnosticSummary));
        OnPropertyChanged(nameof(ExpressionText));
        OnPropertyChanged(nameof(PreviewExpressionText));
        OnPropertyChanged(nameof(Parameters));
    }
}

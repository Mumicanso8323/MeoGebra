using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeoGebra.Models;
using MeoGebra.Plot;
using MeoGebra.Services;
using MeoGebra.Services.Evaluation;
using MeoGebra.Services.History;
using OxyPlot;

namespace MeoGebra.ViewModels;

public partial class MainViewModel : ObservableObject {
    private const int SampleCount = 2000;
    private readonly EvaluationPipeline _pipeline;
    private readonly HistoryService _history = new();

    public MainViewModel() {
        Document = CreateDefaultDocument();
        PaletteOptions = BuildPaletteOptions();
        Functions = new ObservableCollection<FunctionRowViewModel>(
            Document.Functions.Select(f => new FunctionRowViewModel(f, Document, _history, RequestEvaluation)));

        _pipeline = new EvaluationPipeline(new ManagedExpressionSampler());
        PlotModel = PlotModelFactory.CreateEmpty(Document.Viewport);
        UserNotes = Document.UserMarkdownNotes;
        RequestEvaluation();
    }

    public Document Document { get; private set; }

    [ObservableProperty] private PlotModel plotModel;
    [ObservableProperty] private string markdownSummary = string.Empty;
    [ObservableProperty] private string userNotes = string.Empty;

    public ObservableCollection<FunctionRowViewModel> Functions { get; private set; }
    public IReadOnlyList<PaletteOption> PaletteOptions { get; }

    public AngleMode AngleMode {
        get => Document.AngleMode;
        set {
            if (Document.AngleMode != value) {
                Document.AngleMode = value;
                OnPropertyChanged();
                RequestEvaluation();
            }
        }
    }

    partial void OnUserNotesChanged(string value) {
        Document.UserMarkdownNotes = value;
        MarkdownSummary = BuildMarkdownSummary();
    }

    [RelayCommand]
    private void AddFunction() {
        var function = new FunctionObject {
            Name = SuggestName(),
            ExpressionText = "sin(x)",
            PaletteIndex = Functions.Count % PaletteOptions.Count
        };
        _history.Execute(new AddFunctionCommand(Document, function), Document);
        Functions.Add(new FunctionRowViewModel(function, Document, _history, RequestEvaluation));
        RequestEvaluation();
    }

    [RelayCommand]
    private void RemoveFunction(FunctionRowViewModel? function) {
        if (function is null) {
            return;
        }
        var model = Document.FindFunction(function.Id);
        if (model is null) {
            return;
        }
        _history.Execute(new RemoveFunctionCommand(Document, model), Document);
        Functions.Remove(function);
        RequestEvaluation();
    }

    [RelayCommand]
    private void Undo() {
        _history.Undo();
        RebuildFunctionViewModels();
        RequestEvaluation();
    }

    [RelayCommand]
    private void Redo() {
        _history.Redo();
        RebuildFunctionViewModels();
        RequestEvaluation();
    }

    [RelayCommand]
    private void ToggleAngleMode() {
        AngleMode = AngleMode == AngleMode.Degrees ? AngleMode.Radians : AngleMode.Degrees;
    }

    [RelayCommand]
    private void AutoFit() {
        var bounds = ComputeBoundsFromSegments();
        if (bounds is null) {
            return;
        }
        var (minX, maxX, minY, maxY) = bounds.Value;
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;
        var scaleX = Math.Max((maxX - minX) / 2, 1e-3);
        var scaleY = Math.Max((maxY - minY) / 2, 1e-3);
        scaleX *= 1.1;
        scaleY *= 1.1;
        var viewport = new ViewportState(centerX, centerY, scaleX, scaleY);
        _history.Execute(new AutoFitCommand(Document, viewport), Document);
        RequestEvaluation();
    }

    [RelayCommand]
    private void SaveDocument() {
        var dialog = new Microsoft.Win32.SaveFileDialog {
            Filter = "MeoGebra Document (*.json)|*.json",
            DefaultExt = ".json"
        };
        if (dialog.ShowDialog() == true) {
            DocumentPersistence.Save(Document, dialog.FileName);
        }
    }

    [RelayCommand]
    private void LoadDocument() {
        var dialog = new Microsoft.Win32.OpenFileDialog {
            Filter = "MeoGebra Document (*.json)|*.json",
            DefaultExt = ".json"
        };
        if (dialog.ShowDialog() == true) {
            Document = DocumentPersistence.Load(dialog.FileName);
            RebuildFunctionViewModels();
            UserNotes = Document.UserMarkdownNotes;
            RequestEvaluation();
        }
    }

    private void RebuildFunctionViewModels() {
        Functions = new ObservableCollection<FunctionRowViewModel>(
            Document.Functions.Select(f => new FunctionRowViewModel(f, Document, _history, RequestEvaluation)));
        OnPropertyChanged(nameof(Functions));
    }

    private void RequestEvaluation() {
        _ = _pipeline.RequestEvaluationAsync(Document, SampleCount, result =>
            Application.Current.Dispatcher.Invoke(() => ApplyEvaluationResult(result)));
    }

    private void ApplyEvaluationResult(EvaluationResult result) {
        foreach (var function in Document.Functions) {
            function.Diagnostics.Clear();
            if (result.Diagnostics.TryGetValue(function.Id, out var diag)) {
                function.Diagnostics.AddRange(diag);
            }
            if (result.RenderCaches.TryGetValue(function.Id, out var cache)) {
                function.RenderCache = cache;
            } else {
                function.RenderCache = new FunctionRenderCache(new List<FunctionSegment>());
            }
        }

        PlotModel = PlotModelFactory.CreateDocumentPlot(Document, PaletteProvider.Colors);
        MarkdownSummary = BuildMarkdownSummary();

        foreach (var row in Functions) {
            row.Refresh();
        }
    }

    private string BuildMarkdownSummary() {
        var lines = new List<string> {
            $"# Document",
            $"- AngleMode: **{Document.AngleMode}**",
            $"- Function count: **{Document.Functions.Count}**",
            "",
            "## Diagnostics",
            "| Function | Diagnostics |",
            "| --- | --- |"
        };

        foreach (var function in Document.Functions) {
            var diag = function.Diagnostics.Count == 0
                ? "OK"
                : string.Join("<br/>", function.Diagnostics.Select(d => d.ToString()));
            lines.Add($"| {function.Name} | {diag} |");
        }

        lines.Add("");
        lines.Add("## Notes");
        lines.Add(UserNotes);
        return string.Join(Environment.NewLine, lines);
    }

    private (double minX, double maxX, double minY, double maxY)? ComputeBoundsFromSegments() {
        var points = Document.Functions
            .Where(f => f.IsVisible && f.RenderCache is not null)
            .SelectMany(f => f.RenderCache!.Segments)
            .SelectMany(seg => seg.Points)
            .ToList();

        if (points.Count < 2) {
            return null;
        }

        var xs = points.Select(p => p.X).OrderBy(x => x).ToList();
        var ys = points.Select(p => p.Y).OrderBy(y => y).ToList();

        var minX = Percentile(xs, 0.05);
        var maxX = Percentile(xs, 0.95);
        var minY = Percentile(ys, 0.05);
        var maxY = Percentile(ys, 0.95);

        return (minX, maxX, minY, maxY);
    }

    private static double Percentile(IReadOnlyList<double> sorted, double percentile) {
        if (sorted.Count == 0) {
            return 0;
        }
        var index = (int)Math.Round((sorted.Count - 1) * percentile);
        index = Math.Clamp(index, 0, sorted.Count - 1);
        return sorted[index];
    }

    private static Document CreateDefaultDocument() {
        var document = new Document();
        document.Functions.Add(new FunctionObject { Name = "f", ExpressionText = "sin(x)", PaletteIndex = 0 });
        document.Symbols.Restore(document.Functions);
        return document;
    }

    private string SuggestName() {
        var baseName = "f";
        var index = 1;
        var name = baseName;
        while (Document.Symbols.NameToId.ContainsKey(name)) {
            name = $"{baseName}{index}";
            index++;
        }
        return name;
    }

    private static IReadOnlyList<PaletteOption> BuildPaletteOptions() {
        var items = new List<PaletteOption>();
        for (var i = 0; i < PaletteProvider.Colors.Count; i++) {
            var color = PaletteProvider.Colors[i];
            var brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            brush.Freeze();
            items.Add(new PaletteOption(i, brush));
        }
        return items;
    }
}

public sealed record PaletteOption(int Index, SolidColorBrush Color);

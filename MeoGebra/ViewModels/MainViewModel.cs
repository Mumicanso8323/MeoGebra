using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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

        _pipeline = new EvaluationPipeline(new NativeExpressionSampler());
        PlotModel = PlotModelFactory.CreateEmpty(Document.Viewport);
        UserNotes = Document.UserNotes;
        PlotModeOptions = Enum.GetValues<PlotMode>();
        SurfaceFunctions = new ObservableCollection<FunctionRowViewModel>(Functions.Where(f => f.Parameters.Length == 2));
        RequestEvaluation();
    }

    public Document Document { get; private set; }

    [ObservableProperty] private PlotModel plotModel;
    [ObservableProperty] private string diagnosticsSummary = string.Empty;
    [ObservableProperty] private string userNotes = string.Empty;
    [ObservableProperty] private MeshGeometry3D? surfaceMesh;

    public ObservableCollection<FunctionRowViewModel> Functions { get; private set; }
    public IReadOnlyList<PaletteOption> PaletteOptions { get; }
    public Array PlotModeOptions { get; }
    public ObservableCollection<FunctionRowViewModel> SurfaceFunctions { get; private set; }

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

    public PlotMode PlotMode {
        get => Document.PlotMode;
        set {
            if (Document.PlotMode != value) {
                Document.PlotMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTwoDMode));
                OnPropertyChanged(nameof(IsThreeDMode));
                RequestEvaluation();
            }
        }
    }

    public bool IsTwoDMode => Document.PlotMode == PlotMode.TwoD;
    public bool IsThreeDMode => Document.PlotMode == PlotMode.ThreeD;

    public Guid? SelectedSurfaceFunctionId {
        get => Document.SelectedSurfaceFunctionId;
        set {
            if (Document.SelectedSurfaceFunctionId != value) {
                Document.SelectedSurfaceFunctionId = value;
                OnPropertyChanged();
                RequestEvaluation();
            }
        }
    }

    partial void OnUserNotesChanged(string value) {
        Document.UserNotes = value;
        DiagnosticsSummary = BuildDiagnosticsSummary();
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
        RefreshSurfaceFunctions();
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
        RefreshSurfaceFunctions();
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
            UserNotes = Document.UserNotes;
            OnPropertyChanged(nameof(PlotMode));
            OnPropertyChanged(nameof(IsTwoDMode));
            OnPropertyChanged(nameof(IsThreeDMode));
            OnPropertyChanged(nameof(SelectedSurfaceFunctionId));
            RequestEvaluation();
        }
    }

    private void RebuildFunctionViewModels() {
        Functions = new ObservableCollection<FunctionRowViewModel>(
            Document.Functions.Select(f => new FunctionRowViewModel(f, Document, _history, RequestEvaluation)));
        OnPropertyChanged(nameof(Functions));
        RefreshSurfaceFunctions();
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
        DiagnosticsSummary = BuildDiagnosticsSummary();
        SurfaceMesh = result.SurfaceCache?.Mesh;
        RefreshSurfaceFunctions();

        foreach (var row in Functions) {
            row.Refresh();
        }
    }

    private string BuildDiagnosticsSummary() {
        var lines = new List<string> {
            $"Diagnostics summary",
            $"- AngleMode: {Document.AngleMode}",
            $"- PlotMode: {Document.PlotMode}",
            $"- Function count: {Document.Functions.Count}",
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
        document.Functions.Add(new FunctionObject { Name = "f", ExpressionText = "exp(x^2)/exp(x^2-1)", PaletteIndex = 0 });
        document.Functions.Add(new FunctionObject { Name = "g", ExpressionText = "g(x,y)=sin(x)+cos(y)", Parameters = new[] { "x", "y" }, PaletteIndex = 1 });
        document.SelectedSurfaceFunctionId = document.Functions[1].Id;
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

    private void RefreshSurfaceFunctions() {
        SurfaceFunctions = new ObservableCollection<FunctionRowViewModel>(Functions.Where(f => f.Parameters.Length == 2));
        OnPropertyChanged(nameof(SurfaceFunctions));
        if (SurfaceFunctions.Count == 0) {
            SelectedSurfaceFunctionId = null;
        } else if (SelectedSurfaceFunctionId is null || SurfaceFunctions.All(f => f.Id != SelectedSurfaceFunctionId)) {
            SelectedSurfaceFunctionId = SurfaceFunctions[0].Id;
        }
    }
}

public sealed record PaletteOption(int Index, SolidColorBrush Color);

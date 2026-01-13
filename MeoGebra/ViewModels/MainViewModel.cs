using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeoGebra.Services;
using OxyPlot;

namespace MeoGebra.ViewModels;

public partial class MainViewModel : ObservableObject {
    private readonly IFunctionSampler _sampler = new NativeFunctionSampler();

    [ObservableProperty] private double a = 1.0;
    [ObservableProperty] private double b = 1.0;

    [ObservableProperty] private PlotModel plotModel = MeoGebra.Plot.PlotModelFactory.CreateEmpty();

    // サンプル数・範囲はとりあえず固定（拡張しやすいように定数/プロパティ化）
    private const int N = 2000;
    private const double X0 = -10;
    private const double X1 = 10;

    // GC避け：バッファはViewModelで保持して使い回す（毎回newしない）
    private NativeInterop.PointD[] _buffer = new NativeInterop.PointD[N];

    [RelayCommand]
    private void Plot() {
        // 例：y = a * sin(b*x)
        var p = new NativeInterop.FunctionParams { A = A, B = B };

        _sampler.Sample(NativeInterop.FunctionKind.Sin, X0, X1, p, _buffer);

        PlotModel = MeoGebra.Plot.PlotModelFactory.CreateLine(_buffer, N, title: $"y = {A} * sin({B}x)");
    }
}

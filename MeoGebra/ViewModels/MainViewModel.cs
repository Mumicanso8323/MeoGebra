using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeoGebra.Services;
using OxyPlot;

namespace MeoGebra.ViewModels;

public partial class MainViewModel : ObservableObject {
    private readonly IFunctionSampler _sampler = new NativeFunctionSampler();
    private readonly SemaphoreSlim _plotGate = new(1, 1);
    private CancellationTokenSource? _plotCts;
    private long _plotGeneration;

    [ObservableProperty] private double a = 1.0;
    [ObservableProperty] private double b = 1.0;

    [ObservableProperty] private PlotModel plotModel = MeoGebra.Plot.PlotModelFactory.CreateEmpty();

    // サンプル数・範囲はとりあえず固定（拡張しやすいように定数/プロパティ化）
    private const int N = 2000;
    private const double X0 = -10;
    private const double X1 = 10;
    private const int DebounceMs = 150;

    // GC避け：バッファはViewModelで保持して使い回す（毎回newしない）
    private NativeInterop.PointD[] _buffer = new NativeInterop.PointD[N];

    [RelayCommand]
    private void Plot() {
        _ = RequestPlotAsync(immediate: true);
    }

    partial void OnAChanged(double value) {
        _ = RequestPlotAsync();
    }

    partial void OnBChanged(double value) {
        _ = RequestPlotAsync();
    }

    private Task RequestPlotAsync(bool immediate = false) {
        var delayMs = immediate ? 0 : DebounceMs;
        _plotCts?.Cancel();
        _plotCts?.Dispose();
        _plotCts = new CancellationTokenSource();
        var token = _plotCts.Token;
        var generation = Interlocked.Increment(ref _plotGeneration);

        return Task.Run(async () => {
            try {
                if (delayMs > 0) {
                    await Task.Delay(delayMs, token);
                }

                await _plotGate.WaitAsync(token);
                try {
                    if (token.IsCancellationRequested) {
                        return;
                    }

                    var p = new NativeInterop.FunctionParams { A = A, B = B };
                    _sampler.Sample(NativeInterop.FunctionKind.Sin, X0, X1, p, _buffer);
                    var model = MeoGebra.Plot.PlotModelFactory.CreateLine(_buffer, N, title: $"y = {A} * sin({B}x)");

                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        if (generation == _plotGeneration) {
                            PlotModel = model;
                        }
                    });
                } finally {
                    _plotGate.Release();
                }
            } catch (OperationCanceledException) {
            }
        }, token);
    }
}

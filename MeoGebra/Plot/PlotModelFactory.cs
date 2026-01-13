using MeoGebra.NativeInterop;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MeoGebra.Plot;

public static class PlotModelFactory {
    public static PlotModel CreateEmpty() {
        var m = new PlotModel { Title = "Function Plot" };
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });
        m.Axes.Add(new LinearAxis { Position = AxisPosition.Left });
        return m;
    }

    public static PlotModel CreateLine(PointD[] points, int count, string title) {
        var m = CreateEmpty();
        m.Title = title;

        var s = new LineSeries();

        // ここは最小で。必要ならLineSeries.PointsのListを保持して再利用する形に拡張可
        s.Points.Capacity = count;
        for (int i = 0; i < count; i++)
            s.Points.Add(new DataPoint(points[i].X, points[i].Y));

        m.Series.Add(s);
        return m;
    }
}

using System.Collections.Generic;
using MeoGebra.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace MeoGebra.Plot;

public static class PlotModelFactory {
    public static PlotModel CreateEmpty(ViewportState viewport) {
        var m = new PlotModel { Title = "Function Plot" };
        m.Axes.Add(new LinearAxis {
            Position = AxisPosition.Bottom,
            Minimum = viewport.CenterX - viewport.ScaleX,
            Maximum = viewport.CenterX + viewport.ScaleX
        });
        m.Axes.Add(new LinearAxis {
            Position = AxisPosition.Left,
            Minimum = viewport.CenterY - viewport.ScaleY,
            Maximum = viewport.CenterY + viewport.ScaleY
        });
        return m;
    }

    public static PlotModel CreateDocumentPlot(Document document, IReadOnlyList<OxyColor> palette) {
        var model = CreateEmpty(document.Viewport);
        foreach (var function in document.Functions) {
            if (!function.IsVisible || function.RenderCache is null) {
                continue;
            }
            var color = palette.Count > 0
                ? palette[function.PaletteIndex % palette.Count]
                : OxyColors.SkyBlue;

            foreach (var segment in function.RenderCache.Segments) {
                var series = new LineSeries { Color = color, StrokeThickness = 2 };
                series.Points.Capacity = segment.Points.Count;
                foreach (var point in segment.Points) {
                    series.Points.Add(new DataPoint(point.X, point.Y));
                }
                model.Series.Add(series);
            }
        }
        return model;
    }
}

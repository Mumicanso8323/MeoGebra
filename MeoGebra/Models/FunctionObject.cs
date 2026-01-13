using System;
using System.Collections.Generic;

namespace MeoGebra.Models;

public sealed class FunctionObject {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "f";
    public string ExpressionText { get; set; } = "sin(x)";
    public bool IsVisible { get; set; } = true;
    public int PaletteIndex { get; set; }

    public double? XMin { get; set; }
    public double? XMax { get; set; }

    public List<Diagnostic> Diagnostics { get; } = new();

    public FunctionRenderCache? RenderCache { get; set; }

    public bool HasDiagnostics => Diagnostics.Count > 0;
}

public sealed class FunctionRenderCache {
    public FunctionRenderCache(List<FunctionSegment> segments) {
        Segments = segments;
    }

    public List<FunctionSegment> Segments { get; }
}

public sealed class FunctionSegment {
    public FunctionSegment(List<SamplePoint> points) {
        Points = points;
    }

    public List<SamplePoint> Points { get; }
}

public readonly record struct SamplePoint(double X, double Y);

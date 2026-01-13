using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using MeoGebra.Models;
using MeoGebra.Services.Expression;

namespace MeoGebra.Services.Evaluation;

public static class SurfaceSampler {
    public static Task<SurfaceRenderCache> SampleAsync(BoundFunction function, ViewportState viewport, AngleMode angleMode, CancellationToken token, int gridSize = 80) {
        return Task.Run(() => Sample(function, viewport, angleMode, token, gridSize), token);
    }

    private static SurfaceRenderCache Sample(BoundFunction function, ViewportState viewport, AngleMode angleMode, CancellationToken token, int gridSize) {
        var mesh = new MeshGeometry3D();
        var positions = new Point3D[gridSize, gridSize];
        var valid = new bool[gridSize, gridSize];

        var x0 = viewport.CenterX - viewport.ScaleX;
        var x1 = viewport.CenterX + viewport.ScaleX;
        var y0 = viewport.CenterY - viewport.ScaleY;
        var y1 = viewport.CenterY + viewport.ScaleY;
        var dx = (x1 - x0) / (gridSize - 1);
        var dy = (y1 - y0) / (gridSize - 1);
        var zLimit = Math.Max(viewport.ScaleY * 5.0, 1.0);

        for (var ix = 0; ix < gridSize; ix++) {
            token.ThrowIfCancellationRequested();
            var x = x0 + dx * ix;
            for (var iy = 0; iy < gridSize; iy++) {
                var y = y0 + dy * iy;
                var z = ExpressionEvaluator.EvaluateSurface(function.Expression, x, y, angleMode);
                if (double.IsNaN(z) || double.IsInfinity(z) || Math.Abs(z) > zLimit) {
                    valid[ix, iy] = false;
                    continue;
                }
                valid[ix, iy] = true;
                positions[ix, iy] = new Point3D(x, y, z);
            }
        }

        for (var ix = 0; ix < gridSize - 1; ix++) {
            token.ThrowIfCancellationRequested();
            for (var iy = 0; iy < gridSize - 1; iy++) {
                if (!valid[ix, iy] || !valid[ix + 1, iy] || !valid[ix, iy + 1] || !valid[ix + 1, iy + 1]) {
                    continue;
                }

                var baseIndex = mesh.Positions.Count;
                mesh.Positions.Add(positions[ix, iy]);
                mesh.Positions.Add(positions[ix + 1, iy]);
                mesh.Positions.Add(positions[ix + 1, iy + 1]);
                mesh.Positions.Add(positions[ix, iy + 1]);

                mesh.TriangleIndices.Add(baseIndex);
                mesh.TriangleIndices.Add(baseIndex + 1);
                mesh.TriangleIndices.Add(baseIndex + 2);

                mesh.TriangleIndices.Add(baseIndex);
                mesh.TriangleIndices.Add(baseIndex + 2);
                mesh.TriangleIndices.Add(baseIndex + 3);
            }
        }

        return new SurfaceRenderCache(mesh);
    }
}
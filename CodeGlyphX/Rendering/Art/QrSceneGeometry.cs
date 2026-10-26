using CodeGlyphX.Rendering;
using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

// One bounded vector description feeds both the raster and the SVG scene exporters. Coordinates are
// normalized to the square canvas (0..1, y down). Shapes are compound paths: same-direction contours
// unite, and opposite-direction contours cut holes, so glyphs and pixel art render without seams.
internal sealed class QrSceneGeometry {
    internal readonly List<SceneShape> Shapes = new();
    private QrSceneLayerOptions _layer = new();
    private const int MaxShapes = 20000;

    internal void Layer(QrSceneLayerOptions layer) => _layer = layer;

    /// <summary>Unites contours after normalizing them to one winding direction.</summary>
    internal void Fill(ScenePaint paint, params double[][] contours) {
        foreach (var contour in contours) Orient(contour, true);
        Add(paint, contours);
    }

    /// <summary>Fills an outline with holes cut out of it.</summary>
    internal void FillWithHoles(ScenePaint paint, double[] outer, params double[][] holes) {
        Orient(outer, true);
        foreach (var hole in holes) Orient(hole, false);
        var all = new double[holes.Length + 1][];
        all[0] = outer; Array.Copy(holes, 0, all, 1, holes.Length);
        Add(paint, all);
    }

    /// <summary>Adds contours with their existing winding, as font outlines encode their holes.</summary>
    internal void FillRaw(ScenePaint paint, IList<double[]> contours) {
        var copy = new double[contours.Count][];
        contours.CopyTo(copy, 0);
        Add(paint, copy);
    }

    internal void Polygon(Rgba32 color, params double[] points) => Fill(ScenePaint.Solid(color), points);
    internal void Polygon(ScenePaint paint, params double[] points) => Fill(paint, points);
    internal void Rect(ScenePaint paint, double x, double y, double w, double h) => Fill(paint, new[] { x, y, x + w, y, x + w, y + h, x, y + h });
    internal void Oval(ScenePaint paint, double x, double y, double rx, double ry, double angle = 0) => Fill(paint, Ellipse(x, y, rx, ry, angle));
    internal void Circle(ScenePaint paint, double x, double y, double r) => Fill(paint, Ellipse(x, y, r, r, 0));
    internal void RoundRect(ScenePaint paint, double x, double y, double w, double h, double r) => Fill(paint, RoundRectangle(x, y, w, h, r, r));
    internal void Ring(ScenePaint paint, double x, double y, double r, double width) =>
        FillWithHoles(paint, Ellipse(x, y, r + width / 2, r + width / 2, 0), Ellipse(x, y, r - width / 2, r - width / 2, 0));

    /// <summary>Strokes a polyline with round joins and caps as one united shape.</summary>
    internal void Stroke(ScenePaint paint, double width, params double[] points) => Fill(paint, StrokeContours(width, points));

    internal static double[][] StrokeContours(double width, double[] points) {
        var contours = new List<double[]>();
        var half = width / 2;
        for (var i = 0; i + 3 < points.Length; i += 2) {
            var x1 = points[i]; var y1 = points[i + 1]; var x2 = points[i + 2]; var y2 = points[i + 3];
            var length = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            if (length == 0) continue;
            var dx = -(y2 - y1) / length * half; var dy = (x2 - x1) / length * half;
            contours.Add(new[] { x1 + dx, y1 + dy, x2 + dx, y2 + dy, x2 - dx, y2 - dy, x1 - dx, y1 - dy });
        }
        for (var i = 0; i + 1 < points.Length; i += 2) contours.Add(Ellipse(points[i], points[i + 1], half, half, 0, 20));
        return contours.ToArray();
    }

    internal static double[] Ellipse(double x, double y, double rx, double ry, double angle, int segments = 0) {
        if (segments <= 0) segments = Math.Max(24, Math.Min(160, (int)(Math.Max(rx, ry) * 900)));
        var points = new double[segments * 2]; var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        for (var i = 0; i < segments; i++) {
            var a = 2 * Math.PI * i / segments; var px = rx * Math.Cos(a); var py = ry * Math.Sin(a);
            points[2 * i] = x + px * cos - py * sin; points[2 * i + 1] = y + px * sin + py * cos;
        }
        return points;
    }

    /// <summary>A rectangle with separate top and bottom corner radii.</summary>
    internal static double[] RoundRectangle(double x, double y, double w, double h, double top, double bottom) {
        var points = new List<double>();
        Corner(x + w - top, y + top, top, -Math.PI / 2, 0);
        Corner(x + w - bottom, y + h - bottom, bottom, 0, Math.PI / 2);
        Corner(x + bottom, y + h - bottom, bottom, Math.PI / 2, Math.PI);
        Corner(x + top, y + top, top, Math.PI, Math.PI * 1.5);
        return points.ToArray();
        void Corner(double cx, double cy, double r, double from, double to) {
            var steps = r <= 0 ? 0 : Math.Max(4, (int)(r * 300));
            for (var i = 0; i <= steps; i++) {
                var a = from + (to - from) * i / Math.Max(1, steps);
                points.Add(cx + r * Math.Cos(a)); points.Add(cy + r * Math.Sin(a));
            }
        }
    }

    internal static Rgba32 Mix(Rgba32 a, Rgba32 b, double t) => new((byte)Math.Round(a.R + (b.R - a.R) * t),
        (byte)Math.Round(a.G + (b.G - a.G) * t), (byte)Math.Round(a.B + (b.B - a.B) * t));

    private void Add(ScenePaint paint, double[][] contours) {
        if (!_layer.Visible) return;
        if (Shapes.Count >= MaxShapes) throw new InvalidOperationException("Scene geometry exceeds its shape budget.");
        var angle = (_layer.RotationDegrees % 360) * Math.PI / 180;
        var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        var transformed = new List<double[]>(contours.Length);
        foreach (var points in contours) {
            if (points.Length < 6) continue;
            var copy = new double[points.Length];
            for (var i = 0; i < points.Length; i += 2) Transform(points[i], points[i + 1], cos, sin, out copy[i], out copy[i + 1]);
            transformed.Add(copy);
        }
        if (transformed.Count == 0) return;
        Shapes.Add(new SceneShape(paint.Transformed(this, cos, sin), transformed));
    }

    internal void Transform(double x, double y, double cos, double sin, out double tx, out double ty) {
        var sx = (x - 0.5) * _layer.Scale; var sy = (y - 0.5) * _layer.Scale;
        tx = _layer.X + sx * cos - sy * sin;
        ty = _layer.Y + sx * sin + sy * cos;
    }

    internal double LayerScale => _layer.Scale;

    private static void Orient(double[] points, bool positive) {
        var area = 0.0;
        for (var i = 0; i < points.Length; i += 2) {
            var j = (i + 2) % points.Length;
            area += points[i] * points[j + 1] - points[j] * points[i + 1];
        }
        if ((area >= 0) == positive) return;
        for (int i = 0, j = points.Length - 2; i < j; i += 2, j -= 2) {
            (points[i], points[j]) = (points[j], points[i]);
            (points[i + 1], points[j + 1]) = (points[j + 1], points[i + 1]);
        }
    }
}

internal sealed class SceneShape {
    internal ScenePaint Paint { get; }
    internal IReadOnlyList<double[]> Contours { get; }
    internal SceneShape(ScenePaint paint, IReadOnlyList<double[]> contours) { Paint = paint; Contours = contours; }
}

/// <summary>A solid or gradient fill with opacity, in normalized canvas coordinates.</summary>
internal sealed class ScenePaint {
    internal Rgba32 Color { get; }
    internal double Opacity { get; }
    internal bool Radial { get; }
    internal bool IsGradient => Stops is not null;
    // Linear: (X1, Y1) to (X2, Y2). Radial: centre (X1, Y1) and Radius.
    internal double X1 { get; } internal double Y1 { get; } internal double X2 { get; } internal double Y2 { get; }
    internal double Radius { get; }
    internal double[]? Offsets { get; }
    internal Rgba32[]? Stops { get; }

    private ScenePaint(Rgba32 color, double opacity, bool radial, double x1, double y1, double x2, double y2, double radius, double[]? offsets, Rgba32[]? stops) {
        Color = color; Opacity = Math.Max(0, Math.Min(1, opacity)); Radial = radial;
        X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; Radius = radius; Offsets = offsets; Stops = stops;
    }

    internal static ScenePaint Solid(Rgba32 color, double opacity = 1) => new(color, opacity, false, 0, 0, 0, 0, 0, null, null);
    internal static ScenePaint Linear(double x1, double y1, double x2, double y2, params (double Offset, Rgba32 Color)[] stops) =>
        new(stops[0].Color, 1, false, x1, y1, x2, y2, 0, Split(stops, out var colors), colors);
    internal static ScenePaint RadialGradient(double x, double y, double radius, double opacity, params (double Offset, Rgba32 Color)[] stops) =>
        new(stops[0].Color, opacity, true, x, y, 0, 0, radius, Split(stops, out var colors), colors);
    internal ScenePaint WithOpacity(double opacity) => new(Color, opacity, Radial, X1, Y1, X2, Y2, Radius, Offsets, Stops);

    private static double[] Split((double Offset, Rgba32 Color)[] stops, out Rgba32[] colors) {
        var offsets = new double[stops.Length]; colors = new Rgba32[stops.Length];
        for (var i = 0; i < stops.Length; i++) { offsets[i] = stops[i].Offset; colors[i] = stops[i].Color; }
        return offsets;
    }

    internal ScenePaint Transformed(QrSceneGeometry geometry, double cos, double sin) {
        if (!IsGradient) return this;
        geometry.Transform(X1, Y1, cos, sin, out var x1, out var y1);
        geometry.Transform(X2, Y2, cos, sin, out var x2, out var y2);
        return new(Color, Opacity, Radial, x1, y1, x2, y2, Radius * geometry.LayerScale, Offsets, Stops);
    }

    /// <summary>Colour and stop alpha (0..1) at a normalized canvas point. Stop alpha lets glows fade out softly.</summary>
    internal void Evaluate(double x, double y, out double r, out double g, out double b, out double alpha) {
        if (Stops is null || Offsets is null) { r = Color.R; g = Color.G; b = Color.B; alpha = 1; return; }
        double t;
        if (Radial) t = Math.Sqrt((x - X1) * (x - X1) + (y - Y1) * (y - Y1)) / Math.Max(1e-9, Radius);
        else {
            var dx = X2 - X1; var dy = Y2 - Y1; var length = dx * dx + dy * dy;
            t = length == 0 ? 0 : ((x - X1) * dx + (y - Y1) * dy) / length;
        }
        Rgba32 from = Stops[0], to = Stops[0]; var f = 0.0;
        if (t > Offsets[0]) {
            from = to = Stops[Stops.Length - 1];
            for (var i = 1; i < Stops.Length; i++) {
                if (t > Offsets[i]) continue;
                f = (t - Offsets[i - 1]) / Math.Max(1e-9, Offsets[i] - Offsets[i - 1]);
                from = Stops[i - 1]; to = Stops[i];
                break;
            }
        }
        r = from.R + (to.R - from.R) * f; g = from.G + (to.G - from.G) * f; b = from.B + (to.B - from.B) * f;
        alpha = (from.A + (to.A - from.A) * f) / 255.0;
    }
}

/// <summary>Builds flattened Bézier outlines in normalized coordinates.</summary>
internal sealed class ScenePath {
    private readonly List<double> _points = new();
    private double _x, _y;
    internal ScenePath(double x, double y) { _x = x; _y = y; _points.Add(x); _points.Add(y); }
    internal ScenePath Line(double x, double y) { _points.Add(x); _points.Add(y); _x = x; _y = y; return this; }
    internal ScenePath Quad(double cx, double cy, double x, double y, int steps = 14) {
        for (var i = 1; i <= steps; i++) {
            var t = i / (double)steps; var u = 1 - t;
            _points.Add(u * u * _x + 2 * u * t * cx + t * t * x); _points.Add(u * u * _y + 2 * u * t * cy + t * t * y);
        }
        _x = x; _y = y; return this;
    }
    internal ScenePath Cubic(double c1x, double c1y, double c2x, double c2y, double x, double y, int steps = 20) {
        for (var i = 1; i <= steps; i++) {
            var t = i / (double)steps; var u = 1 - t;
            _points.Add(u * u * u * _x + 3 * u * u * t * c1x + 3 * u * t * t * c2x + t * t * t * x);
            _points.Add(u * u * u * _y + 3 * u * u * t * c1y + 3 * u * t * t * c2y + t * t * t * y);
        }
        _x = x; _y = y; return this;
    }
    internal double[] ToArray() => _points.ToArray();
}

// Defined here rather than relying on framework-specific Random sequences.
internal sealed class SceneRandom {
    private uint _state;
    internal SceneRandom(int seed) => _state = unchecked((uint)seed) ^ 0xA341316Cu;
    internal double Next() {
        if (_state == 0) _state = 0xA341316C;
        _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5;
        return _state / 4294967296.0;
    }
    internal double Range(double min, double max) => min + (max - min) * Next();
}

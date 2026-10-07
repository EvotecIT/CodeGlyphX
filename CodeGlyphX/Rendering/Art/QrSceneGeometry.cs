using CodeGlyphX.Rendering;
using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

// One bounded polygon description feeds both raster and vector scene exporters.
internal sealed class QrSceneGeometry {
    internal readonly List<ScenePolygon> Shapes = new();
    private QrSceneLayerOptions _layer = new();
    internal void Layer(QrSceneLayerOptions layer) => _layer = layer;
    internal void Polygon(Rgba32 color, params double[] points) {
        if (!_layer.Visible) return;
        var angle = (_layer.RotationDegrees % 360) * Math.PI / 180;
        var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        var transformed = new double[points.Length];
        for (var i = 0; i < points.Length; i += 2) {
            var x = (points[i] - 0.5) * _layer.Scale; var y = (points[i + 1] - 0.5) * _layer.Scale;
            transformed[i] = _layer.X + x * cos - y * sin;
            transformed[i + 1] = _layer.Y + x * sin + y * cos;
        }
        Shapes.Add(new ScenePolygon(color, transformed));
    }
    internal void Rect(Rgba32 color, double x, double y, double w, double h) => Polygon(color, x, y, x + w, y, x + w, y + h, x, y + h);
    internal void Oval(Rgba32 color, double x, double y, double rx, double ry, double angle = 0) {
        var points = new double[96]; var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        for (var i = 0; i < 48; i++) {
            var a = 2 * Math.PI * i / 48; var px = rx * Math.Cos(a); var py = ry * Math.Sin(a);
            points[2 * i] = x + px * cos - py * sin; points[2 * i + 1] = y + px * sin + py * cos;
        }
        Polygon(color, points);
    }
    internal void Line(Rgba32 color, double x1, double y1, double x2, double y2, double width) {
        var length = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        if (length == 0) return;
        var dx = (y2 - y1) / length * width / 2; var dy = (x2 - x1) / length * width / 2;
        Polygon(color, x1 + dx, y1 - dy, x2 + dx, y2 - dy, x2 - dx, y2 + dy, x1 - dx, y1 + dy);
    }
    internal static Rgba32 Mix(Rgba32 a, Rgba32 b, double t) => new((byte)Math.Round(a.R + (b.R - a.R) * t),
        (byte)Math.Round(a.G + (b.G - a.G) * t), (byte)Math.Round(a.B + (b.B - a.B) * t));
}

internal sealed class ScenePolygon {
    internal Rgba32 Color { get; }
    internal double[] Points { get; }
    internal ScenePolygon(Rgba32 color, double[] points) { Color = color; Points = points; }
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
}

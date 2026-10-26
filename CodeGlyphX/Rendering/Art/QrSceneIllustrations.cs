using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

// Each scene is its own composition: a background, a light local backdrop that belongs to the illustration
// and holds the code (its colour is the paper colour, so the quiet zone merges into it), and motifs that frame
// the backdrop without entering the code. Bold editorial vector shapes with limited shading throughout.
internal static partial class QrSceneIllustrations {
    internal static QrSceneGeometry Build(QrSceneOptions options, QrSceneFrame frame) {
        var g = new QrSceneGeometry(); var random = new SceneRandom(options.Seed);
        var scene = new SceneContext(g, options, frame, random);
        switch (options.Style) {
            case QrSceneStyle.TropicalGarden: Garden(scene); break;
            case QrSceneStyle.ElectricCity: City(scene); break;
            case QrSceneStyle.MusicFestival: Festival(scene); break;
            case QrSceneStyle.OceanReef: Reef(scene); break;
            case QrSceneStyle.CosmicOrbit: Cosmos(scene); break;
            default: Arcade(scene); break;
        }
        return g;
    }

    /// <summary>Caption ink for each composition: dark on light grounds, paper on night scenes.</summary>
    internal static Rgba32 CaptionColor(QrSceneOptions options) => options.Style switch {
        QrSceneStyle.ElectricCity or QrSceneStyle.MusicFestival or QrSceneStyle.CosmicOrbit => options.Paper,
        QrSceneStyle.RetroArcade => options.Colors[2],
        _ => options.Ink
    };

    internal static void Caption(QrSceneGeometry g, QrSceneOptions design, int size) {
        var text = design.Caption.ToUpperInvariant();
        if (!design.CaptionLayer.Visible || text.Trim().Length == 0) return;
        g.Layer(design.CaptionLayer);
        if (design.Style == QrSceneStyle.RetroArcade) PixelCaption(g, design, text, size);
        else TypeCaption(g, design, text);
    }

    // Real outline type in the shared caption face, semibold with open tracking, centred on the caption layer.
    private static void TypeCaption(QrSceneGeometry g, QrSceneOptions design, string text) {
        const double em = 100;
        var font = SceneCaptionFont;
        var contours = new List<double[]>();
        var pen = 0.0; var tracking = em * 0.09;
        double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;
        foreach (var character in text) {
            var shape = font.Shape(character.ToString(), em, strict: false);
            if (shape.Contours.Count > 0) {
                shape = PrintLayoutBuilder.Strengthen(shape, em * 0.022);
                foreach (var contour in shape.Contours) {
                    var points = new double[contour.Count * 2];
                    for (var i = 0; i < contour.Count; i++) {
                        points[2 * i] = contour[i].X + pen; points[2 * i + 1] = contour[i].Y;
                        left = Math.Min(left, points[2 * i]); right = Math.Max(right, points[2 * i]);
                        top = Math.Min(top, points[2 * i + 1]); bottom = Math.Max(bottom, points[2 * i + 1]);
                    }
                    contours.Add(points);
                }
            }
            pen += shape.AdvanceWidth + tracking;
        }
        if (contours.Count == 0) return;
        // Cap height of 4.4% of the canvas, narrowed to fit 92% of the caption layer width.
        var scale = Math.Min(0.044 / Math.Max(1e-6, bottom - top), 0.92 / Math.Max(1e-6, right - left));
        var cx = (left + right) / 2; var cy = (top + bottom) / 2;
        foreach (var points in contours) for (var i = 0; i < points.Length; i += 2) {
            points[i] = 0.5 + (points[i] - cx) * scale; points[i + 1] = 0.5 + (points[i + 1] - cy) * scale;
        }
        g.FillRaw(ScenePaint.Solid(CaptionColor(design)), contours);
    }

    // Pixel type on whole device pixels. Each glyph is one compound shape, so its cells join without seams.
    private static void PixelCaption(QrSceneGeometry g, QrSceneOptions design, string text, int size) {
        var columns = text.Length * 6 - 1;
        var layer = design.CaptionLayer;
        var cell = Math.Max(1, Math.Min(Math.Floor(size * 0.0125), Math.Floor(size * 0.52 / columns)));
        var snapped = Math.Abs(layer.Scale - 1) < 1e-9 && Math.Abs(layer.RotationDegrees % 360) < 1e-9;
        // With an identity scale and rotation the layer only translates, so choose local cells that land on pixels.
        var originX = Math.Round(layer.X * size - columns * cell / 2); var originY = Math.Round(layer.Y * size - 3.5 * cell);
        var shiftX = snapped ? (layer.X - 0.5) * size : 0; var shiftY = snapped ? (layer.Y - 0.5) * size : 0;
        if (!snapped) { originX = Math.Round(size / 2.0 - columns * cell / 2); originY = Math.Round(size / 2.0 - 3.5 * cell); }
        var paint = ScenePaint.Solid(CaptionColor(design));
        for (var i = 0; i < text.Length; i++) {
            var glyph = BarcodeLabelFont.GetGlyph(text[i]);
            var cells = new List<double[]>();
            for (var y = 0; y < 7; y++) for (var x = 0; x < 5; x++) if ((glyph[y] & (1 << (4 - x))) != 0) {
                var px = (originX + (i * 6 + x) * cell - shiftX) / size; var py = (originY + y * cell - shiftY) / size; var w = cell / size;
                cells.Add(new[] { px, py, px + w, py, px + w, py + w, px, py + w });
            }
            if (cells.Count > 0) g.Fill(paint, cells.ToArray());
        }
    }

    // Scene captions follow the canvas caption default face, so a bundled caption font reaches both.
    private static BarcodeFont SceneCaptionFont => new QrCanvasCaptionOptions().Font;

    private static ScenePaint Solid(Rgba32 color, double opacity = 1) => ScenePaint.Solid(color, opacity);
    private static Rgba32 Mix(Rgba32 a, Rgba32 b, double t) => QrSceneGeometry.Mix(a, b, t);
    private static Rgba32 Rgb(int value) => new((byte)(value >> 16), (byte)(value >> 8), (byte)value);

    /// <summary>A pointed leaf from its base along an angle; bend curves the whole blade sideways.</summary>
    private static double[] Leaf(double baseX, double baseY, double length, double width, double angle, double bend = 0) {
        var points = new List<double>();
        var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        for (var i = 0; i <= 28; i++) Add(i / 28.0, 1);
        for (var i = 27; i >= 1; i--) Add(i / 28.0, -1);
        return points.ToArray();
        void Add(double t, double side) {
            var half = 0.5 * width * Math.Pow(Math.Sin(Math.PI * Math.Min(1, t * 1.05)), 0.9);
            var u = t * length; var v = side * half + bend * length * Math.Sin(Math.PI * t);
            points.Add(baseX + u * cos - v * sin); points.Add(baseY + u * sin + v * cos);
        }
    }
}

/// <summary>The code's protected square (including its quiet zone) in normalized canvas coordinates.</summary>
internal readonly struct QrSceneFrame {
    internal readonly double X, Y, Size;
    internal QrSceneFrame(double x, double y, double size) { X = x; Y = y; Size = size; }
    internal double CenterX => X + Size / 2;
    internal double CenterY => Y + Size / 2;
    internal double Right => X + Size;
    internal double Bottom => Y + Size;
}

internal sealed class SceneContext {
    internal QrSceneGeometry G { get; }
    internal QrSceneOptions Options { get; }
    internal QrSceneFrame Frame { get; }
    internal SceneRandom Random { get; }
    internal Rgba32[] C => Options.Colors;
    internal Rgba32 Paper => Options.Paper;
    internal Rgba32 Ink => Options.Ink;
    internal SceneContext(QrSceneGeometry g, QrSceneOptions options, QrSceneFrame frame, SceneRandom random) {
        G = g; Options = options; Frame = frame; Random = random;
    }
    internal void Backdrop() => G.Layer(Options.Backdrop);
    internal void Motifs() => G.Layer(Options.Motifs);
    // The local backdrop follows the actual code placement and the backdrop layer's visibility only.
    internal void Plate() => G.Layer(new QrSceneLayerOptions { Visible = Options.Backdrop.Visible });
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Xml.Linq;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Frames supplied images as deterministic portrait, botanical and geometric QR compositions.</summary>
public static class QrIllustratedComposer {
    /// <summary>Creates coordinated rendering settings which callers may customize before rendering or searching.</summary>
    public static QrImageCompositionOptions CreateOptions(QrIllustratedStyle style, int moduleSize = 16) {
        ValidateStyle(style);
        return new QrImageCompositionOptions {
            ModuleSize = moduleSize, Strength = 0.95,
            Canvas = new QrImageCanvasOptions { PaddingModules = 12 },
            Art = new QrImageArtOptions {
                Style = style == QrIllustratedStyle.EngravedPortrait ? QrImageArtStyle.Engraving
                    : style == QrIllustratedStyle.BotanicalBadge ? QrImageArtStyle.Ribbons : QrImageArtStyle.Weave,
                Finders = style == QrIllustratedStyle.GeometricPoster ? QrImageFinderStyle.Squircle : QrImageFinderStyle.Rounded,
                FunctionalForeground = style == QrIllustratedStyle.EngravedPortrait ? new Rgba32(42, 31, 27) : new Rgba32(24, 48, 45),
                FunctionalBackground = new Rgba32(248, 242, 226),
                Subject = new QrImageSubjectOptions { Radius = 0.22 }
            }
        };
    }

    /// <summary>Composes an image and applies the selected frame. The exact QR and quiet-zone rectangle remain untouched by framing.</summary>
    public static QrIllustratedComposition Render(QrCode code, byte[] rgba, int width, int height,
        QrIllustratedStyle style, QrImageCompositionOptions? options = null, CancellationToken cancellationToken = default) {
        ValidateStyle(style);
        var artwork = QrImageComposer.Render(code, rgba, width, height, options ?? CreateOptions(style), cancellationToken);
        return Frame(artwork, style, cancellationToken);
    }

    /// <summary>Frames an already measured candidate without changing pixels inside its QR and quiet zone.</summary>
    public static QrIllustratedComposition Frame(QrImageComposition artwork, QrIllustratedStyle style, CancellationToken cancellationToken = default) {
        if (artwork is null) throw new ArgumentNullException(nameof(artwork));
        ValidateStyle(style);
        cancellationToken.ThrowIfCancellationRequested();
        var size = artwork.Size;
        var pixels = artwork.GetPixels();
        var paper = new Rgba32(248, 242, 226);
        var ink = style == QrIllustratedStyle.EngravedPortrait ? new Rgba32(91, 67, 48) : new Rgba32(36, 83, 72);
        var lines = BuildFrame(style, size);
        // A translucent paper wash leaves a trace of the source image around the protected QR.
        for (var y = 0; y < size; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < size; x++) {
                if (Protected(artwork, x, y)) continue;
                var p = (y * size + x) * 4;
                pixels[p] = Mix(pixels[p], paper.R); pixels[p + 1] = Mix(pixels[p + 1], paper.G); pixels[p + 2] = Mix(pixels[p + 2], paper.B);
            }
        }
        foreach (var line in lines) PaintLine(pixels, artwork, line, ink, cancellationToken);
        var image = new QrImageComposition(pixels, size, artwork.QrOffsetX, artwork.QrOffsetY, artwork.QrSize);
        var svg = BuildSvg(artwork, lines, ink, paper);
        cancellationToken.ThrowIfCancellationRequested();
        return new QrIllustratedComposition(image, svg);
    }

    private static void ValidateStyle(QrIllustratedStyle style) {
        if (!Enum.IsDefined(typeof(QrIllustratedStyle), style)) throw new ArgumentOutOfRangeException(nameof(style));
    }
    private static byte Mix(byte source, byte paper) => (byte)Math.Round(source * 0.12 + paper * 0.88);
    private static bool Protected(QrImageComposition image, int x, int y) => x >= image.QrOffsetX && y >= image.QrOffsetY
        && x < image.QrOffsetX + image.QrSize && y < image.QrOffsetY + image.QrSize;

    private static List<Stroke> BuildFrame(QrIllustratedStyle style, int size) {
        var lines = new List<Stroke>();
        void Line(double width, params double[] points) {
            for (var i = 0; i < points.Length; i++) points[i] *= size;
            lines.Add(new Stroke(points, width * size));
        }
        void Oval(double cx, double cy, double rx, double ry, double angle, double width) {
            var points = new double[146];
            for (var i = 0; i <= 72; i++) {
                var t = i * Math.PI / 36;
                var x = rx * Math.Cos(t); var y = ry * Math.Sin(t);
                points[2 * i] = cx + x * Math.Cos(angle) - y * Math.Sin(angle);
                points[2 * i + 1] = cy + x * Math.Sin(angle) + y * Math.Cos(angle);
            }
            Line(width, points);
        }
        if (style == QrIllustratedStyle.EngravedPortrait) {
            Oval(.5, .5, .455, .475, 0, .0025);
            Oval(.5, .5, .435, .455, 0, .0012);
            for (var i = 0; i < 32; i++) {
                var t = i * Math.PI / 16;
                Line(.001, .5 + .445 * Math.Cos(t), .5 + .465 * Math.Sin(t), .5 + .455 * Math.Cos(t), .5 + .475 * Math.Sin(t));
            }
        } else if (style == QrIllustratedStyle.BotanicalBadge) {
            for (var side = 0; side < 2; side++) {
                var x = side == 0 ? .07 : .93;
                Line(.0025, x, .15, x, .85);
                for (var i = 0; i < 7; i++) {
                    var y = .2 + i * .1;
                    Oval(x + (side == 0 ? .025 : -.025), y, .034, .012, side == 0 ? -.65 : .65, .0018);
                    Line(.001, x, y + .02, x + (side == 0 ? .05 : -.05), y - .02);
                }
            }
            Line(.002, .18, .065, .82, .065);
            Line(.002, .18, .935, .82, .935);
        } else {
            Line(.003, .045, .25, .045, .045, .25, .045);
            Line(.003, .75, .045, .955, .045, .955, .25);
            Line(.003, .955, .75, .955, .955, .75, .955);
            Line(.003, .25, .955, .045, .955, .045, .75);
            for (var i = 0; i < 5; i++) {
                var y = .085 + i * .017;
                Line(.002, .38, y, .62, y);
                Line(.002, .38, 1 - y, .62, 1 - y);
            }
        }
        return lines;
    }

    private static void PaintLine(byte[] pixels, QrImageComposition image, Stroke stroke, Rgba32 ink, CancellationToken token) {
        for (var i = 2; i < stroke.Points.Length; i += 2) {
            token.ThrowIfCancellationRequested();
            var ax = stroke.Points[i - 2]; var ay = stroke.Points[i - 1];
            var bx = stroke.Points[i]; var by = stroke.Points[i + 1];
            var dx = bx - ax; var dy = by - ay; var length = dx * dx + dy * dy;
            var radius = stroke.Width / 2;
            for (var y = Math.Max(0, (int)Math.Floor(Math.Min(ay, by) - radius - 1)); y < Math.Min(image.Size, Math.Ceiling(Math.Max(ay, by) + radius + 1)); y++) {
                token.ThrowIfCancellationRequested();
                for (var x = Math.Max(0, (int)Math.Floor(Math.Min(ax, bx) - radius - 1)); x < Math.Min(image.Size, Math.Ceiling(Math.Max(ax, bx) + radius + 1)); x++) {
                    if (Protected(image, x, y)) continue;
                    var t = length == 0 ? 0 : Math.Max(0, Math.Min(1, ((x + .5 - ax) * dx + (y + .5 - ay) * dy) / length));
                    var ex = x + .5 - ax - t * dx; var ey = y + .5 - ay - t * dy;
                    var coverage = Math.Max(0, Math.Min(1, radius + .5 - Math.Sqrt(ex * ex + ey * ey)));
                    var p = (y * image.Size + x) * 4;
                    pixels[p] = Blend(pixels[p], ink.R, coverage); pixels[p + 1] = Blend(pixels[p + 1], ink.G, coverage); pixels[p + 2] = Blend(pixels[p + 2], ink.B, coverage);
                }
            }
        }
    }
    private static byte Blend(byte source, byte ink, double amount) => (byte)Math.Round(source + (ink - source) * amount);

    private static string BuildSvg(QrImageComposition image, List<Stroke> lines, Rgba32 ink, Rgba32 paper) {
        XNamespace ns = "http://www.w3.org/2000/svg";
        string N(double number) => number.ToString("0.###", CultureInfo.InvariantCulture);
        string Color(Rgba32 c) => "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        var size = image.Size;
        var clip = $"M0 0H{size}V{size}H0Z M{image.QrOffsetX} {image.QrOffsetY}v{image.QrSize}h{image.QrSize}v-{image.QrSize}Z";
        var svg = new XElement(ns + "svg", new XAttribute("viewBox", $"0 0 {size} {size}"), new XAttribute("width", size), new XAttribute("height", size),
            new XElement(ns + "title", "Illustrated QR composition"),
            new XElement(ns + "defs", new XElement(ns + "clipPath", new XAttribute("id", "frame"),
                new XElement(ns + "path", new XAttribute("d", clip), new XAttribute("clip-rule", "evenodd")))),
            new XElement(ns + "image", new XAttribute("width", size), new XAttribute("height", size), new XAttribute("href", "data:image/png;base64," + Convert.ToBase64String(image.ToPng()))));
        var group = new XElement(ns + "g", new XAttribute("clip-path", "url(#frame)"),
            new XElement(ns + "rect", new XAttribute("width", size), new XAttribute("height", size), new XAttribute("fill", Color(paper)), new XAttribute("fill-opacity", "0.88")));
        foreach (var line in lines) {
            var points = new string[line.Points.Length / 2];
            for (var i = 0; i < points.Length; i++) points[i] = N(line.Points[2 * i]) + "," + N(line.Points[2 * i + 1]);
            group.Add(new XElement(ns + "polyline", new XAttribute("points", string.Join(" ", points)), new XAttribute("fill", "none"),
                new XAttribute("stroke", Color(ink)), new XAttribute("stroke-width", N(line.Width)), new XAttribute("stroke-linecap", "round"), new XAttribute("stroke-linejoin", "round")));
        }
        svg.Add(group);
        return svg.ToString(SaveOptions.DisableFormatting);
    }
    private sealed class Stroke {
        internal double[] Points { get; }
        internal double Width { get; }
        internal Stroke(double[] points, double width) { Points = points; Width = width; }
    }
}

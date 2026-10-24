using System;
using System.Collections.Generic;

namespace CodeGlyphX.Printing;

/// <summary>Collects an owned physical scene under finite geometry and outline-work limits.</summary>
internal sealed class PrintLayoutBuilder {
    internal readonly BarcodePrintOptions Options;
    internal readonly SymbolFormat? Format;
    internal readonly double X;
    internal readonly List<BarcodePrintPath> Paths = new();
    internal readonly List<BarcodePrintText> Text = new();
    internal readonly List<BarcodePrintFinding> Findings = new();
    private int _points;

    internal PrintLayoutBuilder(SymbolFormat? format, BarcodePrintOptions? options, double nominal,
        double minimum = 0, double maximum = double.PositiveInfinity, bool fixedSize = false, bool requiresQuietZones = true) {
        Options = options?.Snapshot() ?? new BarcodePrintOptions();
        Format = format;
        ValidateOptions(Options);
        if (Options.Format.HasValue && format.HasValue && Options.Format != format)
            throw new ArgumentException("The print format conflicts with the encoded symbol.", nameof(options));
        if (Options.TextStyle == BarcodePrintTextStyle.Hibc) {
            if (format is SymbolFormat.Code39 or SymbolFormat.Code128) { nominal = .010 * 25.4; minimum = Math.Max(minimum, .0067 * 25.4); }
            if (format == SymbolFormat.DataMatrix) { nominal = .015 * 25.4; minimum = Math.Max(minimum, .010 * 25.4); }
        }
        if (Options.TextStyle == BarcodePrintTextStyle.Gs1) {
            if (format == SymbolFormat.DataMatrix) { minimum = Math.Max(minimum, .100); maximum = Math.Min(maximum, 3.50); }
            if (format == SymbolFormat.QrCode) { minimum = Math.Max(minimum, .254); maximum = Math.Min(maximum, 3.50); }
            if (format == SymbolFormat.DotCode) { nominal = .380; minimum = Math.Max(minimum, .380); maximum = Math.Min(maximum, .990); }
        }
        var requested = Options.XDimensionMm ?? nominal;
        if (!BarcodeBounds.Finite(requested) || requested <= 0 || requested > 100) throw new ArgumentOutOfRangeException(nameof(options), "X dimension must be positive and at most 100 mm.");
        var pitch = requested * Options.Dpi / 25.4;
        if (!fixedSize && Options.SnapToPrinterPixels && requested >= minimum && requested <= maximum) {
            var least = Math.Max(1, Math.Ceiling(minimum * Options.Dpi / 25.4 - 1e-9));
            var most = Math.Floor(maximum * Options.Dpi / 25.4 + 1e-9);
            if (least <= most) pitch = Math.Max(least, Math.Min(most, Math.Round(pitch)));
        }
        X = fixedSize ? nominal : Options.SnapToPrinterPixels ? Math.Max(1, Math.Round(pitch)) * 25.4 / Options.Dpi : requested;
        if (fixedSize && Options.XDimensionMm.HasValue && Math.Abs(requested - nominal) > 1e-9)
            Finding("FixedGeometry", true, "This format has fixed physical geometry; the requested X dimension was not applied.");
        if (fixedSize && Options.BarHeightMm.HasValue)
            Finding("FixedGeometry", true, "This format has fixed physical geometry; the requested bar height was not applied.");
        if (X < minimum - 1e-9 || X > maximum + 1e-9)
            Finding("ModuleSize", true, "The resolved printer pitch falls outside the selected format's physical range. Change the DPI or X dimension.");
        if (Math.Abs(X * Options.Dpi / 25.4 - Math.Round(X * Options.Dpi / 25.4)) > 1e-7)
            Finding("FractionalPitch", false, "This physical size has fractional printer-pixel pitch. Use vector output or qualify the selected printer resolution.");
        if (!Options.IncludeQuietZones && requiresQuietZones && format.HasValue && format is not (SymbolFormat.Aztec or SymbolFormat.AztecRune or SymbolFormat.FacingIdentificationMark
            or SymbolFormat.Flattermarken or SymbolFormat.Daft or SymbolFormat.Gs1DataBarExpanded or SymbolFormat.Gs1DataBarLimited or SymbolFormat.Gs1DataBarOmnidirectional
            or SymbolFormat.Gs1DataBarTruncated or SymbolFormat.Gs1DataBarStacked or SymbolFormat.Gs1DataBarExpandedStacked or SymbolFormat.Gs1DataBarStackedOmnidirectional))
            Finding("QuietZonesOmitted", true, "Reserve the required clear space around the symbol before printing.");
        if (Options.Foreground.Kind != BarcodePaint.PaintKind.Solid || Options.Background.Kind != BarcodePaint.PaintKind.Solid
            || !Options.Foreground.IsOpaque || !Options.Background.IsOpaque)
            Finding("DecorativePaint", false, "Gradient, patterned or translucent paints require independent contrast and scanner qualification.");
        else if (Options.Foreground.StartColor.R == Options.Background.StartColor.R
            && Options.Foreground.StartColor.G == Options.Background.StartColor.G && Options.Foreground.StartColor.B == Options.Background.StartColor.B)
            Finding("NoPrintContrast", true, "The bar and background colours are identical. Select distinct high-contrast paints.");
        else if (Options.Foreground.StartColor.R != 0 || Options.Foreground.StartColor.G != 0 || Options.Foreground.StartColor.B != 0
            || Options.Background.StartColor.R != 255 || Options.Background.StartColor.G != 255 || Options.Background.StartColor.B != 255)
            Finding("CustomPaintContrast", false, "Qualify the selected solid colours with the intended scanner and substrate.");
    }

    internal void Finding(string code, bool error, string message) => Findings.Add(new BarcodePrintFinding(code, error, message));

    internal void Rect(double x, double y, double width, double height, BarcodePaint? paint = null, string role = "bars") {
        if (width <= 0 || height <= 0) return;
        Path(new[] { new[] { new BarcodePoint(x, y), new BarcodePoint(x + width, y), new BarcodePoint(x + width, y + height), new BarcodePoint(x, y + height) } }, paint ?? Options.Foreground, role);
    }

    internal void Path(IEnumerable<IEnumerable<BarcodePoint>> source, BarcodePaint paint, string role) {
        var contours = new List<IReadOnlyList<BarcodePoint>>();
        foreach (var contour in source) {
            var points = new List<BarcodePoint>();
            foreach (var p in contour) {
                if (++_points > 2097152) throw new ArgumentException("The print scene exceeds its outline limit.");
                points.Add(p);
            }
            if (points.Count >= 3) contours.Add(points.AsReadOnly());
        }
        if (contours.Count == 0) return;
        if (Paths.Count >= 262144) throw new ArgumentException("The print scene contains too many paths.");
        Paths.Add(new BarcodePrintPath(contours.AsReadOnly(), paint, role));
    }

    internal BarcodePrintText Line(string text, IBarcodeFont? font, double points, double left, double top, double width,
        bool alignRight = false, bool center = true, bool caption = false, bool alignInk = false, PrintTextAnchor anchor = PrintTextAnchor.LineBox, bool forceSyntheticBold = false) {
        if (font is null) throw new ArgumentException("Printable text requires a supplied font. Set Font or CaptionFont, or explicitly disable readable text.");
        var em = points * 25.4 / 72;
        var shape = font.Shape(text, em, Options.StrictText);
        if (caption && Options.FitCaptionToSymbolWidth && Math.Max(shape.AdvanceWidth, shape.InkBounds.Width) > width) {
            var fit = points * width / Math.Max(shape.AdvanceWidth, shape.InkBounds.Width);
            points = Math.Max(Options.MinimumCaptionSizePoints, fit);
            em = points * 25.4 / 72;
            shape = font.Shape(text, em, Options.StrictText);
            if (Math.Max(shape.AdvanceWidth, shape.InkBounds.Width) > width + 1e-7)
                Finding("CaptionDoesNotFit", true, "The caption cannot fit at its minimum text size. Increase symbol width or use a shorter caption.");
        }
        var syntheticBold = Options.SyntheticBold || forceSyntheticBold;
        if (syntheticBold) shape = Strengthen(shape, em * .02);
        if (anchor == PrintTextAnchor.InkTop) top -= shape.InkBounds.Y;
        if (anchor == PrintTextAnchor.InkBottom) top -= shape.InkBounds.Bottom;
        var measuredWidth = alignInk ? shape.InkBounds.Width : shape.AdvanceWidth;
        var x = alignRight ? left + width - measuredWidth : center ? left + (width - measuredWidth) / 2 : left;
        if (alignInk) x -= shape.InkBounds.X;
        var line = new BarcodePrintText(shape, font.Name, em, x, top);
        Text.Add(line);
        var positioned = new List<IEnumerable<BarcodePoint>>(shape.Contours.Count);
        foreach (var contour in shape.Contours) {
            var pointsList = new List<BarcodePoint>(contour.Count);
            foreach (var p in contour) pointsList.Add(new BarcodePoint(p.X + x, p.Y + top));
            positioned.Add(pointsList);
        }
        Path(positioned, Options.TextPaint ?? Options.Foreground, caption ? "caption" : "text");
        if (shape.MissingGlyphs.Count > 0 || shape.HasIncompleteShaping)
            Finding("TextFallback", true, "The selected font could not shape every requested character. Select a complete font or enable strict text to reject fallback.");
        var minimum = !caption && Options.TextStyle == BarcodePrintTextStyle.Gs1 ? 2 : 0;
        if (em < minimum) Finding("TextTooSmall", true, "GS1 readable text requires an em size of at least 2 mm.");
        if (Options.TextStyle == BarcodePrintTextStyle.UspsPackage) {
            var capShape = font.Shape("H", em, Options.StrictText);
            if (syntheticBold) capShape = Strengthen(capShape, em * .02);
            var cap = capShape.InkBounds.Height;
            if (cap < .090 * 25.4 || cap > .125 * 25.4)
                Finding("PackageCapHeight", true, "USPS package readable text needs a cap height from 0.090 to 0.125 inch. Adjust the selected font size.");
            var declaredBold = caption && Options.CaptionFont is not null ? Options.CaptionFontIsBold : Options.FontIsBold;
            if (!declaredBold && !syntheticBold)
                Finding("PackageTextWeight", true, "Supply and declare a bold face or explicitly select synthetic weight for USPS package text.");
        }
        return line;
    }

    internal void OrdinaryText(BarcodeBounds symbol, bool humanReadableAbove = false, double minimumInkGap = 0) {
        var textTop = symbol.Y - X;
        if (Options.IncludeHumanReadable && !string.IsNullOrEmpty(Options.HumanReadableText)) {
            if (humanReadableAbove) {
                var font = Options.Font ?? throw new ArgumentException("Printable text requires a supplied font.");
                var lines = SplitLines(Options.HumanReadableText!);
                for (var index = lines.Length - 1; index >= 0; index--) {
                    textTop -= font.Shape(lines[index], Options.FontSizePoints * 25.4 / 72, Options.StrictText).LineHeight;
                    Line(lines[index], font, Options.FontSizePoints, symbol.X, textTop, symbol.Width);
                }
            } else if (minimumInkGap > 0) {
                var top = symbol.Bottom + Math.Max(X, minimumInkGap);
                foreach (var line in SplitLines(Options.HumanReadableText!)) {
                    var placed = Line(line, Options.Font, Options.FontSizePoints, symbol.X, top, symbol.Width, anchor: PrintTextAnchor.InkTop);
                    top += placed.Shape.LineHeight;
                }
            } else Lines(Options.HumanReadableText!, Options.Font, Options.FontSizePoints, symbol.X, symbol.Bottom + X, symbol.Width, false);
        }
        if (!string.IsNullOrEmpty(Options.Caption)) {
            var font = Options.CaptionFont ?? Options.Font;
            var lines = SplitLines(Options.Caption!);
            var top = humanReadableAbove ? textTop - X : symbol.Y - X;
            for (var index = lines.Length - 1; index >= 0; index--) {
                if (font is null) throw new ArgumentException("A caption requires a supplied font.");
                var height = font.Shape(lines[index], Options.CaptionSizePoints * 25.4 / 72, Options.StrictText).LineHeight;
                top -= height;
                Line(lines[index], font, Options.CaptionSizePoints, symbol.X, top, symbol.Width, caption: true);
            }
        }
    }

    internal void Append(BarcodePrintLayout layout, double x, double y) {
        foreach (var path in layout.Paths) {
            var contours = new List<IEnumerable<BarcodePoint>>();
            foreach (var contour in path.Contours) {
                var points = new List<BarcodePoint>(contour.Count);
                foreach (var p in contour) points.Add(new BarcodePoint(p.X + x, p.Y + y));
                contours.Add(points);
            }
            Path(contours, path.Paint, path.Role);
        }
        foreach (var line in layout.Text) Text.Add(new BarcodePrintText(line.Shape, line.FontName, line.EmSizeMm,
            line.Origin.X + x, line.Origin.Y + y));
        Findings.AddRange(layout.Findings);
    }

    internal void Lines(string value, IBarcodeFont? font, double size, double x, double y, double width, bool caption) {
        foreach (var text in SplitLines(value)) {
            var line = Line(text, font, size, x, y, width, caption: caption);
            y += line.Shape.LineHeight;
        }
    }

    internal BarcodePrintLayout Finish(BarcodeBounds symbol) {
        var result = new BarcodePrintLayout(Format, Options.Dpi, X, symbol, Paths, Text, Findings, Options.Background);
        if (result.Bounds.Width > 10000 || result.Bounds.Height > 10000) throw new ArgumentException("Print layout exceeds the 10 metre extent limit.");
        return result;
    }

    private static string[] SplitLines(string text) {
        if (text.Length > 32768) throw new ArgumentException("Print text exceeds 32768 characters.");
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length > 64) throw new ArgumentException("Print text exceeds 64 lines.");
        return lines;
    }

    private static void ValidateOptions(BarcodePrintOptions o) {
        if (o.Dpi < 24 || o.Dpi > 2400) throw new ArgumentOutOfRangeException(nameof(o.Dpi));
        if (o.BarHeightMm.HasValue && (!BarcodeBounds.Finite(o.BarHeightMm.Value) || o.BarHeightMm <= 0 || o.BarHeightMm > 10000))
            throw new ArgumentOutOfRangeException(nameof(o.BarHeightMm));
        if (o.Itf14BearerThicknessMm.HasValue && (!BarcodeBounds.Finite(o.Itf14BearerThicknessMm.Value) || o.Itf14BearerThicknessMm <= 0 || o.Itf14BearerThicknessMm > 100))
            throw new ArgumentOutOfRangeException(nameof(o.Itf14BearerThicknessMm));
        if (!ValidPoints(o.FontSizePoints) || !ValidPoints(o.CaptionSizePoints) || !ValidPoints(o.MinimumCaptionSizePoints))
            throw new ArgumentOutOfRangeException(nameof(o.FontSizePoints));
        if (o.FitCaptionToSymbolWidth && o.MinimumCaptionSizePoints > o.CaptionSizePoints) throw new ArgumentException("The minimum caption size exceeds its requested size.");
        if (o.Foreground is null || o.Background is null) throw new ArgumentException("Print paints cannot be null.");
        if (o.Format.HasValue && !Enum.IsDefined(typeof(SymbolFormat), o.Format.Value)) throw new ArgumentOutOfRangeException(nameof(o.Format));
        if (o.TextStyle < BarcodePrintTextStyle.Plain || o.TextStyle > BarcodePrintTextStyle.UspsPackage) throw new ArgumentOutOfRangeException(nameof(o.TextStyle));
    }
    private static bool ValidPoints(double value) => BarcodeBounds.Finite(value) && value > 0 && value <= 10000;

    internal static BarcodeTextShape Strengthen(BarcodeTextShape shape, double radius) {
        var count = 0;
        foreach (var contour in shape.Contours) count = checked(count + contour.Count);
        if (count > 1048576 / 9) throw new ArgumentException("Synthetic bold would exceed the text-outline limit. Use an actual bold face.");
        var contours = new List<IEnumerable<BarcodePoint>>();
        contours.AddRange(shape.Contours);
        for (var offset = 0; offset < 8; offset++) {
            var x = Math.Cos(offset * Math.PI / 4) * radius;
            var y = Math.Sin(offset * Math.PI / 4) * radius;
            foreach (var contour in shape.Contours) {
                var points = new List<BarcodePoint>(contour.Count);
                foreach (var p in contour) points.Add(new BarcodePoint(p.X + x, p.Y + y));
                contours.Add(points);
            }
        }
        return new BarcodeTextShape(shape.Text, shape.AdvanceWidth, shape.LineHeight, shape.BaselineOffset,
            contours, shape.MissingGlyphs, shape.HasIncompleteShaping);
    }
}

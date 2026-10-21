using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

public static partial class BarcodePrint {
    /// <summary>Positions a validated USPS package barcode, grouped PIC, service banner and identification bars.</summary>
    /// <remarks>Uses USPS2000508 Rev E sections 2.2–2.4. Supply a sans-serif font with permitted outline use.
    /// The layout does not establish identifier assignment, mailing eligibility or postal certification.</remarks>
    public static BarcodePrintLayout Layout(IntelligentMailPackageData package, string banner = "USPS TRACKING #", BarcodePrintOptions? options = null) {
        if (package is null) throw new ArgumentNullException(nameof(package));
        if (string.IsNullOrWhiteSpace(banner) || banner.Length > 128 || banner.IndexOfAny(new[] { '\r', '\n' }) >= 0 || banner != banner.ToUpperInvariant())
            throw new ArgumentException("The USPS service banner requires one uppercase line of at most 128 characters.", nameof(banner));
        var o = options?.Snapshot() ?? new BarcodePrintOptions();
        o.TextStyle = BarcodePrintTextStyle.UspsPackage;
        o.FitCaptionToSymbolWidth = false;
        var b = new PrintLayoutBuilder(SymbolFormat.Gs1Code128, o, .4064, .3302, .5334);
        if (o.Font is null || (o.CaptionFont ?? o.Font) is null) throw new ArgumentException("The USPS package layout requires a supplied sans-serif font.", nameof(options));
        if (!o.IncludeHumanReadable) b.Finding("PackageReadableTextRequired", true, "A USPS package label requires its grouped PIC and service banner.");
        if (o.HumanReadableText is not null && o.HumanReadableText != package.HumanReadable)
            throw new ArgumentException("The USPS readable line is the grouped PIC; routing data and arbitrary replacements cannot appear in it.", nameof(options));
        if (o.Caption is not null && o.Caption != banner)
            throw new ArgumentException("Supply the service banner through the banner argument.", nameof(options));
        if (o.Foreground.Kind != BarcodePaint.PaintKind.Solid || o.Background.Kind != BarcodePaint.PaintKind.Solid
            || !o.Foreground.StartColor.Equals(Rgba32.Black) || !o.Background.StartColor.Equals(Rgba32.White)
            || (o.TextPaint is not null && (o.TextPaint.Kind != BarcodePaint.PaintKind.Solid || !o.TextPaint.StartColor.Equals(Rgba32.Black))))
            b.Finding("PackageBlackOnWhite", true, "Use opaque black bars and text on a white background for the USPS package layout.");
        var symbol = IntelligentMailPackage.Encode(package.TrackingNumber, package.RoutingZip.Length == 0 ? null : package.RoutingZip);
        var quiet = o.IncludeQuietZones ? Math.Max(10 * b.X, .25 * 25.4) : 0;
        var width = symbol.TotalModules * b.X + quiet * 2;
        var height = o.BarHeightMm ?? .75 * 25.4;
        if (height < .75 * 25.4) b.Finding("PackageBarHeight", true, "USPS package bars require at least 0.75 inch height unless a separate postal exception applies.");
        LinearBars(b, symbol, quiet, 0, height);
        const double textGap = .1875 * 25.4;
        const double identificationGap = .031 * 25.4;
        const double identificationHeight = .047 * 25.4;
        var upper = b.Line(banner, o.CaptionFont ?? o.Font, o.CaptionSizePoints, 0, -textGap, width,
            caption: true, alignInk: true, anchor: PrintTextAnchor.InkBottom,
            forceSyntheticBold: o.CaptionFont is null ? !o.FontIsBold : !o.CaptionFontIsBold);
        var lower = b.Line(package.HumanReadable, o.Font, o.FontSizePoints, 0, height + textGap, width,
            alignInk: true, anchor: PrintTextAnchor.InkTop, forceSyntheticBold: !o.FontIsBold);
        if (upper.InkBounds.Width > width || lower.InkBounds.Width > width)
            b.Finding("PackageTextWidth", true, "The service banner and grouped PIC must fit within the barcode and its clear zones.");
        b.Rect(0, upper.InkBounds.Y - identificationGap - identificationHeight, width, identificationHeight, role: "identification");
        b.Rect(0, lower.InkBounds.Bottom + identificationGap, width, identificationHeight, role: "identification");
        return b.Finish(new BarcodeBounds(0, -.125 * 25.4, width, height + .25 * 25.4));
    }
}

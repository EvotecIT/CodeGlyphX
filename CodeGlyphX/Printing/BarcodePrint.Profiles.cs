using System;

namespace CodeGlyphX.Printing;

public static partial class BarcodePrint {
    private static BarcodePrintLayout MarksAndSpencer(Barcode1D symbol, string input, BarcodePrintOptions options) {
        var b = new PrintLayoutBuilder(SymbolFormat.Ean, options, .330, .264, .660);
        var q = options.IncludeQuietZones ? 12 * b.X : 0;
        var height = options.BarHeightMm ?? 18.23 * b.X / .330;
        if (height < 18.23 * b.X / .330)
            b.Finding("RetailBarHeight", true, "M&S bars are shorter than the EAN-8 height for the resolved magnification.");
        var modules = LinearModules(symbol);
        for (var x = 0; x < modules.Length; x++) if (modules[x])
            b.Rect(q + x * b.X, 0, b.X, height + (x < 3 || x >= 64 ? 5 * b.X : 0));
        var bounds = new BarcodeBounds(0, 0, 67 * b.X + 2 * q, height + 5 * b.X);
        if (options.IncludeHumanReadable && options.HumanReadableText is null) {
            var top = height + b.X;
            b.Line("M", options.Font, options.FontSizePoints, q - 12 * b.X, top, 7 * b.X, center: false, anchor: PrintTextAnchor.InkTop);
            for (var i = 0; i < input.Length; i++) {
                var at = i < 4 ? 3 + 7 * i : 36 + 7 * (i - 4);
                b.Line(input.Substring(i, 1), options.Font, options.FontSizePoints, q + at * b.X, top, 7 * b.X, anchor: PrintTextAnchor.InkTop);
            }
            b.Line("S", options.Font, options.FontSizePoints, q + 69 * b.X, top, 7 * b.X, center: false, anchor: PrintTextAnchor.InkTop);
        }
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    private static BarcodePrintLayout GermanPost(Barcode1D symbol, string readable, BarcodePrintOptions options) {
        options.HumanReadableText ??= readable;
        var b = new PrintLayoutBuilder(symbol.Format, options, .495, .375, .5);
        var quiet = options.IncludeQuietZones ? Math.Max(5, b.X * 10) : 0;
        var height = options.BarHeightMm ?? 25;
        if (height < 25) b.Finding("PostalBarHeight", true, "Identcode and Leitcode require bars at least 25 mm high.");
        LinearBars(b, symbol, quiet, 0, height);
        var bounds = new BarcodeBounds(0, 0, symbol.TotalModules * b.X + quiet * 2, height);
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }
    /// <summary>Positions checked HIBC data and its delimited, slashed-zero readable interpretation on the selected carrier.</summary>
    public static BarcodePrintLayout LayoutHibc(SymbolFormat carrier, string input, HibcEncodingOptions? encoding = null, BarcodePrintOptions? options = null) {
        encoding ??= new HibcEncodingOptions();
        var payload = Hibc.PrepareInput(input, encoding);
        var o = options?.Snapshot() ?? new BarcodePrintOptions();
        o.HumanReadableText ??= Hibc.HumanReadable(payload);
        o.TextStyle = BarcodePrintTextStyle.Hibc;
        return carrier is SymbolFormat.Code39 or SymbolFormat.Code128
            ? Layout(Hibc.EncodeLinear(carrier, input, encoding), o) : Layout(Hibc.EncodeMatrix(carrier, input, encoding), o);
    }

    /// <summary>Positions an AI 8110 coupon with its company-prefix and offer label above the symbol.</summary>
    /// <remarks>Coupon rows remain 34 modules high; BarHeightMm does not alter their prescribed geometry.</remarks>
    public static BarcodePrintLayout Layout(NorthAmericanCouponData coupon, int columns = 2, BarcodePrintOptions? options = null) {
        if (coupon is null) throw new ArgumentNullException(nameof(coupon));
        var o = options?.Snapshot() ?? new BarcodePrintOptions();
        o.HumanReadableText ??= coupon.HumanReadable;
        o.BarHeightMm = null;
        var symbol = NorthAmericanCoupon.Encode(coupon.ElementString, columns);
        return Grid(symbol.Modules, symbol.Format, o, .330, .254, .330, humanReadableAbove: true);
    }
}

using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

/// <summary>Builds measured physical print layouts from existing encoded symbols, without re-encoding their payloads.</summary>
public static partial class BarcodePrint {
    /// <summary>Positions linear bars, family quiet zones, retail guards or ITF-14 bearers, and supplied-font text.</summary>
    public static BarcodePrintLayout Layout(Barcode1D symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (symbol.HumanReadableText is not null) {
            options = options?.Snapshot() ?? new BarcodePrintOptions();
            options.HumanReadableText ??= symbol.HumanReadableText;
        }
        var format = symbol.Format ?? options?.Format;
        if ((format == SymbolFormat.Gs1Code128 || IsDataBar(format)) && (options?.TextStyle ?? BarcodePrintTextStyle.Plain) == BarcodePrintTextStyle.Plain) {
            options = options?.Snapshot() ?? new BarcodePrintOptions();
            options.TextStyle = BarcodePrintTextStyle.Gs1;
        }
        if (IsRetail(format) && TryRetail(symbol, format!.Value, out var retail)) return Retail(symbol, retail, options);
        if (symbol.TotalModules > 1048576) throw new ArgumentException("The print symbol exceeds the module limit.", nameof(symbol));
        var nominal = format == SymbolFormat.Code32 ? .25 : format == SymbolFormat.Gs1Code128 ? .495 : .330;
        var min = format == SymbolFormat.Gs1Code128 ? .170 : 0;
        var max = format == SymbolFormat.Gs1Code128 ? 1.016 : double.PositiveInfinity;
        if (format == SymbolFormat.Itf14) { nominal = .495; min = .170; max = 1.016; }
        if (format == SymbolFormat.Pharmacode) { nominal = .5; min = .3; max = .7; }
        if (format == SymbolFormat.Flattermarken) { nominal = 2; min = 2; max = 3; }
        if (format == SymbolFormat.FacingIdentificationMark) { nominal = 25.4 / 32; min = (.03125 - .008) * 25.4; max = (.03125 + .008) * 25.4; }
        if (options?.TextStyle == BarcodePrintTextStyle.UspsPackage) { nominal = .4064; min = .3302; max = .5334; }
        var b = new PrintLayoutBuilder(format, options, nominal, min, max);
        var height = b.Options.BarHeightMm ?? LinearHeight(format, symbol.TotalModules, b.X);
        var quiet = IsDataBar(format) || format is SymbolFormat.FacingIdentificationMark or SymbolFormat.Flattermarken ? 0 : 10;
        var q = b.Options.IncludeQuietZones ? format == SymbolFormat.Pharmacode ? 6 : quiet * b.X : 0;
        var bearer = format == SymbolFormat.Itf14 ? b.Options.Itf14BearerThicknessMm ?? (b.Options.Itf14BearerFrame ? 4.83 : b.X * 5) : 0;
        if (format == SymbolFormat.Itf14 && bearer < 2 * b.X)
            b.Finding("BearerThickness", true, "An ITF-14 bearer must be at least twice the narrow bar width.");
        var sideBearer = b.Options.Itf14BearerFrame ? bearer : 0;
        var symbolWidth = LinearBars(b, symbol, q + sideBearer, bearer, height, format == SymbolFormat.Itf14);
        if (bearer > 0) {
            b.Rect(0, 0, symbolWidth + q * 2 + sideBearer * 2, bearer, role: "bearer");
            b.Rect(0, bearer + height, symbolWidth + q * 2 + sideBearer * 2, bearer, role: "bearer");
            if (sideBearer > 0) {
                b.Rect(0, bearer, sideBearer, height, role: "bearer");
                b.Rect(symbolWidth + q * 2 + sideBearer, bearer, sideBearer, height, role: "bearer");
            }
        }
        if (format == SymbolFormat.Gs1Code128 && height < 31.75) b.Finding("BarHeight", true, "The selected GS1-128 distribution layout requires bars at least 31.75 mm high.");
        if (format == SymbolFormat.Itf14 && height < 31.75) b.Finding("BarHeight", true, "The ITF-14 distribution layout requires bars at least 31.75 mm high.");
        if (format is SymbolFormat.Itf14 or SymbolFormat.Gs1Code128 && b.X < .495)
            b.Finding("ApplicationModuleSize", false, "This module width is below the general-distribution minimum. Qualify it against the selected GS1 application and substrate.");
        if (format == SymbolFormat.Flattermarken && (height < 5 || height > 10))
            b.Finding("BarHeight", true, "Flattermarken bars require a height from 5 through 10 mm.");
        var bounds = new BarcodeBounds(0, 0, symbolWidth + 2 * (q + sideBearer), height + 2 * bearer);
        b.OrdinaryText(bounds, minimumInkGap: format == SymbolFormat.Itf14 ? 1.02 : 0);
        return b.Finish(bounds);
    }

    /// <summary>Positions raw alternating runs with no prescribed quiet zone, no inferred text and a nominal height of 15 percent of their width.</summary>
    public static BarcodePrintLayout LayoutRaw(Barcode1D symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        RawLinear.GetWidths(symbol);
        if (options?.Format.HasValue == true) throw new ArgumentException("A raw layout has no application format.", nameof(options));
        var b = new PrintLayoutBuilder(null, options, .330);
        var width = symbol.TotalModules * b.X;
        var height = b.Options.BarHeightMm ?? width * .15;
        LinearBars(b, symbol, 0, 0, height);
        var bounds = new BarcodeBounds(0, 0, width, height);
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    private static double LinearBars(PrintLayoutBuilder builder, Barcode1D symbol, double x, double y, double height, bool itf14Ratio = false) {
        var start = x;
        var wide = 2.5 * builder.X;
        if (builder.Options.SnapToPrinterPixels) {
            var narrowPixels = Math.Round(builder.X * builder.Options.Dpi / 25.4);
            wide = Math.Round(2.5 * narrowPixels, MidpointRounding.AwayFromZero) * 25.4 / builder.Options.Dpi;
        }
        foreach (var run in symbol.Segments) {
            var width = itf14Ratio && run.Modules == 3 ? wide : run.Modules * builder.X;
            if (run.IsBar) builder.Rect(x, y, width, height);
            x += width;
        }
        return x - start;
    }

    /// <summary>Positions an orthogonal encoded symbol with family-specific row geometry.</summary>
    public static BarcodePrintLayout Layout(MatrixSymbol symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (symbol is Gs1CompositeSymbol composite) return Composite(composite, options);
        if (symbol is Gs1CompositeComponentSymbol component) return Layout(component, options);
        if (symbol is AustraliaPostSymbol postal) return Layout(postal, options);
        return Layout(symbol.Modules, symbol.Format, options);
    }

    /// <summary>Positions caller-sampled modules with an explicit format. A module grid carries no application identity.</summary>
    public static BarcodePrintLayout Layout(BitMatrix modules, SymbolFormat format, BarcodePrintOptions? options = null) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (!Enum.IsDefined(typeof(SymbolFormat), format)) throw new ArgumentOutOfRangeException(nameof(format));
        if (format == SymbolFormat.MaxiCode) throw new ArgumentException("Use the MaxiCodeSymbol overload to retain hexagons and its recognition pattern.", nameof(format));
        if (format == SymbolFormat.Ultracode) throw new ArgumentException("Ultracode requires colour cells.", nameof(format));
        if (format == SymbolFormat.Gs1Composite) throw new ArgumentException("Use Gs1CompositeSymbol so carrier and component row geometry are retained.", nameof(format));
        if (IsFourState(format) || format is SymbolFormat.Postnet or SymbolFormat.Planet or SymbolFormat.PharmacodeTwoTrack)
            return Postal(modules, format, options);
        if (SymbolCapabilities.Get(format).Family == SymbolFamily.Linear && modules.Height == 1)
            return Layout(RetailRuns(modules, 0, 0, modules.Width, format), options);
        return Grid(modules, format, options);
    }

    private static BarcodePrintLayout Grid(BitMatrix modules, SymbolFormat format, BarcodePrintOptions? options, double nominal = .495,
        double minimum = 0, double maximum = double.PositiveInfinity, bool humanReadableAbove = false) {
        var codeOneQuiet = format == SymbolFormat.CodeOne && (modules.Height == 8 || modules.Height == 16 && modules.Width % 16 == 1);
        var b = new PrintLayoutBuilder(format, options, nominal, minimum, maximum, requiresQuietZones: format != SymbolFormat.CodeOne || codeOneQuiet);
        var quiet = MatrixQuiet(format);
        if (format == SymbolFormat.CodeOne) quiet = codeOneQuiet ? 1 : 0;
        var ql = b.Options.IncludeQuietZones && format is SymbolFormat.Code16K or SymbolFormat.Code49 or SymbolFormat.CodablockF ? 10 * b.X : 0;
        var qx = b.Options.IncludeQuietZones ? quiet * b.X : 0;
        if (ql == 0) ql = qx;
        var qy = format is SymbolFormat.Code16K or SymbolFormat.Code49 or SymbolFormat.CodablockF or SymbolFormat.CodeOne ? 0 : qx;
        var bottomQuiet = format == SymbolFormat.CodeOne ? qx : qy;
        if (format == SymbolFormat.CodablockF) qx = ql;
        var rows = MatrixRowHeights(modules, format, b);
        var width = modules.Width * b.X;
        var height = 0D;
        for (var y = 0; y < modules.Height; y++) {
            for (var x = 0; x < modules.Width;) {
                if (!modules[x, y]) { x++; continue; }
                var start = x++;
                if (format != SymbolFormat.DotCode) while (x < modules.Width && modules[x, y]) x++;
                if (format == SymbolFormat.DotCode) Circle(b, ql + (start + .5) * b.X, qy + height + rows[y] / 2, b.X * .4, b.Options.Foreground, "dots");
                else b.Rect(ql + start * b.X, qy + height, (x - start) * b.X, rows[y]);
            }
            height += rows[y];
        }
        var bounds = new BarcodeBounds(0, 0, width + ql + qx, height + qy + bottomQuiet);
        b.OrdinaryText(bounds, humanReadableAbove);
        return b.Finish(bounds);
    }

    /// <summary>Positions a QR symbol with its four-module quiet zone.</summary>
    public static BarcodePrintLayout Layout(QrCode symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        return Layout(symbol.Modules, SymbolFormat.QrCode, options);
    }

    /// <summary>Builds an application identifier's bars and its book, publishing, healthcare or logistics caption.</summary>
    public static BarcodePrintLayout Layout(LinearBarcodeProfile profile, string value, BarcodePrintOptions? options = null) {
        var o = options?.Snapshot() ?? new BarcodePrintOptions();
        var symbol = BarcodeProfiles.Encode(profile, value);
        var caption = BarcodeProfiles.PublishingCaption(profile, value);
        if (profile is LinearBarcodeProfile.Identcode or LinearBarcodeProfile.Leitcode) return GermanPost(symbol, caption, o);
        if (profile == LinearBarcodeProfile.MarksAndSpencer) return MarksAndSpencer(symbol, value.Replace(" ", string.Empty).Replace("-", string.Empty), o);
        if (profile is LinearBarcodeProfile.Isbn or LinearBarcodeProfile.Ismn or LinearBarcodeProfile.Issn) o.Caption ??= caption;
        else o.HumanReadableText ??= caption;
        if (profile is LinearBarcodeProfile.Ean14 or LinearBarcodeProfile.Sscc18) o.TextStyle = BarcodePrintTextStyle.Gs1;
        return Layout(symbol, o);
    }

    private static double LinearHeight(SymbolFormat? format, int width, double x) => format switch {
        SymbolFormat.Gs1Code128 or SymbolFormat.Itf14 => 31.75,
        SymbolFormat.Gs1DataBarOmnidirectional => x * 33,
        SymbolFormat.Gs1DataBarTruncated or SymbolFormat.Gs1DataBarLimited => x * 13,
        SymbolFormat.Gs1DataBarExpanded => x * 34,
        SymbolFormat.FacingIdentificationMark => 25.4 * 5 / 8,
        SymbolFormat.Pharmacode => 8,
        SymbolFormat.Flattermarken => 10,
        _ => Math.Max(6.35, width * x * .15)
    };
    private static bool IsRetail(SymbolFormat? f) => f is SymbolFormat.Ean or SymbolFormat.UpcA or SymbolFormat.UpcE or SymbolFormat.Ean2 or SymbolFormat.Ean5;
    private static bool IsDataBar(SymbolFormat? f) => f is SymbolFormat.Gs1DataBarExpanded or SymbolFormat.Gs1DataBarLimited
        or SymbolFormat.Gs1DataBarOmnidirectional or SymbolFormat.Gs1DataBarTruncated or SymbolFormat.Gs1DataBarStacked
        or SymbolFormat.Gs1DataBarExpandedStacked or SymbolFormat.Gs1DataBarStackedOmnidirectional;
    private static int MatrixQuiet(SymbolFormat f) => f switch {
        SymbolFormat.QrCode or SymbolFormat.D3Aqr => 4,
        SymbolFormat.MicroQrCode or SymbolFormat.RmQrCode or SymbolFormat.Pdf417 or SymbolFormat.MicroPdf417 => 2,
        SymbolFormat.DotCode or SymbolFormat.HanXin => 3,
        SymbolFormat.Aztec or SymbolFormat.AztecRune or SymbolFormat.Gs1DataBarStacked or SymbolFormat.Gs1DataBarStackedOmnidirectional or SymbolFormat.Gs1DataBarExpandedStacked => 0,
        _ => 1
    };
}

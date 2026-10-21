using System;

namespace CodeGlyphX.Printing;

public static partial class BarcodePrint {
    /// <summary>Positions the exact Australia Post fields and decimal parity line above the physically measured bars.</summary>
    public static BarcodePrintLayout Layout(AustraliaPostSymbol symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        var o = options?.Snapshot() ?? new BarcodePrintOptions { FontSizePoints = 8 };
        o.HumanReadableText ??= symbol.Data.HumanReadable;
        return Postal(symbol.Modules, SymbolFormat.AustraliaPost, o);
    }

    private static BarcodePrintLayout Postal(BitMatrix modules, SymbolFormat format, BarcodePrintOptions? options) {
        var fourState = IsFourState(format);
        if (modules.Width < 1 || (modules.Width & 1) == 0 || modules.Width > 20001 || modules.Height < 2 || modules.Height > 16)
            throw new ArgumentException("Postal grids require alternating single bar/gap columns and two through sixteen height bands.", nameof(modules));
        var nominal = format switch {
            SymbolFormat.AustraliaPost => .5,
            SymbolFormat.JapanPost => .6,
            SymbolFormat.PharmacodeTwoTrack => 1,
            SymbolFormat.Postnet or SymbolFormat.Planet or SymbolFormat.UspsIntelligentMail => .508,
            _ => .54
        };
        var minimum = format == SymbolFormat.AustraliaPost ? .4 : format == SymbolFormat.JapanPost ? .48
            : format is SymbolFormat.Postnet or SymbolFormat.Planet or SymbolFormat.UspsIntelligentMail ? .015 * 25.4
            : format == SymbolFormat.PharmacodeTwoTrack ? .8 : .38;
        var maximum = format == SymbolFormat.AustraliaPost ? .6 : format == SymbolFormat.JapanPost ? .69
            : format is SymbolFormat.Postnet or SymbolFormat.Planet or SymbolFormat.UspsIntelligentMail ? .025 * 25.4
            : format == SymbolFormat.PharmacodeTwoTrack ? 2 : .63;
        var b = new PrintLayoutBuilder(format, options, nominal, minimum, maximum);
        if (format == SymbolFormat.UspsIntelligentMail && b.Options.Font is not null && b.Options.HumanReadableText is null
            && CodeGlyphX.Postal.UspsImbDecoder.TryDecode(modules, out var imbText))
            b.Options.HumanReadableText = IntelligentMailReadable(imbText);
        var pitch = format switch {
            SymbolFormat.AustraliaPost => 1.1,
            SymbolFormat.JapanPost => 2 * b.X,
            SymbolFormat.PharmacodeTwoTrack => 2 * b.X,
            SymbolFormat.Postnet or SymbolFormat.Planet or SymbolFormat.UspsIntelligentMail => 25.4 / 22,
            _ => 25.4 / 21.2
        };
        if (b.Options.SnapToPrinterPixels) pitch = Math.Max(1, Math.Round(pitch * b.Options.Dpi / 25.4)) * 25.4 / b.Options.Dpi;
        if (pitch <= b.X) throw new ArgumentException("The selected postal bar width leaves no gap between bars.", nameof(options));
        var full = format switch {
            SymbolFormat.JapanPost => 6 * b.X,
            SymbolFormat.PharmacodeTwoTrack => 8,
            SymbolFormat.Postnet or SymbolFormat.Planet => .125 * 25.4,
            SymbolFormat.UspsIntelligentMail => .145 * 25.4,
            SymbolFormat.AustraliaPost => 5,
            _ => 5.1
        };
        var tracker = format switch {
            SymbolFormat.JapanPost => 2 * b.X,
            SymbolFormat.Postnet or SymbolFormat.Planet or SymbolFormat.UspsIntelligentMail => .05 * 25.4,
            _ => 1.3
        };
        if (b.Options.BarHeightMm.HasValue) {
            tracker *= b.Options.BarHeightMm.Value / full;
            full = b.Options.BarHeightMm.Value;
        }
        var qx = format switch {
            SymbolFormat.AustraliaPost => 6,
            SymbolFormat.PharmacodeTwoTrack => 6,
            SymbolFormat.Daft => 0,
            SymbolFormat.Postnet or SymbolFormat.Planet or SymbolFormat.UspsIntelligentMail => .125 * 25.4,
            _ => 2
        };
        var qy = format switch {
            SymbolFormat.UspsIntelligentMail => .028 * 25.4,
            SymbolFormat.Postnet or SymbolFormat.Planet => .04 * 25.4,
            SymbolFormat.Daft => 0,
            _ => 2
        };
        if (!b.Options.IncludeQuietZones) qx = qy = 0;
        var extension = (full - tracker) / 2;
        for (var x = 0; x < modules.Width; x++) {
            if ((x & 1) != 0) {
                for (var y = 0; y < modules.Height; y++) if (modules[x, y]) throw new ArgumentException("Postal gap columns must be blank.", nameof(modules));
                continue;
            }
            var top = modules[x, 0]; var bottom = modules[x, modules.Height - 1];
            double yStart, barHeight;
            if (format == SymbolFormat.PharmacodeTwoTrack) {
                if (!top && !bottom) throw new ArgumentException("A two-track Pharmacode bar cannot be empty.", nameof(modules));
                yStart = top ? 0 : full / 2;
                barHeight = top && bottom ? full : full / 2;
            } else if (!fourState) {
                if (!bottom) throw new ArgumentException("POSTNET/PLANET bars share a bottom baseline.", nameof(modules));
                yStart = top ? 0 : full - tracker; barHeight = top ? full : tracker;
            } else {
                if (!modules[x, modules.Height / 2]) throw new ArgumentException("A four-state bar requires its tracker.", nameof(modules));
                yStart = top ? 0 : extension;
                barHeight = tracker + (top ? extension : 0) + (bottom ? extension : 0);
            }
            b.Rect(qx + x / 2 * pitch, qy + yStart, b.X, barHeight);
        }
        PostalPreflight(b, format, pitch, full, tracker);
        var bounds = new BarcodeBounds(0, 0, ((modules.Width + 1) / 2 - 1) * pitch + b.X + 2 * qx, full + 2 * qy);
        if (b.Options.IncludeHumanReadable && !string.IsNullOrEmpty(b.Options.HumanReadableText)) {
            if (format == SymbolFormat.UspsIntelligentMail && b.Options.FontSizePoints < 10)
                b.Finding("PostalTextSize", true, "Intelligent Mail readable text requires at least 10-point sans-serif lettering.");
            if (format == SymbolFormat.AustraliaPost && b.Options.FontSizePoints > 8)
                b.Finding("PostalTextSize", true, "Australia Post readable text must not exceed 8 points.");
        }
        b.OrdinaryText(bounds, humanReadableAbove: format == SymbolFormat.AustraliaPost);
        return b.Finish(bounds);
    }

    private static string IntelligentMailReadable(string checkedText) {
        var tracker = checkedText.Substring(0, 20);
        var mailerLength = tracker[5] == '9' ? 9 : 6;
        var readable = tracker.Substring(0, 2) + " " + tracker.Substring(2, 3) + " " + tracker.Substring(5, mailerLength)
            + " " + tracker.Substring(5 + mailerLength);
        if (checkedText.Length <= 20) return readable;
        var routing = checkedText.Substring(21);
        readable += " " + routing.Substring(0, 5);
        if (routing.Length > 5) readable += " " + routing.Substring(5, 4);
        if (routing.Length > 9) readable += " " + routing.Substring(9, 2);
        return readable;
    }

    private static void PostalPreflight(PrintLayoutBuilder b, SymbolFormat format, double pitch, double full, double tracker) {
        var density = 25.4 / pitch;
        var gap = pitch - b.X;
        if (format == SymbolFormat.AustraliaPost) {
            if (full < 4.2 || full > 5.8 || tracker < 1 || tracker > 1.6 || gap < .4 || gap > .7 || density < 22 || density > 25)
                b.Finding("PostalDimensions", true, "The Australia Post bar height, tracker, gap or density lies outside its physical range. Change DPI or dimensions.");
        } else if (format == SymbolFormat.UspsIntelligentMail) {
            if (full < .125 * 25.4 || full > .165 * 25.4 || tracker < .039 * 25.4 || tracker > .063 * 25.4
                || density < 20 || density > 24 || gap < .012 * 25.4 || gap > .04 * 25.4)
                b.Finding("PostalDimensions", true, "The Intelligent Mail bar height, tracker, gap or pitch lies outside USPS-B-3200 limits.");
        } else if (format is SymbolFormat.Postnet or SymbolFormat.Planet) {
            if (full < .115 * 25.4 || full > .135 * 25.4 || tracker < .04 * 25.4 || tracker > .06 * 25.4 || density < 20 || density > 24)
                b.Finding("PostalDimensions", true, "The POSTNET/PLANET bar height or pitch lies outside its printing limits.");
        } else if (format is SymbolFormat.RoyalMail4State or SymbolFormat.Mailmark4State or SymbolFormat.KixCode) {
            if (full < 4.22 || full > 5.84 || tracker < 1.02 || tracker > 1.52 || density < 20 || density > 24 || gap < .4)
                b.Finding("PostalDimensions", true, "The four-state bar height, tracker, gap or pitch lies outside its selected postal range.");
        }
    }
}

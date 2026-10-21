using System;
using System.Collections.Generic;

namespace CodeGlyphX.Printing;

public static partial class BarcodePrint {
    private sealed class RetailData {
        internal string Digits = string.Empty;
        internal string Supplement = string.Empty;
        internal int MainWidth;
        internal SymbolFormat Format;
    }

    private static bool TryRetail(Barcode1D symbol, SymbolFormat format, out RetailData data) {
        data = new RetailData { Format = format };
        if (symbol.TotalModules > 4096) return false;
        var modules = LinearModules(symbol);
        if (!BarcodeDecoder.TryDecode(modules, SymbolCapabilities.GetBarcodeType(format), out var decoded)) return false;
        var split = decoded.Text.IndexOf('+');
        data.Digits = split < 0 ? decoded.Text : decoded.Text.Substring(0, split);
        data.Supplement = split < 0 ? string.Empty : decoded.Text.Substring(split + 1);
        data.MainWidth = format switch {
            SymbolFormat.UpcE => 51,
            SymbolFormat.Ean2 => 20,
            SymbolFormat.Ean5 => 47,
            SymbolFormat.Ean when data.Digits.Length == 8 => 67,
            _ => 95
        };
        return data.Digits.Length is 2 or 5 or 8 or 12 or 13;
    }

    private static BarcodePrintLayout Retail(Barcode1D symbol, RetailData data, BarcodePrintOptions? options) {
        var b = new PrintLayoutBuilder(data.Format, options, .330, .264, .660);
        var standalone = data.Format is SymbolFormat.Ean2 or SymbolFormat.Ean5;
        var ean8 = data.MainWidth == 67;
        var leftQuiet = data.Format == SymbolFormat.UpcA || data.Format == SymbolFormat.UpcE ? 9 : ean8 || standalone ? 7 : 11;
        var rightQuiet = data.Format == SymbolFormat.UpcA ? 9 : standalone ? 5 : 7;
        var ql = b.Options.IncludeQuietZones ? leftQuiet * b.X : 0;
        var qr = b.Options.IncludeQuietZones ? (data.Supplement.Length > 0 ? 5 : rightQuiet) * b.X : 0;
        var height = b.Options.BarHeightMm ?? (ean8 ? 18.23 : 22.85) * b.X / .330;
        var expectedHeight = (ean8 ? 18.23 : 22.85) * b.X / .330;
        if (height + 1e-7 < expectedHeight) b.Finding("RetailBarHeight", true, "Retail bars are shorter than the height for the resolved magnification. Restore the proportional height.");
        var modules = LinearModules(symbol);
        var guard = standalone ? 0 : 5 * b.X;
        var includeText = b.Options.IncludeHumanReadable;
        var supplementWidth = data.Supplement.Length == 2 ? 20 : data.Supplement.Length == 5 ? 47 : 0;
        // Existing pixel renderers retain their historical add-on gap. Only this physical layout repositions it.
        var minimumGap = data.Format is SymbolFormat.UpcA or SymbolFormat.UpcE ? 9 : 7;
        var sourceGap = supplementWidth > 0 ? modules.Length - data.MainWidth - supplementWidth : minimumGap;
        var addOnStart = data.MainWidth + Math.Min(12, Math.Max(minimumGap, sourceGap));
        var top = 0D;
        if (standalone) top = 9 * b.X;
        for (var x = 0; x < data.MainWidth; x++) if (modules[x]) {
            var extension = RetailGuard(data, x) ? guard : 0;
            b.Rect(ql + x * b.X, top, b.X, height + extension - top);
        }
        if (supplementWidth > 0) {
            var sourceStart = modules.Length - supplementWidth;
            var addOnTop = 9 * b.X;
            for (var x = 0; x < supplementWidth; x++) if (modules[sourceStart + x])
                b.Rect(ql + (addOnStart + x) * b.X, addOnTop, b.X, height + guard - addOnTop);
        }
        var width = ql + (supplementWidth > 0 ? addOnStart + supplementWidth : data.MainWidth) * b.X + qr;
        var bounds = new BarcodeBounds(0, 0, width, height + guard);
        var customText = b.Options.HumanReadableText;
        if (includeText && (customText is null || customText == data.Digits + (data.Supplement.Length > 0 ? "+" + data.Supplement : string.Empty))) {
            var font = b.Options.Font ?? throw new ArgumentException("Retail readable digits require a supplied font, or IncludeHumanReadable=false.");
            var reference = font.Shape("0123456789", b.Options.FontSizePoints * 25.4 / 72, b.Options.StrictText);
            var textTop = height + b.X - reference.InkBounds.Y;
            if (reference.InkBounds.Height + 1e-7 < 7 * b.X)
                b.Finding("RetailTextHeight", false, "Readable digits are below the recommended seven-module ink height. Increase the font size.");
            void Digits(string digits, double start, double span, bool right = false, bool center = true) {
                var line = b.Line(digits, font, b.Options.FontSizePoints, ql + start * b.X, textTop, span * b.X,
                    alignRight: right, center: center, alignInk: true);
                if (line.InkBounds.Width > span * b.X + 1e-7)
                    b.Finding("RetailTextDoesNotFit", true, "A retail digit group does not fit its reserved span. Select a narrower font or increase module size.");
            }
            if (standalone) b.Line(data.Digits, font, b.Options.FontSizePoints, ql, -reference.InkBounds.Y, data.MainWidth * b.X, alignInk: true);
            else if (data.Format == SymbolFormat.UpcE) {
                Digits(data.Digits.Substring(0, 1), -leftQuiet, leftQuiet - 1, right: true);
                Digits(data.Digits.Substring(1, 6), 3, 42);
                Digits(data.Digits.Substring(7), 52, rightQuiet - 1, center: false);
            } else if (data.Format == SymbolFormat.UpcA) {
                Digits(data.Digits.Substring(0, 1), -leftQuiet, leftQuiet - 1, right: true);
                Digits(data.Digits.Substring(1, 5), 10, 35);
                Digits(data.Digits.Substring(6, 5), 50, 35);
                Digits(data.Digits.Substring(11), 96, rightQuiet - 1, center: false);
            } else if (ean8) {
                Digits(data.Digits.Substring(0, 4), 3, 28);
                Digits(data.Digits.Substring(4), 36, 28);
            } else {
                Digits(data.Digits.Substring(0, 1), -leftQuiet, leftQuiet - 1, right: true);
                Digits(data.Digits.Substring(1, 6), 3, 42);
                Digits(data.Digits.Substring(7), 50, 42);
            }
            if (data.Supplement.Length > 0)
                b.Line(data.Supplement, font, b.Options.FontSizePoints, ql + addOnStart * b.X,
                    -reference.InkBounds.Y, supplementWidth * b.X, alignInk: true);
            if (b.Options.QuietZoneIndicators && data.Format is not (SymbolFormat.UpcA or SymbolFormat.UpcE)) {
                if (ean8) b.Line("<", font, b.Options.FontSizePoints, 0, textTop, ql, center: false, alignInk: true);
                b.Line(">", font, b.Options.FontSizePoints, width - qr, textTop, qr, alignRight: true, alignInk: true);
            }
        } else if (includeText && !string.IsNullOrEmpty(customText)) {
            b.Lines(customText!, b.Options.Font, b.Options.FontSizePoints, 0, bounds.Bottom + b.X, width, false);
        }
        // Reuse the caption policy without drawing a second copy of the derived digits.
        b.Options.HumanReadableText = null;
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    private static bool RetailGuard(RetailData data, int x) {
        if (data.Format is SymbolFormat.Ean2 or SymbolFormat.Ean5) return false;
        if (data.Format == SymbolFormat.UpcE) return x < 3 || x >= 45;
        if (data.Format == SymbolFormat.UpcA) return x < 10 || x >= 85 || x is >= 45 and < 50;
        return x < 3 || x >= data.MainWidth - 3 || x >= data.MainWidth / 2 - 2 && x <= data.MainWidth / 2 + 2;
    }

    private static bool[] LinearModules(Barcode1D symbol) {
        if (symbol.TotalModules > 1048576) throw new ArgumentException("The print symbol exceeds the module limit.", nameof(symbol));
        var modules = new bool[symbol.TotalModules];
        var x = 0;
        foreach (var run in symbol.Segments) for (var i = 0; i < run.Modules; i++) modules[x++] = run.IsBar;
        return modules;
    }
}

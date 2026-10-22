// Composite alignment and separator rules follow ISO/IEC 24723 and the Zint reference backend.
// Portions adapted from Zint, Copyright (c) Robin Stuart and contributors, BSD-3-Clause.
// See THIRD-PARTY-NOTICES.md.
using System;
using CodeGlyphX.DataBar;
using CodeGlyphX.Ean;
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;

namespace CodeGlyphX.Gs1Composite;

internal static partial class CompositeCarrierCodec {
    internal static Gs1CompositeSymbol Encode(string primary, string secondary, Gs1CompositeEncodingOptions options) {
        if (options.Component == Gs1CompositeComponent.CcC)
            throw new ArgumentException("CC-C requires a GS1-128 carrier.", nameof(options));
        var carrier = BuildCarrier(primary, options.Carrier, options.DataBarExpandedColumns, out var linearText);
        var composite = Gs1.ElementString(secondary);
        var columns = Columns(options.Carrier);
        var type = options.Component == Gs1CompositeComponent.Auto ? Gs1CompositeComponent.CcA : options.Component;
        bool[] bits;
        int resolvedColumns, correction;
        try {
            bits = CompositeBitStreamCodec.Encode(composite, type, columns, carrier.Width, out resolvedColumns, out correction);
        } catch (ArgumentException ex) when (ex.ParamName == "elementString" && options.Component == Gs1CompositeComponent.Auto) {
            type = Gs1CompositeComponent.CcB;
            bits = CompositeBitStreamCodec.Encode(composite, type, columns, carrier.Width, out resolvedColumns, out correction);
        }
        var component = CompositeComponentCodec.Encode(bits, type, resolvedColumns, correction);
        var merged = Merge(component, carrier, options.Carrier, type);
        return new Gs1CompositeSymbol(merged, linearText, composite, type, component.Height, options.Carrier);
    }

    private static int Columns(Gs1CompositeCarrier carrier) => carrier switch {
        Gs1CompositeCarrier.Ean8 or Gs1CompositeCarrier.DataBarLimited => 3,
        Gs1CompositeCarrier.UpcE or Gs1CompositeCarrier.DataBarStacked or Gs1CompositeCarrier.DataBarStackedOmnidirectional => 2,
        _ => 4
    };

    private static BitMatrix BuildCarrier(string primary, Gs1CompositeCarrier carrier, int expandedColumns, out string linearText) {
        if (primary is null) throw new ArgumentNullException(nameof(primary));
        if (IsRetail(carrier)) {
            var barcode = carrier switch {
                Gs1CompositeCarrier.UpcA => UpcAEncoder.EncodeComposite(primary),
                Gs1CompositeCarrier.UpcE => UpcEEncoder.EncodeComposite(primary),
                _ => EanEncoder.EncodeComposite(primary)
            };
            var modules = Row(barcode);
            var expected = carrier == Gs1CompositeCarrier.UpcA ? BarcodeType.UPCA
                : carrier == Gs1CompositeCarrier.UpcE ? BarcodeType.UPCE : BarcodeType.EAN;
            if (!BarcodeDecoder.TryDecode(modules, expected, out var decoded))
                throw new ArgumentException("The retail carrier could not be validated.", nameof(primary));
            linearText = decoded.Text;
            var digits = linearText.IndexOf('+');
            if (digits < 0) digits = linearText.Length;
            if ((carrier == Gs1CompositeCarrier.Ean8 && digits != 8) || (carrier == Gs1CompositeCarrier.Ean13 && digits != 13))
                throw new ArgumentException("The EAN input length does not match the selected Composite carrier.", nameof(primary));
            return RetailCarrier(modules, carrier);
        }
        BitMatrix data;
        if (carrier == Gs1CompositeCarrier.DataBarExpanded || carrier == Gs1CompositeCarrier.DataBarExpandedStacked) {
            linearText = Gs1.ElementString(primary);
            data = carrier == Gs1CompositeCarrier.DataBarExpanded
                ? Matrix(DataBarExpandedEncoder.EncodeComposite(linearText))
                : DataBarExpandedEncoder.EncodeCompositeStacked(linearText, expandedColumns);
        } else {
            var gtin = NormalizeGtin(primary);
            linearText = "01" + gtin;
            data = carrier == Gs1CompositeCarrier.DataBarLimited
                ? Matrix(DataBarLimitedEncoder.EncodeComposite(gtin))
                : DataBar14Encoder.EncodeComposite(gtin.Substring(0, 13), carrier);
        }
        var result = new BitMatrix(data.Width, data.Height + 1);
        Copy(data, result, 0, 1);
        FillDataBarSeparator(result, carrier);
        return result;
    }

    private static string NormalizeGtin(string primary) {
        var value = Gs1.ElementString(primary);
        if (value.Length == 16 && value.StartsWith("01", StringComparison.Ordinal)) value = value.Substring(2);
        if (value.Length < 1 || value.Length > 14) throw new ArgumentException("This DataBar carrier expects a GTIN.", nameof(primary));
        for (var i = 0; i < value.Length; i++)
            if (value[i] < '0' || value[i] > '9') throw new ArgumentException("A GTIN contains digits only.", nameof(primary));
        if (value.Length < 14) value = value.PadLeft(13, '0') + CheckDigit(value.PadLeft(13, '0'));
        if (value[13] != CheckDigit(value.Substring(0, 13))) throw new ArgumentException("The GTIN check digit does not match.", nameof(primary));
        return value;
    }

    private static char CheckDigit(string digits) {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++) sum += (digits[i] - '0') * ((i & 1) == 0 ? 3 : 1);
        return (char)('0' + (10 - sum % 10) % 10);
    }

    private static BitMatrix RetailCarrier(bool[] modules, Gs1CompositeCarrier carrier) {
        var mainWidth = carrier == Gs1CompositeCarrier.Ean8 ? 67 : carrier == Gs1CompositeCarrier.UpcE ? 51 : 95;
        var hasAddOn = modules.Length > mainWidth;
        var result = new BitMatrix(modules.Length + (hasAddOn ? 1 : 2), hasAddOn ? 5 : 4);
        result[1, 0] = result[mainWidth, 0] = true;
        result[0, 1] = result[mainWidth + 1, 1] = true;
        result[1, 2] = result[mainWidth, 2] = true;
        for (var x = 0; x < modules.Length; x++) {
            result[x + 1, 3] = modules[x];
            if (hasAddOn && x >= mainWidth) result[x + 1, 4] = modules[x];
        }
        return result;
    }

    private static BitMatrix Merge(BitMatrix component, BitMatrix carrier, Gs1CompositeCarrier kind, Gs1CompositeComponent type) {
        Shifts(carrier, kind, type, out var top, out var bottom);
        var result = new BitMatrix(Math.Max(component.Width + top, carrier.Width + bottom), component.Height + carrier.Height);
        Copy(component, result, top, 0);
        Copy(carrier, result, bottom, component.Height);
        return result;
    }

    private static void Shifts(BitMatrix carrier, Gs1CompositeCarrier kind, Gs1CompositeComponent type, out int top, out int bottom) {
        top = bottom = 0;
        switch (kind) {
            case Gs1CompositeCarrier.Ean8:
            case Gs1CompositeCarrier.Ean13:
            case Gs1CompositeCarrier.UpcA:
            case Gs1CompositeCarrier.UpcE: bottom = RetailOffset(kind, type); break;
            case Gs1CompositeCarrier.DataBarOmnidirectional:
            case Gs1CompositeCarrier.DataBarTruncated: bottom = 4; break;
            case Gs1CompositeCarrier.DataBarLimited:
                if (type == Gs1CompositeComponent.CcA) top = 1; else bottom = 9;
                break;
            case Gs1CompositeCarrier.DataBarStacked:
            case Gs1CompositeCarrier.DataBarStackedOmnidirectional: top = 1; break;
            case Gs1CompositeCarrier.DataBarExpanded:
            case Gs1CompositeCarrier.DataBarExpandedStacked:
                top = 1;
                while (top < carrier.Width && !carrier[top - 1, 1] && carrier[top, 1]) top++;
                break;
        }
    }

    /// <summary>Returns the retail carrier's horizontal displacement from the upper component's left edge.</summary>
    internal static int RetailOffset(Gs1CompositeCarrier kind, Gs1CompositeComponent type) =>
        kind == Gs1CompositeCarrier.Ean8 ? type == Gs1CompositeComponent.CcA ? 3 : 13 : 2;

    private static bool IsRetail(Gs1CompositeCarrier carrier) => carrier >= Gs1CompositeCarrier.Ean13 && carrier <= Gs1CompositeCarrier.UpcE;

    private static bool[] Row(Barcode1D barcode) {
        var row = new bool[barcode.TotalModules];
        var x = 0;
        foreach (var segment in barcode.Segments)
            for (var module = 0; module < segment.Modules; module++) row[x++] = segment.IsBar;
        return row;
    }

    private static BitMatrix Matrix(Barcode1D barcode) {
        var row = Row(barcode);
        var result = new BitMatrix(row.Length, 1);
        for (var x = 0; x < row.Length; x++) result[x, 0] = row[x];
        return result;
    }

    private static void Copy(BitMatrix source, BitMatrix target, int left, int top) {
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
            if (source[x, y]) target[x + left, y + top] = true;
    }
}

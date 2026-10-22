using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.DataBar;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Gs1Composite;

internal static partial class CompositeCarrierCodec {
    internal static bool TryDecode(BitMatrix source, Gs1CompositeCarrier? expected, out Gs1CompositeDecoded decoded, bool tolerateSamplingErrors = false) {
        decoded = null!;
        if (source is null || source.Width > 600 || source.Height < 5 || source.Height > 90) return false;
        if (expected.HasValue && (expected.Value < Gs1CompositeCarrier.Ean13 || expected.Value > Gs1CompositeCarrier.DataBarExpandedStacked)) return false;
        for (var rows = 3; rows <= Math.Min(44, source.Height - 2); rows++) {
            if (DecodeBudget.ShouldAbort(CancellationToken.None)) return false;
            if (!CropComponent(source, rows, out var component, out var left)) continue;
            if (component.Width != 55 && component.Width != 72 && component.Width != 82 && component.Width != 99) continue;
            if (!CompositeComponentCodec.TryDecode(component, out var bits, out var type, tolerateSamplingErrors) || type == Gs1CompositeComponent.CcC) continue;
            if (!CompositeBitStreamCodec.TryDecode(bits, out var secondary)) continue;
            var first = expected.HasValue ? (int)expected.Value : (int)Gs1CompositeCarrier.Ean13;
            var last = expected.HasValue ? first : (int)Gs1CompositeCarrier.DataBarExpandedStacked;
            for (var value = first; value <= last; value++) {
                var carrier = (Gs1CompositeCarrier)value;
                var columns = Columns(carrier);
                var expectedWidth = columns == 2 ? 55 : columns == 4 ? 99 : type == Gs1CompositeComponent.CcA ? 72 : 82;
                if (component.Width != expectedWidth || !TryCarrier(source, rows, left, carrier, type, out var primary, out var linearRegion)) continue;
                decoded = new Gs1CompositeDecoded(primary, secondary, type, carrier,
                    new ImageRegion(left, 0, component.Width, rows), linearRegion);
                return true;
            }
        }
        return false;
    }

    private static bool TryCarrier(BitMatrix source, int rows, int componentLeft, Gs1CompositeCarrier kind,
        Gs1CompositeComponent type, out string primary, out ImageRegion linearRegion) {
        primary = string.Empty;
        linearRegion = default;
        var retail = IsRetail(kind);
        var bodyHeight = source.Height - rows;
        if (retail ? bodyHeight != 4 && bodyHeight != 5
            : kind == Gs1CompositeCarrier.DataBarStacked ? bodyHeight != 4
            : kind == Gs1CompositeCarrier.DataBarStackedOmnidirectional ? bodyHeight != 6
            : kind == Gs1CompositeCarrier.DataBarExpandedStacked ? bodyHeight < 2 || (bodyHeight - 2) % 4 != 0
            : bodyHeight != 2) return false;
        var bottom = retail ? RetailOffset(kind, type)
            : kind == Gs1CompositeCarrier.DataBarOmnidirectional || kind == Gs1CompositeCarrier.DataBarTruncated ? 4
            : kind == Gs1CompositeCarrier.DataBarLimited && type == Gs1CompositeComponent.CcB ? 9 : 0;
        var width = kind == Gs1CompositeCarrier.DataBarStacked || kind == Gs1CompositeCarrier.DataBarStackedOmnidirectional ? 50
            : kind == Gs1CompositeCarrier.DataBarLimited ? 79
            : kind == Gs1CompositeCarrier.DataBarOmnidirectional || kind == Gs1CompositeCarrier.DataBarTruncated ? 96
            : source.Width - bottom;
        if (width <= 0 || bottom + width > source.Width) return false;
        var body = new BitMatrix(width, bodyHeight);
        for (var y = 0; y < bodyHeight; y++) for (var x = 0; x < width; x++) body[x, y] = source[x + bottom, y + rows];
        Shifts(body, kind, type, out var top, out var expectedBottom);
        if (componentLeft != top || bottom != expectedBottom) return false;
        // A complete Composite must have no unrelated ink beside its carrier rows.
        for (var y = rows; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
            if ((x < bottom || x >= bottom + width) && source[x, y]) return false;
        linearRegion = new ImageRegion(bottom + (retail ? 1 : 0), rows + (retail ? 3 : 1),
            width - (retail ? bodyHeight == 5 ? 1 : 2 : 0), bodyHeight - (retail ? 3 : 1));
        if (retail) return TryRetail(body, kind, out primary);
        if (!ValidateDataBarSeparator(body, kind)) return false;
        var data = new BitMatrix(width, bodyHeight - 1);
        for (var y = 0; y < data.Height; y++) for (var x = 0; x < width; x++) data[x, y] = body[x, y + 1];
        if (kind == Gs1CompositeCarrier.DataBarExpanded || kind == Gs1CompositeCarrier.DataBarExpandedStacked)
            return DataBarExpandedDecoder.TryDecodeComposite(data, out primary);
        string digits;
        if (kind == Gs1CompositeCarrier.DataBarLimited) {
            var segments = new List<BarSegment>();
            var bar = data[0, 0]; var run = 0;
            for (var x = 0; x < width; x++) {
                if (data[x, 0] == bar) run++;
                else { segments.Add(new BarSegment(bar, run)); bar = !bar; run = 1; }
            }
            segments.Add(new BarSegment(bar, run));
            if (!DataBarLimitedDecoder.TryDecodeComposite(new Barcode1D(segments), out digits)) return false;
        } else if (!DataBar14Decoder.TryDecodeComposite(data, out digits)) return false;
        primary = "01" + digits + CheckDigit(digits);
        return true;
    }

    private static bool TryRetail(BitMatrix body, Gs1CompositeCarrier kind, out string primary) {
        primary = string.Empty;
        var mainWidth = kind == Gs1CompositeCarrier.Ean8 ? 67 : kind == Gs1CompositeCarrier.UpcE ? 51 : 95;
        if (body.Width < mainWidth + 2) return false;
        for (var y = 0; y < 3; y++) for (var x = 0; x < body.Width; x++) {
            var expected = y == 1 ? x == 0 || x == mainWidth + 1 : x == 1 || x == mainWidth;
            if (body[x, y] != expected) return false;
        }
        if (body[0, 3]) return false;
        var row = new bool[body.Width - 1];
        for (var x = 0; x < row.Length; x++) row[x] = body[x + 1, 3];
        var hint = kind == Gs1CompositeCarrier.UpcA ? BarcodeType.UPCA : kind == Gs1CompositeCarrier.UpcE ? BarcodeType.UPCE : BarcodeType.EAN;
        if (!BarcodeDecoder.TryDecode(row, hint, out var decoded)) return false;
        primary = decoded.Text;
        var length = primary.IndexOf('+');
        if (body.Height == 5) {
            if (length < 0) return false;
            for (var x = 0; x < body.Width; x++)
                if (body[x, 4] != (x > mainWidth && body[x, 3])) return false;
        } else if (length >= 0) return false;
        if (length < 0) length = primary.Length;
        return kind != Gs1CompositeCarrier.Ean8 && kind != Gs1CompositeCarrier.Ean13
            || length == (kind == Gs1CompositeCarrier.Ean8 ? 8 : 13);
    }

    private static bool ValidateDataBarSeparator(BitMatrix body, Gs1CompositeCarrier kind) {
        var expected = new BitMatrix(body.Width, 2);
        for (var x = 0; x < body.Width; x++) expected[x, 1] = body[x, 1];
        FillDataBarSeparator(expected, kind);
        if (MatchesDataBarSeparator(body, expected, kind)) return true;
        if (body.Width <= 200 || kind != Gs1CompositeCarrier.DataBarExpanded && kind != Gs1CompositeCarrier.DataBarExpandedStacked) return false;
        // Zint 2.15.0 adjusts the first four finder blocks only. Accept that complete
        // independent layout, without allowing a per-cell mixture of both encoders.
        expected = new BitMatrix(body.Width, 2);
        for (var x = 0; x < body.Width; x++) expected[x, 1] = body[x, 1];
        FillDataBarSeparator(expected, kind, finderLimit: 4);
        return MatchesDataBarSeparator(body, expected, kind);
    }

    private static bool MatchesDataBarSeparator(BitMatrix body, BitMatrix expected, Gs1CompositeCarrier kind) {
        for (var x = 0; x < body.Width; x++) {
            // BWIPP inverts Limited column 3; Zint leaves that boundary column blank.
            if (kind == Gs1CompositeCarrier.DataBarLimited && x == 3 && body[x, 0] == !body[x, 1]) continue;
            if (body[x, 0] != expected[x, 0]) return false;
        }
        return true;
    }

    private static bool CropComponent(BitMatrix source, int height, out BitMatrix component, out int left) {
        left = source.Width;
        var right = -1;
        for (var y = 0; y < height; y++) for (var x = 0; x < source.Width; x++) {
            if (!source[x, y]) continue;
            if (x < left) left = x;
            if (x > right) right = x;
        }
        component = null!;
        if (right < left) return false;
        component = new BitMatrix(right - left + 1, height);
        for (var y = 0; y < height; y++) for (var x = left; x <= right; x++) component[x - left, y] = source[x, y];
        return true;
    }
}

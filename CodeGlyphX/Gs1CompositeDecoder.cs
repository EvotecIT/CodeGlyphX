using System;
using CodeGlyphX.Gs1Composite;

namespace CodeGlyphX;

/// <summary>Decodes standards-linked GS1 Composite module matrices.</summary>
public static class Gs1CompositeDecoder {
    /// <summary>Attempts to decode both messages and identify their linear carrier.</summary>
    /// <remarks>Omnidirectional and Truncated DataBar have the same modules; use a carrier hint to retain the intended height profile.</remarks>
    public static bool TryDecode(BitMatrix modules, out Gs1CompositeDecoded decoded) {
        return TryDecode(modules, null, out decoded);
    }

    /// <summary>Attempts to decode a Composite symbol, optionally requiring a particular carrier.</summary>
    public static bool TryDecode(BitMatrix modules, Gs1CompositeCarrier? expectedCarrier, out Gs1CompositeDecoded decoded) {
        return TryDecodeCore(modules, expectedCarrier, out decoded, tolerateSamplingErrors: false);
    }

    internal static bool TryDecodeImage(BitMatrix modules, Gs1CompositeCarrier? expectedCarrier, out Gs1CompositeDecoded decoded) =>
        TryDecodeCore(modules, expectedCarrier, out decoded, tolerateSamplingErrors: true);

    private static bool TryDecodeCore(BitMatrix modules, Gs1CompositeCarrier? expectedCarrier, out Gs1CompositeDecoded decoded, bool tolerateSamplingErrors) {
        decoded = null!;
        if ((!expectedCarrier.HasValue || expectedCarrier == Gs1CompositeCarrier.Gs1_128) && TryDecode128(modules, out decoded, tolerateSamplingErrors)) return true;
        return expectedCarrier != Gs1CompositeCarrier.Gs1_128 && CompositeCarrierCodec.TryDecode(modules, expectedCarrier, out decoded, tolerateSamplingErrors);
    }

    private static bool TryDecode128(BitMatrix modules, out Gs1CompositeDecoded decoded, bool tolerateSamplingErrors) {
        decoded = null!;
        if (modules is null || modules.Height < 5) return false;
        var componentRows = modules.Height - 2;
        if (!TryCrop(modules, 0, componentRows, out var component)) return false;
        if (!CompositeComponentCodec.TryDecode(component, out var bits, out var type, tolerateSamplingErrors)) return false;
        if (!ValidateSeparatorRow(modules, modules.Height - 2, modules.Height - 1)) return false;
        if (!TryCropRow(modules, modules.Height - 1, out var linearModules)) return false;
        if (!BarcodeDecoder.TryDecodeGs1CompositeCarrier(linearModules, out var linearText, out var linkage)) return false;
        var expectedLinkage = type == Gs1CompositeComponent.CcC ? 2 : 1;
        if (linkage != expectedLinkage) return false;
        if (!CompositeBitStreamCodec.TryDecode(bits, out var compositeText)) return false;
        var componentLeft = 0;
        while (componentLeft < modules.Width && !HasInk(modules, componentLeft, componentRows)) componentLeft++;
        var linearLeft = 0;
        while (linearLeft < modules.Width && !modules[linearLeft, modules.Height - 1]) linearLeft++;
        decoded = new Gs1CompositeDecoded(linearText, compositeText, type, Gs1CompositeCarrier.Gs1_128,
            new ImageRegion(componentLeft, 0, component.Width, componentRows),
            new ImageRegion(linearLeft, modules.Height - 1, linearModules.Length, 1));
        return true;
    }

    private static bool HasInk(BitMatrix source, int x, int height) {
        for (var y = 0; y < height; y++) if (source[x, y]) return true;
        return false;
    }

    private static bool ValidateSeparatorRow(BitMatrix source, int separatorRow, int linearRow) {
        var left = 0;
        while (left < source.Width && !source[left, linearRow]) left++;
        var right = source.Width - 1;
        while (right >= left && !source[right, linearRow]) right--;
        if (right < left) return false;
        for (var x = 0; x < source.Width; x++) {
            var expected = x >= left && x <= right && !source[x, linearRow];
            if (source[x, separatorRow] != expected) return false;
        }
        return true;
    }

    private static bool TryCrop(BitMatrix source, int startRow, int rowCount, out BitMatrix cropped) {
        cropped = null!;
        var left = source.Width;
        var right = -1;
        for (var y = startRow; y < startRow + rowCount; y++) {
            for (var x = 0; x < source.Width; x++) {
                if (!source[x, y]) continue;
                if (x < left) left = x;
                if (x > right) right = x;
            }
        }
        if (right < left) return false;
        cropped = new BitMatrix(right - left + 1, rowCount);
        for (var y = 0; y < rowCount; y++) {
            for (var x = left; x <= right; x++) if (source[x, startRow + y]) cropped[x - left, y] = true;
        }
        return true;
    }

    private static bool TryCropRow(BitMatrix source, int row, out bool[] modules) {
        modules = Array.Empty<bool>();
        var left = 0;
        while (left < source.Width && !source[left, row]) left++;
        var right = source.Width - 1;
        while (right >= left && !source[right, row]) right--;
        if (right < left) return false;
        modules = new bool[right - left + 1];
        for (var x = left; x <= right; x++) modules[x - left] = source[x, row];
        return true;
    }
}

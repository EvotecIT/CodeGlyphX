using System;
using CodeGlyphX.Gs1Composite;

namespace CodeGlyphX;

/// <summary>Decodes standards-linked GS1-128 Composite module matrices.</summary>
public static class Gs1CompositeDecoder {
    /// <summary>Attempts to decode both messages from a GS1-128 Composite symbol.</summary>
    public static bool TryDecode(BitMatrix modules, out Gs1CompositeDecoded decoded) {
        decoded = null!;
        if (modules is null || modules.Height < 5) return false;
        var componentRows = modules.Height - 2;
        if (!TryCrop(modules, 0, componentRows, out var component)) return false;
        if (!TryCropRow(modules, modules.Height - 1, out var linearModules)) return false;
        if (!BarcodeDecoder.TryDecode(linearModules, BarcodeType.GS1_128, out var linear)) return false;
        if (!CompositeComponentCodec.TryDecode(component, out var bits, out var type)) return false;
        if (!CompositeBitStreamCodec.TryDecode(bits, out var compositeText)) return false;
        decoded = new Gs1CompositeDecoded(linear.Text, compositeText, type);
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

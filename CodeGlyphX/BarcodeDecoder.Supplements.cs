using System;
using CodeGlyphX.Ean;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    /// <summary>Decodes an exact standalone EAN supplement, validating its digit parity.</summary>
    public static bool TryDecodeEanSupplement(ReadOnlySpan<bool> modules, out string text) {
        text = string.Empty;
        var count = modules.Length == 20 ? 2 : modules.Length == 47 ? 5 : 0;
        if (count == 0) return false;
        var source = modules.ToArray();
        if (!MatchPattern(source, 0, AddOnStartPattern)) return false;
        if (!TryDecodeAddOnDigits(source, AddOnStartPattern.Length, count, out var digits, out var parity, out var end)
            || end != source.Length || !EanAddOn.ValidateParity(digits.AsSpan(), parity)) return false;
        text = digits;
        return true;
    }
}

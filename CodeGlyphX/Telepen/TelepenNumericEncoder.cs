using System;

namespace CodeGlyphX.Telepen;

/// <summary>Encodes Telepen Numeric using decimal pairs and optional X in the second position.</summary>
public static class TelepenNumericEncoder {
    /// <summary>Encodes a nonempty even-length numeric value. X may replace a pair's second digit.</summary>
    /// <remarks>Odd input is rejected rather than silently adding a leading digit.</remarks>
    public static Barcode1D Encode(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0 || (value.Length & 1) != 0)
            throw new ArgumentException("Telepen Numeric requires a nonempty even-length value.", nameof(value));
        var characters = new char[value.Length / 2];
        for (var i = 0; i < characters.Length; i++) {
            var first = value[i * 2] - '0';
            var second = value[i * 2 + 1];
            if ((uint)first > 9 || (second != 'X' && (uint)(second - '0') > 9))
                throw new ArgumentException("Telepen Numeric allows decimal digits and X in even positions.", nameof(value));
            characters[i] = (char)(second == 'X' ? first + 17 : first * 10 + second - '0' + 27);
        }
        var barcode = TelepenEncoder.Encode(new string(characters));
        return new Barcode1D(barcode.Segments, SymbolFormat.TelepenNumeric);
    }
}

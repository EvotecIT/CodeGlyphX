using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Ean;

internal static class EanAddOn {
    private static readonly bool[] AddOnStartPattern = { true, false, true, true };
    private static readonly bool[] AddOnSeparatorPattern = { false, true };

    private static readonly bool[][] AddOn2Parity = {
        new[] { false, false },
        new[] { false, true },
        new[] { true, false },
        new[] { true, true }
    };

    private static readonly bool[][] AddOn5Parity = {
        new[] { true, true, false, false, false },
        new[] { true, false, true, false, false },
        new[] { true, false, false, true, false },
        new[] { true, false, false, false, true },
        new[] { false, true, true, false, false },
        new[] { false, false, true, true, false },
        new[] { false, false, false, true, true },
        new[] { false, true, false, true, false },
        new[] { false, true, false, false, true },
        new[] { false, false, true, false, true }
    };

    internal static void AppendAddOn(List<BarSegment> segments, string value) {
        if (segments is null) throw new ArgumentNullException(nameof(segments));
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (!RegexCache.DigitsRequired().IsMatch(value)) throw new InvalidOperationException("Add-on must contain only digits (0-9).");

        if (!TryGetParityPattern(value.AsSpan(), out var parity)) {
            throw new InvalidOperationException("Add-on length must be 2 or 5 digits.");
        }

        BarcodeSegments.AppendBit(segments, isBar: false);
        BarcodeSegments.AppendBits(segments, AddOnStartPattern);

        for (var i = 0; i < value.Length; i++) {
            var encoded = EanTables.EncodingTable[value[i]];
            var bits = parity[i] ? encoded.LeftEven : encoded.LeftOdd;
            BarcodeSegments.AppendBits(segments, bits);
            if (i + 1 < value.Length) {
                BarcodeSegments.AppendBits(segments, AddOnSeparatorPattern);
            }
        }
    }

    internal static bool TryGetParityPattern(ReadOnlySpan<char> digits, out bool[] parity) {
        parity = Array.Empty<bool>();
        if (digits.Length == 2) {
            var value = (digits[0] - '0') * 10 + (digits[1] - '0');
            if ((uint)value > 99) return false;
            parity = AddOn2Parity[value % 4];
            return true;
        }
        if (digits.Length == 5) {
            var checksum = CalcAddOn5Checksum(digits);
            if (checksum < 0) return false;
            parity = AddOn5Parity[checksum];
            return true;
        }
        return false;
    }

    internal static bool ValidateParity(ReadOnlySpan<char> digits, ReadOnlySpan<bool> parity) {
        if (!TryGetParityPattern(digits, out var expected)) return false;
        if (parity.Length != expected.Length) return false;
        for (var i = 0; i < expected.Length; i++) {
            if (parity[i] != expected[i]) return false;
        }
        return true;
    }

    private static int CalcAddOn5Checksum(ReadOnlySpan<char> digits) {
        if (digits.Length != 5) return -1;
        var sum = 0;
        for (var i = 0; i < digits.Length; i++) {
            var digit = digits[i] - '0';
            if ((uint)digit > 9) return -1;
            sum += (i % 2 == 0 ? 3 : 9) * digit;
        }
        return sum % 10;
    }
}

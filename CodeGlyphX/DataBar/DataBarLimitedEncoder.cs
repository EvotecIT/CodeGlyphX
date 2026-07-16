// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Encodes GS1 DataBar Limited symbols.
/// </summary>
public static class DataBarLimitedEncoder {
    /// <summary>
    /// Encodes a GS1 DataBar Limited symbol into a <see cref="Barcode1D"/>.
    /// </summary>
    /// <param name="content">
    /// A value from 0 through 1999999999999, optionally zero-padded to 13 digits. A valid GTIN-14 check
    /// digit and the optional <c>01</c>, <c>(01)</c>, or <c>[01]</c> application identifier are accepted.
    /// </param>
    public static Barcode1D Encode(string content) {
        var value = ParseContent(content);
        var leftValue = (int)(value / DataBarLimitedTables.PairDivisor);
        var rightValue = (int)(value % DataBarLimitedTables.PairDivisor);
        var leftWidths = EncodePair(leftValue);
        var rightWidths = EncodePair(rightValue);
        var checksum = (ComputePairChecksum(leftWidths) + 20 * ComputePairChecksum(rightWidths)) % 89;
        var finderWidths = PatternToWidths(DataBarLimitedTables.CheckPatterns[checksum]);

        var totalWidths = new int[47];
        totalWidths[0] = 1;
        totalWidths[1] = 1;
        Array.Copy(leftWidths, 0, totalWidths, 2, 14);
        Array.Copy(finderWidths, 0, totalWidths, 16, 14);
        Array.Copy(rightWidths, 0, totalWidths, 30, 14);
        totalWidths[44] = 1;
        totalWidths[45] = 1;
        totalWidths[46] = 5;

        var segments = new List<BarSegment>(totalWidths.Length);
        var isBar = false;
        for (var i = 0; i < totalWidths.Length; i++) {
            segments.Add(new BarSegment(isBar, totalWidths[i]));
            isBar = !isBar;
        }
        return new Barcode1D(segments);
    }

    private static int[] EncodePair(int pairValue) {
        var group = FindGroup(ref pairValue);
        var oddValue = pairValue / DataBarLimitedTables.EvenTable[group];
        var evenValue = pairValue % DataBarLimitedTables.EvenTable[group];
        var oddWidths = DataBarCommon.GetWidths(oddValue, DataBarLimitedTables.OddModules[group], 7,
            DataBarLimitedTables.OddWidest[group], noNarrow: 1);
        var evenWidths = DataBarCommon.GetWidths(evenValue, 26 - DataBarLimitedTables.OddModules[group], 7,
            9 - DataBarLimitedTables.OddWidest[group], noNarrow: 0);
        var widths = new int[14];
        for (var i = 0; i < 7; i++) {
            widths[i * 2] = oddWidths[i];
            widths[i * 2 + 1] = evenWidths[i];
        }
        return widths;
    }

    private static int FindGroup(ref int value) {
        for (var group = DataBarLimitedTables.GroupSum.Length - 1; group > 0; group--) {
            if (value < DataBarLimitedTables.GroupSum[group]) continue;
            value -= DataBarLimitedTables.GroupSum[group];
            return group;
        }
        return 0;
    }

    internal static int ComputePairChecksum(ReadOnlySpan<int> widths) {
        var checksum = 0;
        for (var i = widths.Length - 1; i >= 0; i--) {
            checksum = 3 * checksum + widths[i];
        }
        return checksum;
    }

    internal static int[] PatternToWidths(int pattern) {
        var widths = new int[14];
        var widthIndex = 0;
        var current = 1;
        var count = 0;
        for (var bitIndex = 17; bitIndex >= 0; bitIndex--) {
            var bit = (pattern >> bitIndex) & 1;
            if (bit == current) {
                count++;
                continue;
            }
            if (count == 0 || widthIndex >= widths.Length) {
                throw new InvalidOperationException("Invalid GS1 DataBar Limited check-character pattern.");
            }
            widths[widthIndex++] = count;
            current = bit;
            count = 1;
        }
        if (widthIndex != widths.Length - 1 || count == 0) {
            throw new InvalidOperationException("Invalid GS1 DataBar Limited check-character pattern.");
        }
        widths[widthIndex] = count;
        return widths;
    }

    private static long ParseContent(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if ((content.StartsWith("(01)", StringComparison.Ordinal) || content.StartsWith("[01]", StringComparison.Ordinal)) &&
            (content.Length == 17 || content.Length == 18)) {
            content = content.Substring(4);
        } else if (content.StartsWith("01", StringComparison.Ordinal) && (content.Length == 15 || content.Length == 16)) {
            content = content.Substring(2);
        }

        if (content.Length == 0 || content.Length > 14) {
            throw new InvalidOperationException("GS1 DataBar Limited expects 1 to 13 digits, optionally followed by a GTIN check digit.");
        }
        for (var i = 0; i < content.Length; i++) {
            if (content[i] < '0' || content[i] > '9') {
                throw new InvalidOperationException("GS1 DataBar Limited expects numeric digits only.");
            }
        }

        if (content.Length == 14) {
            var expected = ComputeGtinCheckDigit(content, 13);
            if (content[13] - '0' != expected) {
                throw new InvalidOperationException($"Invalid GTIN check digit; expected {expected}.");
            }
            content = content.Substring(0, 13);
        }

        var value = long.Parse(content);
        if (value > DataBarLimitedTables.MaximumSymbolValue) {
            throw new InvalidOperationException("GS1 DataBar Limited value must be between 0 and 1999999999999.");
        }
        return value;
    }

    private static int ComputeGtinCheckDigit(string digits, int length) {
        var sum = 0;
        for (var i = 0; i < length; i++) {
            var digit = digits[i] - '0';
            sum += digit * (((length - i) & 1) == 1 ? 3 : 1);
        }
        return (10 - sum % 10) % 10;
    }
}

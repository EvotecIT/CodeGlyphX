using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Code39;

/// <summary>
/// Encodes Code 39 barcodes.
/// </summary>
public static class Code39Encoder {
    /// <summary>
    /// Encodes a Code 39 barcode.
    /// </summary>
    public static Barcode1D Encode(string content, bool includeChecksum = true, bool fullAsciiMode = false) {
        if (content is null) throw new ArgumentNullException(nameof(content));

        if (fullAsciiMode) {
            content = Prepare(content);
        } else if (content.Contains("*")) {
            throw new InvalidOperationException("Invalid data! Try full ASCII mode");
        }

        var checksumChar = GetChecksum(content);
        var sb = new StringBuilder("*");
        sb.Append(content);
        if (includeChecksum) sb.Append(checksumChar);
        sb.Append('*');

        var segments = new List<BarSegment>(sb.Length * 12);
        var first = true;
        foreach (var ch in sb.ToString()) {
            if (!first) {
                BarcodeSegments.AppendBit(segments, false); // inter-character narrow space
            }
            first = false;

            if (!Code39Tables.EncodingTable.TryGetValue(ch, out var entry)) {
                throw new InvalidOperationException("Invalid data! Try full ASCII mode");
            }
            BarcodeSegments.AppendBits(segments, entry.data);
        }

        return new Barcode1D(segments);
    }

    private static string Prepare(string content) {
        var sb = new StringBuilder();
        foreach (var ch in content) {
            if (ch > '\u007F') throw new InvalidOperationException("Only ASCII strings can be encoded");
            if (Code39Tables.ExtendedTable.TryGetValue(ch, out var mapped)) sb.Append(mapped);
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    private static char GetChecksum(string content) {
        var sum = 0;
        foreach (var ch in content) {
            if (!Code39Tables.EncodingTable.TryGetValue(ch, out var entry) || entry.value < 0) {
                return '#';
            }
            sum += entry.value;
        }
        sum %= 43;
        foreach (var kvp in Code39Tables.EncodingTable) {
            if (kvp.Value.value == sum) return kvp.Key;
        }
        return '#';
    }
}

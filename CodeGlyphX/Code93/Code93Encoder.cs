using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Code93;

/// <summary>
/// Encodes Code 93 barcodes.
/// </summary>
public static class Code93Encoder {
    /// <summary>
    /// Encodes a Code 93 barcode.
    /// </summary>
    public static Barcode1D Encode(string content, bool includeChecksum = true, bool fullAsciiMode = false) {
        if (content is null) throw new ArgumentNullException(nameof(content));

        if (fullAsciiMode) {
            content = Prepare(content);
        } else if (content.Contains("*")) {
            throw new InvalidOperationException("Invalid data! Try full ASCII mode");
        }

        var body = content;
        if (includeChecksum) {
            body += Code93Tables.GetChecksum(body.AsSpan(), 20);
            body += Code93Tables.GetChecksum(body.AsSpan(), 15);
        }

        var encoded = "*" + body + "*";

        var segments = new List<BarSegment>(encoded.Length * 9 + 1);
        foreach (var ch in encoded) {
            if (!Code93Tables.EncodingTable.TryGetValue(ch, out var entry)) {
                throw new InvalidOperationException("Invalid data");
            }
            BarcodeSegments.AppendBits(segments, entry.data, 9);
        }

        // Termination bar.
        BarcodeSegments.AppendBit(segments, true);
        return new Barcode1D(segments, SymbolFormat.Code93);
    }

    private static string Prepare(string content) {
        var sb = new StringBuilder();
        foreach (var ch in content) {
            if (ch > '\u007F') throw new InvalidOperationException("Only ASCII strings can be encoded");
            sb.Append(Code93Tables.ExtendedTable[ch]);
        }
        return sb.ToString();
    }

}

using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Msi;

/// <summary>
/// Encodes MSI (Modified Plessey) barcodes.
/// </summary>
public static class MsiEncoder {
    /// <summary>
    /// Encodes an MSI barcode.
    /// </summary>
    public static Barcode1D Encode(string content, MsiChecksumType checksum = MsiChecksumType.Mod10) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.Length == 0) throw new InvalidOperationException("MSI content cannot be empty.");

        for (var i = 0; i < content.Length; i++) {
            if ((uint)(content[i] - '0') > 9) {
                throw new InvalidOperationException("MSI supports digits only.");
            }
        }

        var data = MsiChecks.Append(content, checksum);

        var segments = new List<BarSegment>(data.Length * 12 + 16);
        AppendPattern(segments, MsiTables.Start);
        for (var i = 0; i < data.Length; i++) {
            if (!MsiTables.DigitPatterns.TryGetValue(data[i], out var pattern)) {
                throw new InvalidOperationException("MSI supports digits only.");
            }
            AppendPattern(segments, pattern);
        }
        AppendPattern(segments, MsiTables.Stop);

        return new Barcode1D(segments, SymbolFormat.Msi);
    }

    private static void AppendPattern(List<BarSegment> segments, string pattern) {
        for (var i = 0; i < pattern.Length; i++) {
            BarcodeSegments.AppendBit(segments, pattern[i] == '1');
        }
    }

}

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
            if (!char.IsDigit(content[i])) {
                throw new InvalidOperationException("MSI supports digits only.");
            }
        }

        var data = content;
        switch (checksum) {
            case MsiChecksumType.None:
                break;
            case MsiChecksumType.Mod10:
                data += CalcMod10(data);
                break;
            case MsiChecksumType.Mod10Mod10:
                data += CalcMod10(data);
                data += CalcMod10(data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(checksum));
        }

        var segments = new List<BarSegment>(data.Length * 12 + 16);
        AppendPattern(segments, MsiTables.Start);
        for (var i = 0; i < data.Length; i++) {
            if (!MsiTables.DigitPatterns.TryGetValue(data[i], out var pattern)) {
                throw new InvalidOperationException("MSI supports digits only.");
            }
            AppendPattern(segments, pattern);
        }
        AppendPattern(segments, MsiTables.Stop);

        return new Barcode1D(segments);
    }

    private static void AppendPattern(List<BarSegment> segments, string pattern) {
        for (var i = 0; i < pattern.Length; i++) {
            BarcodeSegments.AppendBit(segments, pattern[i] == '1');
        }
    }

    private static char CalcMod10(string content) {
        var sum = 0;
        var doubleIt = true;
        for (var i = content.Length - 1; i >= 0; i--) {
            var digit = content[i] - '0';
            if (doubleIt) {
                digit *= 2;
                if (digit > 9) digit = (digit / 10) + (digit % 10);
            }
            sum += digit;
            doubleIt = !doubleIt;
        }
        var mod = sum % 10;
        var check = mod == 0 ? 0 : 10 - mod;
        return (char)('0' + check);
    }
}

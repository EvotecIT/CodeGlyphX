using System;
using System.Numerics;

namespace CodeGlyphX.Postal;

/// <summary>
/// Encodes USPS Intelligent Mail Barcodes (IMB).
/// </summary>
public static class UspsImbEncoder {
    /// <summary>
    /// Encodes a USPS IMB payload into a <see cref="BitMatrix"/>.
    /// </summary>
    /// <param name="content">Tracking code (20 digits) with optional routing code separated by '-'.</param>
    public static BitMatrix Encode(string content) {
        UspsImbHelpers.SplitContent(content, out var tracker, out var zip);

        var accum = UspsImbHelpers.BuildAccum(tracker, zip);
        var crc = UspsImbHelpers.ComputeCrc(accum);

        var codewords = new int[10];
        var xReg = accum;

        codewords[9] = (int)(xReg % 636);
        xReg /= 636;

        for (var i = 8; i >= 0; i--) {
            codewords[i] = (int)(xReg % 1365);
            xReg /= 1365;
        }

        codewords[9] *= 2;
        if (crc >= 1024) {
            codewords[0] += 659;
        }

        var characters = new int[10];
        for (var i = 0; i < 10; i++) {
            var cw = codewords[i];
            if (cw < 1287) {
                characters[i] = UspsImbTables.AppxDI[cw];
            } else {
                characters[i] = UspsImbTables.AppxDII[cw - 1287];
            }
        }

        for (var i = 0; i < 10; i++) {
            if ((crc & (1 << i)) != 0) {
                characters[i] = 0x1FFF - characters[i];
            }
        }

        var barMap = new bool[130];
        for (var i = 0; i < 10; i++) {
            var character = characters[i];
            for (var j = 0; j < 13; j++) {
                var idx = UspsImbTables.AppxDIV[(13 * i) + j] - 1;
                barMap[idx] = (character & (1 << j)) != 0;
            }
        }

        return BuildMatrix(barMap);
    }

    private static BitMatrix BuildMatrix(bool[] barMap) {
        var width = 65 * 2 - 1;
        var matrix = new BitMatrix(width, UspsImbTables.BarcodeHeight);

        var x = 0;
        for (var i = 0; i < 65; i++) {
            var lower = barMap[i];
            var upper = barMap[i + 65];
            SetBar(matrix, x, upper, lower);
            x += 2;
        }

        return matrix;
    }

    private static void SetBar(BitMatrix matrix, int x, bool ascender, bool descender) {
        matrix[x, 3] = true;
        matrix[x, 4] = true;

        if (descender) {
            matrix[x, 5] = true;
            matrix[x, 6] = true;
            matrix[x, 7] = true;
        }

        if (ascender) {
            matrix[x, 0] = true;
            matrix[x, 1] = true;
            matrix[x, 2] = true;
        }
    }
}

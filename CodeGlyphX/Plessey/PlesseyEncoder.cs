using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Plessey;

/// <summary>
/// Encodes Plessey barcodes.
/// </summary>
public static class PlesseyEncoder {
    /// <summary>
    /// Encodes a Plessey barcode (hex digits only).
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.Length == 0) throw new InvalidOperationException("Plessey content cannot be empty.");

        var bits = new List<bool>(content.Length * 4 + 16);
        for (var i = 0; i < content.Length; i++) {
            var ch = char.ToUpperInvariant(content[i]);
            if (!PlesseyTables.HexValues.TryGetValue(ch, out var value)) {
                throw new InvalidOperationException($"Invalid Plessey hex digit: '{content[i]}'.");
            }
            for (var b = 0; b < 4; b++) {
                bits.Add(((value >> b) & 1) != 0);
            }
        }

        var crc = CalcCrc(bits);
        for (var b = 0; b < 8; b++) {
            bits.Add(((crc >> b) & 1) != 0);
        }

        var segments = new List<BarSegment>(bits.Count * 4 + 32);
        AppendBits(segments, PlesseyTables.StartBits, reverse: false);
        for (var i = 0; i < bits.Count; i++) {
            AppendBitPair(segments, bits[i]);
        }
        AppendTerminationBar(segments);
        AppendBits(segments, PlesseyTables.StopBits, reverse: true);

        return new Barcode1D(segments);
    }

    private static void AppendBits(List<BarSegment> segments, string bits, bool reverse) {
        for (var i = 0; i < bits.Length; i++) {
            AppendBitPair(segments, bits[i] == '1', reverse);
        }
    }

    private static void AppendBitPair(List<BarSegment> segments, bool bit, bool reverse = false) {
        var wideBar = bit ? 2 : 1;
        var wideSpace = bit ? 1 : 2;
        if (!reverse) {
            AppendElements(segments, true, wideBar);
            AppendElements(segments, false, wideSpace);
        } else {
            AppendElements(segments, false, wideSpace);
            AppendElements(segments, true, wideBar);
        }
    }

    private static void AppendTerminationBar(List<BarSegment> segments) {
        AppendElements(segments, true, 2);
    }

    private static void AppendElements(List<BarSegment> segments, bool isBar, int width) {
        for (var i = 0; i < width; i++) {
            BarcodeSegments.AppendBit(segments, isBar);
        }
    }

    private static byte CalcCrc(List<bool> bits) {
        const int poly = 0x1E9;
        var crc = 0;
        for (var i = 0; i < bits.Count; i++) {
            crc = (crc << 1) | (bits[i] ? 1 : 0);
            if ((crc & 0x100) != 0) crc ^= poly;
        }
        for (var i = 0; i < 8; i++) {
            crc <<= 1;
            if ((crc & 0x100) != 0) crc ^= poly;
        }
        return (byte)(crc & 0xFF);
    }
}

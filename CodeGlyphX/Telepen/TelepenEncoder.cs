using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Telepen;

/// <summary>
/// Encodes Telepen barcodes (ASCII mode).
/// </summary>
public static class TelepenEncoder {
    /// <summary>
    /// Encodes a Telepen barcode. Supports ASCII characters in the 0-127 range.
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (content.Length == 0) throw new InvalidOperationException("Telepen content cannot be empty.");

        var values = TelepenTables.BuildValues(content);
        var bits = TelepenTables.BuildBitStream(values);
        var segments = new List<BarSegment>(bits.Count * 2);
        TelepenTables.AppendEncodedBits(segments, bits);
        return new Barcode1D(segments);
    }
}

internal static class TelepenTables {
    internal const byte StartValue = (byte)'_';
    internal const byte StopValue = (byte)'z';

    internal static List<byte> BuildValues(string content) {
        var data = new byte[content.Length];
        for (var i = 0; i < content.Length; i++) {
            var ch = content[i];
            if (ch > 127) throw new InvalidOperationException($"Telepen supports ASCII 0-127 only. Invalid character: '{ch}'.");
            data[i] = (byte)ch;
        }

        var checksum = CalcChecksum(data);
        var values = new List<byte>(data.Length + 3) {
            ApplyEvenParity(StartValue)
        };
        for (var i = 0; i < data.Length; i++) {
            values.Add(ApplyEvenParity(data[i]));
        }
        values.Add(ApplyEvenParity(checksum));
        values.Add(ApplyEvenParity(StopValue));
        return values;
    }

    internal static List<bool> BuildBitStream(List<byte> values) {
        var bits = new List<bool>(values.Count * 8);
        for (var i = 0; i < values.Count; i++) {
            var value = values[i];
            for (var b = 0; b < 8; b++) {
                bits.Add(((value >> b) & 1) != 0);
            }
        }
        return bits;
    }

    internal static void AppendEncodedBits(List<BarSegment> segments, List<bool> bits) {
        var index = 0;
        while (index < bits.Count) {
            if (bits[index]) {
                AppendPair(segments, narrowBar: true, wideSpace: false);
                index++;
                continue;
            }

            var next = index + 1;
            while (next < bits.Count && bits[next]) next++;
            if (next >= bits.Count) throw new InvalidOperationException("Telepen bit stream terminated unexpectedly.");

            var ones = next - index - 1;
            if (ones == 0) {
                AppendPair(segments, narrowBar: false, wideSpace: false); // 00
            } else if (ones == 1) {
                AppendPair(segments, narrowBar: false, wideSpace: true); // 010
            } else {
                AppendPair(segments, narrowBar: true, wideSpace: true); // 01
                for (var i = 0; i < ones - 2; i++) {
                    AppendPair(segments, narrowBar: true, wideSpace: false); // 1
                }
                AppendPair(segments, narrowBar: true, wideSpace: true); // 10
            }

            index = next + 1;
        }
    }

    internal static byte CalcChecksum(ReadOnlySpan<byte> data) {
        var sum = 0;
        for (var i = 0; i < data.Length; i++) sum += data[i];
        var mod = sum % 127;
        var check = (127 - mod) % 127;
        return (byte)check;
    }

    internal static byte CalcChecksum(byte[] data, int offset, int count) {
        var sum = 0;
        var end = offset + count;
        for (var i = offset; i < end; i++) sum += data[i];
        var mod = sum % 127;
        var check = (127 - mod) % 127;
        return (byte)check;
    }

    internal static bool HasEvenParity(byte value) => (CountBits(value) & 1) == 0;

    internal static byte ApplyEvenParity(byte value7) {
        var ones = CountBits(value7);
        if ((ones & 1) != 0) return (byte)(value7 | 0x80);
        return value7;
    }

    private static int CountBits(int value) {
        var count = 0;
        while (value != 0) {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }

    private static void AppendPair(List<BarSegment> segments, bool narrowBar, bool wideSpace) {
        AppendElements(segments, true, narrowBar ? 1 : 3);
        AppendElements(segments, false, wideSpace ? 3 : 1);
    }

    private static void AppendElements(List<BarSegment> segments, bool isBar, int width) {
        for (var i = 0; i < width; i++) {
            BarcodeSegments.AppendBit(segments, isBar);
        }
    }
}

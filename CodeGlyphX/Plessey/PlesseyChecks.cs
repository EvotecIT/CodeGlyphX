using System;

namespace CodeGlyphX.Plessey;

/// <summary>The Plessey polynomial remainder in on-symbol, least-significant-bit order.</summary>
internal static class PlesseyChecks {
    internal static byte Calculate(ReadOnlySpan<bool> bits) {
        var crc = 0;
        foreach (var bit in bits) {
            crc = (crc << 1) | (bit ? 1 : 0);
            if ((crc & 0x100) != 0) crc ^= 0x1E9;
        }
        for (var i = 0; i < 8; i++) {
            crc <<= 1;
            if ((crc & 0x100) != 0) crc ^= 0x1E9;
        }
        var result = 0;
        for (var i = 0; i < 8; i++) { result = (result << 1) | (crc & 1); crc >>= 1; }
        return (byte)result;
    }
}

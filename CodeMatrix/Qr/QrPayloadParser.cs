using System;
using System.Collections.Generic;

namespace CodeMatrix.Qr;

internal enum QrTextEncoding {
    Utf8 = 0,
    Latin1 = 1,
    Ascii = 2,
}

internal static class QrPayloadParser {
    private const string AlphanumericTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    public static bool TryParse(byte[] dataCodewords, int version, out byte[] payload, out QrTextEncoding encoding) {
        payload = null!;
        encoding = QrTextEncoding.Utf8;

        if (dataCodewords is null) return false;
        if (version is < 1 or > 40) return false;

        var bitLen = dataCodewords.Length * 8;
        var bitPos = 0;

        int ReadBits(int n) {
            if (n == 0) return 0;
            if (n < 0 || n > 31) throw new ArgumentOutOfRangeException(nameof(n));
            if (bitPos + n > bitLen) return -1;
            var val = 0;
            for (var i = 0; i < n; i++) {
                var b = (dataCodewords[(bitPos + i) >> 3] >> (7 - ((bitPos + i) & 7))) & 1;
                val = (val << 1) | b;
            }
            bitPos += n;
            return val;
        }

        var bytes = new List<byte>(64);

        while (true) {
            var mode = ReadBits(4);
            if (mode < 0) {
                // Some encoders omit an explicit terminator if the payload exactly fills the available space,
                // or leave <4 padding bits that can't form a mode indicator. Accept only zero padding.
                var remaining = bitLen - bitPos;
                if (remaining <= 0) break;
                if (remaining < 4 && AreRemainingBitsZero(dataCodewords, bitPos, bitLen)) break;
                return false;
            }

            if (mode == 0) {
                payload = bytes.Count == 0 ? Array.Empty<byte>() : bytes.ToArray();
                return true;
            }

            if (mode == 0b0100) {
                var countBits = QrTables.GetByteModeCharCountBits(version);
                var count = ReadBits(countBits);
                if (count < 0) return false;

                for (var i = 0; i < count; i++) {
                    var b = ReadBits(8);
                    if (b < 0) return false;
                    bytes.Add((byte)b);
                }

                continue;
            }

            if (mode == 0b0001) {
                // Numeric mode
                var countBits = QrTables.GetNumericModeCharCountBits(version);
                var count = ReadBits(countBits);
                if (count < 0) return false;

                var remaining = count;
                while (remaining >= 3) {
                    var v = ReadBits(10);
                    if (v < 0 || v > 999) return false;
                    bytes.Add((byte)('0' + (v / 100)));
                    bytes.Add((byte)('0' + ((v / 10) % 10)));
                    bytes.Add((byte)('0' + (v % 10)));
                    remaining -= 3;
                }

                if (remaining == 2) {
                    var v = ReadBits(7);
                    if (v < 0 || v > 99) return false;
                    bytes.Add((byte)('0' + (v / 10)));
                    bytes.Add((byte)('0' + (v % 10)));
                } else if (remaining == 1) {
                    var v = ReadBits(4);
                    if (v < 0 || v > 9) return false;
                    bytes.Add((byte)('0' + v));
                }

                continue;
            }

            if (mode == 0b0010) {
                // Alphanumeric mode
                var countBits = QrTables.GetAlphanumericModeCharCountBits(version);
                var count = ReadBits(countBits);
                if (count < 0) return false;

                var remaining = count;
                while (remaining >= 2) {
                    var v = ReadBits(11);
                    if (v < 0 || v >= 45 * 45) return false;
                    var a = v / 45;
                    var b = v % 45;
                    bytes.Add((byte)AlphanumericTable[a]);
                    bytes.Add((byte)AlphanumericTable[b]);
                    remaining -= 2;
                }

                if (remaining == 1) {
                    var v = ReadBits(6);
                    if (v < 0 || v >= 45) return false;
                    bytes.Add((byte)AlphanumericTable[v]);
                }

                continue;
            }

            if (mode == 0b0111) {
                if (!TryReadEciAssignmentNumber(ReadBits, out var assignmentNumber)) return false;

                // Minimal set for OTP / URL QR use cases.
                encoding = assignmentNumber switch {
                    3 => QrTextEncoding.Latin1,  // ISO-8859-1
                    26 => QrTextEncoding.Utf8,   // UTF-8
                    27 => QrTextEncoding.Ascii,  // US-ASCII
                    _ => encoding,
                };

                // Unknown ECI: accept but keep current encoding (best-effort).
                continue;
            }

            return false;
        }

        payload = bytes.Count == 0 ? Array.Empty<byte>() : bytes.ToArray();
        return true;
    }

    private static bool TryReadEciAssignmentNumber(Func<int, int> readBits, out int assignmentNumber) {
        assignmentNumber = 0;

        var first = readBits(8);
        if (first < 0) return false;

        // QR ECI encoding:
        // 0xxxxxxx: 7-bit value
        // 10xxxxxx xxxxxxxx: 14-bit value
        // 110xxxxx xxxxxxxx xxxxxxxx: 21-bit value
        if ((first & 0b1000_0000) == 0) {
            assignmentNumber = first & 0b0111_1111;
            return true;
        }

        if ((first & 0b1100_0000) == 0b1000_0000) {
            var second = readBits(8);
            if (second < 0) return false;
            assignmentNumber = ((first & 0b0011_1111) << 8) | second;
            return true;
        }

        if ((first & 0b1110_0000) == 0b1100_0000) {
            var rest = readBits(16);
            if (rest < 0) return false;
            assignmentNumber = ((first & 0b0001_1111) << 16) | rest;
            return true;
        }

        return false;
    }

    private static bool AreRemainingBitsZero(byte[] bytes, int bitPos, int bitLen) {
        for (var i = bitPos; i < bitLen; i++) {
            var b = (bytes[i >> 3] >> (7 - (i & 7))) & 1;
            if (b != 0) return false;
        }
        return true;
    }
}

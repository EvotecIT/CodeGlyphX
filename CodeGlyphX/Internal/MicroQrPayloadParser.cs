using System;
using System.Collections.Generic;
using CodeGlyphX;

namespace CodeGlyphX.Internal;

internal readonly struct MicroQrSegment {
    public QrTextEncoding Encoding { get; }
    public byte[] Bytes { get; }

    public MicroQrSegment(QrTextEncoding encoding, byte[] bytes) {
        Encoding = encoding;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }
}

internal static class MicroQrPayloadParser {
    private const string AlphanumericTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    public static bool TryParse(byte[] dataBytes, int dataBits, int version, out byte[] payload, out string text) {
        payload = null!;
        text = string.Empty;

        if (dataBytes is null) return false;
        if (version is < 1 or > 4) return false;
        if (dataBits < 0 || dataBits > dataBytes.Length * 8) return false;

        var bitPos = 0;

        int ReadBits(int n) {
            if (n == 0) return 0;
            if (n < 0 || n > 31) throw new ArgumentOutOfRangeException(nameof(n));
            if (bitPos + n > dataBits) return -1;
            var val = 0;
            for (var i = 0; i < n; i++) {
                var b = (dataBytes[(bitPos + i) >> 3] >> (7 - ((bitPos + i) & 7))) & 1;
                val = (val << 1) | b;
            }
            bitPos += n;
            return val;
        }

        var bytes = new List<byte>(64);
        var segmentBytes = new List<byte>(64);
        var segments = new List<MicroQrSegment>(4);
        var encoding = QrTextEncoding.Latin1;
        var modeBits = MicroQrTables.GetModeIndicatorBits(version);
        var terminatorBits = MicroQrTables.GetTerminatorBits(version);

        void AddByte(byte b) {
            bytes.Add(b);
            segmentBytes.Add(b);
        }

        void FlushSegment() {
            if (segmentBytes.Count == 0) return;
            segments.Add(new MicroQrSegment(encoding, segmentBytes.ToArray()));
            segmentBytes.Clear();
        }

        while (true) {
            var remaining = dataBits - bitPos;
            if (remaining <= 0) break;
            if (remaining < modeBits) {
                if (AreRemainingBitsZero(dataBytes, bitPos, dataBits)) break;
                return false;
            }

            if (remaining >= terminatorBits) {
                if (AreNextBitsZero(dataBytes, bitPos, terminatorBits)) {
                    bitPos += terminatorBits;
                    break;
                }
            } else {
                if (AreRemainingBitsZero(dataBytes, bitPos, dataBits)) break;
                return false;
            }

            var mode = modeBits == 0 ? (int)MicroQrMode.Numeric : ReadBits(modeBits);
            if (mode < 0) return false;
            if (mode is < 0 or > 3) return false;

            var countBits = MicroQrTables.GetLengthIndicatorBits((MicroQrMode)mode, version);
            if (countBits <= 0) return false;
            var count = ReadBits(countBits);
            if (count < 0) return false;

            if ((MicroQrMode)mode == MicroQrMode.Numeric) {
                var remainingDigits = count;
                while (remainingDigits >= 3) {
                    var v = ReadBits(10);
                    if (v < 0 || v > 999) return false;
                    AddByte((byte)('0' + (v / 100)));
                    AddByte((byte)('0' + ((v / 10) % 10)));
                    AddByte((byte)('0' + (v % 10)));
                    remainingDigits -= 3;
                }
                if (remainingDigits == 2) {
                    var v = ReadBits(7);
                    if (v < 0 || v > 99) return false;
                    AddByte((byte)('0' + (v / 10)));
                    AddByte((byte)('0' + (v % 10)));
                } else if (remainingDigits == 1) {
                    var v = ReadBits(4);
                    if (v < 0 || v > 9) return false;
                    AddByte((byte)('0' + v));
                }
                continue;
            }

            if ((MicroQrMode)mode == MicroQrMode.Alphanumeric) {
                var remainingChars = count;
                while (remainingChars >= 2) {
                    var v = ReadBits(11);
                    if (v < 0 || v >= 45 * 45) return false;
                    AddByte((byte)AlphanumericTable[v / 45]);
                    AddByte((byte)AlphanumericTable[v % 45]);
                    remainingChars -= 2;
                }
                if (remainingChars == 1) {
                    var v = ReadBits(6);
                    if (v < 0 || v >= 45) return false;
                    AddByte((byte)AlphanumericTable[v]);
                }
                continue;
            }

            if ((MicroQrMode)mode == MicroQrMode.Byte) {
                var previous = encoding;
                encoding = QrTextEncoding.Latin1;
                FlushSegment();
                for (var i = 0; i < count; i++) {
                    var b = ReadBits(8);
                    if (b < 0) return false;
                    AddByte((byte)b);
                }
                FlushSegment();
                encoding = previous;
                continue;
            }

            if ((MicroQrMode)mode == MicroQrMode.Kanji) {
                var previous = encoding;
                encoding = QrTextEncoding.ShiftJis;
                FlushSegment();
                for (var i = 0; i < count; i++) {
                    var v = ReadBits(13);
                    if (v < 0) return false;
                    var assembled = ((v / 0xC0) << 8) | (v % 0xC0);
                    var sjis = assembled < 0x1F00 ? assembled + 0x8140 : assembled + 0xC140;
                    AddByte((byte)(sjis >> 8));
                    AddByte((byte)sjis);
                }
                FlushSegment();
                encoding = previous;
                continue;
            }

            return false;
        }

        FlushSegment();
        payload = bytes.Count == 0 ? Array.Empty<byte>() : bytes.ToArray();
        if (segments.Count == 0) {
            text = string.Empty;
            return true;
        }

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < segments.Count; i++) {
            sb.Append(QrEncoding.Decode(segments[i].Encoding, segments[i].Bytes));
        }
        text = sb.ToString();
        return true;
    }

    private static bool AreRemainingBitsZero(byte[] bytes, int bitPos, int bitLen) {
        for (var i = bitPos; i < bitLen; i++) {
            var b = (bytes[i >> 3] >> (7 - (i & 7))) & 1;
            if (b != 0) return false;
        }
        return true;
    }

    private static bool AreNextBitsZero(byte[] bytes, int bitPos, int count) {
        for (var i = 0; i < count; i++) {
            var b = (bytes[(bitPos + i) >> 3] >> (7 - ((bitPos + i) & 7))) & 1;
            if (b != 0) return false;
        }
        return true;
    }
}

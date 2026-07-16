// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.RmQr;

internal static class RmQrPayloadParser {
    private const string AlphanumericTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    internal static bool TryParse(
        byte[] data,
        int version,
        out byte[] payload,
        out string text,
        out bool isGs1,
        out int? eciAssignmentNumber) {
        payload = Array.Empty<byte>();
        text = string.Empty;
        isGs1 = false;
        eciAssignmentNumber = null;
        var reader = new BitReader(data);
        var bytes = new List<byte>();
        var builder = new StringBuilder();
        var encoding = QrTextEncoding.Latin1;

        while (reader.Remaining >= 3) {
            var modeValue = reader.Read(3);
            if (modeValue < 0) return false;
            if (modeValue == 0) break;
            if (modeValue == 5) {
                if (isGs1 || bytes.Count != 0) return false;
                isGs1 = true;
                continue;
            }
            if (modeValue == 7) {
                if (!TryReadEci(ref reader, out var assignment)) return false;
                eciAssignmentNumber = assignment;
                if (TryMapEci(assignment, out var mapped)) encoding = mapped;
                continue;
            }
            if (modeValue is < 1 or > 4) return false;

            var mode = (RmQrMode)modeValue;
            var countBits = RmQrTables.GetCharacterCountBits(version, mode);
            var count = reader.Read(countBits);
            if (count < 0) return false;
            switch (mode) {
                case RmQrMode.Numeric:
                    if (!ReadNumeric(ref reader, count, bytes, builder)) return false;
                    break;
                case RmQrMode.Alphanumeric:
                    if (!ReadAlphanumeric(ref reader, count, isGs1, bytes, builder)) return false;
                    break;
                case RmQrMode.Byte:
                    if (!ReadBytes(ref reader, count, encoding, bytes, builder)) return false;
                    break;
                case RmQrMode.Kanji:
                    if (!ReadKanji(ref reader, count, bytes, builder)) return false;
                    break;
                default:
                    return false;
            }
        }

        payload = bytes.ToArray();
        text = builder.ToString();
        return true;
    }

    private static bool ReadNumeric(ref BitReader reader, int count, List<byte> bytes, StringBuilder text) {
        while (count >= 3) {
            var value = reader.Read(10);
            if (value is < 0 or > 999) return false;
            AppendAscii(bytes, text, (char)('0' + value / 100));
            AppendAscii(bytes, text, (char)('0' + value / 10 % 10));
            AppendAscii(bytes, text, (char)('0' + value % 10));
            count -= 3;
        }
        if (count == 2) {
            var value = reader.Read(7);
            if (value is < 0 or > 99) return false;
            AppendAscii(bytes, text, (char)('0' + value / 10));
            AppendAscii(bytes, text, (char)('0' + value % 10));
        } else if (count == 1) {
            var value = reader.Read(4);
            if (value is < 0 or > 9) return false;
            AppendAscii(bytes, text, (char)('0' + value));
        }
        return true;
    }

    private static bool ReadAlphanumeric(ref BitReader reader, int count, bool isGs1, List<byte> bytes, StringBuilder text) {
        var encoded = new StringBuilder(count);
        while (count >= 2) {
            var value = reader.Read(11);
            if (value < 0) return false;
            var first = value / 45;
            var second = value % 45;
            if (first >= 45 || second >= 45) return false;
            encoded.Append(AlphanumericTable[first]);
            encoded.Append(AlphanumericTable[second]);
            count -= 2;
        }
        if (count == 1) {
            var value = reader.Read(6);
            if (value is < 0 or >= 45) return false;
            encoded.Append(AlphanumericTable[value]);
        }

        for (var i = 0; i < encoded.Length; i++) {
            var character = encoded[i];
            if (isGs1 && character == '%') {
                if (i + 1 < encoded.Length && encoded[i + 1] == '%') {
                    AppendAscii(bytes, text, '%');
                    i++;
                } else {
                    bytes.Add(0x1D);
                    text.Append((char)0x1D);
                }
            } else {
                AppendAscii(bytes, text, character);
            }
        }
        return true;
    }

    private static bool ReadBytes(ref BitReader reader, int count, QrTextEncoding encoding, List<byte> payload, StringBuilder text) {
        if (count < 0 || reader.Remaining < count * 8L) return false;
        var segment = new byte[count];
        for (var i = 0; i < count; i++) {
            var value = reader.Read(8);
            if (value < 0) return false;
            segment[i] = (byte)value;
            payload.Add((byte)value);
        }
        text.Append(QrEncoding.Decode(encoding, segment));
        return true;
    }

    private static bool ReadKanji(ref BitReader reader, int count, List<byte> payload, StringBuilder text) {
        if (count < 0 || reader.Remaining < count * 13L) return false;
        var segment = new byte[count * 2];
        for (var i = 0; i < count; i++) {
            var value = reader.Read(13);
            if (value < 0) return false;
            var intermediate = (value / 0xC0 << 8) | value % 0xC0;
            var shiftJis = intermediate + (intermediate < 0x1F00 ? 0x8140 : 0xC140);
            segment[i * 2] = (byte)(shiftJis >> 8);
            segment[i * 2 + 1] = (byte)shiftJis;
            payload.Add(segment[i * 2]);
            payload.Add(segment[i * 2 + 1]);
        }
        text.Append(QrEncoding.Decode(QrTextEncoding.ShiftJis, segment));
        return true;
    }

    private static bool TryReadEci(ref BitReader reader, out int assignment) {
        assignment = 0;
        var first = reader.Read(8);
        if (first < 0) return false;
        if ((first & 0x80) == 0) {
            assignment = first;
            return true;
        }
        if ((first & 0xC0) == 0x80) {
            var second = reader.Read(8);
            if (second < 0) return false;
            assignment = ((first & 0x3F) << 8) | second;
            return true;
        }
        if ((first & 0xE0) == 0xC0) {
            var rest = reader.Read(16);
            if (rest < 0) return false;
            assignment = ((first & 0x1F) << 16) | rest;
            return assignment <= 999999;
        }
        return false;
    }

    private static bool TryMapEci(int assignment, out QrTextEncoding encoding) {
        switch (assignment) {
            case 3: encoding = QrTextEncoding.Latin1; return true;
            case 4: encoding = QrTextEncoding.Iso8859_2; return true;
            case 6: encoding = QrTextEncoding.Iso8859_4; return true;
            case 7: encoding = QrTextEncoding.Iso8859_5; return true;
            case 9: encoding = QrTextEncoding.Iso8859_7; return true;
            case 12: encoding = QrTextEncoding.Iso8859_10; return true;
            case 15: encoding = QrTextEncoding.Iso8859_15; return true;
            case 20: encoding = QrTextEncoding.ShiftJis; return true;
            case 26: encoding = QrTextEncoding.Utf8; return true;
            case 27: encoding = QrTextEncoding.Ascii; return true;
            default: encoding = QrTextEncoding.Latin1; return false;
        }
    }

    private static void AppendAscii(List<byte> bytes, StringBuilder text, char value) {
        bytes.Add((byte)value);
        text.Append(value);
    }

    private struct BitReader {
        private readonly byte[] _data;
        private int _position;

        internal BitReader(byte[] data) {
            _data = data;
            _position = 0;
        }

        internal int Remaining => _data.Length * 8 - _position;

        internal int Read(int count) {
            if (count is < 0 or > 31 || Remaining < count) return -1;
            var result = 0;
            for (var i = 0; i < count; i++) {
                result = result << 1 | ((_data[_position >> 3] >> (7 - (_position & 7))) & 1);
                _position++;
            }
            return result;
        }
    }
}

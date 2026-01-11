using System;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Internal;

internal static class MicroQrEncoder {
    public static MicroQrCode EncodeBytes(byte[] data, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return Encode(MicroQrMode.Byte, data, data.Length, ecc, minVersion, maxVersion, forceMask, null);
    }

    public static MicroQrCode EncodeNumeric(string digits, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask) {
        if (digits is null) throw new ArgumentNullException(nameof(digits));
        if (digits.Length == 0) throw new ArgumentException("Numeric payload is empty.", nameof(digits));
        for (var i = 0; i < digits.Length; i++) {
            if (digits[i] is < '0' or > '9') throw new ArgumentException("Numeric payload contains non-digit characters.", nameof(digits));
        }
        return Encode(MicroQrMode.Numeric, null, digits.Length, ecc, minVersion, maxVersion, forceMask, digits);
    }

    public static MicroQrCode EncodeAlphanumeric(string text, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (text.Length == 0) throw new ArgumentException("Alphanumeric payload is empty.", nameof(text));
        for (var i = 0; i < text.Length; i++) {
            if (QrPayloadParserIsAlphanumeric(text[i]) == false) {
                throw new ArgumentException("Alphanumeric payload contains unsupported characters.", nameof(text));
            }
        }
        return Encode(MicroQrMode.Alphanumeric, null, text.Length, ecc, minVersion, maxVersion, forceMask, text);
    }

    public static MicroQrCode EncodeKanji(string text, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (text.Length == 0) throw new ArgumentException("Kanji payload is empty.", nameof(text));
        var values = new ushort[text.Length];
        for (var i = 0; i < text.Length; i++) {
            if (!QrKanjiTable.TryGetValue(text[i], out var v)) {
                throw new ArgumentException("Text contains characters not encodable in Micro QR Kanji mode.", nameof(text));
            }
            values[i] = v;
        }
        return Encode(MicroQrMode.Kanji, null, values.Length, ecc, minVersion, maxVersion, forceMask, values);
    }

    private static MicroQrCode Encode(MicroQrMode mode, byte[]? bytes, int count, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask, object? payload) {
        if (minVersion is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(minVersion));
        if (maxVersion is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(maxVersion));
        if (minVersion > maxVersion) throw new ArgumentOutOfRangeException(nameof(minVersion));
        if (forceMask is not null && forceMask.Value is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(forceMask));

        var version = 0;
        for (var v = minVersion; v <= maxVersion; v++) {
            if (!MicroQrTables.IsSupported(v, ecc)) continue;
            var countBits = MicroQrTables.GetLengthIndicatorBits(mode, v);
            if (countBits == 0) continue;
            var requiredBits = MicroQrTables.GetModeIndicatorBits(v) + countBits + GetDataBits(mode, count, payload);
            if (requiredBits <= MicroQrTables.GetDataLengthBits(v, ecc)) {
                version = v;
                break;
            }
        }

        if (version == 0)
            throw new ArgumentException($"Data too long for Micro QR version range {minVersion}..{maxVersion} at ECC {ecc}.");

        var dataBits = MicroQrTables.GetDataLengthBits(version, ecc);
        var dataBytesLen = MicroQrTables.GetDataLengthBytes(version, ecc);
        var buffer = new MicroQrBitBuffer();

        var modeBits = MicroQrTables.GetModeIndicatorBits(version);
        if (modeBits > 0) buffer.AppendBits((int)mode, modeBits);

        var countBitsFinal = MicroQrTables.GetLengthIndicatorBits(mode, version);
        buffer.AppendBits(count, countBitsFinal);

        switch (mode) {
            case MicroQrMode.Numeric:
                AppendNumeric(buffer, (string)payload!);
                break;
            case MicroQrMode.Alphanumeric:
                AppendAlphanumeric(buffer, (string)payload!);
                break;
            case MicroQrMode.Byte:
                AppendBytes(buffer, bytes!);
                break;
            case MicroQrMode.Kanji:
                AppendKanji(buffer, (ushort[])payload!);
                break;
        }

        AppendPaddingMicro(buffer, version, ecc);
        if (buffer.LengthBits != dataBits) {
            throw new InvalidOperationException("Micro QR padding produced incorrect bit length.");
        }

        var dataBytes = buffer.ToByteArray();
        if (dataBytes.Length != dataBytesLen) {
            throw new InvalidOperationException("Micro QR data byte length mismatch.");
        }

        var eccLen = MicroQrTables.GetEccLength(version, ecc);
        var rsDiv = QrReedSolomon.ComputeDivisor(eccLen);
        var eccBytes = QrReedSolomon.ComputeRemainder(dataBytes, rsDiv);

        var size = MicroQrTables.GetWidth(version);
        var modules = new BitMatrix(size, size);
        var isFunction = new BitMatrix(size, size);
        DrawFunctionPatterns(version, modules, isFunction);
        DrawCodewords(dataBytes, dataBits, eccBytes, modules, isFunction);

        var bestMask = 0;
        BitMatrix? bestModules = null;
        var bestScore = int.MinValue;
        var startMask = forceMask ?? 0;
        var endMask = forceMask ?? 3;
        for (var mask = startMask; mask <= endMask; mask++) {
            var temp = modules.Clone();
            ApplyMask(mask, temp, isFunction);
            DrawFormatBits(mask, version, ecc, temp, isFunction);
            var score = MicroQrMask.EvaluateSymbol(temp);
            if (score > bestScore) {
                bestScore = score;
                bestMask = mask;
                bestModules = temp;
            }
        }

        if (bestModules is null) throw new InvalidOperationException("Failed to choose Micro QR mask.");
        return new MicroQrCode(version, ecc, bestMask, bestModules);
    }

    private static int GetDataBits(MicroQrMode mode, int count, object? payload) {
        return mode switch {
            MicroQrMode.Numeric => (count / 3) * 10 + (count % 3 == 1 ? 4 : (count % 3 == 2 ? 7 : 0)),
            MicroQrMode.Alphanumeric => (count / 2) * 11 + (count % 2 == 1 ? 6 : 0),
            MicroQrMode.Byte => count * 8,
            MicroQrMode.Kanji => count * 13,
            _ => 0
        };
    }

    private static void AppendNumeric(MicroQrBitBuffer buffer, string digits) {
        var i = 0;
        while (i + 2 < digits.Length) {
            var v = (digits[i] - '0') * 100 + (digits[i + 1] - '0') * 10 + (digits[i + 2] - '0');
            buffer.AppendBits(v, 10);
            i += 3;
        }
        if (i + 1 < digits.Length) {
            var v = (digits[i] - '0') * 10 + (digits[i + 1] - '0');
            buffer.AppendBits(v, 7);
        } else if (i < digits.Length) {
            buffer.AppendBits(digits[i] - '0', 4);
        }
    }

    private static void AppendAlphanumeric(MicroQrBitBuffer buffer, string text) {
        var i = 0;
        while (i + 1 < text.Length) {
            var v = QrPayloadParserAlphanumericValue(text[i]) * 45 + QrPayloadParserAlphanumericValue(text[i + 1]);
            buffer.AppendBits(v, 11);
            i += 2;
        }
        if (i < text.Length) {
            buffer.AppendBits(QrPayloadParserAlphanumericValue(text[i]), 6);
        }
    }

    private static void AppendBytes(MicroQrBitBuffer buffer, byte[] data) {
        for (var i = 0; i < data.Length; i++) buffer.AppendBits(data[i], 8);
    }

    private static void AppendKanji(MicroQrBitBuffer buffer, ushort[] values) {
        for (var i = 0; i < values.Length; i++) buffer.AppendBits(values[i], 13);
    }

    private static void AppendPaddingMicro(MicroQrBitBuffer buffer, int version, QrErrorCorrectionLevel ecc) {
        var bits = buffer.LengthBits;
        var maxBits = MicroQrTables.GetDataLengthBits(version, ecc);
        var maxWords = maxBits / 8;
        if (maxBits < bits) throw new ArgumentException("Input exceeds Micro QR capacity.");
        if (maxBits == bits) return;

        var termBits = MicroQrTables.GetTerminatorBits(version);
        if (maxBits - bits <= termBits) {
            buffer.AppendBits(0, maxBits - bits);
            return;
        }

        bits += termBits;
        var words = (bits + 7) / 8;
        if (maxBits - words * 8 > 0) {
            termBits += words * 8 - bits;
            if (words == maxWords) termBits += maxBits - words * 8;
        } else {
            termBits += words * 8 - bits;
        }
        if (termBits > 0) buffer.AppendBits(0, termBits);

        var padLen = maxWords - words;
        for (var i = 0; i < padLen; i++) {
            buffer.AppendBits((i & 1) == 1 ? 0x11 : 0xEC, 8);
        }

        termBits = maxBits - maxWords * 8;
        if (termBits > 0) buffer.AppendBits(0, termBits);
    }

    private static void DrawFunctionPatterns(int version, BitMatrix modules, BitMatrix isFunction) {
        DrawFinderPattern(modules, isFunction);
        DrawSeparator(modules, isFunction);
        DrawFormatArea(modules, isFunction);
        DrawTimingPattern(version, modules, isFunction);
    }

    private static void DrawFinderPattern(BitMatrix modules, BitMatrix isFunction) {
        for (var y = 0; y < 7; y++) {
            for (var x = 0; x < 7; x++) {
                var dark = x == 0 || x == 6 || y == 0 || y == 6 || (x is >= 2 and <= 4 && y is >= 2 and <= 4);
                SetFunctionModule(x, y, dark, modules, isFunction);
            }
        }
    }

    private static void DrawSeparator(BitMatrix modules, BitMatrix isFunction) {
        for (var y = 0; y < 7; y++) {
            SetFunctionModule(7, y, false, modules, isFunction);
        }
        for (var x = 0; x < 8; x++) {
            SetFunctionModule(x, 7, false, modules, isFunction);
        }
    }

    private static void DrawFormatArea(BitMatrix modules, BitMatrix isFunction) {
        for (var x = 1; x <= 8; x++) {
            SetFunctionModule(x, 8, false, modules, isFunction);
        }
        for (var y = 1; y <= 7; y++) {
            SetFunctionModule(8, y, false, modules, isFunction);
        }
    }

    private static void DrawTimingPattern(int version, BitMatrix modules, BitMatrix isFunction) {
        var size = MicroQrTables.GetWidth(version);
        for (var i = 1; i < size - 7; i++) {
            var x = 7 + i;
            var dark = (i & 1) == 1;
            SetFunctionModule(x, 0, dark, modules, isFunction);
            SetFunctionModule(0, x, dark, modules, isFunction);
        }
    }

    private static void DrawCodewords(byte[] dataBytes, int dataBits, byte[] eccBytes, BitMatrix modules, BitMatrix isFunction) {
        var filler = new MicroQrFrameFiller(modules.Width, isFunction);
        for (var i = 0; i < dataBits; i++) {
            var bit = ((dataBytes[i >> 3] >> (7 - (i & 7))) & 1) != 0;
            filler.PlaceNext(modules, bit);
        }
        for (var i = 0; i < eccBytes.Length * 8; i++) {
            var bit = ((eccBytes[i >> 3] >> (7 - (i & 7))) & 1) != 0;
            filler.PlaceNext(modules, bit);
        }
    }

    private static void ApplyMask(int mask, BitMatrix modules, BitMatrix isFunction) {
        var size = modules.Width;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (isFunction[x, y]) continue;
                if (MicroQrMask.ShouldInvert(mask, x, y)) modules[x, y] = !modules[x, y];
            }
        }
    }

    private static void DrawFormatBits(int mask, int version, QrErrorCorrectionLevel ecc, BitMatrix modules, BitMatrix isFunction) {
        var format = MicroQrTables.GetFormatInfo(mask, version, ecc);
        for (var i = 0; i < 8; i++) {
            var bit = ((format >> i) & 1) != 0;
            SetFunctionModule(8, i + 1, bit, modules, isFunction);
        }
        for (var i = 0; i < 7; i++) {
            var bit = ((format >> (8 + i)) & 1) != 0;
            SetFunctionModule(7 - i, 8, bit, modules, isFunction);
        }
    }

    private static void SetFunctionModule(int x, int y, bool isDark, BitMatrix modules, BitMatrix isFunction) {
        modules[x, y] = isDark;
        isFunction[x, y] = true;
    }

    private static bool QrPayloadParserIsAlphanumeric(char c) {
        return QrPayloadParserAlphanumericValue(c) >= 0;
    }

    private static int QrPayloadParserAlphanumericValue(char c) {
        const string table = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
        return table.IndexOf(c);
    }
}

internal sealed class MicroQrFrameFiller {
    private readonly int _width;
    private readonly BitMatrix _isFunction;
    private int _x;
    private int _y;
    private int _dir;
    private int _bit;

    public MicroQrFrameFiller(int width, BitMatrix isFunction) {
        _width = width;
        _isFunction = isFunction;
        _x = width - 1;
        _y = width - 1;
        _dir = -1;
        _bit = -1;
    }

    public void PlaceNext(BitMatrix modules, bool bit) {
        if (!TryNext(out var x, out var y)) throw new InvalidOperationException("Micro QR frame overflow.");
        modules[x, y] = bit;
    }

    public bool TryNext(out int x, out int y) {
        var pos = Next();
        if (pos is null) {
            x = 0;
            y = 0;
            return false;
        }
        x = pos.Value.x;
        y = pos.Value.y;
        return true;
    }

    public bool? ReadNext(BitMatrix modules) {
        if (!TryNext(out var x, out var y)) return null;
        return modules[x, y];
    }

    private (int x, int y)? Next() {
        if (_bit == -1) {
            _bit = 0;
            return (_x, _y);
        }

        var x = _x;
        var y = _y;

        if (_bit == 0) {
            x--;
            _bit++;
        } else {
            x++;
            y += _dir;
            _bit--;
        }

        if (_dir < 0) {
            if (y < 0) {
                y = 0;
                x -= 2;
                _dir = 1;
            }
        } else if (y == _width) {
            y = _width - 1;
            x -= 2;
            _dir = -1;
        }

        if (x < 0 || y < 0) return null;

        _x = x;
        _y = y;

        if (_isFunction[x, y]) {
            return Next();
        }

        return (x, y);
    }
}

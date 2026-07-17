using System;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX.RmQr;

internal sealed class RmQrSegment {
    private const string AlphanumericTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
    private readonly string? _text;
    private readonly byte[]? _bytes;
    private readonly ushort[]? _kanjiValues;

    private RmQrSegment(RmQrMode mode, int characterCount, string? text, byte[]? bytes, ushort[]? kanjiValues) {
        Mode = mode;
        CharacterCount = characterCount;
        _text = text;
        _bytes = bytes;
        _kanjiValues = kanjiValues;
    }

    internal RmQrMode Mode { get; }
    internal int CharacterCount { get; }

    internal static RmQrSegment CreateNumeric(string text) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        for (var i = 0; i < text.Length; i++) {
            if (text[i] is < '0' or > '9') throw new ArgumentException("Numeric rMQR segments accept digits only.", nameof(text));
        }
        return new RmQrSegment(RmQrMode.Numeric, text.Length, text, null, null);
    }

    internal static RmQrSegment CreateAlphanumeric(string text) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        for (var i = 0; i < text.Length; i++) {
            if (AlphanumericTable.IndexOf(text[i]) < 0) {
                throw new ArgumentException("Alphanumeric rMQR segments contain an unsupported character.", nameof(text));
            }
        }
        return new RmQrSegment(RmQrMode.Alphanumeric, text.Length, text, null, null);
    }

    internal static RmQrSegment CreateBytes(byte[] bytes) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        return new RmQrSegment(RmQrMode.Byte, bytes.Length, null, bytes, null);
    }

    internal static RmQrSegment CreateKanji(string text) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var values = new ushort[text.Length];
        for (var i = 0; i < text.Length; i++) {
            if (!QrKanjiTable.TryGetValue(text[i], out values[i])) {
                throw new ArgumentException("Text contains characters not encodable in rMQR Kanji mode.", nameof(text));
            }
        }
        return new RmQrSegment(RmQrMode.Kanji, values.Length, null, null, values);
    }

    internal static bool IsNumeric(string text) {
        if (text.Length == 0) return false;
        for (var i = 0; i < text.Length; i++) if (text[i] is < '0' or > '9') return false;
        return true;
    }

    internal static bool IsAlphanumeric(string text) {
        if (text.Length == 0) return false;
        for (var i = 0; i < text.Length; i++) if (AlphanumericTable.IndexOf(text[i]) < 0) return false;
        return true;
    }

    internal static bool IsKanji(string text) {
        if (text.Length == 0) return false;
        for (var i = 0; i < text.Length; i++) if (!QrKanjiTable.TryGetValue(text[i], out _)) return false;
        return true;
    }

    internal int GetBitLength(int version) {
        var countBits = RmQrTables.GetCharacterCountBits(version, Mode);
        if (CharacterCount >= (1 << countBits)) return int.MaxValue;
        return 3 + countBits + GetDataBitLength(Mode, CharacterCount);
    }

    internal void AppendTo(QrBitBuffer buffer, int version) {
        buffer.AppendBits((int)Mode, 3);
        buffer.AppendBits(CharacterCount, RmQrTables.GetCharacterCountBits(version, Mode));
        switch (Mode) {
            case RmQrMode.Numeric:
                AppendNumeric(buffer, _text!);
                break;
            case RmQrMode.Alphanumeric:
                AppendAlphanumeric(buffer, _text!);
                break;
            case RmQrMode.Byte:
                for (var i = 0; i < _bytes!.Length; i++) buffer.AppendBits(_bytes[i], 8);
                break;
            case RmQrMode.Kanji:
                for (var i = 0; i < _kanjiValues!.Length; i++) buffer.AppendBits(_kanjiValues[i], 13);
                break;
            default:
                throw new InvalidOperationException("Unsupported rMQR segment mode.");
        }
    }

    private static int GetDataBitLength(RmQrMode mode, int count) {
        return mode switch {
            RmQrMode.Numeric => count / 3 * 10 + (count % 3 == 1 ? 4 : count % 3 == 2 ? 7 : 0),
            RmQrMode.Alphanumeric => count / 2 * 11 + count % 2 * 6,
            RmQrMode.Byte => count * 8,
            RmQrMode.Kanji => count * 13,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static void AppendNumeric(QrBitBuffer buffer, string text) {
        var index = 0;
        while (index + 3 <= text.Length) {
            var value = (text[index] - '0') * 100 + (text[index + 1] - '0') * 10 + text[index + 2] - '0';
            buffer.AppendBits(value, 10);
            index += 3;
        }
        if (text.Length - index == 2) buffer.AppendBits((text[index] - '0') * 10 + text[index + 1] - '0', 7);
        else if (text.Length - index == 1) buffer.AppendBits(text[index] - '0', 4);
    }

    private static void AppendAlphanumeric(QrBitBuffer buffer, string text) {
        var index = 0;
        while (index + 2 <= text.Length) {
            buffer.AppendBits(AlphanumericTable.IndexOf(text[index]) * 45 + AlphanumericTable.IndexOf(text[index + 1]), 11);
            index += 2;
        }
        if (index < text.Length) buffer.AppendBits(AlphanumericTable.IndexOf(text[index]), 6);
    }
}

using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal enum QrSegmentMode {
    Numeric = 0b0001,
    Alphanumeric = 0b0010,
    Byte = 0b0100,
    Kanji = 0b1000
}

internal sealed class QrSegment {
    private const string AlphanumericTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
    private readonly string? _text;
    private readonly byte[]? _bytes;
    private readonly ushort[]? _kanjiValues;

    public QrSegmentMode Mode { get; }
    public int CharacterCount { get; }

    private QrSegment(QrSegmentMode mode, int characterCount, string? text, byte[]? bytes, ushort[]? kanjiValues) {
        Mode = mode;
        CharacterCount = characterCount;
        _text = text;
        _bytes = bytes;
        _kanjiValues = kanjiValues;
    }

    public static QrSegment CreateNumeric(string text) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        for (var i = 0; i < text.Length; i++) {
            if (text[i] is < '0' or > '9') throw new ArgumentException("Numeric QR segments accept digits only.", nameof(text));
        }
        return new QrSegment(QrSegmentMode.Numeric, text.Length, text, null, null);
    }

    public static QrSegment CreateAlphanumeric(string encodedText) {
        if (encodedText is null) throw new ArgumentNullException(nameof(encodedText));
        for (var i = 0; i < encodedText.Length; i++) {
            if (AlphanumericTable.IndexOf(encodedText[i]) < 0)
                throw new ArgumentException("Alphanumeric QR segment contains an unsupported character.", nameof(encodedText));
        }
        return new QrSegment(QrSegmentMode.Alphanumeric, encodedText.Length, encodedText, null, null);
    }

    public static QrSegment CreateByte(byte[] bytes) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        return new QrSegment(QrSegmentMode.Byte, bytes.Length, null, bytes, null);
    }

    public static QrSegment CreateKanji(string text) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var values = new ushort[text.Length];
        for (var i = 0; i < text.Length; i++) {
            if (!QrKanjiTable.TryGetValue(text[i], out values[i]))
                throw new ArgumentException("Text contains characters not encodable in QR Kanji mode.", nameof(text));
        }
        return new QrSegment(QrSegmentMode.Kanji, values.Length, null, null, values);
    }

    public int GetTotalBitLength(int version) {
        return 4 + GetCharacterCountBitLength(version) + GetDataBitLength();
    }

    public void AppendTo(QrBitBuffer buffer, int version) {
        buffer.AppendBits((int)Mode, 4);
        buffer.AppendBits(CharacterCount, GetCharacterCountBitLength(version));

        switch (Mode) {
            case QrSegmentMode.Numeric:
                AppendNumeric(buffer, _text!);
                break;
            case QrSegmentMode.Alphanumeric:
                AppendAlphanumeric(buffer, _text!);
                break;
            case QrSegmentMode.Byte:
                for (var i = 0; i < _bytes!.Length; i++) buffer.AppendBits(_bytes[i], 8);
                break;
            case QrSegmentMode.Kanji:
                for (var i = 0; i < _kanjiValues!.Length; i++) buffer.AppendBits(_kanjiValues[i], 13);
                break;
            default:
                throw new InvalidOperationException("Unsupported QR segment mode.");
        }
    }

    public static int GetDataBitLength(QrSegmentMode mode, int characterCount) {
        return mode switch {
            QrSegmentMode.Numeric => (characterCount / 3 * 10) + (characterCount % 3 == 1 ? 4 : characterCount % 3 == 2 ? 7 : 0),
            QrSegmentMode.Alphanumeric => (characterCount / 2 * 11) + (characterCount % 2 * 6),
            QrSegmentMode.Byte => characterCount * 8,
            QrSegmentMode.Kanji => characterCount * 13,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    public static int GetCharacterCountBitLength(QrSegmentMode mode, int version) {
        return mode switch {
            QrSegmentMode.Numeric => QrTables.GetNumericModeCharCountBits(version),
            QrSegmentMode.Alphanumeric => QrTables.GetAlphanumericModeCharCountBits(version),
            QrSegmentMode.Byte => QrTables.GetByteModeCharCountBits(version),
            QrSegmentMode.Kanji => QrTables.GetKanjiModeCharCountBits(version),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private int GetCharacterCountBitLength(int version) => GetCharacterCountBitLength(Mode, version);

    private int GetDataBitLength() => GetDataBitLength(Mode, CharacterCount);

    private static void AppendNumeric(QrBitBuffer buffer, string text) {
        var index = 0;
        while (index + 3 <= text.Length) {
            var value = (text[index] - '0') * 100 + (text[index + 1] - '0') * 10 + text[index + 2] - '0';
            buffer.AppendBits(value, 10);
            index += 3;
        }
        var remaining = text.Length - index;
        if (remaining == 2) {
            buffer.AppendBits((text[index] - '0') * 10 + text[index + 1] - '0', 7);
        } else if (remaining == 1) {
            buffer.AppendBits(text[index] - '0', 4);
        }
    }

    private static void AppendAlphanumeric(QrBitBuffer buffer, string text) {
        var index = 0;
        while (index + 2 <= text.Length) {
            var value = AlphanumericTable.IndexOf(text[index]) * 45 + AlphanumericTable.IndexOf(text[index + 1]);
            buffer.AppendBits(value, 11);
            index += 2;
        }
        if (index < text.Length) buffer.AppendBits(AlphanumericTable.IndexOf(text[index]), 6);
    }
}

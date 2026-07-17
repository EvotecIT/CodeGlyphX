using System;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.DataMatrix;

internal static class DataMatrixEciEncoding {
    private static readonly UnicodeEncoding Utf16BigEndianStrict = new UnicodeEncoding(
        bigEndian: true,
        byteOrderMark: false,
        throwOnInvalidBytes: true);

    public static byte[] Encode(string text, int assignmentNumber) {
        if (text is null) throw new ArgumentNullException(nameof(text));

        if (assignmentNumber == 25) {
            try {
                return Utf16BigEndianStrict.GetBytes(text);
            } catch (EncoderFallbackException exception) {
                throw new ArgumentException("Text contains malformed UTF-16 and cannot be encoded with ECI 25 (UTF-16BE).", nameof(text), exception);
            }
        }

        if (!TryGetQrEncoding(assignmentNumber, out var encoding)) {
            throw new ArgumentException(
                $"ECI assignment {assignmentNumber} is not supported for string payloads. Use EncodeBytes to supply bytes for custom ECI assignments.",
                nameof(assignmentNumber));
        }
        if (!QrEncoding.CanEncode(text, encoding)) {
            throw new ArgumentException(
                $"Text contains characters that cannot be represented by ECI assignment {assignmentNumber} ({encoding}).",
                nameof(text));
        }

        return QrEncoding.Encode(text, encoding);
    }

    public static bool TryDecode(byte[] bytes, int count, int assignmentNumber, out string text) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (count < 0 || count > bytes.Length) throw new ArgumentOutOfRangeException(nameof(count));

        try {
            if (assignmentNumber == 25) {
                text = Utf16BigEndianStrict.GetString(bytes, 0, count);
                return true;
            }
            if (assignmentNumber == 26) {
                text = EncodingUtils.Utf8Strict.GetString(bytes, 0, count);
                return true;
            }
            if (TryGetQrEncoding(assignmentNumber, out var encoding)) {
                text = QrEncoding.Decode(encoding, bytes, 0, count);
                return true;
            }
        } catch (DecoderFallbackException) {
            // Preserve the decoder's best-effort fallback for malformed or mislabeled payloads.
        }

        text = string.Empty;
        return false;
    }

    private static bool TryGetQrEncoding(int assignmentNumber, out QrTextEncoding encoding) {
        encoding = assignmentNumber switch {
            3 => QrTextEncoding.Latin1,
            4 => QrTextEncoding.Iso8859_2,
            6 => QrTextEncoding.Iso8859_4,
            7 => QrTextEncoding.Iso8859_5,
            9 => QrTextEncoding.Iso8859_7,
            12 => QrTextEncoding.Iso8859_10,
            15 => QrTextEncoding.Iso8859_15,
            20 => QrTextEncoding.ShiftJis,
            26 => QrTextEncoding.Utf8,
            27 => QrTextEncoding.Ascii,
            _ => default
        };
        return assignmentNumber is 3 or 4 or 6 or 7 or 9 or 12 or 15 or 20 or 26 or 27;
    }
}

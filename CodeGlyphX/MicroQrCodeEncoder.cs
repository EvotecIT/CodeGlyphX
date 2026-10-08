using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>
/// Encodes Micro QR codes (M1..M4).
/// </summary>
public static class MicroQrCodeEncoder {
    /// <summary>
    /// Encodes a byte payload as a Micro QR code.
    /// </summary>
    public static MicroQrCode EncodeBytes(
        byte[] data,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.L,
        int minVersion = 1,
        int maxVersion = 4,
        int? forceMask = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return MicroQrEncoder.EncodeBytes(data, ecc, minVersion, maxVersion, forceMask);
    }

    /// <summary>
    /// Encodes text in Micro QR byte mode. The selected encoding must preserve Latin-1 text;
    /// Micro QR has no ECI charset declaration. Use <see cref="EncodeKanji"/> for Kanji text.
    /// </summary>
    public static MicroQrCode EncodeText(
        string text,
        QrTextEncoding encoding = QrTextEncoding.Latin1,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.L,
        int minVersion = 1,
        int maxVersion = 4,
        int? forceMask = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (!QrEncoding.CanEncode(text, encoding)) {
            throw new ArgumentException($"Text cannot be represented by {encoding}.", nameof(text));
        }
        var data = QrEncoding.Encode(text, encoding);
        if (!string.Equals(QrEncoding.Decode(QrTextEncoding.Latin1, data), text, StringComparison.Ordinal)) {
            throw new ArgumentException("Micro QR byte mode has no ECI declaration and decodes as Latin-1. Use EncodeKanji for Kanji text or EncodeBytes for an explicitly managed byte payload.", nameof(encoding));
        }
        return MicroQrEncoder.EncodeBytes(data, ecc, minVersion, maxVersion, forceMask);
    }

    /// <summary>
    /// Encodes numeric text as a Micro QR code (numeric mode).
    /// </summary>
    public static MicroQrCode EncodeNumeric(
        string digits,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.L,
        int minVersion = 1,
        int maxVersion = 4,
        int? forceMask = null) {
        return MicroQrEncoder.EncodeNumeric(digits, ecc, minVersion, maxVersion, forceMask);
    }

    /// <summary>
    /// Encodes alphanumeric text as a Micro QR code (alphanumeric mode).
    /// </summary>
    public static MicroQrCode EncodeAlphanumeric(
        string text,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.L,
        int minVersion = 1,
        int maxVersion = 4,
        int? forceMask = null) {
        return MicroQrEncoder.EncodeAlphanumeric(text, ecc, minVersion, maxVersion, forceMask);
    }

    /// <summary>
    /// Encodes Kanji text as a Micro QR code (Kanji mode).
    /// </summary>
    public static MicroQrCode EncodeKanji(
        string text,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.L,
        int minVersion = 1,
        int maxVersion = 4,
        int? forceMask = null) {
        return MicroQrEncoder.EncodeKanji(text, ecc, minVersion, maxVersion, forceMask);
    }
}

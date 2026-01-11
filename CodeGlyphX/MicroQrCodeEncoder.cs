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
    /// Encodes a text payload as a Micro QR code (byte mode, using a QR text encoding).
    /// </summary>
    public static MicroQrCode EncodeText(
        string text,
        QrTextEncoding encoding = QrTextEncoding.Latin1,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.L,
        int minVersion = 1,
        int maxVersion = 4,
        int? forceMask = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var data = QrEncoding.Encode(text, encoding);
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

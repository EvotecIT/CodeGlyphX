using System;
using CodeGlyphX.Gs1Data;
using CodeGlyphX.RmQr;

namespace CodeGlyphX;

/// <summary>
/// Encodes rectangular Micro QR (rMQR) symbols defined by ISO/IEC 23941:2022.
/// </summary>
public static class RmQrCodeEncoder {
    /// <summary>Encodes text using automatic whole-payload mode selection.</summary>
    public static RmQrCode EncodeText(string text, RmQrEncodingOptions? options = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        return RmQrEncoder.EncodeText(text, options ?? new RmQrEncodingOptions());
    }

    /// <summary>Encodes an arbitrary binary payload in byte mode.</summary>
    public static RmQrCode EncodeBytes(byte[] data, RmQrEncodingOptions? options = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return RmQrEncoder.EncodeBytes(data, options ?? new RmQrEncodingOptions { Mode = RmQrEncodingMode.Byte });
    }

    /// <summary>Encodes decimal digits in numeric mode.</summary>
    public static RmQrCode EncodeNumeric(
        string digits,
        QrErrorCorrectionLevel errorCorrectionLevel = QrErrorCorrectionLevel.M,
        int minimumVersion = 1,
        int maximumVersion = 32) {
        if (digits is null) throw new ArgumentNullException(nameof(digits));
        return EncodeText(digits, CreateOptions(RmQrEncodingMode.Numeric, errorCorrectionLevel, minimumVersion, maximumVersion));
    }

    /// <summary>Encodes the QR alphanumeric character set.</summary>
    public static RmQrCode EncodeAlphanumeric(
        string text,
        QrErrorCorrectionLevel errorCorrectionLevel = QrErrorCorrectionLevel.M,
        int minimumVersion = 1,
        int maximumVersion = 32) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        return EncodeText(text, CreateOptions(RmQrEncodingMode.Alphanumeric, errorCorrectionLevel, minimumVersion, maximumVersion));
    }

    /// <summary>Encodes Shift-JIS double-byte characters in Kanji mode.</summary>
    public static RmQrCode EncodeKanji(
        string text,
        QrErrorCorrectionLevel errorCorrectionLevel = QrErrorCorrectionLevel.M,
        int minimumVersion = 1,
        int maximumVersion = 32) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        return EncodeText(text, CreateOptions(RmQrEncodingMode.Kanji, errorCorrectionLevel, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Validates a bracketed or raw GS1 message and encodes its element string with FNC1 in first position.
    /// </summary>
    public static RmQrCode EncodeGs1(
        string gs1Message,
        QrErrorCorrectionLevel errorCorrectionLevel = QrErrorCorrectionLevel.M,
        int minimumVersion = 1,
        int maximumVersion = 32) {
        if (gs1Message is null) throw new ArgumentNullException(nameof(gs1Message));
        var elementString = Gs1Validator.ToElementString(gs1Message);
        return EncodeText(elementString, new RmQrEncodingOptions {
            ErrorCorrectionLevel = errorCorrectionLevel,
            Mode = RmQrEncodingMode.Byte,
            TextEncoding = QrTextEncoding.Latin1,
            IsGs1 = true,
            MinimumVersion = minimumVersion,
            MaximumVersion = maximumVersion
        });
    }

    private static RmQrEncodingOptions CreateOptions(
        RmQrEncodingMode mode,
        QrErrorCorrectionLevel ecc,
        int minimumVersion,
        int maximumVersion) {
        return new RmQrEncodingOptions {
            Mode = mode,
            ErrorCorrectionLevel = ecc,
            MinimumVersion = minimumVersion,
            MaximumVersion = maximumVersion
        };
    }
}

using System;
using System.Text;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX;

/// <summary>
/// Encodes QR codes (UTF-8 byte mode).
/// </summary>
/// <remarks>
/// This encoder currently supports byte mode only (sufficient for URLs and <c>otpauth://</c> payloads).
/// </remarks>
public static class QrCodeEncoder {
    /// <summary>
    /// Encodes a UTF-8 text payload as a QR code.
    /// </summary>
    /// <param name="text">Text payload to encode.</param>
    /// <param name="ecc">Error correction level.</param>
    /// <param name="minVersion">Minimum allowed QR version (1..40).</param>
    /// <param name="maxVersion">Maximum allowed QR version (1..40).</param>
    /// <param name="forceMask">Optional forced mask (0..7). When null, the best mask is chosen.</param>
    public static QrCode EncodeText(
        string text,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 40,
        int? forceMask = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var data = Encoding.UTF8.GetBytes(text);
        return EncodeBytes(data, ecc, minVersion, maxVersion, forceMask);
    }

    /// <summary>
    /// Encodes a text payload using a specific QR text encoding (optionally with ECI).
    /// </summary>
    /// <param name="text">Text payload to encode.</param>
    /// <param name="encoding">Encoding to use for QR byte mode.</param>
    /// <param name="ecc">Error correction level.</param>
    /// <param name="minVersion">Minimum allowed QR version (1..40).</param>
    /// <param name="maxVersion">Maximum allowed QR version (1..40).</param>
    /// <param name="forceMask">Optional forced mask (0..7). When null, the best mask is chosen.</param>
    /// <param name="includeEci">When true, emits an ECI header for non-default encodings.</param>
    public static QrCode EncodeText(
        string text,
        QrTextEncoding encoding,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 40,
        int? forceMask = null,
        bool includeEci = true) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var data = QrEncoding.Encode(text, encoding);
        int? eci = null;
        if (includeEci && encoding != QrTextEncoding.Latin1 && QrEncoding.TryGetEciAssignment(encoding, out var assignment)) {
            eci = assignment;
        }
        return QrEncoder.EncodeByteMode(data, ecc, minVersion, maxVersion, forceMask, eci);
    }

    /// <summary>
    /// Encodes an arbitrary byte payload (QR byte mode).
    /// </summary>
    /// <param name="data">Bytes to encode.</param>
    /// <param name="ecc">Error correction level.</param>
    /// <param name="minVersion">Minimum allowed QR version (1..40).</param>
    /// <param name="maxVersion">Maximum allowed QR version (1..40).</param>
    /// <param name="forceMask">Optional forced mask (0..7). When null, the best mask is chosen.</param>
    /// <param name="eciAssignmentNumber">Optional ECI assignment number to emit before the payload.</param>
    public static QrCode EncodeBytes(
        byte[] data,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 40,
        int? forceMask = null,
        int? eciAssignmentNumber = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return QrEncoder.EncodeByteMode(data, ecc, minVersion, maxVersion, forceMask, eciAssignmentNumber);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes an arbitrary byte payload (QR byte mode).
    /// </summary>
    /// <param name="data">Bytes to encode.</param>
    /// <param name="ecc">Error correction level.</param>
    /// <param name="minVersion">Minimum allowed QR version (1..40).</param>
    /// <param name="maxVersion">Maximum allowed QR version (1..40).</param>
    /// <param name="forceMask">Optional forced mask (0..7). When null, the best mask is chosen.</param>
    /// <param name="eciAssignmentNumber">Optional ECI assignment number to emit before the payload.</param>
    public static QrCode EncodeBytes(
        ReadOnlySpan<byte> data,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 40,
        int? forceMask = null,
        int? eciAssignmentNumber = null) {
        return QrEncoder.EncodeByteMode(data.ToArray(), ecc, minVersion, maxVersion, forceMask, eciAssignmentNumber);
    }
#endif
}

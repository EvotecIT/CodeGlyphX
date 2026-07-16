using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX;

/// <summary>
/// Encodes standards-aware QR codes.
/// </summary>
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
        return EncodeText(text, new QrEncodingOptions {
            ErrorCorrectionLevel = ecc,
            MinVersion = minVersion,
            MaxVersion = maxVersion,
            ForceMask = forceMask,
            TextEncoding = QrTextEncoding.Utf8,
            EciMode = QrEciMode.Auto,
            OptimizeSegments = true
        });
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
        return EncodeText(text, new QrEncodingOptions {
            ErrorCorrectionLevel = ecc,
            MinVersion = minVersion,
            MaxVersion = maxVersion,
            ForceMask = forceMask,
            TextEncoding = encoding,
            EciMode = includeEci && encoding != QrTextEncoding.Latin1 ? QrEciMode.Always : QrEciMode.Never,
            OptimizeSegments = true
        });
    }

    /// <summary>
    /// Encodes text using standards-aware segment optimization and the supplied options.
    /// </summary>
    /// <param name="text">Text payload to encode.</param>
    /// <param name="options">Encoding, version, ECI, FNC1, and optimization options.</param>
    public static QrCode EncodeText(string text, QrEncodingOptions options) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (options is null) throw new ArgumentNullException(nameof(options));
        return QrEncoder.EncodeText(text, options);
    }

    /// <summary>
    /// Encodes a GS1 element string using FNC1 in first position.
    /// </summary>
    /// <remarks>
    /// Use ASCII group separator (<c>\u001D</c>) between variable-length element strings.
    /// Literal percent signs are escaped automatically when an alphanumeric segment is selected.
    /// </remarks>
    /// <param name="elementString">GS1 element string without human-readable parentheses.</param>
    /// <param name="options">Optional QR encoding options. The FNC1 mode is set to first position.</param>
    public static QrCode EncodeGs1(string elementString, QrEncodingOptions? options = null) {
        if (elementString is null) throw new ArgumentNullException(nameof(elementString));
        var effective = options?.Clone() ?? new QrEncodingOptions {
            TextEncoding = QrTextEncoding.Latin1,
            EciMode = QrEciMode.Never
        };
        effective.Fnc1Mode = QrFnc1Mode.FirstPosition;
        effective.Fnc1ApplicationIndicator = null;
        return QrEncoder.EncodeText(elementString, effective);
    }

    /// <summary>
    /// Encodes two through sixteen pre-split text parts as a QR structured-append sequence.
    /// </summary>
    /// <remarks>
    /// All symbols receive the standard zero-based sequence indicator on the wire and the XOR parity
    /// of the complete byte payload. Supplying parts explicitly keeps application record boundaries intact.
    /// </remarks>
    /// <param name="parts">Ordered payload parts (2..16).</param>
    /// <param name="options">Optional encoding options shared by every symbol.</param>
    public static QrCode[] EncodeStructuredAppend(IReadOnlyList<string> parts, QrEncodingOptions? options = null) {
        if (parts is null) throw new ArgumentNullException(nameof(parts));
        if (parts.Count is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(parts), "Structured append requires two through sixteen parts.");
        var effective = options?.Clone() ?? new QrEncodingOptions();

        var encodedParts = new byte[parts.Count][];
        var parity = 0;
        for (var i = 0; i < parts.Count; i++) {
            if (parts[i] is null) throw new ArgumentException("Structured append parts cannot contain null values.", nameof(parts));
            if (!QrEncoding.CanEncode(parts[i], effective.TextEncoding))
                throw new ArgumentException($"Part {i + 1} cannot be encoded as {effective.TextEncoding}.", nameof(parts));
            encodedParts[i] = QrEncoding.Encode(parts[i], effective.TextEncoding);
            for (var j = 0; j < encodedParts[i].Length; j++) parity ^= encodedParts[i][j];
        }

        var result = new QrCode[parts.Count];
        for (var i = 0; i < parts.Count; i++) {
            result[i] = QrEncoder.EncodeText(parts[i], effective, new QrStructuredAppend(i + 1, parts.Count, parity));
        }
        return result;
    }

    /// <summary>
    /// Encodes two through sixteen pre-split binary parts as a QR structured-append sequence.
    /// </summary>
    /// <param name="parts">Ordered binary payload parts (2..16).</param>
    /// <param name="ecc">Error correction level shared by every symbol.</param>
    /// <param name="minVersion">Minimum allowed QR version (1..40).</param>
    /// <param name="maxVersion">Maximum allowed QR version (1..40).</param>
    /// <param name="forceMask">Optional forced mask (0..7). When null, the best mask is chosen per symbol.</param>
    /// <param name="eciAssignmentNumber">Optional ECI assignment number emitted before each byte payload.</param>
    public static QrCode[] EncodeStructuredAppend(
        IReadOnlyList<byte[]> parts,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 40,
        int? forceMask = null,
        int? eciAssignmentNumber = null) {
        if (parts is null) throw new ArgumentNullException(nameof(parts));
        if (parts.Count is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(parts), "Structured append requires two through sixteen parts.");

        var parity = 0;
        for (var i = 0; i < parts.Count; i++) {
            if (parts[i] is null) throw new ArgumentException("Structured append parts cannot contain null values.", nameof(parts));
            for (var j = 0; j < parts[i].Length; j++) parity ^= parts[i][j];
        }

        var result = new QrCode[parts.Count];
        for (var i = 0; i < parts.Count; i++) {
            result[i] = QrEncoder.EncodeByteMode(
                parts[i],
                ecc,
                minVersion,
                maxVersion,
                forceMask,
                eciAssignmentNumber,
                new QrStructuredAppend(i + 1, parts.Count, parity));
        }
        return result;
    }

    /// <summary>
    /// Encodes a QR Kanji-mode payload (Shift-JIS JIS X 0208).
    /// </summary>
    /// <param name="text">Kanji text payload to encode.</param>
    /// <param name="ecc">Error correction level.</param>
    /// <param name="minVersion">Minimum allowed QR version (1..40).</param>
    /// <param name="maxVersion">Maximum allowed QR version (1..40).</param>
    /// <param name="forceMask">Optional forced mask (0..7). When null, the best mask is chosen.</param>
    public static QrCode EncodeKanji(
        string text,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 40,
        int? forceMask = null) {
        return QrEncoder.EncodeKanjiMode(text, ecc, minVersion, maxVersion, forceMask);
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

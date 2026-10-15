using System;

namespace CodeGlyphX;

/// <summary>
/// Result of decoding a QR code payload.
/// </summary>
public sealed class QrDecoded {
    /// <summary>
    /// Gets the decoded QR version (1..40).
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the decoded error correction level.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>
    /// Gets the decoded mask pattern (0..7).
    /// </summary>
    public int Mask { get; }

    /// <summary>
    /// Gets the decoded payload bytes.
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    /// Gets the decoded payload interpreted as text (ISO-8859-1 by default; respects ECI per segment).
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Structured append metadata when the payload participates in a multi-symbol sequence.
    /// </summary>
    public QrStructuredAppend? StructuredAppend { get; }

    /// <summary>
    /// Indicates whether the payload uses FNC1 (GS1) mode.
    /// </summary>
    public QrFnc1Mode Fnc1Mode { get; }

    /// <summary>
    /// Gets the 8-bit application indicator when FNC1 is used in second position.
    /// </summary>
    public int? Fnc1ApplicationIndicator { get; }

    /// <summary>
    /// Gets the parsed payload when recognized (URL, WiFi, contact, etc.).
    /// </summary>
    public Payloads.QrParsedPayload Parsed { get; }

    internal QrDecoded(int version, QrErrorCorrectionLevel ecc, int mask, byte[] bytes, string text, QrStructuredAppend? structuredAppend, QrFnc1Mode fnc1Mode, int? fnc1ApplicationIndicator) {
        Version = version;
        ErrorCorrectionLevel = ecc;
        Mask = mask;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        if (text is null) throw new ArgumentNullException(nameof(text));
        // Swiss payment data declares UTF-8 in its SPC header without a QR ECI.
        // Accept that interpretation only after its complete payment fields validate.
        var swissText = string.Empty;
        var isSwiss = fnc1Mode == QrFnc1Mode.None && structuredAppend is null
            && Payloads.SwissQrCodePayload.TryParseBytes(bytes, out _, out swissText);
        Text = isSwiss ? swissText : text;
        StructuredAppend = structuredAppend;
        Fnc1Mode = fnc1Mode;
        Fnc1ApplicationIndicator = fnc1ApplicationIndicator;
        if (!Payloads.QrPayloadParser.TryParse(Text, out var parsed)) {
            parsed = new Payloads.QrParsedPayload(Payloads.QrPayloadType.Text, Text, Text);
        }
        Parsed = parsed;
    }
}

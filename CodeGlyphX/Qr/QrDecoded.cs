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
    /// Gets the parsed payload when recognized (URL, WiFi, contact, etc.).
    /// </summary>
    public Payloads.QrParsedPayload Parsed { get; }

    internal QrDecoded(int version, QrErrorCorrectionLevel ecc, int mask, byte[] bytes, string text) {
        Version = version;
        ErrorCorrectionLevel = ecc;
        Mask = mask;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        if (!Payloads.QrPayloadParser.TryParse(text, out var parsed)) {
            parsed = new Payloads.QrParsedPayload(Payloads.QrPayloadType.Text, text, text);
        }
        Parsed = parsed;
    }
}

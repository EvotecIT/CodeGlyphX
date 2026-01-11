using System;

namespace CodeGlyphX;

/// <summary>
/// Result of decoding a Micro QR code payload.
/// </summary>
public sealed class MicroQrDecoded {
    /// <summary>
    /// Gets the decoded Micro QR version (1..4).
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the decoded error correction level.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>
    /// Gets the decoded mask pattern (0..3).
    /// </summary>
    public int Mask { get; }

    /// <summary>
    /// Gets the decoded payload bytes.
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    /// Gets the decoded payload interpreted as text.
    /// </summary>
    public string Text { get; }

    internal MicroQrDecoded(int version, QrErrorCorrectionLevel ecc, int mask, byte[] bytes, string text) {
        Version = version;
        ErrorCorrectionLevel = ecc;
        Mask = mask;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}

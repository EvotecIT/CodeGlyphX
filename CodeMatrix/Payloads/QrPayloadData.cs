using System;
namespace CodeMatrix.Payloads;

/// <summary>
/// QR payload with optional encoding/version defaults.
/// </summary>
public sealed class QrPayloadData {
    /// <summary>
    /// Payload text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Optional error correction level recommendation.
    /// </summary>
    public QrErrorCorrectionLevel? ErrorCorrectionLevel { get; }

    /// <summary>
    /// Optional minimum QR version.
    /// </summary>
    public int? MinVersion { get; }

    /// <summary>
    /// Optional maximum QR version.
    /// </summary>
    public int? MaxVersion { get; }

    /// <summary>
    /// Optional text encoding recommendation.
    /// </summary>
    public QrTextEncoding? TextEncoding { get; }

    public QrPayloadData(
        string text,
        QrErrorCorrectionLevel? errorCorrectionLevel = null,
        int? minVersion = null,
        int? maxVersion = null,
        QrTextEncoding? textEncoding = null) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        ErrorCorrectionLevel = errorCorrectionLevel;
        MinVersion = minVersion;
        MaxVersion = maxVersion;
        TextEncoding = textEncoding;
    }

    public override string ToString() => Text;
}

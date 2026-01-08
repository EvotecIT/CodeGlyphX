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

    /// <summary>
    /// Creates a payload with optional encoding/version defaults.
    /// </summary>
    /// <param name="text">Payload text.</param>
    /// <param name="errorCorrectionLevel">Optional error correction level recommendation.</param>
    /// <param name="minVersion">Optional minimum QR version.</param>
    /// <param name="maxVersion">Optional maximum QR version.</param>
    /// <param name="textEncoding">Optional text encoding recommendation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
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

    /// <summary>
    /// Returns the payload text.
    /// </summary>
    /// <returns>Payload text.</returns>
    public override string ToString() => Text;
}

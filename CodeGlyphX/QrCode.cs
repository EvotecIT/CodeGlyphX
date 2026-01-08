using System;

namespace CodeGlyphX;

/// <summary>
/// A generated QR code (modules + metadata).
/// </summary>
public sealed class QrCode {
    /// <summary>
    /// Gets the QR version (1..40).
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the error correction level used for encoding.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>
    /// Gets the selected mask pattern (0..7).
    /// </summary>
    public int Mask { get; }

    /// <summary>
    /// Gets the QR modules (dark = <c>true</c>, light = <c>false</c>), without quiet zone.
    /// </summary>
    public BitMatrix Modules { get; }

    /// <summary>
    /// Gets the module matrix size (width/height), i.e. <c>Version * 4 + 17</c>.
    /// </summary>
    public int Size => Modules.Width;

    /// <summary>
    /// Creates a new <see cref="QrCode"/>.
    /// </summary>
    public QrCode(int version, QrErrorCorrectionLevel errorCorrectionLevel, int mask, BitMatrix modules) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (mask is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(mask));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));

        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
    }
}

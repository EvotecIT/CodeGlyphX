using System;

namespace CodeGlyphX;

/// <summary>
/// A generated Micro QR code (modules + metadata).
/// </summary>
public sealed class MicroQrCode : MatrixSymbol {
    /// <summary>
    /// Gets the Micro QR version (1..4).
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the error correction level used for encoding.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>
    /// Gets the selected mask pattern (0..3).
    /// </summary>
    public int Mask { get; }

    /// <summary>
    /// Gets the module matrix size (width/height), i.e. <c>Version * 2 + 9</c>.
    /// </summary>
    public int Size => Modules.Width;

    /// <summary>
    /// Creates a new <see cref="MicroQrCode"/>.
    /// </summary>
    public MicroQrCode(int version, QrErrorCorrectionLevel errorCorrectionLevel, int mask, BitMatrix modules)
        : base(SymbolFormat.MicroQrCode, modules) {
        if (version is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(version));
        if (mask is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(mask));
        if (!Internal.MicroQrTables.IsSupported(version, errorCorrectionLevel)) throw new ArgumentOutOfRangeException(nameof(errorCorrectionLevel), "The error correction level is not supported by this Micro QR version.");
        if (modules.Width != version * 2 + 9 || modules.Height != version * 2 + 9) throw new ArgumentException("Matrix width and height must equal version * 2 + 9.", nameof(modules));

        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
    }
}

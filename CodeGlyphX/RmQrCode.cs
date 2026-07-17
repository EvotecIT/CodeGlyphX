using System;

namespace CodeGlyphX;

/// <summary>
/// A generated rectangular Micro QR (rMQR) symbol and its metadata.
/// </summary>
public sealed class RmQrCode {
    /// <summary>Gets the ISO/IEC 23941 symbol version (1..32).</summary>
    public int Version { get; }

    /// <summary>Gets the human-readable version name, such as R11x27.</summary>
    public string VersionName { get; }

    /// <summary>Gets the error correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>Gets whether FNC1 in first position identifies a GS1 payload.</summary>
    public bool IsGs1 { get; }

    /// <summary>Gets the encoded modules without a quiet zone.</summary>
    public BitMatrix Modules { get; }

    /// <summary>Gets the symbol width in modules.</summary>
    public int Width => Modules.Width;

    /// <summary>Gets the symbol height in modules.</summary>
    public int Height => Modules.Height;

    internal RmQrCode(int version, string versionName, QrErrorCorrectionLevel ecc, bool isGs1, BitMatrix modules) {
        Version = version;
        VersionName = versionName ?? throw new ArgumentNullException(nameof(versionName));
        ErrorCorrectionLevel = ecc;
        IsGs1 = isGs1;
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }
}

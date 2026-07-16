namespace CodeGlyphX;

/// <summary>
/// Describes how a Micro QR symbol was recognized in an image.
/// </summary>
public sealed class MicroQrPixelDecodeInfo {
    /// <summary>Gets the detected symbol quadrilateral in source-image coordinates.</summary>
    public SymbolGeometry Geometry { get; }

    /// <summary>Gets whether light modules on a dark background were recognized.</summary>
    public bool IsInverted { get; }

    /// <summary>Gets whether the sampled symbol was mirrored.</summary>
    public bool IsMirrored { get; }

    /// <summary>Gets the grayscale threshold used for the successful recognition.</summary>
    public byte Threshold { get; }

    internal MicroQrPixelDecodeInfo(SymbolGeometry geometry, bool isInverted, bool isMirrored, byte threshold) {
        Geometry = geometry;
        IsInverted = isInverted;
        IsMirrored = isMirrored;
        Threshold = threshold;
    }
}

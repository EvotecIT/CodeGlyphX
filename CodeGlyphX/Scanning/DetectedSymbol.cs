using System;

namespace CodeGlyphX;

/// <summary>
/// Unified result returned by <see cref="SymbolScanner"/>.
/// </summary>
public sealed class DetectedSymbol {
    private readonly byte[]? _rawBytes;

    /// <summary>Gets the physical symbol format.</summary>
    public SymbolFormat Format { get; }
    /// <summary>Gets the decoded text.</summary>
    public string Text { get; }
    /// <summary>Gets exact payload bytes when the underlying decoder exposes them.</summary>
    public ReadOnlyMemory<byte> RawBytes => _rawBytes ?? ReadOnlyMemory<byte>.Empty;
    /// <summary>Gets whether <see cref="RawBytes"/> came from the underlying decoder.</summary>
    public bool HasRawBytes => _rawBytes is not null;
    /// <summary>Gets a recognized payload profile.</summary>
    public SymbolPayloadProfile PayloadProfile { get; }
    /// <summary>Gets the detected symbol geometry, or <see langword="null"/> when the decoder does not report location.</summary>
    public SymbolGeometry? Geometry { get; }
    /// <summary>Gets the image region searched for this result.</summary>
    public ImageRegion SearchRegion { get; }
    /// <summary>Gets decoder confidence in the range [0, 1], or <see langword="null"/> when unavailable.</summary>
    public double? Confidence { get; }
    /// <summary>Gets whether an inverted image was used, or <see langword="null"/> when unavailable.</summary>
    public bool? IsInverted { get; }
    /// <summary>Gets whether a mirrored image was used, or <see langword="null"/> when unavailable.</summary>
    public bool? IsMirrored { get; }
    /// <summary>Gets the animation frame index, or <see langword="null"/> for a single raw frame.</summary>
    public int? FrameIndex { get; }
    /// <summary>Gets the document page index, or <see langword="null"/> for a single raw frame.</summary>
    public int? PageIndex { get; }
    /// <summary>Gets the AIM symbology identifier when known.</summary>
    public string? SymbologyIdentifier { get; }
    /// <summary>Gets the compatibility result produced by the existing decoding facade.</summary>
    public CodeGlyphDecoded LegacyResult { get; }

    internal DetectedSymbol(
        SymbolFormat format,
        CodeGlyphDecoded legacyResult,
        ImageRegion searchRegion,
        SymbolGeometry? geometry = null,
        double? confidence = null,
        bool? isInverted = null,
        bool? isMirrored = null,
        int? frameIndex = null,
        int? pageIndex = null,
        string? symbologyIdentifier = null) {
        if (confidence.HasValue && (confidence.Value < 0d || confidence.Value > 1d || double.IsNaN(confidence.Value))) {
            throw new ArgumentOutOfRangeException(nameof(confidence));
        }
        LegacyResult = legacyResult ?? throw new ArgumentNullException(nameof(legacyResult));
        Format = format;
        Text = legacyResult.Text;
        _rawBytes = legacyResult.Bytes is null ? null : (byte[])legacyResult.Bytes.Clone();
        PayloadProfile = ResolvePayloadProfile(legacyResult);
        SearchRegion = searchRegion;
        Geometry = geometry;
        Confidence = confidence;
        IsInverted = isInverted;
        IsMirrored = isMirrored;
        FrameIndex = frameIndex;
        PageIndex = pageIndex;
        SymbologyIdentifier = symbologyIdentifier;
    }

    private static SymbolPayloadProfile ResolvePayloadProfile(CodeGlyphDecoded decoded) {
        if (decoded.Barcode?.Type == BarcodeType.GS1_128) return SymbolPayloadProfile.Gs1;
        if (decoded.Barcode?.Type == BarcodeType.GS1DataBarTruncated ||
            decoded.Barcode?.Type == BarcodeType.GS1DataBarOmni ||
            decoded.Barcode?.Type == BarcodeType.GS1DataBarStacked ||
            decoded.Barcode?.Type == BarcodeType.GS1DataBarExpanded ||
            decoded.Barcode?.Type == BarcodeType.GS1DataBarExpandedStacked) {
            return SymbolPayloadProfile.Gs1;
        }
        if (decoded.Qr?.Fnc1Mode != QrFnc1Mode.None) return SymbolPayloadProfile.Gs1;
        return SymbolPayloadProfile.None;
    }
}

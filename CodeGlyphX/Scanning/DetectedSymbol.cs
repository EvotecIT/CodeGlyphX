using System;

namespace CodeGlyphX;

/// <summary>A stable decoded payload with its physical format and available structural information.</summary>
public sealed class DetectedSymbol {
    private readonly byte[]? _rawBytes;
    /// <summary>Gets the physical symbol format.</summary>
    public SymbolFormat Format { get; }
    /// <summary>Gets decoded text using the decoder's encoding and control-mode interpretation.</summary>
    public string Text { get; }
    /// <summary>Gets decoded payload bytes when the decoder exposes them; these are not symbol codewords.</summary>
    public ReadOnlyMemory<byte> RawBytes => _rawBytes ?? ReadOnlyMemory<byte>.Empty;
    /// <summary>Gets whether exact decoded payload bytes are available, including an empty payload.</summary>
    public bool HasRawBytes => _rawBytes is not null;
    /// <summary>Gets typed structural information, or null when the decoder reports none.</summary>
    public SymbolMetadata? Metadata { get; }
    /// <summary>Gets the recognized payload profile.</summary>
    public SymbolPayloadProfile PayloadProfile { get; }
    /// <summary>Gets reported symbol geometry, or null when unavailable.</summary>
    public SymbolGeometry? Geometry { get; }
    /// <summary>Gets the searched source-image region, or null for module-level decoding.</summary>
    public ImageRegion? SearchRegion { get; }
    /// <summary>Gets decoder confidence from zero through one, or null when unavailable.</summary>
    public double? Confidence { get; }
    /// <summary>Gets whether inverted pixels were required, or null when unavailable.</summary>
    public bool? IsInverted { get; }
    /// <summary>Gets whether mirrored pixels were required, or null when unavailable.</summary>
    public bool? IsMirrored { get; }
    /// <summary>Gets the AIM symbology identifier when the decoder reports it.</summary>
    public string? SymbologyIdentifier { get; }
    /// <summary>Gets the direct-part-mark preprocessing profile when preprocessing was required.</summary>
    public DirectPartMarkProfile? DirectPartMarkProfile { get; }
    /// <summary>Gets whether direct-part-mark preprocessing was required.</summary>
    public bool WasDirectPartMarkPreprocessed => DirectPartMarkProfile.HasValue;

    internal DetectedSymbol(SymbolFormat format, string text, byte[]? rawBytes = null,
        SymbolMetadata? metadata = null, SymbolPayloadProfile payloadProfile = SymbolPayloadProfile.None,
        ImageRegion? searchRegion = null, SymbolGeometry? geometry = null, double? confidence = null,
        bool? isInverted = null, bool? isMirrored = null, string? symbologyIdentifier = null,
        DirectPartMarkProfile? directPartMarkProfile = null) {
        if (confidence.HasValue && (confidence.Value < 0 || confidence.Value > 1 || double.IsNaN(confidence.Value)))
            throw new ArgumentOutOfRangeException(nameof(confidence));
        Format = format;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _rawBytes = rawBytes is null ? null : (byte[])rawBytes.Clone();
        Metadata = metadata;
        PayloadProfile = payloadProfile;
        SearchRegion = searchRegion;
        Geometry = geometry;
        Confidence = confidence;
        IsInverted = isInverted;
        IsMirrored = isMirrored;
        SymbologyIdentifier = symbologyIdentifier;
        DirectPartMarkProfile = directPartMarkProfile;
    }
}

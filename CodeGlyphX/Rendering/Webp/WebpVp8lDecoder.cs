using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8L lossless decoder scaffold (work in progress).
/// </summary>
internal static class WebpVp8lDecoder {
    private const int TransformSubtractGreen = 2;

    /// <summary>
    /// Attempts to decode a VP8L payload. Currently parses only the header and a
    /// small subset of flags, then returns <c>false</c> so native fallback can run.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> payload, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        var reader = new WebpBitReader(payload);
        if (!TryReadHeader(ref reader, out var header)) return false;

        width = header.Width;
        height = header.Height;

        // Color cache flag (1 bit). We parse it for roadmap progress, but do not
        // decode the entropy-coded image yet.
        var colorCacheFlag = reader.ReadBits(1);
        if (colorCacheFlag < 0) return false;
        var colorCacheBits = 0;
        if (colorCacheFlag != 0) {
            // color_cache_code_bits is 4 bits in the lossless bitstream.
            colorCacheBits = reader.ReadBits(4);
            if (colorCacheBits is < 1 or > 11) return false;
        }
        var colorCacheSize = colorCacheBits == 0 ? 0 : 1 << colorCacheBits;

        // Meta prefix codes flag (1 bit). A value of 1 indicates an entropy image
        // we cannot parse yet without full prefix-code support.
        var metaPrefixFlag = reader.ReadBits(1);
        if (metaPrefixFlag != 0) return false;

        var literalAlphabetSize = 256;
        var greenAlphabetSize = literalAlphabetSize + 24 + colorCacheSize;
        if (!TryReadPrefixCodesGroup(ref reader, greenAlphabetSize, literalAlphabetSize, out _)) return false;

        return false;
    }

    internal static bool TryReadHeader(ReadOnlySpan<byte> payload, out WebpVp8lHeader header) {
        var reader = new WebpBitReader(payload);
        return TryReadHeader(ref reader, out header);
    }

    internal static bool TryReadHeader(ref WebpBitReader reader, out WebpVp8lHeader header) {
        header = default;

        var signature = reader.ReadBits(8);
        if (signature != 0x2F) return false;

        var widthMinus1 = reader.ReadBits(14);
        var heightMinus1 = reader.ReadBits(14);
        var alphaUsed = reader.ReadBits(1);
        var version = reader.ReadBits(3);

        if (widthMinus1 < 0 || heightMinus1 < 0 || alphaUsed < 0 || version < 0) return false;
        if (version != 0) return false;

        var width = widthMinus1 + 1;
        var height = heightMinus1 + 1;
        if (width <= 0 || height <= 0) return false;

        var transformMask = 0;
        while (true) {
            var hasTransform = reader.ReadBits(1);
            if (hasTransform < 0) return false;
            if (hasTransform == 0) break;

            var transformType = reader.ReadBits(2);
            if (transformType < 0) return false;

            var bit = 1 << transformType;
            if ((transformMask & bit) != 0) return false;
            transformMask |= bit;

            // Only subtract-green has no additional transform data. Any other
            // transform would require decoding an embedded image to continue.
            if (transformType != TransformSubtractGreen) return false;
        }

        header = new WebpVp8lHeader(
            width,
            height,
            alphaUsed != 0,
            transformMask,
            reader.BitsConsumed);
        return true;
    }

    private static bool TryReadPrefixCodesGroup(
        ref WebpBitReader reader,
        int greenAlphabetSize,
        int literalAlphabetSize,
        out WebpPrefixCodesGroup group) {
        group = default;

        if (!WebpPrefixCodeReader.TryReadPrefixCode(ref reader, greenAlphabetSize, out var green)) return false;
        if (!WebpPrefixCodeReader.TryReadPrefixCode(ref reader, literalAlphabetSize, out var red)) return false;
        if (!WebpPrefixCodeReader.TryReadPrefixCode(ref reader, literalAlphabetSize, out var blue)) return false;
        if (!WebpPrefixCodeReader.TryReadPrefixCode(ref reader, literalAlphabetSize, out var alpha)) return false;
        if (!WebpPrefixCodeReader.TryReadPrefixCode(ref reader, alphabetSize: 40, out var distance)) return false;

        group = new WebpPrefixCodesGroup(green, red, blue, alpha, distance);
        return true;
    }
}

internal readonly struct WebpVp8lHeader {
    public WebpVp8lHeader(int width, int height, bool alphaUsed, int transformMask, int bitsConsumed) {
        Width = width;
        Height = height;
        AlphaUsed = alphaUsed;
        TransformMask = transformMask;
        BitsConsumed = bitsConsumed;
    }

    public int Width { get; }
    public int Height { get; }
    public bool AlphaUsed { get; }
    public int TransformMask { get; }
    public int BitsConsumed { get; }

    public bool SubtractGreenTransformUsed => (TransformMask & (1 << 2)) != 0;
}

internal readonly struct WebpPrefixCodesGroup {
    public WebpPrefixCodesGroup(
        WebpPrefixCode green,
        WebpPrefixCode red,
        WebpPrefixCode blue,
        WebpPrefixCode alpha,
        WebpPrefixCode distance) {
        Green = green;
        Red = red;
        Blue = blue;
        Alpha = alpha;
        Distance = distance;
    }

    public WebpPrefixCode Green { get; }
    public WebpPrefixCode Red { get; }
    public WebpPrefixCode Blue { get; }
    public WebpPrefixCode Alpha { get; }
    public WebpPrefixCode Distance { get; }
}

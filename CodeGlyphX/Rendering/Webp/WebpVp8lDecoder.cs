using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8L lossless decoder scaffold (work in progress).
/// </summary>
internal static class WebpVp8lDecoder {
    private const int TransformSubtractGreen = 2;
    private const int LiteralAlphabetSize = 256;
    private const int LengthPrefixCount = 24;
    private const int GreenAlphabetBase = LiteralAlphabetSize + LengthPrefixCount;
    private const int MaxBackwardLength = 4096;
    private const int DistanceMapSize = 120;
    private const uint ColorCacheHashMultiplier = 0x1e35a7bd;

    private static readonly (int xi, int yi)[] DistanceMap = {
        (0, 1), (1, 0), (1, 1), (-1, 1), (0, 2), (2, 0), (1, 2), (-1, 2),
        (2, 1), (-2, 1), (2, 2), (-2, 2), (0, 3), (3, 0), (1, 3), (-1, 3),
        (3, 1), (-3, 1), (2, 3), (-2, 3), (3, 2), (-3, 2), (0, 4), (4, 0),
        (1, 4), (-1, 4), (4, 1), (-4, 1), (3, 3), (-3, 3), (2, 4), (-2, 4),
        (4, 2), (-4, 2), (0, 5), (3, 4), (-3, 4), (4, 3), (-4, 3), (5, 0),
        (1, 5), (-1, 5), (5, 1), (-5, 1), (2, 5), (-2, 5), (5, 2), (-5, 2),
        (4, 4), (-4, 4), (3, 5), (-3, 5), (5, 3), (-5, 3), (0, 6), (6, 0),
        (1, 6), (-1, 6), (6, 1), (-6, 1), (2, 6), (-2, 6), (6, 2), (-6, 2),
        (4, 5), (-4, 5), (5, 4), (-5, 4), (3, 6), (-3, 6), (6, 3), (-6, 3),
        (0, 7), (7, 0), (1, 7), (-1, 7), (5, 5), (-5, 5), (7, 1), (-7, 1),
        (4, 6), (-4, 6), (6, 4), (-6, 4), (2, 7), (-2, 7), (7, 2), (-7, 2),
        (3, 7), (-3, 7), (7, 3), (-7, 3), (5, 6), (-5, 6), (6, 5), (-6, 5),
        (8, 0), (4, 7), (-4, 7), (7, 4), (-7, 4), (8, 1), (8, 2), (6, 6),
        (-6, 6), (8, 3), (5, 7), (-5, 7), (7, 5), (-7, 5), (8, 4), (6, 7),
        (-6, 7), (7, 6), (-7, 6), (8, 5), (7, 7), (-7, 7), (8, 6), (8, 7),
    };

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
        var colorCache = colorCacheBits == 0 ? null : new int[colorCacheSize];

        // Meta prefix codes flag (1 bit). A value of 1 indicates an entropy image
        var metaPrefixFlag = reader.ReadBits(1);
        if (metaPrefixFlag < 0) return false;

        var prefixBits = 0;
        var metaGroups = Array.Empty<int>();
        var metaWidth = 0;
        var groupCount = 1;
        if (metaPrefixFlag != 0) {
            var prefixBitsCode = reader.ReadBits(3);
            if (prefixBitsCode < 0) return false;
            prefixBits = prefixBitsCode + 2;

            var blockSize = 1 << prefixBits;
            metaWidth = (header.Width + blockSize - 1) >> prefixBits;
            var metaHeight = (header.Height + blockSize - 1) >> prefixBits;
            if (metaWidth <= 0 || metaHeight <= 0) return false;

            if (!TryDecodeMetaImage(ref reader, metaWidth, metaHeight, out metaGroups, out groupCount)) return false;
        }

        var greenAlphabetSize = GreenAlphabetBase + colorCacheSize;
        var groups = new WebpPrefixCodesGroup[groupCount];
        for (var i = 0; i < groupCount; i++) {
            if (!TryReadPrefixCodesGroup(ref reader, greenAlphabetSize, LiteralAlphabetSize, out var group)) return false;
            groups[i] = group;
        }

        if (!TryDecodeImageNoMeta(ref reader, header, groups, metaGroups, metaWidth, prefixBits, colorCache, colorCacheBits, out var transformed)) {
            rgba = Array.Empty<byte>();
            return false;
        }

        rgba = ConvertToRgba(transformed, header.SubtractGreenTransformUsed);
        return true;
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

    private static bool TryDecodeImageNoMeta(
        ref WebpBitReader reader,
        WebpVp8lHeader header,
        WebpPrefixCodesGroup[] groups,
        int[] metaGroups,
        int metaWidth,
        int prefixBits,
        int[]? colorCache,
        int colorCacheBits,
        out int[] transformed) {
        transformed = Array.Empty<int>();
        if (groups is null || groups.Length == 0) return false;

        var pixelCount = checked(header.Width * header.Height);
        var buffer = new int[pixelCount];
        var pos = 0;
        while (pos < pixelCount) {
            var groupIndex = GetGroupIndex(pos, header.Width, groups.Length, metaGroups, metaWidth, prefixBits);
            if (groupIndex < 0) return false;
            var group = groups[groupIndex];

            var symbol = group.Green.DecodeSymbol(ref reader);
            if (symbol < 0) return false;

            if (symbol < LiteralAlphabetSize) {
                if (!TryReadLiteral(ref reader, group, symbol, out var pixel)) return false;
                buffer[pos++] = pixel;
                InsertColorCache(colorCache, colorCacheBits, pixel);
                continue;
            }

            if (symbol >= GreenAlphabetBase) {
                if (colorCache is null) return false;
                var cacheIndex = symbol - GreenAlphabetBase;
                if ((uint)cacheIndex >= (uint)colorCache.Length) return false;
                var pixel = colorCache[cacheIndex];
                buffer[pos++] = pixel;
                InsertColorCache(colorCache, colorCacheBits, pixel);
                continue;
            }

            var lengthPrefix = symbol - LiteralAlphabetSize;
            if (lengthPrefix < 0 || lengthPrefix >= LengthPrefixCount) return false;
            if (!TryDecodePrefixValue(ref reader, lengthPrefix, out var length)) return false;
            if (length <= 0 || length > MaxBackwardLength) return false;

            var distancePrefix = group.Distance.DecodeSymbol(ref reader);
            if (distancePrefix < 0 || distancePrefix >= 40) return false;
            if (!TryDecodePrefixValue(ref reader, distancePrefix, out var distanceCode)) return false;
            if (!TryMapDistanceCode(distanceCode, header.Width, out var distance)) return false;

            var nextPos = CopyLz77(buffer, pos, distance, length, pixelCount, colorCache, colorCacheBits);
            if (nextPos < 0) return false;
            pos = nextPos;
        }

        transformed = buffer;
        return true;
    }

    private static bool TryReadLiteral(ref WebpBitReader reader, WebpPrefixCodesGroup group, int green, out int pixel) {
        pixel = 0;

        var red = group.Red.DecodeSymbol(ref reader);
        var blue = group.Blue.DecodeSymbol(ref reader);
        var alpha = group.Alpha.DecodeSymbol(ref reader);
        if (red < 0 || blue < 0 || alpha < 0) return false;
        if (red >= LiteralAlphabetSize || blue >= LiteralAlphabetSize || alpha >= LiteralAlphabetSize) return false;
        if (green < 0 || green >= LiteralAlphabetSize) return false;

        pixel = PackArgb(alpha, red, green, blue);
        return true;
    }

    private static byte[] ConvertToRgba(int[] transformed, bool subtractGreen) {
        var rgba = new byte[checked(transformed.Length * 4)];
        var offset = 0;
        for (var i = 0; i < transformed.Length; i++) {
            var argb = transformed[i];
            var a = (byte)(argb >> 24);
            var r = (byte)(argb >> 16);
            var g = (byte)(argb >> 8);
            var b = (byte)argb;

            if (subtractGreen) {
                r = (byte)(r + g);
                b = (byte)(b + g);
            }

            rgba[offset++] = r;
            rgba[offset++] = g;
            rgba[offset++] = b;
            rgba[offset++] = a;
        }
        return rgba;
    }

    private static int PackArgb(int a, int r, int g, int b) {
        return ((a & 0xFF) << 24)
            | ((r & 0xFF) << 16)
            | ((g & 0xFF) << 8)
            | (b & 0xFF);
    }

    private static int GetGroupIndex(
        int pos,
        int width,
        int groupCount,
        int[] metaGroups,
        int metaWidth,
        int prefixBits) {
        if (groupCount <= 1 || metaGroups.Length == 0) return 0;
        if (metaWidth <= 0 || prefixBits < 0) return -1;

        var x = pos % width;
        var y = pos / width;
        var metaX = x >> prefixBits;
        var metaY = y >> prefixBits;
        var metaIndex = metaY * metaWidth + metaX;
        if ((uint)metaIndex >= (uint)metaGroups.Length) return -1;
        var groupIndex = metaGroups[metaIndex];
        if (groupIndex < 0 || groupIndex >= groupCount) return -1;
        return groupIndex;
    }

    private static bool TryDecodeMetaImage(
        ref WebpBitReader reader,
        int expectedWidth,
        int expectedHeight,
        out int[] metaGroups,
        out int groupCount) {
        metaGroups = Array.Empty<int>();
        groupCount = 1;

        if (!TryReadHeader(ref reader, out var header)) return false;
        if (header.Width != expectedWidth || header.Height != expectedHeight) return false;
        if (header.TransformMask != 0) return false;

        var colorCacheFlag = reader.ReadBits(1);
        if (colorCacheFlag != 0) return false;
        var metaPrefixFlag = reader.ReadBits(1);
        if (metaPrefixFlag != 0) return false;

        var greenAlphabetSize = GreenAlphabetBase;
        if (!TryReadPrefixCodesGroup(ref reader, greenAlphabetSize, LiteralAlphabetSize, out var group)) return false;
        var groups = new[] { group };

        if (!TryDecodeImageNoMeta(ref reader, header, groups, metaGroups: Array.Empty<int>(), metaWidth: 0, prefixBits: 0, colorCache: null, colorCacheBits: 0, out var transformed)) {
            return false;
        }

        var maxGroup = 0;
        metaGroups = new int[transformed.Length];
        for (var i = 0; i < transformed.Length; i++) {
            var green = (transformed[i] >> 8) & 0xFF;
            metaGroups[i] = green;
            if (green > maxGroup) maxGroup = green;
        }

        if (maxGroup >= 256) return false;
        groupCount = maxGroup + 1;
        return groupCount > 0;
    }

    internal static int ComputeColorCacheIndex(int pixel, int cacheBits) {
        if (cacheBits is < 1 or > 11) return -1;
        var hash = unchecked((uint)pixel) * ColorCacheHashMultiplier;
        return (int)(hash >> (32 - cacheBits));
    }

    private static void InsertColorCache(int[]? colorCache, int colorCacheBits, int pixel) {
        if (colorCache is null) return;
        var index = ComputeColorCacheIndex(pixel, colorCacheBits);
        if ((uint)index >= (uint)colorCache.Length) return;
        colorCache[index] = pixel;
    }

    internal static bool TryDecodePrefixValue(ref WebpBitReader reader, int prefixCode, out int value) {
        value = 0;
        if (prefixCode < 0) return false;

        if (prefixCode < 4) {
            value = prefixCode + 1;
            return true;
        }

        var extraBits = (prefixCode - 2) >> 1;
        if (extraBits < 0 || extraBits > 24) return false;
        var offset = (2 + (prefixCode & 1)) << extraBits;
        var extra = reader.ReadBits(extraBits);
        if (extra < 0) return false;
        value = offset + extra + 1;
        return value > 0;
    }

    internal static bool TryMapDistanceCode(int distanceCode, int width, out int distance) {
        distance = 0;
        if (distanceCode <= 0 || width <= 0) return false;

        if (distanceCode > DistanceMapSize) {
            distance = distanceCode - DistanceMapSize;
            return distance > 0;
        }

        var index = distanceCode - 1;
        if ((uint)index >= (uint)DistanceMap.Length) return false;
        var (xi, yi) = DistanceMap[index];
        var mapped = xi + (long)yi * width;
        if (mapped < 1) mapped = 1;
        if (mapped > int.MaxValue) return false;
        distance = (int)mapped;
        return true;
    }

    internal static int CopyLz77(
        int[] buffer,
        int pos,
        int distance,
        int length,
        int pixelCount,
        int[]? colorCache = null,
        int colorCacheBits = 0) {
        if (buffer is null) return -1;
        if (pos < 0 || pos > pixelCount) return -1;
        if (distance <= 0 || length <= 0) return -1;

        var src = pos - distance;
        if (src < 0) return -1;

        var remaining = pixelCount - pos;
        if (remaining <= 0) return pos;
        if (length > remaining) length = remaining;

        for (var i = 0; i < length; i++) {
            var pixel = buffer[src + i];
            buffer[pos + i] = pixel;
            InsertColorCache(colorCache, colorCacheBits, pixel);
        }
        return pos + length;
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

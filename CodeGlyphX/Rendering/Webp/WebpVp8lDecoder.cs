using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8L lossless decoder.
/// </summary>
internal static class WebpVp8lDecoder {
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

    private enum WebpTransformType {
        Predictor = 0,
        Color = 1,
        SubtractGreen = 2,
        ColorIndexing = 3
    }

    private readonly struct WebpTransform {
        public WebpTransform(
            WebpTransformType type,
            int sizeBits = 0,
            int width = 0,
            int height = 0,
            int[]? data = null,
            int widthBits = 0,
            int widthBefore = 0,
            int encodedWidth = 0) {
            Type = type;
            SizeBits = sizeBits;
            Width = width;
            Height = height;
            Data = data ?? Array.Empty<int>();
            WidthBits = widthBits;
            WidthBefore = widthBefore;
            EncodedWidth = encodedWidth;
        }

        public WebpTransformType Type { get; }
        public int SizeBits { get; }
        public int Width { get; }
        public int Height { get; }
        public int[] Data { get; }
        public int WidthBits { get; }
        public int WidthBefore { get; }
        public int EncodedWidth { get; }
    }

    /// <summary>
    /// Attempts to decode a VP8L payload to RGBA32 (lossless).
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> payload, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        var reader = new WebpBitReader(payload);
        if (!TryReadHeader(ref reader, out var header)) return false;

        width = header.Width;
        height = header.Height;

        if (!TryReadTransforms(ref reader, width, height, depth: 0, out var transforms, out var dataWidth, out var dataHeight)) return false;
        if (!TryDecodeImageCore(ref reader, dataWidth, dataHeight, depth: 0, out var transformed)) return false;
        if (!ApplyTransforms(ref transformed, width, height, dataWidth, dataHeight, transforms)) return false;

        rgba = ConvertToRgba(transformed);
        return true;
    }

    internal static bool TryDecodeImageStream(ReadOnlySpan<byte> payload, int width, int height, out int[] argb) {
        argb = Array.Empty<int>();
        if (width <= 0 || height <= 0) return false;

        var reader = new WebpBitReader(payload);
        if (!TryReadTransforms(ref reader, width, height, depth: 0, out var transforms, out var dataWidth, out var dataHeight)) return false;
        if (!TryDecodeImageCore(ref reader, dataWidth, dataHeight, depth: 0, out var transformed)) return false;
        if (!ApplyTransforms(ref transformed, width, height, dataWidth, dataHeight, transforms)) return false;

        argb = transformed;
        return true;
    }

    internal static bool TryDecodeWithReason(ReadOnlySpan<byte> payload, out byte[] rgba, out int width, out int height, out string reason) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;
        reason = string.Empty;

        var reader = new WebpBitReader(payload);
        if (!TryReadHeader(ref reader, out var header)) {
            reason = "Failed to read VP8L header.";
            return false;
        }

        width = header.Width;
        height = header.Height;

        if (!TryReadTransformsWithReason(ref reader, width, height, depth: 0, out var transforms, out var dataWidth, out var dataHeight, out var transformsReason)) {
            reason = transformsReason;
            return false;
        }

        if (!TryDecodeImageCore(ref reader, dataWidth, dataHeight, depth: 0, out var transformed)) {
            reason = "Failed to decode VP8L image data.";
            return false;
        }

        if (!ApplyTransforms(ref transformed, width, height, dataWidth, dataHeight, transforms)) {
            reason = "Failed to apply VP8L transforms.";
            return false;
        }

        rgba = ConvertToRgba(transformed);
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

        header = new WebpVp8lHeader(
            width,
            height,
            alphaUsed != 0,
            reader.BitsConsumed);
        return true;
    }

    private static bool TryReadTransforms(
        ref WebpBitReader reader,
        int width,
        int height,
        int depth,
        out WebpTransform[] transforms,
        out int dataWidth,
        out int dataHeight) {
        transforms = Array.Empty<WebpTransform>();
        dataWidth = width;
        dataHeight = height;
        var seenMask = 0;
        var list = new List<WebpTransform>(4);
        var currentWidth = width;
        var currentHeight = height;

        while (true) {
            var hasTransform = reader.ReadBits(1);
            if (hasTransform < 0) return false;
            if (hasTransform == 0) break;

            var transformTypeCode = reader.ReadBits(2);
            if (transformTypeCode < 0) return false;
            if (transformTypeCode > (int)WebpTransformType.ColorIndexing) return false;
            var transformType = (WebpTransformType)transformTypeCode;

            var bit = 1 << transformTypeCode;
            if ((seenMask & bit) != 0) return false;
            seenMask |= bit;

            switch (transformType) {
                case WebpTransformType.SubtractGreen:
                    list.Add(new WebpTransform(WebpTransformType.SubtractGreen));
                    break;
                case WebpTransformType.Predictor:
                    if (!TryReadPredictorTransform(ref reader, currentWidth, currentHeight, depth, out var predictor)) return false;
                    list.Add(predictor);
                    break;
                case WebpTransformType.Color:
                    if (!TryReadColorTransform(ref reader, currentWidth, currentHeight, depth, out var colorTransform)) return false;
                    list.Add(colorTransform);
                    break;
                case WebpTransformType.ColorIndexing:
                    if (!TryReadColorIndexingTransform(ref reader, currentWidth, currentHeight, depth, out var indexing, out currentWidth)) return false;
                    list.Add(indexing);
                    break;
                default:
                    return false;
            }
        }

        transforms = list.ToArray();
        dataWidth = currentWidth;
        dataHeight = currentHeight;
        return true;
    }

    private static bool TryReadTransformsWithReason(
        ref WebpBitReader reader,
        int width,
        int height,
        int depth,
        out WebpTransform[] transforms,
        out int dataWidth,
        out int dataHeight,
        out string reason) {
        transforms = Array.Empty<WebpTransform>();
        dataWidth = width;
        dataHeight = height;
        reason = string.Empty;

        var seenMask = 0;
        var list = new List<WebpTransform>(4);
        var currentWidth = width;
        var currentHeight = height;

        while (true) {
            var hasTransform = reader.ReadBits(1);
            if (hasTransform < 0) {
                reason = "Failed to read transform flag.";
                return false;
            }
            if (hasTransform == 0) break;

            var transformTypeCode = reader.ReadBits(2);
            if (transformTypeCode < 0) {
                reason = "Failed to read transform type.";
                return false;
            }
            if (transformTypeCode > (int)WebpTransformType.ColorIndexing) {
                reason = "Unknown transform type.";
                return false;
            }
            var transformType = (WebpTransformType)transformTypeCode;

            var bit = 1 << transformTypeCode;
            if ((seenMask & bit) != 0) {
                reason = $"Duplicate transform ({transformType}).";
                return false;
            }
            seenMask |= bit;

            switch (transformType) {
                case WebpTransformType.SubtractGreen:
                    list.Add(new WebpTransform(WebpTransformType.SubtractGreen));
                    break;
                case WebpTransformType.Predictor:
                    if (!TryReadPredictorTransformWithReason(ref reader, currentWidth, currentHeight, depth, out var predictor, out reason)) {
                        reason = $"Predictor transform: {reason}";
                        return false;
                    }
                    list.Add(predictor);
                    break;
                case WebpTransformType.Color:
                    if (!TryReadColorTransformWithReason(ref reader, currentWidth, currentHeight, depth, out var colorTransform, out reason)) {
                        reason = $"Color transform: {reason}";
                        return false;
                    }
                    list.Add(colorTransform);
                    break;
                case WebpTransformType.ColorIndexing:
                    if (!TryReadColorIndexingTransformWithReason(ref reader, currentWidth, currentHeight, depth, out var indexing, out currentWidth, out reason)) {
                        reason = $"Color indexing transform: {reason}";
                        return false;
                    }
                    list.Add(indexing);
                    break;
                default:
                    reason = "Unknown transform type.";
                    return false;
            }
        }

        transforms = list.ToArray();
        dataWidth = currentWidth;
        dataHeight = currentHeight;
        return true;
    }

    private static bool TryReadPredictorTransform(ref WebpBitReader reader, int width, int height, int depth, out WebpTransform transform) {
        transform = default;

        var sizeBitsCode = reader.ReadBits(3);
        if (sizeBitsCode < 0) return false;
        var sizeBits = sizeBitsCode + 2;
        var blockSize = 1 << sizeBits;

        var transformWidth = (width + blockSize - 1) >> sizeBits;
        var transformHeight = (height + blockSize - 1) >> sizeBits;
        if (transformWidth <= 0 || transformHeight <= 0) return false;

        if (!TryDecodeImageNoTransforms(ref reader, transformWidth, transformHeight, depth + 1, readMetaPrefix: false, out var predictorPixels)) return false;
        var modes = new int[predictorPixels.Length];
        for (var i = 0; i < predictorPixels.Length; i++) {
            var green = (predictorPixels[i] >> 8) & 0xFF;
            modes[i] = green % 14;
        }

        transform = new WebpTransform(WebpTransformType.Predictor, sizeBits, transformWidth, transformHeight, modes);
        return true;
    }

    private static bool TryReadPredictorTransformWithReason(
        ref WebpBitReader reader,
        int width,
        int height,
        int depth,
        out WebpTransform transform,
        out string reason) {
        transform = default;
        reason = string.Empty;

        var sizeBitsCode = reader.ReadBits(3);
        if (sizeBitsCode < 0) {
            reason = "Failed to read predictor transform size bits.";
            return false;
        }
        var sizeBits = sizeBitsCode + 2;
        var blockSize = 1 << sizeBits;

        var transformWidth = (width + blockSize - 1) >> sizeBits;
        var transformHeight = (height + blockSize - 1) >> sizeBits;
        if (transformWidth <= 0 || transformHeight <= 0) {
            reason = "Predictor transform dimensions are invalid.";
            return false;
        }

        var probe = reader;
        if (!TryDecodeImageNoTransformsWithReason(ref probe, transformWidth, transformHeight, depth + 1, readMetaPrefix: false, out var predictorPixels, out reason)) {
            var sigProbe = reader;
            var nextByte = sigProbe.ReadBits(8);
            if (nextByte >= 0) {
                reason = $"{reason} (next byte 0x{nextByte:X2})";
            }
            return false;
        }
        reader = probe;

        var modes = new int[predictorPixels.Length];
        for (var i = 0; i < predictorPixels.Length; i++) {
            var green = (predictorPixels[i] >> 8) & 0xFF;
            modes[i] = green % 14;
        }

        transform = new WebpTransform(WebpTransformType.Predictor, sizeBits, transformWidth, transformHeight, modes);
        return true;
    }

    private static bool TryReadColorTransform(ref WebpBitReader reader, int width, int height, int depth, out WebpTransform transform) {
        transform = default;

        var sizeBitsCode = reader.ReadBits(3);
        if (sizeBitsCode < 0) return false;
        var sizeBits = sizeBitsCode + 2;
        var blockSize = 1 << sizeBits;

        var transformWidth = (width + blockSize - 1) >> sizeBits;
        var transformHeight = (height + blockSize - 1) >> sizeBits;
        if (transformWidth <= 0 || transformHeight <= 0) return false;

        if (!TryDecodeImageNoTransforms(ref reader, transformWidth, transformHeight, depth + 1, readMetaPrefix: false, out var transformPixels)) return false;
        transform = new WebpTransform(WebpTransformType.Color, sizeBits, transformWidth, transformHeight, transformPixels);
        return true;
    }

    private static bool TryReadColorTransformWithReason(
        ref WebpBitReader reader,
        int width,
        int height,
        int depth,
        out WebpTransform transform,
        out string reason) {
        transform = default;
        reason = string.Empty;

        var sizeBitsCode = reader.ReadBits(3);
        if (sizeBitsCode < 0) {
            reason = "Failed to read color transform size bits.";
            return false;
        }
        var sizeBits = sizeBitsCode + 2;
        var blockSize = 1 << sizeBits;

        var transformWidth = (width + blockSize - 1) >> sizeBits;
        var transformHeight = (height + blockSize - 1) >> sizeBits;
        if (transformWidth <= 0 || transformHeight <= 0) {
            reason = "Color transform dimensions are invalid.";
            return false;
        }

        var probe = reader;
        if (!TryDecodeImageNoTransformsWithReason(ref probe, transformWidth, transformHeight, depth + 1, readMetaPrefix: false, out var transformPixels, out reason)) {
            var sigProbe = reader;
            var nextByte = sigProbe.ReadBits(8);
            if (nextByte >= 0) {
                reason = $"{reason} (next byte 0x{nextByte:X2})";
            }
            return false;
        }
        reader = probe;

        transform = new WebpTransform(WebpTransformType.Color, sizeBits, transformWidth, transformHeight, transformPixels);
        return true;
    }

    private static bool TryReadColorIndexingTransform(
        ref WebpBitReader reader,
        int currentWidth,
        int currentHeight,
        int depth,
        out WebpTransform transform,
        out int nextWidth) {
        transform = default;
        nextWidth = currentWidth;

        var colorTableSizeMinus1 = reader.ReadBits(8);
        if (colorTableSizeMinus1 < 0) return false;
        var colorTableSize = colorTableSizeMinus1 + 1;
        if (colorTableSize <= 0) return false;

        if (!TryDecodeImageNoTransforms(ref reader, colorTableSize, 1, depth + 1, readMetaPrefix: false, out var paletteDeltas)) return false;
        var palette = BuildColorTableFromDeltas(paletteDeltas);

        var widthBits = GetColorIndexWidthBits(colorTableSize);
        var widthBefore = currentWidth;
        if (widthBits > 0) {
            var group = 1 << widthBits;
            nextWidth = (currentWidth + group - 1) >> widthBits;
        }

        transform = new WebpTransform(
            WebpTransformType.ColorIndexing,
            widthBits: widthBits,
            widthBefore: widthBefore,
            encodedWidth: nextWidth,
            data: palette);
        return true;
    }

    private static bool TryReadColorIndexingTransformWithReason(
        ref WebpBitReader reader,
        int currentWidth,
        int currentHeight,
        int depth,
        out WebpTransform transform,
        out int nextWidth,
        out string reason) {
        transform = default;
        nextWidth = currentWidth;
        reason = string.Empty;

        var colorTableSizeMinus1 = reader.ReadBits(8);
        if (colorTableSizeMinus1 < 0) {
            reason = "Failed to read color indexing palette size.";
            return false;
        }
        var colorTableSize = colorTableSizeMinus1 + 1;
        if (colorTableSize <= 0) {
            reason = "Color indexing palette size is invalid.";
            return false;
        }

        var probe = reader;
        if (!TryDecodeImageNoTransformsWithReason(ref probe, colorTableSize, 1, depth + 1, readMetaPrefix: false, out var paletteDeltas, out reason)) {
            var sigProbe = reader;
            var nextByte = sigProbe.ReadBits(8);
            if (nextByte >= 0) {
                reason = $"{reason} (next byte 0x{nextByte:X2})";
            }
            return false;
        }
        reader = probe;
        var palette = BuildColorTableFromDeltas(paletteDeltas);

        var widthBits = GetColorIndexWidthBits(colorTableSize);
        var widthBefore = currentWidth;
        if (widthBits > 0) {
            var group = 1 << widthBits;
            nextWidth = (currentWidth + group - 1) >> widthBits;
        }

        transform = new WebpTransform(
            WebpTransformType.ColorIndexing,
            widthBits: widthBits,
            widthBefore: widthBefore,
            encodedWidth: nextWidth,
            data: palette);
        return true;
    }

    private static int[] BuildColorTableFromDeltas(int[] paletteDeltas) {
        var palette = new int[paletteDeltas.Length];

        var a = 0;
        var r = 0;
        var g = 0;
        var b = 0;

        for (var i = 0; i < paletteDeltas.Length; i++) {
            var delta = paletteDeltas[i];

            a = (a + ((delta >> 24) & 0xFF)) & 0xFF;
            r = (r + ((delta >> 16) & 0xFF)) & 0xFF;
            g = (g + ((delta >> 8) & 0xFF)) & 0xFF;
            b = (b + (delta & 0xFF)) & 0xFF;

            palette[i] = PackArgb(a, r, g, b);
        }

        return palette;
    }

    private static int GetColorIndexWidthBits(int colorTableSize) {
        if (colorTableSize <= 2) return 3;
        if (colorTableSize <= 4) return 2;
        if (colorTableSize <= 16) return 1;
        return 0;
    }

    private static bool TryDecodeImageCore(ref WebpBitReader reader, int width, int height, int depth, out int[] transformed) {
        return TryDecodeImageCore(ref reader, width, height, depth, readMetaPrefix: true, out transformed);
    }

    private static bool TryDecodeImageCore(
        ref WebpBitReader reader,
        int width,
        int height,
        int depth,
        bool readMetaPrefix,
        out int[] transformed) {
        transformed = Array.Empty<int>();
        if (depth > 6) return false;

        var colorCacheFlag = reader.ReadBits(1);
        if (colorCacheFlag < 0) return false;
        var colorCacheBits = 0;
        if (colorCacheFlag != 0) {
            colorCacheBits = reader.ReadBits(4);
            if (colorCacheBits is < 1 or > 11) return false;
        }
        var colorCacheSize = colorCacheBits == 0 ? 0 : 1 << colorCacheBits;
        var colorCache = colorCacheBits == 0 ? null : new int[colorCacheSize];

        var metaPrefixFlag = 0;
        if (readMetaPrefix) {
            metaPrefixFlag = reader.ReadBits(1);
            if (metaPrefixFlag < 0) return false;
        }
        var prefixBits = 0;
        var metaGroups = Array.Empty<int>();
        var metaWidth = 0;
        var groupCount = 1;
        if (metaPrefixFlag != 0) {
            var prefixBitsCode = reader.ReadBits(3);
            if (prefixBitsCode < 0) return false;
            prefixBits = prefixBitsCode + 2;

            var blockSize = 1 << prefixBits;
            metaWidth = (width + blockSize - 1) >> prefixBits;
            var metaHeight = (height + blockSize - 1) >> prefixBits;
            if (metaWidth <= 0 || metaHeight <= 0) return false;

            if (!TryDecodeImageNoTransforms(ref reader, metaWidth, metaHeight, depth + 1, readMetaPrefix: false, out var metaPixels)) return false;
            if (!TryBuildMetaGroups(metaPixels, out metaGroups, out groupCount)) return false;
        }

        var greenAlphabetSize = GreenAlphabetBase + colorCacheSize;
        var groups = new WebpPrefixCodesGroup[groupCount];
        for (var i = 0; i < groupCount; i++) {
            if (!TryReadPrefixCodesGroup(ref reader, greenAlphabetSize, LiteralAlphabetSize, out var group)) return false;
            groups[i] = group;
        }

        return TryDecodeImageData(ref reader, width, height, groups, metaGroups, metaWidth, prefixBits, colorCache, colorCacheBits, out transformed);
    }

    private static bool TryDecodeImageCoreWithReason(
        ref WebpBitReader reader,
        int width,
        int height,
        int depth,
        out int[] transformed,
        out string reason) {
        return TryDecodeImageCoreWithReason(ref reader, width, height, depth, readMetaPrefix: true, out transformed, out reason);
    }

    private static bool TryDecodeImageCoreWithReason(
        ref WebpBitReader reader,
        int width,
        int height,
        int depth,
        bool readMetaPrefix,
        out int[] transformed,
        out string reason) {
        transformed = Array.Empty<int>();
        reason = string.Empty;
        if (depth > 6) {
            reason = "Exceeded nested VP8L depth limit.";
            return false;
        }

        var colorCacheFlag = reader.ReadBits(1);
        if (colorCacheFlag < 0) {
            reason = "Failed to read color cache flag.";
            return false;
        }
        var colorCacheBits = 0;
        if (colorCacheFlag != 0) {
            colorCacheBits = reader.ReadBits(4);
            if (colorCacheBits is < 1 or > 11) {
                reason = $"Invalid color cache bits ({colorCacheBits}).";
                return false;
            }
        }
        var colorCacheSize = colorCacheBits == 0 ? 0 : 1 << colorCacheBits;
        var colorCache = colorCacheBits == 0 ? null : new int[colorCacheSize];

        var metaPrefixFlag = 0;
        if (readMetaPrefix) {
            metaPrefixFlag = reader.ReadBits(1);
            if (metaPrefixFlag < 0) {
                reason = "Failed to read meta prefix flag.";
                return false;
            }
        }
        var prefixBits = 0;
        var metaGroups = Array.Empty<int>();
        var metaWidth = 0;
        var groupCount = 1;
        if (metaPrefixFlag != 0) {
            var prefixBitsCode = reader.ReadBits(3);
            if (prefixBitsCode < 0) {
                reason = "Failed to read meta prefix bits.";
                return false;
            }
            prefixBits = prefixBitsCode + 2;

            var blockSize = 1 << prefixBits;
            metaWidth = (width + blockSize - 1) >> prefixBits;
            var metaHeight = (height + blockSize - 1) >> prefixBits;
            if (metaWidth <= 0 || metaHeight <= 0) {
                reason = "Invalid meta prefix image dimensions.";
                return false;
            }

            if (!TryDecodeImageNoTransformsWithReason(ref reader, metaWidth, metaHeight, depth + 1, readMetaPrefix: false, out var metaPixels, out reason)) {
                return false;
            }
            if (!TryBuildMetaGroups(metaPixels, out metaGroups, out groupCount)) {
                reason = "Failed to build meta prefix groups.";
                return false;
            }
        }

        var greenAlphabetSize = GreenAlphabetBase + colorCacheSize;
        var groups = new WebpPrefixCodesGroup[groupCount];
        for (var i = 0; i < groupCount; i++) {
            if (!TryReadPrefixCodesGroupWithReason(ref reader, greenAlphabetSize, LiteralAlphabetSize, out var group, out reason)) {
                return false;
            }
            groups[i] = group;
        }

        if (!TryDecodeImageData(ref reader, width, height, groups, metaGroups, metaWidth, prefixBits, colorCache, colorCacheBits, out transformed)) {
            reason = "Failed to decode VP8L pixel data.";
            return false;
        }

        return true;
    }

    private static bool TryDecodeImageNoTransforms(ref WebpBitReader reader, int expectedWidth, int expectedHeight, int depth, out int[] transformed) {
        return TryDecodeImageNoTransforms(ref reader, expectedWidth, expectedHeight, depth, readMetaPrefix: true, out transformed);
    }

    private static bool TryDecodeImageNoTransforms(
        ref WebpBitReader reader,
        int expectedWidth,
        int expectedHeight,
        int depth,
        bool readMetaPrefix,
        out int[] transformed) {
        transformed = Array.Empty<int>();
        if (expectedWidth <= 0 || expectedHeight <= 0) return false;

        var baseReader = reader;

        var probe = baseReader;
        if (TryDecodeImageCore(ref probe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed)) {
            reader = probe;
            return true;
        }

        var transformFlagProbe = baseReader;
        var hasTransform = transformFlagProbe.ReadBits(1);
        if (hasTransform == 0 && TryDecodeImageCore(ref transformFlagProbe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed)) {
            reader = transformFlagProbe;
            return true;
        }

        var headerProbe = baseReader;
        if (TryReadHeader(ref headerProbe, out var header) && header.Width == expectedWidth && header.Height == expectedHeight) {
            var noTransformFlagProbe = headerProbe;
            if (TryDecodeImageCore(ref noTransformFlagProbe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed)) {
                reader = noTransformFlagProbe;
                return true;
            }

            var withTransformFlagProbe = headerProbe;
            hasTransform = withTransformFlagProbe.ReadBits(1);
            if (hasTransform == 0 && TryDecodeImageCore(ref withTransformFlagProbe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed)) {
                reader = withTransformFlagProbe;
                return true;
            }
        }

        return false;
    }

    private static bool TryDecodeImageNoTransformsWithReason(
        ref WebpBitReader reader,
        int expectedWidth,
        int expectedHeight,
        int depth,
        out int[] transformed,
        out string reason) {
        return TryDecodeImageNoTransformsWithReason(
            ref reader,
            expectedWidth,
            expectedHeight,
            depth,
            readMetaPrefix: true,
            out transformed,
            out reason);
    }

    private static bool TryDecodeImageNoTransformsWithReason(
        ref WebpBitReader reader,
        int expectedWidth,
        int expectedHeight,
        int depth,
        bool readMetaPrefix,
        out int[] transformed,
        out string reason) {
        transformed = Array.Empty<int>();
        reason = string.Empty;

        if (expectedWidth <= 0 || expectedHeight <= 0) {
            reason = "Embedded VP8L dimensions are invalid.";
            return false;
        }

        var baseReader = reader;

        var probe = baseReader;
        if (TryDecodeImageCoreWithReason(ref probe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed, out reason)) {
            reader = probe;
            return true;
        }

        var transformFlagProbe = baseReader;
        var hasTransform = transformFlagProbe.ReadBits(1);
        if (hasTransform == 0 && TryDecodeImageCoreWithReason(ref transformFlagProbe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed, out reason)) {
            reader = transformFlagProbe;
            return true;
        }

        var headerProbe = baseReader;
        if (TryReadHeader(ref headerProbe, out var header) && header.Width == expectedWidth && header.Height == expectedHeight) {
            var noTransformFlagProbe = headerProbe;
            if (TryDecodeImageCoreWithReason(ref noTransformFlagProbe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed, out reason)) {
                reader = noTransformFlagProbe;
                return true;
            }

            var withTransformFlagProbe = headerProbe;
            hasTransform = withTransformFlagProbe.ReadBits(1);
            if (hasTransform == 0 && TryDecodeImageCoreWithReason(ref withTransformFlagProbe, expectedWidth, expectedHeight, depth, readMetaPrefix, out transformed, out reason)) {
                reader = withTransformFlagProbe;
                return true;
            }
        }

        return false;
    }

    private static bool TryBuildMetaGroups(int[] metaPixels, out int[] metaGroups, out int groupCount) {
        metaGroups = Array.Empty<int>();
        groupCount = 1;
        if (metaPixels.Length == 0) return false;

        var maxGroup = 0;
        var groups = new int[metaPixels.Length];
        for (var i = 0; i < metaPixels.Length; i++) {
            var metaPrefixCode = (metaPixels[i] >> 8) & 0xFFFF; // red + green
            groups[i] = metaPrefixCode;
            if (metaPrefixCode > maxGroup) maxGroup = metaPrefixCode;
        }

        metaGroups = groups;
        groupCount = maxGroup + 1;
        return groupCount > 0;
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

    private static bool TryReadPrefixCodesGroupWithReason(
        ref WebpBitReader reader,
        int greenAlphabetSize,
        int literalAlphabetSize,
        out WebpPrefixCodesGroup group,
        out string reason) {
        group = default;
        reason = string.Empty;

        if (!WebpPrefixCodeReader.TryReadPrefixCodeWithReason(ref reader, greenAlphabetSize, out var green, out var greenReason)) {
            reason = $"Failed to read green prefix code: {greenReason}";
            return false;
        }
        if (!WebpPrefixCodeReader.TryReadPrefixCodeWithReason(ref reader, literalAlphabetSize, out var red, out var redReason)) {
            reason = $"Failed to read red prefix code: {redReason}";
            return false;
        }
        if (!WebpPrefixCodeReader.TryReadPrefixCodeWithReason(ref reader, literalAlphabetSize, out var blue, out var blueReason)) {
            reason = $"Failed to read blue prefix code: {blueReason}";
            return false;
        }
        if (!WebpPrefixCodeReader.TryReadPrefixCodeWithReason(ref reader, literalAlphabetSize, out var alpha, out var alphaReason)) {
            reason = $"Failed to read alpha prefix code: {alphaReason}";
            return false;
        }
        if (!WebpPrefixCodeReader.TryReadPrefixCodeWithReason(ref reader, alphabetSize: 40, out var distance, out var distanceReason)) {
            reason = $"Failed to read distance prefix code: {distanceReason}";
            return false;
        }

        group = new WebpPrefixCodesGroup(green, red, blue, alpha, distance);
        return true;
    }

    private static bool TryDecodeImageData(
        ref WebpBitReader reader,
        int width,
        int height,
        WebpPrefixCodesGroup[] groups,
        int[] metaGroups,
        int metaWidth,
        int prefixBits,
        int[]? colorCache,
        int colorCacheBits,
        out int[] transformed) {
        transformed = Array.Empty<int>();
        if (groups is null || groups.Length == 0) return false;

        var pixelCount = checked(width * height);
        var buffer = new int[pixelCount];
        var pos = 0;
        while (pos < pixelCount) {
            var groupIndex = GetGroupIndex(pos, width, groups.Length, metaGroups, metaWidth, prefixBits);
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
            if (!TryMapDistanceCode(distanceCode, width, out var distance)) return false;

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

    private static bool ApplyTransforms(
        ref int[] pixels,
        int originalWidth,
        int originalHeight,
        int dataWidth,
        int dataHeight,
        WebpTransform[] transforms) {
        if (transforms.Length == 0) return true;
        var currentWidth = dataWidth;
        var currentHeight = dataHeight;
        for (var i = transforms.Length - 1; i >= 0; i--) {
            var transform = transforms[i];
            switch (transform.Type) {
                case WebpTransformType.SubtractGreen:
                    ApplySubtractGreen(pixels);
                    break;
                case WebpTransformType.Predictor:
                    if (!ApplyPredictorTransform(pixels, currentWidth, currentHeight, transform)) return false;
                    break;
                case WebpTransformType.Color:
                    if (!ApplyColorTransform(pixels, currentWidth, currentHeight, transform)) return false;
                    break;
                case WebpTransformType.ColorIndexing:
                    if (!ApplyColorIndexingTransform(pixels, currentWidth, currentHeight, transform, out var expanded)) return false;
                    pixels = expanded;
                    currentWidth = transform.WidthBefore;
                    break;
                default:
                    return false;
            }
        }
        return currentWidth == originalWidth && currentHeight == originalHeight;
    }

    private static void ApplySubtractGreen(int[] pixels) {
        for (var i = 0; i < pixels.Length; i++) {
            var argb = pixels[i];
            var a = (byte)(argb >> 24);
            var r = (byte)(argb >> 16);
            var g = (byte)(argb >> 8);
            var b = (byte)argb;
            r = (byte)(r + g);
            b = (byte)(b + g);
            pixels[i] = PackArgb(a, r, g, b);
        }
    }

    private static bool ApplyPredictorTransform(int[] pixels, int width, int height, WebpTransform transform) {
        if (transform.Data.Length == 0) return false;
        if (transform.Width <= 0 || transform.Height <= 0) return false;

        var sizeBits = transform.SizeBits;
        var transformWidth = transform.Width;
        var modes = transform.Data;

        for (var y = 0; y < height; y++) {
            var rowStart = y * width;
            for (var x = 0; x < width; x++) {
                var index = rowStart + x;
                var mode = GetPredictorMode(modes, transformWidth, sizeBits, x, y);
                var predicted = PredictPixel(pixels, width, x, y, mode);
                pixels[index] = AddPixelsModulo(pixels[index], predicted);
            }
        }
        return true;
    }

    private static bool ApplyColorTransform(int[] pixels, int width, int height, WebpTransform transform) {
        if (transform.Data.Length == 0) return false;
        if (transform.Width <= 0 || transform.Height <= 0) return false;

        var sizeBits = transform.SizeBits;
        var transformWidth = transform.Width;
        var elements = transform.Data;

        for (var y = 0; y < height; y++) {
            var rowStart = y * width;
            for (var x = 0; x < width; x++) {
                var index = rowStart + x;
                var element = GetTransformElement(elements, transformWidth, sizeBits, x, y);

                var g2r = unchecked((sbyte)(element & 0xFF));
                var g2b = unchecked((sbyte)((element >> 8) & 0xFF));
                var r2b = unchecked((sbyte)((element >> 16) & 0xFF));

                var argb = pixels[index];
                var a = (byte)(argb >> 24);
                var r = (byte)(argb >> 16);
                var g = (byte)(argb >> 8);
                var b = (byte)argb;

                r = (byte)(r + ColorTransformDelta(g2r, g));
                b = (byte)(b + ColorTransformDelta(g2b, g));
                b = (byte)(b + ColorTransformDelta(r2b, r));

                pixels[index] = PackArgb(a, r, g, b);
            }
        }
        return true;
    }

    private static bool ApplyColorIndexingTransform(
        int[] pixels,
        int currentWidth,
        int currentHeight,
        WebpTransform transform,
        out int[] expanded) {
        expanded = Array.Empty<int>();
        var palette = transform.Data;
        if (palette.Length == 0) return false;
        if (transform.WidthBefore <= 0 || transform.EncodedWidth <= 0) return false;
        if (currentWidth != transform.EncodedWidth) return false;

        var widthBefore = transform.WidthBefore;
        var widthBits = transform.WidthBits;
        var pixelCount = checked(widthBefore * currentHeight);
        var output = new int[pixelCount];

        if (widthBits == 0) {
            for (var i = 0; i < pixels.Length; i++) {
                var green = (pixels[i] >> 8) & 0xFF;
                if ((uint)green >= (uint)palette.Length) return false;
                output[i] = palette[green];
            }
            expanded = output;
            return true;
        }

        var indicesPerPixel = 1 << widthBits;
        var bitsPerIndex = 8 >> widthBits;
        var indexMask = (1 << bitsPerIndex) - 1;
        for (var y = 0; y < currentHeight; y++) {
            var rowStart = y * currentWidth;
            for (var xSub = 0; xSub < currentWidth; xSub++) {
                var argb = pixels[rowStart + xSub];
                var packed = (argb >> 8) & 0xFF;
                var baseX = xSub << widthBits;
                for (var i = 0; i < indicesPerPixel; i++) {
                    var x = baseX + i;
                    if (x >= widthBefore) break;
                    var index = (packed >> (i * bitsPerIndex)) & indexMask;
                    if ((uint)index >= (uint)palette.Length) return false;
                    output[(y * widthBefore) + x] = palette[index];
                }
            }
        }

        expanded = output;
        return true;
    }

    private static int GetPredictorMode(int[] modes, int transformWidth, int sizeBits, int x, int y) {
        var blockX = x >> sizeBits;
        var blockY = y >> sizeBits;
        var blockIndex = blockY * transformWidth + blockX;
        if ((uint)blockIndex >= (uint)modes.Length) return 0;
        var mode = modes[blockIndex];
        if (mode < 0) mode = 0;
        return mode % 14;
    }

    private static int GetTransformElement(int[] elements, int transformWidth, int sizeBits, int x, int y) {
        var blockX = x >> sizeBits;
        var blockY = y >> sizeBits;
        var blockIndex = blockY * transformWidth + blockX;
        if ((uint)blockIndex >= (uint)elements.Length) return elements[0];
        return elements[blockIndex];
    }

    private static int PredictPixel(int[] pixels, int width, int x, int y, int mode) {
        if (x == 0 && y == 0) return unchecked((int)0xFF000000);
        var index = y * width + x;

        if (y == 0) return pixels[index - 1];
        if (x == 0) return pixels[index - width];

        var left = pixels[index - 1];
        var top = pixels[index - width];
        var topLeft = pixels[index - width - 1];
        var topRight = x == width - 1 ? pixels[index - x] : pixels[index - width + 1];

        return mode switch {
            0 => unchecked((int)0xFF000000),
            1 => left,
            2 => top,
            3 => topRight,
            4 => topLeft,
            5 => Average2(Average2(left, topRight), top),
            6 => Average2(left, topLeft),
            7 => Average2(left, top),
            8 => Average2(topLeft, top),
            9 => Average2(top, topRight),
            10 => Average2(Average2(left, topLeft), Average2(top, topRight)),
            11 => Select(left, top, topLeft),
            12 => ClampAddSubtractFull(left, top, topLeft),
            13 => ClampAddSubtractHalf(Average2(left, top), topLeft),
            _ => left
        };
    }

    private static int ColorTransformDelta(sbyte transform, byte channel) {
        return (transform * channel) >> 5;
    }

    private static int AddPixelsModulo(int residual, int predicted) {
        var a = (byte)((residual >> 24) + (predicted >> 24));
        var r = (byte)((residual >> 16) + (predicted >> 16));
        var g = (byte)((residual >> 8) + (predicted >> 8));
        var b = (byte)(residual + predicted);
        return PackArgb(a, r, g, b);
    }

    private static int Average2(int a, int b) {
        var aa = ((a >> 24) & 0xFF) + ((b >> 24) & 0xFF);
        var ar = ((a >> 16) & 0xFF) + ((b >> 16) & 0xFF);
        var ag = ((a >> 8) & 0xFF) + ((b >> 8) & 0xFF);
        var ab = (a & 0xFF) + (b & 0xFF);
        return PackArgb(aa >> 1, ar >> 1, ag >> 1, ab >> 1);
    }

    private static int ClampAddSubtractFull(int a, int b, int c) {
        var aa = Clamp(((a >> 24) & 0xFF) + ((b >> 24) & 0xFF) - ((c >> 24) & 0xFF));
        var ar = Clamp(((a >> 16) & 0xFF) + ((b >> 16) & 0xFF) - ((c >> 16) & 0xFF));
        var ag = Clamp(((a >> 8) & 0xFF) + ((b >> 8) & 0xFF) - ((c >> 8) & 0xFF));
        var ab = Clamp((a & 0xFF) + (b & 0xFF) - (c & 0xFF));
        return PackArgb(aa, ar, ag, ab);
    }

    private static int ClampAddSubtractHalf(int a, int b) {
        var aa = Clamp(((a >> 24) & 0xFF) + ((((a >> 24) & 0xFF) - ((b >> 24) & 0xFF)) >> 1));
        var ar = Clamp(((a >> 16) & 0xFF) + ((((a >> 16) & 0xFF) - ((b >> 16) & 0xFF)) >> 1));
        var ag = Clamp(((a >> 8) & 0xFF) + ((((a >> 8) & 0xFF) - ((b >> 8) & 0xFF)) >> 1));
        var ab = Clamp((a & 0xFF) + (((a & 0xFF) - (b & 0xFF)) >> 1));
        return PackArgb(aa, ar, ag, ab);
    }

    private static int Select(int left, int top, int topLeft) {
        var pa = ((left >> 24) & 0xFF) + ((top >> 24) & 0xFF) - ((topLeft >> 24) & 0xFF);
        var pr = ((left >> 16) & 0xFF) + ((top >> 16) & 0xFF) - ((topLeft >> 16) & 0xFF);
        var pg = ((left >> 8) & 0xFF) + ((top >> 8) & 0xFF) - ((topLeft >> 8) & 0xFF);
        var pb = (left & 0xFF) + (top & 0xFF) - (topLeft & 0xFF);

        var distLeft = Abs(pa - ((left >> 24) & 0xFF))
            + Abs(pr - ((left >> 16) & 0xFF))
            + Abs(pg - ((left >> 8) & 0xFF))
            + Abs(pb - (left & 0xFF));
        var distTop = Abs(pa - ((top >> 24) & 0xFF))
            + Abs(pr - ((top >> 16) & 0xFF))
            + Abs(pg - ((top >> 8) & 0xFF))
            + Abs(pb - (top & 0xFF));
        return distLeft <= distTop ? left : top;
    }

    private static int Clamp(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return value;
    }

    private static int Abs(int value) => value < 0 ? -value : value;

    private static byte[] ConvertToRgba(int[] transformed) {
        var rgba = new byte[checked(transformed.Length * 4)];
        var offset = 0;
        for (var i = 0; i < transformed.Length; i++) {
            var argb = transformed[i];
            rgba[offset++] = (byte)(argb >> 16);
            rgba[offset++] = (byte)(argb >> 8);
            rgba[offset++] = (byte)argb;
            rgba[offset++] = (byte)(argb >> 24);
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
    public WebpVp8lHeader(int width, int height, bool alphaUsed, int bitsConsumed) {
        Width = width;
        Height = height;
        AlphaUsed = alphaUsed;
        BitsConsumed = bitsConsumed;
    }

    public int Width { get; }
    public int Height { get; }
    public bool AlphaUsed { get; }
    public int BitsConsumed { get; }
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

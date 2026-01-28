using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8L lossless encoder (subset) for RGBA images.
/// </summary>
internal static class WebpVp8lEncoder {
    private const int LiteralAlphabetSize = 256;
    private const int LengthPrefixCount = 24;
    private const int GreenAlphabetBase = LiteralAlphabetSize + LengthPrefixCount; // 280
    private const int MaxPrefixBits = 15;
    private const int MaxPaletteSize = 16;
    private const int MaxBackwardDistance = 4096;
    private const int DistanceMapSize = 120;
    private const int MaxColorCacheBits = 11;
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

    public static bool TryEncodeLiteralRgba32(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        out byte[] webp,
        out string reason) {
        webp = Array.Empty<byte>();
        reason = string.Empty;

        if (width <= 0 || height <= 0) {
            reason = "Width and height must be positive.";
            return false;
        }

        var minStride = checked(width * 4);
        if (stride < minStride) {
            reason = "Stride is smaller than width * 4.";
            return false;
        }

        var requiredBytes = checked((height - 1) * stride + minStride);
        if (rgba.Length < requiredBytes) {
            reason = "Input RGBA buffer is too small for the provided dimensions/stride.";
            return false;
        }

        var best = Array.Empty<byte>();
        var bestLength = int.MaxValue;
        var bestReason = "Managed WebP encode failed.";
        var succeeded = false;

        if (TryEncodeWithColorIndexing(rgba, width, height, stride, out var candidate, out var candidateReason)) {
            succeeded = true;
            best = candidate;
            bestLength = candidate.Length;
        } else if (!string.IsNullOrEmpty(candidateReason)) {
            bestReason = candidateReason;
        }

        if (TryEncodeWithPredictorTransform(rgba, width, height, stride, out candidate, out candidateReason)) {
            if (!succeeded || candidate.Length < bestLength) {
                best = candidate;
                bestLength = candidate.Length;
                succeeded = true;
            }
        } else if (!succeeded && !string.IsNullOrEmpty(candidateReason)) {
            bestReason = candidateReason;
        }

        if (TryEncodeWithoutTransforms(rgba, width, height, stride, out candidate, out candidateReason)) {
            if (!succeeded || candidate.Length < bestLength) {
                best = candidate;
                bestLength = candidate.Length;
                succeeded = true;
            }
        } else if (!succeeded && !string.IsNullOrEmpty(candidateReason)) {
            bestReason = candidateReason;
        }

        if (!succeeded) {
            reason = bestReason;
            return false;
        }

        webp = best;
        return true;
    }

    private static bool TryEncodeWithoutTransforms(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        out byte[] webp,
        out string reason) {
        webp = Array.Empty<byte>();
        reason = string.Empty;

        var alphaUsed = ComputeAlphaUsed(rgba, width, height, stride);

        var writer = new WebpBitWriter();
        WriteHeader(writer, width, height, alphaUsed);

        // Transform section present, but empty.
        writer.WriteBits(0, 1);

        if (!TryWriteImageCore(writer, rgba, width, height, stride, out reason)) return false;

        webp = WriteWebpContainer(writer.ToArray());
        return true;
    }

    private static bool TryEncodeWithColorIndexing(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        out byte[] webp,
        out string reason) {
        webp = Array.Empty<byte>();
        reason = string.Empty;

        if (!TryCollectPalette(rgba, width, height, stride, MaxPaletteSize, out var palette)) {
            return false;
        }

        var widthBits = GetColorIndexWidthBits(palette.Length);
        var group = widthBits == 0 ? 1 : 1 << widthBits;
        var encodedWidth = widthBits == 0 ? width : (width + group - 1) >> widthBits;
        var encodedStride = checked(encodedWidth * 4);

        // If indexing does not shrink the main image width, the palette
        // transform overhead is not worth it for this minimal encoder.
        if (encodedWidth >= width) {
            return false;
        }

        var indexedRgba = BuildIndexedImageRgba(rgba, width, height, stride, palette, widthBits, encodedWidth);
        var paletteDeltasRgba = BuildPaletteDeltaRgba(palette);

        // Palette subimage: no header, no transforms.
        var paletteWriter = new WebpBitWriter();
        if (!TryWriteImageCore(paletteWriter, paletteDeltasRgba, palette.Length, height: 1, palette.Length * 4, out reason)) return false;

        var alphaUsed = ComputeAlphaUsed(rgba, width, height, stride);

        var writer = new WebpBitWriter();
        WriteHeader(writer, width, height, alphaUsed);

        // Transform section: color indexing with inline palette subimage.
        writer.WriteBits(1, 1); // has transform
        writer.WriteBits(3, 2); // color indexing transform
        writer.WriteBits(palette.Length - 1, 8);
        writer.Append(paletteWriter);
        writer.WriteBits(0, 1); // no more transforms

        if (!TryWriteImageCore(writer, indexedRgba, encodedWidth, height, encodedStride, out reason)) return false;

        webp = WriteWebpContainer(writer.ToArray());
        return true;
    }

    private static bool TryEncodeWithPredictorTransform(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        out byte[] webp,
        out string reason) {
        webp = Array.Empty<byte>();
        reason = string.Empty;

        if (width <= 0 || height <= 0) return false;

        const int sizeBits = 2; // 4x4 predictor blocks.
        var blockSize = 1 << sizeBits;
        var transformWidth = (width + blockSize - 1) >> sizeBits;
        var transformHeight = (height + blockSize - 1) >> sizeBits;
        if (transformWidth <= 0 || transformHeight <= 0) return false;

        var pixelCount = checked(width * height);
        var pixels = new int[pixelCount];
        FillArgbPixels(rgba, width, height, stride, pixels);
        var useSubtractGreen = ShouldApplySubtractGreen(pixels);
        if (useSubtractGreen) {
            ApplySubtractGreen(pixels);
        }

        var modes = ChoosePredictorModes(pixels, width, height, sizeBits, transformWidth, transformHeight);
        var predictorRgba = BuildPredictorImage(modes, transformWidth, transformHeight);
        var residualRgba = BuildResidualImage(pixels, width, height, sizeBits, transformWidth, modes);

        var predictorWriter = new WebpBitWriter();
        if (!TryWriteImageCore(predictorWriter, predictorRgba, transformWidth, transformHeight, transformWidth * 4, out reason)) {
            return false;
        }

        var alphaUsed = ComputeAlphaUsed(rgba, width, height, stride);
        var writer = new WebpBitWriter();
        WriteHeader(writer, width, height, alphaUsed);

        // Transform section: optional subtract-green, then predictor transform.
        writer.WriteBits(1, 1); // has transform
        if (useSubtractGreen) {
            writer.WriteBits(2, 2); // subtract-green transform
            writer.WriteBits(1, 1); // another transform follows
        }

        writer.WriteBits(0, 2); // predictor transform
        writer.WriteBits(sizeBits - 2, 3);
        writer.Append(predictorWriter);
        writer.WriteBits(0, 1); // no more transforms

        if (!TryWriteImageCore(writer, residualRgba, width, height, width * 4, out reason)) {
            return false;
        }

        webp = WriteWebpContainer(writer.ToArray());
        return true;
    }

    private static void WriteHeader(WebpBitWriter writer, int width, int height, bool alphaUsed) {
        // VP8L header.
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(width - 1, 14);
        writer.WriteBits(height - 1, 14);
        writer.WriteBits(alphaUsed ? 1 : 0, 1);
        writer.WriteBits(0, 3); // version
    }

    private static bool TryWriteImageCore(
        WebpBitWriter writer,
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        out string reason) {
        reason = string.Empty;

        var pixelCount = checked(width * height);
        var pixels = new int[pixelCount];
        FillArgbPixels(rgba, width, height, stride, pixels);

        var colorCacheBits = ChooseColorCacheBits(pixels);
        var colorCacheSize = colorCacheBits == 0 ? 0 : 1 << colorCacheBits;

        // Color cache flag + bits, then no meta prefix codes.
        if (colorCacheBits > 0) {
            writer.WriteBits(1, 1);
            writer.WriteBits(colorCacheBits, 4);
        } else {
            writer.WriteBits(0, 1);
        }
        writer.WriteBits(0, 1);

        var tokens = colorCacheBits > 0
            ? BuildTokensWithColorCache(pixels, width, colorCacheBits)
            : BuildSmallDistanceTokens(pixels);
        if (!TryWriteTokensWithPrefixCodes(writer, tokens, width, colorCacheSize, out reason)) {
            // Fall back to literal-only encoding if our constrained backref path
            // cannot be expressed with the current prefix-code strategy.
            tokens = BuildLiteralTokens(pixels);
            return TryWriteTokensWithPrefixCodes(writer, tokens, width, colorCacheSize: 0, out reason);
        }

        return true;
    }

    private static bool ComputeAlphaUsed(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        for (var y = 0; y < height; y++) {
            var src = y * stride + 3;
            for (var x = 0; x < width; x++) {
                if (rgba[src] != 255) return true;
                src += 4;
            }
        }
        return false;
    }

    private static bool TryCollectPalette(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        int maxPaletteSize,
        out int[] palette) {
        var unique = new HashSet<int>();
        for (var y = 0; y < height; y++) {
            var src = y * stride;
            for (var x = 0; x < width; x++) {
                var color = PackArgb(rgba[src], rgba[src + 1], rgba[src + 2], rgba[src + 3]);
                unique.Add(color);
                if (unique.Count > maxPaletteSize) {
                    palette = Array.Empty<int>();
                    return false;
                }
                src += 4;
            }
        }

        palette = new int[unique.Count];
        unique.CopyTo(palette);
        Array.Sort(palette);
        return palette.Length > 0;
    }

    private static bool PaletteUsesAlpha(ReadOnlySpan<int> palette) {
        for (var i = 0; i < palette.Length; i++) {
            var a = (palette[i] >> 24) & 0xFF;
            if (a != 255) return true;
        }
        return false;
    }

    private static byte[] BuildIndexedImageRgba(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        ReadOnlySpan<int> palette,
        int widthBits,
        int encodedWidth) {
        var paletteIndex = new Dictionary<int, int>(palette.Length);
        for (var i = 0; i < palette.Length; i++) {
            paletteIndex[palette[i]] = i;
        }

        var encodedStride = checked(encodedWidth * 4);
        var encoded = new byte[checked(height * encodedStride)];

        if (widthBits == 0) {
            for (var y = 0; y < height; y++) {
                var src = y * stride;
                var dst = y * encodedStride;
                for (var x = 0; x < width; x++) {
                    var color = PackArgb(rgba[src], rgba[src + 1], rgba[src + 2], rgba[src + 3]);
                    var index = paletteIndex[color];
                    encoded[dst] = 0;
                    encoded[dst + 1] = (byte)index;
                    encoded[dst + 2] = 0;
                    encoded[dst + 3] = 255;
                    src += 4;
                    dst += 4;
                }
            }
            return encoded;
        }

        var indicesPerPixel = 1 << widthBits;
        var bitsPerIndex = 8 >> widthBits;
        var indexMask = (1 << bitsPerIndex) - 1;

        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            var dstRow = y * encodedStride;
            for (var xSub = 0; xSub < encodedWidth; xSub++) {
                var baseX = xSub << widthBits;
                var packed = 0;
                for (var i = 0; i < indicesPerPixel; i++) {
                    var x = baseX + i;
                    if (x >= width) break;
                    var src = srcRow + (x * 4);
                    var color = PackArgb(rgba[src], rgba[src + 1], rgba[src + 2], rgba[src + 3]);
                    var index = paletteIndex[color] & indexMask;
                    packed |= index << (i * bitsPerIndex);
                }

                var dst = dstRow + (xSub * 4);
                encoded[dst] = 0;
                encoded[dst + 1] = (byte)packed;
                encoded[dst + 2] = 0;
                encoded[dst + 3] = 255;
            }
        }

        return encoded;
    }

    private static byte[] BuildPaletteDeltaRgba(ReadOnlySpan<int> palette) {
        var deltas = new byte[checked(palette.Length * 4)];

        var prevA = 0;
        var prevR = 0;
        var prevG = 0;
        var prevB = 0;

        for (var i = 0; i < palette.Length; i++) {
            var color = palette[i];
            var a = (color >> 24) & 0xFF;
            var r = (color >> 16) & 0xFF;
            var g = (color >> 8) & 0xFF;
            var b = color & 0xFF;

            var deltaA = (a - prevA) & 0xFF;
            var deltaR = (r - prevR) & 0xFF;
            var deltaG = (g - prevG) & 0xFF;
            var deltaB = (b - prevB) & 0xFF;

            var dst = i * 4;
            deltas[dst] = (byte)deltaR;
            deltas[dst + 1] = (byte)deltaG;
            deltas[dst + 2] = (byte)deltaB;
            deltas[dst + 3] = (byte)deltaA;

            prevA = a;
            prevR = r;
            prevG = g;
            prevB = b;
        }

        return deltas;
    }

    private static int GetColorIndexWidthBits(int colorTableSize) {
        if (colorTableSize <= 2) return 3;
        if (colorTableSize <= 4) return 2;
        if (colorTableSize <= 16) return 1;
        return 0;
    }

    private static int PackArgb(int r, int g, int b, int a) {
        return ((a & 0xFF) << 24)
            | ((r & 0xFF) << 16)
            | ((g & 0xFF) << 8)
            | (b & 0xFF);
    }

    private static void FillArgbPixels(ReadOnlySpan<byte> rgba, int width, int height, int stride, Span<int> pixels) {
        var pos = 0;
        for (var y = 0; y < height; y++) {
            var src = y * stride;
            for (var x = 0; x < width; x++) {
                pixels[pos++] = PackArgb(rgba[src], rgba[src + 1], rgba[src + 2], rgba[src + 3]);
                src += 4;
            }
        }
    }

    private static int[] ChoosePredictorModes(
        int[] pixels,
        int width,
        int height,
        int sizeBits,
        int transformWidth,
        int transformHeight) {
        var modes = new int[transformWidth * transformHeight];
        if (pixels.Length == 0) return modes;

        var blockSize = 1 << sizeBits;
        var candidateModes = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        for (var by = 0; by < transformHeight; by++) {
            var y0 = by * blockSize;
            for (var bx = 0; bx < transformWidth; bx++) {
                var x0 = bx * blockSize;
                var bestMode = 0;
                var bestScore = long.MaxValue;

                for (var m = 0; m < candidateModes.Length; m++) {
                    var mode = candidateModes[m];
                    long score = 0;
                    for (var y = 0; y < blockSize; y++) {
                        var py = y0 + y;
                        if (py >= height) break;
                        var rowIndex = py * width;
                        for (var x = 0; x < blockSize; x++) {
                            var px = x0 + x;
                            if (px >= width) break;
                            var index = rowIndex + px;
                            var predicted = PredictPixel(pixels, width, px, py, mode);
                            var value = pixels[index];
                            score += ChannelAbsDiff(value, predicted);
                        }
                    }

                    if (score < bestScore) {
                        bestScore = score;
                        bestMode = mode;
                        if (score == 0) break;
                    }
                }

                modes[by * transformWidth + bx] = bestMode;
            }
        }

        return modes;
    }

    private static bool ShouldApplySubtractGreen(int[] pixels) {
        if (pixels.Length == 0) return false;

        const int sampleLimit = 4096;
        var uniqueOriginal = new HashSet<int>();
        var uniqueTransformed = new HashSet<int>();

        var step = pixels.Length <= sampleLimit ? 1 : Math.Max(1, pixels.Length / sampleLimit);
        for (var i = 0; i < pixels.Length; i += step) {
            var pixel = pixels[i];
            uniqueOriginal.Add(pixel);
            uniqueTransformed.Add(ApplySubtractGreen(pixel));
        }

        return uniqueTransformed.Count + 4 < uniqueOriginal.Count;
    }

    private static void ApplySubtractGreen(int[] pixels) {
        for (var i = 0; i < pixels.Length; i++) {
            pixels[i] = ApplySubtractGreen(pixels[i]);
        }
    }

    private static int ApplySubtractGreen(int pixel) {
        var a = (byte)(pixel >> 24);
        var r = (byte)(pixel >> 16);
        var g = (byte)(pixel >> 8);
        var b = (byte)pixel;
        r = (byte)(r - g);
        b = (byte)(b - g);
        return PackArgb(r, g, b, a);
    }

    private static byte[] BuildPredictorImage(int[] modes, int width, int height) {
        var rgba = new byte[width * height * 4];
        var pos = 0;
        for (var i = 0; i < modes.Length; i++) {
            rgba[pos] = 0;
            rgba[pos + 1] = (byte)(modes[i] & 0xFF);
            rgba[pos + 2] = 0;
            rgba[pos + 3] = 255;
            pos += 4;
        }
        return rgba;
    }

    private static byte[] BuildResidualImage(
        int[] pixels,
        int width,
        int height,
        int sizeBits,
        int transformWidth,
        int[] modes) {
        var rgba = new byte[width * height * 4];
        var pos = 0;
        for (var y = 0; y < height; y++) {
            var rowIndex = y * width;
            var blockY = y >> sizeBits;
            for (var x = 0; x < width; x++) {
                var blockX = x >> sizeBits;
                var modeIndex = blockY * transformWidth + blockX;
                var mode = (uint)modeIndex < (uint)modes.Length ? modes[modeIndex] : 0;
                var index = rowIndex + x;
                var predicted = PredictPixel(pixels, width, x, y, mode);
                var residual = SubtractPixelsModulo(pixels[index], predicted);

                rgba[pos] = (byte)((residual >> 16) & 0xFF);
                rgba[pos + 1] = (byte)((residual >> 8) & 0xFF);
                rgba[pos + 2] = (byte)(residual & 0xFF);
                rgba[pos + 3] = (byte)((residual >> 24) & 0xFF);
                pos += 4;
            }
        }
        return rgba;
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

    private static int SubtractPixelsModulo(int value, int predicted) {
        var a = (byte)((value >> 24) - (predicted >> 24));
        var r = (byte)((value >> 16) - (predicted >> 16));
        var g = (byte)((value >> 8) - (predicted >> 8));
        var b = (byte)(value - predicted);
        return PackArgb(r, g, b, a);
    }

    private static long ChannelAbsDiff(int value, int predicted) {
        var da = Abs(((value >> 24) & 0xFF) - ((predicted >> 24) & 0xFF));
        var dr = Abs(((value >> 16) & 0xFF) - ((predicted >> 16) & 0xFF));
        var dg = Abs(((value >> 8) & 0xFF) - ((predicted >> 8) & 0xFF));
        var db = Abs((value & 0xFF) - (predicted & 0xFF));
        return da + dr + dg + db;
    }

    private static int Average2(int a, int b) {
        var aa = ((a >> 24) & 0xFF) + ((b >> 24) & 0xFF);
        var ar = ((a >> 16) & 0xFF) + ((b >> 16) & 0xFF);
        var ag = ((a >> 8) & 0xFF) + ((b >> 8) & 0xFF);
        var ab = (a & 0xFF) + (b & 0xFF);
        return PackArgb(ar >> 1, ag >> 1, ab >> 1, aa >> 1);
    }

    private static int ClampAddSubtractFull(int a, int b, int c) {
        var aa = Clamp(((a >> 24) & 0xFF) + ((b >> 24) & 0xFF) - ((c >> 24) & 0xFF));
        var ar = Clamp(((a >> 16) & 0xFF) + ((b >> 16) & 0xFF) - ((c >> 16) & 0xFF));
        var ag = Clamp(((a >> 8) & 0xFF) + ((b >> 8) & 0xFF) - ((c >> 8) & 0xFF));
        var ab = Clamp((a & 0xFF) + (b & 0xFF) - (c & 0xFF));
        return PackArgb(ar, ag, ab, aa);
    }

    private static int ClampAddSubtractHalf(int a, int b) {
        var aa = Clamp(((a >> 24) & 0xFF) + ((((a >> 24) & 0xFF) - ((b >> 24) & 0xFF)) >> 1));
        var ar = Clamp(((a >> 16) & 0xFF) + ((((a >> 16) & 0xFF) - ((b >> 16) & 0xFF)) >> 1));
        var ag = Clamp(((a >> 8) & 0xFF) + ((((a >> 8) & 0xFF) - ((b >> 8) & 0xFF)) >> 1));
        var ab = Clamp((a & 0xFF) + (((a & 0xFF) - (b & 0xFF)) >> 1));
        return PackArgb(ar, ag, ab, aa);
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

    private static Token[] BuildLiteralTokens(ReadOnlySpan<int> pixels) {
        var tokens = new Token[pixels.Length];
        for (var i = 0; i < pixels.Length; i++) {
            tokens[i] = Token.Literal(pixels[i]);
        }
        return tokens;
    }

    private static Token[] BuildTokensWithColorCache(ReadOnlySpan<int> pixels, int width, int colorCacheBits) {
        if (pixels.Length == 0) return Array.Empty<Token>();
        if (colorCacheBits is < 1 or > MaxColorCacheBits) return BuildSmallDistanceTokens(pixels);

        var cacheSize = 1 << colorCacheBits;
        var cache = new int[cacheSize];
        var cacheValid = new bool[cacheSize];
        var list = new List<Token>(pixels.Length);

        var pos = 0;
        var maxDistance = Math.Min(MaxBackwardDistance, pixels.Length);
        const int maxMatchLength = 256;
        while (pos < pixels.Length) {
            var pixel = pixels[pos];
            var cacheIndex = GetColorCacheIndex(pixel, colorCacheBits);
            if (cacheValid[cacheIndex] && cache[cacheIndex] == pixel) {
                list.Add(Token.CacheIndex(cacheIndex));
                cache[cacheIndex] = pixel;
                cacheValid[cacheIndex] = true;
                pos++;
                continue;
            }

            var bestLength = 0;
            var bestDistance = 0;
            var bestScore = int.MinValue;

            var remaining = pixels.Length - pos;
            var maxLen = remaining < MaxBackwardDistance ? remaining : MaxBackwardDistance;
            if (maxLen > maxMatchLength) maxLen = maxMatchLength;

            var maxSearchDistance = pos < maxDistance ? pos : maxDistance;
            for (var distance = 1; distance <= maxSearchDistance; distance++) {
                var requiredLength = distance <= 8 ? 3
                    : distance <= 64 ? 4
                    : distance <= 256 ? 5
                    : 6;

                var length = 0;
                while (length < maxLen && pixels[pos + length] == pixels[pos - distance + length]) {
                    length++;
                }

                if (length < requiredLength) continue;

                var score = length - requiredLength;
                if (score > bestScore || (score == bestScore && distance < bestDistance)) {
                    bestScore = score;
                    bestLength = length;
                    bestDistance = distance;
                }
            }

            if (bestLength > 0) {
                list.Add(Token.BackReference(bestDistance, bestLength));
                for (var i = 0; i < bestLength; i++) {
                    var copied = pixels[pos + i];
                    var idx = GetColorCacheIndex(copied, colorCacheBits);
                    cache[idx] = copied;
                    cacheValid[idx] = true;
                }
                pos += bestLength;
                continue;
            }

            list.Add(Token.Literal(pixel));
            cache[cacheIndex] = pixel;
            cacheValid[cacheIndex] = true;
            pos++;
        }

        return list.ToArray();
    }

    private static Token[] BuildSmallDistanceTokens(ReadOnlySpan<int> pixels) {
        if (pixels.Length == 0) return Array.Empty<Token>();

        var list = new List<Token>(pixels.Length);
        var pos = 0;
        var maxDistance = Math.Min(MaxBackwardDistance, pixels.Length);
        const int maxMatchLength = 256;
        while (pos < pixels.Length) {
            var bestLength = 0;
            var bestDistance = 0;
            var bestScore = int.MinValue;

            var remaining = pixels.Length - pos;
            var maxLen = remaining < MaxBackwardDistance ? remaining : MaxBackwardDistance;
            if (maxLen > maxMatchLength) maxLen = maxMatchLength;

            var maxSearchDistance = pos < maxDistance ? pos : maxDistance;
            for (var distance = 1; distance <= maxSearchDistance; distance++) {

                var requiredLength = distance <= 8 ? 3
                    : distance <= 64 ? 4
                    : distance <= 256 ? 5
                    : 6;

                var length = 0;
                while (length < maxLen && pixels[pos + length] == pixels[pos - distance + length]) {
                    length++;
                }

                if (length < requiredLength) continue;

                var score = length - requiredLength;
                if (score > bestScore || (score == bestScore && distance < bestDistance)) {
                    bestScore = score;
                    bestLength = length;
                    bestDistance = distance;
                }
            }

            if (bestLength > 0) {
                list.Add(Token.BackReference(bestDistance, bestLength));
                pos += bestLength;
                continue;
            }

            list.Add(Token.Literal(pixels[pos]));
            pos++;
        }

        return list.ToArray();
    }

    private static int ChooseColorCacheBits(ReadOnlySpan<int> pixels) {
        if (pixels.Length < 16) return 0;

        var bestBits = 0;
        var bestHits = 0;
        for (var bits = 4; bits <= 7; bits++) {
            var hits = CountColorCacheHits(pixels, bits);
            if (hits > bestHits) {
                bestHits = hits;
                bestBits = bits;
            }
        }

        if (bestBits == 0) return 0;
        var threshold = pixels.Length / 8;
        return bestHits >= threshold ? bestBits : 0;
    }

    private static int CountColorCacheHits(ReadOnlySpan<int> pixels, int colorCacheBits) {
        if (colorCacheBits is < 1 or > MaxColorCacheBits) return 0;
        var cacheSize = 1 << colorCacheBits;
        var cache = new int[cacheSize];
        var valid = new bool[cacheSize];
        var hits = 0;
        for (var i = 0; i < pixels.Length; i++) {
            var pixel = pixels[i];
            var index = GetColorCacheIndex(pixel, colorCacheBits);
            if (valid[index] && cache[index] == pixel) {
                hits++;
            } else {
                cache[index] = pixel;
                valid[index] = true;
            }
        }
        return hits;
    }

    private static int GetColorCacheIndex(int pixel, int colorCacheBits) {
        var hash = (uint)pixel * ColorCacheHashMultiplier;
        return (int)(hash >> (32 - colorCacheBits));
    }

    private static bool TryWriteTokensWithPrefixCodes(
        WebpBitWriter writer,
        ReadOnlySpan<Token> tokens,
        int width,
        int colorCacheSize,
        out string reason) {
        reason = string.Empty;

        if (!TryCollectLiteralChannelValues(tokens, out var uniqueR, out var uniqueG, out var uniqueB, out var uniqueA)) {
            reason = "Failed to collect literal channel values.";
            return false;
        }

        if (!TryCollectLengthPrefixes(tokens, out var lengthPrefixes)) {
            reason = "Failed to compute length prefixes for back-references.";
            return false;
        }

        var hasBackrefs = lengthPrefixes.Length > 0;
        var cacheIndexes = Array.Empty<int>();
        var hasColorCache = colorCacheSize > 0 && TryCollectCacheIndexes(tokens, colorCacheSize, out cacheIndexes);

        Codebook greenBook;
        if (hasBackrefs || hasColorCache) {
            var cacheSymbols = hasColorCache ? cacheIndexes : Array.Empty<int>();
            if (!TryWriteGreenPrefixCodeWithExtras(writer, uniqueG, lengthPrefixes, cacheSymbols, colorCacheSize, out greenBook, out reason)) {
                return false;
            }
        } else {
            if (!TryWriteChannelPrefixCode(writer, GreenAlphabetBase, uniqueG, fixedLiteralCount: LiteralAlphabetSize, out greenBook, out reason)) return false;
        }

        if (!TryWriteChannelPrefixCode(writer, LiteralAlphabetSize, uniqueR, fixedLiteralCount: LiteralAlphabetSize, out var redBook, out reason)) return false;
        if (!TryWriteChannelPrefixCode(writer, LiteralAlphabetSize, uniqueB, fixedLiteralCount: LiteralAlphabetSize, out var blueBook, out reason)) return false;
        if (!TryWriteChannelPrefixCode(writer, LiteralAlphabetSize, uniqueA, fixedLiteralCount: LiteralAlphabetSize, out var alphaBook, out reason)) return false;

        if (hasBackrefs) {
            if (!TryWriteDistancePrefixCodeForBackrefs(writer, out var distanceBook, out reason)) return false;
            return TryEncodeTokens(writer, tokens, width, greenBook, redBook, blueBook, alphaBook, distanceBook, out reason);
        }

        WriteSimplePrefixCode(writer, symbols: new byte[] { 0 }); // distance unused
        return TryEncodeTokens(writer, tokens, width, greenBook, redBook, blueBook, alphaBook, distanceBook: default, out reason);
    }

    private static bool TryCollectLiteralChannelValues(
        ReadOnlySpan<Token> tokens,
        out byte[] uniqueR,
        out byte[] uniqueG,
        out byte[] uniqueB,
        out byte[] uniqueA) {
        var seenR = new bool[256];
        var seenG = new bool[256];
        var seenB = new bool[256];
        var seenA = new bool[256];

        var any = false;
        for (var i = 0; i < tokens.Length; i++) {
            var token = tokens[i];
            if (token.Kind != TokenKind.Literal) continue;
            any = true;
            var argb = token.LiteralArgb;
            seenA[(argb >> 24) & 0xFF] = true;
            seenR[(argb >> 16) & 0xFF] = true;
            seenG[(argb >> 8) & 0xFF] = true;
            seenB[argb & 0xFF] = true;
        }

        if (!any) {
            uniqueR = Array.Empty<byte>();
            uniqueG = Array.Empty<byte>();
            uniqueB = Array.Empty<byte>();
            uniqueA = Array.Empty<byte>();
            return false;
        }

        uniqueR = BuildSymbolList(seenR);
        uniqueG = BuildSymbolList(seenG);
        uniqueB = BuildSymbolList(seenB);
        uniqueA = BuildSymbolList(seenA);
        return uniqueR.Length > 0 && uniqueG.Length > 0 && uniqueB.Length > 0 && uniqueA.Length > 0;
    }

    private static bool TryCollectLengthPrefixes(ReadOnlySpan<Token> tokens, out int[] lengthPrefixes) {
        var set = new HashSet<int>();
        for (var i = 0; i < tokens.Length; i++) {
            var token = tokens[i];
            if (token.Kind != TokenKind.BackReference) continue;

            if (!TryEncodePrefixValue(token.Length, maxPrefix: LengthPrefixCount - 1, out var prefix, out _, out _)) {
                lengthPrefixes = Array.Empty<int>();
                return false;
            }
            set.Add(prefix);
        }

        lengthPrefixes = new int[set.Count];
        set.CopyTo(lengthPrefixes);
        Array.Sort(lengthPrefixes);
        return true;
    }

    private static bool TryCollectCacheIndexes(ReadOnlySpan<Token> tokens, int colorCacheSize, out int[] cacheIndexes) {
        var set = new HashSet<int>();
        for (var i = 0; i < tokens.Length; i++) {
            var token = tokens[i];
            if (token.Kind != TokenKind.CacheIndex) continue;
            if (token.CacheIndexValue is < 0 or >= 4096) continue;
            if (token.CacheIndexValue >= colorCacheSize) continue;
            set.Add(token.CacheIndexValue);
        }

        cacheIndexes = new int[set.Count];
        set.CopyTo(cacheIndexes);
        Array.Sort(cacheIndexes);
        return true;
    }

    private static byte[] BuildSymbolList(bool[] seen) {
        var count = 0;
        for (var i = 0; i < seen.Length; i++) {
            if (seen[i]) count++;
        }

        var symbols = new byte[count];
        var idx = 0;
        for (var i = 0; i < seen.Length; i++) {
            if (!seen[i]) continue;
            symbols[idx++] = (byte)i;
        }
        return symbols;
    }

    private static bool TryWriteGreenPrefixCodeWithExtras(
        WebpBitWriter writer,
        ReadOnlySpan<byte> literalGreens,
        ReadOnlySpan<int> lengthPrefixes,
        ReadOnlySpan<int> cacheIndexes,
        int colorCacheSize,
        out Codebook greenBook,
        out string reason) {
        reason = string.Empty;
        greenBook = default;

        var required = new HashSet<int>();
        for (var i = 0; i < literalGreens.Length; i++) {
            required.Add(literalGreens[i]);
        }
        for (var i = 0; i < lengthPrefixes.Length; i++) {
            required.Add(LiteralAlphabetSize + lengthPrefixes[i]);
        }
        for (var i = 0; i < cacheIndexes.Length; i++) {
            var index = cacheIndexes[i];
            if (index < 0 || (colorCacheSize > 0 && index >= colorCacheSize)) continue;
            required.Add(GreenAlphabetBase + index);
        }

        var greenAlphabetSize = GreenAlphabetBase + (colorCacheSize > 0 ? colorCacheSize : 0);
        if (!TryBuildZeroOrEightLengths(greenAlphabetSize, required, out var lengths, out reason)) {
            return false;
        }

        WriteNormalPrefixCodeZeroOrEight(writer, lengths);
        if (!TryBuildCodebookFromLengths(lengths, out greenBook, out reason)) {
            return false;
        }

        return true;
    }

    private static bool TryBuildZeroOrEightLengths(
        int alphabetSize,
        HashSet<int> required,
        out byte[] lengths,
        out string reason) {
        lengths = new byte[alphabetSize];
        reason = string.Empty;

        if (required.Count > LiteralAlphabetSize) {
            reason = "Required green symbols exceed the 0/8-length strategy limit.";
            return false;
        }

        var selected = new HashSet<int>(required);
        for (var sym = 0; sym < alphabetSize && selected.Count < LiteralAlphabetSize; sym++) {
            selected.Add(sym);
        }

        if (selected.Count != LiteralAlphabetSize) {
            reason = "Unable to select exactly 256 symbols for 0/8-length coding.";
            return false;
        }

        foreach (var sym in selected) {
            lengths[sym] = 8;
        }

        return true;
    }

    private static void WriteNormalPrefixCodeZeroOrEight(WebpBitWriter writer, ReadOnlySpan<byte> lengths) {
        // Normal prefix code flag.
        writer.WriteBits(0, 1);

        // Provide 12 code-length code lengths so we can reach symbol 8 in the order.
        const int numCodeLengthCodes = 12;
        writer.WriteBits(numCodeLengthCodes - 4, 4);

        // Code-length code with just symbols {0, 8}.
        writer.WriteBits(0, 3); // 17
        writer.WriteBits(0, 3); // 18
        writer.WriteBits(1, 3); // 0
        writer.WriteBits(0, 3); // 1
        writer.WriteBits(0, 3); // 2
        writer.WriteBits(0, 3); // 3
        writer.WriteBits(0, 3); // 4
        writer.WriteBits(0, 3); // 5
        writer.WriteBits(0, 3); // 16
        writer.WriteBits(0, 3); // 6
        writer.WriteBits(0, 3); // 7
        writer.WriteBits(1, 3); // 8

        // use_max_symbol = false (max symbol is full alphabet size).
        writer.WriteBits(0, 1);

        for (var i = 0; i < lengths.Length; i++) {
            writer.WriteBits(lengths[i] == 0 ? 0 : 1, 1);
        }
    }

    private static bool TryBuildCodebookFromLengths(ReadOnlySpan<byte> lengths, out Codebook codebook, out string reason) {
        reason = string.Empty;
        codebook = default;

        if (!WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out _)) {
            reason = "Failed to build prefix code from generated lengths.";
            return false;
        }

        var codes = BuildCanonicalCodewords(lengths, MaxPrefixBits);
        codebook = new Codebook(codes, singleSymbol: -1);
        return true;
    }

    private static bool TryWriteDistancePrefixCodeForBackrefs(WebpBitWriter writer, out Codebook distanceBook, out string reason) {
        reason = string.Empty;
        distanceBook = default;

        var lengths = BuildFixedDistanceCodeLengths();
        WriteNormalPrefixCodeWithFixedLengths(writer, lengths);
        return TryBuildCodebookFromLengths(lengths, out distanceBook, out reason);
    }

    private static bool TryEncodeTokens(
        WebpBitWriter writer,
        ReadOnlySpan<Token> tokens,
        int width,
        Codebook greenBook,
        Codebook redBook,
        Codebook blueBook,
        Codebook alphaBook,
        Codebook distanceBook,
        out string reason) {
        reason = string.Empty;
        var distanceCodeMap = BuildDistanceCodeMap(width);

        for (var i = 0; i < tokens.Length; i++) {
            var token = tokens[i];
            if (token.Kind == TokenKind.Literal) {
                var argb = token.LiteralArgb;
                var a = (argb >> 24) & 0xFF;
                var r = (argb >> 16) & 0xFF;
                var g = (argb >> 8) & 0xFF;
                var b = argb & 0xFF;

                if (!greenBook.TryWrite(writer, g)) {
                    reason = "Green channel symbol not present in prefix code.";
                    return false;
                }
                if (!redBook.TryWrite(writer, r)) {
                    reason = "Red channel symbol not present in prefix code.";
                    return false;
                }
                if (!blueBook.TryWrite(writer, b)) {
                    reason = "Blue channel symbol not present in prefix code.";
                    return false;
                }
                if (!alphaBook.TryWrite(writer, a)) {
                    reason = "Alpha channel symbol not present in prefix code.";
                    return false;
                }

                continue;
            }

            if (token.Kind == TokenKind.CacheIndex) {
                var cacheGreenSymbol = GreenAlphabetBase + token.CacheIndexValue;
                if (!greenBook.TryWrite(writer, cacheGreenSymbol)) {
                    reason = "Green cache symbol not present in prefix code.";
                    return false;
                }
                continue;
            }

            if (!TryEncodePrefixValue(token.Length, maxPrefix: LengthPrefixCount - 1, out var lengthPrefix, out var lengthExtraBits, out var lengthExtraValue)) {
                reason = "Back-reference length is not encodable.";
                return false;
            }

            var greenSymbol = LiteralAlphabetSize + lengthPrefix;
            if (!greenBook.TryWrite(writer, greenSymbol)) {
                reason = "Green length-prefix symbol not present in prefix code.";
                return false;
            }
            if (lengthExtraBits > 0) {
                writer.WriteBits(lengthExtraValue, lengthExtraBits);
            }

            if (token.Distance is < 1 or > MaxBackwardDistance) {
                reason = $"Only distances 1..{MaxBackwardDistance} are supported in this encoder step.";
                return false;
            }

            var distanceCode = ResolveDistanceCode(distanceCodeMap, token.Distance);
            if (!TryEncodePrefixValue(distanceCode, maxPrefix: 39, out var distancePrefix, out var distanceExtraBits, out var distanceExtraValue)) {
                reason = "Distance code could not be encoded.";
                return false;
            }

            if (!distanceBook.TryWrite(writer, distancePrefix)) {
                reason = "Distance prefix symbol not present in prefix code.";
                return false;
            }

            if (distanceExtraBits > 0) {
                writer.WriteBits(distanceExtraValue, distanceExtraBits);
            }
        }

        return true;
    }

    private static bool TryEncodePrefixValue(int value, int maxPrefix, out int prefix, out int extraBits, out int extraValue) {
        prefix = 0;
        extraBits = 0;
        extraValue = 0;
        if (value <= 0) return false;

        for (var p = 0; p <= maxPrefix; p++) {
            if (p < 4) {
                var v = p + 1;
                if (v == value) {
                    prefix = p;
                    return true;
                }
                continue;
            }

            var bits = (p - 2) >> 1;
            if (bits < 0 || bits > 24) return false;
            var offset = (2 + (p & 1)) << bits;
            var min = offset + 1;
            var max = offset + (1 << bits);
            if (value < min || value > max) continue;

            prefix = p;
            extraBits = bits;
            extraValue = value - min;
            return true;
        }

        return false;
    }

    private static int[] BuildDistanceCodeMap(int width) {
        if (width <= 0) return Array.Empty<int>();
        var map = new int[MaxBackwardDistance + 1];
        for (var i = 0; i < DistanceMap.Length; i++) {
            var (xi, yi) = DistanceMap[i];
            var mapped = xi + (long)yi * width;
            if (mapped <= 0 || mapped > MaxBackwardDistance) continue;
            var distance = (int)mapped;
            if (map[distance] == 0) {
                map[distance] = i + 1;
            }
        }
        return map;
    }

    private static int ResolveDistanceCode(int[] distanceCodeMap, int distance) {
        if (distanceCodeMap.Length > distance && distanceCodeMap[distance] > 0) {
            return distanceCodeMap[distance];
        }
        return DistanceMapSize + distance;
    }

    private readonly struct Token {
        private Token(TokenKind kind, int literalArgb, int length, int distance, int cacheIndex) {
            Kind = kind;
            LiteralArgb = literalArgb;
            Length = length;
            Distance = distance;
            CacheIndexValue = cacheIndex;
        }

        public TokenKind Kind { get; }
        public int LiteralArgb { get; }
        public int Length { get; }
        public int Distance { get; }
        public int CacheIndexValue { get; }

        public static Token Literal(int argb) => new Token(TokenKind.Literal, argb, length: 0, distance: 0, cacheIndex: 0);
        public static Token BackReference(int distance, int length) => new Token(TokenKind.BackReference, literalArgb: 0, length, distance, cacheIndex: 0);
        public static Token CacheIndex(int cacheIndex) => new Token(TokenKind.CacheIndex, literalArgb: 0, length: 0, distance: 0, cacheIndex);
    }

    private enum TokenKind {
        Literal = 0,
        BackReference = 1,
        CacheIndex = 2
    }

    private static bool TryCollectUniqueChannelValues(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        int channelOffset,
        out byte[] symbols,
        out string reason) {
        reason = string.Empty;
        var seen = new bool[256];
        var count = 0;

        for (var y = 0; y < height; y++) {
            var src = y * stride + channelOffset;
            for (var x = 0; x < width; x++) {
                var value = rgba[src];
                if (!seen[value]) {
                    seen[value] = true;
                    count++;
                }
                src += 4;
            }
        }

        if (count == 0) {
            symbols = Array.Empty<byte>();
            reason = "Image contains no pixels.";
            return false;
        }

        symbols = new byte[count];
        var index = 0;
        for (var i = 0; i < seen.Length; i++) {
            if (!seen[i]) continue;
            symbols[index++] = (byte)i;
        }
        Array.Sort(symbols);
        return true;
    }

    private static bool TryWriteChannelPrefixCode(
        WebpBitWriter writer,
        int alphabetSize,
        ReadOnlySpan<byte> uniqueSymbols,
        int fixedLiteralCount,
        out Codebook codebook,
        out string reason) {
        reason = string.Empty;
        codebook = default;

        if (uniqueSymbols.Length <= 2) {
            WriteSimplePrefixCode(writer, uniqueSymbols);
            return TryBuildSimpleCodebook(alphabetSize, uniqueSymbols, out codebook, out reason);
        }

        WriteFixedNormalPrefixCode(writer, alphabetSize, fixedLiteralCount);
        return TryBuildFixedNormalCodebook(alphabetSize, fixedLiteralCount, out codebook, out reason);
    }

    private static void WriteSimplePrefixCode(WebpBitWriter writer, ReadOnlySpan<byte> symbols) {
        // Simple prefix code flag.
        writer.WriteBits(1, 1);

        var twoSymbols = symbols.Length == 2 ? 1 : 0;
        writer.WriteBits(twoSymbols, 1);

        // Use 8-bit symbol encoding for clarity and stability.
        writer.WriteBits(1, 1);
        writer.WriteBits(symbols[0], 8);
        if (twoSymbols != 0) {
            writer.WriteBits(symbols[1], 8);
        }
    }

    private static void WriteFixedNormalPrefixCode(WebpBitWriter writer, int alphabetSize, int fixedLiteralCount) {
        // Normal prefix code flag.
        writer.WriteBits(0, 1);

        // Provide 12 code-length code lengths so we can reach symbol 8 in the order.
        const int numCodeLengthCodes = 12;
        writer.WriteBits(numCodeLengthCodes - 4, 4);

        // Code-length-code order prefix (first 12 symbols):
        // 17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8
        // We define a trivial code-length code with just symbols {0, 8}.
        writer.WriteBits(0, 3); // 17
        writer.WriteBits(0, 3); // 18
        writer.WriteBits(1, 3); // 0
        writer.WriteBits(0, 3); // 1
        writer.WriteBits(0, 3); // 2
        writer.WriteBits(0, 3); // 3
        writer.WriteBits(0, 3); // 4
        writer.WriteBits(0, 3); // 5
        writer.WriteBits(0, 3); // 16
        writer.WriteBits(0, 3); // 6
        writer.WriteBits(0, 3); // 7
        writer.WriteBits(1, 3); // 8

        // use_max_symbol = false (max symbol is full alphabet size).
        writer.WriteBits(0, 1);

        // Encode code lengths: 0 => bit 0, 8 => bit 1.
        for (var symbol = 0; symbol < alphabetSize; symbol++) {
            var len = symbol < fixedLiteralCount ? 8 : 0;
            writer.WriteBits(len == 0 ? 0 : 1, 1);
        }
    }

    private static bool TryBuildSimpleCodebook(int alphabetSize, ReadOnlySpan<byte> symbols, out Codebook codebook, out string reason) {
        reason = string.Empty;
        codebook = default;
        if (alphabetSize <= 0) {
            reason = "Alphabet size must be positive.";
            return false;
        }
        if (symbols.Length is < 1 or > 2) {
            reason = "Simple prefix codes support only 1 or 2 symbols.";
            return false;
        }

        var lengths = new byte[alphabetSize];
        for (var i = 0; i < symbols.Length; i++) {
            var symbol = symbols[i];
            if (symbol >= alphabetSize) {
                reason = "Symbol exceeds alphabet size.";
                return false;
            }
            lengths[symbol] = 1;
        }

        if (!WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out _)) {
            reason = "Failed to build a valid prefix code for the channel.";
            return false;
        }

        var nonZeroCount = symbols.Length;
        if (nonZeroCount == 1) {
            codebook = new Codebook(Array.Empty<Codeword>(), singleSymbol: symbols[0]);
            return true;
        }

        var codes = BuildCanonicalCodewords(lengths, MaxPrefixBits);
        codebook = new Codebook(codes, singleSymbol: -1);
        return true;
    }

    private static bool TryBuildFixedNormalCodebook(int alphabetSize, int fixedLiteralCount, out Codebook codebook, out string reason) {
        reason = string.Empty;
        codebook = default;

        if (alphabetSize <= 0) {
            reason = "Alphabet size must be positive.";
            return false;
        }
        if (fixedLiteralCount != LiteralAlphabetSize) {
            reason = "Fixed normal prefix codes currently require exactly 256 literal symbols.";
            return false;
        }
        if (fixedLiteralCount > alphabetSize) {
            reason = "Fixed literal count exceeds alphabet size.";
            return false;
        }

        var lengths = new byte[alphabetSize];
        for (var i = 0; i < fixedLiteralCount; i++) {
            lengths[i] = 8;
        }

        if (!WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out _)) {
            reason = "Failed to build fixed normal prefix code.";
            return false;
        }

        var codes = BuildCanonicalCodewords(lengths, MaxPrefixBits);
        codebook = new Codebook(codes, singleSymbol: -1);
        return true;
    }

    private static bool TryBuildFixedNormalCodebookAnyCount(int alphabetSize, int fixedLiteralCount, out Codebook codebook, out string reason) {
        reason = string.Empty;
        codebook = default;

        if (alphabetSize <= 0) {
            reason = "Alphabet size must be positive.";
            return false;
        }
        if (fixedLiteralCount <= 0 || fixedLiteralCount > alphabetSize) {
            reason = "Fixed literal count exceeds alphabet size.";
            return false;
        }

        var lengths = new byte[alphabetSize];
        for (var i = 0; i < fixedLiteralCount; i++) {
            lengths[i] = 8;
        }

        if (!WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out _)) {
            reason = "Failed to build fixed normal prefix code.";
            return false;
        }

        var codes = BuildCanonicalCodewords(lengths, MaxPrefixBits);
        codebook = new Codebook(codes, singleSymbol: -1);
        return true;
    }

    private static byte[] BuildFixedDistanceCodeLengths() {
        var lengths = new byte[40];
        for (var i = 0; i < lengths.Length; i++) {
            lengths[i] = (byte)(i < 24 ? 5 : 6);
        }
        return lengths;
    }

    private static void WriteNormalPrefixCodeWithFixedLengths(WebpBitWriter writer, ReadOnlySpan<byte> lengths) {
        // Normal prefix code flag.
        writer.WriteBits(0, 1);

        // We need code-length symbols 5 and 6, so include up to symbol 6 in order.
        const int numCodeLengthCodes = 10; // 17,18,0,1,2,3,4,5,16,6
        writer.WriteBits(numCodeLengthCodes - 4, 4);

        // Code-length code lengths: only symbols 5 and 6 have length 1.
        writer.WriteBits(0, 3); // 17
        writer.WriteBits(0, 3); // 18
        writer.WriteBits(0, 3); // 0
        writer.WriteBits(0, 3); // 1
        writer.WriteBits(0, 3); // 2
        writer.WriteBits(0, 3); // 3
        writer.WriteBits(0, 3); // 4
        writer.WriteBits(1, 3); // 5
        writer.WriteBits(0, 3); // 16
        writer.WriteBits(1, 3); // 6

        // use_max_symbol = false (max symbol is full alphabet size).
        writer.WriteBits(0, 1);

        for (var i = 0; i < lengths.Length; i++) {
            writer.WriteBits(lengths[i] == 5 ? 0 : 1, 1);
        }
    }

    private static Codeword[] BuildCanonicalCodewords(ReadOnlySpan<byte> lengths, int maxBits) {
        var counts = new int[maxBits + 1];
        for (var i = 0; i < lengths.Length; i++) {
            var len = lengths[i];
            if (len == 0) continue;
            counts[len]++;
        }

        var nextCode = new int[maxBits + 1];
        var canonical = 0;
        for (var bits = 1; bits <= maxBits; bits++) {
            canonical = (canonical + counts[bits - 1]) << 1;
            nextCode[bits] = canonical;
        }

        var codewords = new Codeword[lengths.Length];
        for (var sym = 0; sym < lengths.Length; sym++) {
            var len = lengths[sym];
            if (len == 0) continue;

            var codeValue = nextCode[len]++;
            var reversed = ReverseBits(codeValue, len);
            codewords[sym] = new Codeword(reversed, len);
        }
        return codewords;
    }

    private static int ReverseBits(int value, int length) {
        var result = 0;
        for (var i = 0; i < length; i++) {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }
        return result;
    }

    private static byte[] WriteWebpContainer(byte[] vp8lPayload) {
        using var ms = new MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0); // placeholder
        WriteAscii(ms, "WEBP");
        WriteAscii(ms, "VP8L");
        WriteU32LE(ms, (uint)vp8lPayload.Length);
        ms.Write(vp8lPayload, 0, vp8lPayload.Length);
        if ((vp8lPayload.Length & 1) != 0) {
            ms.WriteByte(0);
        }

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    private static void WriteAscii(Stream stream, string text) {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteU32LE(Stream stream, uint value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    private static void WriteU32LE(byte[] buffer, int offset, uint value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private readonly struct Codebook {
        private readonly Codeword[] _codewords;
        private readonly int _singleSymbol;

        public Codebook(Codeword[] codewords, int singleSymbol) {
            _codewords = codewords;
            _singleSymbol = singleSymbol;
        }

        public bool TryWrite(WebpBitWriter writer, int symbol) {
            if (_singleSymbol >= 0) {
                return symbol == _singleSymbol;
            }

            if ((uint)symbol >= (uint)_codewords.Length) return false;
            var codeword = _codewords[symbol];
            if (codeword.Length <= 0) return false;

            writer.WriteBits(codeword.Bits, codeword.Length);
            return true;
        }
    }

    private readonly struct Codeword {
        public Codeword(int bits, int length) {
            Bits = bits;
            Length = length;
        }

        public int Bits { get; }
        public int Length { get; }
    }
}

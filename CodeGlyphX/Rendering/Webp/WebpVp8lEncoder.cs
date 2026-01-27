using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8L lossless encoder scaffold (literal-only, no transforms/LZ77).
/// </summary>
internal static class WebpVp8lEncoder {
    private const int LiteralAlphabetSize = 256;
    private const int LengthPrefixCount = 24;
    private const int GreenAlphabetBase = LiteralAlphabetSize + LengthPrefixCount; // 280
    private const int MaxPrefixBits = 15;
    private const int MaxPaletteSize = 16;

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

        // Prefer a small palette transform when it applies; fall back to
        // literal-only encoding without transforms.
        if (TryEncodeWithColorIndexing(rgba, width, height, stride, out webp, out reason)) {
            return true;
        }

        return TryEncodeWithoutTransforms(rgba, width, height, stride, out webp, out reason);
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

        // For very small palettes simple literal coding is already tiny.
        if (palette.Length <= 2) {
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

        // Palette subimage: no transform section.
        var paletteWriter = new WebpBitWriter();
        var paletteAlphaUsed = PaletteUsesAlpha(palette);
        WriteHeader(paletteWriter, palette.Length, height: 1, paletteAlphaUsed);
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

        // No color cache, no meta prefix codes.
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 0, out var uniqueR, out reason)) return false;
        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 1, out var uniqueG, out reason)) return false;
        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 2, out var uniqueB, out reason)) return false;
        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 3, out var uniqueA, out reason)) return false;

        // Prefix codes group: per-channel choose simple codes when possible,
        // otherwise fall back to a fixed normal-prefix-code layout.
        if (!TryWriteChannelPrefixCode(writer, GreenAlphabetBase, uniqueG, fixedLiteralCount: LiteralAlphabetSize, out var greenBook, out reason)) return false;
        if (!TryWriteChannelPrefixCode(writer, LiteralAlphabetSize, uniqueR, fixedLiteralCount: LiteralAlphabetSize, out var redBook, out reason)) return false;
        if (!TryWriteChannelPrefixCode(writer, LiteralAlphabetSize, uniqueB, fixedLiteralCount: LiteralAlphabetSize, out var blueBook, out reason)) return false;
        if (!TryWriteChannelPrefixCode(writer, LiteralAlphabetSize, uniqueA, fixedLiteralCount: LiteralAlphabetSize, out var alphaBook, out reason)) return false;
        WriteSimplePrefixCode(writer, symbols: new byte[] { 0 }); // distance

        // Encode literal pixels (no LZ77, no cache, no transforms).
        for (var y = 0; y < height; y++) {
            var src = y * stride;
            for (var x = 0; x < width; x++) {
                var r = rgba[src];
                var g = rgba[src + 1];
                var b = rgba[src + 2];
                var a = rgba[src + 3];

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

                src += 4;
            }
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

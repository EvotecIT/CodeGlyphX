using System;
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
    private const int DistanceAlphabetSize = 40;
    private const int MaxPrefixBits = 15;

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

        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 0, out var uniqueR, out reason)) return false;
        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 1, out var uniqueG, out reason)) return false;
        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 2, out var uniqueB, out reason)) return false;
        if (!TryCollectUniqueChannelValues(rgba, width, height, stride, channelOffset: 3, out var uniqueA, out reason)) return false;

        var alphaUsed = uniqueA.Length != 1 || uniqueA[0] != 255;

        var bitWriter = new WebpBitWriter();

        // VP8L header.
        bitWriter.WriteBits(0x2F, 8);
        bitWriter.WriteBits(width - 1, 14);
        bitWriter.WriteBits(height - 1, 14);
        bitWriter.WriteBits(alphaUsed ? 1 : 0, 1);
        bitWriter.WriteBits(0, 3); // version

        // No transforms, no color cache, no meta prefix codes.
        bitWriter.WriteBits(0, 1);
        bitWriter.WriteBits(0, 1);
        bitWriter.WriteBits(0, 1);

        // Prefix codes group: simple codes only (one or two symbols).
        WriteSimplePrefixCode(bitWriter, uniqueG);
        WriteSimplePrefixCode(bitWriter, uniqueR);
        WriteSimplePrefixCode(bitWriter, uniqueB);
        WriteSimplePrefixCode(bitWriter, uniqueA);
        WriteSimplePrefixCode(bitWriter, symbols: new byte[] { 0 }); // distance

        // Build canonical codebooks for encoding symbols.
        if (!TryBuildSimpleCodebook(GreenAlphabetBase, uniqueG, out var greenBook, out reason)) return false;
        if (!TryBuildSimpleCodebook(LiteralAlphabetSize, uniqueR, out var redBook, out reason)) return false;
        if (!TryBuildSimpleCodebook(LiteralAlphabetSize, uniqueB, out var blueBook, out reason)) return false;
        if (!TryBuildSimpleCodebook(LiteralAlphabetSize, uniqueA, out var alphaBook, out reason)) return false;

        // Encode literal pixels (no LZ77, no cache, no transforms).
        for (var y = 0; y < height; y++) {
            var src = y * stride;
            for (var x = 0; x < width; x++) {
                var r = rgba[src];
                var g = rgba[src + 1];
                var b = rgba[src + 2];
                var a = rgba[src + 3];

                if (!greenBook.TryWrite(bitWriter, g)) {
                    reason = "Green channel symbol not present in prefix code.";
                    return false;
                }
                if (!redBook.TryWrite(bitWriter, r)) {
                    reason = "Red channel symbol not present in prefix code.";
                    return false;
                }
                if (!blueBook.TryWrite(bitWriter, b)) {
                    reason = "Blue channel symbol not present in prefix code.";
                    return false;
                }
                if (!alphaBook.TryWrite(bitWriter, a)) {
                    reason = "Alpha channel symbol not present in prefix code.";
                    return false;
                }

                src += 4;
            }
        }

        var payload = bitWriter.ToArray();
        webp = WriteWebpContainer(payload);
        return true;
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
                    if (count > 2) {
                        symbols = Array.Empty<byte>();
                        reason = "Managed VP8L encoder currently supports up to 2 unique values per channel.";
                        return false;
                    }
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


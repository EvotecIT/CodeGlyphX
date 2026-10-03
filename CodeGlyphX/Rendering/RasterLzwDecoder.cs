using System;

namespace CodeGlyphX.Rendering;

/// <summary>Bounded LZW raster expansion shared by PDF and TIFF; callers select bit order and code-width timing.</summary>
internal static class RasterLzwDecoder {
    internal static byte[] Decode(ReadOnlySpan<byte> src, int expected, int earlyChange, bool msb) {
        DecodeGuards.EnsureByteCount(expected, "LZW output exceeds decode limits.");
        var prefix = new short[4096];
        var suffix = new byte[4096];
        var stack = new byte[4096];
        var output = new byte[expected];

        for (var i = 0; i < 256; i++) {
            prefix[i] = -1;
            suffix[i] = (byte)i;
        }

        long bitPos = 0;
        var codeSize = 9;
        var clear = 256;
        var eoi = 257;
        var nextCode = 258;
        var oldCode = -1;
        var outIndex = 0;
        byte firstChar = 0;

        while (true) {
            var code = msb ? ReadBitsMsb(src, ref bitPos, codeSize) : ReadBitsLsb(src, ref bitPos, codeSize);
            if (code < 0) break;
            if (code == clear) {
                codeSize = 9;
                nextCode = 258;
                oldCode = -1;
                continue;
            }
            if (code == eoi) {
                break;
            }

            var stackTop = ExpandCode(code, nextCode, oldCode, prefix, suffix, stack, ref firstChar);

            while (stackTop > 0) {
                if (outIndex >= output.Length) throw new FormatException("LZW output too large.");
                output[outIndex++] = stack[--stackTop];
            }

            if (oldCode >= 0 && nextCode < 4096) {
                prefix[nextCode] = (short)oldCode;
                suffix[nextCode] = firstChar;
                nextCode++;
                if (nextCode == (1 << codeSize) - earlyChange && codeSize < 12) codeSize++;
            }
            oldCode = code;
        }

        if (outIndex != output.Length) throw new FormatException("LZW output truncated.");
        return output;
    }

    private static int ExpandCode(int code, int nextCode, int oldCode, short[] prefix, byte[] suffix, byte[] stack, ref byte firstChar) {
        var stackTop = 0;
        if (code > nextCode) throw new FormatException("Invalid LZW code.");
        if (code == nextCode) {
            if (oldCode < 0) throw new FormatException("Invalid LZW stream.");
            stack[stackTop++] = firstChar;
            code = oldCode;
        }
        while (code >= 256) {
            if ((uint)code >= 4096) throw new FormatException("Invalid LZW code.");
            if (stackTop >= stack.Length) throw new FormatException("Invalid LZW stack overflow.");
            stack[stackTop++] = suffix[code];
            code = prefix[code];
        }
        firstChar = (byte)code;
        if (stackTop >= stack.Length) throw new FormatException("Invalid LZW stack overflow.");
        stack[stackTop++] = firstChar;
        return stackTop;
    }

    private static int ReadBitsMsb(ReadOnlySpan<byte> data, ref long bitPos, int bitCount) {
        var totalBits = (long)data.Length * 8;
        if ((long)bitPos + bitCount > totalBits) return -1;
        var value = 0;
        for (var i = 0; i < bitCount; i++) {
            var bitIndex = bitPos + i;
            var byteIndex = (int)(bitIndex >> 3);
            var shift = 7 - (int)(bitIndex & 7);
            var bit = (data[byteIndex] >> shift) & 1;
            value = (value << 1) | bit;
        }
        bitPos += bitCount;
        return value;
    }

    private static int ReadBitsLsb(ReadOnlySpan<byte> data, ref long bitPos, int bitCount) {
        var totalBits = (long)data.Length * 8;
        if ((long)bitPos + bitCount > totalBits) return -1;
        var value = 0;
        for (var i = 0; i < bitCount; i++) {
            var bitIndex = bitPos + i;
            var byteIndex = (int)(bitIndex >> 3);
            var shift = (int)(bitIndex & 7);
            var bit = (data[byteIndex] >> shift) & 1;
            value |= bit << i;
        }
        bitPos += bitCount;
        return value;
    }

}


using System;
using System.IO;

namespace CodeGlyphX.Rendering.Pdf;

public static partial class PdfReader {
    private static int GetFilterOutputLimit(PdfImageInfo info, bool final) {
        var maxBytes = ImageReader.EffectiveMaxDecodedBytes;
        var limit = maxBytes > 0 ? (int)Math.Min(maxBytes, int.MaxValue) : int.MaxValue;
        if (final) {
            if (!TryGetExpectedDecodedLength(info, out var expected)) throw new FormatException("Invalid PDF raster dimensions or sample layout.");
            limit = Math.Min(limit, expected);
        }
        return limit;
    }

    private static void EnsureFilterOutputSize(long length, int limit) {
        if (length > limit) {
            ImageReader.ReportLimitViolation(new ImageDecodeLimitViolation(ImageDecodeLimitKind.MaxDecodedBytes, limit, length, ImageFormat.Pdf));
            throw new FormatException("PDF filter output exceeds size limits.");
        }
    }

    private static bool TryDecodeAscii85(ReadOnlySpan<byte> src, int limit, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        using var ms = new MemoryStream();
        uint tuple = 0;
        var count = 0;
        for (var i = 0; i < src.Length; i++) {
            var b = src[i];
            if (b == (byte)'~') {
                if (i + 1 < src.Length && src[i + 1] == (byte)'>') {
                    i++;
                    break;
                }
            }
            if (b == (byte)'z') {
                if (count != 0) return false;
                EnsureFilterOutputSize(ms.Length + 4, limit);
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                continue;
            }
            if (b <= 32) continue;
            if (b < (byte)'!' || b > (byte)'u') return false;
            tuple = tuple * 85 + (uint)(b - (byte)'!');
            count++;
            if (count == 5) {
                EnsureFilterOutputSize(ms.Length + 4, limit);
                WriteTuple(ms, tuple);
                tuple = 0;
                count = 0;
            }
        }

        if (count > 0) {
            for (var i = count; i < 5; i++) {
                tuple = tuple * 85 + 84;
            }
            var buffer = new byte[4];
            buffer[0] = (byte)(tuple >> 24);
            buffer[1] = (byte)(tuple >> 16);
            buffer[2] = (byte)(tuple >> 8);
            buffer[3] = (byte)tuple;
            EnsureFilterOutputSize(ms.Length + count - 1, limit);
            ms.Write(buffer, 0, count - 1);
        }

        decoded = ms.ToArray();
        return true;
    }

    private static bool TryDecodeAsciiHex(ReadOnlySpan<byte> src, int limit, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        var buffer = new System.Collections.Generic.List<byte>();
        var highNibble = -1;
        for (var i = 0; i < src.Length; i++) {
            var b = src[i];
            if (b == (byte)'>') {
                if (highNibble >= 0) {
                    EnsureFilterOutputSize((long)buffer.Count + 1, limit);
                    buffer.Add((byte)(highNibble << 4));
                }
                decoded = buffer.ToArray();
                return true;
            }
            if (b <= 32) continue;
            var nibble = HexToNibble(b);
            if (nibble < 0) return false;
            if (highNibble < 0) {
                highNibble = nibble;
            } else {
                EnsureFilterOutputSize((long)buffer.Count + 1, limit);
                buffer.Add((byte)((highNibble << 4) | nibble));
                highNibble = -1;
            }
        }
        if (highNibble >= 0) {
            EnsureFilterOutputSize((long)buffer.Count + 1, limit);
            buffer.Add((byte)(highNibble << 4));
        }
        decoded = buffer.ToArray();
        return true;
    }

    private static bool TryDecodeLzw(PdfImageInfo info, ReadOnlySpan<byte> src, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        if (!TryGetExpectedDecodedLength(info, out var expected)) return false;
        try {
            if (info.LzwEarlyChange == 0 || info.LzwEarlyChange == 1) {
                decoded = DecompressLzw(src, expected, info.LzwEarlyChange);
            } else {
                decoded = DecompressLzwCompat(src, expected);
            }
            return true;
        } catch (FormatException) {
            if (info.LzwEarlyChange == 0 || info.LzwEarlyChange == 1) {
                try {
                    decoded = DecompressLzwCompat(src, expected);
                    return true;
                } catch (FormatException) {
                    return false;
                }
            }
            return false;
        }
    }

    private static bool TryGetExpectedDecodedLength(PdfImageInfo info, out int expected) {
        expected = 0;
        if (!DecodeGuards.TryEnsurePixelCount(info.Width, info.Height, out _)) return false;

        var colors = info.Colors;
        if (info.IsImageMask || info.ColorSpaceKind == PdfColorSpaceKind.Indexed) {
            colors = 1;
        } else if (colors <= 0) {
            colors = info.ColorSpaceKind switch {
                PdfColorSpaceKind.DeviceGray => 1,
                PdfColorSpaceKind.DeviceRGB => 3,
                PdfColorSpaceKind.DeviceCMYK => 4,
                _ => 4
            };
        }
        if (colors <= 0) return false;

        var bits = info.BitsPerComponent;
        if (bits != 1 && bits != 2 && bits != 4 && bits != 8 && bits != 16) return false;

        try {
            if (bits == 8) {
                var rowSize = checked(info.Width * colors);
                expected = info.Predictor >= 10
                    ? checked((rowSize + 1) * info.Height)
                    : checked(rowSize * info.Height);
                DecodeGuards.EnsureByteCount(expected, PdfImageLimitMessage);
                return true;
            }
            var rowBits = checked(info.Width * colors * bits);
            var rowBytes = (rowBits + 7) / 8;
            expected = info.Predictor >= 10
                ? checked((rowBytes + 1) * info.Height)
                : checked(rowBytes * info.Height);
            DecodeGuards.EnsureByteCount(expected, PdfImageLimitMessage);
            return true;
        } catch (OverflowException) {
            expected = 0;
            return false;
        }
    }

    private static byte[] DecompressLzwCompat(ReadOnlySpan<byte> src, int expected) {
        FormatException? last = null;
        var attempts = new[] { 1, 0 };
        foreach (var earlyChange in attempts) {
            try {
                return DecompressLzw(src, expected, earlyChange);
            } catch (FormatException ex) {
                last = ex;
            }
        }
        throw last ?? new FormatException("Invalid PDF LZW data.");
    }

    private static byte[] DecompressLzw(ReadOnlySpan<byte> src, int expected, int earlyChange) {
        DecodeGuards.EnsureByteCount(expected, "PDF LZW output exceeds size limits.");
        var prefix = new short[4096];
        var suffix = new byte[4096];
        var stack = new byte[4096];
        var output = new byte[expected];

        for (var i = 0; i < 256; i++) {
            prefix[i] = -1;
            suffix[i] = (byte)i;
        }

        var bitPos = 0;
        var codeSize = 9;
        const int clear = 256;
        const int eoi = 257;
        var nextCode = 258;
        var oldCode = -1;
        var outIndex = 0;
        byte firstChar = 0;

        while (true) {
            var code = ReadBitsMsb(src, ref bitPos, codeSize);
            if (code < 0) break;
            if (code == clear) {
                codeSize = 9;
                nextCode = 258;
                oldCode = -1;
                continue;
            }
            if (code == eoi) break;

            var inCode = code;
            var stackTop = 0;
            if (code >= nextCode) {
                if (oldCode < 0) throw new FormatException("Invalid PDF LZW stream.");
                if (stackTop >= stack.Length) throw new FormatException("Invalid PDF LZW stack overflow.");
                stack[stackTop++] = firstChar;
                code = oldCode;
            }

            while (code >= 256) {
                if ((uint)code >= 4096) throw new FormatException("Invalid PDF LZW code.");
                if (stackTop >= stack.Length) throw new FormatException("Invalid PDF LZW stack overflow.");
                stack[stackTop++] = suffix[code];
                code = prefix[code];
            }

            firstChar = (byte)code;
            if (stackTop >= stack.Length) throw new FormatException("Invalid PDF LZW stack overflow.");
            stack[stackTop++] = firstChar;

            while (stackTop > 0) {
                if (outIndex >= output.Length) throw new FormatException("PDF LZW output too large.");
                output[outIndex++] = stack[--stackTop];
            }

            if (oldCode >= 0) {
                if (nextCode < 4096) {
                    prefix[nextCode] = (short)oldCode;
                    suffix[nextCode] = firstChar;
                    nextCode++;
                    if (nextCode == (1 << codeSize) - earlyChange && codeSize < 12) {
                        codeSize++;
                    }
                }
            }
            oldCode = inCode;
        }

        if (outIndex != output.Length) throw new FormatException("PDF LZW output truncated.");
        return output;
    }

    private static int ReadBitsMsb(ReadOnlySpan<byte> data, ref int bitPos, int bitCount) {
        var totalBits = data.Length * 8;
        if (bitPos + bitCount > totalBits) return -1;
        var value = 0;
        for (var i = 0; i < bitCount; i++) {
            var bitIndex = bitPos + i;
            var byteIndex = bitIndex >> 3;
            var shift = 7 - (bitIndex & 7);
            var bit = (data[byteIndex] >> shift) & 1;
            value = (value << 1) | bit;
        }
        bitPos += bitCount;
        return value;
    }

    private static void WriteTuple(Stream stream, uint tuple) {
        stream.WriteByte((byte)(tuple >> 24));
        stream.WriteByte((byte)(tuple >> 16));
        stream.WriteByte((byte)(tuple >> 8));
        stream.WriteByte((byte)tuple);
    }

    private static bool TryDecodeRunLength(ReadOnlySpan<byte> src, int limit, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        using var ms = new MemoryStream();
        var i = 0;
        while (i < src.Length) {
            var b = src[i++];
            if (b == 128) break;
            if (b <= 127) {
                var count = b + 1;
                EnsureFilterOutputSize(ms.Length + count, limit);
                if (i + count > src.Length) return false;
                var chunk = new byte[count];
                src.Slice(i, count).CopyTo(chunk);
                ms.Write(chunk, 0, chunk.Length);
                i += count;
            } else {
                var count = 257 - b;
                EnsureFilterOutputSize(ms.Length + count, limit);
                if (i >= src.Length) return false;
                var value = src[i++];
                for (var j = 0; j < count; j++) {
                    ms.WriteByte(value);
                }
            }
        }
        decoded = ms.ToArray();
        return true;
    }

}

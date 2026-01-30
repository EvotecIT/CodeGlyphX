using System;
using System.Collections.Generic;
using System.IO;

namespace CodeGlyphX.Rendering.Gif;

/// <summary>
/// Writes single-frame GIF images from RGBA buffers with palette optimization and dithering.
/// </summary>
public static class GifWriter {
    private const int MaxCodeSize = 12;
    private const int PaletteSize = 256;

    /// <summary>
    /// Encodes an RGBA buffer into a GIF byte array.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes an RGBA buffer into a GIF stream.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (width > ushort.MaxValue || height > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(width), "GIF dimensions exceed 65535.");
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < stride * height) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var hasTransparency = false;
        var colorCounts = new Dictionary<int, int>(256);
        var tooManyColors = false;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var idx = row + x * 4;
                var a = rgba[idx + 3];
                if (a < 128) {
                    hasTransparency = true;
                    continue;
                }
                if (tooManyColors) continue;
                var key = (rgba[idx + 0] << 16) | (rgba[idx + 1] << 8) | rgba[idx + 2];
                if (colorCounts.TryGetValue(key, out var count)) {
                    colorCounts[key] = count + 1;
                } else {
                    if (colorCounts.Count >= PaletteSize) {
                        tooManyColors = true;
                    } else {
                        colorCounts[key] = 1;
                    }
                }
            }
        }

        var useExactPalette = !tooManyColors && (!hasTransparency || colorCounts.Count <= PaletteSize - 1);
        Dictionary<int, byte>? colorIndex = null;
        var transparentIndex = 0;
        var paletteSizePower = PaletteSize;
        byte[] palette;
        var minCodeSize = 8;

        if (useExactPalette) {
            colorIndex = new Dictionary<int, byte>(colorCounts.Count);
            var paletteIndex = 0;
            var paletteEntries = colorCounts.Count + (hasTransparency ? 1 : 0);
            paletteSizePower = NextPowerOfTwo(Math.Max(paletteEntries, 2), PaletteSize);
            minCodeSize = Math.Max(2, Log2(paletteSizePower));
            palette = new byte[paletteSizePower * 3];
            foreach (var entry in colorCounts) {
                palette[paletteIndex * 3 + 0] = (byte)((entry.Key >> 16) & 0xFF);
                palette[paletteIndex * 3 + 1] = (byte)((entry.Key >> 8) & 0xFF);
                palette[paletteIndex * 3 + 2] = (byte)(entry.Key & 0xFF);
                colorIndex[entry.Key] = (byte)paletteIndex;
                paletteIndex++;
            }
            if (hasTransparency) {
                transparentIndex = paletteIndex;
            }
        } else {
            palette = new byte[PaletteSize * 3];
            BuildFixedPalette(palette);
        }

        var counts = useExactPalette ? null : new int[PaletteSize];
        if (!useExactPalette) {
            for (var y = 0; y < height; y++) {
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    if (rgba[idx + 3] < 128) continue;
                    counts![Quantize(rgba[idx + 0], rgba[idx + 1], rgba[idx + 2])]++;
                }
            }
            transparentIndex = hasTransparency ? FindLeastUsedIndex(counts!) : 0;
        }

        var pixels = new byte[width * height];
        if (useExactPalette) {
            var dst = 0;
            for (var y = 0; y < height; y++) {
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    var a = rgba[idx + 3];
                    if (hasTransparency && a < 128) {
                        pixels[dst++] = (byte)transparentIndex;
                        continue;
                    }
                    var key = (rgba[idx + 0] << 16) | (rgba[idx + 1] << 8) | rgba[idx + 2];
                    pixels[dst++] = colorIndex![key];
                }
            }
        } else {
            var errR = new int[width + 1];
            var errG = new int[width + 1];
            var errB = new int[width + 1];
            var nextErrR = new int[width + 1];
            var nextErrG = new int[width + 1];
            var nextErrB = new int[width + 1];
            var dst = 0;

            for (var y = 0; y < height; y++) {
                Array.Clear(nextErrR, 0, nextErrR.Length);
                Array.Clear(nextErrG, 0, nextErrG.Length);
                Array.Clear(nextErrB, 0, nextErrB.Length);
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    var a = rgba[idx + 3];
                    if (hasTransparency && a < 128) {
                        pixels[dst++] = (byte)transparentIndex;
                        continue;
                    }

                    var r = ClampByte(rgba[idx + 0] + errR[x]);
                    var g = ClampByte(rgba[idx + 1] + errG[x]);
                    var b = ClampByte(rgba[idx + 2] + errB[x]);
                    var quantized = Quantize(r, g, b);
                    if (hasTransparency && quantized == transparentIndex) {
                        quantized = (quantized ^ 0x01) & 0xFF;
                    }
                    pixels[dst++] = (byte)quantized;

                    var (qr, qg, qb) = Dequantize(quantized);
                    var errRVal = r - qr;
                    var errGVal = g - qg;
                    var errBVal = b - qb;

                    if (x + 1 < width) {
                        errR[x + 1] += (errRVal * 7) / 16;
                        errG[x + 1] += (errGVal * 7) / 16;
                        errB[x + 1] += (errBVal * 7) / 16;
                    }
                    if (x > 0) {
                        nextErrR[x - 1] += (errRVal * 3) / 16;
                        nextErrG[x - 1] += (errGVal * 3) / 16;
                        nextErrB[x - 1] += (errBVal * 3) / 16;
                    }
                    nextErrR[x] += (errRVal * 5) / 16;
                    nextErrG[x] += (errGVal * 5) / 16;
                    nextErrB[x] += (errBVal * 5) / 16;
                    if (x + 1 < width) {
                        nextErrR[x + 1] += errRVal / 16;
                        nextErrG[x + 1] += errGVal / 16;
                        nextErrB[x + 1] += errBVal / 16;
                    }
                }

                var swapR = errR;
                var swapG = errG;
                var swapB = errB;
                errR = nextErrR;
                errG = nextErrG;
                errB = nextErrB;
                nextErrR = swapR;
                nextErrG = swapG;
                nextErrB = swapB;
            }
        }

        WriteHeader(stream, width, height, palette, paletteSizePower, hasTransparency, transparentIndex);
        WriteImage(stream, width, height, pixels, hasTransparency, transparentIndex, minCodeSize);
        stream.WriteByte(0x3B); // Trailer
    }

    private static void WriteHeader(Stream stream, int width, int height, byte[] palette, int paletteSize, bool hasTransparency, int transparentIndex) {
        WriteAscii(stream, "GIF89a");
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);

        const int colorResolution = 7; // 8 bits per channel
        var gctSize = Log2(paletteSize) - 1; // 2^(gctSize+1)
        var packed = (byte)(0x80 | (colorResolution << 4) | gctSize);
        stream.WriteByte(packed);
        stream.WriteByte((byte)(hasTransparency ? transparentIndex : 0)); // Background color index
        stream.WriteByte(0); // Pixel aspect ratio
        stream.Write(palette, 0, paletteSize * 3);
    }

    private static void WriteImage(Stream stream, int width, int height, byte[] pixels, bool hasTransparency, int transparentIndex, int minCodeSize) {
        if (hasTransparency) {
            stream.WriteByte(0x21); // Extension introducer
            stream.WriteByte(0xF9); // GCE label
            stream.WriteByte(4); // Block size
            stream.WriteByte(0x01); // Transparency flag
            WriteUInt16(stream, 0); // Delay
            stream.WriteByte((byte)transparentIndex);
            stream.WriteByte(0); // Block terminator
        }

        stream.WriteByte(0x2C); // Image descriptor
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);
        stream.WriteByte(0); // No local color table

        stream.WriteByte((byte)minCodeSize);
        var lzwData = EncodeLzw(pixels, minCodeSize);
        WriteSubBlocks(stream, lzwData);
    }

    private static void WriteSubBlocks(Stream stream, byte[] data) {
        var offset = 0;
        while (offset < data.Length) {
            var count = Math.Min(255, data.Length - offset);
            stream.WriteByte((byte)count);
            stream.Write(data, offset, count);
            offset += count;
        }
        stream.WriteByte(0); // Terminator
    }

    private static byte[] EncodeLzw(ReadOnlySpan<byte> indices, int minCodeSize) {
        if (indices.Length == 0) return Array.Empty<byte>();

        var clearCode = 1 << minCodeSize;
        var endCode = clearCode + 1;
        var nextCode = endCode + 1;
        var codeSize = minCodeSize + 1;

        var dict = new Dictionary<int, int>(4096);
        var output = new List<byte>(indices.Length);
        var bitBuffer = 0;
        var bitCount = 0;

        void WriteCode(int code) {
            bitBuffer |= code << bitCount;
            bitCount += codeSize;
            while (bitCount >= 8) {
                output.Add((byte)(bitBuffer & 0xFF));
                bitBuffer >>= 8;
                bitCount -= 8;
            }
        }

        void ResetDictionary() {
            dict.Clear();
            codeSize = minCodeSize + 1;
            nextCode = endCode + 1;
            WriteCode(clearCode);
        }

        ResetDictionary();

        var prefix = indices[0];
        for (var i = 1; i < indices.Length; i++) {
            var c = indices[i];
            var key = (prefix << 8) | c;
            if (dict.TryGetValue(key, out var code)) {
                prefix = code;
                continue;
            }

            WriteCode(prefix);
            if (nextCode < (1 << MaxCodeSize)) {
                dict[key] = nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < MaxCodeSize) {
                    codeSize++;
                }
            } else {
                ResetDictionary();
            }

            prefix = c;
        }

        WriteCode(prefix);
        WriteCode(endCode);
        if (bitCount > 0) {
            output.Add((byte)(bitBuffer & 0xFF));
        }
        return output.ToArray();
    }

    private static void BuildFixedPalette(byte[] palette) {
        var idx = 0;
        for (var r = 0; r < 8; r++) {
            var rr = (byte)(r * 255 / 7);
            for (var g = 0; g < 8; g++) {
                var gg = (byte)(g * 255 / 7);
                for (var b = 0; b < 4; b++) {
                    var bb = (byte)(b * 255 / 3);
                    palette[idx++] = rr;
                    palette[idx++] = gg;
                    palette[idx++] = bb;
                }
            }
        }
    }

    private static int Quantize(byte r, byte g, byte b) {
        var r3 = r >> 5;
        var g3 = g >> 5;
        var b2 = b >> 6;
        return (r3 << 5) | (g3 << 2) | b2;
    }

    private static (int r, int g, int b) Dequantize(int index) {
        var r3 = (index >> 5) & 0x7;
        var g3 = (index >> 2) & 0x7;
        var b2 = index & 0x3;
        return (r3 * 255 / 7, g3 * 255 / 7, b2 * 255 / 3);
    }

    private static int NextPowerOfTwo(int value, int max) {
        var result = 1;
        while (result < value && result < max) {
            result <<= 1;
        }
        return result > max ? max : result;
    }

    private static int Log2(int value) {
        var log = 0;
        while ((1 << log) < value) {
            log++;
        }
        return log;
    }

    private static byte ClampByte(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }

    private static int FindLeastUsedIndex(int[] counts) {
        var best = 0;
        var bestCount = counts[0];
        for (var i = 1; i < counts.Length; i++) {
            if (counts[i] < bestCount) {
                best = i;
                bestCount = counts[i];
                if (bestCount == 0) break;
            }
        }
        return best;
    }

    private static void WriteAscii(Stream stream, string text) {
        for (var i = 0; i < text.Length; i++) {
            stream.WriteByte((byte)text[i]);
        }
    }

    private static void WriteUInt16(Stream stream, ushort value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }
}

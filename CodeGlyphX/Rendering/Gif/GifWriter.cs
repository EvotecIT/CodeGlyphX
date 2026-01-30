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
        int[]? paletteMap = null;
        var transparentIndex = 0;
        var paletteSizePower = PaletteSize;
        var paletteCount = 0;
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
            paletteCount = paletteIndex;
            if (hasTransparency) {
                transparentIndex = paletteIndex;
            }
        } else {
            var histogram = BuildHistogram(rgba, width, height, stride);
            palette = BuildMedianCutPalette(histogram, hasTransparency ? PaletteSize - 1 : PaletteSize, out paletteCount);
            paletteSizePower = NextPowerOfTwo(Math.Max(paletteCount + (hasTransparency ? 1 : 0), 2), PaletteSize);
            minCodeSize = Math.Max(2, Log2(paletteSizePower));
            if (palette.Length < paletteSizePower * 3) {
                var padded = new byte[paletteSizePower * 3];
                Buffer.BlockCopy(palette, 0, padded, 0, palette.Length);
                palette = padded;
            }
            if (hasTransparency) {
                transparentIndex = paletteCount;
            }
            paletteMap = BuildPaletteMap(palette, paletteCount);
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
                    var qIdx = ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
                    var palIndex = paletteMap![qIdx];
                    pixels[dst++] = (byte)palIndex;

                    var baseIndex = palIndex * 3;
                    var pr = palette[baseIndex + 0];
                    var pg = palette[baseIndex + 1];
                    var pb = palette[baseIndex + 2];
                    var errRVal = r - pr;
                    var errGVal = g - pg;
                    var errBVal = b - pb;

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

    private static int[] BuildHistogram(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        var histogram = new int[32 * 32 * 32];
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var idx = row + x * 4;
                if (rgba[idx + 3] < 128) continue;
                var r5 = rgba[idx + 0] >> 3;
                var g5 = rgba[idx + 1] >> 3;
                var b5 = rgba[idx + 2] >> 3;
                var colorIndex = (r5 << 10) | (g5 << 5) | b5;
                histogram[colorIndex]++;
            }
        }
        return histogram;
    }

    private static byte[] BuildMedianCutPalette(int[] histogram, int maxColors, out int paletteCount) {
        var colors = new List<int>();
        for (var i = 0; i < histogram.Length; i++) {
            if (histogram[i] > 0) {
                colors.Add(i);
            }
        }

        if (colors.Count == 0) {
            paletteCount = 1;
            return new byte[] { 0, 0, 0 };
        }

        var buckets = new List<ColorBucket> { ColorBucket.Create(colors.ToArray(), histogram) };
        while (buckets.Count < maxColors) {
            var splitIndex = -1;
            var bestRange = -1;
            for (var i = 0; i < buckets.Count; i++) {
                var bucket = buckets[i];
                if (bucket.Count < 2) continue;
                var range = bucket.MaxRange;
                if (range > bestRange) {
                    bestRange = range;
                    splitIndex = i;
                }
            }

            if (splitIndex < 0) break;
            var target = buckets[splitIndex];
            var channel = target.LongestChannel;
            Array.Sort(target.Colors, 0, target.Count, new ChannelComparer(channel));

            var total = target.TotalCount;
            var cumulative = 0;
            var cut = 0;
            while (cut < target.Count - 1 && cumulative < total / 2) {
                cumulative += histogram[target.Colors[cut]];
                cut++;
            }

            if (cut <= 0 || cut >= target.Count) break;

            var leftColors = new int[cut];
            var rightColors = new int[target.Count - cut];
            Array.Copy(target.Colors, 0, leftColors, 0, cut);
            Array.Copy(target.Colors, cut, rightColors, 0, rightColors.Length);

            buckets[splitIndex] = ColorBucket.Create(leftColors, histogram);
            buckets.Add(ColorBucket.Create(rightColors, histogram));
        }

        paletteCount = buckets.Count;
        var palette = new byte[paletteCount * 3];
        for (var i = 0; i < buckets.Count; i++) {
            var bucket = buckets[i];
            var total = bucket.TotalCount;
            if (total <= 0) total = 1;
            long sumR = 0;
            long sumG = 0;
            long sumB = 0;
            for (var j = 0; j < bucket.Count; j++) {
                var idx = bucket.Colors[j];
                var count = histogram[idx];
                var r5 = (idx >> 10) & 31;
                var g5 = (idx >> 5) & 31;
                var b5 = idx & 31;
                sumR += r5 * (long)count;
                sumG += g5 * (long)count;
                sumB += b5 * (long)count;
            }

            var avgR5 = (int)((sumR + total / 2) / total);
            var avgG5 = (int)((sumG + total / 2) / total);
            var avgB5 = (int)((sumB + total / 2) / total);
            palette[i * 3 + 0] = (byte)((avgR5 * 255 + 15) / 31);
            palette[i * 3 + 1] = (byte)((avgG5 * 255 + 15) / 31);
            palette[i * 3 + 2] = (byte)((avgB5 * 255 + 15) / 31);
        }

        return palette;
    }

    private static int[] BuildPaletteMap(byte[] palette, int paletteCount) {
        var map = new int[32 * 32 * 32];
        for (var idx = 0; idx < map.Length; idx++) {
            var r5 = (idx >> 10) & 31;
            var g5 = (idx >> 5) & 31;
            var b5 = idx & 31;
            var r8 = r5 * 255 / 31;
            var g8 = g5 * 255 / 31;
            var b8 = b5 * 255 / 31;

            var best = 0;
            var bestDist = int.MaxValue;
            for (var p = 0; p < paletteCount; p++) {
                var baseIndex = p * 3;
                var dr = r8 - palette[baseIndex + 0];
                var dg = g8 - palette[baseIndex + 1];
                var db = b8 - palette[baseIndex + 2];
                var dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist) {
                    bestDist = dist;
                    best = p;
                    if (dist == 0) break;
                }
            }
            map[idx] = best;
        }
        return map;
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

    private readonly struct ColorBucket {
        public ColorBucket(int[] colors, int count, int totalCount, byte minR, byte maxR, byte minG, byte maxG, byte minB, byte maxB) {
            Colors = colors;
            Count = count;
            TotalCount = totalCount;
            MinR = minR;
            MaxR = maxR;
            MinG = minG;
            MaxG = maxG;
            MinB = minB;
            MaxB = maxB;
        }

        public int[] Colors { get; }
        public int Count { get; }
        public int TotalCount { get; }
        public byte MinR { get; }
        public byte MaxR { get; }
        public byte MinG { get; }
        public byte MaxG { get; }
        public byte MinB { get; }
        public byte MaxB { get; }

        public int RangeR => MaxR - MinR;
        public int RangeG => MaxG - MinG;
        public int RangeB => MaxB - MinB;
        public int MaxRange => Math.Max(RangeR, Math.Max(RangeG, RangeB));

        public int LongestChannel {
            get {
                var r = RangeR;
                var g = RangeG;
                var b = RangeB;
                if (r >= g && r >= b) return 0;
                return g >= b ? 1 : 2;
            }
        }

        public static ColorBucket Create(int[] colors, int[] histogram) {
            var minR = (byte)31;
            var minG = (byte)31;
            var minB = (byte)31;
            var maxR = (byte)0;
            var maxG = (byte)0;
            var maxB = (byte)0;
            var total = 0;
            for (var i = 0; i < colors.Length; i++) {
                var idx = colors[i];
                var count = histogram[idx];
                total += count;
                var r5 = (byte)((idx >> 10) & 31);
                var g5 = (byte)((idx >> 5) & 31);
                var b5 = (byte)(idx & 31);
                if (r5 < minR) minR = r5;
                if (r5 > maxR) maxR = r5;
                if (g5 < minG) minG = g5;
                if (g5 > maxG) maxG = g5;
                if (b5 < minB) minB = b5;
                if (b5 > maxB) maxB = b5;
            }
            return new ColorBucket(colors, colors.Length, total, minR, maxR, minG, maxG, minB, maxB);
        }
    }

    private sealed class ChannelComparer : IComparer<int> {
        private readonly int _channel;

        public ChannelComparer(int channel) {
            _channel = channel;
        }

        public int Compare(int x, int y) {
            var xr = (x >> 10) & 31;
            var xg = (x >> 5) & 31;
            var xb = x & 31;
            var yr = (y >> 10) & 31;
            var yg = (y >> 5) & 31;
            var yb = y & 31;
            return _channel switch {
                0 => xr.CompareTo(yr),
                1 => xg.CompareTo(yg),
                _ => xb.CompareTo(yb),
            };
        }
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

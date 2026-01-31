using System;

namespace CodeGlyphX.Rendering.Psd;

/// <summary>
/// Minimal PSD decoder (flattened image only).
/// </summary>
public static class PsdReader {
    private const uint Signature8Bps = 0x38425053; // "8BPS"

    /// <summary>
    /// Returns true when the data looks like a PSD file.
    /// </summary>
    public static bool IsPsd(ReadOnlySpan<byte> data) {
        if (data.Length < 26) return false;
        return ReadU32BE(data, 0) == Signature8Bps;
    }

    /// <summary>
    /// Attempts to read PSD dimensions without decoding pixels.
    /// </summary>
    public static bool TryReadDimensions(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!IsPsd(data)) return false;
        if (data.Length < 26) return false;
        var version = ReadU16BE(data, 4);
        if (version != 1 && version != 2) return false;
        height = (int)ReadU32BE(data, 14);
        width = (int)ReadU32BE(data, 18);
        return width > 0 && height > 0;
    }

    /// <summary>
    /// Decodes a PSD image to an RGBA buffer (flattened, 8/16-bit Grayscale/RGB/CMYK only).
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (!IsPsd(data)) throw new FormatException("Invalid PSD signature.");
        if (data.Length < 26) throw new FormatException("Invalid PSD header.");

        var version = ReadU16BE(data, 4);
        if (version != 1 && version != 2) throw new FormatException("Unsupported PSD version.");

        var channels = (int)ReadU16BE(data, 12);
        height = (int)ReadU32BE(data, 14);
        width = (int)ReadU32BE(data, 18);
        var depth = ReadU16BE(data, 22);
        var colorMode = ReadU16BE(data, 24);
        var channelCount = (int)channels;

        if (width <= 0 || height <= 0) throw new FormatException("Invalid PSD dimensions.");
        if (channelCount <= 0) throw new FormatException("Invalid PSD channel count.");
        if (depth != 8 && depth != 16) throw new FormatException("Only 8-bit or 16-bit PSD images are supported.");
        if (colorMode != 1 && colorMode != 3 && colorMode != 4) {
            throw new FormatException("Only Grayscale, RGB, or CMYK PSD images are supported.");
        }
        if (colorMode == 3 && channelCount < 3) throw new FormatException("RGB PSD must have at least 3 channels.");
        if (colorMode == 4 && channelCount < 4) throw new FormatException("CMYK PSD must have at least 4 channels.");

        var offset = 26;
        offset = SkipSection(data, offset); // color mode data
        offset = SkipSection(data, offset); // image resources
        offset = SkipSection(data, offset); // layer and mask info
        if (offset + 2 > data.Length) throw new FormatException("PSD image data is missing.");

        var compression = ReadU16BE(data, offset);
        offset += 2;
        if (compression != 0 && compression != 1) throw new FormatException("Unsupported PSD compression.");

        var bytesPerSample = depth == 16 ? 2 : 1;
        var colorChannels = colorMode == 1 ? 1 : colorMode == 3 ? 3 : 4;
        var hasAlpha = channelCount > colorChannels;
        var channelsToRead = colorChannels + (hasAlpha ? 1 : 0);

        var pixelCount = checked(width * height);
        var channelBuffers = new byte[channelsToRead][];
        for (var c = 0; c < channelsToRead; c++) channelBuffers[c] = new byte[pixelCount];

        if (compression == 0) {
            var channelBytes = checked(width * height * bytesPerSample);
            for (var c = 0; c < channelCount; c++) {
                if (offset + channelBytes > data.Length) throw new FormatException("Truncated PSD data.");
                if (c < channelsToRead) {
                    if (bytesPerSample == 1) {
                        data.Slice(offset, channelBytes).CopyTo(channelBuffers[c]);
                    } else {
                        ConvertChannel16To8(data.Slice(offset, channelBytes), channelBuffers[c]);
                    }
                }
                offset += channelBytes;
            }
        } else {
            var rowCount = checked(channelCount * height);
            var lengthsBytes = checked(rowCount * 2);
            if (offset + lengthsBytes > data.Length) throw new FormatException("Truncated PSD RLE header.");
            var rowLengths = new ushort[rowCount];
            for (var i = 0; i < rowCount; i++) {
                rowLengths[i] = ReadU16BE(data, offset + i * 2);
            }
            offset += lengthsBytes;

            var rowBytes = checked(width * bytesPerSample);
            var rowBuffer = new byte[rowBytes];
            for (var c = 0; c < channelCount; c++) {
                for (var y = 0; y < height; y++) {
                    var rowLength = rowLengths[c * height + y];
                    if (offset + rowLength > data.Length) throw new FormatException("Truncated PSD RLE data.");
                    var rowSpan = data.Slice(offset, rowLength);
                    if (!TryDecodePackBitsRow(rowSpan, rowBuffer)) {
                        throw new FormatException("Invalid PSD RLE data.");
                    }
                    if (c < channelsToRead) {
                        var dstRow = channelBuffers[c].AsSpan(y * width, width);
                        if (bytesPerSample == 1) {
                            rowBuffer.AsSpan(0, width).CopyTo(dstRow);
                        } else {
                            ConvertRow16To8(rowBuffer, dstRow, width);
                        }
                    }
                    offset += rowLength;
                }
            }
        }

        var rgba = new byte[pixelCount * 4];
        if (colorMode == 3) {
            var r = channelBuffers[0];
            var g = channelBuffers[1];
            var b = channelBuffers[2];
            var a = hasAlpha ? channelBuffers[3] : null;
            for (var i = 0; i < pixelCount; i++) {
                var dst = i * 4;
                rgba[dst + 0] = r[i];
                rgba[dst + 1] = g[i];
                rgba[dst + 2] = b[i];
                rgba[dst + 3] = a is null ? (byte)255 : a[i];
            }
        } else if (colorMode == 4) {
            var c = channelBuffers[0];
            var m = channelBuffers[1];
            var y = channelBuffers[2];
            var k = channelBuffers[3];
            var a = hasAlpha ? channelBuffers[4] : null;
            for (var i = 0; i < pixelCount; i++) {
                var dst = i * 4;
                var cVal = c[i];
                var mVal = m[i];
                var yVal = y[i];
                var kVal = k[i];
                var r = 255 - cVal - kVal + (cVal * kVal + 127) / 255;
                var g = 255 - mVal - kVal + (mVal * kVal + 127) / 255;
                var b = 255 - yVal - kVal + (yVal * kVal + 127) / 255;
                if (r < 0) r = 0;
                if (g < 0) g = 0;
                if (b < 0) b = 0;
                if (r > 255) r = 255;
                if (g > 255) g = 255;
                if (b > 255) b = 255;
                rgba[dst + 0] = (byte)r;
                rgba[dst + 1] = (byte)g;
                rgba[dst + 2] = (byte)b;
                rgba[dst + 3] = a is null ? (byte)255 : a[i];
            }
        } else {
            var v = channelBuffers[0];
            var a = hasAlpha ? channelBuffers[1] : null;
            for (var i = 0; i < pixelCount; i++) {
                var dst = i * 4;
                var value = v[i];
                rgba[dst + 0] = value;
                rgba[dst + 1] = value;
                rgba[dst + 2] = value;
                rgba[dst + 3] = a is null ? (byte)255 : a[i];
            }
        }

        return rgba;
    }

    private static int SkipSection(ReadOnlySpan<byte> data, int offset) {
        if (offset + 4 > data.Length) throw new FormatException("Truncated PSD section.");
        var length = (int)ReadU32BE(data, offset);
        offset += 4;
        if (length < 0 || offset + length > data.Length) throw new FormatException("Invalid PSD section length.");
        return offset + length;
    }

    private static bool TryDecodePackBitsRow(ReadOnlySpan<byte> src, Span<byte> dst) {
        var si = 0;
        var di = 0;
        while (si < src.Length && di < dst.Length) {
            var n = unchecked((sbyte)src[si++]);
            if (n >= 0) {
                var count = n + 1;
                if (si + count > src.Length || di + count > dst.Length) return false;
                src.Slice(si, count).CopyTo(dst.Slice(di, count));
                si += count;
                di += count;
            } else if (n != -128) {
                var count = 1 - n;
                if (si >= src.Length || di + count > dst.Length) return false;
                var value = src[si++];
                dst.Slice(di, count).Fill(value);
                di += count;
            }
        }
        return di == dst.Length;
    }

    private static void ConvertChannel16To8(ReadOnlySpan<byte> src, Span<byte> dst) {
        if (src.Length < dst.Length * 2) throw new FormatException("Truncated 16-bit PSD data.");
        for (var i = 0; i < dst.Length; i++) {
            var offset = i * 2;
            var value = (ushort)((src[offset] << 8) | src[offset + 1]);
            dst[i] = Convert16To8(value);
        }
    }

    private static void ConvertRow16To8(ReadOnlySpan<byte> src, Span<byte> dst, int width) {
        if (src.Length < width * 2) throw new FormatException("Truncated 16-bit PSD row.");
        if (dst.Length < width) throw new ArgumentOutOfRangeException(nameof(dst));
        for (var i = 0; i < width; i++) {
            var offset = i * 2;
            var value = (ushort)((src[offset] << 8) | src[offset + 1]);
            dst[i] = Convert16To8(value);
        }
    }

    private static byte Convert16To8(ushort value) {
        return (byte)((value + 128) / 257);
    }

    private static ushort ReadU16BE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 2 > data.Length) return 0;
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadU32BE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)((data[offset] << 24)
            | (data[offset + 1] << 16)
            | (data[offset + 2] << 8)
            | data[offset + 3]);
    }
}

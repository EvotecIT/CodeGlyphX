using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Writes baseline TIFF images from RGBA buffers (single IFD, PackBits when smaller).
/// </summary>
public static class TiffWriter {
    private const ushort CompressionNone = 1;
    private const ushort CompressionPackBits = 32773;
    private const ushort CompressionDeflate = 8;

    /// <summary>
    /// Encodes an RGBA buffer into a TIFF byte array.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compression = TiffCompressionMode.Auto) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride, compression);
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes an RGBA buffer into a TIFF stream.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compressionMode = TiffCompressionMode.Auto) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < stride * height) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var hasAlpha = false;
        for (var y = 0; y < height && !hasAlpha; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                if (rgba[row + x * 4 + 3] < 255) {
                    hasAlpha = true;
                    break;
                }
            }
        }

        var samples = hasAlpha ? 4 : 3;
        var bytesPerPixel = samples;
        var pixelBytes = width * height * bytesPerPixel;
        var pixelOffset = 8;

        var pixelData = new byte[pixelBytes];
        var dst = 0;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var idx = row + x * 4;
                pixelData[dst++] = rgba[idx + 0];
                pixelData[dst++] = rgba[idx + 1];
                pixelData[dst++] = rgba[idx + 2];
                if (hasAlpha) {
                    pixelData[dst++] = rgba[idx + 3];
                }
            }
        }

        byte[] stripData;
        ushort compression;
        if (compressionMode == TiffCompressionMode.None) {
            stripData = pixelData;
            compression = CompressionNone;
        } else if (compressionMode == TiffCompressionMode.PackBits) {
            stripData = CompressPackBits(pixelData);
            compression = CompressionPackBits;
        } else if (compressionMode == TiffCompressionMode.Deflate) {
            stripData = CompressDeflate(pixelData);
            compression = CompressionDeflate;
        } else {
            var packBits = CompressPackBits(pixelData);
            var deflate = CompressDeflate(pixelData);
            stripData = pixelData;
            compression = CompressionNone;
            if (packBits.Length < stripData.Length) {
                stripData = packBits;
                compression = CompressionPackBits;
            }
            if (deflate.Length < stripData.Length) {
                stripData = deflate;
                compression = CompressionDeflate;
            }
        }

        var ifdOffset = pixelOffset + stripData.Length;
        if ((ifdOffset & 1) != 0) {
            ifdOffset++;
        }

        // Header
        WriteAscii(stream, "II");
        WriteUInt16(stream, 42);
        WriteUInt32(stream, (uint)ifdOffset);

        // Pixel data
        stream.Write(stripData, 0, stripData.Length);
        if ((pixelOffset + stripData.Length) != ifdOffset) {
            stream.WriteByte(0);
        }

        var entryCount = (ushort)(hasAlpha ? 11 : 10);
        var ifdSize = 2 + entryCount * 12 + 4;
        var bitsOffset = ifdOffset + ifdSize;
        if ((bitsOffset & 1) != 0) {
            bitsOffset++;
        }

        WriteUInt16(stream, entryCount);
        WriteIfdEntry(stream, 256, 4, 1, (uint)width); // ImageWidth
        WriteIfdEntry(stream, 257, 4, 1, (uint)height); // ImageLength
        WriteIfdEntry(stream, 258, 3, (uint)samples, (uint)bitsOffset); // BitsPerSample
        WriteIfdEntry(stream, 259, 3, 1, compression); // Compression
        WriteIfdEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
        WriteIfdEntry(stream, 273, 4, 1, (uint)pixelOffset); // StripOffsets
        WriteIfdEntry(stream, 277, 3, 1, (uint)samples); // SamplesPerPixel
        WriteIfdEntry(stream, 278, 4, 1, (uint)height); // RowsPerStrip
        WriteIfdEntry(stream, 279, 4, 1, (uint)stripData.Length); // StripByteCounts
        WriteIfdEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (chunky)
        if (hasAlpha) {
            WriteIfdEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
        }

        WriteUInt32(stream, 0); // Next IFD offset

        var pad = bitsOffset - (ifdOffset + ifdSize);
        if (pad > 0) {
            stream.Write(new byte[pad], 0, pad);
        }

        for (var i = 0; i < samples; i++) {
            WriteUInt16(stream, 8);
        }
    }

    private static byte[] CompressPackBits(ReadOnlySpan<byte> src) {
        if (src.Length == 0) return Array.Empty<byte>();

        var output = new List<byte>(src.Length);
        var i = 0;
        while (i < src.Length) {
            var run = 1;
            while (i + run < src.Length && run < 128 && src[i + run] == src[i]) {
                run++;
            }

            if (run >= 3) {
                output.Add((byte)(1 - run));
                output.Add(src[i]);
                i += run;
                continue;
            }

            var start = i;
            var literal = 0;
            while (i < src.Length) {
                run = 1;
                while (i + run < src.Length && run < 128 && src[i + run] == src[i]) {
                    run++;
                }
                if (run >= 3) break;
                i++;
                literal++;
                if (literal == 128) break;
            }

            output.Add((byte)(literal - 1));
            for (var j = 0; j < literal; j++) {
                output.Add(src[start + j]);
            }
        }

        return output.ToArray();
    }

    private static byte[] CompressDeflate(byte[] src) {
        if (src.Length == 0) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
            deflate.Write(src, 0, src.Length);
        }
        return ms.ToArray();
    }

    private static void WriteIfdEntry(Stream stream, ushort tag, ushort type, uint count, uint value) {
        WriteUInt16(stream, tag);
        WriteUInt16(stream, type);
        WriteUInt32(stream, count);
        WriteUInt32(stream, value);
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

    private static void WriteUInt32(Stream stream, uint value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }
}

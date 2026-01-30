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
    /// Encodes multiple RGBA buffers into a multi-page TIFF byte array.
    /// </summary>
    public static byte[] WriteRgba32(ReadOnlySpan<TiffRgba32Page> pages) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, pages);
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

        var encoded = EncodeStrip(rgba, width, height, stride, compressionMode);
        var stripData = encoded.StripData;
        var compression = encoded.Compression;
        var hasAlpha = encoded.HasAlpha;
        var samples = encoded.Samples;
        var pixelOffset = 8;

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

    /// <summary>
    /// Encodes multiple RGBA buffers into a multi-page TIFF stream.
    /// </summary>
    public static void WriteRgba32(Stream stream, ReadOnlySpan<TiffRgba32Page> pages) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (pages.Length == 0) throw new ArgumentException("At least one page is required.", nameof(pages));

        var encodedPages = new EncodedPage[pages.Length];
        for (var i = 0; i < pages.Length; i++) {
            var page = pages[i];
            if (page.Rgba is null) throw new ArgumentNullException(nameof(page.Rgba));
            if (page.Width <= 0) throw new ArgumentOutOfRangeException(nameof(page.Width));
            if (page.Height <= 0) throw new ArgumentOutOfRangeException(nameof(page.Height));
            if (page.Stride < page.Width * 4) throw new ArgumentOutOfRangeException(nameof(page.Stride));
            if (page.Rgba.Length < page.Stride * page.Height) throw new ArgumentException("RGBA buffer is too small.", nameof(page.Rgba));

            var encoded = EncodeStrip(page.Rgba, page.Width, page.Height, page.Stride, page.Compression);
            encodedPages[i] = new EncodedPage(page.Width, page.Height, encoded.StripData, encoded.Compression, encoded.HasAlpha, encoded.Samples);
        }

        var offset = 8L;
        for (var i = 0; i < encodedPages.Length; i++) {
            var length = encodedPages[i].StripData.Length;
            if (offset > uint.MaxValue) throw new InvalidOperationException("TIFF data exceeds supported size.");
            encodedPages[i].StripOffset = (uint)offset;
            offset += length;
            if ((offset & 1) != 0) offset++;
        }

        if ((offset & 1) != 0) offset++;
        var ifdStart = offset;

        var ifdOffset = ifdStart;
        for (var i = 0; i < encodedPages.Length; i++) {
            var entryCount = (ushort)(encodedPages[i].HasAlpha ? 11 : 10);
            var ifdSize = 2 + entryCount * 12 + 4;
            var bitsOffset = ifdOffset + ifdSize;
            if ((bitsOffset & 1) != 0) bitsOffset++;
            var bitsSize = encodedPages[i].Samples * 2;
            if (ifdOffset > uint.MaxValue || bitsOffset > uint.MaxValue) throw new InvalidOperationException("TIFF data exceeds supported size.");

            encodedPages[i].IfdOffset = (uint)ifdOffset;
            encodedPages[i].BitsOffset = (uint)bitsOffset;
            ifdOffset = bitsOffset + bitsSize;
        }

        WriteAscii(stream, "II");
        WriteUInt16(stream, 42);
        WriteUInt32(stream, encodedPages[0].IfdOffset);

        for (var i = 0; i < encodedPages.Length; i++) {
            var stripData = encodedPages[i].StripData;
            stream.Write(stripData, 0, stripData.Length);
            if ((stream.Position & 1) != 0) {
                stream.WriteByte(0);
            }
        }

        if (stream.Position < ifdStart) {
            var pad = (int)(ifdStart - stream.Position);
            stream.Write(new byte[pad], 0, pad);
        }

        for (var i = 0; i < encodedPages.Length; i++) {
            var page = encodedPages[i];
            if (stream.Position < page.IfdOffset) {
                var pad = (int)(page.IfdOffset - stream.Position);
                stream.Write(new byte[pad], 0, pad);
            }

            var entryCount = (ushort)(page.HasAlpha ? 11 : 10);
            WriteUInt16(stream, entryCount);
            WriteIfdEntry(stream, 256, 4, 1, (uint)page.Width); // ImageWidth
            WriteIfdEntry(stream, 257, 4, 1, (uint)page.Height); // ImageLength
            WriteIfdEntry(stream, 258, 3, page.Samples, page.BitsOffset); // BitsPerSample
            WriteIfdEntry(stream, 259, 3, 1, page.Compression); // Compression
            WriteIfdEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
            WriteIfdEntry(stream, 273, 4, 1, page.StripOffset); // StripOffsets
            WriteIfdEntry(stream, 277, 3, 1, page.Samples); // SamplesPerPixel
            WriteIfdEntry(stream, 278, 4, 1, (uint)page.Height); // RowsPerStrip
            WriteIfdEntry(stream, 279, 4, 1, (uint)page.StripData.Length); // StripByteCounts
            WriteIfdEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (chunky)
            if (page.HasAlpha) {
                WriteIfdEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
            }

            var nextIfd = i + 1 < encodedPages.Length ? encodedPages[i + 1].IfdOffset : 0u;
            WriteUInt32(stream, nextIfd);

            if (stream.Position < page.BitsOffset) {
                var pad = (int)(page.BitsOffset - stream.Position);
                stream.Write(new byte[pad], 0, pad);
            }

            for (var j = 0; j < page.Samples; j++) {
                WriteUInt16(stream, 8);
            }
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

    private static EncodedStrip EncodeStrip(ReadOnlySpan<byte> rgba, int width, int height, int stride, TiffCompressionMode compressionMode) {
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

        var samples = (ushort)(hasAlpha ? 4 : 3);
        var bytesPerPixel = samples;
        var pixelBytes = checked(width * height * bytesPerPixel);
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

        return new EncodedStrip(stripData, compression, hasAlpha, samples);
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

    private readonly struct EncodedStrip {
        public EncodedStrip(byte[] stripData, ushort compression, bool hasAlpha, ushort samples) {
            StripData = stripData;
            Compression = compression;
            HasAlpha = hasAlpha;
            Samples = samples;
        }

        public byte[] StripData { get; }
        public ushort Compression { get; }
        public bool HasAlpha { get; }
        public ushort Samples { get; }
    }

    private struct EncodedPage {
        public EncodedPage(int width, int height, byte[] stripData, ushort compression, bool hasAlpha, ushort samples) {
            Width = width;
            Height = height;
            StripData = stripData;
            Compression = compression;
            HasAlpha = hasAlpha;
            Samples = samples;
            StripOffset = 0;
            IfdOffset = 0;
            BitsOffset = 0;
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] StripData { get; }
        public ushort Compression { get; }
        public bool HasAlpha { get; }
        public ushort Samples { get; }
        public uint StripOffset { get; set; }
        public uint IfdOffset { get; set; }
        public uint BitsOffset { get; set; }
    }
}

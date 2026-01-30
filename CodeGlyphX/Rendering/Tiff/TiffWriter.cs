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
    /// Encodes an RGBA buffer into a TIFF byte array using multiple strips.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compression, int rowsPerStrip) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride, compression, rowsPerStrip);
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
        WriteRgba32(stream, width, height, rgba, stride, compressionMode, rowsPerStrip: 0);
    }

    /// <summary>
    /// Encodes an RGBA buffer into a TIFF stream using multiple strips.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compressionMode, int rowsPerStrip) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < stride * height) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var normalizedRows = rowsPerStrip <= 0 || rowsPerStrip >= height ? height : rowsPerStrip;
        if (normalizedRows == height) {
            var encoded = EncodeStrip(rgba, width, height, stride, compressionMode);
            WriteSingleStrip(stream, width, height, encoded);
            return;
        }

        var encodedStrips = EncodeStrips(rgba, width, height, stride, compressionMode, normalizedRows);
        WriteMultiStrip(stream, width, height, encodedStrips);
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

            var rowsPerStrip = page.RowsPerStrip <= 0 || page.RowsPerStrip >= page.Height
                ? page.Height
                : page.RowsPerStrip;

            if (rowsPerStrip == page.Height) {
                var encoded = EncodeStrip(page.Rgba, page.Width, page.Height, page.Stride, page.Compression);
                encodedPages[i] = EncodedPage.FromSingleStrip(page.Width, page.Height, encoded, rowsPerStrip);
            } else {
                var encoded = EncodeStrips(page.Rgba, page.Width, page.Height, page.Stride, page.Compression, rowsPerStrip);
                encodedPages[i] = EncodedPage.FromStrips(page.Width, page.Height, encoded);
            }
        }

        var offset = 8L;
        for (var i = 0; i < encodedPages.Length; i++) {
            var page = encodedPages[i];
            page.StripOffsets = new uint[page.Strips.Length];
            for (var s = 0; s < page.Strips.Length; s++) {
                if (offset > uint.MaxValue) throw new InvalidOperationException("TIFF data exceeds supported size.");
                page.StripOffsets[s] = (uint)offset;
                offset += page.Strips[s].Length;
                if ((offset & 1) != 0) offset++;
            }
            encodedPages[i] = page;
        }

        if ((offset & 1) != 0) offset++;
        var ifdStart = offset;

        var ifdOffset = ifdStart;
        for (var i = 0; i < encodedPages.Length; i++) {
            var page = encodedPages[i];
            var entryCount = (ushort)(page.HasAlpha ? 11 : 10);
            var ifdSize = 2 + entryCount * 12 + 4;
            var extraOffset = ifdOffset + ifdSize;
            if ((extraOffset & 1) != 0) extraOffset++;

            if (page.Strips.Length > 1) {
                page.StripOffsetsOffset = (uint)extraOffset;
                extraOffset += page.Strips.Length * 4L;
                if ((extraOffset & 1) != 0) extraOffset++;
                page.StripByteCountsOffset = (uint)extraOffset;
                extraOffset += page.Strips.Length * 4L;
                if ((extraOffset & 1) != 0) extraOffset++;
            } else {
                page.StripOffsetsOffset = 0;
                page.StripByteCountsOffset = 0;
            }

            var bitsOffset = extraOffset;
            if ((bitsOffset & 1) != 0) bitsOffset++;
            var bitsSize = page.Samples * 2;
            if (ifdOffset > uint.MaxValue || bitsOffset > uint.MaxValue) throw new InvalidOperationException("TIFF data exceeds supported size.");

            page.IfdOffset = (uint)ifdOffset;
            page.BitsOffset = (uint)bitsOffset;
            ifdOffset = bitsOffset + bitsSize;
            encodedPages[i] = page;
        }

        WriteAscii(stream, "II");
        WriteUInt16(stream, 42);
        WriteUInt32(stream, encodedPages[0].IfdOffset);

        for (var i = 0; i < encodedPages.Length; i++) {
            var page = encodedPages[i];
            for (var s = 0; s < page.Strips.Length; s++) {
                var strip = page.Strips[s];
                stream.Write(strip, 0, strip.Length);
                if ((stream.Position & 1) != 0) {
                    stream.WriteByte(0);
                }
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
            WriteIfdEntry(stream, 273, 4, (uint)page.Strips.Length, page.Strips.Length == 1 ? page.StripOffsets[0] : page.StripOffsetsOffset); // StripOffsets
            WriteIfdEntry(stream, 277, 3, 1, page.Samples); // SamplesPerPixel
            WriteIfdEntry(stream, 278, 4, 1, (uint)page.RowsPerStrip); // RowsPerStrip
            WriteIfdEntry(stream, 279, 4, (uint)page.Strips.Length, page.Strips.Length == 1 ? page.ByteCounts[0] : page.StripByteCountsOffset); // StripByteCounts
            WriteIfdEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (chunky)
            if (page.HasAlpha) {
                WriteIfdEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
            }

            var nextIfd = i + 1 < encodedPages.Length ? encodedPages[i + 1].IfdOffset : 0u;
            WriteUInt32(stream, nextIfd);

            if (page.Strips.Length > 1) {
                if (stream.Position < page.StripOffsetsOffset) {
                    var pad = (int)(page.StripOffsetsOffset - stream.Position);
                    stream.Write(new byte[pad], 0, pad);
                }
                WriteUInt32Array(stream, page.StripOffsets);

                if (stream.Position < page.StripByteCountsOffset) {
                    var pad = (int)(page.StripByteCountsOffset - stream.Position);
                    stream.Write(new byte[pad], 0, pad);
                }
                WriteUInt32Array(stream, page.ByteCounts);
            }

            if (stream.Position < page.BitsOffset) {
                var pad = (int)(page.BitsOffset - stream.Position);
                stream.Write(new byte[pad], 0, pad);
            }

            for (var j = 0; j < page.Samples; j++) {
                WriteUInt16(stream, 8);
            }
        }
    }

    private static void WriteSingleStrip(Stream stream, int width, int height, EncodedStrip encoded) {
        var stripData = encoded.StripData;
        var compression = encoded.Compression;
        var hasAlpha = encoded.HasAlpha;
        var samples = encoded.Samples;
        var pixelOffset = 8;

        var ifdOffset = pixelOffset + stripData.Length;
        if ((ifdOffset & 1) != 0) {
            ifdOffset++;
        }

        WriteAscii(stream, "II");
        WriteUInt16(stream, 42);
        WriteUInt32(stream, (uint)ifdOffset);

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
        WriteIfdEntry(stream, 258, 3, samples, (uint)bitsOffset); // BitsPerSample
        WriteIfdEntry(stream, 259, 3, 1, compression); // Compression
        WriteIfdEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
        WriteIfdEntry(stream, 273, 4, 1, (uint)pixelOffset); // StripOffsets
        WriteIfdEntry(stream, 277, 3, 1, samples); // SamplesPerPixel
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

    private static void WriteMultiStrip(Stream stream, int width, int height, EncodedStrips encoded) {
        var stripCount = encoded.Strips.Length;
        var stripOffsets = new uint[stripCount];
        var stripByteCounts = encoded.ByteCounts;

        long offset = 8;
        for (var i = 0; i < stripCount; i++) {
            if (offset > uint.MaxValue) throw new InvalidOperationException("TIFF data exceeds supported size.");
            stripOffsets[i] = (uint)offset;
            offset += encoded.Strips[i].Length;
            if ((offset & 1) != 0) offset++;
        }

        if ((offset & 1) != 0) offset++;
        var ifdOffset = offset;
        if (ifdOffset > uint.MaxValue) throw new InvalidOperationException("TIFF data exceeds supported size.");

        var entryCount = (ushort)(encoded.HasAlpha ? 11 : 10);
        var ifdSize = 2 + entryCount * 12 + 4;
        var extraOffset = ifdOffset + ifdSize;
        if ((extraOffset & 1) != 0) extraOffset++;

        var stripOffsetsOffset = 0u;
        var stripByteCountsOffset = 0u;
        if (stripCount > 1) {
            stripOffsetsOffset = (uint)extraOffset;
            extraOffset += stripCount * 4L;
            if ((extraOffset & 1) != 0) extraOffset++;
            stripByteCountsOffset = (uint)extraOffset;
            extraOffset += stripCount * 4L;
            if ((extraOffset & 1) != 0) extraOffset++;
        }

        var bitsOffset = extraOffset;
        if ((bitsOffset & 1) != 0) bitsOffset++;

        WriteAscii(stream, "II");
        WriteUInt16(stream, 42);
        WriteUInt32(stream, (uint)ifdOffset);

        for (var i = 0; i < stripCount; i++) {
            var strip = encoded.Strips[i];
            stream.Write(strip, 0, strip.Length);
            if ((stream.Position & 1) != 0) {
                stream.WriteByte(0);
            }
        }

        if (stream.Position < ifdOffset) {
            var pad = (int)(ifdOffset - stream.Position);
            stream.Write(new byte[pad], 0, pad);
        }

        WriteUInt16(stream, entryCount);
        WriteIfdEntry(stream, 256, 4, 1, (uint)width); // ImageWidth
        WriteIfdEntry(stream, 257, 4, 1, (uint)height); // ImageLength
        WriteIfdEntry(stream, 258, 3, encoded.Samples, (uint)bitsOffset); // BitsPerSample
        WriteIfdEntry(stream, 259, 3, 1, encoded.Compression); // Compression
        WriteIfdEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
        WriteIfdEntry(stream, 273, 4, (uint)stripCount, stripCount == 1 ? stripOffsets[0] : stripOffsetsOffset); // StripOffsets
        WriteIfdEntry(stream, 277, 3, 1, encoded.Samples); // SamplesPerPixel
        WriteIfdEntry(stream, 278, 4, 1, (uint)encoded.RowsPerStrip); // RowsPerStrip
        WriteIfdEntry(stream, 279, 4, (uint)stripCount, stripCount == 1 ? stripByteCounts[0] : stripByteCountsOffset); // StripByteCounts
        WriteIfdEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (chunky)
        if (encoded.HasAlpha) {
            WriteIfdEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
        }

        WriteUInt32(stream, 0); // Next IFD offset

        if (stripCount > 1) {
            if (stream.Position < stripOffsetsOffset) {
                var pad = (int)(stripOffsetsOffset - stream.Position);
                stream.Write(new byte[pad], 0, pad);
            }
            WriteUInt32Array(stream, stripOffsets);

            if (stream.Position < stripByteCountsOffset) {
                var pad = (int)(stripByteCountsOffset - stream.Position);
                stream.Write(new byte[pad], 0, pad);
            }
            WriteUInt32Array(stream, stripByteCounts);
        }

        if (stream.Position < bitsOffset) {
            var pad = (int)(bitsOffset - stream.Position);
            stream.Write(new byte[pad], 0, pad);
        }

        for (var i = 0; i < encoded.Samples; i++) {
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

    private static EncodedStrips EncodeStrips(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        TiffCompressionMode compressionMode,
        int rowsPerStrip) {
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
        var stripCount = (height + rowsPerStrip - 1) / rowsPerStrip;
        var rawStrips = new byte[stripCount][];

        var stripIndex = 0;
        for (var y = 0; y < height; y += rowsPerStrip) {
            var rows = Math.Min(rowsPerStrip, height - y);
            var pixelBytes = checked(width * rows * bytesPerPixel);
            var pixelData = new byte[pixelBytes];
            var dst = 0;
            for (var row = 0; row < rows; row++) {
                var srcRow = (y + row) * stride;
                for (var x = 0; x < width; x++) {
                    var idx = srcRow + x * 4;
                    pixelData[dst++] = rgba[idx + 0];
                    pixelData[dst++] = rgba[idx + 1];
                    pixelData[dst++] = rgba[idx + 2];
                    if (hasAlpha) {
                        pixelData[dst++] = rgba[idx + 3];
                    }
                }
            }
            rawStrips[stripIndex++] = pixelData;
        }

        ushort compression;
        byte[][] strips;

        if (compressionMode == TiffCompressionMode.None) {
            compression = CompressionNone;
            strips = rawStrips;
        } else if (compressionMode == TiffCompressionMode.PackBits) {
            compression = CompressionPackBits;
            strips = CompressStrips(rawStrips, CompressPackBits);
        } else if (compressionMode == TiffCompressionMode.Deflate) {
            compression = CompressionDeflate;
            strips = CompressStrips(rawStrips, CompressDeflate);
        } else {
            var packBits = CompressStrips(rawStrips, CompressPackBits);
            var deflate = CompressStrips(rawStrips, CompressDeflate);
            var rawSize = SumSizes(rawStrips);
            var packSize = SumSizes(packBits);
            var deflateSize = SumSizes(deflate);

            compression = CompressionNone;
            strips = rawStrips;
            if (packSize < rawSize) {
                compression = CompressionPackBits;
                strips = packBits;
                rawSize = packSize;
            }
            if (deflateSize < rawSize) {
                compression = CompressionDeflate;
                strips = deflate;
            }
        }

        var byteCounts = new uint[strips.Length];
        for (var i = 0; i < strips.Length; i++) {
            byteCounts[i] = (uint)strips[i].Length;
        }

        return new EncodedStrips(strips, byteCounts, compression, hasAlpha, samples, rowsPerStrip);
    }

    private static byte[][] CompressStrips(byte[][] strips, Func<ReadOnlySpan<byte>, byte[]> compressor) {
        var output = new byte[strips.Length][];
        for (var i = 0; i < strips.Length; i++) {
            output[i] = compressor(strips[i]);
        }
        return output;
    }

    private static byte[][] CompressStrips(byte[][] strips, Func<byte[], byte[]> compressor) {
        var output = new byte[strips.Length][];
        for (var i = 0; i < strips.Length; i++) {
            output[i] = compressor(strips[i]);
        }
        return output;
    }

    private static long SumSizes(byte[][] strips) {
        long total = 0;
        for (var i = 0; i < strips.Length; i++) {
            total += strips[i].Length;
        }
        return total;
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

    private static void WriteUInt32Array(Stream stream, uint[] values) {
        for (var i = 0; i < values.Length; i++) {
            WriteUInt32(stream, values[i]);
        }
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

    private readonly struct EncodedStrips {
        public EncodedStrips(byte[][] strips, uint[] byteCounts, ushort compression, bool hasAlpha, ushort samples, int rowsPerStrip) {
            Strips = strips;
            ByteCounts = byteCounts;
            Compression = compression;
            HasAlpha = hasAlpha;
            Samples = samples;
            RowsPerStrip = rowsPerStrip;
        }

        public byte[][] Strips { get; }
        public uint[] ByteCounts { get; }
        public ushort Compression { get; }
        public bool HasAlpha { get; }
        public ushort Samples { get; }
        public int RowsPerStrip { get; }
    }

    private struct EncodedPage {
        public static EncodedPage FromSingleStrip(int width, int height, EncodedStrip encoded, int rowsPerStrip) {
            return new EncodedPage(
                width,
                height,
                new[] { encoded.StripData },
                new[] { (uint)encoded.StripData.Length },
                encoded.Compression,
                encoded.HasAlpha,
                encoded.Samples,
                rowsPerStrip);
        }

        public static EncodedPage FromStrips(int width, int height, EncodedStrips encoded) {
            return new EncodedPage(
                width,
                height,
                encoded.Strips,
                encoded.ByteCounts,
                encoded.Compression,
                encoded.HasAlpha,
                encoded.Samples,
                encoded.RowsPerStrip);
        }

        private EncodedPage(
            int width,
            int height,
            byte[][] strips,
            uint[] byteCounts,
            ushort compression,
            bool hasAlpha,
            ushort samples,
            int rowsPerStrip) {
            Width = width;
            Height = height;
            Strips = strips;
            ByteCounts = byteCounts;
            Compression = compression;
            HasAlpha = hasAlpha;
            Samples = samples;
            RowsPerStrip = rowsPerStrip;
            StripOffsets = Array.Empty<uint>();
            StripOffsetsOffset = 0;
            StripByteCountsOffset = 0;
            IfdOffset = 0;
            BitsOffset = 0;
        }

        public int Width { get; }
        public int Height { get; }
        public byte[][] Strips { get; }
        public uint[] ByteCounts { get; }
        public ushort Compression { get; }
        public bool HasAlpha { get; }
        public ushort Samples { get; }
        public int RowsPerStrip { get; }
        public uint[] StripOffsets { get; set; }
        public uint StripOffsetsOffset { get; set; }
        public uint StripByteCountsOffset { get; set; }
        public uint IfdOffset { get; set; }
        public uint BitsOffset { get; set; }
    }
}

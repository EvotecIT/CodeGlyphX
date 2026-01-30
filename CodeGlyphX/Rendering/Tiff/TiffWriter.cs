using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Writes TIFF images from RGBA buffers (baseline RGBA, optional PackBits/Deflate/LZW).
/// </summary>
public static class TiffWriter {
    private const ushort EntryCount = 11;
    private const ushort BilevelEntryCount = 9;
    private const ushort BilevelTileEntryCount = 10;
    private const ushort PredictorTag = 317;
    private const int BitsPerSampleSize = 8;

    /// <summary>
    /// Writes a TIFF byte array from an RGBA buffer (single page).
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compression) {
        return WriteRgba32(width, height, rgba, stride, compression, rowsPerStrip: 0);
    }

    /// <summary>
    /// Writes a TIFF byte array from an RGBA buffer (single page) with configurable rows per strip.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compression, int rowsPerStrip) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride, compression, rowsPerStrip);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page).
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compression) {
        WriteRgba32(stream, width, height, rgba, stride, compression, rowsPerStrip: 0);
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page) with configurable rows per strip.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, TiffCompressionMode compression, int rowsPerStrip) {
        if (compression == TiffCompressionMode.Auto) {
            var auto = WriteRgba32Auto(width, height, rgba, stride, rowsPerStrip);
            stream.Write(auto, 0, auto.Length);
            return;
        }

        var resolved = ResolveCompression(compression);
        var stripRows = rowsPerStrip <= 0 ? height : rowsPerStrip;
        WriteRgba32(stream, width, height, rgba, stride, stripRows, resolved, usePredictor: false);
    }

    /// <summary>
    /// Writes a TIFF byte array from an RGBA buffer (single page).
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride, height, TiffCompression.None);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page).
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        WriteRgba32(stream, width, height, rgba, stride, height, TiffCompression.None);
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page) with configurable rows per strip.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, int rowsPerStrip) {
        WriteRgba32(stream, width, height, rgba, stride, rowsPerStrip, TiffCompression.None);
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page) with configurable rows per strip and compression.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, int rowsPerStrip, TiffCompression compression) {
        WriteRgba32(stream, width, height, rgba, stride, rowsPerStrip, compression, usePredictor: false);
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page) with configurable rows per strip, compression, and predictor.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, int rowsPerStrip, TiffCompression compression, bool usePredictor) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));
        if (rowsPerStrip <= 0) throw new ArgumentOutOfRangeException(nameof(rowsPerStrip));
        if (compression != TiffCompression.None && compression != TiffCompression.PackBits && compression != TiffCompression.Deflate && compression != TiffCompression.Lzw) {
            throw new ArgumentOutOfRangeException(nameof(compression));
        }

        var rowSize = width * 4;
        var stripRows = Math.Min(rowsPerStrip, height);
        var stripCount = (height + stripRows - 1) / stripRows;
        var imageSize = (long)rowSize * height;
        if (imageSize > uint.MaxValue) throw new ArgumentException("TIFF image exceeds 4GB.");

        var entryCount = (ushort)(EntryCount + (usePredictor ? 1 : 0));
        var ifdSize = 2 + entryCount * 12 + 4;
        var bitsPerSampleOffset = 8 + ifdSize;
        var offset = bitsPerSampleOffset + BitsPerSampleSize;
        var stripOffsetsValue = 0u;
        var stripByteCountsValue = 0u;
        uint[]? stripOffsets = null;
        uint[]? stripByteCounts = null;
        byte[][]? stripData = null;
        uint imageOffset;

        if (compression != TiffCompression.None) {
            stripData = new byte[stripCount][];
            for (var s = 0; s < stripCount; s++) {
                var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                var raw = new byte[rowsInStrip * rowSize];
                for (var y = 0; y < rowsInStrip; y++) {
                    var srcRow = (s * stripRows + y) * stride;
                    rgba.Slice(srcRow, rowSize).CopyTo(raw.AsSpan(y * rowSize, rowSize));
                }
                if (usePredictor) {
                    ApplyHorizontalPredictor(raw, rowSize, rowsInStrip, bytesPerPixel: 4);
                }
                var encoded = EncodeCompressed(raw, compression, rowSize, rowsInStrip);
                stripData[s] = encoded;
            }
            if (stripCount == 1) {
                imageOffset = (uint)offset;
                stripOffsetsValue = imageOffset;
                stripByteCountsValue = (uint)stripData[0].Length;
            } else {
                stripOffsets = new uint[stripCount];
                stripByteCounts = new uint[stripCount];
                stripOffsetsValue = (uint)offset;
                offset += stripCount * 4;
                stripByteCountsValue = (uint)offset;
                offset += stripCount * 4;
                imageOffset = (uint)offset;

                var current = imageOffset;
                for (var s = 0; s < stripCount; s++) {
                    stripOffsets[s] = current;
                    stripByteCounts[s] = (uint)stripData[s].Length;
                    current += stripByteCounts[s];
                }
            }
        } else if (stripCount == 1) {
            imageOffset = (uint)offset;
            stripOffsetsValue = imageOffset;
            stripByteCountsValue = (uint)imageSize;
        } else {
            stripOffsetsValue = (uint)offset;
            offset += stripCount * 4;
            stripByteCountsValue = (uint)offset;
            offset += stripCount * 4;
            imageOffset = (uint)offset;

            stripOffsets = new uint[stripCount];
            stripByteCounts = new uint[stripCount];
            var current = imageOffset;
            for (var s = 0; s < stripCount; s++) {
                var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                var count = (uint)(rowSize * rowsInStrip);
                stripOffsets[s] = current;
                stripByteCounts[s] = count;
                current += count;
            }
        }

        WriteHeader(stream);
        WriteUInt16(stream, entryCount);

        WriteEntry(stream, 256, 4, 1, (uint)width); // ImageWidth
        WriteEntry(stream, 257, 4, 1, (uint)height); // ImageLength
        WriteEntry(stream, 258, 3, 4, (uint)bitsPerSampleOffset); // BitsPerSample
        WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
        WriteEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
        WriteEntry(stream, 273, 4, (uint)stripCount, stripOffsetsValue); // StripOffsets
        WriteEntry(stream, 277, 3, 1, 4); // SamplesPerPixel
        WriteEntry(stream, 278, 4, 1, (uint)stripRows); // RowsPerStrip
        WriteEntry(stream, 279, 4, (uint)stripCount, stripByteCountsValue); // StripByteCounts
        WriteEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (contiguous)
        WriteEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
        if (usePredictor) {
            WriteEntry(stream, PredictorTag, 3, 1, 2); // Predictor (horizontal)
        }

        WriteUInt32(stream, 0); // next IFD

        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);

        if (stripCount > 1) {
            for (var s = 0; s < stripCount; s++) {
                WriteUInt32(stream, stripOffsets![s]);
            }
            for (var s = 0; s < stripCount; s++) {
                WriteUInt32(stream, stripByteCounts![s]);
            }
        }

        if (compression != TiffCompression.None) {
            for (var s = 0; s < stripCount; s++) {
                var encoded = stripData![s];
                stream.Write(encoded, 0, encoded.Length);
            }
        } else {
            var rowBuffer = new byte[rowSize];
            for (var s = 0; s < stripCount; s++) {
                var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                for (var y = 0; y < rowsInStrip; y++) {
                    var srcRow = (s * stripRows + y) * stride;
                    rgba.Slice(srcRow, rowSize).CopyTo(rowBuffer);
                    if (usePredictor) {
                        ApplyHorizontalPredictor(rowBuffer, rowSize, rows: 1, bytesPerPixel: 4);
                    }
                    stream.Write(rowBuffer, 0, rowBuffer.Length);
                }
            }
        }
    }

    /// <summary>
    /// Writes a TIFF byte array from a packed 1-bit buffer (single page).
    /// </summary>
    public static byte[] WriteBilevel(int width, int height, ReadOnlySpan<byte> packed, int stride) {
        using var ms = new MemoryStream();
        WriteBilevel(ms, width, height, packed, stride, height, TiffCompression.None, photometric: 1);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a bilevel (1-bit) TIFF byte array with configurable rows-per-strip, compression, and photometric.
    /// </summary>
    public static byte[] WriteBilevel(
        int width,
        int height,
        ReadOnlySpan<byte> packed,
        int stride,
        int rowsPerStrip,
        TiffCompression compression,
        ushort photometric = 1) {
        using var ms = new MemoryStream();
        WriteBilevel(ms, width, height, packed, stride, rowsPerStrip, compression, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from a packed 1-bit buffer (single page).
    /// </summary>
    public static void WriteBilevel(Stream stream, int width, int height, ReadOnlySpan<byte> packed, int stride) {
        WriteBilevel(stream, width, height, packed, stride, height, TiffCompression.None, photometric: 1);
    }

    /// <summary>
    /// Writes a TIFF to a stream from a packed 1-bit buffer (single page) with configurable rows per strip.
    /// </summary>
    public static void WriteBilevel(Stream stream, int width, int height, ReadOnlySpan<byte> packed, int stride, int rowsPerStrip) {
        WriteBilevel(stream, width, height, packed, stride, rowsPerStrip, TiffCompression.None, photometric: 1);
    }

    /// <summary>
    /// Writes a TIFF to a stream from a packed 1-bit buffer (single page) with configurable rows per strip and compression.
    /// </summary>
    public static void WriteBilevel(
        Stream stream,
        int width,
        int height,
        ReadOnlySpan<byte> packed,
        int stride,
        int rowsPerStrip,
        TiffCompression compression,
        ushort photometric = 1) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (rowsPerStrip <= 0) throw new ArgumentOutOfRangeException(nameof(rowsPerStrip));
        if (compression != TiffCompression.None && compression != TiffCompression.PackBits && compression != TiffCompression.Deflate && compression != TiffCompression.Lzw) {
            throw new ArgumentOutOfRangeException(nameof(compression));
        }
        if (photometric != 0 && photometric != 1) {
            throw new ArgumentOutOfRangeException(nameof(photometric));
        }

        var rowSize = (width + 7) / 8;
        if (stride < rowSize) throw new ArgumentOutOfRangeException(nameof(stride));
        if (packed.Length < (height - 1) * stride + rowSize) throw new ArgumentException("Packed buffer is too small.", nameof(packed));

        var stripRows = Math.Min(rowsPerStrip, height);
        var stripCount = (height + stripRows - 1) / stripRows;
        var imageSize = (long)rowSize * height;
        if (imageSize > uint.MaxValue) throw new ArgumentException("TIFF image exceeds 4GB.");

        var entryCount = BilevelEntryCount;
        var ifdSize = 2 + entryCount * 12 + 4;
        var offset = 8 + ifdSize;
        var stripOffsetsValue = 0u;
        var stripByteCountsValue = 0u;
        uint[]? stripOffsets = null;
        uint[]? stripByteCounts = null;
        byte[][]? stripData = null;
        uint imageOffset;

        if (compression != TiffCompression.None) {
            stripData = new byte[stripCount][];
            for (var s = 0; s < stripCount; s++) {
                var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                var raw = new byte[rowsInStrip * rowSize];
                for (var y = 0; y < rowsInStrip; y++) {
                    var srcRow = (s * stripRows + y) * stride;
                    packed.Slice(srcRow, rowSize).CopyTo(raw.AsSpan(y * rowSize, rowSize));
                }
                var encoded = EncodeCompressed(raw, compression, rowSize, rowsInStrip);
                stripData[s] = encoded;
            }
            if (stripCount == 1) {
                imageOffset = (uint)offset;
                stripOffsetsValue = imageOffset;
                stripByteCountsValue = (uint)stripData[0].Length;
            } else {
                stripOffsets = new uint[stripCount];
                stripByteCounts = new uint[stripCount];
                stripOffsetsValue = (uint)offset;
                offset += stripCount * 4;
                stripByteCountsValue = (uint)offset;
                offset += stripCount * 4;
                imageOffset = (uint)offset;

                var current = imageOffset;
                for (var s = 0; s < stripCount; s++) {
                    stripOffsets[s] = current;
                    stripByteCounts[s] = (uint)stripData[s].Length;
                    current += stripByteCounts[s];
                }
            }
        } else if (stripCount == 1) {
            imageOffset = (uint)offset;
            stripOffsetsValue = imageOffset;
            stripByteCountsValue = (uint)imageSize;
        } else {
            stripOffsetsValue = (uint)offset;
            offset += stripCount * 4;
            stripByteCountsValue = (uint)offset;
            offset += stripCount * 4;
            imageOffset = (uint)offset;

            stripOffsets = new uint[stripCount];
            stripByteCounts = new uint[stripCount];
            var current = imageOffset;
            for (var s = 0; s < stripCount; s++) {
                var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                var count = (uint)(rowSize * rowsInStrip);
                stripOffsets[s] = current;
                stripByteCounts[s] = count;
                current += count;
            }
        }

        WriteHeader(stream);
        WriteUInt16(stream, entryCount);

        WriteEntry(stream, 256, 4, 1, (uint)width); // ImageWidth
        WriteEntry(stream, 257, 4, 1, (uint)height); // ImageLength
        WriteEntry(stream, 258, 3, 1, 1); // BitsPerSample
        WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
        WriteEntry(stream, 262, 3, 1, photometric); // PhotometricInterpretation
        WriteEntry(stream, 273, 4, (uint)stripCount, stripOffsetsValue); // StripOffsets
        WriteEntry(stream, 277, 3, 1, 1); // SamplesPerPixel
        WriteEntry(stream, 278, 4, 1, (uint)stripRows); // RowsPerStrip
        WriteEntry(stream, 279, 4, (uint)stripCount, stripByteCountsValue); // StripByteCounts

        WriteUInt32(stream, 0); // next IFD

        if (stripCount > 1) {
            for (var s = 0; s < stripCount; s++) {
                WriteUInt32(stream, stripOffsets![s]);
            }
            for (var s = 0; s < stripCount; s++) {
                WriteUInt32(stream, stripByteCounts![s]);
            }
        }

        if (compression != TiffCompression.None) {
            for (var s = 0; s < stripCount; s++) {
                var encoded = stripData![s];
                stream.Write(encoded, 0, encoded.Length);
            }
        } else {
            var rowBuffer = new byte[rowSize];
            for (var s = 0; s < stripCount; s++) {
                var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                for (var y = 0; y < rowsInStrip; y++) {
                    var srcRow = (s * stripRows + y) * stride;
                    packed.Slice(srcRow, rowSize).CopyTo(rowBuffer);
                    stream.Write(rowBuffer, 0, rowBuffer.Length);
                }
            }
        }
    }

    /// <summary>
    /// Writes a TIFF byte array from an RGBA buffer (single page) as a 1-bit bilevel image.
    /// </summary>
    public static byte[] WriteBilevelFromRgba(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteBilevelFromRgba(ms, width, height, rgba, stride, height, TiffCompression.None, threshold: 128, photometric: 1);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page) as a 1-bit bilevel image.
    /// </summary>
    public static void WriteBilevelFromRgba(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        WriteBilevelFromRgba(stream, width, height, rgba, stride, height, TiffCompression.None, threshold: 128, photometric: 1);
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page) as a 1-bit bilevel image with configurable rows per strip, compression, and threshold.
    /// </summary>
    public static void WriteBilevelFromRgba(
        Stream stream,
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int rowsPerStrip,
        TiffCompression compression,
        byte threshold = 128,
        ushort photometric = 1) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));
        if (photometric != 0 && photometric != 1) throw new ArgumentOutOfRangeException(nameof(photometric));

        var packedStride = (width + 7) / 8;
        var packed = PackBilevelFromRgba(width, height, rgba, stride, packedStride, threshold, photometric);
        WriteBilevel(stream, width, height, packed, packedStride, rowsPerStrip <= 0 ? height : rowsPerStrip, compression, photometric);
    }

    /// <summary>
    /// Writes a TIFF byte array from a packed 1-bit buffer (single page) as tiled data.
    /// </summary>
    public static byte[] WriteBilevelTiled(int width, int height, ReadOnlySpan<byte> packed, int stride, int tileWidth, int tileHeight) {
        using var ms = new MemoryStream();
        WriteBilevelTiled(ms, width, height, packed, stride, tileWidth, tileHeight, TiffCompression.None, photometric: 1);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF byte array from a packed 1-bit buffer (single page) as tiled data with compression.
    /// </summary>
    public static byte[] WriteBilevelTiled(
        int width,
        int height,
        ReadOnlySpan<byte> packed,
        int stride,
        int tileWidth,
        int tileHeight,
        TiffCompression compression,
        ushort photometric = 1) {
        using var ms = new MemoryStream();
        WriteBilevelTiled(ms, width, height, packed, stride, tileWidth, tileHeight, compression, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from a packed 1-bit buffer (single page) as tiled data.
    /// </summary>
    public static void WriteBilevelTiled(Stream stream, int width, int height, ReadOnlySpan<byte> packed, int stride, int tileWidth, int tileHeight) {
        WriteBilevelTiled(stream, width, height, packed, stride, tileWidth, tileHeight, TiffCompression.None, photometric: 1);
    }

    /// <summary>
    /// Writes a TIFF to a stream from a packed 1-bit buffer (single page) as tiled data with compression.
    /// </summary>
    public static void WriteBilevelTiled(
        Stream stream,
        int width,
        int height,
        ReadOnlySpan<byte> packed,
        int stride,
        int tileWidth,
        int tileHeight,
        TiffCompression compression,
        ushort photometric = 1) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (tileWidth <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        if (compression != TiffCompression.None && compression != TiffCompression.PackBits && compression != TiffCompression.Deflate && compression != TiffCompression.Lzw) {
            throw new ArgumentOutOfRangeException(nameof(compression));
        }
        if (photometric != 0 && photometric != 1) {
            throw new ArgumentOutOfRangeException(nameof(photometric));
        }

        var rowSize = (width + 7) / 8;
        if (stride < rowSize) throw new ArgumentOutOfRangeException(nameof(stride));
        if (packed.Length < (height - 1) * stride + rowSize) throw new ArgumentException("Packed buffer is too small.", nameof(packed));

        var tilesAcross = (width + tileWidth - 1) / tileWidth;
        var tilesDown = (height + tileHeight - 1) / tileHeight;
        if (tilesAcross <= 0 || tilesDown <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        var tileCount = checked(tilesAcross * tilesDown);

        var tileRowBytes = (tileWidth + 7) / 8;
        var tileBytesLong = (long)tileRowBytes * tileHeight;
        if (tileBytesLong > int.MaxValue) throw new ArgumentException("Tile size is too large.");
        var tileBytes = (int)tileBytesLong;

        var entryCount = BilevelTileEntryCount;
        var ifdSize = 2 + entryCount * 12 + 4;
        var offset = 8 + ifdSize;
        var tileOffsetsValue = 0u;
        var tileByteCountsValue = 0u;
        uint[]? tileOffsets = null;
        uint[]? tileByteCounts = null;
        byte[][]? tileData = null;

        if (tileCount == 1) {
            tileOffsetsValue = (uint)offset;
            if (compression == TiffCompression.None) {
                tileByteCountsValue = (uint)tileBytes;
            }
        } else {
            tileOffsetsValue = (uint)offset;
            offset += tileCount * 4;
            tileByteCountsValue = (uint)offset;
            offset += tileCount * 4;
            tileOffsets = new uint[tileCount];
            tileByteCounts = new uint[tileCount];
        }

        if (compression != TiffCompression.None) {
            tileData = new byte[tileCount][];
            var tileBuffer = new byte[tileBytes];
            var current = (uint)offset;
            for (var t = 0; t < tileCount; t++) {
                FillTileBilevel(packed, width, height, stride, tileWidth, tileHeight, tilesAcross, t, tileBuffer, photometric);
                var encoded = EncodeCompressed(tileBuffer, compression, tileRowBytes, tileHeight);
                tileData[t] = encoded;
                if (tileCount > 1) {
                    tileOffsets![t] = current;
                    tileByteCounts![t] = (uint)encoded.Length;
                    current += tileByteCounts[t];
                } else {
                    tileByteCountsValue = (uint)encoded.Length;
                }
            }
        } else if (tileCount > 1) {
            var current = (uint)offset;
            for (var t = 0; t < tileCount; t++) {
                tileOffsets![t] = current;
                tileByteCounts![t] = (uint)tileBytes;
                current += (uint)tileBytes;
            }
        }

        WriteHeader(stream);
        WriteUInt16(stream, entryCount);
        WriteEntry(stream, 256, 4, 1, (uint)width); // ImageWidth
        WriteEntry(stream, 257, 4, 1, (uint)height); // ImageLength
        WriteEntry(stream, 258, 3, 1, 1); // BitsPerSample
        WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
        WriteEntry(stream, 262, 3, 1, photometric); // PhotometricInterpretation
        WriteEntry(stream, 277, 3, 1, 1); // SamplesPerPixel
        WriteEntry(stream, 322, 4, 1, (uint)tileWidth); // TileWidth
        WriteEntry(stream, 323, 4, 1, (uint)tileHeight); // TileLength
        WriteEntry(stream, 324, 4, (uint)tileCount, tileOffsetsValue); // TileOffsets
        WriteEntry(stream, 325, 4, (uint)tileCount, tileByteCountsValue); // TileByteCounts

        WriteUInt32(stream, 0); // next IFD

        if (tileCount > 1) {
            for (var t = 0; t < tileCount; t++) {
                WriteUInt32(stream, tileOffsets![t]);
            }
            for (var t = 0; t < tileCount; t++) {
                WriteUInt32(stream, tileByteCounts![t]);
            }
        }

        if (compression != TiffCompression.None) {
            for (var t = 0; t < tileCount; t++) {
                var encoded = tileData![t];
                stream.Write(encoded, 0, encoded.Length);
            }
        } else {
            var tileBuffer = new byte[tileBytes];
            for (var t = 0; t < tileCount; t++) {
                FillTileBilevel(packed, width, height, stride, tileWidth, tileHeight, tilesAcross, t, tileBuffer, photometric);
                stream.Write(tileBuffer, 0, tileBuffer.Length);
            }
        }
    }

    /// <summary>
    /// Writes a TIFF byte array from an RGBA buffer (single page) as tiled bilevel data.
    /// </summary>
    public static byte[] WriteBilevelTiledFromRgba(
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        using var ms = new MemoryStream();
        WriteBilevelTiledFromRgba(ms, width, height, rgba, stride, tileWidth, tileHeight, compression, threshold, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from an RGBA buffer (single page) as tiled bilevel data.
    /// </summary>
    public static void WriteBilevelTiledFromRgba(
        Stream stream,
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));
        if (photometric != 0 && photometric != 1) throw new ArgumentOutOfRangeException(nameof(photometric));

        var packedStride = (width + 7) / 8;
        var packed = PackBilevelFromRgba(width, height, rgba, stride, packedStride, threshold, photometric);
        WriteBilevelTiled(stream, width, height, packed, packedStride, tileWidth, tileHeight, compression, photometric);
    }

    /// <summary>
    /// Writes a tiled TIFF byte array from an RGBA buffer (single page).
    /// </summary>
    public static byte[] WriteRgba32Tiled(int width, int height, ReadOnlySpan<byte> rgba, int stride, int tileWidth, int tileHeight) {
        using var ms = new MemoryStream();
        WriteRgba32Tiled(ms, width, height, rgba, stride, tileWidth, tileHeight, TiffCompression.None, usePredictor: false);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a tiled TIFF byte array from an RGBA buffer (single page) with compression and predictor.
    /// </summary>
    public static byte[] WriteRgba32Tiled(
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int tileWidth,
        int tileHeight,
        TiffCompression compression,
        bool usePredictor) {
        using var ms = new MemoryStream();
        WriteRgba32Tiled(ms, width, height, rgba, stride, tileWidth, tileHeight, compression, usePredictor);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a tiled TIFF to a stream from an RGBA buffer (single page).
    /// </summary>
    public static void WriteRgba32Tiled(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, int tileWidth, int tileHeight) {
        WriteRgba32Tiled(stream, width, height, rgba, stride, tileWidth, tileHeight, TiffCompression.None, usePredictor: false);
    }

    /// <summary>
    /// Writes a tiled TIFF to a stream from an RGBA buffer (single page) with compression and predictor.
    /// </summary>
    public static void WriteRgba32Tiled(
        Stream stream,
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int tileWidth,
        int tileHeight,
        TiffCompression compression,
        bool usePredictor) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));
        if (tileWidth <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        if (compression != TiffCompression.None && compression != TiffCompression.PackBits && compression != TiffCompression.Deflate && compression != TiffCompression.Lzw) {
            throw new ArgumentOutOfRangeException(nameof(compression));
        }

        var tilesAcross = (width + tileWidth - 1) / tileWidth;
        var tilesDown = (height + tileHeight - 1) / tileHeight;
        if (tilesAcross <= 0 || tilesDown <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        var tileCount = checked(tilesAcross * tilesDown);

        var tileRowBytes = checked(tileWidth * 4);
        var tileBytesLong = (long)tileRowBytes * tileHeight;
        if (tileBytesLong > int.MaxValue) throw new ArgumentException("Tile size is too large.");
        var tileBytes = (int)tileBytesLong;

        var entryCount = (ushort)(12 + (usePredictor ? 1 : 0));
        var ifdSize = 2 + entryCount * 12 + 4;
        var bitsPerSampleOffset = 8 + ifdSize;
        var offset = bitsPerSampleOffset + BitsPerSampleSize;
        var tileOffsetsValue = 0u;
        var tileByteCountsValue = 0u;
        uint[]? tileOffsets = null;
        uint[]? tileByteCounts = null;
        byte[][]? tileData = null;

        if (tileCount == 1) {
            tileOffsetsValue = (uint)offset;
            if (compression == TiffCompression.None) {
                tileByteCountsValue = (uint)tileBytes;
            }
        } else {
            tileOffsetsValue = (uint)offset;
            offset += tileCount * 4;
            tileByteCountsValue = (uint)offset;
            offset += tileCount * 4;
            tileOffsets = new uint[tileCount];
            tileByteCounts = new uint[tileCount];
        }

        if (compression != TiffCompression.None) {
            tileData = new byte[tileCount][];
            var tileBuffer = new byte[tileBytes];
            var current = (uint)offset;
            for (var t = 0; t < tileCount; t++) {
                FillTileRgba(rgba, width, height, stride, tileWidth, tileHeight, tilesAcross, t, tileBuffer);
                if (usePredictor) {
                    ApplyHorizontalPredictor(tileBuffer, tileRowBytes, tileHeight, bytesPerPixel: 4);
                }
                var encoded = EncodeCompressed(tileBuffer, compression, tileRowBytes, tileHeight);
                tileData[t] = encoded;
                if (tileCount > 1) {
                    tileOffsets![t] = current;
                    tileByteCounts![t] = (uint)encoded.Length;
                    current += tileByteCounts[t];
                } else {
                    tileByteCountsValue = (uint)encoded.Length;
                }
            }
        } else if (tileCount > 1) {
            var current = (uint)offset;
            for (var t = 0; t < tileCount; t++) {
                tileOffsets![t] = current;
                tileByteCounts![t] = (uint)tileBytes;
                current += (uint)tileBytes;
            }
        }

        WriteHeader(stream);
        WriteUInt16(stream, entryCount);
        WriteEntry(stream, 256, 4, 1, (uint)width); // ImageWidth
        WriteEntry(stream, 257, 4, 1, (uint)height); // ImageLength
        WriteEntry(stream, 258, 3, 4, (uint)bitsPerSampleOffset); // BitsPerSample
        WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
        WriteEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
        WriteEntry(stream, 277, 3, 1, 4); // SamplesPerPixel
        WriteEntry(stream, 322, 4, 1, (uint)tileWidth); // TileWidth
        WriteEntry(stream, 323, 4, 1, (uint)tileHeight); // TileLength
        WriteEntry(stream, 324, 4, (uint)tileCount, tileOffsetsValue); // TileOffsets
        WriteEntry(stream, 325, 4, (uint)tileCount, tileByteCountsValue); // TileByteCounts
        WriteEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (contiguous)
        WriteEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
        if (usePredictor) {
            WriteEntry(stream, PredictorTag, 3, 1, 2); // Predictor (horizontal)
        }

        WriteUInt32(stream, 0); // next IFD

        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);

        if (tileCount > 1) {
            for (var t = 0; t < tileCount; t++) {
                WriteUInt32(stream, tileOffsets![t]);
            }
            for (var t = 0; t < tileCount; t++) {
                WriteUInt32(stream, tileByteCounts![t]);
            }
        }

        if (compression != TiffCompression.None) {
            for (var t = 0; t < tileCount; t++) {
                var encoded = tileData![t];
                stream.Write(encoded, 0, encoded.Length);
            }
        } else {
            var tileBuffer = new byte[tileBytes];
            for (var t = 0; t < tileCount; t++) {
                FillTileRgba(rgba, width, height, stride, tileWidth, tileHeight, tilesAcross, t, tileBuffer);
                if (usePredictor) {
                    ApplyHorizontalPredictor(tileBuffer, tileRowBytes, tileHeight, bytesPerPixel: 4);
                }
                stream.Write(tileBuffer, 0, tileBuffer.Length);
            }
        }
    }

    /// <summary>
    /// Writes a planar TIFF byte array from an RGBA buffer (single page).
    /// </summary>
    public static byte[] WriteRgba32Planar(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32Planar(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a planar TIFF byte array from an RGBA buffer (single page) with rows per strip, compression, and predictor.
    /// </summary>
    public static byte[] WriteRgba32Planar(
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int rowsPerStrip,
        TiffCompression compression,
        bool usePredictor) {
        using var ms = new MemoryStream();
        WriteRgba32Planar(ms, width, height, rgba, stride, rowsPerStrip, compression, usePredictor);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a planar TIFF to a stream from an RGBA buffer (single page).
    /// </summary>
    public static void WriteRgba32Planar(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        WriteRgba32Planar(stream, width, height, rgba, stride, height, TiffCompression.None, usePredictor: false);
    }

    /// <summary>
    /// Writes a planar TIFF to a stream from an RGBA buffer with rows per strip, compression, and predictor.
    /// </summary>
    public static void WriteRgba32Planar(
        Stream stream,
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int rowsPerStrip,
        TiffCompression compression,
        bool usePredictor) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));
        if (rowsPerStrip <= 0) throw new ArgumentOutOfRangeException(nameof(rowsPerStrip));
        if (compression != TiffCompression.None && compression != TiffCompression.PackBits && compression != TiffCompression.Deflate && compression != TiffCompression.Lzw) {
            throw new ArgumentOutOfRangeException(nameof(compression));
        }

        var planeRowBytes = width;
        var stripRows = Math.Min(rowsPerStrip, height);
        var stripsPerPlane = (height + stripRows - 1) / stripRows;
        var totalStrips = stripsPerPlane * 4;
        var entryCount = (ushort)(EntryCount + (usePredictor ? 1 : 0));
        var ifdSize = 2 + entryCount * 12 + 4;
        var bitsPerSampleOffset = 8 + ifdSize;
        var stripOffsetsOffset = bitsPerSampleOffset + BitsPerSampleSize;
        var stripByteCountsOffset = stripOffsetsOffset + totalStrips * 4;
        var dataOffset = stripByteCountsOffset + totalStrips * 4;

        var offsets = new uint[totalStrips];
        var counts = new uint[totalStrips];
        byte[][]? stripData = compression == TiffCompression.None ? null : new byte[totalStrips][];

        var current = (uint)dataOffset;
        for (var plane = 0; plane < 4; plane++) {
            for (var s = 0; s < stripsPerPlane; s++) {
                var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                var rawSize = rowsInStrip * planeRowBytes;
                var raw = new byte[rawSize];
                for (var y = 0; y < rowsInStrip; y++) {
                    var srcRow = (s * stripRows + y) * stride;
                    for (var x = 0; x < width; x++) {
                        raw[y * planeRowBytes + x] = rgba[srcRow + x * 4 + plane];
                    }
                }
                if (usePredictor) {
                    ApplyHorizontalPredictor(raw, planeRowBytes, rowsInStrip, bytesPerPixel: 1);
                }

                var index = plane * stripsPerPlane + s;
                if (compression == TiffCompression.None) {
                    counts[index] = (uint)raw.Length;
                    offsets[index] = current;
                    current += counts[index];
                } else {
                    var encoded = EncodeCompressed(raw, compression, planeRowBytes, rowsInStrip);
                    stripData![index] = encoded;
                    counts[index] = (uint)encoded.Length;
                    offsets[index] = current;
                    current += counts[index];
                }
            }
        }

        WriteHeader(stream);
        WriteUInt16(stream, entryCount);

        WriteEntry(stream, 256, 4, 1, (uint)width); // ImageWidth
        WriteEntry(stream, 257, 4, 1, (uint)height); // ImageLength
        WriteEntry(stream, 258, 3, 4, (uint)bitsPerSampleOffset); // BitsPerSample
        WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
        WriteEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
        WriteEntry(stream, 273, 4, (uint)totalStrips, (uint)stripOffsetsOffset); // StripOffsets
        WriteEntry(stream, 277, 3, 1, 4); // SamplesPerPixel
        WriteEntry(stream, 278, 4, 1, (uint)stripRows); // RowsPerStrip
        WriteEntry(stream, 279, 4, (uint)totalStrips, (uint)stripByteCountsOffset); // StripByteCounts
        WriteEntry(stream, 284, 3, 1, 2); // PlanarConfiguration (separate)
        WriteEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
        if (usePredictor) {
            WriteEntry(stream, PredictorTag, 3, 1, 2); // Predictor (horizontal)
        }

        WriteUInt32(stream, 0); // next IFD

        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);
        WriteUInt16(stream, 8);

        for (var i = 0; i < totalStrips; i++) {
            WriteUInt32(stream, offsets[i]);
        }
        for (var i = 0; i < totalStrips; i++) {
            WriteUInt32(stream, counts[i]);
        }

        if (compression == TiffCompression.None) {
            var rowBuffer = new byte[planeRowBytes];
            for (var plane = 0; plane < 4; plane++) {
                for (var s = 0; s < stripsPerPlane; s++) {
                    var rowsInStrip = Math.Min(stripRows, height - s * stripRows);
                    for (var y = 0; y < rowsInStrip; y++) {
                        var srcRow = (s * stripRows + y) * stride;
                        for (var x = 0; x < width; x++) {
                            rowBuffer[x] = rgba[srcRow + x * 4 + plane];
                        }
                        if (usePredictor) {
                            ApplyHorizontalPredictor(rowBuffer, planeRowBytes, rows: 1, bytesPerPixel: 1);
                        }
                        stream.Write(rowBuffer, 0, rowBuffer.Length);
                    }
                }
            }
        } else {
            for (var i = 0; i < totalStrips; i++) {
                var encoded = stripData![i];
                stream.Write(encoded, 0, encoded.Length);
            }
        }
    }

    /// <summary>
    /// Writes a TIFF byte array from RGBA buffers (multi-page) using per-page settings.
    /// </summary>
    public static byte[] WriteRgba32(TiffRgba32Page[] pages) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, pages);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from RGBA buffers (multi-page) using per-page settings.
    /// </summary>
    public static void WriteRgba32(Stream stream, TiffRgba32Page[] pages) {
        WriteRgba32PagesPerPage(stream, pages);
    }

    /// <summary>
    /// Writes a TIFF byte array from RGBA buffers (multi-page).
    /// </summary>
    public static byte[] WriteRgba32Pages(params TiffRgba32Page[] pages) {
        using var ms = new MemoryStream();
        WriteRgba32Pages(ms, pages, int.MaxValue, TiffCompression.None, usePredictor: false);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from RGBA buffers (multi-page).
    /// </summary>
    public static void WriteRgba32Pages(Stream stream, TiffRgba32Page[] pages) {
        WriteRgba32Pages(stream, pages, int.MaxValue, TiffCompression.None, usePredictor: false);
    }

    /// <summary>
    /// Writes a TIFF byte array from RGBA buffers (multi-page) with compression and predictor.
    /// </summary>
    public static byte[] WriteRgba32Pages(TiffCompression compression, bool usePredictor, params TiffRgba32Page[] pages) {
        using var ms = new MemoryStream();
        WriteRgba32Pages(ms, pages, int.MaxValue, compression, usePredictor);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF byte array from RGBA buffers (multi-page) with rows per strip, compression, and predictor.
    /// </summary>
    public static byte[] WriteRgba32Pages(int rowsPerStrip, TiffCompression compression, bool usePredictor, params TiffRgba32Page[] pages) {
        using var ms = new MemoryStream();
        WriteRgba32Pages(ms, pages, rowsPerStrip, compression, usePredictor);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a TIFF to a stream from RGBA buffers (multi-page) with compression and predictor.
    /// </summary>
    public static void WriteRgba32Pages(Stream stream, TiffRgba32Page[] pages, TiffCompression compression, bool usePredictor) {
        WriteRgba32Pages(stream, pages, int.MaxValue, compression, usePredictor);
    }

    /// <summary>
    /// Writes a TIFF to a stream from RGBA buffers (multi-page) with rows per strip, compression, and predictor.
    /// </summary>
    public static void WriteRgba32Pages(Stream stream, TiffRgba32Page[] pages, int rowsPerStrip, TiffCompression compression, bool usePredictor) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (pages is null) throw new ArgumentNullException(nameof(pages));
        if (pages.Length == 0) throw new ArgumentException("At least one page is required.", nameof(pages));
        if (compression != TiffCompression.None && compression != TiffCompression.PackBits && compression != TiffCompression.Deflate && compression != TiffCompression.Lzw) {
            throw new ArgumentOutOfRangeException(nameof(compression));
        }
        if (rowsPerStrip <= 0) throw new ArgumentOutOfRangeException(nameof(rowsPerStrip));

        var entryCount = (ushort)(EntryCount + (usePredictor ? 1 : 0));
        var ifdSize = 2 + entryCount * 12 + 4;
        var pageCount = pages.Length;
        var ifdOffsets = new uint[pageCount];
        var bitsOffsets = new uint[pageCount];
        var imageSizes = new uint[pageCount];
        var nextOffsets = new uint[pageCount];
        var stripCounts = new int[pageCount];
        var stripOffsetsValues = new uint[pageCount];
        var stripByteCountsValues = new uint[pageCount];
        uint[][]? stripOffsets = null;
        uint[][]? stripByteCounts = null;
        byte[][][]? stripData = null;

        long cursor = 8;
        for (var i = 0; i < pageCount; i++) {
            var page = pages[i];
            ValidatePage(page);

            var rowSize = page.Width * 4;
            var stripRows = Math.Min(rowsPerStrip, page.Height);
            var stripCount = (page.Height + stripRows - 1) / stripRows;
            stripCounts[i] = stripCount;
            var useArrays = stripCount > 1;
            var dataOffset = cursor + ifdSize + BitsPerSampleSize + (useArrays ? stripCount * 8L : 0);

            if (dataOffset > uint.MaxValue) throw new ArgumentException("TIFF content exceeds 4GB.");
            ifdOffsets[i] = (uint)cursor;
            bitsOffsets[i] = (uint)(cursor + ifdSize);
            stripOffsetsValues[i] = useArrays ? (uint)(cursor + ifdSize + BitsPerSampleSize) : (uint)dataOffset;
            stripByteCountsValues[i] = useArrays ? (uint)(cursor + ifdSize + BitsPerSampleSize + stripCount * 4L) : 0;

            if (compression != TiffCompression.None) {
                stripData ??= new byte[pageCount][][];
                stripByteCounts ??= new uint[pageCount][];
                stripOffsets ??= new uint[pageCount][];
                stripData[i] = new byte[stripCount][];
                stripByteCounts[i] = new uint[stripCount];
                stripOffsets[i] = useArrays ? new uint[stripCount] : Array.Empty<uint>();
                for (var s = 0; s < stripCount; s++) {
                    var rowsInStrip = Math.Min(stripRows, page.Height - s * stripRows);
                    var rawSize = (long)rowsInStrip * rowSize;
                    if (rawSize > int.MaxValue) throw new ArgumentException("TIFF image exceeds 2GB for compressed pages.");
                    var raw = new byte[(int)rawSize];
                    for (var y = 0; y < rowsInStrip; y++) {
                        var srcRow = (s * stripRows + y) * page.Stride;
                        page.Rgba.AsSpan(srcRow, rowSize).CopyTo(raw.AsSpan(y * rowSize, rowSize));
                    }
                    if (usePredictor) {
                        ApplyHorizontalPredictor(raw, rowSize, rowsInStrip, bytesPerPixel: 4);
                    }
                    var encoded = EncodeCompressed(raw, compression, rowSize, rowsInStrip);
                    stripData[i][s] = encoded;
                    stripByteCounts[i][s] = (uint)encoded.Length;
                }

                var current = (uint)dataOffset;
                for (var s = 0; s < stripCount; s++) {
                    if (useArrays) stripOffsets![i][s] = current;
                    current += stripByteCounts[i][s];
                }
                imageSizes[i] = current - (uint)dataOffset;
                if (stripCount == 1) {
                    stripOffsetsValues[i] = (uint)dataOffset;
                    stripByteCountsValues[i] = stripByteCounts[i][0];
                }
            } else {
                var imageSize = (long)rowSize * page.Height;
                if (imageSize > uint.MaxValue) throw new ArgumentException("TIFF image exceeds 4GB.");
                imageSizes[i] = (uint)imageSize;
                if (useArrays) {
                    stripOffsets ??= new uint[pageCount][];
                    stripByteCounts ??= new uint[pageCount][];
                    stripOffsets[i] = new uint[stripCount];
                    stripByteCounts[i] = new uint[stripCount];
                    var current = (uint)dataOffset;
                    for (var s = 0; s < stripCount; s++) {
                        var rowsInStrip = Math.Min(stripRows, page.Height - s * stripRows);
                        var count = (uint)(rowsInStrip * rowSize);
                        stripOffsets[i][s] = current;
                        stripByteCounts[i][s] = count;
                        current += count;
                    }
                    imageSizes[i] = current - (uint)dataOffset;
                } else {
                    stripByteCountsValues[i] = (uint)imageSize;
                }
            }

            cursor = dataOffset + imageSizes[i];
            if (cursor > uint.MaxValue) throw new ArgumentException("TIFF content exceeds 4GB.");
        }

        for (var i = 0; i < pageCount; i++) {
            nextOffsets[i] = i < pageCount - 1 ? ifdOffsets[i + 1] : 0;
        }

        WriteHeader(stream);
        for (var i = 0; i < pageCount; i++) {
            var page = pages[i];
            var stripCount = stripCounts[i];
            var stripRows = Math.Min(rowsPerStrip, page.Height);
            var useArrays = stripCount > 1;
            WriteUInt16(stream, entryCount);

            WriteEntry(stream, 256, 4, 1, (uint)page.Width); // ImageWidth
            WriteEntry(stream, 257, 4, 1, (uint)page.Height); // ImageLength
            WriteEntry(stream, 258, 3, 4, bitsOffsets[i]); // BitsPerSample
            WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
            WriteEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
            WriteEntry(stream, 273, 4, (uint)stripCount, stripOffsetsValues[i]); // StripOffsets
            WriteEntry(stream, 277, 3, 1, 4); // SamplesPerPixel
            WriteEntry(stream, 278, 4, 1, (uint)stripRows); // RowsPerStrip
            WriteEntry(stream, 279, 4, (uint)stripCount, stripByteCountsValues[i]); // StripByteCounts
            WriteEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (contiguous)
            WriteEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
            if (usePredictor) {
                WriteEntry(stream, PredictorTag, 3, 1, 2); // Predictor (horizontal)
            }

            WriteUInt32(stream, nextOffsets[i]); // next IFD

            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);

            if (useArrays) {
                var offsets = stripOffsets![i];
                var counts = stripByteCounts![i];
                for (var s = 0; s < stripCount; s++) {
                    WriteUInt32(stream, offsets[s]);
                }
                for (var s = 0; s < stripCount; s++) {
                    WriteUInt32(stream, counts[s]);
                }
            }

            if (compression != TiffCompression.None) {
                var encoded = stripData![i];
                for (var s = 0; s < stripCount; s++) {
                    var data = encoded[s];
                    stream.Write(data, 0, data.Length);
                }
            } else {
                var rowBytes = page.Width * 4;
                var rowBuffer = usePredictor ? new byte[rowBytes] : null;
                for (var s = 0; s < stripCount; s++) {
                    var rowsInStrip = Math.Min(stripRows, page.Height - s * stripRows);
                    for (var y = 0; y < rowsInStrip; y++) {
                        var srcRow = (s * stripRows + y) * page.Stride;
                        if (usePredictor) {
                            page.Rgba.AsSpan(srcRow, rowBytes).CopyTo(rowBuffer);
                            ApplyHorizontalPredictor(rowBuffer!, rowBytes, rows: 1, bytesPerPixel: 4);
                            stream.Write(rowBuffer!, 0, rowBuffer!.Length);
                        } else {
                            stream.Write(page.Rgba, srcRow, rowBytes);
                        }
                    }
                }
            }
        }
    }

    private static void WriteRgba32PagesPerPage(Stream stream, TiffRgba32Page[] pages) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (pages is null) throw new ArgumentNullException(nameof(pages));
        if (pages.Length == 0) throw new ArgumentException("At least one page is required.", nameof(pages));

        var entryCount = EntryCount;
        var ifdSize = 2 + entryCount * 12 + 4;
        var pageCount = pages.Length;
        var ifdOffsets = new uint[pageCount];
        var bitsOffsets = new uint[pageCount];
        var imageSizes = new uint[pageCount];
        var nextOffsets = new uint[pageCount];
        var stripCounts = new int[pageCount];
        var stripOffsetsValues = new uint[pageCount];
        var stripByteCountsValues = new uint[pageCount];
        var stripRowsPerPage = new int[pageCount];
        var compressions = new TiffCompression[pageCount];

        uint[][]? stripOffsets = null;
        uint[][]? stripByteCounts = null;
        byte[][][]? stripData = null;

        long cursor = 8;
        for (var i = 0; i < pageCount; i++) {
            var page = pages[i];
            ValidatePage(page);

            var rowSize = page.Width * 4;
            var stripRows = page.RowsPerStrip <= 0 ? page.Height : page.RowsPerStrip;
            if (stripRows <= 0) throw new ArgumentOutOfRangeException(nameof(page.RowsPerStrip));
            stripRows = Math.Min(stripRows, page.Height);
            stripRowsPerPage[i] = stripRows;

            var stripCount = (page.Height + stripRows - 1) / stripRows;
            stripCounts[i] = stripCount;
            var useArrays = stripCount > 1;
            var dataOffset = cursor + ifdSize + BitsPerSampleSize + (useArrays ? stripCount * 8L : 0);

            if (dataOffset > uint.MaxValue) throw new ArgumentException("TIFF content exceeds 4GB.");
            ifdOffsets[i] = (uint)cursor;
            bitsOffsets[i] = (uint)(cursor + ifdSize);
            stripOffsetsValues[i] = useArrays ? (uint)(cursor + ifdSize + BitsPerSampleSize) : (uint)dataOffset;
            stripByteCountsValues[i] = useArrays ? (uint)(cursor + ifdSize + BitsPerSampleSize + stripCount * 4L) : 0u;

            byte[][]? encoded = null;
            var compressionMode = page.Compression;
            var compression = compressionMode == TiffCompressionMode.Auto
                ? SelectAutoCompression(page, stripRows, rowSize, out encoded)
                : ResolveCompression(compressionMode);

            compressions[i] = compression;

            if (compression != TiffCompression.None) {
                encoded ??= EncodePageStrips(page, stripRows, rowSize, compression, usePredictor: false);
                stripData ??= new byte[pageCount][][];
                stripByteCounts ??= new uint[pageCount][];
                stripOffsets ??= new uint[pageCount][];
                stripData[i] = encoded;
                stripByteCounts[i] = new uint[stripCount];
                stripOffsets[i] = useArrays ? new uint[stripCount] : Array.Empty<uint>();

                var current = (uint)dataOffset;
                for (var s = 0; s < stripCount; s++) {
                    var size = (uint)encoded[s].Length;
                    stripByteCounts[i][s] = size;
                    if (useArrays) {
                        stripOffsets[i][s] = current;
                        current += size;
                    }
                }

                imageSizes[i] = current - (uint)dataOffset;
                if (!useArrays) {
                    stripOffsetsValues[i] = (uint)dataOffset;
                    stripByteCountsValues[i] = stripByteCounts[i][0];
                }
            } else {
                var imageSize = (long)rowSize * page.Height;
                if (imageSize > uint.MaxValue) throw new ArgumentException("TIFF image exceeds 4GB.");
                imageSizes[i] = (uint)imageSize;
                if (useArrays) {
                    stripOffsets ??= new uint[pageCount][];
                    stripByteCounts ??= new uint[pageCount][];
                    stripOffsets[i] = new uint[stripCount];
                    stripByteCounts[i] = new uint[stripCount];
                    var current = (uint)dataOffset;
                    for (var s = 0; s < stripCount; s++) {
                        var rowsInStrip = Math.Min(stripRows, page.Height - s * stripRows);
                        var count = (uint)(rowsInStrip * rowSize);
                        stripOffsets[i][s] = current;
                        stripByteCounts[i][s] = count;
                        current += count;
                    }
                    imageSizes[i] = current - (uint)dataOffset;
                } else {
                    stripByteCountsValues[i] = (uint)imageSize;
                }
            }

            cursor = dataOffset + imageSizes[i];
            if (cursor > uint.MaxValue) throw new ArgumentException("TIFF content exceeds 4GB.");
        }

        for (var i = 0; i < pageCount; i++) {
            nextOffsets[i] = i < pageCount - 1 ? ifdOffsets[i + 1] : 0;
        }

        WriteHeader(stream);
        for (var i = 0; i < pageCount; i++) {
            var page = pages[i];
            var stripCount = stripCounts[i];
            var stripRows = stripRowsPerPage[i];
            var useArrays = stripCount > 1;
            var compression = compressions[i];

            WriteUInt16(stream, entryCount);

            WriteEntry(stream, 256, 4, 1, (uint)page.Width); // ImageWidth
            WriteEntry(stream, 257, 4, 1, (uint)page.Height); // ImageLength
            WriteEntry(stream, 258, 3, 4, bitsOffsets[i]); // BitsPerSample
            WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
            WriteEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
            WriteEntry(stream, 273, 4, (uint)stripCount, stripOffsetsValues[i]); // StripOffsets
            WriteEntry(stream, 277, 3, 1, 4); // SamplesPerPixel
            WriteEntry(stream, 278, 4, 1, (uint)stripRows); // RowsPerStrip
            WriteEntry(stream, 279, 4, (uint)stripCount, stripByteCountsValues[i]); // StripByteCounts
            WriteEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (contiguous)
            WriteEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)

            WriteUInt32(stream, nextOffsets[i]); // next IFD

            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);

            if (useArrays) {
                var offsets = stripOffsets![i];
                var counts = stripByteCounts![i];
                for (var s = 0; s < stripCount; s++) {
                    WriteUInt32(stream, offsets[s]);
                }
                for (var s = 0; s < stripCount; s++) {
                    WriteUInt32(stream, counts[s]);
                }
            }

            if (compression != TiffCompression.None) {
                var encoded = stripData![i];
                for (var s = 0; s < stripCount; s++) {
                    var data = encoded[s];
                    stream.Write(data, 0, data.Length);
                }
            } else {
                var rowBytes = page.Width * 4;
                for (var s = 0; s < stripCount; s++) {
                    var rowsInStrip = Math.Min(stripRows, page.Height - s * stripRows);
                    for (var y = 0; y < rowsInStrip; y++) {
                        var srcRow = (s * stripRows + y) * page.Stride;
                        stream.Write(page.Rgba, srcRow, rowBytes);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Writes a tiled TIFF byte array from RGBA buffers (multi-page).
    /// </summary>
    public static byte[] WriteRgba32PagesTiled(int tileWidth, int tileHeight, params TiffRgba32Page[] pages) {
        using var ms = new MemoryStream();
        WriteRgba32PagesTiled(ms, pages, tileWidth, tileHeight, TiffCompression.None, usePredictor: false);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a tiled TIFF byte array from RGBA buffers (multi-page) with compression and predictor.
    /// </summary>
    public static byte[] WriteRgba32PagesTiled(
        int tileWidth,
        int tileHeight,
        TiffCompression compression,
        bool usePredictor,
        params TiffRgba32Page[] pages) {
        using var ms = new MemoryStream();
        WriteRgba32PagesTiled(ms, pages, tileWidth, tileHeight, compression, usePredictor);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a tiled TIFF to a stream from RGBA buffers (multi-page) with compression and predictor.
    /// </summary>
    public static void WriteRgba32PagesTiled(
        Stream stream,
        TiffRgba32Page[] pages,
        int tileWidth,
        int tileHeight,
        TiffCompression compression,
        bool usePredictor) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (pages is null) throw new ArgumentNullException(nameof(pages));
        if (pages.Length == 0) throw new ArgumentException("At least one page is required.", nameof(pages));
        if (tileWidth <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        if (compression != TiffCompression.None && compression != TiffCompression.PackBits && compression != TiffCompression.Deflate && compression != TiffCompression.Lzw) {
            throw new ArgumentOutOfRangeException(nameof(compression));
        }

        var entryCount = (ushort)(12 + (usePredictor ? 1 : 0));
        var ifdSize = 2 + entryCount * 12 + 4;
        var pageCount = pages.Length;
        var ifdOffsets = new uint[pageCount];
        var bitsOffsets = new uint[pageCount];
        var tileCounts = new int[pageCount];
        var tileOffsetsValues = new uint[pageCount];
        var tileByteCountsValues = new uint[pageCount];
        var nextOffsets = new uint[pageCount];
        uint[][]? tileOffsets = null;
        uint[][]? tileByteCounts = null;
        byte[][][]? tileData = null;

        long cursor = 8;
        for (var i = 0; i < pageCount; i++) {
            var page = pages[i];
            ValidatePage(page);

            var tilesAcross = (page.Width + tileWidth - 1) / tileWidth;
            var tilesDown = (page.Height + tileHeight - 1) / tileHeight;
            if (tilesAcross <= 0 || tilesDown <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));

            var tileCount = checked(tilesAcross * tilesDown);
            tileCounts[i] = tileCount;

            var tileRowBytes = checked(tileWidth * 4);
            var tileBytesLong = (long)tileRowBytes * tileHeight;
            if (tileBytesLong > int.MaxValue) throw new ArgumentException("Tile size is too large.");
            var tileBytes = (int)tileBytesLong;

            ifdOffsets[i] = (uint)cursor;
            bitsOffsets[i] = (uint)(cursor + ifdSize);
            var tableOffset = cursor + ifdSize + BitsPerSampleSize;
            var dataOffset = tableOffset + (tileCount > 1 ? tileCount * 8L : 0L);
            if (dataOffset > uint.MaxValue) throw new ArgumentException("TIFF content exceeds 4GB.");

            tileOffsetsValues[i] = tileCount > 1 ? (uint)tableOffset : (uint)dataOffset;
            tileByteCountsValues[i] = tileCount > 1 ? (uint)(tableOffset + tileCount * 4L) : 0u;

            if (compression != TiffCompression.None) {
                tileData ??= new byte[pageCount][][];
                tileByteCounts ??= new uint[pageCount][];
                tileOffsets ??= new uint[pageCount][];
                tileData[i] = new byte[tileCount][];
                tileByteCounts[i] = new uint[tileCount];
                tileOffsets[i] = tileCount > 1 ? new uint[tileCount] : Array.Empty<uint>();
                var tileBuffer = new byte[tileBytes];
                var current = (uint)dataOffset;
                for (var t = 0; t < tileCount; t++) {
                    FillTileRgba(page.Rgba, page.Width, page.Height, page.Stride, tileWidth, tileHeight, tilesAcross, t, tileBuffer);
                    if (usePredictor) {
                        ApplyHorizontalPredictor(tileBuffer, tileRowBytes, tileHeight, bytesPerPixel: 4);
                    }
                    var encoded = EncodeCompressed(tileBuffer, compression, tileRowBytes, tileHeight);
                    tileData[i][t] = encoded;
                    tileByteCounts[i][t] = (uint)encoded.Length;
                    if (tileCount > 1) {
                        tileOffsets[i][t] = current;
                        current += tileByteCounts[i][t];
                    } else {
                        tileByteCountsValues[i] = tileByteCounts[i][t];
                        current += tileByteCounts[i][t];
                    }
                }
                cursor = current;
            } else {
                if (tileCount > 1) {
                    tileOffsets ??= new uint[pageCount][];
                    tileByteCounts ??= new uint[pageCount][];
                    tileOffsets[i] = new uint[tileCount];
                    tileByteCounts[i] = new uint[tileCount];
                    var current = (uint)dataOffset;
                    for (var t = 0; t < tileCount; t++) {
                        tileOffsets[i][t] = current;
                        tileByteCounts[i][t] = (uint)tileBytes;
                        current += (uint)tileBytes;
                    }
                    cursor = current;
                } else {
                    tileByteCountsValues[i] = (uint)tileBytes;
                    cursor = dataOffset + tileBytes;
                }
            }

            if (cursor > uint.MaxValue) throw new ArgumentException("TIFF content exceeds 4GB.");
        }

        for (var i = 0; i < pageCount; i++) {
            nextOffsets[i] = i < pageCount - 1 ? ifdOffsets[i + 1] : 0;
        }

        WriteHeader(stream);
        for (var i = 0; i < pageCount; i++) {
            var page = pages[i];
            var tileCount = tileCounts[i];
            var tilesAcross = (page.Width + tileWidth - 1) / tileWidth;
            var tileRowBytes = tileWidth * 4;
            var tileBytes = tileRowBytes * tileHeight;

            WriteUInt16(stream, entryCount);
            WriteEntry(stream, 256, 4, 1, (uint)page.Width); // ImageWidth
            WriteEntry(stream, 257, 4, 1, (uint)page.Height); // ImageLength
            WriteEntry(stream, 258, 3, 4, bitsOffsets[i]); // BitsPerSample
            WriteEntry(stream, 259, 3, 1, (uint)compression); // Compression
            WriteEntry(stream, 262, 3, 1, 2); // PhotometricInterpretation (RGB)
            WriteEntry(stream, 277, 3, 1, 4); // SamplesPerPixel
            WriteEntry(stream, 322, 4, 1, (uint)tileWidth); // TileWidth
            WriteEntry(stream, 323, 4, 1, (uint)tileHeight); // TileLength
            WriteEntry(stream, 324, 4, (uint)tileCount, tileOffsetsValues[i]); // TileOffsets
            WriteEntry(stream, 325, 4, (uint)tileCount, tileByteCountsValues[i]); // TileByteCounts
            WriteEntry(stream, 284, 3, 1, 1); // PlanarConfiguration (contiguous)
            WriteEntry(stream, 338, 3, 1, 1); // ExtraSamples (unassociated alpha)
            if (usePredictor) {
                WriteEntry(stream, PredictorTag, 3, 1, 2); // Predictor (horizontal)
            }

            WriteUInt32(stream, nextOffsets[i]); // next IFD

            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);
            WriteUInt16(stream, 8);

            if (tileCount > 1) {
                var offsets = tileOffsets![i];
                var counts = tileByteCounts![i];
                for (var t = 0; t < tileCount; t++) {
                    WriteUInt32(stream, offsets[t]);
                }
                for (var t = 0; t < tileCount; t++) {
                    WriteUInt32(stream, counts[t]);
                }
            }

            if (compression != TiffCompression.None) {
                var encoded = tileData![i];
                for (var t = 0; t < tileCount; t++) {
                    var data = encoded[t];
                    stream.Write(data, 0, data.Length);
                }
            } else {
                var tileBuffer = new byte[tileBytes];
                for (var t = 0; t < tileCount; t++) {
                    FillTileRgba(page.Rgba, page.Width, page.Height, page.Stride, tileWidth, tileHeight, tilesAcross, t, tileBuffer);
                    if (usePredictor) {
                        ApplyHorizontalPredictor(tileBuffer, tileRowBytes, tileHeight, bytesPerPixel: 4);
                    }
                    stream.Write(tileBuffer, 0, tileBuffer.Length);
                }
            }
        }
    }

    private static byte[] WriteRgba32Auto(int width, int height, ReadOnlySpan<byte> rgba, int stride, int rowsPerStrip) {
        var stripRows = rowsPerStrip <= 0 ? height : rowsPerStrip;
        var none = WriteRgba32WithCompression(width, height, rgba, stride, stripRows, TiffCompression.None);
        var packBits = WriteRgba32WithCompression(width, height, rgba, stride, stripRows, TiffCompression.PackBits);
        var deflate = WriteRgba32WithCompression(width, height, rgba, stride, stripRows, TiffCompression.Deflate);

        var best = none;
        if (packBits.Length < best.Length) best = packBits;
        if (deflate.Length < best.Length) best = deflate;
        return best;
    }

    private static byte[] WriteRgba32WithCompression(int width, int height, ReadOnlySpan<byte> rgba, int stride, int rowsPerStrip, TiffCompression compression) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride, rowsPerStrip, compression, usePredictor: false);
        return ms.ToArray();
    }

    private static TiffCompression ResolveCompression(TiffCompressionMode compression) {
        return compression switch {
            TiffCompressionMode.None => TiffCompression.None,
            TiffCompressionMode.PackBits => TiffCompression.PackBits,
            TiffCompressionMode.Deflate => TiffCompression.Deflate,
            TiffCompressionMode.Lzw => TiffCompression.Lzw,
            _ => throw new ArgumentOutOfRangeException(nameof(compression))
        };
    }

    private static TiffCompression SelectAutoCompression(TiffRgba32Page page, int stripRows, int rowSize, out byte[][]? encoded) {
        encoded = null;
        var bestCompression = TiffCompression.None;
        var bestSize = (long)rowSize * page.Height;

        var packBits = EncodePageStrips(page, stripRows, rowSize, TiffCompression.PackBits, usePredictor: false);
        var packSize = SumLengths(packBits);
        if (packSize < bestSize) {
            bestSize = packSize;
            bestCompression = TiffCompression.PackBits;
            encoded = packBits;
        }

        var deflate = EncodePageStrips(page, stripRows, rowSize, TiffCompression.Deflate, usePredictor: false);
        var deflateSize = SumLengths(deflate);
        if (deflateSize < bestSize) {
            bestCompression = TiffCompression.Deflate;
            encoded = deflate;
        }

        return bestCompression;
    }

    private static long SumLengths(byte[][] data) {
        long total = 0;
        for (var i = 0; i < data.Length; i++) {
            total += data[i].Length;
        }
        return total;
    }

    private static byte[][] EncodePageStrips(TiffRgba32Page page, int stripRows, int rowSize, TiffCompression compression, bool usePredictor) {
        var stripCount = (page.Height + stripRows - 1) / stripRows;
        var strips = new byte[stripCount][];
        for (var s = 0; s < stripCount; s++) {
            var rowsInStrip = Math.Min(stripRows, page.Height - s * stripRows);
            var rawSize = (long)rowsInStrip * rowSize;
            if (rawSize > int.MaxValue) throw new ArgumentException("TIFF image exceeds 2GB for compressed pages.");
            var raw = new byte[(int)rawSize];
            for (var y = 0; y < rowsInStrip; y++) {
                var srcRow = (s * stripRows + y) * page.Stride;
                page.Rgba.AsSpan(srcRow, rowSize).CopyTo(raw.AsSpan(y * rowSize, rowSize));
            }
            if (usePredictor) {
                ApplyHorizontalPredictor(raw, rowSize, rowsInStrip, bytesPerPixel: 4);
            }
            strips[s] = EncodeCompressed(raw, compression, rowSize, rowsInStrip);
        }
        return strips;
    }

    private static void WriteHeader(Stream stream) {
        stream.WriteByte((byte)'I');
        stream.WriteByte((byte)'I');
        WriteUInt16(stream, 42);
        WriteUInt32(stream, 8);
    }

    private static void WriteEntry(Stream stream, ushort tag, ushort type, uint count, uint value) {
        WriteUInt16(stream, tag);
        WriteUInt16(stream, type);
        WriteUInt32(stream, count);
        WriteUInt32(stream, value);
    }

    private static void WriteUInt16(Stream stream, ushort value) {
        stream.WriteByte((byte)value);
        stream.WriteByte((byte)(value >> 8));
    }

    private static void WriteUInt32(Stream stream, uint value) {
        stream.WriteByte((byte)value);
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 24));
    }

    private static byte[] PackBitsEncode(ReadOnlySpan<byte> data) {
        var output = new byte[data.Length * 2 + 16];
        var outIndex = 0;
        var i = 0;
        while (i < data.Length) {
            var runStart = i;
            var runValue = data[i];
            var runLength = 1;
            while (runStart + runLength < data.Length && runLength < 128 && data[runStart + runLength] == runValue) {
                runLength++;
            }

            if (runLength >= 3) {
                output[outIndex++] = (byte)(257 - runLength);
                output[outIndex++] = runValue;
                i += runLength;
                continue;
            }

            var literalStart = i;
            var literalLength = 0;
            while (i < data.Length && literalLength < 128) {
                if (i + 2 < data.Length && data[i] == data[i + 1] && data[i] == data[i + 2]) {
                    break;
                }
                i++;
                literalLength++;
            }

            output[outIndex++] = (byte)(literalLength - 1);
            for (var l = 0; l < literalLength; l++) {
                output[outIndex++] = data[literalStart + l];
            }
        }

        var result = new byte[outIndex];
        Buffer.BlockCopy(output, 0, result, 0, outIndex);
        return result;
    }

    private static byte[] PackBitsEncodeRows(ReadOnlySpan<byte> data, int rowSize, int rows) {
        if (rowSize <= 0 || rows <= 0) return PackBitsEncode(data);
        var needed = (long)rowSize * rows;
        if (needed < 0 || needed > data.Length) throw new ArgumentOutOfRangeException(nameof(data));

        using var ms = new MemoryStream();
        for (var y = 0; y < rows; y++) {
            var row = data.Slice(y * rowSize, rowSize);
            var encoded = PackBitsEncode(row);
            ms.Write(encoded, 0, encoded.Length);
        }
        return ms.ToArray();
    }

    private static byte[] EncodeCompressed(byte[] data, TiffCompression compression) {
        return compression switch {
            TiffCompression.PackBits => PackBitsEncode(data),
            TiffCompression.Deflate => DeflateEncode(data),
            TiffCompression.Lzw => LzwEncode(data),
            _ => throw new ArgumentOutOfRangeException(nameof(compression))
        };
    }

    private static byte[] EncodeCompressed(byte[] data, TiffCompression compression, int rowSize, int rows) {
        if (compression == TiffCompression.PackBits) {
            return PackBitsEncodeRows(data, rowSize, rows);
        }
        return EncodeCompressed(data, compression);
    }

    private static byte[] LzwEncode(ReadOnlySpan<byte> data) {
        const int clear = 256;
        const int eoi = 257;
        const int maxCode = 4096;
        const int earlyChange = 1;

        var writer = new LzwBitWriter(data.Length);
        var dict = new Dictionary<int, int>(4096);
        var codeSize = 9;
        var nextCode = 258;

        writer.Write(clear, codeSize);
        if (data.IsEmpty) {
            writer.Write(eoi, codeSize);
            return writer.ToArray();
        }

        var prefix = (int)data[0];
        for (var i = 1; i < data.Length; i++) {
            var b = data[i];
            var key = (prefix << 8) | b;
            if (dict.TryGetValue(key, out var code)) {
                prefix = code;
                continue;
            }

            writer.Write(prefix, codeSize);
            if (nextCode < maxCode) {
                dict[key] = nextCode++;
                if (nextCode == (1 << codeSize) - earlyChange && codeSize < 12) {
                    codeSize++;
                }
            } else {
                writer.Write(clear, codeSize);
                dict.Clear();
                codeSize = 9;
                nextCode = 258;
            }
            prefix = b;
        }

        writer.Write(prefix, codeSize);
        writer.Write(eoi, codeSize);
        return writer.ToArray();
    }

    private sealed class LzwBitWriter {
        private readonly List<byte> _output;
        private uint _buffer;
        private int _bitCount;

        public LzwBitWriter(int dataLength) {
            _output = new List<byte>(Math.Max(16, dataLength));
        }

        public void Write(int code, int codeSize) {
            _buffer = (_buffer << codeSize) | (uint)code;
            _bitCount += codeSize;
            while (_bitCount >= 8) {
                var shift = _bitCount - 8;
                _output.Add((byte)(_buffer >> shift));
                _bitCount -= 8;
                if (_bitCount == 0) {
                    _buffer = 0;
                } else {
                    _buffer &= (1u << _bitCount) - 1u;
                }
            }
        }

        public byte[] ToArray() {
            if (_bitCount > 0) {
                _output.Add((byte)(_buffer << (8 - _bitCount)));
            }
            return _output.ToArray();
        }
    }

    private static byte[] PackBilevelFromRgba(
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        int stride,
        int packedStride,
        byte threshold,
        ushort photometric) {
        var packed = new byte[packedStride * height];
        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            var dstRow = y * packedStride;
            for (var x = 0; x < width; x++) {
                var src = srcRow + x * 4;
                var a = rgba[src + 3];
                int luminance;
                if (a == 0) {
                    luminance = 255;
                } else {
                    var r = rgba[src + 0];
                    var g = rgba[src + 1];
                    var b = rgba[src + 2];
                    luminance = (r * 77 + g * 150 + b * 29) >> 8;
                }

                var isBlack = luminance < threshold;
                var bit = photometric == 0 ? (isBlack ? 1 : 0) : (isBlack ? 0 : 1);
                if (bit != 0) {
                    var dst = dstRow + (x >> 3);
                    packed[dst] |= (byte)(1 << (7 - (x & 7)));
                }
            }
        }
        return packed;
    }

    private static void FillTileRgba(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        int tileWidth,
        int tileHeight,
        int tilesAcross,
        int tileIndex,
        byte[] tileBuffer) {
        tileBuffer.AsSpan().Clear();
        var tileX = tileIndex % tilesAcross;
        var tileY = tileIndex / tilesAcross;
        var startX = tileX * tileWidth;
        var startY = tileY * tileHeight;
        if (startX >= width || startY >= height) return;

        var copyWidth = Math.Min(tileWidth, width - startX);
        var copyHeight = Math.Min(tileHeight, height - startY);
        var rowBytes = copyWidth * 4;
        for (var y = 0; y < copyHeight; y++) {
            var srcOffset = (startY + y) * stride + startX * 4;
            var dstOffset = y * tileWidth * 4;
            rgba.Slice(srcOffset, rowBytes).CopyTo(tileBuffer.AsSpan(dstOffset, rowBytes));
        }
    }

    private static void FillTileBilevel(
        ReadOnlySpan<byte> packed,
        int width,
        int height,
        int stride,
        int tileWidth,
        int tileHeight,
        int tilesAcross,
        int tileIndex,
        byte[] tileBuffer,
        ushort photometric) {
        var fill = photometric == 0 ? (byte)0x00 : (byte)0xFF;
        Array.Fill(tileBuffer, fill);
        var tileX = tileIndex % tilesAcross;
        var tileY = tileIndex / tilesAcross;
        var startX = tileX * tileWidth;
        var startY = tileY * tileHeight;
        if (startX >= width || startY >= height) return;

        var copyWidth = Math.Min(tileWidth, width - startX);
        var copyHeight = Math.Min(tileHeight, height - startY);
        var tileRowBytes = (tileWidth + 7) / 8;

        for (var y = 0; y < copyHeight; y++) {
            var srcRow = (startY + y) * stride;
            var dstRow = y * tileRowBytes;
            for (var x = 0; x < copyWidth; x++) {
                var srcByte = packed[srcRow + ((startX + x) >> 3)];
                var bit = (srcByte >> (7 - ((startX + x) & 7))) & 1;
                var dstIndex = dstRow + (x >> 3);
                var mask = (byte)(1 << (7 - (x & 7)));
                if (bit != 0) {
                    tileBuffer[dstIndex] |= mask;
                } else {
                    tileBuffer[dstIndex] &= (byte)~mask;
                }
            }
        }
    }

    private static void ApplyHorizontalPredictor(byte[] data, int rowSize, int rows, int bytesPerPixel) {
        if (bytesPerPixel <= 0) return;
        if (rowSize <= bytesPerPixel) return;
        var rowCopy = new byte[rowSize];
        for (var y = 0; y < rows; y++) {
            var rowStart = y * rowSize;
            Buffer.BlockCopy(data, rowStart, rowCopy, 0, rowSize);
            for (var i = bytesPerPixel; i < rowSize; i++) {
                var value = rowCopy[i];
                var prev = rowCopy[i - bytesPerPixel];
                data[rowStart + i] = unchecked((byte)(value - prev));
            }
        }
    }

    private static byte[] DeflateEncode(byte[] data) {
        using var ms = new MemoryStream();
#if NET8_0_OR_GREATER
        using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
            zlib.Write(data, 0, data.Length);
        }
#else
        WriteZlibHeader(ms);
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
            deflate.Write(data, 0, data.Length);
        }
        var adler = ComputeAdler32(data);
        WriteUInt32BE(ms, adler);
#endif
        return ms.ToArray();
    }

#if !NET8_0_OR_GREATER
    private static void WriteZlibHeader(Stream stream) {
        stream.WriteByte(0x78);
        stream.WriteByte(0x9C);
    }

    private static uint ComputeAdler32(byte[] buffer) {
        const uint mod = 65521;
        uint a = 1;
        uint b = 0;
        var offset = 0;
        while (offset < buffer.Length) {
            var n = Math.Min(5552, buffer.Length - offset);
            var limit = offset + n;
            while (offset < limit) {
                a += buffer[offset++];
                b += a;
            }
            a %= mod;
            b %= mod;
        }
        return (b << 16) | a;
    }

    private static void WriteUInt32BE(Stream stream, uint value) {
        stream.WriteByte((byte)((value >> 24) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }
#endif

    private static void ValidatePage(TiffRgba32Page page) {
        if (page.Width <= 0) throw new ArgumentOutOfRangeException(nameof(page.Width));
        if (page.Height <= 0) throw new ArgumentOutOfRangeException(nameof(page.Height));
        if (page.Stride < page.Width * 4) throw new ArgumentOutOfRangeException(nameof(page.Stride));
        if (page.Rgba.Length < (page.Height - 1) * page.Stride + page.Width * 4) {
            throw new ArgumentException("RGBA buffer is too small.", nameof(page.Rgba));
        }
    }
}

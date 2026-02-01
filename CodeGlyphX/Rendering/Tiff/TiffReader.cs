using System;
using System.IO;
using System.IO.Compression;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Minimal TIFF decoder for baseline images (strips/tiles).
/// </summary>
public static class TiffReader {
    private const ushort Magic = 42;
    private const ushort TagImageWidth = 256;
    private const ushort TagImageLength = 257;
    private const ushort TagBitsPerSample = 258;
    private const ushort TagCompression = 259;
    private const ushort TagPhotometric = 262;
    private const ushort TagStripOffsets = 273;
    private const ushort TagSamplesPerPixel = 277;
    private const ushort TagRowsPerStrip = 278;
    private const ushort TagStripByteCounts = 279;
    private const ushort TagPlanarConfiguration = 284;
    private const ushort TagPredictor = 317;
    private const ushort TagExtraSamples = 338;
    private const ushort TagColorMap = 320;
    private const ushort TagTileWidth = 322;
    private const ushort TagTileLength = 323;
    private const ushort TagTileOffsets = 324;
    private const ushort TagTileByteCounts = 325;
    private const string TiffTileLimitMessage = "TIFF tile exceeds size limits.";

    private const ushort TypeByte = 1;
    private const ushort TypeShort = 3;
    private const ushort TypeLong = 4;

    private const int CompressionNone = 1;
    private const int CompressionLzw = 5;
    private const int CompressionDeflate = 8;
    private const int CompressionPackBits = 32773;
    private const int CompressionDeflateAdobe = 32946;

    /// <summary>
    /// Returns true when the buffer looks like a TIFF file header.
    /// </summary>
    public static bool IsTiff(ReadOnlySpan<byte> data) {
        if (data.Length < 8) return false;
        var little = data[0] == (byte)'I' && data[1] == (byte)'I';
        var big = data[0] == (byte)'M' && data[1] == (byte)'M';
        if (!little && !big) return false;
        return ReadU16(data, 2, little) == Magic;
    }

    /// <summary>
    /// Attempts to decode all TIFF pages to RGBA buffers.
    /// </summary>
    public static bool TryDecodePagesRgba32(ReadOnlySpan<byte> data, out TiffRgba32Page[] pages) {
        try {
            pages = DecodePagesRgba32(data);
            return true;
        } catch (FormatException) {
            pages = Array.Empty<TiffRgba32Page>();
            return false;
        }
    }

    /// <summary>
    /// Decodes all TIFF pages to RGBA buffers.
    /// </summary>
    public static TiffRgba32Page[] DecodePagesRgba32(ReadOnlySpan<byte> data) {
        if (!IsTiff(data)) throw new FormatException("Not a TIFF image.");

        var little = data[0] == (byte)'I';
        var ifdOffset = ReadU32(data, 4, little);
        if (ifdOffset > data.Length - 2) throw new FormatException("Invalid TIFF IFD offset.");

        var pages = new System.Collections.Generic.List<TiffRgba32Page>();
        while (ifdOffset != 0) {
            var rgba = DecodeRgba32Internal(data, little, (int)ifdOffset, out var width, out var height, out var nextIfdOffset);
            var stride = DecodeGuards.EnsureByteCount((long)width * 4, "TIFF stride exceeds size limits.");
            pages.Add(new TiffRgba32Page(rgba, width, height, stride));
            ifdOffset = nextIfdOffset <= 0 ? 0 : (uint)nextIfdOffset;
        }

        return pages.ToArray();
    }

    /// <summary>
    /// Decodes the first TIFF page into an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (!IsTiff(data)) throw new FormatException("Not a TIFF image.");

        var little = data[0] == (byte)'I';
        var ifdOffset = ReadU32(data, 4, little);
        if (ifdOffset > data.Length - 2) throw new FormatException("Invalid TIFF IFD offset.");

        return DecodeRgba32Internal(data, little, (int)ifdOffset, out width, out height, out _);
    }

    /// <summary>
    /// Attempts to decode the first TIFF page into an RGBA buffer.
    /// </summary>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        try {
            rgba = DecodeRgba32(data, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    /// <summary>
    /// Decodes a specific TIFF page into an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, int pageIndex, out int width, out int height) {
        if (!IsTiff(data)) throw new FormatException("Not a TIFF image.");
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));

        var little = data[0] == (byte)'I';
        var ifdOffset = ReadU32(data, 4, little);
        if (ifdOffset > data.Length - 2) throw new FormatException("Invalid TIFF IFD offset.");

        var currentOffset = (int)ifdOffset;
        for (var page = 0; page <= pageIndex; page++) {
            var rgba = DecodeRgba32Internal(data, little, currentOffset, out width, out height, out var nextIfdOffset);
            if (page == pageIndex) {
                return rgba;
            }
            if (nextIfdOffset <= 0 || nextIfdOffset > data.Length - 2) {
                throw new FormatException("TIFF page index out of range.");
            }
            currentOffset = nextIfdOffset;
        }

        throw new FormatException("TIFF page index out of range.");
    }

    private static byte[] DecodeRgba32Internal(ReadOnlySpan<byte> data, bool little, int ifdOffset, out int width, out int height, out int nextIfdOffset) {
        if (ifdOffset < 0 || ifdOffset > data.Length - 2) throw new FormatException("Invalid TIFF IFD offset.");

        var entryCount = ReadU16(data, ifdOffset, little);
        var entriesOffset = ifdOffset + 2;
        var maxEntries = Math.Min(entryCount, (ushort)((data.Length - entriesOffset) / 12));

        width = 0;
        height = 0;
        var compression = CompressionNone;
        var photometric = 1;
        var samplesPerPixel = 1;
        var rowsPerStrip = 0;
        var planar = 1;
        var predictor = 1;
        ushort[]? bitsPerSample = null;
        int[]? stripOffsets = null;
        int[]? stripByteCounts = null;
        ushort[]? colorMap = null;
        var hasExtraSamples = false;
        var tileWidth = 0;
        var tileLength = 0;
        int[]? tileOffsets = null;
        int[]? tileByteCounts = null;

        for (var i = 0; i < maxEntries; i++) {
            var entryOffset = entriesOffset + i * 12;
            var tag = ReadU16(data, entryOffset, little);
            var type = ReadU16(data, entryOffset + 2, little);
            var count = ReadU32(data, entryOffset + 4, little);

            if (!TryGetValueSpan(data, entryOffset, little, type, count, out var valueSpan)) continue;

            switch (tag) {
                case TagImageWidth:
                    width = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagImageLength:
                    height = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagBitsPerSample:
                    bitsPerSample = ReadValuesUShort(valueSpan, type, little, (int)count);
                    break;
                case TagCompression:
                    compression = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagPhotometric:
                    photometric = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagStripOffsets:
                    stripOffsets = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
                case TagSamplesPerPixel:
                    samplesPerPixel = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagRowsPerStrip:
                    rowsPerStrip = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagStripByteCounts:
                    stripByteCounts = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
                case TagPlanarConfiguration:
                    planar = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagPredictor:
                    predictor = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagExtraSamples:
                    hasExtraSamples = true;
                    break;
                case TagColorMap:
                    colorMap = ReadValuesUShort(valueSpan, type, little, (int)count);
                    break;
                case TagTileWidth:
                    tileWidth = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagTileLength:
                    tileLength = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagTileOffsets:
                    tileOffsets = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
                case TagTileByteCounts:
                    tileByteCounts = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
            }
        }

        nextIfdOffset = 0;
        var ifdEnd = entriesOffset + entryCount * 12;
        if (ifdEnd >= 0 && ifdEnd + 4 <= data.Length) {
            nextIfdOffset = (int)ReadU32(data, ifdEnd, little);
        }

        var pixelCount = DecodeGuards.EnsurePixelCount(width, height, "TIFF dimensions exceed size limits.");
        if (compression != CompressionNone
            && compression != CompressionPackBits
            && compression != CompressionLzw
            && compression != CompressionDeflate
            && compression != CompressionDeflateAdobe) {
            throw new FormatException("Unsupported TIFF compression.");
        }
        if (planar != 1 && planar != 2) throw new FormatException("Unsupported TIFF planar configuration.");

        if (samplesPerPixel <= 0) {
            samplesPerPixel = bitsPerSample?.Length ?? 1;
        }
        if (bitsPerSample is null || bitsPerSample.Length == 0) {
            bitsPerSample = new ushort[samplesPerPixel];
            for (var i = 0; i < bitsPerSample.Length; i++) bitsPerSample[i] = 8;
        }
        var bitsPerSampleValue = bitsPerSample[0];
        for (var i = 1; i < bitsPerSample.Length; i++) {
            if (bitsPerSample[i] != bitsPerSampleValue) throw new FormatException("Mixed TIFF sample sizes are not supported.");
        }
        var isPacked1 = bitsPerSampleValue == 1;
        if (!isPacked1 && bitsPerSampleValue != 8 && bitsPerSampleValue != 16) {
            throw new FormatException("Only 1-bit, 8-bit, or 16-bit TIFF samples are supported.");
        }
        if (isPacked1 && samplesPerPixel != 1) {
            throw new FormatException("Packed 1-bit TIFF samples with multiple channels are not supported.");
        }
        if (isPacked1 && predictor == 2) {
            throw new FormatException("TIFF predictor is not supported for 1-bit samples.");
        }

        var useTiles = tileOffsets is not null && tileByteCounts is not null && tileOffsets.Length > 0;
        if (!useTiles) {
            if (stripOffsets is null || stripByteCounts is null || stripOffsets.Length == 0) {
                throw new FormatException("Missing TIFF strip data.");
            }
        } else if (tileWidth <= 0 || tileLength <= 0) {
            throw new FormatException("Invalid TIFF tile dimensions.");
        }
        if (rowsPerStrip <= 0) rowsPerStrip = height;

        var rgba = new byte[DecodeGuards.EnsureByteCount((long)pixelCount * 4, "TIFF dimensions exceed size limits.")];
        var paletteSize = colorMap is not null ? colorMap.Length / 3 : 0;
        var paletteStride = paletteSize;

        if (isPacked1) {
            if (photometric == 3 && paletteSize == 0) {
                throw new FormatException("Missing TIFF palette data.");
            }
            if (photometric != 0 && photometric != 1 && photometric != 3) {
                throw new FormatException("Unsupported TIFF photometric for 1-bit samples.");
            }
            var bytesPerRowPacked = DecodeGuards.EnsureByteCount(((long)width + 7) / 8, "TIFF row exceeds size limits.");
            if (!useTiles) {
                if (stripOffsets is null || stripByteCounts is null || stripOffsets.Length == 0) {
                    throw new FormatException("Missing TIFF strip data.");
                }
                var row = 0;
                for (var s = 0; s < stripOffsets.Length && row < height; s++) {
                    var offset = stripOffsets[s];
                    var count = stripByteCounts[Math.Min(s, stripByteCounts.Length - 1)];
                    if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF strip offset.");
                    if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF strip length.");

                    var rowsInStrip = Math.Min(rowsPerStrip, height - row);
                    var expected = DecodeGuards.EnsureByteCount((long)rowsInStrip * bytesPerRowPacked, "TIFF strip exceeds size limits.");
                    var stripSpan = DecodeChunk(data, offset, count, expected, compression, predictor, bytesPerRowPacked, 1, 1, little, out _);

                    for (var r = 0; r < rowsInStrip; r++) {
                        var dstRow = (row + r) * width * 4;
                        var rowOffset = r * bytesPerRowPacked;
                        for (var x = 0; x < width; x++) {
                            var byteIndex = rowOffset + (x >> 3);
                            var bit = (stripSpan[byteIndex] >> (7 - (x & 7))) & 1;
                            WritePixelBit(bit, photometric, colorMap, paletteSize, paletteStride, rgba, dstRow + x * 4);
                        }
                    }
                    row += rowsInStrip;
                }
            } else {
                var tilesAcross = (width + tileWidth - 1) / tileWidth;
                var tilesDown = (height + tileLength - 1) / tileLength;
                var tileCount = Math.Min(tileOffsets!.Length, tileByteCounts!.Length);
                var bytesPerTileRow = (tileWidth + 7) / 8;
                var expectedBytes = DecodeGuards.EnsureByteCount((long)tileLength * bytesPerTileRow, TiffTileLimitMessage);

                for (var t = 0; t < tileCount && t < tilesAcross * tilesDown; t++) {
                    var offset = tileOffsets[t];
                    var count = tileByteCounts[Math.Min(t, tileByteCounts.Length - 1)];
                    if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF tile offset.");
                    if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF tile length.");

                    var tileSpan = DecodeChunk(data, offset, count, expectedBytes, compression, predictor, bytesPerTileRow, 1, 1, little, out _);

                    var tileX = (t % tilesAcross) * tileWidth;
                    var tileY = (t / tilesAcross) * tileLength;
                    for (var ty = 0; ty < tileLength; ty++) {
                        var dstY = tileY + ty;
                        if (dstY >= height) break;
                        var dstRow = dstY * width * 4;
                        var rowOffset = ty * bytesPerTileRow;
                        for (var tx = 0; tx < tileWidth; tx++) {
                            var dstX = tileX + tx;
                            if (dstX >= width) break;
                            var byteIndex = rowOffset + (tx >> 3);
                            var bit = (tileSpan[byteIndex] >> (7 - (tx & 7))) & 1;
                            WritePixelBit(bit, photometric, colorMap, paletteSize, paletteStride, rgba, dstRow + dstX * 4);
                        }
                    }
                }
            }

            return rgba;
        }

        var bytesPerSample = bitsPerSampleValue / 8;
        var bytesPerPixel = DecodeGuards.EnsureByteCount((long)samplesPerPixel * bytesPerSample, "TIFF pixel exceeds size limits.");
        var bytesPerRow = DecodeGuards.EnsureByteCount((long)width * bytesPerPixel, "TIFF row exceeds size limits.");
        var planeRowBytes = DecodeGuards.EnsureByteCount((long)width * bytesPerSample, "TIFF row exceeds size limits.");

        if (!useTiles && planar == 1) {
            var row = 0;
            for (var s = 0; s < stripOffsets!.Length && row < height; s++) {
                var offset = stripOffsets[s];
                var count = stripByteCounts![Math.Min(s, stripByteCounts.Length - 1)];
                if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF strip offset.");
                if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF strip length.");

                var rowsInStrip = Math.Min(rowsPerStrip, height - row);
                var expected = DecodeGuards.EnsureByteCount((long)rowsInStrip * bytesPerRow, "TIFF strip exceeds size limits.");
                var stripSpan = DecodeChunk(data, offset, count, expected, compression, predictor, bytesPerRow, samplesPerPixel, bytesPerSample, little, out _);

                var srcIndex = 0;
                for (var r = 0; r < rowsInStrip; r++) {
                    var dstRow = (row + r) * width * 4;
                    for (var x = 0; x < width; x++) {
                        WritePixel(stripSpan, srcIndex, photometric, samplesPerPixel, bytesPerSample, hasExtraSamples, little, colorMap, paletteSize, paletteStride, rgba, dstRow + x * 4);
                        srcIndex += bytesPerPixel;
                    }
                }
                row += rowsInStrip;
            }
        } else if (!useTiles && planar == 2) {
            if (stripOffsets is null || stripByteCounts is null) throw new FormatException("Missing TIFF strip data.");
            var stripsPerPlane = (height + rowsPerStrip - 1) / rowsPerStrip;
            var expectedEntries = stripsPerPlane * samplesPerPixel;
            if (stripOffsets.Length < expectedEntries || stripByteCounts.Length < expectedEntries) {
                throw new FormatException("Invalid TIFF planar strip layout.");
            }

            var row = 0;
            for (var s = 0; s < stripsPerPlane && row < height; s++) {
                var rowsInStrip = Math.Min(rowsPerStrip, height - row);
                var expected = DecodeGuards.EnsureByteCount((long)rowsInStrip * planeRowBytes, "TIFF strip exceeds size limits.");

                var planes = new byte[samplesPerPixel][];
                for (var p = 0; p < samplesPerPixel; p++) {
                    var index = p * stripsPerPlane + s;
                    var offset = stripOffsets[index];
                    var count = stripByteCounts[index];
                    if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF strip offset.");
                    if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF strip length.");
                    var span = DecodeChunk(data, offset, count, expected, compression, predictor, planeRowBytes, 1, bytesPerSample, little, out var buffer);
                    planes[p] = buffer ?? span.ToArray();
                }

                for (var r = 0; r < rowsInStrip; r++) {
                    var dstRow = (row + r) * width * 4;
                    var rowOffset = r * planeRowBytes;
                    for (var x = 0; x < width; x++) {
                        var srcIndex = rowOffset + x * bytesPerSample;
                        WritePixelPlanar(planes, srcIndex, photometric, samplesPerPixel, bytesPerSample, hasExtraSamples, little, colorMap, paletteSize, paletteStride, rgba, dstRow + x * 4);
                    }
                }

                row += rowsInStrip;
            }
        } else {
            var tilesAcross = (width + tileWidth - 1) / tileWidth;
            var tilesDown = (height + tileLength - 1) / tileLength;
            var tileCount = Math.Min(tileOffsets!.Length, tileByteCounts!.Length);
            var tileRowBytes = DecodeGuards.EnsureByteCount((long)tileWidth * bytesPerPixel, TiffTileLimitMessage);
            var expected = DecodeGuards.EnsureByteCount((long)tileWidth * tileLength * bytesPerPixel, TiffTileLimitMessage);

            if (planar == 1) {
                for (var t = 0; t < tileCount && t < tilesAcross * tilesDown; t++) {
                    var offset = tileOffsets[t];
                    var count = tileByteCounts[Math.Min(t, tileByteCounts.Length - 1)];
                    if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF tile offset.");
                    if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF tile length.");

                    var tileSpan = DecodeChunk(data, offset, count, expected, compression, predictor, tileRowBytes, samplesPerPixel, bytesPerSample, little, out _);

                    var tileX = (t % tilesAcross) * tileWidth;
                    var tileY = (t / tilesAcross) * tileLength;
                    for (var ty = 0; ty < tileLength; ty++) {
                        var dstY = tileY + ty;
                        if (dstY >= height) break;
                        var dstRow = dstY * width * 4;
                        var srcRow = ty * tileRowBytes;
                        for (var tx = 0; tx < tileWidth; tx++) {
                            var dstX = tileX + tx;
                            if (dstX >= width) break;
                            var srcIndex = srcRow + tx * bytesPerPixel;
                            WritePixel(tileSpan, srcIndex, photometric, samplesPerPixel, bytesPerSample, hasExtraSamples, little, colorMap, paletteSize, paletteStride, rgba, dstRow + dstX * 4);
                        }
                    }
                }
            } else {
                var tilesPerPlane = tilesAcross * tilesDown;
                var expectedEntries = tilesPerPlane * samplesPerPixel;
                if (tileOffsets.Length < expectedEntries || tileByteCounts.Length < expectedEntries) {
                    throw new FormatException("Invalid TIFF planar tile layout.");
                }
                var planeTileRowBytes = DecodeGuards.EnsureByteCount((long)tileWidth * bytesPerSample, TiffTileLimitMessage);
                var expectedPlane = DecodeGuards.EnsureByteCount((long)tileWidth * tileLength * bytesPerSample, TiffTileLimitMessage);

                for (var t = 0; t < tilesPerPlane; t++) {
                    var planes = new byte[samplesPerPixel][];
                    for (var p = 0; p < samplesPerPixel; p++) {
                        var index = p * tilesPerPlane + t;
                        var offset = tileOffsets[index];
                        var count = tileByteCounts[index];
                        if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF tile offset.");
                        if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF tile length.");
                        var span = DecodeChunk(data, offset, count, expectedPlane, compression, predictor, planeTileRowBytes, 1, bytesPerSample, little, out var buffer);
                        planes[p] = buffer ?? span.ToArray();
                    }

                    var tileX = (t % tilesAcross) * tileWidth;
                    var tileY = (t / tilesAcross) * tileLength;
                    for (var ty = 0; ty < tileLength; ty++) {
                        var dstY = tileY + ty;
                        if (dstY >= height) break;
                        var dstRow = dstY * width * 4;
                        var srcRow = ty * planeTileRowBytes;
                        for (var tx = 0; tx < tileWidth; tx++) {
                            var dstX = tileX + tx;
                            if (dstX >= width) break;
                            var srcIndex = srcRow + tx * bytesPerSample;
                            WritePixelPlanar(planes, srcIndex, photometric, samplesPerPixel, bytesPerSample, hasExtraSamples, little, colorMap, paletteSize, paletteStride, rgba, dstRow + dstX * 4);
                        }
                    }
                }
            }
        }

        return rgba;
    }

    private static ReadOnlySpan<byte> DecodeChunk(
        ReadOnlySpan<byte> data,
        int offset,
        int count,
        int expected,
        int compression,
        int predictor,
        int bytesPerRow,
        int samplesPerPixel,
        int bytesPerSample,
        bool little,
        out byte[]? buffer) {
        buffer = null;
        var src = data.Slice(offset, count);
        if (compression == CompressionNone) {
            if (count < expected) throw new FormatException("TIFF strip too short.");
            if (predictor == 2) {
                buffer = src.Slice(0, expected).ToArray();
                ApplyPredictor(buffer, bytesPerRow, samplesPerPixel, bytesPerSample, little);
                return buffer;
            }
            return src.Slice(0, expected);
        }

        if (compression == CompressionPackBits) {
            buffer = new byte[expected];
            var written = DecompressPackBits(src, buffer);
            if (written != expected) throw new FormatException("Invalid TIFF PackBits data.");
        } else if (compression == CompressionLzw) {
            buffer = DecompressLzwCompat(src, expected);
        } else {
            buffer = DecompressDeflate(src, expected);
        }

        if (predictor == 2) {
            ApplyPredictor(buffer, bytesPerRow, samplesPerPixel, bytesPerSample, little);
        }

        return buffer;
    }

    private static void WritePixel(
        ReadOnlySpan<byte> source,
        int srcIndex,
        int photometric,
        int samplesPerPixel,
        int bytesPerSample,
        bool hasExtraSamples,
        bool little,
        ushort[]? colorMap,
        int paletteSize,
        int paletteStride,
        byte[] rgba,
        int dst) {
        if (photometric == 3 && paletteSize > 0) {
            var idx = bytesPerSample == 2 ? ReadU16(source, srcIndex, little) : source[srcIndex];
            if (idx < paletteSize) {
                var r0 = (byte)(colorMap![idx] >> 8);
                var g0 = (byte)(colorMap[idx + paletteStride] >> 8);
                var b0 = (byte)(colorMap[idx + 2 * paletteStride] >> 8);
                rgba[dst + 0] = r0;
                rgba[dst + 1] = g0;
                rgba[dst + 2] = b0;
                rgba[dst + 3] = 255;
            } else {
                rgba[dst + 3] = 255;
            }
        } else if (photometric == 2 && samplesPerPixel >= 3) {
            if (bytesPerSample == 2) {
                var r16 = ReadU16(source, srcIndex + 0, little);
                var g16 = ReadU16(source, srcIndex + 2, little);
                var b16 = ReadU16(source, srcIndex + 4, little);
                var a16 = samplesPerPixel >= 4 ? ReadU16(source, srcIndex + 6, little) : (ushort)65535;
                rgba[dst + 0] = Scale16To8(r16);
                rgba[dst + 1] = Scale16To8(g16);
                rgba[dst + 2] = Scale16To8(b16);
                rgba[dst + 3] = Scale16To8(a16);
            } else {
                var r0 = source[srcIndex + 0];
                var g0 = source[srcIndex + 1];
                var b0 = source[srcIndex + 2];
                var a0 = samplesPerPixel >= 4 ? source[srcIndex + 3] : (byte)255;
                rgba[dst + 0] = r0;
                rgba[dst + 1] = g0;
                rgba[dst + 2] = b0;
                rgba[dst + 3] = a0;
            }
        } else if (samplesPerPixel >= 2 && hasExtraSamples) {
            if (bytesPerSample == 2) {
                var v16 = ReadU16(source, srcIndex, little);
                var a16 = ReadU16(source, srcIndex + 2, little);
                if (photometric == 0) v16 = (ushort)(65535 - v16);
                rgba[dst + 0] = Scale16To8(v16);
                rgba[dst + 1] = Scale16To8(v16);
                rgba[dst + 2] = Scale16To8(v16);
                rgba[dst + 3] = Scale16To8(a16);
            } else {
                var v = source[srcIndex];
                var a0 = source[srcIndex + 1];
                if (photometric == 0) v = (byte)(255 - v);
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = a0;
            }
        } else if (samplesPerPixel >= 1) {
            if (bytesPerSample == 2) {
                var v16 = ReadU16(source, srcIndex, little);
                if (photometric == 0) v16 = (ushort)(65535 - v16);
                var v = Scale16To8(v16);
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            } else {
                var v = source[srcIndex];
                if (photometric == 0) v = (byte)(255 - v);
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
        } else {
            rgba[dst + 3] = 255;
        }
    }

    private static void WritePixelBit(
        int bit,
        int photometric,
        ushort[]? colorMap,
        int paletteSize,
        int paletteStride,
        byte[] rgba,
        int dst) {
        if (photometric == 3 && paletteSize > 0) {
            var idx = bit & 1;
            if (idx < paletteSize) {
                var r0 = (byte)(colorMap![idx] >> 8);
                var g0 = (byte)(colorMap[idx + paletteStride] >> 8);
                var b0 = (byte)(colorMap[idx + 2 * paletteStride] >> 8);
                rgba[dst + 0] = r0;
                rgba[dst + 1] = g0;
                rgba[dst + 2] = b0;
                rgba[dst + 3] = 255;
            } else {
                rgba[dst + 3] = 255;
            }
            return;
        }

        var v = (bit & 1) == 0 ? 0 : 255;
        if (photometric == 0) v = 255 - v;
        rgba[dst + 0] = (byte)v;
        rgba[dst + 1] = (byte)v;
        rgba[dst + 2] = (byte)v;
        rgba[dst + 3] = 255;
    }

    private static void WritePixelPlanar(
        byte[][] planes,
        int srcIndex,
        int photometric,
        int samplesPerPixel,
        int bytesPerSample,
        bool hasExtraSamples,
        bool little,
        ushort[]? colorMap,
        int paletteSize,
        int paletteStride,
        byte[] rgba,
        int dst) {
        if (planes.Length == 0) {
            rgba[dst + 3] = 255;
            return;
        }

        if (photometric == 3 && paletteSize > 0) {
            var idx = bytesPerSample == 2 ? ReadU16(planes[0], srcIndex, little) : planes[0][srcIndex];
            if (idx < paletteSize) {
                var r0 = (byte)(colorMap![idx] >> 8);
                var g0 = (byte)(colorMap[idx + paletteStride] >> 8);
                var b0 = (byte)(colorMap[idx + 2 * paletteStride] >> 8);
                rgba[dst + 0] = r0;
                rgba[dst + 1] = g0;
                rgba[dst + 2] = b0;
                rgba[dst + 3] = 255;
            } else {
                rgba[dst + 3] = 255;
            }
            return;
        }

        if (photometric == 2 && samplesPerPixel >= 3 && planes.Length >= 3) {
            if (bytesPerSample == 2) {
                var r16 = ReadU16(planes[0], srcIndex, little);
                var g16 = ReadU16(planes[1], srcIndex, little);
                var b16 = ReadU16(planes[2], srcIndex, little);
                var a16 = (samplesPerPixel >= 4 && planes.Length >= 4) ? ReadU16(planes[3], srcIndex, little) : (ushort)65535;
                rgba[dst + 0] = Scale16To8(r16);
                rgba[dst + 1] = Scale16To8(g16);
                rgba[dst + 2] = Scale16To8(b16);
                rgba[dst + 3] = Scale16To8(a16);
            } else {
                var r0 = planes[0][srcIndex];
                var g0 = planes[1][srcIndex];
                var b0 = planes[2][srcIndex];
                var a0 = (samplesPerPixel >= 4 && planes.Length >= 4) ? planes[3][srcIndex] : (byte)255;
                rgba[dst + 0] = r0;
                rgba[dst + 1] = g0;
                rgba[dst + 2] = b0;
                rgba[dst + 3] = a0;
            }
            return;
        }

        if (samplesPerPixel >= 2 && hasExtraSamples && planes.Length >= 2) {
            if (bytesPerSample == 2) {
                var v16 = ReadU16(planes[0], srcIndex, little);
                var a16 = ReadU16(planes[1], srcIndex, little);
                if (photometric == 0) v16 = (ushort)(65535 - v16);
                rgba[dst + 0] = Scale16To8(v16);
                rgba[dst + 1] = Scale16To8(v16);
                rgba[dst + 2] = Scale16To8(v16);
                rgba[dst + 3] = Scale16To8(a16);
            } else {
                var v = planes[0][srcIndex];
                var a0 = planes[1][srcIndex];
                if (photometric == 0) v = (byte)(255 - v);
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = a0;
            }
            return;
        }

        if (samplesPerPixel >= 1) {
            if (bytesPerSample == 2) {
                var v16 = ReadU16(planes[0], srcIndex, little);
                if (photometric == 0) v16 = (ushort)(65535 - v16);
                var v = Scale16To8(v16);
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            } else {
                var v = planes[0][srcIndex];
                if (photometric == 0) v = (byte)(255 - v);
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
            return;
        }

        rgba[dst + 3] = 255;
    }

    private static int DecompressPackBits(ReadOnlySpan<byte> src, Span<byte> dst) {
        var si = 0;
        var di = 0;
        while (si < src.Length && di < dst.Length) {
            var n = (sbyte)src[si++];
            if (n >= 0) {
                var count = n + 1;
                if (si + count > src.Length || di + count > dst.Length) break;
                src.Slice(si, count).CopyTo(dst.Slice(di, count));
                si += count;
                di += count;
            } else if (n != -128) {
                var count = 1 - n;
                if (si >= src.Length || di + count > dst.Length) break;
                var value = src[si++];
                dst.Slice(di, count).Fill(value);
                di += count;
            }
        }
        return di;
    }

    private static byte[] DecompressLzwCompat(ReadOnlySpan<byte> src, int expected) {
        FormatException? last = null;
        var attempts = new (int earlyChange, bool msb)[] {
            (1, true),
            (0, true),
            (1, false),
            (0, false)
        };
        foreach (var attempt in attempts) {
            try {
                return DecompressLzw(src, expected, attempt.earlyChange, attempt.msb);
            } catch (FormatException ex) {
                last = ex;
            }
        }
        throw last ?? new FormatException("Invalid TIFF LZW data.");
    }

    private static byte[] DecompressLzw(ReadOnlySpan<byte> src, int expected, int earlyChange, bool msb) {
        if (expected <= 0) throw new FormatException("Invalid TIFF LZW output size.");
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

            var inCode = code;
            var stackTop = 0;
            if (code >= nextCode) {
                if (oldCode < 0) throw new FormatException("Invalid TIFF LZW stream.");
                if (stackTop >= stack.Length) throw new FormatException("Invalid TIFF LZW stack overflow.");
                stack[stackTop++] = firstChar;
                code = oldCode;
            }

            while (code >= 256) {
                if ((uint)code >= 4096) throw new FormatException("Invalid TIFF LZW code.");
                if (stackTop >= stack.Length) throw new FormatException("Invalid TIFF LZW stack overflow.");
                stack[stackTop++] = suffix[code];
                code = prefix[code];
            }

            firstChar = (byte)code;
            if (stackTop >= stack.Length) throw new FormatException("Invalid TIFF LZW stack overflow.");
            stack[stackTop++] = firstChar;

            while (stackTop > 0) {
                if (outIndex >= output.Length) throw new FormatException("TIFF LZW output too large.");
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

        if (outIndex != output.Length) throw new FormatException("TIFF LZW output truncated.");
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

    private static int ReadBitsLsb(ReadOnlySpan<byte> data, ref int bitPos, int bitCount) {
        var totalBits = data.Length * 8;
        if (bitPos + bitCount > totalBits) return -1;
        var value = 0;
        for (var i = 0; i < bitCount; i++) {
            var bitIndex = bitPos + i;
            var byteIndex = bitIndex >> 3;
            var shift = bitIndex & 7;
            var bit = (data[byteIndex] >> shift) & 1;
            value |= bit << i;
        }
        bitPos += bitCount;
        return value;
    }

    private static void ApplyPredictor(Span<byte> data, int bytesPerRow, int samplesPerPixel, int bytesPerSample, bool little) {
        if (samplesPerPixel <= 0) return;
        if (bytesPerSample == 1) {
            for (var rowStart = 0; rowStart + bytesPerRow <= data.Length; rowStart += bytesPerRow) {
                for (var i = samplesPerPixel; i < bytesPerRow; i++) {
                    var value = data[rowStart + i] + data[rowStart + i - samplesPerPixel];
                    data[rowStart + i] = (byte)value;
                }
            }
            return;
        }

        if (bytesPerSample != 2) throw new FormatException("Unsupported TIFF predictor sample size.");
        var sampleStride = samplesPerPixel * bytesPerSample;
        for (var rowStart = 0; rowStart + bytesPerRow <= data.Length; rowStart += bytesPerRow) {
            for (var offset = sampleStride; offset < bytesPerRow; offset += bytesPerSample) {
                var value = ReadU16(data, rowStart + offset, little);
                var prev = ReadU16(data, rowStart + offset - sampleStride, little);
                var sum = (ushort)(value + prev);
                WriteU16(data, rowStart + offset, little, sum);
            }
        }
    }

    private static byte Scale16To8(ushort value) {
        return (byte)((value * 255 + 32767) / 65535);
    }

    private static byte[] DecompressDeflate(ReadOnlySpan<byte> src, int expected) {
        using var input = new MemoryStream(src.ToArray(), writable: false);
#if NET8_0_OR_GREATER
        Stream stream = LooksLikeZlib(src)
            ? new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true)
            : new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
#else
        Stream stream;
        if (LooksLikeZlib(src)) {
            if (src.Length < 6) throw new FormatException("Invalid TIFF deflate stream.");
            stream = new DeflateStream(new MemoryStream(src.Slice(2, src.Length - 6).ToArray(), writable: false), CompressionMode.Decompress, leaveOpen: true);
        } else {
            stream = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
        }
#endif
        using (stream) {
            var buffer = new byte[expected];
            ReadExact(stream, buffer);
            return buffer;
        }
    }

    private static bool LooksLikeZlib(ReadOnlySpan<byte> data) {
        if (data.Length < 2) return false;
        var cmf = data[0];
        var flg = data[1];
        if ((cmf & 0x0F) != 8) return false;
        return ((cmf << 8) + flg) % 31 == 0;
    }

    private static void ReadExact(Stream stream, byte[] buffer) {
        var offset = 0;
        while (offset < buffer.Length) {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read <= 0) throw new FormatException("Truncated TIFF data.");
            offset += read;
        }
    }

    private static bool TryGetValueSpan(ReadOnlySpan<byte> data, int entryOffset, bool little, ushort type, uint count, out ReadOnlySpan<byte> valueSpan) {
        valueSpan = default;
        var typeSize = GetTypeSize(type);
        if (typeSize == 0) return false;
        var total = checked((int)(count * (uint)typeSize));
        var valueOffset = entryOffset + 8;
        if (total <= 4) {
            if (valueOffset + total > data.Length) return false;
            valueSpan = data.Slice(valueOffset, total);
            return true;
        }
        var offset = (int)ReadU32(data, valueOffset, little);
        if (offset < 0 || offset + total > data.Length) return false;
        valueSpan = data.Slice(offset, total);
        return true;
    }

    private static int GetTypeSize(ushort type) {
        return type switch {
            TypeByte => 1,
            TypeShort => 2,
            TypeLong => 4,
            _ => 0
        };
    }

    private static uint ReadValue(ReadOnlySpan<byte> span, ushort type, bool little, int index) {
        return type switch {
            TypeByte => span[index],
            TypeShort => ReadU16(span, index * 2, little),
            TypeLong => ReadU32(span, index * 4, little),
            _ => 0
        };
    }

    private static ushort[] ReadValuesUShort(ReadOnlySpan<byte> span, ushort type, bool little, int count) {
        var values = new ushort[count];
        for (var i = 0; i < count; i++) {
            values[i] = (ushort)ReadValue(span, type, little, i);
        }
        return values;
    }

    private static int[] ReadValuesInt(ReadOnlySpan<byte> span, ushort type, bool little, int count) {
        var values = new int[count];
        for (var i = 0; i < count; i++) {
            values[i] = (int)ReadValue(span, type, little, i);
        }
        return values;
    }

    private static ushort ReadU16(ReadOnlySpan<byte> data, int offset, bool little) {
        if (little) {
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static void WriteU16(Span<byte> data, int offset, bool little, ushort value) {
        if (little) {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)(value >> 8);
        } else {
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = (byte)(value & 0xFF);
        }
    }

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset, bool little) {
        if (little) {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }
}

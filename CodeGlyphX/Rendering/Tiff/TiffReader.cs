using System;
using System.IO;
using System.IO.Compression;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Minimal TIFF decoder for baseline images.
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

    private const ushort TypeByte = 1;
    private const ushort TypeShort = 3;
    private const ushort TypeLong = 4;

    private const int CompressionNone = 1;
    private const int CompressionLzw = 5;
    private const int CompressionDeflate = 8;
    private const int CompressionPackBits = 32773;
    private const int CompressionDeflateAdobe = 32946;

    public static bool IsTiff(ReadOnlySpan<byte> data) {
        if (data.Length < 8) return false;
        var little = data[0] == (byte)'I' && data[1] == (byte)'I';
        var big = data[0] == (byte)'M' && data[1] == (byte)'M';
        if (!little && !big) return false;
        return ReadU16(data, 2, little) == Magic;
    }

    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        return DecodeRgba32(data, 0, out width, out height);
    }

    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, int pageIndex, out int width, out int height) {
        if (!IsTiff(data)) throw new FormatException("Not a TIFF image.");
        if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));

        var little = data[0] == (byte)'I';
        var ifdOffset = (int)ReadU32(data, 4, little);
        if (ifdOffset <= 0 || ifdOffset > data.Length - 2) throw new FormatException("Invalid TIFF IFD offset.");

        TiffIfdInfo info = default;
        var currentOffset = ifdOffset;
        for (var i = 0; i <= pageIndex; i++) {
            info = ReadIfd(data, little, currentOffset);
            if (i == pageIndex) break;
            if (info.NextIfdOffset == 0) throw new FormatException("TIFF page index out of range.");
            currentOffset = checked((int)info.NextIfdOffset);
        }

        return DecodeFromIfd(data, little, info, out width, out height);
    }

    private static TiffIfdInfo ReadIfd(ReadOnlySpan<byte> data, bool little, int ifdOffset) {
        if (ifdOffset < 0 || ifdOffset > data.Length - 2) throw new FormatException("Invalid TIFF IFD offset.");

        var entryCount = ReadU16(data, ifdOffset, little);
        var entriesOffset = ifdOffset + 2;
        var maxEntries = Math.Min(entryCount, (ushort)((data.Length - entriesOffset) / 12));

        var info = new TiffIfdInfo {
            Compression = CompressionNone,
            Photometric = 1,
            SamplesPerPixel = 1,
            RowsPerStrip = 0,
            Planar = 1,
            Predictor = 1
        };

        for (var i = 0; i < maxEntries; i++) {
            var entryOffset = entriesOffset + i * 12;
            var tag = ReadU16(data, entryOffset, little);
            var type = ReadU16(data, entryOffset + 2, little);
            var count = ReadU32(data, entryOffset + 4, little);

            if (!TryGetValueSpan(data, entryOffset, little, type, count, out var valueSpan)) continue;

            switch (tag) {
                case TagImageWidth:
                    info.Width = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagImageLength:
                    info.Height = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagBitsPerSample:
                    info.BitsPerSample = ReadValuesUShort(valueSpan, type, little, (int)count);
                    break;
                case TagCompression:
                    info.Compression = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagPhotometric:
                    info.Photometric = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagStripOffsets:
                    info.StripOffsets = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
                case TagSamplesPerPixel:
                    info.SamplesPerPixel = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagRowsPerStrip:
                    info.RowsPerStrip = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagStripByteCounts:
                    info.StripByteCounts = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
                case TagPlanarConfiguration:
                    info.Planar = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagPredictor:
                    info.Predictor = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagExtraSamples:
                    info.HasExtraSamples = true;
                    break;
                case TagColorMap:
                    info.ColorMap = ReadValuesUShort(valueSpan, type, little, (int)count);
                    break;
                case TagTileWidth:
                    info.TileWidth = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagTileLength:
                    info.TileLength = (int)ReadValue(valueSpan, type, little, 0);
                    break;
                case TagTileOffsets:
                    info.TileOffsets = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
                case TagTileByteCounts:
                    info.TileByteCounts = ReadValuesInt(valueSpan, type, little, (int)count);
                    break;
            }
        }

        var nextOffsetPos = entriesOffset + entryCount * 12;
        if (nextOffsetPos + 4 <= data.Length) {
            info.NextIfdOffset = ReadU32(data, nextOffsetPos, little);
        }

        return info;
    }

    private static byte[] DecodeFromIfd(ReadOnlySpan<byte> data, bool little, TiffIfdInfo info, out int width, out int height) {
        width = info.Width;
        height = info.Height;

        if (width <= 0 || height <= 0) throw new FormatException("Invalid TIFF dimensions.");
        if (info.Compression != CompressionNone
            && info.Compression != CompressionPackBits
            && info.Compression != CompressionLzw
            && info.Compression != CompressionDeflate
            && info.Compression != CompressionDeflateAdobe) {
            throw new FormatException("Unsupported TIFF compression.");
        }
        if (info.Planar != 1) throw new FormatException("Unsupported TIFF planar configuration.");

        var samplesPerPixel = info.SamplesPerPixel;
        if (samplesPerPixel <= 0) {
            samplesPerPixel = info.BitsPerSample?.Length ?? 1;
        }
        var bitsPerSample = info.BitsPerSample;
        if (bitsPerSample is null || bitsPerSample.Length == 0) {
            bitsPerSample = new ushort[samplesPerPixel];
            for (var i = 0; i < bitsPerSample.Length; i++) bitsPerSample[i] = 8;
        }
        var bitsPerSampleValue = bitsPerSample[0];
        for (var i = 1; i < bitsPerSample.Length; i++) {
            if (bitsPerSample[i] != bitsPerSampleValue) throw new FormatException("Mixed TIFF sample sizes are not supported.");
        }
        if (bitsPerSampleValue != 8 && bitsPerSampleValue != 16) {
            throw new FormatException("Only 8-bit or 16-bit TIFF samples are supported.");
        }

        var hasTiles = info.TileOffsets is not null && info.TileByteCounts is not null && info.TileOffsets.Length > 0;
        var hasStrips = info.StripOffsets is not null && info.StripByteCounts is not null && info.StripOffsets.Length > 0;
        if (!hasTiles && !hasStrips) throw new FormatException("Missing TIFF strip data.");

        var rgba = new byte[width * height * 4];
        var bytesPerSample = bitsPerSampleValue / 8;
        var bytesPerPixel = samplesPerPixel * bytesPerSample;
        var paletteStride = info.ColorMap is not null ? info.ColorMap.Length / 3 : 0;

        if (hasTiles) {
            DecodeTiles(data, little, info, rgba, width, height, bytesPerSample, bytesPerPixel, samplesPerPixel, paletteStride);
            return rgba;
        }

        DecodeStrips(data, little, info, rgba, width, height, bytesPerSample, bytesPerPixel, samplesPerPixel, paletteStride);
        return rgba;
    }

    private static void DecodeStrips(ReadOnlySpan<byte> data, bool little, TiffIfdInfo info, byte[] rgba, int width, int height, int bytesPerSample, int bytesPerPixel, int samplesPerPixel, int paletteStride) {
        var stripOffsets = info.StripOffsets;
        var stripByteCounts = info.StripByteCounts;
        if (stripOffsets is null || stripByteCounts is null || stripOffsets.Length == 0) {
            throw new FormatException("Missing TIFF strip data.");
        }

        var rowsPerStrip = info.RowsPerStrip;
        if (rowsPerStrip <= 0) rowsPerStrip = height;
        var bytesPerRow = width * bytesPerPixel;
        var row = 0;

        for (var s = 0; s < stripOffsets.Length && row < height; s++) {
            var offset = stripOffsets[s];
            var count = stripByteCounts[Math.Min(s, stripByteCounts.Length - 1)];
            if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF strip offset.");
            if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF strip length.");

            var rowsInStrip = Math.Min(rowsPerStrip, height - row);
            var expected = rowsInStrip * bytesPerRow;
            var src = data.Slice(offset, count);
            var block = DecodeBlock(src, expected, info.Compression, info.Predictor, bytesPerRow, samplesPerPixel, bytesPerSample, little);

            for (var r = 0; r < rowsInStrip; r++) {
                var dstRow = (row + r) * width * 4;
                var srcIndex = r * bytesPerRow;
                WritePixelsRow(block, srcIndex, rgba, dstRow, width, samplesPerPixel, bytesPerSample, bytesPerPixel, info.Photometric, info.HasExtraSamples, little, info.ColorMap, paletteStride);
            }
            row += rowsInStrip;
        }
    }

    private static void DecodeTiles(ReadOnlySpan<byte> data, bool little, TiffIfdInfo info, byte[] rgba, int width, int height, int bytesPerSample, int bytesPerPixel, int samplesPerPixel, int paletteStride) {
        var tileOffsets = info.TileOffsets;
        var tileByteCounts = info.TileByteCounts;
        if (tileOffsets is null || tileByteCounts is null || tileOffsets.Length == 0) {
            throw new FormatException("Missing TIFF tile data.");
        }
        if (info.TileWidth <= 0 || info.TileLength <= 0) throw new FormatException("Invalid TIFF tile size.");

        var tilesAcross = (width + info.TileWidth - 1) / info.TileWidth;
        var tilesDown = (height + info.TileLength - 1) / info.TileLength;
        var tileCount = tilesAcross * tilesDown;
        if (tileOffsets.Length < tileCount || tileByteCounts.Length < tileCount) {
            throw new FormatException("Missing TIFF tile data.");
        }

        var bytesPerTileRow = info.TileWidth * bytesPerPixel;
        var expected = info.TileLength * bytesPerTileRow;

        for (var t = 0; t < tileCount; t++) {
            var offset = tileOffsets[t];
            var count = tileByteCounts[t];
            if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF tile offset.");
            if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF tile length.");

            var src = data.Slice(offset, count);
            var block = DecodeBlock(src, expected, info.Compression, info.Predictor, bytesPerTileRow, samplesPerPixel, bytesPerSample, little);

            var tileRow = t / tilesAcross;
            var tileCol = t % tilesAcross;
            var rowStart = tileRow * info.TileLength;
            var colStart = tileCol * info.TileWidth;
            var rowsInTile = Math.Min(info.TileLength, height - rowStart);
            var colsInTile = Math.Min(info.TileWidth, width - colStart);

            for (var r = 0; r < rowsInTile; r++) {
                var dstRow = (rowStart + r) * width * 4 + colStart * 4;
                var srcIndex = r * bytesPerTileRow;
                WritePixelsRow(block, srcIndex, rgba, dstRow, colsInTile, samplesPerPixel, bytesPerSample, bytesPerPixel, info.Photometric, info.HasExtraSamples, little, info.ColorMap, paletteStride);
            }
        }
    }

    private static ReadOnlySpan<byte> DecodeBlock(ReadOnlySpan<byte> src, int expected, int compression, int predictor, int bytesPerRow, int samplesPerPixel, int bytesPerSample, bool little) {
        if (expected <= 0) throw new FormatException("Invalid TIFF data length.");
        byte[]? buffer = null;
        ReadOnlySpan<byte> block;

        if (compression == CompressionNone) {
            if (src.Length < expected) throw new FormatException("TIFF data block too short.");
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
            block = buffer;
        } else if (compression == CompressionLzw) {
            buffer = DecompressLzwCompat(src, expected);
            block = buffer;
        } else {
            buffer = DecompressDeflate(src, expected);
            block = buffer;
        }

        if (predictor == 2) {
            ApplyPredictor(buffer, bytesPerRow, samplesPerPixel, bytesPerSample, little);
            block = buffer;
        }

        return block;
    }

    private static void WritePixelsRow(ReadOnlySpan<byte> src, int srcIndex, byte[] rgba, int dstOffset, int pixelCount, int samplesPerPixel, int bytesPerSample, int bytesPerPixel, int photometric, bool hasExtraSamples, bool little, ushort[]? colorMap, int paletteStride) {
        for (var x = 0; x < pixelCount; x++) {
            if (photometric == 3 && paletteStride > 0) {
                var idx = bytesPerSample == 2 ? ReadU16(src, srcIndex, little) : src[srcIndex];
                if (idx < paletteStride && colorMap is not null) {
                    var r0 = (byte)(colorMap[idx] >> 8);
                    var g0 = (byte)(colorMap[idx + paletteStride] >> 8);
                    var b0 = (byte)(colorMap[idx + 2 * paletteStride] >> 8);
                    rgba[dstOffset + 0] = r0;
                    rgba[dstOffset + 1] = g0;
                    rgba[dstOffset + 2] = b0;
                    rgba[dstOffset + 3] = 255;
                } else {
                    rgba[dstOffset + 3] = 255;
                }
            } else if (photometric == 2 && samplesPerPixel >= 3) {
                if (bytesPerSample == 2) {
                    var r16 = ReadU16(src, srcIndex + 0, little);
                    var g16 = ReadU16(src, srcIndex + 2, little);
                    var b16 = ReadU16(src, srcIndex + 4, little);
                    var a16 = samplesPerPixel >= 4 ? ReadU16(src, srcIndex + 6, little) : (ushort)65535;
                    rgba[dstOffset + 0] = Scale16To8(r16);
                    rgba[dstOffset + 1] = Scale16To8(g16);
                    rgba[dstOffset + 2] = Scale16To8(b16);
                    rgba[dstOffset + 3] = Scale16To8(a16);
                } else {
                    var r0 = src[srcIndex + 0];
                    var g0 = src[srcIndex + 1];
                    var b0 = src[srcIndex + 2];
                    var a0 = samplesPerPixel >= 4 ? src[srcIndex + 3] : (byte)255;
                    rgba[dstOffset + 0] = r0;
                    rgba[dstOffset + 1] = g0;
                    rgba[dstOffset + 2] = b0;
                    rgba[dstOffset + 3] = a0;
                }
            } else if (samplesPerPixel >= 2 && hasExtraSamples) {
                if (bytesPerSample == 2) {
                    var v16 = ReadU16(src, srcIndex, little);
                    var a16 = ReadU16(src, srcIndex + 2, little);
                    if (photometric == 0) v16 = (ushort)(65535 - v16);
                    rgba[dstOffset + 0] = Scale16To8(v16);
                    rgba[dstOffset + 1] = Scale16To8(v16);
                    rgba[dstOffset + 2] = Scale16To8(v16);
                    rgba[dstOffset + 3] = Scale16To8(a16);
                } else {
                    var v = src[srcIndex];
                    var a0 = src[srcIndex + 1];
                    if (photometric == 0) v = (byte)(255 - v);
                    rgba[dstOffset + 0] = v;
                    rgba[dstOffset + 1] = v;
                    rgba[dstOffset + 2] = v;
                    rgba[dstOffset + 3] = a0;
                }
            } else if (samplesPerPixel >= 1) {
                if (bytesPerSample == 2) {
                    var v16 = ReadU16(src, srcIndex, little);
                    if (photometric == 0) v16 = (ushort)(65535 - v16);
                    var v = Scale16To8(v16);
                    rgba[dstOffset + 0] = v;
                    rgba[dstOffset + 1] = v;
                    rgba[dstOffset + 2] = v;
                    rgba[dstOffset + 3] = 255;
                } else {
                    var v = src[srcIndex];
                    if (photometric == 0) v = (byte)(255 - v);
                    rgba[dstOffset + 0] = v;
                    rgba[dstOffset + 1] = v;
                    rgba[dstOffset + 2] = v;
                    rgba[dstOffset + 3] = 255;
                }
            } else {
                rgba[dstOffset + 3] = 255;
            }

            srcIndex += bytesPerPixel;
            dstOffset += 4;
        }
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

    private struct TiffIfdInfo {
        public int Width;
        public int Height;
        public int Compression;
        public int Photometric;
        public int SamplesPerPixel;
        public int RowsPerStrip;
        public int Planar;
        public int Predictor;
        public ushort[]? BitsPerSample;
        public int[]? StripOffsets;
        public int[]? StripByteCounts;
        public ushort[]? ColorMap;
        public bool HasExtraSamples;
        public int TileWidth;
        public int TileLength;
        public int[]? TileOffsets;
        public int[]? TileByteCounts;
        public uint NextIfdOffset;
    }
}

using System;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Minimal TIFF decoder for uncompressed baseline images.
/// </summary>
internal static class TiffReader {
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
    private const ushort TagExtraSamples = 338;

    private const ushort TypeByte = 1;
    private const ushort TypeShort = 3;
    private const ushort TypeLong = 4;

    public static bool IsTiff(ReadOnlySpan<byte> data) {
        if (data.Length < 8) return false;
        var little = data[0] == (byte)'I' && data[1] == (byte)'I';
        var big = data[0] == (byte)'M' && data[1] == (byte)'M';
        if (!little && !big) return false;
        return ReadU16(data, 2, little) == Magic;
    }

    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (!IsTiff(data)) throw new FormatException("Not a TIFF image.");

        var little = data[0] == (byte)'I';
        var ifdOffset = ReadU32(data, 4, little);
        if (ifdOffset > data.Length - 2) throw new FormatException("Invalid TIFF IFD offset.");

        var entryCount = ReadU16(data, (int)ifdOffset, little);
        var entriesOffset = (int)ifdOffset + 2;
        var maxEntries = Math.Min(entryCount, (ushort)((data.Length - entriesOffset) / 12));

        width = 0;
        height = 0;
        var compression = 1;
        var photometric = 1;
        var samplesPerPixel = 1;
        var rowsPerStrip = 0;
        var planar = 1;
        ushort[]? bitsPerSample = null;
        int[]? stripOffsets = null;
        int[]? stripByteCounts = null;

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
                case TagExtraSamples:
                    _ = valueSpan;
                    break;
            }
        }

        if (width <= 0 || height <= 0) throw new FormatException("Invalid TIFF dimensions.");
        if (compression != 1) throw new FormatException("Unsupported TIFF compression.");
        if (planar != 1) throw new FormatException("Unsupported TIFF planar configuration.");

        if (samplesPerPixel <= 0) {
            samplesPerPixel = bitsPerSample?.Length ?? 1;
        }
        if (bitsPerSample is null || bitsPerSample.Length == 0) {
            bitsPerSample = new ushort[samplesPerPixel];
            for (var i = 0; i < bitsPerSample.Length; i++) bitsPerSample[i] = 8;
        }
        for (var i = 0; i < bitsPerSample.Length; i++) {
            if (bitsPerSample[i] != 8) throw new FormatException("Only 8-bit TIFF samples are supported.");
        }

        if (stripOffsets is null || stripByteCounts is null || stripOffsets.Length == 0) {
            throw new FormatException("Missing TIFF strip data.");
        }
        if (rowsPerStrip <= 0) rowsPerStrip = height;

        var rgba = new byte[width * height * 4];
        var bytesPerPixel = samplesPerPixel;
        var bytesPerRow = width * bytesPerPixel;
        var row = 0;

        for (var s = 0; s < stripOffsets.Length && row < height; s++) {
            var offset = stripOffsets[s];
            var count = stripByteCounts[Math.Min(s, stripByteCounts.Length - 1)];
            if (offset < 0 || offset >= data.Length) throw new FormatException("Invalid TIFF strip offset.");
            if (count < 0 || offset + count > data.Length) throw new FormatException("Invalid TIFF strip length.");

            var rowsInStrip = Math.Min(rowsPerStrip, height - row);
            var expected = rowsInStrip * bytesPerRow;
            if (count < expected) throw new FormatException("TIFF strip too short.");

            var src = data.Slice(offset, expected);
            var srcIndex = 0;
            for (var r = 0; r < rowsInStrip; r++) {
                var dstRow = (row + r) * width * 4;
                for (var x = 0; x < width; x++) {
                    if (photometric == 2 && samplesPerPixel >= 3) {
                        var r0 = src[srcIndex + 0];
                        var g0 = src[srcIndex + 1];
                        var b0 = src[srcIndex + 2];
                        var a0 = samplesPerPixel >= 4 ? src[srcIndex + 3] : (byte)255;
                        rgba[dstRow + x * 4 + 0] = r0;
                        rgba[dstRow + x * 4 + 1] = g0;
                        rgba[dstRow + x * 4 + 2] = b0;
                        rgba[dstRow + x * 4 + 3] = a0;
                    } else if (samplesPerPixel >= 1) {
                        var v = src[srcIndex];
                        if (photometric == 0) v = (byte)(255 - v);
                        rgba[dstRow + x * 4 + 0] = v;
                        rgba[dstRow + x * 4 + 1] = v;
                        rgba[dstRow + x * 4 + 2] = v;
                        rgba[dstRow + x * 4 + 3] = 255;
                    } else {
                        rgba[dstRow + x * 4 + 3] = 255;
                    }
                    srcIndex += bytesPerPixel;
                }
            }
            row += rowsInStrip;
        }

        return rgba;
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

    private static uint ReadU32(ReadOnlySpan<byte> data, int offset, bool little) {
        if (little) {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
    }
}

using System;
using System.IO;
using CodeGlyphX.Rendering.Jpeg;
using Xunit;

namespace CodeGlyphX.Tests.TestHelpers;

internal static class JpegTestHelpers {
    public static string GetFixturePath(string fileName) {
        return FixturePaths.Get("Jpeg", fileName);
    }

    public static (int width, int height) ReadJpegSize(byte[] data) {
        var offset = 2;
        while (offset + 4 < data.Length) {
            if (data[offset] != 0xFF) {
                offset++;
                continue;
            }
            while (offset < data.Length && data[offset] == 0xFF) offset++;
            if (offset >= data.Length) break;
            var marker = data[offset++];
            if (marker == 0xD9 || marker == 0xDA) break;
            if (marker == 0xD8) continue;

            var length = ReadUInt16BE(data, offset);
            offset += 2;
            if (length < 2 || offset + length - 2 > data.Length) break;

            if (marker == 0xC0 || marker == 0xC2) {
                var height = ReadUInt16BE(data, offset + 1);
                var width = ReadUInt16BE(data, offset + 3);
                return (width, height);
            }

            offset += length - 2;
        }

        throw new InvalidDataException("JPEG size not found.");
    }

    public static int ReadExifOrientation(byte[] data) {
        var offset = 2;
        while (offset + 4 < data.Length) {
            if (data[offset] != 0xFF) {
                offset++;
                continue;
            }
            while (offset < data.Length && data[offset] == 0xFF) offset++;
            if (offset >= data.Length) break;
            var marker = data[offset++];
            if (marker == 0xD9 || marker == 0xDA) break;

            var length = ReadUInt16BE(data, offset);
            offset += 2;
            if (length < 2 || offset + length - 2 > data.Length) break;

            if (marker == 0xE1) {
                var segment = new ReadOnlySpan<byte>(data, offset, length - 2);
                if (TryReadExifOrientation(segment, out var orientation)) return orientation;
            }

            offset += length - 2;
        }

        return 1;
    }

    public static void AssertExifOrientationSynthetic(ushort orientation) {
        var baseJpeg = CreatePatternJpeg(12, 9, out var rawWidth, out var rawHeight);
        var baseRgba = JpegReader.DecodeRgba32(baseJpeg, out var baseWidth, out var baseHeight);

        var jpeg = AddExifOrientation(baseJpeg, orientation);
        var rgba = JpegReader.DecodeRgba32(jpeg, out var width, out var height);

        Assert.Equal(rawWidth, baseWidth);
        Assert.Equal(rawHeight, baseHeight);

        var expected = ApplyOrientation(baseRgba, rawWidth, rawHeight, orientation, out var expectedWidth, out var expectedHeight);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        Assert.Equal(expected, rgba);
    }

    private static bool TryReadExifOrientation(ReadOnlySpan<byte> data, out int orientation) {
        orientation = 1;
        if (data.Length < 6) return false;
        if (data[0] != (byte)'E' || data[1] != (byte)'x' || data[2] != (byte)'i' || data[3] != (byte)'f' || data[4] != 0 || data[5] != 0) {
            return false;
        }

        var tiff = data.Slice(6);
        if (tiff.Length < 8) return false;
        var little = tiff[0] == (byte)'I' && tiff[1] == (byte)'I';
        var big = tiff[0] == (byte)'M' && tiff[1] == (byte)'M';
        if (!little && !big) return false;
        if (ReadUInt16(tiff, 2, little) != 0x2A) return false;
        var ifdOffset = ReadUInt32(tiff, 4, little);
        if (ifdOffset > (uint)(tiff.Length - 2)) return false;
        var ifd = tiff.Slice((int)ifdOffset);
        var count = ReadUInt16(ifd, 0, little);
        var entriesOffset = 2;
        for (var i = 0; i < count; i++) {
            var entryOffset = entriesOffset + i * 12;
            if (entryOffset + 12 > ifd.Length) break;
            var tag = ReadUInt16(ifd, entryOffset, little);
            if (tag != 0x0112) continue;
            var type = ReadUInt16(ifd, entryOffset + 2, little);
            var entryCount = ReadUInt32(ifd, entryOffset + 4, little);
            if (type != 3 || entryCount != 1) break;
            var value = ReadUInt16(ifd, entryOffset + 8, little);
            if (value is >= 1 and <= 8) {
                orientation = value;
                return true;
            }
            break;
        }

        return false;
    }

    private static ushort ReadUInt16BE(byte[] data, int offset) {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset, bool little) {
        return little
            ? (ushort)(data[offset] | (data[offset + 1] << 8))
            : (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset, bool little) {
        return little
            ? (uint)(data[offset]
                     | (data[offset + 1] << 8)
                     | (data[offset + 2] << 16)
                     | (data[offset + 3] << 24))
            : (uint)((data[offset] << 24)
                     | (data[offset + 1] << 16)
                     | (data[offset + 2] << 8)
                     | data[offset + 3]);
    }

    private static byte[] CreatePatternJpeg(int width, int height, out int rawWidth, out int rawHeight) {
        rawWidth = width;
        rawHeight = height;
        var rgba = new byte[rawWidth * rawHeight * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var r = (byte)((x * 37 + y * 19) & 0xFF);
                var g = (byte)((x * 13 + y * 29) & 0xFF);
                var b = (byte)((x * 11 + y * 7) & 0xFF);
                var p = (y * width + x) * 4;
                rgba[p + 0] = r;
                rgba[p + 1] = g;
                rgba[p + 2] = b;
                rgba[p + 3] = 255;
            }
        }
        return JpegWriter.WriteRgba(rawWidth, rawHeight, rgba, rawWidth * 4, 100);
    }

    private static byte[] ApplyOrientation(byte[] rgba, int width, int height, int orientation, out int outWidth, out int outHeight) {
        if (orientation <= 1) {
            outWidth = width;
            outHeight = height;
            return rgba;
        }

        var destWidth = (orientation >= 5 && orientation <= 8) ? height : width;
        var destHeight = (orientation >= 5 && orientation <= 8) ? width : height;
        var result = new byte[destWidth * destHeight * 4];

        for (var y = 0; y < destHeight; y++) {
            for (var x = 0; x < destWidth; x++) {
                int sx;
                int sy;
                switch (orientation) {
                    case 2:
                        sx = width - 1 - x;
                        sy = y;
                        break;
                    case 3:
                        sx = width - 1 - x;
                        sy = height - 1 - y;
                        break;
                    case 4:
                        sx = x;
                        sy = height - 1 - y;
                        break;
                    case 5:
                        sx = y;
                        sy = x;
                        break;
                    case 6:
                        sx = y;
                        sy = height - 1 - x;
                        break;
                    case 7:
                        sx = width - 1 - y;
                        sy = height - 1 - x;
                        break;
                    case 8:
                        sx = width - 1 - y;
                        sy = x;
                        break;
                    default:
                        sx = x;
                        sy = y;
                        break;
                }

                var srcIndex = (sy * width + sx) * 4;
                var dstIndex = (y * destWidth + x) * 4;
                result[dstIndex + 0] = rgba[srcIndex + 0];
                result[dstIndex + 1] = rgba[srcIndex + 1];
                result[dstIndex + 2] = rgba[srcIndex + 2];
                result[dstIndex + 3] = rgba[srcIndex + 3];
            }
        }

        outWidth = destWidth;
        outHeight = destHeight;
        return result;
    }

    private static byte[] AddExifOrientation(byte[] jpeg, ushort orientation) {
        var app1 = BuildExifApp1(orientation);
        var result = new byte[jpeg.Length + app1.Length];
        Buffer.BlockCopy(jpeg, 0, result, 0, 2);
        Buffer.BlockCopy(app1, 0, result, 2, app1.Length);
        Buffer.BlockCopy(jpeg, 2, result, 2 + app1.Length, jpeg.Length - 2);
        return result;
    }

    private static byte[] BuildExifApp1(ushort orientation) {
        var data = new byte[6 + 8 + 2 + 12 + 4];
        data[0] = (byte)'E';
        data[1] = (byte)'x';
        data[2] = (byte)'i';
        data[3] = (byte)'f';
        data[4] = 0;
        data[5] = 0;

        var tiff = 6;
        data[tiff + 0] = (byte)'I';
        data[tiff + 1] = (byte)'I';
        data[tiff + 2] = 0x2A;
        data[tiff + 3] = 0x00;
        data[tiff + 4] = 0x08;
        data[tiff + 5] = 0x00;
        data[tiff + 6] = 0x00;
        data[tiff + 7] = 0x00;

        var ifd = tiff + 8;
        data[ifd + 0] = 0x01;
        data[ifd + 1] = 0x00;
        data[ifd + 2] = 0x12;
        data[ifd + 3] = 0x01;
        data[ifd + 4] = 0x03;
        data[ifd + 5] = 0x00;
        data[ifd + 6] = 0x01;
        data[ifd + 7] = 0x00;
        data[ifd + 8] = 0x00;
        data[ifd + 9] = 0x00;
        data[ifd + 10] = (byte)(orientation & 0xFF);
        data[ifd + 11] = (byte)(orientation >> 8);
        data[ifd + 12] = 0x00;
        data[ifd + 13] = 0x00;
        data[ifd + 14] = 0x00;
        data[ifd + 15] = 0x00;
        data[ifd + 16] = 0x00;
        data[ifd + 17] = 0x00;

        var length = data.Length + 2;
        var app1 = new byte[2 + 2 + data.Length];
        app1[0] = 0xFF;
        app1[1] = 0xE1;
        app1[2] = (byte)(length >> 8);
        app1[3] = (byte)(length & 0xFF);
        Buffer.BlockCopy(data, 0, app1, 4, data.Length);
        return app1;
    }
}

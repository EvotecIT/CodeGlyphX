using System;
using System.IO;
using CodeGlyphX.Rendering.Jpeg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class JpegDefaultHuffmanTests {
    [Fact]
    public void Decode_JpegWithoutDht_UsesStandardTables() {
        const int width = 32;
        const int height = 32;
        var stride = width * 4;
        var rgba = new byte[height * stride];

        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var idx = y * stride + x * 4;
                rgba[idx + 0] = (byte)((x * 7 + y * 3) % 256);
                rgba[idx + 1] = (byte)((x * 5 + y * 11) % 256);
                rgba[idx + 2] = (byte)((x * 13 + y * 2) % 256);
                rgba[idx + 3] = 255;
            }
        }

        var jpeg = JpegWriter.WriteRgba(width, height, rgba, stride, quality: 90);
        var stripped = StripDhtSegments(jpeg);

        var decodedOriginal = JpegReader.DecodeRgba32(jpeg, out var w1, out var h1);
        var decodedStripped = JpegReader.DecodeRgba32(stripped, out var w2, out var h2);

        Assert.Equal(w1, w2);
        Assert.Equal(h1, h2);
        Assert.Equal(decodedOriginal, decodedStripped);
    }

    private static byte[] StripDhtSegments(byte[] jpeg) {
        if (jpeg is null) throw new ArgumentNullException(nameof(jpeg));
        if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8) {
            throw new ArgumentException("Invalid JPEG data.", nameof(jpeg));
        }

        using var ms = new MemoryStream(jpeg.Length);
        ms.Write(jpeg, 0, 2);
        var offset = 2;

        while (offset < jpeg.Length) {
            if (jpeg[offset] != 0xFF) {
                ms.Write(jpeg, offset, jpeg.Length - offset);
                break;
            }

            while (offset < jpeg.Length && jpeg[offset] == 0xFF) offset++;
            if (offset >= jpeg.Length) break;

            var marker = jpeg[offset++];
            if (marker == 0xDA) {
                ms.WriteByte(0xFF);
                ms.WriteByte(marker);
                if (offset + 1 >= jpeg.Length) break;
                var length = (jpeg[offset] << 8) | jpeg[offset + 1];
                if (length < 2 || offset + length > jpeg.Length) break;
                ms.Write(jpeg, offset, length);
                offset += length;
                ms.Write(jpeg, offset, jpeg.Length - offset);
                break;
            }

            if (marker == 0xD9) {
                ms.WriteByte(0xFF);
                ms.WriteByte(marker);
                break;
            }

            if (marker >= 0xD0 && marker <= 0xD7) {
                ms.WriteByte(0xFF);
                ms.WriteByte(marker);
                continue;
            }

            if (offset + 1 >= jpeg.Length) break;
            var segLength = (jpeg[offset] << 8) | jpeg[offset + 1];
            if (segLength < 2 || offset + segLength > jpeg.Length) break;

            if (marker != 0xC4) {
                ms.WriteByte(0xFF);
                ms.WriteByte(marker);
                ms.Write(jpeg, offset, segLength);
            }
            offset += segLength;
        }

        return ms.ToArray();
    }
}

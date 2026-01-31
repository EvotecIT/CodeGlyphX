using System;
using CodeGlyphX.Rendering.Jpeg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class JpegEncodeOptionsTests {
    [Theory]
    [InlineData(JpegSubsampling.Y444, 0x11)]
    [InlineData(JpegSubsampling.Y422, 0x21)]
    [InlineData(JpegSubsampling.Y420, 0x22)]
    public void Encode_WithSubsampling_WritesSamplingFactors(JpegSubsampling subsampling, int expectedY) {
        var rgba = CreateTestRgba(16, 16);
        var jpeg = JpegWriter.WriteRgba(16, 16, rgba, 16 * 4, new JpegEncodeOptions {
            Quality = 80,
            Subsampling = subsampling
        });

        var sof = ReadSof(jpeg);
        Assert.NotNull(sof);
        Assert.Equal(expectedY, sof.Value.YSampling);
        Assert.Equal(0x11, sof.Value.CbSampling);
        Assert.Equal(0x11, sof.Value.CrSampling);
    }

    [Fact]
    public void Encode_Progressive_WritesSof2AndMultipleScans() {
        var rgba = CreateTestRgba(16, 16);
        var jpeg = JpegWriter.WriteRgba(16, 16, rgba, 16 * 4, new JpegEncodeOptions {
            Quality = 85,
            Progressive = true
        });

        Assert.Contains((byte)0xC2, jpeg);
        Assert.True(CountMarker(jpeg, 0xDA) >= 2);
    }

    [Fact]
    public void Encode_OptimizedHuffman_DecodesRoundTrip() {
        var rgba = CreateTestRgba(24, 24);
        var jpeg = JpegWriter.WriteRgba(24, 24, rgba, 24 * 4, new JpegEncodeOptions {
            Quality = 80,
            OptimizeHuffman = true,
            Subsampling = JpegSubsampling.Y420
        });

        var decoded = JpegReader.DecodeRgba32(jpeg, out var width, out var height);
        Assert.Equal(24, width);
        Assert.Equal(24, height);
        Assert.Equal(width * height * 4, decoded.Length);
    }

    [Fact]
    public void Encode_Metadata_WritesExifSegment() {
        var rgba = CreateTestRgba(8, 8);
        var jpeg = JpegWriter.WriteRgba(8, 8, rgba, 8 * 4, new JpegEncodeOptions {
            Quality = 75,
            Metadata = new JpegMetadata(exif: new byte[] { 1, 2, 3, 4 })
        });

        var exifPrefix = new byte[] { (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0 };
        Assert.True(FindSequence(jpeg, exifPrefix) >= 0);
    }

    private static byte[] CreateTestRgba(int width, int height) {
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var i = (y * width + x) * 4;
                rgba[i + 0] = (byte)(x * 7 + y * 3);
                rgba[i + 1] = (byte)(255 - x * 5);
                rgba[i + 2] = (byte)(y * 11);
                rgba[i + 3] = 255;
            }
        }
        return rgba;
    }

    private static (byte Marker, int YSampling, int CbSampling, int CrSampling)? ReadSof(byte[] jpeg) {
        var i = 2;
        while (i + 3 < jpeg.Length) {
            if (jpeg[i] != 0xFF) {
                i++;
                continue;
            }
            var marker = jpeg[i + 1];
            i += 2;
            if (marker == 0xD8 || marker == 0xD9) continue;
            if (marker == 0xDA) break;
            if (i + 2 > jpeg.Length) break;
            var len = (jpeg[i] << 8) | jpeg[i + 1];
            if (len < 2 || i + len > jpeg.Length) break;
            if (marker == 0xC0 || marker == 0xC2) {
                var pos = i + 2;
                var components = jpeg[pos + 5];
                var compPos = pos + 6;
                var ySample = 0;
                var cbSample = 0;
                var crSample = 0;
                for (var c = 0; c < components; c++) {
                    var id = jpeg[compPos];
                    var sampling = jpeg[compPos + 1];
                    if (id == 1) ySample = sampling;
                    if (id == 2) cbSample = sampling;
                    if (id == 3) crSample = sampling;
                    compPos += 3;
                }
                return ((byte)marker, ySample, cbSample, crSample);
            }
            i += len;
        }
        return null;
    }

    private static int CountMarker(byte[] jpeg, byte marker) {
        var count = 0;
        for (var i = 0; i + 1 < jpeg.Length; i++) {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == marker) count++;
        }
        return count;
    }

    private static int FindSequence(byte[] data, byte[] needle) {
        for (var i = 0; i <= data.Length - needle.Length; i++) {
            var match = true;
            for (var j = 0; j < needle.Length; j++) {
                if (data[i + j] != needle[j]) {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }
}

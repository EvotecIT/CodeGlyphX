using System.Collections.Generic;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Psd;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PsdDecodeTests {
    [Fact]
    public void Decode_Psd_Rgb_Raw() {
        var psd = BuildRgbPsdRaw(width: 2, height: 1, r: new byte[] { 255, 0 }, g: new byte[] { 0, 255 }, b: new byte[] { 0, 0 });
        Assert.True(ImageReader.TryDetectFormat(psd, out var format));
        Assert.Equal(ImageFormat.Psd, format);
        Assert.True(PsdReader.TryReadDimensions(psd, out var width, out var height));
        Assert.Equal(2, width);
        Assert.Equal(1, height);

        var rgba = ImageReader.DecodeRgba32(psd, out width, out height);
        Assert.Equal(2, width);
        Assert.Equal(1, height);
        Assert.Equal(8, rgba.Length);
        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[1]);
        Assert.Equal(0, rgba[2]);
        Assert.Equal(255, rgba[3]);
        Assert.Equal(0, rgba[4]);
        Assert.Equal(255, rgba[5]);
        Assert.Equal(0, rgba[6]);
        Assert.Equal(255, rgba[7]);
    }

    [Fact]
    public void Decode_Psd_Grayscale_Rle() {
        var psd = BuildGrayscalePsdRle(width: 1, height: 1, value: 128);
        var rgba = ImageReader.DecodeRgba32(psd, out var width, out var height);
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
        Assert.Equal(128, rgba[0]);
        Assert.Equal(128, rgba[1]);
        Assert.Equal(128, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    private static byte[] BuildRgbPsdRaw(int width, int height, byte[] r, byte[] g, byte[] b) {
        var bytes = new List<byte>();
        WriteU32BE(bytes, 0x38425053u); // 8BPS
        WriteU16BE(bytes, 1);
        bytes.AddRange(new byte[6]);
        WriteU16BE(bytes, 3); // channels
        WriteU32BE(bytes, (uint)height);
        WriteU32BE(bytes, (uint)width);
        WriteU16BE(bytes, 8);
        WriteU16BE(bytes, 3); // RGB
        WriteU32BE(bytes, 0);
        WriteU32BE(bytes, 0);
        WriteU32BE(bytes, 0);
        WriteU16BE(bytes, 0); // raw
        bytes.AddRange(r);
        bytes.AddRange(g);
        bytes.AddRange(b);
        return bytes.ToArray();
    }

    private static byte[] BuildGrayscalePsdRle(int width, int height, byte value) {
        var bytes = new List<byte>();
        WriteU32BE(bytes, 0x38425053u); // 8BPS
        WriteU16BE(bytes, 1);
        bytes.AddRange(new byte[6]);
        WriteU16BE(bytes, 1); // channels
        WriteU32BE(bytes, (uint)height);
        WriteU32BE(bytes, (uint)width);
        WriteU16BE(bytes, 8);
        WriteU16BE(bytes, 1); // grayscale
        WriteU32BE(bytes, 0);
        WriteU32BE(bytes, 0);
        WriteU32BE(bytes, 0);
        WriteU16BE(bytes, 1); // RLE

        // RLE row lengths (channels * height)
        WriteU16BE(bytes, 2); // one row, 2 bytes encoded
        bytes.Add(0); // literal count = 1
        bytes.Add(value);
        return bytes.ToArray();
    }

    private static void WriteU16BE(List<byte> bytes, ushort value) {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    private static void WriteU32BE(List<byte> bytes, uint value) {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageReaderHostileTests {
    [Fact]
    public void DecodeRgba32_Rejects_OversizedPngDimensions() {
        var png = BuildPngHeader(width: 100_000, height: 100_000);
        var options = new ImageDecodeOptions { MaxPixels = 10_000_000 };

        var ex = Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(png, options, out _, out _));
        Assert.Contains("got 100000x100000", ex.Message);
        Assert.Contains("max 10,000,000 px", ex.Message);
    }

    [Fact]
    public void TryDecodeRgba32_Rejects_OversizedGifDimensions() {
        var gif = BuildGifHeader(width: 60_000, height: 60_000);
        var options = new ImageDecodeOptions { MaxPixels = 1_000_000 };

        Assert.False(ImageReader.TryDecodeRgba32(gif, options, out _, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Rejects_InputAboveMaxBytes() {
        var data = new byte[32];
        var options = new ImageDecodeOptions { MaxBytes = 8 };

        var ex = Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(data, options, out _, out _));
        Assert.Contains("got 32 B", ex.Message);
        Assert.Contains("max 8 B", ex.Message);
    }


    [Fact]
    public void DecodeRgba32_Rejects_OversizedJpegDimensions() {
        var jpeg = BuildJpegHeader(width: 10_000, height: 10_000);
        var options = new ImageDecodeOptions { MaxPixels = 1_000_000 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(jpeg, options, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Rejects_OversizedPdfDimensions() {
        var pdf = BuildPdfWithFlateImage(width: 64, height: 64);
        var options = new ImageDecodeOptions { MaxPixels = 1 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(pdf, options, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Rejects_OversizedPsdDimensions() {
        var psd = BuildPsdHeader(width: 64, height: 64);
        var options = new ImageDecodeOptions { MaxPixels = 1 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(psd, options, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Rejects_OversizedTiffDimensions() {
        var tiff = BuildTiffHeader(width: 64, height: 64);
        var options = new ImageDecodeOptions { MaxPixels = 1 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(tiff, options, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Rejects_OversizedWebpDimensions() {
        var webp = BuildWebpHeader(width: 64, height: 64);
        var options = new ImageDecodeOptions { MaxPixels = 1 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(webp, options, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Rejects_OversizedIcoDimensions() {
        var ico = BuildIcoHeader(width: 64, height: 64);
        var options = new ImageDecodeOptions { MaxPixels = 1 };

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(ico, options, out _, out _));
    }
    private static byte[] BuildJpegHeader(int width, int height) {
        var data = new byte[21];
        data[0] = 0xFF; // SOI
        data[1] = 0xD8;
        data[2] = 0xFF; // SOF0
        data[3] = 0xC0;
        data[4] = 0x00; // length (17)
        data[5] = 0x11;
        data[6] = 0x08; // precision
        WriteUInt16BE(data, 7, (ushort)height);
        WriteUInt16BE(data, 9, (ushort)width);
        data[11] = 0x03; // components
        data[12] = 0x01; data[13] = 0x11; data[14] = 0x00;
        data[15] = 0x02; data[16] = 0x11; data[17] = 0x00;
        data[18] = 0x03; data[19] = 0x11; data[20] = 0x00;
        return data;
    }

    private static byte[] BuildPngHeader(int width, int height) {
        var data = new byte[24];
        data[0] = 137;
        data[1] = 80;
        data[2] = 78;
        data[3] = 71;
        data[4] = 13;
        data[5] = 10;
        data[6] = 26;
        data[7] = 10;
        data[12] = (byte)'I';
        data[13] = (byte)'H';
        data[14] = (byte)'D';
        data[15] = (byte)'R';
        WriteUInt32BE(data, 16, (uint)width);
        WriteUInt32BE(data, 20, (uint)height);
        return data;
    }

    private static byte[] BuildGifHeader(int width, int height) {
        var data = new byte[10];
        data[0] = (byte)'G';
        data[1] = (byte)'I';
        data[2] = (byte)'F';
        data[3] = (byte)'8';
        data[4] = (byte)'9';
        data[5] = (byte)'a';
        WriteUInt16LE(data, 6, (ushort)width);
        WriteUInt16LE(data, 8, (ushort)height);
        return data;
    }

    private static byte[] BuildPsdHeader(int width, int height) {
        var data = new byte[26];
        data[0] = (byte)'8';
        data[1] = (byte)'B';
        data[2] = (byte)'P';
        data[3] = (byte)'S';
        WriteUInt16BE(data, 4, 1);
        WriteUInt16BE(data, 12, 3);
        WriteUInt32BE(data, 14, (uint)height);
        WriteUInt32BE(data, 18, (uint)width);
        WriteUInt16BE(data, 22, 8);
        WriteUInt16BE(data, 24, 3);
        return data;
    }

    private static byte[] BuildTiffHeader(int width, int height) {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
        bw.Write((byte)'I');
        bw.Write((byte)'I');
        bw.Write((ushort)42);
        bw.Write((uint)8);
        bw.Write((ushort)2);
        WriteTiffEntry(bw, 256, 4, 1, (uint)width);
        WriteTiffEntry(bw, 257, 4, 1, (uint)height);
        bw.Write((uint)0);
        return ms.ToArray();
    }

    private static void WriteTiffEntry(BinaryWriter bw, ushort tag, ushort type, uint count, uint value) {
        bw.Write(tag);
        bw.Write(type);
        bw.Write(count);
        bw.Write(value);
    }

    private static byte[] BuildIcoHeader(int width, int height) {
        var data = new byte[6 + 16];
        data[2] = 1;
        data[4] = 1;
        data[6] = (byte)(width == 256 ? 0 : width);
        data[7] = (byte)(height == 256 ? 0 : height);
        return data;
    }

    private static byte[] BuildWebpHeader(int width, int height) {
        var payload = WebpVp8TestHelper.BuildKeyframePayload(width, height, WebpVp8TestHelper.CreateBoolData(16));
        return WebpVp8TestHelper.BuildWebpVp8(payload);
    }

    private static byte[] BuildPdfWithFlateImage(int width, int height) {
        var payload = new byte[] { 0, 0, 0 };
        byte[] compressed;
        using (var ms = new MemoryStream()) {
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
                deflate.Write(payload, 0, payload.Length);
            }
            compressed = ms.ToArray();
        }

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace /DeviceRGB ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append("/Filter /FlateDecode ");
        sb.Append("/Length ").Append(compressed.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + compressed.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(compressed, 0, output, header.Length, compressed.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + compressed.Length, footer.Length);
        return output;
    }

    private static void WriteUInt16BE(byte[] data, int offset, ushort value) {
        data[offset + 0] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static void WriteUInt32BE(byte[] data, int offset, uint value) {
        data[offset + 0] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteUInt16LE(byte[] data, int offset, ushort value) {
        data[offset + 0] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)(value >> 8);
    }
}

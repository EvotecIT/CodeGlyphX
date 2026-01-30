using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PdfDecodeTests {
    [Fact]
    public void Decode_Pdf_Flate_Image() {
        var rgb = new byte[] { 255, 0, 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, rgb, "/DeviceRGB", "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[1]);
        Assert.Equal(0, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    [Fact]
    public void Decode_Pdf_Flate_Filter_Array() {
        var rgb = new byte[] { 0, 255, 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, rgb, "/DeviceRGB", "/Filter [/FlateDecode] ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0, 255, 0, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_Cmyk_Image() {
        var cmyk = new byte[] { 0, 255, 0, 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, cmyk, "/DeviceCMYK", "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 255, 0, 255, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_DecodeArray_Inverts_Gray() {
        var gray = new byte[] { 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, gray, "/DeviceGray", "/Filter /FlateDecode /Decode [1 0] ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Ascii85_Flate_Image() {
        var rgb = new byte[] { 0, 0, 255 };
        var pdf = BuildPdfWithAscii85FlateImage(1, 1, rgb);

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, rgba);
    }

    private static byte[] BuildPdfWithFlateImage(int width, int height, byte[] payload, string colorSpace, string filterClause) {
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
        sb.Append("/ColorSpace ").Append(colorSpace).Append(' ');
        sb.Append("/BitsPerComponent 8 ");
        sb.Append(filterClause);
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

    private static byte[] BuildPdfWithAscii85FlateImage(int width, int height, byte[] rgb) {
        var compressed = Deflate(rgb);
        var encoded = Ascii85Encode(compressed);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace /DeviceRGB ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append("/Filter [/ASCII85Decode /FlateDecode] ");
        sb.Append("/Length ").Append(encoded.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + encoded.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(encoded, 0, output, header.Length, encoded.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + encoded.Length, footer.Length);
        return output;
    }

    private static byte[] Deflate(byte[] data) {
        using var ms = new MemoryStream();
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
            deflate.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    private static byte[] Ascii85Encode(byte[] data) {
        using var ms = new MemoryStream();
        var index = 0;
        while (index < data.Length) {
            var remaining = Math.Min(4, data.Length - index);
            uint tuple = 0;
            for (var i = 0; i < 4; i++) {
                tuple <<= 8;
                if (i < remaining) tuple |= data[index + i];
            }

            if (remaining == 4 && tuple == 0) {
                ms.WriteByte((byte)'z');
                index += 4;
                continue;
            }

            var encoded = new byte[5];
            for (var i = 4; i >= 0; i--) {
                encoded[i] = (byte)((tuple % 85) + (byte)'!');
                tuple /= 85;
            }

            var count = remaining + 1;
            ms.Write(encoded, 0, count);
            index += remaining;
        }

        ms.WriteByte((byte)'~');
        ms.WriteByte((byte)'>');
        return ms.ToArray();
    }
}

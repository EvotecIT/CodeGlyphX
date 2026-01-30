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
        var pdf = BuildPdfWithFlateImage(1, 1, rgb);

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[1]);
        Assert.Equal(0, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    private static byte[] BuildPdfWithFlateImage(int width, int height, byte[] rgb) {
        byte[] compressed;
        using (var ms = new MemoryStream()) {
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
                deflate.Write(rgb, 0, rgb.Length);
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
}

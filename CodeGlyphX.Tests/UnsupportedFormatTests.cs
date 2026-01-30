using System;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class UnsupportedFormatTests {
    [Fact]
    public void Decode_Pdf_ThrowsUnsupported() {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n");
        AssertUnsupported(pdf, "PDF/PS");
    }

    [Fact]
    public void Decode_PostScript_ThrowsUnsupported() {
        var ps = Encoding.ASCII.GetBytes("%!PS-Adobe-3.0\n");
        AssertUnsupported(ps, "PDF/PS");
    }

    [Fact]
    public void Decode_Psd_ThrowsUnsupported() {
        var psd = Encoding.ASCII.GetBytes("8BPS");
        AssertUnsupported(psd, "PSD");
    }

    [Fact]
    public void Decode_Jpeg2000_ThrowsUnsupported() {
        var jp2 = new byte[] {
            0x00, 0x00, 0x00, 0x0C,
            0x6A, 0x50, 0x20, 0x20,
            0x0D, 0x0A, 0x87, 0x0A
        };
        AssertUnsupported(jp2, "JPEG2000");
    }

    [Fact]
    public void Decode_Avif_ThrowsUnsupported() {
        var avif = BuildFtyp("avif", "avif");
        AssertUnsupported(avif, "AVIF/HEIC");
    }

    [Fact]
    public void Decode_Heic_ThrowsUnsupported() {
        var heic = BuildFtyp("heic", "heic");
        AssertUnsupported(heic, "AVIF/HEIC");
    }

    private static byte[] BuildFtyp(string major, string compatible) {
        var data = new byte[24];
        data[0] = 0x00;
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x18;
        WriteAscii(data, 4, "ftyp");
        WriteAscii(data, 8, major);
        data[12] = 0;
        data[13] = 0;
        data[14] = 0;
        data[15] = 0;
        WriteAscii(data, 16, compatible);
        return data;
    }

    private static void WriteAscii(byte[] data, int offset, string value) {
        for (var i = 0; i < 4; i++) {
            data[offset + i] = (byte)value[i];
        }
    }

    private static void AssertUnsupported(byte[] data, string messagePart) {
        var ex = Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(data, out _, out _));
        Assert.Contains(messagePart, ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}

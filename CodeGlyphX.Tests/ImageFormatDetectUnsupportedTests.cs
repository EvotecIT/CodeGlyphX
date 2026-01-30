using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageFormatDetectUnsupportedTests {
    [Fact]
    public void DetectFormat_Pdf() {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n");
        Assert.True(ImageReader.TryDetectFormat(pdf, out var format));
        Assert.Equal(ImageFormat.Pdf, format);
    }

    [Fact]
    public void DetectFormat_PostScript() {
        var ps = Encoding.ASCII.GetBytes("%!PS-Adobe-3.0\n");
        Assert.True(ImageReader.TryDetectFormat(ps, out var format));
        Assert.Equal(ImageFormat.Ps, format);
    }

    [Fact]
    public void DetectFormat_Psd() {
        var psd = Encoding.ASCII.GetBytes("8BPS");
        Assert.True(ImageReader.TryDetectFormat(psd, out var format));
        Assert.Equal(ImageFormat.Psd, format);
    }

    [Fact]
    public void DetectFormat_Jpeg2000() {
        var jp2 = new byte[] {
            0x00, 0x00, 0x00, 0x0C,
            0x6A, 0x50, 0x20, 0x20,
            0x0D, 0x0A, 0x87, 0x0A
        };
        Assert.True(ImageReader.TryDetectFormat(jp2, out var format));
        Assert.Equal(ImageFormat.Jpeg2000, format);
    }

    [Fact]
    public void DetectFormat_Avif() {
        var avif = BuildFtyp("avif", "avif");
        Assert.True(ImageReader.TryDetectFormat(avif, out var format));
        Assert.Equal(ImageFormat.Avif, format);
    }

    [Fact]
    public void DetectFormat_Heic() {
        var heic = BuildFtyp("heic", "heic");
        Assert.True(ImageReader.TryDetectFormat(heic, out var format));
        Assert.Equal(ImageFormat.Heic, format);
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
}

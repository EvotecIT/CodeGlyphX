using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageFormatDecodeTests {
    private const string Payload = "FORMAT-OK";

    [Fact]
    public void Decode_Png() => AssertRoundTrip(QrPngRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Jpeg() => AssertRoundTrip(QrJpegRenderer.Render(Encode(), DefaultOptions(), quality: 100), decodeQr: false);

    [Fact]
    public void Decode_Bmp() => AssertRoundTrip(QrBmpRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Gif_Render() => AssertRoundTrip(QrGifRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Ppm() => AssertRoundTrip(QrPpmRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Pgm() => AssertRoundTrip(QrPgmRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Pbm() => AssertRoundTrip(QrPbmRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Pam() => AssertRoundTrip(QrPamRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Xbm() => AssertRoundTrip(Encoding.ASCII.GetBytes(QrXbmRenderer.Render(Encode(), DefaultOptions())));

    [Fact]
    public void Decode_Xpm() => AssertRoundTrip(Encoding.ASCII.GetBytes(QrXpmRenderer.Render(Encode(), DefaultOptions())));

    [Fact]
    public void Decode_Tga() => AssertRoundTrip(QrTgaRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Tiff_Render() => AssertRoundTrip(QrTiffRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Ico() => AssertRoundTrip(QrIcoRenderer.Render(Encode(), DefaultOptions()));

    [Fact]
    public void Decode_Tiff() {
        var tiff = Convert.FromBase64String("SUkqAAgAAAAJAAABAwABAAAAAQAAAAEBAwABAAAAAQAAAAIBAwABAAAACAAAAAMBAwABAAAAAQAAAAYBAwABAAAAAQAAABEBBAABAAAAegAAABUBAwABAAAAAQAAABYBBAABAAAAAQAAABcBBAABAAAAAQAAAAAAAAAA");
        Assert.True(ImageReader.TryDecodeRgba32(tiff, out var rgba, out var width, out var height));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
        Assert.Equal(0, rgba[0]);
        Assert.Equal(0, rgba[1]);
        Assert.Equal(0, rgba[2]);
    }

    [Fact]
    public void Detect_Format_Png() {
        var png = QrPngRenderer.Render(Encode(), DefaultOptions());
        Assert.True(ImageReader.TryDetectFormat(png, out var format));
        Assert.Equal(ImageFormat.Png, format);
    }

    [Fact]
    public void Detect_Format_Jpeg() {
        var jpeg = QrJpegRenderer.Render(Encode(), DefaultOptions(), quality: 100);
        Assert.True(ImageReader.TryDetectFormat(jpeg, out var format));
        Assert.Equal(ImageFormat.Jpeg, format);
    }

    [Fact]
    public void Detect_Format_Ico() {
        var ico = QrIcoRenderer.Render(Encode(), DefaultOptions());
        Assert.True(ImageReader.TryDetectFormat(ico, out var format));
        Assert.Equal(ImageFormat.Ico, format);
    }

    [Fact]
    public void Decode_Gif() {
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
        Assert.True(ImageReader.TryDecodeRgba32(gif, out var rgba, out var width, out var height));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
    }

    private static BitMatrix Encode() => QrCodeEncoder.EncodeText(Payload).Modules;

    private static QrPngRenderOptions DefaultOptions() => new() {
        ModuleSize = 8,
        QuietZone = 4
    };

    private static void AssertRoundTrip(byte[] data, bool decodeQr = true) {
        Assert.True(ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height));
        if (!decodeQr) return;
        Assert.True(QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decoded, new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true,
            MaxMilliseconds = 2000
        }));
        Assert.Equal(Payload, decoded.Text);
    }
}

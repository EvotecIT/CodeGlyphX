using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Webp;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

public sealed class WebpDecodeEndToEndTests {
    private readonly ITestOutputHelper _output;

    public WebpDecodeEndToEndTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Webp_EndToEnd_QrDecode_LosslessManaged() {
        const string payload = "WEBP-END-TO-END";

        var modules = QrCodeEncoder.EncodeText(payload).Modules;
        const int moduleSize = 8;
        const int quietZone = 4;

        var (rgba, width, height, stride) = RenderQrToRgba(modules, moduleSize, quietZone);
        var webp = WebpWriter.WriteRgba32(width, height, rgba, stride);
        _output.WriteLine($"Managed WebP bytes: {webp.Length}");

        Assert.True(ImageReader.TryDetectFormat(webp, out var format));
        Assert.Equal(ImageFormat.Webp, format);

        Assert.True(ImageReader.TryReadInfo(webp, out var info));
        Assert.Equal(width, info.Width);
        Assert.Equal(height, info.Height);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decodedRgba, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(width * height * 4, decodedRgba.Length);

        Assert.True(QrDecoder.TryDecode(decodedRgba, decodedWidth, decodedHeight, decodedWidth * 4, PixelFormat.Rgba32, out var decoded, new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true,
            MaxMilliseconds = 2000
        }));
        Assert.Equal(payload, decoded.Text);
    }

    private static (byte[] rgba, int width, int height, int stride) RenderQrToRgba(BitMatrix modules, int moduleSize, int quietZone) {
        var modulesWidth = modules.Width + (quietZone * 2);
        var modulesHeight = modules.Height + (quietZone * 2);
        var width = checked(modulesWidth * moduleSize);
        var height = checked(modulesHeight * moduleSize);
        var stride = checked(width * 4);
        var rgba = new byte[checked(height * stride)];

        Fill(rgba, stride, width, height, 255, 255, 255, 255);

        for (var y = 0; y < modules.Height; y++) {
            for (var x = 0; x < modules.Width; x++) {
                if (!modules[x, y]) continue;
                var px = (x + quietZone) * moduleSize;
                var py = (y + quietZone) * moduleSize;
                FillBlock(rgba, stride, px, py, moduleSize, moduleSize, 0, 0, 0, 255);
            }
        }

        return (rgba, width, height, stride);
    }

    private static void Fill(byte[] rgba, int stride, int width, int height, byte r, byte g, byte b, byte a) {
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + (x * 4);
                rgba[i] = r;
                rgba[i + 1] = g;
                rgba[i + 2] = b;
                rgba[i + 3] = a;
            }
        }
    }

    private static void FillBlock(byte[] rgba, int stride, int x, int y, int blockWidth, int blockHeight, byte r, byte g, byte b, byte a) {
        for (var yy = 0; yy < blockHeight; yy++) {
            var row = (y + yy) * stride;
            for (var xx = 0; xx < blockWidth; xx++) {
                var i = row + ((x + xx) * 4);
                rgba[i] = r;
                rgba[i + 1] = g;
                rgba[i + 2] = b;
                rgba[i + 3] = a;
            }
        }
    }
}

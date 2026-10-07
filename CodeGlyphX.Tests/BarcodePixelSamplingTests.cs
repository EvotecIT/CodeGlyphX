using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodePixelSamplingTests {
    [Fact]
    public void Decode_NativeSvgRaster_IgnoresTransparentPaddingAcrossImageInputs() {
        // The native SVG import is 273x93 at 2.25 pixels per module. Its final
        // transparent column contains black RGB, outside the visible barcode.
        var png = ReadFixture("native-svg-asset.png");
        Assert.True(ImageReader.TryDecodeRgba32(png, out var pixels, out var width, out var height));
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.Code128, out var decoded));
        Assert.Equal("AT-1042", decoded.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var all, BarcodeType.Code128));
        Assert.Equal("AT-1042", Assert.Single(all).Text);
        var options = Code128Options();
        AssertAsset(SymbolScanner.Scan(png, options));
        AssertAsset(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options));
        AssertFile(png, options);
    }

    [Fact]
    public void Scan_SavedDocumentRegion_PreservesFractionalModulesWithOnePixelRuns() {
        // This opaque saved document retains every run at about 1.78 pixels per
        // module; its shortest observed run is one pixel.
        var png = ReadFixture("word-asset-page.png");
        Assert.True(ImageReader.TryDecodeRgba32(png, out var pixels, out var width, out var height));
        var options = Code128Options();
        options.Region = new ImageRegion(72, 242, 216, 90);
        AssertAsset(SymbolScanner.Scan(png, options));
        AssertAsset(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options));
        AssertFile(png, options);
    }

    [Theory]
    [InlineData(BarcodeType.Code128, PixelFormat.Rgba32, false, false)]
    [InlineData(BarcodeType.Code93, PixelFormat.Bgra32, true, false)]
    [InlineData(BarcodeType.Code128, PixelFormat.Argb32, true, true)]
    [InlineData(BarcodeType.Code93, PixelFormat.Abgr32, false, true)]
    public void Decode_AlphaPixels_PreservesStrideOrientationAndFrameChannelOrder(
        BarcodeType type, PixelFormat format, bool vertical, bool reverse) {
        const string text = "ALPHA-1042";
        var native = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(type, text),
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 10, HeightModules = 24 },
            out var sourceWidth, out var sourceHeight, out var sourceStride);
        var width = vertical ? sourceHeight : sourceWidth;
        var height = vertical ? sourceWidth : sourceHeight;
        var stride = width * 4 + 12;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var sourceX = vertical ? y : x;
                var sourceY = vertical ? x : y;
                if (reverse) sourceX = sourceWidth - 1 - sourceX;
                var isBar = native[sourceY * sourceStride + sourceX * 4] == 0;
                // Hidden RGB varies inside transparent spaces. It must not
                // introduce alternating foreground runs or affect visible bars.
                var coloredSpace = !isBar && (x & 1) != 0;
                byte red = coloredSpace ? (byte)48 : (byte)0;
                byte green = coloredSpace ? (byte)16 : (byte)0;
                byte blue = coloredSpace ? (byte)80 : (byte)0;
                byte alpha = isBar ? (byte)192 : ((x & 1) == 0 ? (byte)0 : (byte)64);
                var target = (height - 1 - y) * stride + x * 4;
                switch (format) {
                    case PixelFormat.Rgba32:
                        pixels[target] = red; pixels[target + 1] = green; pixels[target + 2] = blue; pixels[target + 3] = alpha;
                        break;
                    case PixelFormat.Bgra32:
                        pixels[target] = blue; pixels[target + 1] = green; pixels[target + 2] = red; pixels[target + 3] = alpha;
                        break;
                    case PixelFormat.Argb32:
                        pixels[target] = alpha; pixels[target + 1] = red; pixels[target + 2] = green; pixels[target + 3] = blue;
                        break;
                    default:
                        pixels[target] = alpha; pixels[target + 1] = blue; pixels[target + 2] = green; pixels[target + 3] = red;
                        break;
                }
            }
        }

        if (format == PixelFormat.Rgba32 || format == PixelFormat.Bgra32) {
            Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, format, type, out var direct));
            Assert.Equal(text, direct.Text);
#if NET8_0_OR_GREATER
            Assert.True(BarcodeDecoder.TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, format, type, out var span));
            Assert.Equal(text, span.Text);
#endif
        }
        var frame = new ImageFrame(pixels, width, height, stride, format, ImageRowOrder.BottomUp);
        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { type == BarcodeType.Code128 ? SymbolFormat.Code128 : SymbolFormat.Code93 },
            TimeoutMilliseconds = 0, MaxSymbols = 16
        });
        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        Assert.Equal(text, Assert.Single(result.Symbols).Text);
    }

    private static ScanOptions Code128Options() => new ScanOptions {
        Formats = new[] { SymbolFormat.Code128 }, TimeoutMilliseconds = 0, MaxSymbols = 16
    };

    private static void AssertAsset(ScanResult result) {
        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Code128, symbol.Format);
        Assert.Equal("AT-1042", symbol.Text);
    }

    private static byte[] ReadFixture(string name) {
        using var source = typeof(BarcodePixelSamplingTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(source);
        using var buffer = new MemoryStream();
        source!.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void AssertFile(byte[] png, ScanOptions options) {
        var path = Path.Combine(Path.GetTempPath(), "CodeGlyphX-pixels-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            File.WriteAllBytes(path, png);
            AssertAsset(SymbolScanner.ScanFile(path, options));
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

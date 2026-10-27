using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrReviewRegressionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SquareTimingBridges_KeepLightCellEdgesClear_InPixelsPngAndStreams(bool coloured) {
        var qr = QR.Encode("https://evotec.xyz");
        const int module = 20;
        var options = new QrPngRenderOptions { ModuleSize = module, TimingThicknessModules = .2,
            Foreground = coloured ? new Rgba32(10, 20, 30) : Rgba32.Black };
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, options, out var width, out _, out _);
        var light = Enumerable.Range(8, qr.Size - 16).First(x => !qr.Modules[x, 6]);
        int At(double x, double y) => ((int)((options.QuietZone + y) * module) * width + (int)((options.QuietZone + x) * module)) * 4;
        Assert.Equal(options.Foreground.R, pixels[At(light + .5, 6.5)]);
        Assert.Equal(255, pixels[At(light + .5, 6.05)]);
        Assert.Equal(options.Foreground.R, pixels[At(6.5, light + .5)]);
        Assert.Equal(255, pixels[At(6.05, light + .5)]);
        Assert.Equal(pixels, ImageReader.DecodeRgba32(QrPngRenderer.Render(qr.Modules, options), out _, out _));
        using var stream = new MemoryStream();
        QrPngRenderer.RenderToStream(qr.Modules, options, stream);
        Assert.Equal(pixels, ImageReader.DecodeRgba32(stream.ToArray(), out _, out _));
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(-3, 1)]
    [InlineData(1, 0)]
    [InlineData(int.MinValue, 1)]
    public void ActiveModuleShadows_RejectOffsetsOutsideTheQuietZone(int offset, int quiet) {
        var modules = new BitMatrix(21, 21);
        modules[0, 8] = modules[8, 0] = modules[20, 20] = true;
        var options = new QrPngRenderOptions { ModuleSize = 2, QuietZone = quiet,
            ModuleShadowOffsetPx = offset, ModuleShadowColor = new Rgba32(255, 0, 0) };
        Assert.Throws<ArgumentOutOfRangeException>(() => QrPngRenderer.RenderPixels(modules, options, out _, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrPngRenderer.Render(modules, options));
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => QrPngRenderer.RenderToStream(modules, options, stream));
        Assert.Equal(0, stream.Length);
        options.ModuleShadowColor = Rgba32.Transparent;
        Assert.NotEmpty(QrPngRenderer.Render(modules, options));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-2)]
    public void ModuleShadows_AllowTheExactQuietZoneBoundary(int offset) {
        var modules = new BitMatrix(21, 21);
        modules[0, 8] = modules[8, 0] = modules[20, 20] = true;
        var pixels = QrPngRenderer.RenderPixels(modules, new QrPngRenderOptions { ModuleSize = 2, QuietZone = 1,
            ModuleShadowOffsetPx = offset, ModuleShadowColor = new Rgba32(255, 0, 0) }, out var width, out _, out _);
        var edge = offset < 0 ? 0 : width - 1;
        var at = ((offset < 0 ? 16 : edge) * width + edge) * 4;
        Assert.Equal(255, pixels[at]);
        Assert.Equal(0, pixels[at + 1]);
    }

    [Fact]
    public void FallbackUrl_KeepsItsMeasuredSizeWhenTheHeadlineWraps() {
        var single = CaptionUrl("SCAN ME", 16, 5, QrCanvasCaptionWeight.Regular);
        var wrapped = CaptionUrl("EXPLORE COLLECTION", 16, 5, QrCanvasCaptionWeight.Regular);
        Assert.Equal(single.Width, wrapped.Width);
        Assert.Equal(single.Height, wrapped.Height);
        Assert.Equal(single.Ink, wrapped.Ink);
    }

    [Fact]
    public void FallbackUrl_UsesTheRequestedCaptionWeight() {
        var regular = CaptionUrl("SCAN ME", 40, 20, QrCanvasCaptionWeight.Regular);
        var bold = CaptionUrl("SCAN ME", 40, 20, QrCanvasCaptionWeight.Bold);
        Assert.True(bold.Ink > regular.Ink);
        Assert.True(bold.Width >= regular.Width);
    }

    private static (int Width, int Height, int Ink) CaptionUrl(string text, int fontSize, int module, QrCanvasCaptionWeight weight) {
        var qr = QR.Encode("https://evotec.xyz");
        var options = new QrPngRenderOptions { ModuleSize = module, Canvas = new QrCanvasOptions {
            StrokeColor = new Rgba32(255, 0, 0), Caption = new QrCanvasCaptionOptions {
                Text = text, Url = "evotec.xyz", FontSizePx = fontSize, Weight = weight
            }
        } };
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, options, out var width, out var height, out _);
        var points = new List<(int X, int Y)>();
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var p = (y * width + x) * 4;
            if (pixels[p] > pixels[p + 1] && pixels[p] > pixels[p + 2]) points.Add((x, y));
        }
        Assert.NotEmpty(points);
        var bottom = points.Max(p => p.Y);
        var top = bottom;
        var rows = new HashSet<int>(points.Select(p => p.Y));
        while (rows.Contains(top - 1)) top--;
        var url = points.Where(p => p.Y >= top).ToArray();
        return (url.Max(p => p.X) - url.Min(p => p.X) + 1, bottom - top + 1, url.Length);
    }
}

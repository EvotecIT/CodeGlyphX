using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrReviewRegressionTests {
    [Theory]
    [InlineData(QrCanvasCaptionPosition.Top)]
    [InlineData(QrCanvasCaptionPosition.Bottom)]
    public void FallbackUrl_WithoutAHeadline_RemainsVisibleAndWithinTheCanvas(QrCanvasCaptionPosition position) {
        var qr = QR.Encode("https://evotec.xyz");
        byte[]? glyph = null;
        var glyphWidth = 0;
        var glyphHeight = 0;
        foreach (QrCanvasTemplate template in Enum.GetValues(typeof(QrCanvasTemplate))) {
            var options = new QrPngRenderOptions { ModuleSize = 16, Canvas = new QrCanvasOptions {
                Template = template, StrokeColor = new Rgba32(255, 0, 0),
                Caption = new QrCanvasCaptionOptions { Text = "", Url = "evotec.xyz", Position = position,
                    FontSizePx = 40, Color = new Rgba32(0, 0, 255) }
            } };
            var pixels = QrPngRenderer.RenderPixels(qr.Modules, options, out var width, out var height, out _);
            var url = new List<(int X, int Y)>();
            var lastQrInkRow = -1;
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
                var at = (y * width + x) * 4;
                // Only opaque black ink belongs to the QR; decorations and the fallback URL use red.
                if (pixels[at + 3] >= 200 && pixels[at] < 64 && pixels[at + 1] < 64 && pixels[at + 2] < 64) lastQrInkRow = y;
            }
            Assert.True(lastQrInkRow >= 0);
            var quietZoneBottom = lastQrInkRow + 1 + options.QuietZone * options.ModuleSize;
            var urlTop = -1;
            // The unframed case has no red decoration; every red pixel belongs to the URL.
            if (template == QrCanvasTemplate.None) {
                for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
                    var at = (y * width + x) * 4;
                    if (pixels[at] > pixels[at + 1] && pixels[at] > pixels[at + 2]) url.Add((x, y));
                }
                Assert.NotEmpty(url);
                urlTop = url.Min(p => p.Y);
                var cap = url.Max(p => p.Y) - url.Min(p => p.Y) + 1;
                Assert.True(url.Min(p => p.X) >= cap && url.Max(p => p.X) < width - cap);
                Assert.True(url.Min(p => p.Y) >= cap && url.Max(p => p.Y) < height - cap);
                glyphWidth = url.Max(p => p.X) - url.Min(p => p.X) + 1;
                glyphHeight = cap;
                glyph = new byte[glyphWidth * glyphHeight * 4];
                for (var row = 0; row < glyphHeight; row++) Buffer.BlockCopy(pixels,
                    ((url.Min(p => p.Y) + row) * width + url.Min(p => p.X)) * 4,
                    glyph, row * glyphWidth * 4, glyphWidth * 4);
            } else {
                Assert.NotNull(glyph);
                var found = false;
                for (var y = height / 2; y <= height - glyphHeight && !found; y++) {
                    for (var x = (width - glyphWidth) / 2 - 2; x <= (width - glyphWidth) / 2 + 2 && !found; x++) {
                        var match = true;
                        for (var row = 0; row < glyphHeight && match; row++) for (var at = 0; at < glyphWidth * 4; at++) {
                            if (pixels[((y + row) * width + x) * 4 + at] != glyph![row * glyphWidth * 4 + at]) { match = false; break; }
                        }
                        found = match;
                        if (match) urlTop = y;
                    }
                }
                Assert.True(found, $"The URL glyphs are missing from {template}/{position}.");
            }
            Assert.True(urlTop >= quietZoneBottom,
                $"The fallback URL overlaps the QR quiet zone in {template}/{position}: glyph top {urlTop}, QR bottom {quietZoneBottom}.");
            Assert.Equal(pixels, ImageReader.DecodeRgba32(QrPngRenderer.Render(qr.Modules, options), out _, out _));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SceneEdges_OutsideTheAccumulationSpan_MatchAnUnclippedReferenceCrop(bool mirror) {
        const int size = 64;
        var contour = new[] { -1.5, -.25, .5, .125, .875, .875 };
        if (mirror) for (var i = 0; i < contour.Length; i += 2) contour[i] = 1 - contour[i];
        var original = new QrSceneGeometry();
        original.Shapes.Add(new SceneShape(ScenePaint.Solid(Rgba32.Black), new[] { contour }));
        var embedded = contour.Select(v => (v + 8) / 16).ToArray();
        var reference = new QrSceneGeometry();
        reference.Shapes.Add(new SceneShape(ScenePaint.Solid(Rgba32.Black), new[] { embedded }));
        var options = new QrSceneOptions { Paper = Rgba32.White };
        var actual = QrSceneRasterizer.Paint(size, options, original, default);
        var large = QrSceneRasterizer.Paint(size * 16, options, reference, default);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var at = (y * size + x) * 4;
            var expected = ((y + size * 8) * size * 16 + x + size * 8) * 4;
            for (var channel = 0; channel < 4; channel++)
                Assert.InRange(Math.Abs(actual[at + channel] - large[expected + channel]), 0, 1);
        }
    }

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

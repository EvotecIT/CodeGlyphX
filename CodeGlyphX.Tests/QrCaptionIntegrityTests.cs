using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class QrCaptionIntegrityTests {
    public static IEnumerable<object[]> Frames() => QrCanvasCaptionTests.Templates().Where(c => (QrCanvasTemplate)c[0] != QrCanvasTemplate.None);

    [Theory, MemberData(nameof(Frames))]
    public void LongCaptionsAndBands_StayWithinVisibleCodeWidth_WithCanvasClearance(QrCanvasTemplate template, QrCanvasCaptionPosition position) {
        var qr = QR.Encode("https://evotec.xyz");
        const int m = 30;
        var canvas = new QrCanvasOptions { Template = template, StrokeColor = new Rgba32(0, 0, 255), BandColor = new Rgba32(0, 0, 255),
            Caption = new QrCanvasCaptionOptions { Text = "Scan to explore the collection", Position = position, Color = new Rgba32(255, 0, 255) } };
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = m, Canvas = canvas }, out var w, out var h, out _);
        var text = Points(pixels, w, h, (r, g, b) => r > g && b > g);
        Assert.NotEmpty(text);
        var codeLeft = (w - (qr.Size + 8) * m) / 2 + 4 * m;
        var cap = (int)Math.Floor(qr.Size * m * .055 * .75);
        Assert.True(text.Min(p => p.X) >= codeLeft + cap);
        Assert.True(text.Max(p => p.X) < codeLeft + qr.Size * m - cap);
        Assert.True(text.Min(p => p.Y) >= cap && text.Max(p => p.Y) < h - cap);
        if (template is QrCanvasTemplate.ScanMePill or QrCanvasTemplate.SpeechBubble) {
            var band = Points(pixels, w, h, (r, g, b) => b > r && b > g);
            Assert.True(band.Min(p => p.X) >= codeLeft);
            Assert.True(band.Max(p => p.X) < codeLeft + qr.Size * m);
        }
    }

    [Theory, MemberData(nameof(Frames))]
    public void FrameStrokes_HaveCompleteCoverageInsideCanvasEdges(QrCanvasTemplate template, QrCanvasCaptionPosition position) {
        var qr = QR.Encode("https://evotec.xyz");
        foreach (var m in new[] { 10, 30 }) {
            var canvas = new QrCanvasOptions { Template = template, StrokeColor = new Rgba32(0, 0, 255), BandColor = new Rgba32(0, 0, 255),
                Caption = new QrCanvasCaptionOptions { Text = "SCAN ME", Position = position } };
            var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = m, Canvas = canvas }, out var w, out var h, out _);
            var decoration = Points(pixels, w, h, (r, g, b) => b > r && b > g);
            Assert.NotEmpty(decoration);
            var cap = (int)Math.Floor(qr.Size * m * .055);
            Assert.All(decoration, p => Assert.True(p.X >= cap && p.Y >= cap && p.X < w - cap && p.Y < h - cap,
                $"{template}/{position}: stroke reaches the canvas edge at {p.X},{p.Y}."));
            if (template == QrCanvasTemplate.PhoneOutline) {
                var bodyWidth = decoration.Max(p => p.X) - decoration.Min(p => p.X) + 1;
                var bodyHeight = decoration.Max(p => p.Y) - decoration.Min(p => p.Y) + 1;
                Assert.InRange(bodyHeight / (double)bodyWidth, 2.14, 2.19);
                var t = Math.Max(2, (int)Math.Round(m * .5));
                var edge = decoration.Where(p => p.Y == h / 2).Min(p => p.X);
                foreach (var x in new[] { edge + t, w - edge - t - 1 }) {
                    var at = ((h / 2) * w + x) * 4;
                    Assert.True(pixels[at + 2] > pixels[at], "The complete bezel must clear the square code panel.");
                }
            }
        }
    }

    [Theory, MemberData(nameof(Frames))]
    public void PhoneIcon_IsAHandsetOutline_WithoutDependingOnFontSymbolGlyphs(QrCanvasTemplate template, QrCanvasCaptionPosition position) {
        var qr = QR.Encode("https://evotec.xyz");
        var caption = new QrCanvasCaptionOptions { Text = "H", Font = BarcodeFont.OcrB, FontSizePx = 80,
            Position = position, Icon = QrCanvasCaptionIcon.Phone, Color = new Rgba32(255, 0, 255) };
        var canvas = new QrCanvasOptions { Template = template, Caption = caption };
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = 30, Canvas = canvas }, out var w, out var h, out _);
        var points = Points(pixels, w, h, (r, g, b) => r > g && b > g);
        var columns = points.Select(p => p.X).Distinct().OrderBy(x => x).ToArray();
        var gap = Enumerable.Range(1, columns.Length - 1).First(i => columns[i] > columns[i - 1] + 1);
        var icon = points.Where(p => p.X <= columns[gap - 1]).ToArray();
        var iw = icon.Max(p => p.X) - icon.Min(p => p.X) + 1;
        var ih = icon.Max(p => p.Y) - icon.Min(p => p.Y) + 1;
        // A handset spans a square and has an open upper-right quadrant. A missing-glyph
        // box or the former narrow rectangle fails these independent silhouette checks.
        Assert.InRange(iw / (double)ih, .85, 1.15);
        Assert.InRange(icon.Length / (double)(iw * ih), .2, .7);
        Assert.DoesNotContain(icon, p => p.X > icon.Min(v => v.X) + iw * .6 && p.Y < icon.Min(v => v.Y) + ih * .4);
    }

    private static List<(int X, int Y)> Points(byte[] pixels, int w, int h, Func<byte, byte, byte, bool> matches) {
        var points = new List<(int, int)>();
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) {
            var p = (y * w + x) * 4;
            if (pixels[p + 3] > 0 && matches(pixels[p], pixels[p + 1], pixels[p + 2])) points.Add((x, y));
        }
        return points;
    }

    [Fact]
    public void Subline_IsCentredBelowTheWholeHeadlineIncludingItsIcon() {
        var caption = new QrCanvasCaptionOptions { Text = "SCAN ME", Subline = "Collection", FontSizePx = 80,
            Icon = QrCanvasCaptionIcon.Phone, Color = new Rgba32(255, 0, 255) };
        var pixels = QrPngRenderer.RenderPixels(QR.Encode("https://evotec.xyz").Modules,
            new QrPngRenderOptions { ModuleSize = 30, Canvas = new QrCanvasOptions { Caption = caption } }, out var w, out var h, out _);
        var points = Points(pixels, w, h, (r, g, b) => r > g && b > g);
        var rows = points.Select(p => p.Y).Distinct().OrderBy(y => y).ToArray();
        var gap = Enumerable.Range(1, rows.Length - 1).First(i => rows[i] > rows[i - 1] + 1);
        var headline = points.Where(p => p.Y < rows[gap]).ToArray();
        var subline = points.Where(p => p.Y >= rows[gap]).ToArray();
        var headCentre = (headline.Min(p => p.X) + headline.Max(p => p.X)) / 2D;
        var subCentre = (subline.Min(p => p.X) + subline.Max(p => p.X)) / 2D;
        Assert.InRange(Math.Abs(headCentre - subCentre), 0, 1.5);
    }

    [Theory]
    [InlineData(QrCanvasCaptionIcon.Phone)]
    [InlineData(QrCanvasCaptionIcon.Camera)]
    public void CaptionIcons_LeavePointThreeFiveEmBeforeTheHeadline(QrCanvasCaptionIcon iconKind) {
        var caption = new QrCanvasCaptionOptions { Text = "H", FontSizePx = 80, Icon = iconKind, Color = new Rgba32(255, 0, 255) };
        var pixels = QrPngRenderer.RenderPixels(QR.Encode("https://evotec.xyz").Modules,
            new QrPngRenderOptions { ModuleSize = 30, Canvas = new QrCanvasOptions { Caption = caption } }, out var w, out var h, out _);
        var ink = Points(pixels, w, h, (r, g, b) => r > g && b > g);
        var columns = ink.Select(p => p.X).Distinct().OrderBy(x => x).ToArray();
        var split = Enumerable.Range(1, columns.Length - 1).First(i => columns[i] > columns[i - 1] + 1);
        Assert.InRange(columns[split] - columns[split - 1] - 1, 27, 30);
        if (iconKind == QrCanvasCaptionIcon.Camera) {
            var cameraTop = ink.Where(p => p.X < columns[split]).Min(p => p.Y);
            var textTop = ink.Where(p => p.X >= columns[split]).Min(p => p.Y);
            var x = caption.Font.Shape("x", 80); var headline = caption.Font.Shape("H", 80);
            Assert.InRange(cameraTop - textTop - (x.InkBounds.Y - headline.InkBounds.Y), -5.5, -2.5);
        }
    }

    [Fact]
    public void TicketNotches_KeepTheCompleteSemicircleOutsideTheCodePanel() {
        var qr = QR.Encode("https://evotec.xyz");
        foreach (var m in new[] { 10, 30 }) foreach (QrCanvasCaptionPosition position in Enum.GetValues(typeof(QrCanvasCaptionPosition))) {
            var canvas = new QrCanvasOptions { Template = QrCanvasTemplate.TicketStub, StrokeColor = new Rgba32(0, 0, 255),
                Caption = new QrCanvasCaptionOptions { Text = "SCAN ME", Position = position, Color = Rgba32.Transparent } };
            var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = m, Canvas = canvas }, out var w, out var h, out _);
            var pad = (w - (qr.Size + 8) * m) / 2;
            var data = Points(pixels, w, h, (r, g, b) => r == 0 && g == 0 && b == 0);
            var qrTop = data.Min(p => p.Y) - 4 * m;
            var qrBottom = qrTop + (qr.Size + 8) * m;
            var blue = Points(pixels, w, h, (r, g, b) => b > r + 100);
            var rows = blue.Where(p => p.X > w * .3 && p.X < w * .7 && (position == QrCanvasCaptionPosition.Top ? p.Y < qrTop : p.Y >= qrBottom))
                .Select(p => p.Y).Distinct().OrderBy(y => y).ToArray();
            var nearest = position == QrCanvasCaptionPosition.Top ? rows.Last() : rows.First();
            var t = Math.Max(2, (int)Math.Round(m * .5));
            var divider = nearest + (position == QrCanvasCaptionPosition.Top ? -(t - 1) / 2D : (t - 1) / 2D);
            foreach (var right in new[] { false, true }) for (var angle = 0; angle <= 180; angle += 5) {
                var theta = angle * Math.PI / 180;
                var edge = blue.Where(p => p.Y == (qrTop + qrBottom) / 2).Min(p => p.X);
                var axis = right ? w - edge - t / 2D : edge + t / 2D;
                var x = axis + (right ? -1 : 1) * w * .045 * Math.Sin(theta);
                var y = divider - w * .045 * Math.Cos(theta);
                var at = ((int)y * w + (int)x) * 4;
                Assert.True(pixels[at + 2] > pixels[at] + 100, $"{position}/{m}: clipped notch at {x:F1},{y:F1}.");
            }
            var centreRow = blue.Where(p => Math.Abs(p.Y - divider) <= .6).Select(p => p.X).Distinct().OrderBy(v => v).ToArray();
            var gaps = Enumerable.Range(1, centreRow.Length - 1).Where(i => centreRow[i] - centreRow[i - 1] > 1).ToArray();
            Assert.True(gaps.Length >= 2);
            Assert.True(centreRow[gaps.First()] - centreRow[gaps.First() - 1] - 1 >= 2 * t,
                "The first perforation must leave a complete dash-gap after the notch.");
            Assert.True(centreRow[gaps.Last()] - centreRow[gaps.Last() - 1] - 1 >= 2 * t,
                "The last perforation must leave a complete dash-gap before the notch.");
        }
    }

    [Theory]
    [InlineData(QrModuleShape.Dot, QrLogoPlateShape.Circle)]
    [InlineData(QrModuleShape.ConnectedRounded, QrLogoPlateShape.Classy)]
    [InlineData(QrModuleShape.Dot, QrLogoPlateShape.RoundedSquare)]
    [InlineData(QrModuleShape.ConnectedRounded, QrLogoPlateShape.RoundedSquare)]
    [InlineData(QrModuleShape.Liquid, QrLogoPlateShape.RoundedSquare)]
    public void LogoPlates_RemoveEveryOverlappedModuleWithoutSlicingItsInk(QrModuleShape shape, QrLogoPlateShape plate) {
        var modules = new BitMatrix(9, 9);
        for (var y = 0; y < 9; y++) for (var x = 0; x < 9; x++) modules[x, y] = true;
        var original = modules.Clone();
        var logo = QrRasterLogoOptions.FromPixels(new byte[4], 1, 1, 4, PixelFormat.Rgba32);
        logo.Scale = .23; logo.PaddingPx = 3; logo.OffsetXPx = 65; logo.OffsetYPx = 67;
        logo.PlateShape = plate;
        var options = new QrPngRenderOptions { ModuleSize = 20, QuietZone = 0, ModuleShape = shape, Logo = logo };
        var pixels = QrPngRenderer.RenderPixels(modules, options, out var w, out _, out _);
        // The 41px plate plus padding touches cells 3..5 on both axes. Every
        // one must be completely blank, including the parts outside the plate.
        for (var y = 60; y < 120; y++) for (var x = 60; x < 120; x++) {
            var p = (y * w + x) * 4;
            Assert.True(pixels[p] == 255 && pixels[p + 1] == 255 && pixels[p + 2] == 255,
                $"{shape}: sliced module at {x},{y}.");
        }
        Assert.Contains(Enumerable.Range(0, 60), x => pixels[(30 * w + x) * 4] == 0);
        for (var y = 0; y < 9; y++) for (var x = 0; x < 9; x++) Assert.Equal(original[x, y], modules[x, y]);
    }
}

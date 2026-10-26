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
            Assert.All(decoration, p => Assert.True(p.X > 0 && p.Y > 0 && p.X < w - 1 && p.Y < h - 1,
                $"{template}/{position}: stroke reaches the canvas edge at {p.X},{p.Y}."));
            if (template == QrCanvasTemplate.PhoneOutline) {
                var t = Math.Max(2, (int)Math.Round(m * .5));
                foreach (var x in new[] { t + t * 3 / 2, w - t - t * 3 / 2 }) {
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
                var axis = right ? w - pad / 2D - t / 2D : pad / 2D + t / 2D;
                var x = axis + (right ? -1 : 1) * w * .045 * Math.Sin(theta);
                var y = divider - w * .045 * Math.Cos(theta);
                var at = ((int)y * w + (int)x) * 4;
                Assert.True(pixels[at + 2] > pixels[at] + 100, $"{position}/{m}: clipped notch at {x:F1},{y:F1}.");
            }
        }
    }
}

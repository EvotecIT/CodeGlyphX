using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class QrCanvasCaptionTests {
    private const string Payload = "https://evotec.xyz";
    public static IEnumerable<object[]> Templates() {
        foreach (QrCanvasTemplate template in Enum.GetValues(typeof(QrCanvasTemplate)))
            foreach (QrCanvasCaptionPosition position in Enum.GetValues(typeof(QrCanvasCaptionPosition)))
                yield return new object[] { template, position };
    }
    public static IEnumerable<object[]> CaptionGeometryCases() {
        foreach (var frame in Templates())
            foreach (var padding in new[] { 4, 16 })
                foreach (var supersample in new[] { 1, 3 })
                    yield return new[] { frame[0], frame[1], padding, supersample };
    }

    [Theory]
    [MemberData(nameof(CaptionGeometryCases))]
    public void Captions_ClearFrameStrokes_AndStayInsideFilledBands(QrCanvasTemplate template,
        QrCanvasCaptionPosition position, int captionPadding, int supersample) {
        var qr = QR.Encode(Payload);
        var options = Options(template, position);
        options.BackgroundSupersample = supersample;
        options.Canvas!.Frame = new QrCanvasFrameOptions { Color = new Rgba32(0, 0, 255) };
        options.Canvas.Caption!.Text = "Zażółć gęślą jaźń";
        options.Canvas.Caption.FontSizePx = 28;
        options.Canvas.Caption.PaddingPx = captionPadding;
        options.Canvas.Caption.Color = new Rgba32(255, 0, 255);
        var actual = QrPngRenderer.RenderPixels(qr.Modules, options, out var width, out var height, out _);
        options.Canvas.Caption.Color = Rgba32.Transparent;
        var frame = QrPngRenderer.RenderPixels(qr.Modules, options, out var frameWidth, out var frameHeight, out _);
        Assert.Equal(width, frameWidth);
        Assert.Equal(height, frameHeight);
        var textPixels = new List<(int X, int Y)>();
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var p = (y * width + x) * 4;
            if (actual[p] > actual[p + 1]) textPixels.Add((x, y));
        }
        Assert.NotEmpty(textPixels);
        bool IsFrame(int x, int y) {
            var p = (y * width + x) * 4;
            return frame[p + 2] > frame[p] && frame[p + 2] > frame[p + 1];
        }
        if (template is QrCanvasTemplate.ScanMePill or QrCanvasTemplate.SpeechBubble or QrCanvasTemplate.RoundedCardHeader) {
            Assert.All(textPixels, point => Assert.True(IsFrame(point.X, point.Y), "Caption ink must stay inside its filled band."));
        } else {
            var clearance = template == QrCanvasTemplate.PhoneOutline ? 4 : 0;
            var left = Math.Max(0, textPixels.Min(point => point.X) - clearance);
            var right = Math.Min(width - 1, textPixels.Max(point => point.X) + clearance);
            var top = Math.Max(0, textPixels.Min(point => point.Y) - clearance);
            var bottom = Math.Min(height - 1, textPixels.Max(point => point.Y) + clearance);
            for (var y = top; y <= bottom; y++) for (var x = left; x <= right; x++)
                Assert.False(IsFrame(x, y), $"{template}/{position}: frame stroke at {x},{y} enters the caption's protected area (gap={clearance}).");
        }
    }
    private static QrPngRenderOptions Options(QrCanvasTemplate template, QrCanvasCaptionPosition position) => new() {
        ModuleSize = 10, QuietZone = 4,
        Canvas = new QrCanvasOptions { PaddingPx = template == QrCanvasTemplate.PhoneOutline ? 64 : 32, Template = template,
            Caption = new QrCanvasCaptionOptions { Text = "Scan to explore", Position = position } }
    };

    [Fact]
    public void DefaultCaptionFont_PreservesPolishAndRejectsUnavailableGlyphs() {
        var caption = new QrCanvasCaptionOptions();
        Assert.Same(BarcodeFont.ManropeSemiBold, caption.Font);
        Assert.Equal(QrCanvasCaptionWeight.Regular, caption.Weight);
        Assert.Contains("Manrope", caption.Font.Name);
        Assert.Contains("SemiBold", caption.Font.Name);
        const string polish = "Zeskanuj kod — Łódź. Zażółć gęślą jaźń ĄĆĘŁŃÓŚŹŻ";
        var shaped = caption.Font.Shape(polish, 32);
        Assert.Equal(polish, shaped.Text);
        Assert.Empty(shaped.MissingGlyphs);
        Assert.False(shaped.HasIncompleteShaping);
        Assert.NotEmpty(shaped.Contours);
        Assert.Throws<ArgumentException>(() => caption.Font.Shape("\u4e16", 32));
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public void Templates_PreserveCompleteQrPixels_AndExportExactVectorCoverage(QrCanvasTemplate template, QrCanvasCaptionPosition position) {
        var qr = QR.Encode(Payload);
        var options = Options(template, position);
        var plain = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = 10 }, out var side, out _, out _);
        var actual = QrPngRenderer.RenderPixels(qr.Modules, options, out var w, out var h, out var stride);
        Assert.True(h > w);
        var pad = options.Canvas!.PaddingPx;
        var qy = position == QrCanvasCaptionPosition.Top ? h - pad - side : pad;
        if (template == QrCanvasTemplate.PhoneOutline) {
            var ordinary = options.Canvas!;
            ordinary.Template = QrCanvasTemplate.None;
            QrPngRenderer.RenderPixels(qr.Modules, options, out _, out var ordinaryHeight, out _);
            ordinary.Template = QrCanvasTemplate.PhoneOutline;
            qy = (position == QrCanvasCaptionPosition.Top ? ordinaryHeight - pad - side : pad) + (h - ordinaryHeight) / 2;
            Assert.InRange(h / (double)w, 2.1, 2.2);
        }
        for (var y = 0; y < side; y++) for (var x = 0; x < side * 4; x++)
            Assert.Equal(plain[y * side * 4 + x], actual[(y + qy) * stride + pad * 4 + x]);
        var root = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { Appearance = options }));
        Assert.Equal(w, (int)root.Attribute("width")!);
        Assert.Equal(h, (int)root.Attribute("height")!);
        Assert.Equal(actual, ReadSvg(root, w, h));
        var png = QrPngRenderer.Render(qr.Modules, options);
        Assert.True(QrImageDecoder.TryDecodeImage(png, out var decoded));
        Assert.Equal(Payload, decoded.Text);
        var facade = new QrRenderOptions { ModuleSize = 10, QuietZone = 4, Canvas = options.Canvas };

        Assert.Equal(png, qr.Render(OutputFormat.Png, facade).Data.ToArray());
        var reader = new ZXing.BarcodeReaderGeneric { AutoRotate = false, Options = new ZXing.Common.DecodingOptions {
            TryHarder = false, TryInverted = false, PureBarcode = false, PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE } } };
        Assert.Equal(Payload, reader.Decode(new ZXing.RGBLuminanceSource(actual, w, h, ZXing.RGBLuminanceSource.BitmapFormat.RGBA32))?.Text);
    }

    [Fact]
    public void Caption_UsesCallerFontForUnicode_AndClonesWithoutMutatingCallerOptions() {
        using var stream = typeof(QrCanvasCaptionTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Fonts.OfficeIMOBaselineSans-Regular.ttf")!;
        using var memory = new System.IO.MemoryStream(); stream.CopyTo(memory);
        var font = BarcodeFont.FromBytes(memory.ToArray());
        var input = new QrRenderOptions { ArtGuardrailsEnabled = false, Canvas = Options(QrCanvasTemplate.CornerViewfinder, QrCanvasCaptionPosition.Top).Canvas };
        input.Canvas!.Caption!.Font = font; input.Canvas.Caption.Text = "Zażółć gęślą jaźń";
        input.Canvas.Caption.Weight = QrCanvasCaptionWeight.Bold;
        var copy = QrRenderer.CloneOptions(input);
        Assert.NotSame(input.Canvas, copy.Canvas); Assert.NotSame(input.Canvas.Caption, copy.Canvas!.Caption);
        Assert.Same(font, copy.Canvas.Caption!.Font);
        copy.Canvas.Caption.Weight = QrCanvasCaptionWeight.Regular;
        var bold = QR.Encode(Payload).Render(OutputFormat.Png, input).Data.ToArray();
        var regular = QR.Encode(Payload).Render(OutputFormat.Png, copy).Data.ToArray();
        Assert.False(bold.SequenceEqual(regular));
        Assert.Equal(QrCanvasCaptionWeight.Bold, input.Canvas.Caption.Weight);
        copy.Canvas.Caption.Font = BarcodeFont.OcrB;
        Assert.False(regular.SequenceEqual(QR.Encode(Payload).Render(OutputFormat.Png, copy).Data.ToArray()));
    }

    [Fact]
    public void LargeRoundedFrameCorners_KeepTheirCompleteStrokeOutsideTheCodePanel() {
        var qr = QR.Encode(Payload);
        foreach (var template in new[] { QrCanvasTemplate.TicketStub, QrCanvasTemplate.RoundedCardHeader })
            foreach (var position in new[] { QrCanvasCaptionPosition.Top, QrCanvasCaptionPosition.Bottom }) {
                const int m = 30;
                var canvas = new QrCanvasOptions { Template = template, StrokeColor = new Rgba32(0, 0, 255),
                    BandColor = Rgba32.Transparent, Caption = new QrCanvasCaptionOptions {
                        Text = "SCAN ME", Position = position, Color = Rgba32.Transparent } };
                var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = m, Canvas = canvas }, out var w, out var h, out _);
                var pad = (w - (qr.Size + 8) * m) / 2;
                var radius = template == QrCanvasTemplate.TicketStub ? w * .045 * 1.1 : Math.Round(w * .06);
                var middle = radius - m * .25;
                foreach (var right in new[] { false, true }) foreach (var bottom in new[] { false, true })
                    for (var angle = 0; angle <= 90; angle += 5) {
                        var theta = angle * Math.PI / 180;
                        var x = (right ? w - pad / 2.0 - radius : pad / 2.0 + radius) + (right ? 1 : -1) * middle * Math.Cos(theta);
                        var y = (bottom ? h - pad / 2.0 - radius : pad / 2.0 + radius) + (bottom ? 1 : -1) * middle * Math.Sin(theta);
                        var at = ((int)y * w + (int)x) * 4;
                        Assert.True(pixels[at + 2] > pixels[at] + 100, $"{template}/{position}: clipped corner stroke at {x:F1},{y:F1}.");
                    }
            }
    }

    [Fact]
    public void ThinRoundedFrame_RemainsOutsideSquarePanel_EvenAtCorners() {
        var qr = QR.Encode(Payload);
        var options = new QrPngRenderOptions { ModuleSize = 8, Canvas = new QrCanvasOptions {
            PaddingPx = 16, Frame = new QrCanvasFrameOptions { ThicknessPx = 1, GapPx = 0, RadiusPx = 24, Color = new Rgba32(255, 0, 0) } } };
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, options, out var w, out _, out _);
        var at = (15 * w + 20) * 4;
        Assert.Equal(255, pixels[at]); Assert.Equal(0, pixels[at + 1]);
    }

    [Fact]
    public void Caption_RejectsUnboundedOrUnreadableText_AndSupersamplingKeepsDimensionsAndOptions() {
        var qr = QR.Encode(Payload); var options = Options(QrCanvasTemplate.PhoneOutline, QrCanvasCaptionPosition.Top);
        var baseline = QrPngRenderer.RenderPixels(qr.Modules, options, out var w, out var h, out _);
        options.BackgroundSupersample = 2;
        QrPngRenderer.RenderPixels(qr.Modules, options, out var sw, out var sh, out _);
        Assert.Equal(w, sw); Assert.Equal(h, sh); Assert.Equal(0, options.Canvas!.Caption!.FontSizePx);
        options.Canvas.Caption.Text = new string('W', 256);
        Assert.Throws<ArgumentException>(() => QrPngRenderer.Render(qr.Modules, options));
        options.Canvas.Caption.Text += "W";
        Assert.Throws<ArgumentException>(() => QrPngRenderer.Render(qr.Modules, options));
        options.Canvas.Caption.Text = "Line\nbreak";
        Assert.Throws<ArgumentException>(() => QrPngRenderer.Render(qr.Modules, options));
    }

    [Fact]
    public void CaptionColor_IsPaintedOutsideTheCode_AndUnsupportedGlyphsFailClearly() {
        var qr = QR.Encode(Payload);
        var options = Options(QrCanvasTemplate.None, QrCanvasCaptionPosition.Bottom);
        options.Canvas!.Caption!.Color = new Rgba32(190, 0, 0);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, options, out var w, out var h, out _);
        var red = 0;
        for (var y = h - 70; y < h; y++) for (var x = 0; x < w; x++) {
            var at = (y * w + x) * 4;
            if (pixels[at] == 190 && pixels[at + 1] == 0 && pixels[at + 2] == 0) red++;
        }
        Assert.True(red > 30);
        options.Canvas.Caption.Text = "\u4e16";
        Assert.Throws<ArgumentException>(() => QrPngRenderer.Render(qr.Modules, options));
        options.Canvas.Caption.Text = "SCAN ME";
        options.Canvas.Caption.FontSizePx = 257;
        Assert.Throws<ArgumentOutOfRangeException>(() => QrPngRenderer.Render(qr.Modules, options));
    }

    [Fact]
    public void Template_ReplacesIndependentFramesAndBorders() {
        var qr = QR.Encode(Payload);
        var options = Options(QrCanvasTemplate.RoundedCardHeader, QrCanvasCaptionPosition.Top);
        var expected = QrPngRenderer.RenderPixels(qr.Modules, options, out _, out _, out _);
        options.Canvas!.Frame = new QrCanvasFrameOptions { ThicknessPx = 12 };
        options.Canvas.BorderPx = 6;
        options.Canvas.Band = new QrCanvasBandOptions();
        options.Canvas.Badge = new QrCanvasBadgeOptions();
        // Frame.Color is the template's accent; keep that colour equal while changing decorations.
        options.Canvas.Frame.Color = new Rgba32(20, 24, 40);
        Assert.Equal(expected, QrPngRenderer.RenderPixels(qr.Modules, options, out _, out _, out _));
    }

    [Theory]
    [InlineData(QrCanvasCaptionPosition.Top)]
    [InlineData(QrCanvasCaptionPosition.Bottom)]
    public void SmallPhoneCaption_DoesNotCrossSpeakerOrHomeIndicator(QrCanvasCaptionPosition position) {
        var options = Options(QrCanvasTemplate.PhoneOutline, position);
        var caption = options.Canvas!.Caption!;
        caption.FontSizePx = 8; caption.PaddingPx = 4; caption.Text = "SCAN"; caption.Color = new Rgba32(255, 0, 0);
        var pixels = QrPngRenderer.RenderPixels(QR.Encode(Payload).Modules, options, out var w, out var h, out _);
        var found = false;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) {
            var at = (y * w + x) * 4;
            if (pixels[at] <= pixels[at + 1] || pixels[at + 1] != pixels[at + 2]) continue;
            found = true;
            Assert.True(position == QrCanvasCaptionPosition.Top ? y > 25 : y < h - 25);
        }
        Assert.True(found);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void Caption_MinimumSizeUsesFinalPixels_WhenBackgroundSupersampled(int scale) {
        var qr = QR.Encode("A");
        Assert.Equal(21, qr.Size);
        var caption = new QrCanvasCaptionOptions {
            Text = new string('B', 60), Font = BarcodeFont.OcrB, FontSizePx = 24,
            Weight = QrCanvasCaptionWeight.Regular, PaddingPx = 16
        };
        var options = new QrPngRenderOptions {
            ModuleSize = 10, QuietZone = 4, BackgroundSupersample = scale,
            Canvas = new QrCanvasOptions { PaddingPx = 32, Caption = caption }
        };
        var error = Assert.Throws<ArgumentException>(() => QrPngRenderer.Render(qr.Modules, options));
        Assert.Contains("fit readably", error.Message);
        Assert.Throws<ArgumentException>(() => SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { Appearance = options }));
        // This run also shrinks to fit, but remains above eight final pixels.
        caption.Text = new string('B', 12);
        Assert.NotEmpty(QrPngRenderer.Render(qr.Modules, options));
        Assert.Contains("<svg", SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { Appearance = options }));
        Assert.Equal(24, caption.FontSizePx);
        Assert.Equal(16, caption.PaddingPx);
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public void LongCaptionBounds_KeepOneCapHeightClearOfFrameDecorations(QrCanvasTemplate template, QrCanvasCaptionPosition position) {
        var qr = QR.Encode(Payload);
        var options = Options(template, position);
        options.Canvas!.Caption!.Text = "Scan to explore the collection";
        options.Canvas.Caption.Color = new Rgba32(255, 0, 255);
        options.Canvas.StrokeColor = options.Canvas.BandColor = new Rgba32(0, 0, 255);
        var actual = QrPngRenderer.RenderPixels(qr.Modules, options, out var w, out var h, out _);
        options.Canvas.Caption.Color = Rgba32.Transparent;
        var frame = QrPngRenderer.RenderPixels(qr.Modules, options, out _, out _, out _);
        var points = new List<(int X, int Y)>();
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) {
            var at = (y * w + x) * 4;
            if (actual[at] > actual[at + 1]) points.Add((x, y));
        }
        Assert.NotEmpty(points);
        // Automatic text may shrink to 75% before wrapping. This independently checks its
        // protected rectangle against actual decoration pixels, including bracket arms.
        var cap = (int)Math.Floor((qr.Size + 8) * 10 * .045 * .75);
        var left = points.Min(p => p.X); var right = points.Max(p => p.X);
        var top = points.Min(p => p.Y); var bottom = points.Max(p => p.Y);
        var filled = template is QrCanvasTemplate.ScanMePill or QrCanvasTemplate.SpeechBubble or QrCanvasTemplate.RoundedCardHeader;
        bool IsBlue(int x, int y) { var at = (y * w + x) * 4; return frame[at + 2] > frame[at] && frame[at + 2] > frame[at + 1]; }
        if (filled) {
            foreach (var point in points) Assert.True(IsBlue(point.X, point.Y));
            for (var d = 1; d <= cap; d++) {
                Assert.True(IsBlue((left + right) / 2, top - d));
                Assert.True(IsBlue((left + right) / 2, bottom + d));
            }
        } else {
            for (var y = Math.Max(0, top - cap); y <= Math.Min(h - 1, bottom + cap); y++)
                for (var x = Math.Max(0, left - cap); x <= Math.Min(w - 1, right + cap); x++)
                    Assert.False(IsBlue(x, y), $"{template}/{position}: decoration enters the cap-height clearance at {x},{y}.");
        }
    }

    [Theory]
    [InlineData("OfficeIMOBaselineSans-Regular.ttf", "Za\u017c\u00f3\u0142\u0107 g\u0119\u015bl\u0105 ja\u017a\u0144")]
    [InlineData("TestOutlines.otf", "AB")]
    public void CallerTtfAndOtfFonts_RenderThroughTheCaptionFactory(string fixture, string text) {
        using var stream = typeof(QrCanvasCaptionTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Fonts." + fixture)!;
        using var memory = new System.IO.MemoryStream(); stream.CopyTo(memory);
        var caption = QrCanvasCaptionOptions.FromFontBytes(memory.ToArray());
        caption.Text = text;
        var options = Options(QrCanvasTemplate.ScanMePill, QrCanvasCaptionPosition.Bottom);
        options.Canvas!.Caption = caption;
        var pixels = QrPngRenderer.RenderPixels(QR.Encode(Payload).Modules, options, out var width, out var height, out _);
        Assert.True(height > width);
        Assert.Contains(pixels, c => c > 0 && c < 255);
        var svg = SvgQrRenderer.Render(QR.Encode(Payload).Modules, new QrSvgRenderOptions { Appearance = options });
        Assert.Equal(pixels, ReadSvg(XElement.Parse(svg), width, height));
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public void CaptionDetails_RenderIconSublineFallbackUrlAndBandLogo_WithoutChangingQrPixels(QrCanvasTemplate template, QrCanvasCaptionPosition position) {
        var options = Options(template, position);
        options.Canvas!.Caption!.Text = "SCAN ME";
        options.Canvas.Caption.Subline = "Collection";
        options.Canvas.Caption.Url = "evotec.xyz";
        options.Canvas.Caption.Icon = QrCanvasCaptionIcon.Camera;
        options.Canvas.BandColor = new Rgba32(20, 50, 100);
        options.Canvas.StrokeColor = new Rgba32(20, 50, 100);
        options.Canvas.StrokeWidthPx = 4;
        var qr = QR.Encode(Payload);
        var first = QrPngRenderer.RenderPixels(qr.Modules, options, out var w, out var h, out _);
        options.Canvas.BandLogoPng = PngImageEncoder.EncodeRgba32(new byte[] { 180, 40, 60, 255 }, 1, 1);
        var second = QrPngRenderer.RenderPixels(qr.Modules, options, out var sw, out var sh, out _);
        Assert.Equal(w, sw); Assert.Equal(h, sh);
        Assert.False(first.SequenceEqual(second));
        Assert.True(QrImageDecoder.TryDecode(second, w, h, w * 4, PixelFormat.Rgba32, out var read));
        Assert.Equal(Payload, read.Text);
        var cloned = QrRenderer.CloneCanvas(options.Canvas)!;
        Assert.NotSame(options.Canvas.BandLogoPng, cloned.BandLogoPng);
        Assert.Equal(options.Canvas.Caption.Subline, cloned.Caption!.Subline);
        Assert.Equal(options.Canvas.Caption.Url, cloned.Caption.Url);
        Assert.Equal(options.Canvas.Caption.Icon, cloned.Caption.Icon);
    }

    private static byte[] ReadSvg(XElement root, int width, int height) {
        var pixels = new byte[width * height * 4];
        foreach (var path in root.Elements()) {
            var rgb = uint.Parse(((string)path.Attribute("fill")!).Substring(1), NumberStyles.HexNumber);
            var opacity = (string?)path.Attribute("fill-opacity");
            var a = opacity is null ? (byte)255 : (byte)Math.Round(double.Parse(opacity, CultureInfo.InvariantCulture) * 255);
            foreach (Match run in Regex.Matches((string)path.Attribute("d")!, @"M(\d+) (\d+)h(\d+)v(\d+)h-\d+z")) {
                int N(int i) => int.Parse(run.Groups[i].Value, CultureInfo.InvariantCulture);
                for (var y = N(2); y < N(2) + N(4); y++) for (var x = N(1); x < N(1) + N(3); x++) {
                    var at = (y * width + x) * 4;
                    pixels[at] = (byte)(rgb >> 16); pixels[at + 1] = (byte)(rgb >> 8); pixels[at + 2] = (byte)rgb; pixels[at + 3] = a;
                }
            }
        }
        return pixels;
    }
}

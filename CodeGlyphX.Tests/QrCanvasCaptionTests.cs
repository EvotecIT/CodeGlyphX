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

public sealed class QrCanvasCaptionTests {
    private readonly Xunit.Abstractions.ITestOutputHelper _output;
    public QrCanvasCaptionTests(Xunit.Abstractions.ITestOutputHelper output) => _output = output;
    private const string Payload = "https://evotec.xyz";
    public static IEnumerable<object[]> Templates() {
        foreach (QrCanvasTemplate template in Enum.GetValues(typeof(QrCanvasTemplate)))
            foreach (QrCanvasCaptionPosition position in Enum.GetValues(typeof(QrCanvasCaptionPosition)))
                yield return new object[] { template, position };
    }
    private static QrPngRenderOptions Options(QrCanvasTemplate template, QrCanvasCaptionPosition position) => new() {
        ModuleSize = 10, QuietZone = 4,
        Canvas = new QrCanvasOptions { PaddingPx = 32, Template = template,
            Caption = new QrCanvasCaptionOptions { Text = "Scan to explore", Position = position } }
    };

    [Theory]
    [MemberData(nameof(Templates))]
    public void Templates_PreserveCompleteQrPixels_AndExportExactVectorCoverage(QrCanvasTemplate template, QrCanvasCaptionPosition position) {
        var qr = QR.Encode(Payload);
        var options = Options(template, position);
        var plain = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = 10 }, out var side, out _, out _);
        var actual = QrPngRenderer.RenderPixels(qr.Modules, options, out var w, out var h, out var stride);
        Assert.True(h > w);
        var qy = position == QrCanvasCaptionPosition.Top ? h - 32 - side : 32;
        for (var y = 0; y < side; y++) for (var x = 0; x < side * 4; x++)
            Assert.Equal(plain[y * side * 4 + x], actual[(y + qy) * stride + 32 * 4 + x]);
        var root = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { Appearance = options }));
        Assert.Equal(w, (int)root.Attribute("width")!);
        Assert.Equal(h, (int)root.Attribute("height")!);
        Assert.Equal(actual, ReadSvg(root, w, h));
        var png = QrPngRenderer.Render(qr.Modules, options);
#if !NETFRAMEWORK
        // Legacy native decoding cannot locate a code inside a tall decorated canvas.
        Assert.True(QR.TryDecodePng(png, out var decoded));
        Assert.Equal(Payload, decoded.Text);
#endif
        var facade = new QrRenderOptions { ModuleSize = 10, QuietZone = 4, Canvas = options.Canvas };
#if NETFRAMEWORK
        if (template is QrCanvasTemplate.TicketStub or QrCanvasTemplate.PhoneOutline) {
            // Keep the real legacy guarded-rendering limitation visible and fail closed.
            var error = Assert.Throws<InvalidOperationException>(() => qr.Render(OutputFormat.Png, facade));
            Assert.Contains("native decode validation", error.Message);
            Assert.Throws<InvalidOperationException>(() => qr.Render(OutputFormat.Svg, facade));
            var report = QrArt.ValidateImage(png, Payload, 250);
            _output.WriteLine($"{template}/{position}: guarded PNG and SVG throw: {error.Message}; native exported PNG decode={QR.TryDecodePng(png, out _)}; image validation={report.AllPassed}; checks={string.Join(";", report.Checks.Select(c => c.Name + "=" + c.Passed))}");
        } else {
            Assert.Equal(png, qr.Render(OutputFormat.Png, facade).Data.ToArray());
            qr.Render(OutputFormat.Svg, facade);
            _output.WriteLine($"{template}/{position}: guarded PNG and SVG succeeded; native exported PNG decode={QR.TryDecodePng(png, out _)}");
        }
        facade.ArtGuardrailsEnabled = false;
#else
        Assert.Equal(png, qr.Render(OutputFormat.Png, facade).Data.ToArray());
#endif
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
        Assert.Equal(w, sw); Assert.Equal(h, sh); Assert.Equal(24, options.Canvas!.Caption!.FontSizePx);
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

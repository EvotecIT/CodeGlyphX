using CodeGlyphX.Rendering;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrSceneExportTests {
    private const string Payload = "https://example.com/scenes";
    [Theory]
    [InlineData(QrModuleShape.ConnectedRounded, 0)]
    [InlineData(QrModuleShape.ConnectedSquircle, 90)]
    [InlineData(QrModuleShape.Circle, 270)]
    public void SvgContoursRecoverExactCanonicalPixelsAndKeepIllustrationsVector(QrModuleShape shape, int rotation) {
        var options = QrScenePresets.Create(QrSceneStyle.MusicFestival, 600);
        options.ModuleShape = shape; options.Qr.RotationDegrees = rotation;
        var scene = QrArt.ComposeScene(Payload, options);
        var root = XElement.Parse(scene.ToSvg()); var ns = root.Name.Namespace;
        Assert.Equal("0 0 600 600", (string?)root.Attribute("viewBox"));
        Assert.True(root.Descendants(ns + "polygon").Count() > 20);
        Assert.Empty(root.Descendants(ns + "image"));
        var ink = root.Descendants(ns + "path").Single();
        var mask = new bool[600 * 600];
        foreach (Match rect in Regex.Matches((string)ink.Attribute("d")!, @"M(\d+) (\d+)h(\d+)v(\d+)h-\d+z")) {
            var x = int.Parse(rect.Groups[1].Value); var y = int.Parse(rect.Groups[2].Value);
            var width = int.Parse(rect.Groups[3].Value); var height = int.Parse(rect.Groups[4].Value);
            for (var yy = y; yy < y + height; yy++) for (var xx = x; xx < x + width; xx++) mask[yy * 600 + xx] = true;
        }
        var image = scene.Image; var pixels = image.GetPixels();
        for (var y = image.QrOffsetY; y < image.QrOffsetY + image.QrSize; y++) for (var x = image.QrOffsetX; x < image.QrOffsetX + image.QrSize; x++) {
            var p = (y * 600 + x) * 4;
            Assert.Equal(pixels[p] == options.Ink.R && pixels[p + 1] == options.Ink.G && pixels[p + 2] == options.Ink.B, mask[y * 600 + x]);
        }
    }

    [Fact]
    public void PhysicalExportRendersFreshGridAndPngCompressionPreservesPixels() {
        var scene = QrArt.ComposeScene(Payload, QrScenePresets.Create(QrSceneStyle.OceanReef, 600));
        var settings = new QrSceneExportOptions { WidthMillimeters = 80, Dpi = 150, PngCompressionLevel = 0 };
        var export = scene.Export(settings); settings.WidthMillimeters = 100; settings.Dpi = 300;
        Assert.Equal(472, export.Scene.Image.Size); Assert.Equal(150, export.Dpi); Assert.Equal(80, export.WidthMillimeters);
        var png = export.ToPng(); var decoded = PngReader.DecodeRgba32(png, out var width, out var height);
        Assert.Equal(472, width); Assert.Equal(472, height); Assert.Equal(export.Scene.Image.GetPixels(), decoded);
        var compressed = PngImageEncoder.EncodeRgba32(decoded, width, height, 9, 150);
        Assert.True(compressed.Length < png.Length); Assert.Equal(decoded, PngReader.DecodeRgba32(compressed, out _, out _));
        var chunk = Encoding.ASCII.GetBytes("pHYs"); var offset = Find(png, chunk) + 4;
        Assert.True(offset > 4); Assert.Equal(5906u, Read32(png, offset)); Assert.Equal(5906u, Read32(png, offset + 4)); Assert.Equal(1, png[offset + 8]);
        var svg = XElement.Parse(export.ToSvg()); Assert.Equal("80mm", (string?)svg.Attribute("width"));
        Assert.Equal("0 0 472 472", (string?)svg.Attribute("viewBox"));
        Assert.Equal(export.Scene.ToPng(), QrArt.ComposeScene(Payload, export.Scene.Design).ToPng());
#if !NET472
        Assert.True(QrArt.ValidateImage(png, Payload, 3000).AllPassed);
#endif
    }

    [Fact]
    public void PdfPhysicalPageKeepsImageResolutionAndUsesInvariantNumbers() {
        var scene = QrArt.ComposeScene(Payload, QrScenePresets.Create(QrSceneStyle.CosmicOrbit, 600));
        var old = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            var pdf = Encoding.ASCII.GetString(scene.ToPdf(100));
            Assert.Contains("/MediaBox [0 0 283.464567 283.464567]", pdf);
            Assert.Contains("/Subtype /Image /Width 600 /Height 600", pdf);
            Assert.Contains("283.464567 0 0 283.464567 0 0 cm", pdf);
        } finally { CultureInfo.CurrentCulture = old; }
        Assert.Throws<ArgumentOutOfRangeException>(() => scene.ToSvg(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => scene.ToPdf(double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => scene.Export(new() { WidthMillimeters = 10, Dpi = 72 }));
        Assert.Throws<ArgumentException>(() => scene.Export(new() { WidthMillimeters = 200, Dpi = 600 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngImageEncoder.EncodeRgba32(new byte[4], 1, 1, 10));
        Assert.Throws<ArgumentException>(() => PdfWriter.WriteRgba32(2, 3, new byte[24], int.MaxValue));
    }

    [Fact]
    public void SvgLogoIsNormalizedEmbeddedImageWithPreservedAspectRatio() {
        var pixels = Enumerable.Repeat((byte)255, 4 * 2 * 4).ToArray();
        var design = QrScenePresets.Create(QrSceneStyle.OceanReef, 600);
        design.LogoImage = PngImageEncoder.EncodeRgba32(pixels, 4, 2); design.Logo.RotationDegrees = 90;
        var scene = QrArt.ComposeScene(Payload, design);
        var root = XElement.Parse(scene.ToSvg());
        var logo = root.Descendants(root.Name.Namespace + "image").Single();
        Assert.Equal("60", (string?)logo.Attribute("width")); Assert.Equal("30", (string?)logo.Attribute("height"));
        Assert.Equal("rotate(90 300 72)", (string?)logo.Attribute("transform"));
        var href = (string)logo.Attribute("href")!; Assert.StartsWith("data:image/png;base64,", href);
        Assert.Equal(pixels, PngReader.DecodeRgba32(Convert.FromBase64String(href.Substring(href.IndexOf(',') + 1)), out _, out _));
    }

    [Fact]
    public async Task DeliverySearchIsBoundedAndRetainsHonestFailedChecks() {
        var design = QrScenePresets.Create(QrSceneStyle.TropicalGarden, 600); design.Qr.Scale = .4; design.Qr.X = .25;
        var result = await QrArt.SearchSceneAsync(Payload, design, new QrSceneSearchOptions { MaxCandidates = 3, Delivery = new() { ScreenSize = 32, PrintMillimeters = 10, PrintDpi = 72, DecodeBudgetMilliseconds = 1000 } });
        Assert.Equal(3, result.AttemptedCandidates); Assert.True(result.RejectedCandidates > 0); Assert.True(result.ValidatedCandidates >= 1);
        Assert.False(result.Best.Validation.AllPassed); Assert.Contains(result.Best.Validation.Checks, c => c.Name == "ScreenSize" && !c.Passed);
        Assert.Equal(Payload, result.Best.Scene.Payload); Assert.Equal(design.Seed, result.Best.Scene.Design.Seed);
        Assert.Equal(.4, design.Qr.Scale); Assert.Equal(7, result.Best.Validation.Checks.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.SearchScene(Payload, design, new() { MaxCandidates = 7 }));
        Assert.Throws<OperationCanceledException>(() => QrArt.SearchScene(Payload, design, cancellationToken: new CancellationToken(true)));
    }

    [Fact]
    public async Task DeliverySearchSkipsAnUnrenderableOriginalAndFailsClearlyWhenLimited() {
        var design = QrScenePresets.Create(QrSceneStyle.RetroArcade, 600); design.Qr.Scale = .25;
        Assert.Equal(600, QrArt.ComposeScene(Payload, design).Image.Size);
        design.Size = 256;
        var delivery = new QrImageDeliveryOptions { ScreenSize = 256, PrintMillimeters = 43.4, PrintDpi = 150, PerspectiveInset = 0, DecodeBudgetMilliseconds = 1000 };
        var result = await QrArt.SearchSceneAsync(Payload, design, new() { MaxCandidates = 2, Delivery = delivery });
        Assert.Equal(256, result.Best.Scene.Image.Size); Assert.True(result.Best.Scene.Design.Qr.Scale > .25);
        Assert.True(result.RejectedCandidates >= 1); Assert.True(result.ValidatedCandidates >= 1);
        Assert.Equal(Payload, result.Best.Scene.Payload); Assert.Equal(.25, design.Qr.Scale);
        var error = Assert.Throws<ArgumentException>(() => QrArt.SearchScene(Payload, design, new() { MaxCandidates = 1, Delivery = delivery }));
        Assert.Contains("No QR candidate fits", error.Message);
        design.Qr.X = 0;
        Assert.Throws<ArgumentException>(() => QrArt.SearchScene(Payload, design, new() { Delivery = delivery }));
    }

    [Fact]
    public void SceneSvgClipsAtItsOwnViewportWithoutHostFragmentReferences() {
        foreach (var size in new[] { 600, 1200 }) {
            var scene = QrArt.ComposeScene(Payload, QrScenePresets.Create(QrSceneStyle.TropicalGarden, size));
            var root = XElement.Parse(scene.ToSvg());
            Assert.Equal($"0 0 {size} {size}", (string?)root.Attribute("viewBox"));
            Assert.Equal("hidden", (string?)root.Attribute("overflow"));
            Assert.Empty(root.DescendantsAndSelf().Attributes("clip-path"));
        }
    }

    [Fact]
    public async Task DeliverySearchRetainsOriginalWhenAllChecksPass() {
        var design = QrScenePresets.Create(QrSceneStyle.RetroArcade, 600);
        var settings = new QrSceneSearchOptions { Delivery = new() { ScreenSize = 600, PrintMillimeters = 100, PrintDpi = 150, PerspectiveInset = 0, DecodeBudgetMilliseconds = 3000 } };
        var result = await QrArt.SearchSceneAsync(Payload, design, settings);
#if !NET472
        Assert.True(result.Best.Validation.AllPassed); Assert.Equal(1, result.AttemptedCandidates);
#endif
#if !NET472
        Assert.Equal(design.Qr.Scale, result.Best.Scene.Design.Qr.Scale);
        Assert.Equal(design.ModuleShape, result.Best.Scene.Design.ModuleShape);
#endif
        Assert.Equal(Payload, result.Best.Scene.Payload);
        Assert.Equal(result.Best.Scene.ToPng(), QrArt.ComposeScene(Payload, result.Best.Scene.Design).ToPng());
    }
#if !NET472
    [Fact]
    public async Task DeliverySearchCanSelectLargerQrWhenDeliveryIsTooSmall() {
        var design = QrScenePresets.Create(QrSceneStyle.RetroArcade, 600); design.Qr.Scale = .25;
        var delivery = new QrImageDeliveryOptions { ScreenSize = 160, PrintMillimeters = 100, PrintDpi = 150, PerspectiveInset = 0, DecodeBudgetMilliseconds = 3000 };
        var original = QrArt.ValidateDelivery(QrArt.ComposeScene(Payload, design).ToPng(), Payload, delivery);
        var result = await QrArt.SearchSceneAsync(Payload, design, new() { Delivery = delivery });
        Assert.False(original.AllPassed);
        Assert.True(result.Best.Scene.Design.Qr.Scale > design.Qr.Scale);
        Assert.True(result.Best.PassedChecks > original.Checks.Count(c => c.Passed));
        Assert.True(result.Best.Validation.AllPassed);
    }
#endif
    private static uint Read32(byte[] bytes, int p) => (uint)bytes[p] << 24 | (uint)bytes[p + 1] << 16 | (uint)bytes[p + 2] << 8 | bytes[p + 3];
    private static int Find(byte[] bytes, byte[] pattern) { for (var i = 0; i <= bytes.Length - pattern.Length; i++) if (bytes.Skip(i).Take(pattern.Length).SequenceEqual(pattern)) return i; return -1; }
}

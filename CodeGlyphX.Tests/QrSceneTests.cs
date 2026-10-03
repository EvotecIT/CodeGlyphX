using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrSceneTests {
    private const string Payload = "https://example.com/scenes";

    [Theory]
    [InlineData(QrSceneStyle.TropicalGarden)]
    [InlineData(QrSceneStyle.ElectricCity)]
    [InlineData(QrSceneStyle.MusicFestival)]
    [InlineData(QrSceneStyle.OceanReef)]
    [InlineData(QrSceneStyle.CosmicOrbit)]
    [InlineData(QrSceneStyle.RetroArcade)]
    public void PresetsReproduceScenesAndFinalPixelsRecoverExactPayload(QrSceneStyle style) {
        var options = QrScenePresets.Create(style, 600);
        var result = QrArt.ComposeScene(Payload, options);
        Assert.Equal(result.ToPng(), QrArt.ComposeScene(Payload, options).ToPng());
        options.Seed++;
        Assert.NotEqual(result.ToPng(), QrArt.ComposeScene(Payload, options).ToPng());
#if !NET472
        var report = QrArt.ValidateImage(result.ToPng(), Payload, 3000);
        Assert.True(report.AllPassed, string.Join(", ", report.Checks.Select(c => c.Name + "=" + c.Passed)));
#endif
    }

    [Theory]
    [InlineData("https://example.com/?a=<>&b=\"x\"")]
    [InlineData("first\rsecond\r\nthird\nfourth\t ")]
    [InlineData("  ")]
    [InlineData("\0\u0001🎨 café 日本")]
    public void RecipesPreserveExactPayloadAndIndependentSettings(string payload) {
        var options = QrScenePresets.Create(QrSceneStyle.OceanReef, 600);
        options.Seed = int.MinValue;
        options.Motifs.RotationDegrees = 12.123456789;
        options.LogoImage = new byte[] { 1, 2, 3 };
        var recipe = new QrSceneRecipe(payload, options);
        var loaded = QrSceneRecipe.FromXml(recipe.ToXml());
        Assert.Equal(payload, loaded.Payload);
        Assert.Equal(recipe.ToXml(), loaded.ToXml());
        options.Colors[0] = Rgba32.Black;
        options.Motifs.X = .1;
        options.LogoImage[0] = 99;
        var edited = loaded.Design;
        edited.LogoImage![0] = 44;
        edited.Colors[0] = Rgba32.Black;
        Assert.Equal(int.MinValue, loaded.Design.Seed);
        Assert.Equal(.5, loaded.Design.Motifs.X);
        Assert.Equal(12.123456789, loaded.Design.Motifs.RotationDegrees);
        Assert.Equal(1, loaded.Design.LogoImage![0]);
        Assert.Equal(recipe.Design.Colors, loaded.Design.Colors);
    }

    [Fact]
    public void RecipesRejectUnsupportedAndUntrustedInput() {
        var xml = new QrSceneRecipe(Payload, QrScenePresets.Create(QrSceneStyle.RetroArcade)).ToXml();
        foreach (var malformed in new[] {
            "<!DOCTYPE cgx-scene [<!ENTITY external SYSTEM 'file:///must-not-open'>]>" + xml,
            xml.Replace("version=\"1\"", "version=\"2\""),
            xml.Replace("</cgx-scene>", "<payload>duplicate</payload></cgx-scene>"),
            xml.Replace("</cgx-scene>", "<unknown /></cgx-scene>"),
            xml.Replace("name=\"motifs\"", "name=\"qr\""),
            xml.Replace("rotation=\"0\"", "rotation=\"NaN\""),
            xml + "<cgx-scene />",
            new string('x', QrSceneRecipe.MaxCharacters + 1)
        }) Assert.Throws<FormatException>(() => QrSceneRecipe.FromXml(malformed));
    }

    [Fact]
    public void FinishedSceneSnapshotsDesignAndPixels() {
        var options = QrScenePresets.Create(QrSceneStyle.TropicalGarden, 400);
        var result = QrArt.ComposeScene(Payload, options);
        var png = result.ToPng();
        options.Colors[0] = Rgba32.Black;
        options.Qr.X = .1;
        result.Design.Motifs.Visible = false;
        var pixels = result.Image.GetPixels(); pixels[0] = 0;
        Assert.Equal(png, result.ToPng());
        var replay = QrSceneRecipe.FromXml(result.ToRecipe().ToXml());
        Assert.Equal(png, QrArt.ComposeScene(replay.Payload, replay.Design).ToPng());
        Assert.Equal(Payload, result.Payload);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(-90)]
    [InlineData(180)]
    public void ForegroundArtworkCannotCoverQuietZoneOrQr(int rotation) {
        var options = QrScenePresets.Create(QrSceneStyle.ElectricCity, 400);
        options.Qr.RotationDegrees = rotation;
        options.CaptionLayer.Y = .5;
        options.CaptionLayer.Scale = 2;
        options.LogoImage = PngImageEncoder.EncodeRgba32(new byte[] { 255, 0, 0, 255 }, 1, 1);
        options.Logo.X = options.Logo.Y = .5; options.Logo.Scale = 2;
        var result = QrArt.ComposeScene(Payload, options);
        var pixels = result.Image.GetPixels();
        var blank = result.Design; blank.Motifs.Visible = blank.Backdrop.Visible = blank.CaptionLayer.Visible = blank.Logo.Visible = false;
        var clear = QrArt.ComposeScene(Payload, blank).Image.GetPixels();
        for (var y = 0; y < result.Image.QrSize; y++) {
            var offset = ((result.Image.QrOffsetY + y) * options.Size + result.Image.QrOffsetX) * 4;
            Assert.Equal(clear.Skip(offset).Take(result.Image.QrSize * 4), pixels.Skip(offset).Take(result.Image.QrSize * 4));
        }
        var top = (result.Image.QrOffsetY * options.Size + result.Image.QrOffsetX) * 4;
        Assert.Equal(new[] { options.Paper.R, options.Paper.G, options.Paper.B, (byte)255 }, pixels.Skip(top).Take(4));
    }

    [Fact]
    public void LogosFitTheirAspectRatioAndRotateWithoutStretching() {
        var options = QrScenePresets.Create(QrSceneStyle.TropicalGarden, 400);
        options.Backdrop.Visible = options.Motifs.Visible = options.CaptionLayer.Visible = false;
        var red = Enumerable.Range(0, 8).SelectMany(_ => new byte[] { 255, 0, 0, 255 }).ToArray();
        options.LogoImage = PngImageEncoder.EncodeRgba32(red, 4, 2);
        options.Logo.X = .5; options.Logo.Y = .1; options.Logo.Scale = .2;
        var horizontal = QrArt.ComposeScene(Payload, options).Image.GetPixels();
        AssertPixel(horizontal, 165, 40, new Rgba32(255, 0, 0));
        AssertPixel(horizontal, 200, 10, options.Paper);
        options.Logo.RotationDegrees = 90;
        var vertical = QrArt.ComposeScene(Payload, options).Image.GetPixels();
        AssertPixel(vertical, 165, 40, options.Paper);
        AssertPixel(vertical, 200, 10, new Rgba32(255, 0, 0));
        static void AssertPixel(byte[] pixels, int x, int y, Rgba32 color) {
            var p = (y * 400 + x) * 4;
            Assert.Equal(new[] { color.R, color.G, color.B, color.A }, pixels.Skip(p).Take(4));
        }
    }

    [Fact]
    public void SceneRejectsClippedQrUnsupportedCaptionsAndUnboundedInput() {
        var options = QrScenePresets.Create(QrSceneStyle.TropicalGarden, 400);
        options.Qr.X = 0;
        Assert.Throws<ArgumentException>(() => QrArt.ComposeScene(Payload, options));
        options.Qr.X = .5; options.Caption = "unsupported 🎨";
        Assert.Throws<ArgumentException>(() => QrArt.ComposeScene(Payload, options));
        options.Caption = "SCAN ME"; options.Size = 4097;
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.ComposeScene(Payload, options));
        Assert.Throws<OperationCanceledException>(() => QrArt.ComposeScene(Payload, cancellationToken: new CancellationToken(true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrScenePresets.Create((QrSceneStyle)99));
        var first = QrScenePresets.Create(QrSceneStyle.TropicalGarden); first.Colors[0] = Rgba32.Black;
        Assert.NotEqual(first.Colors[0], QrScenePresets.Create(first.Style).Colors[0]);
    }
}

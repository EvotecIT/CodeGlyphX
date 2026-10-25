using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrHeroPresetTests {
    private const string Payload = "https://example.com/qr-art/wave1?scan=independent";
    private readonly ITestOutputHelper _output;
    public QrHeroPresetTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> HeroCases() {
        foreach (QrHeroPreset preset in Enum.GetValues(typeof(QrHeroPreset))) {
            yield return new object[] { preset, false };
            yield return new object[] { preset, true };
        }
    }

    [Theory]
    [Trait("Category", "CorpusTiming")]
    [MemberData(nameof(HeroCases))]
    public void Heroes_DecodeWithNativeAndStrictIndependentReaders_AcrossSizeRotationAndBlur(QrHeroPreset preset, bool darkPanel) {
        var options = QrArt.Hero(preset, darkPanel);
        if (preset == QrHeroPreset.Brand) {
            var logo = PngImageEncoder.EncodeRgba32(new byte[] { 20, 60, 110, 255 }, 1, 1);
            var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
            var fit = QrArt.FitLogo(qr);
            options.LogoPng = logo;
            options.LogoScale = fit.LogoSideModules / (double)qr.Size;
            options.LogoPaddingPx = fit.PaddingModules * options.ModuleSize;
            options.LogoOffsetXPx = (fit.XModules + fit.PaddingModules) * options.ModuleSize;
            options.LogoOffsetYPx = (fit.YModules + fit.PaddingModules) * options.ModuleSize;
        }
        var observations = QrArtWave1ScanTests.ScanMatrix(options).ToArray();
        Assert.Equal(9, observations.Length);
        foreach (var observation in observations) {
            _output.WriteLine(observation.ToString());
            Assert.True(observation.StrictPassed && observation.LibraryPassed, observation.ToString());
        }
    }

    [Theory]
    [MemberData(nameof(HeroCases))]
    public void HeroOptions_AreIndependent_AndNeverInvertTheLightCode(QrHeroPreset preset, bool darkPanel) {
        var first = QrArt.Hero(preset, darkPanel);
        var second = QrArt.Hero(preset, darkPanel);
        Assert.NotSame(first, second);
        Assert.NotSame(first.Eyes, second.Eyes);
        Assert.True(second.Background.R >= 200 && second.Background.G >= 200 && second.Background.B >= 200);
        Assert.Equal(4, second.QuietZone);
        if (darkPanel) Assert.NotNull(second.Canvas);
        first.Eyes!.InnerColor = Rgba32.White;
        Assert.Null(second.Eyes!.InnerColor);
        Assert.False(string.IsNullOrWhiteSpace(QrArt.DescribeHero(preset)));
    }
}

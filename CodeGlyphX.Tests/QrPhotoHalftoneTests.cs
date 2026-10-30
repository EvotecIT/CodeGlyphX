using System;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

/// <summary>
/// Contracts of <see cref="QrImageCompositionStyle.PhotoHalftone"/>: identical input gives identical bytes, every module
/// centre keeps decode-safe contrast in its own colour, the quiet zone stays uniformly light, the logo plate respects its
/// budget, and invalid settings are refused. QrPhotoHalftoneScanTests covers strict decoding after resizing and blur.
/// </summary>
[Collection("ImageScannerSerial")]
public sealed class QrPhotoHalftoneTests {
    private const string Payload = "https://example.com/halftone";
    private const int Module = 24;

    public static IEnumerable<object[]> Screens() {
        foreach (var screen in new[] { QrHalftoneScreen.Dots, QrHalftoneScreen.ErrorDiffusion, QrHalftoneScreen.Ordered })
            foreach (var duotone in new[] { false, true })
                yield return new object[] { screen, duotone };
    }

    [Theory]
    [MemberData(nameof(Screens))]
    public void SameInput_GivesIdenticalBytes(QrHalftoneScreen screen, bool duotone) {
        var photo = Photo(160);
        byte[] Render() => QrArt.Compose(Payload, photo, Options(screen, duotone)).ToPng();
        var first = Render();
        Assert.Equal(first, Render());
        // A different photo changes the picture, so the screen really follows the image.
        Assert.NotEqual(first, QrArt.Compose(Payload, Photo(160, mirror: true), Options(screen, duotone)).ToPng());
    }

    [Theory]
    [MemberData(nameof(Screens))]
    public void ModuleCentres_KeepDecodeSafeContrast_AndQuietZoneStaysLight(QrHalftoneScreen screen, bool duotone) {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 6 });
        var source = ImageReader.DecodeRgba32(Photo(160), out var width, out var height);
        var options = Options(screen, duotone);
        var result = QrImageComposer.Render(qr, source, width, height, options);
        var pixels = result.GetPixels();
        var quiet = options.QuietZone * Module;
        Assert.Equal((qr.Size + 2 * options.QuietZone) * Module, result.Size);

        // Duotone centres are the ink and paper themselves; photo centres are a deep or light tone of the local colour.
        double darkLimit = duotone ? Luma(options.Duotone!.Ink) + 0.5 : 31;
        double lightLimit = duotone ? Luma(options.Duotone!.Paper) - 0.5 : 239;
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        for (var my = 0; my < qr.Size; my++) for (var mx = 0; mx < qr.Size; mx++) {
            // The central 28% of every module is solid in the module's colour. Finder and alignment rings have rounded
            // corners, so for their modules only the centre itself is checked.
            var reach = InRing(qr, mx, my) ? 1 : 3;
            for (var dy = -reach; dy <= reach; dy++) for (var dx = -reach; dx <= reach; dx++) {
                var x = quiet + mx * Module + Module / 2 + dx;
                var y = quiet + my * Module + Module / 2 + dy;
                var luma = Luma(pixels, (y * result.Size + x) * 4);
                if (qr.Modules[mx, my]) Assert.True(luma <= darkLimit, $"Dark {(functions[mx, my] ? "function" : "data")} module {mx},{my} centre luma {luma:0.0}.");
                else Assert.True(luma >= lightLimit, $"Light {(functions[mx, my] ? "function" : "data")} module {mx},{my} centre luma {luma:0.0}.");
            }
        }
        var paper = duotone ? options.Duotone!.Paper : Rgba32.White;
        for (var y = 0; y < result.Size; y++) for (var x = 0; x < result.Size; x++) {
            if (x >= quiet && y >= quiet && x < result.Size - quiet && y < result.Size - quiet) continue;
            var p = (y * result.Size + x) * 4;
            Assert.Equal((paper.R, paper.G, paper.B), (pixels[p], pixels[p + 1], pixels[p + 2]));
        }
        AssertPayload(result);
    }

    [Fact]
    public void FinderPatterns_KeepTheirRingsAndSeparatorBand() {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 6 });
        var source = ImageReader.DecodeRgba32(Photo(160), out var width, out var height);
        var result = QrImageComposer.Render(qr, source, width, height, Options(QrHalftoneScreen.Dots, false));
        var pixels = result.GetPixels();
        var quiet = 4 * Module;
        // Along the finder's middle row: dark ring, light gap, dark ball, light gap, dark ring, then the light separator band.
        var row = quiet + (int)(3.5 * Module);
        var expected = new[] { true, false, true, true, true, false, true };
        for (var module = 0; module < 7; module++) {
            var luma = Luma(pixels, (row * result.Size + quiet + module * Module + Module / 2) * 4);
            Assert.True(expected[module] ? luma <= 31 : luma >= 239, $"Finder module {module}: luma {luma:0.0}.");
        }
        var band = Luma(pixels, (row * result.Size + quiet + 7 * Module + Module / 4) * 4);
        Assert.True(band >= 239, $"Separator band luma {band:0.0}.");
    }

    [Fact]
    public void TargetSize_ResamplesTheWholeComposition() {
        var options = Options(QrHalftoneScreen.Dots, false);
        options.TargetSizePx = 320;
        var result = QrArt.Compose(Payload, Photo(160), options);
        Assert.Equal(320, result.Size);
        Assert.Equal(320, result.QrSize);
        Assert.Equal((0, 0), (result.QrOffsetX, result.QrOffsetY));
        var pixels = result.GetPixels();
        Assert.Equal((255, 255, 255), (pixels[0], pixels[1], pixels[2]));
    }

    [Fact]
    public void Logo_IsPlacedOnAPlateWithinTheBudget_OrRefused() {
        var logo = PngImageEncoder.EncodeRgba32(Solid(40, new Rgba32(200, 20, 60)), 40, 40);
        var options = Options(QrHalftoneScreen.Dots, true);
        options.Logo = new QrImageLogoOptions { Image = logo };
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 6 });
        var budget = QrArt.FitLogo(qr, 0, 1, 3, 3);
        var source = ImageReader.DecodeRgba32(Photo(160), out var width, out var height);
        var result = QrImageComposer.Render(qr, source, width, height, options);
        var pixels = result.GetPixels();
        var quiet = 4 * Module;
        // Padding is the plate (duotone paper); the logo's centre is the logo colour.
        var padX = quiet + budget.XModules * Module + Module / 2;
        var padY = quiet + (budget.YModules + budget.KnockoutSideModules / 2) * Module;
        var paper = options.Duotone!.Paper;
        var p = (padY * result.Size + padX) * 4;
        Assert.Equal((paper.R, paper.G, paper.B), (pixels[p], pixels[p + 1], pixels[p + 2]));
        var centre = quiet + (budget.XModules * 2 + budget.KnockoutSideModules) * Module / 2;
        var centreY = quiet + (budget.YModules * 2 + budget.KnockoutSideModules) * Module / 2;
        p = (centreY * result.Size + centre) * 4;
        Assert.Equal((200, 20, 60), (pixels[p], pixels[p + 1], pixels[p + 2]));
        AssertPayload(result);

        options.Logo.MinimumLogoSideModules = 40;
        Assert.Throws<InvalidOperationException>(() => QrArt.Compose(Payload, Photo(160), options));
    }

    [Fact]
    public void InvalidSettings_AreRefused() {
        var photo = Photo(64);
        void Refuse<T>(Action<QrImageCompositionOptions> change) where T : Exception {
            var options = Options(QrHalftoneScreen.Dots, false);
            change(options);
            Assert.Throws<T>(() => QrArt.Compose(Payload, photo, options));
        }
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.SubCells = 2);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.SubCells = 8);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.CenterSize = 0.29);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.CenterSize = 0.61);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.CenterSize = double.NaN);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.Contrast = 0.4);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.Contrast = 2.1);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.Brightness = 0.41);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.Screen = (QrHalftoneScreen)3);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.DotShape = (QrHalftoneDotShape)2);
        Refuse<ArgumentOutOfRangeException>(o => o.Halftone!.Finders = (QrImageFinderStyle)5);
        Refuse<ArgumentOutOfRangeException>(o => o.TargetSizePx = 15);
        Refuse<ArgumentOutOfRangeException>(o => o.TargetSizePx = 8193);
        Refuse<ArgumentException>(o => o.Logo = new QrImageLogoOptions());
        Refuse<ArgumentException>(o => o.Logo = new QrImageLogoOptions { Image = new byte[] { 1 }, Plate = new Rgba32(255, 255, 255, 128) });
        Refuse<ArgumentOutOfRangeException>(o => o.Logo = new QrImageLogoOptions { Image = new byte[] { 1 }, PaddingModules = 5 });
        Refuse<ArgumentException>(o => o.Art = new QrImageArtOptions());
        Refuse<ArgumentException>(o => o.Duotone = new QrImageDuotoneOptions { Ink = new Rgba32(120, 120, 120) });
        // Halftone settings belong to PhotoHalftone only.
        Refuse<ArgumentException>(o => o.Style = QrImageCompositionStyle.ImageOverlay);
        var plain = new QrImageCompositionOptions { Style = QrImageCompositionStyle.ColorModules, Halftone = new QrImageHalftoneOptions() };
        Assert.Throws<ArgumentException>(() => QrArt.Compose(Payload, photo, plain));
    }

    [Fact]
    public void Defaults_AreConservative() {
        var halftone = new QrImageHalftoneOptions();
        Assert.Equal(5, halftone.SubCells);
        Assert.Equal(QrHalftoneScreen.Dots, halftone.Screen);
        Assert.Equal(QrHalftoneDotShape.Round, halftone.DotShape);
        Assert.Equal(0.42, halftone.CenterSize);
        Assert.Equal(QrImageFinderStyle.Rounded, halftone.Finders);
        var logo = new QrImageLogoOptions();
        Assert.Equal((1, 3, 3), (logo.PaddingModules, logo.ReservedErrorsPerBlock, logo.MinimumLogoSideModules));
        // Default composition settings, including the 12 px module, still produce a readable symbol.
        var result = QrArt.Compose(Payload, Photo(160), new QrImageCompositionOptions { Style = QrImageCompositionStyle.PhotoHalftone });
        AssertPayload(result);
    }

    internal static QrImageCompositionOptions Options(QrHalftoneScreen screen, bool duotone) => new() {
        Style = QrImageCompositionStyle.PhotoHalftone,
        ModuleSize = Module,
        Duotone = duotone ? new QrImageDuotoneOptions() : null,
        Halftone = new QrImageHalftoneOptions { Screen = screen },
    };

    // A deterministic photo-like test image: a warm-to-cool sky, a dark disc and a bright diagonal band.
    internal static byte[] Photo(int side, bool mirror = false) {
        var pixels = new byte[side * side * 4];
        for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) {
            var u = (mirror ? side - 1 - x : x) / (double)(side - 1);
            var v = y / (double)(side - 1);
            double r = 240 - 150 * v, g = 170 + 40 * u - 60 * v, b = 90 + 140 * v;
            if ((u - 0.38) * (u - 0.38) + (v - 0.55) * (v - 0.55) < 0.06) { r = 52; g = 40; b = 70; }
            if (Math.Abs(u - v - 0.25) < 0.06) { r = 250; g = 246; b = 228; }
            var p = (y * side + x) * 4;
            pixels[p] = (byte)Math.Round(r); pixels[p + 1] = (byte)Math.Round(g); pixels[p + 2] = (byte)Math.Round(b); pixels[p + 3] = 255;
        }
        return PngImageEncoder.EncodeRgba32(pixels, side, side);
    }

    private static byte[] Solid(int side, Rgba32 color) {
        var pixels = new byte[side * side * 4];
        for (var i = 0; i < pixels.Length; i += 4) { pixels[i] = color.R; pixels[i + 1] = color.G; pixels[i + 2] = color.B; pixels[i + 3] = 255; }
        return pixels;
    }

    private static void AssertPayload(QrImageComposition result) {
        var png = result.ToPng();
        var pixels = ImageReader.DecodeRgba32(png, out var width, out var height);
        Assert.True(QrImageDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, DisableTransforms = true }, out var decoded));
        Assert.Equal(Payload, decoded.Text);
        Assert.Equal(Payload, QrLogoPlacementTests.IndependentDecode(png));
    }

    private static bool InRing(QrCode qr, int x, int y) {
        if ((x < 7 && y < 7) || (x >= qr.Size - 7 && y < 7) || (x < 7 && y >= qr.Size - 7)) return true;
        var positions = QrTables.GetAlignmentPatternPositions(qr.Version);
        foreach (var ay in positions) foreach (var ax in positions)
            if (Math.Abs(x - ax) <= 2 && Math.Abs(y - ay) <= 2 && !((ax < 8 && ay < 8) || (ax >= qr.Size - 8 && ay < 8) || (ax < 8 && ay >= qr.Size - 8))) return true;
        return false;
    }

    private static double Luma(byte[] pixels, int p) => 0.299 * pixels[p] + 0.587 * pixels[p + 1] + 0.114 * pixels[p + 2];
    private static double Luma(Rgba32 color) => 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
}

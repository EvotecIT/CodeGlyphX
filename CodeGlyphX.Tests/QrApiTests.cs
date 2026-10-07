using System;
using System.Text;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrApiTests {
    [Theory]
    [InlineData(QrEciMode.Auto)]
    [InlineData(QrEciMode.Never)]
    [InlineData(QrEciMode.Always)]
    public void Encode_HonorsExplicitEciPolicy(QrEciMode eciMode) {
        const string text = "Zażółć 😀";
        var options = new QrEncodingOptions { EciMode = eciMode, ForceMask = 0 };
        var actual = QR.Encode(text, options);
        var expected = QrCodeEncoder.EncodeText(text, options);

        AssertSameModules(expected, actual);
        Assert.True(QrDecoder.TryDecode(actual.Modules, out var decoded));
        Assert.Equal(Encoding.UTF8.GetBytes(text), decoded.Bytes);
    }

    [Fact]
    public void Encode_UsesStableDefaultCorrectionForPlainText() {
        Assert.Equal(QrErrorCorrectionLevel.M, QR.Encode("otpauth://totp/Example?secret=ABCDEF").ErrorCorrectionLevel);
    }

    [Fact]
    public void Encode_PayloadRecommendationsApplyOnlyWhenOptionsAreOmitted() {
        var payload = new QrPayloadData("HELLO", QrErrorCorrectionLevel.H, minVersion: 7);
        var recommended = QR.Encode(payload);
        var explicitOptions = QR.Encode(payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.L });

        Assert.Equal(QrErrorCorrectionLevel.H, recommended.ErrorCorrectionLevel);
        Assert.Equal(7, recommended.Version);
        Assert.Equal(QrErrorCorrectionLevel.L, explicitOptions.ErrorCorrectionLevel);
        Assert.Equal(1, explicitOptions.Version);
        AssertSameModules(explicitOptions, QR.Create(payload, encodingOptions: new QrEncodingOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.L
        }).Encode());
    }

    [Fact]
    public void Render_LeavesTheEncodedSymbolUnchanged() {
        var qr = QR.Encode("HELLO", new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.L, ForceMask = 0 });
        var before = qr.Modules.Clone();
        var appearance = new QrRenderOptions { Art = QrArt.Theme(QrArtTheme.NeonGlow), ModuleSize = 4 };

        var pixels = qr.RenderPixels(out var width, out var height, out var stride, appearance);

        Assert.Equal(1, qr.Version);
        Assert.Equal(QrErrorCorrectionLevel.L, qr.ErrorCorrectionLevel);
        Assert.Equal(0, qr.Mask);
        Assert.Equal(width * 4, stride);
        Assert.Equal(stride * height, pixels.Length);
        for (var y = 0; y < before.Height; y++) {
            for (var x = 0; x < before.Width; x++) Assert.Equal(before[x, y], qr.Modules[x, y]);
        }
    }

    [Fact]
    public void Builder_OwnsIndependentEncodingAndNestedAppearanceState() {
        var encoding = new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, ForceMask = 2 };
        var rendering = new QrRenderOptions {
            ModuleSize = 5,
            Art = QrArt.Theme(QrArtTheme.PaintSplash, intensity: 55),
            Debug = new QrRasterDebugOptions { StrokePx = 2 },
            ForegroundPalette = new QrPaletteOptions { Colors = new[] { Rgba32.Black, Rgba32.White } }
        };
        var builder = QR.Create("BUILDER", rendering, encoding);
        encoding.ErrorCorrectionLevel = QrErrorCorrectionLevel.L;
        rendering.ModuleSize = 9;
        rendering.Art!.Intensity = 90;
        rendering.Debug!.StrokePx = 8;
        rendering.ForegroundPalette!.Colors[0] = Rgba32.White;

        Assert.Equal(QrErrorCorrectionLevel.H, builder.Encoding.ErrorCorrectionLevel);
        Assert.Equal(5, builder.Rendering.ModuleSize);
        Assert.Equal(55, builder.Rendering.Art!.Intensity);
        Assert.Equal(2, builder.Rendering.Debug!.StrokePx);
        Assert.Equal(Rgba32.Black, builder.Rendering.ForegroundPalette!.Colors[0]);

        builder.WithEncoding(options => options.ForceMask = 1).WithRendering(options => options.ModuleSize = 4);
        Assert.Equal(2, encoding.ForceMask);
        Assert.Equal(9, rendering.ModuleSize);
        Assert.Equal(1, builder.Encode().Mask);
    }

    [Theory]
    [InlineData(OutputFormat.Svg)]
    [InlineData(OutputFormat.Svgz)]
    [InlineData(OutputFormat.Html)]
    public void VectorOutputs_RejectAppearanceTheyCannotRepresent(OutputFormat format) {
        var options = new QrRenderOptions { ForegroundPalette = new QrPaletteOptions() };
        var error = Assert.Throws<NotSupportedException>(() => QR.Render("VECTOR", format, options));

        Assert.Contains(nameof(QrRenderOptions.ForegroundPalette), error.Message);
        Assert.Contains(format.ToString(), error.Message);
    }

    [Fact]
    public void BuilderStyleSetters_SnapshotCallerOwnedOptionsAndNestedArrays() {
        var scaleMap = new QrModuleScaleMapOptions { MinScale = 0.8 };
        var shapeMap = new QrModuleShapeMapOptions { SecondaryChance = 0.2 };
        var jitter = new QrModuleJitterOptions { MaxOffsetPx = 1 };
        var foreground = new QrGradientOptions { StartColor = Rgba32.Black };
        var background = new QrGradientOptions { StartColor = Rgba32.White };
        var palette = new QrPaletteOptions { Colors = new[] { Rgba32.Black } };
        var canvas = new QrCanvasOptions {
            BackgroundGradient = new QrGradientOptions { StartColor = Rgba32.White },
            Splash = new QrCanvasSplashOptions { Count = 2, Colors = new[] { Rgba32.Black } }
        };
        var zones = new QrPaletteZoneOptions {
            CenterPalette = new QrPaletteOptions { Colors = new[] { Rgba32.Black } },
            CornerPalette = new QrPaletteOptions { Colors = new[] { Rgba32.White } }
        };
        var eyes = new QrEyeOptions {
            OuterColors = new[] { Rgba32.Black },
            InnerColors = new[] { Rgba32.White },
            OuterGradient = new QrGradientOptions { StartColor = Rgba32.Black },
            InnerGradient = new QrGradientOptions { StartColor = Rgba32.White },
            OuterGradients = new[] { new QrGradientOptions { StartColor = Rgba32.Black } },
            InnerGradients = new[] { new QrGradientOptions { StartColor = Rgba32.White } }
        };
        var builder = QR.Create("STYLE-OWNERSHIP")
            .WithModuleScaleMap(scaleMap)
            .WithModuleShapeMap(shapeMap)
            .WithModuleJitter(jitter)
            .WithForegroundGradient(foreground)
            .WithBackgroundGradient(background)
            .WithForegroundPalette(palette)
            .WithCanvas(canvas)
            .WithForegroundPaletteZones(zones)
            .WithEyes(eyes);

        scaleMap.MinScale = 0.9;
        shapeMap.SecondaryChance = 0.7;
        jitter.MaxOffsetPx = 3;
        foreground.StartColor = Rgba32.White;
        background.StartColor = Rgba32.Black;
        palette.Colors[0] = Rgba32.White;
        canvas.BackgroundGradient.StartColor = Rgba32.Black;
        canvas.Splash.Count = 9;
        canvas.Splash.Colors![0] = Rgba32.White;
        zones.CenterPalette.Colors[0] = Rgba32.White;
        zones.CornerPalette.Colors[0] = Rgba32.Black;
        eyes.OuterColors[0] = Rgba32.White;
        eyes.InnerColors[0] = Rgba32.Black;
        eyes.OuterGradient.StartColor = Rgba32.White;
        eyes.InnerGradient.StartColor = Rgba32.Black;
        eyes.OuterGradients[0].StartColor = Rgba32.White;
        eyes.InnerGradients[0].StartColor = Rgba32.Black;

        Assert.Equal(0.8, builder.Rendering.ModuleScaleMap!.MinScale);
        Assert.Equal(0.2, builder.Rendering.ModuleShapeMap!.SecondaryChance);
        Assert.Equal(1, builder.Rendering.ModuleJitter!.MaxOffsetPx);
        Assert.Equal(Rgba32.Black, builder.Rendering.ForegroundGradient!.StartColor);
        Assert.Equal(Rgba32.White, builder.Rendering.BackgroundGradient!.StartColor);
        Assert.Equal(Rgba32.Black, builder.Rendering.ForegroundPalette!.Colors[0]);
        Assert.Equal(Rgba32.White, builder.Rendering.Canvas!.BackgroundGradient!.StartColor);
        Assert.Equal(2, builder.Rendering.Canvas.Splash!.Count);
        Assert.Equal(Rgba32.Black, builder.Rendering.Canvas.Splash.Colors![0]);
        Assert.Equal(Rgba32.Black, builder.Rendering.ForegroundPaletteZones!.CenterPalette!.Colors[0]);
        Assert.Equal(Rgba32.White, builder.Rendering.ForegroundPaletteZones.CornerPalette!.Colors[0]);
        Assert.Equal(Rgba32.Black, builder.Rendering.Eyes!.OuterColors![0]);
        Assert.Equal(Rgba32.White, builder.Rendering.Eyes.InnerColors![0]);
        Assert.Equal(Rgba32.Black, builder.Rendering.Eyes.OuterGradient!.StartColor);
        Assert.Equal(Rgba32.White, builder.Rendering.Eyes.InnerGradient!.StartColor);
        Assert.Equal(Rgba32.Black, builder.Rendering.Eyes.OuterGradients![0].StartColor);
        Assert.Equal(Rgba32.White, builder.Rendering.Eyes.InnerGradients![0].StartColor);
    }

    [Fact]
    public void OtpBuilder_UsesExplicitWorkflowDefaultsAndCopiesOptions() {
        var encoding = new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.Q };
        var rendering = new QrRenderOptions { ModuleSize = 5 };
        var builder = Otp.Totp("Example", "user", "JBSWY3DPEHPK3PXP", rendering, encoding);
        encoding.ErrorCorrectionLevel = QrErrorCorrectionLevel.L;
        rendering.ModuleSize = 10;

        Assert.Equal(QrErrorCorrectionLevel.Q, builder.Encode().ErrorCorrectionLevel);
        Assert.Equal(5, builder.Rendering.ModuleSize);
        Assert.Equal(QrErrorCorrectionLevel.H, Otp.Totp("Example", "user", "JBSWY3DPEHPK3PXP").Encode().ErrorCorrectionLevel);
    }

    [Fact]
    public void Render_DoesNotMutateCallerOwnedAsciiLayout() {
        var ascii = new MatrixAsciiRenderOptions { QuietZone = RenderDefaults.QrQuietZone };
        var outputOptions = new OutputOptions { MatrixAscii = ascii };

        var text = QR.Render("ASCII", OutputFormat.Ascii, new QrRenderOptions { QuietZone = 6 }, outputOptions: outputOptions).GetText();

        Assert.NotEmpty(text);
        Assert.Equal(RenderDefaults.QrQuietZone, ascii.QuietZone);
    }

    private static void AssertSameModules(QrCode expected, QrCode actual) {
        Assert.Equal(expected.Size, actual.Size);
        for (var y = 0; y < expected.Size; y++) {
            for (var x = 0; x < expected.Size; x++) Assert.Equal(expected.Modules[x, y], actual.Modules[x, y]);
        }
    }
}

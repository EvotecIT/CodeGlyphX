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
            Debug = new QrPngDebugOptions { StrokePx = 2 },
            ForegroundPalette = new QrPngPaletteOptions { Colors = new[] { Rgba32.Black, Rgba32.White } }
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
        var options = new QrRenderOptions { ForegroundPalette = new QrPngPaletteOptions() };
        var error = Assert.Throws<NotSupportedException>(() => QR.Render("VECTOR", format, options));

        Assert.Contains(nameof(QrRenderOptions.ForegroundPalette), error.Message);
        Assert.Contains(format.ToString(), error.Message);
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
        var extras = new RenderExtras { MatrixAscii = ascii };

        var text = QR.Render("ASCII", OutputFormat.Ascii, new QrRenderOptions { QuietZone = 6 }, extras: extras).GetText();

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

using System.Threading;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeGlyphDecodeOptionsTests {
    [Fact]
    public void Decode_UsesOptionsObject() {
        var qr = QrCodeEncoder.EncodeText("OPTIONS");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 4, QuietZone = 4 });
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var options = new CodeGlyphDecodeOptions {
            Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast }
        };

        Assert.True(CodeGlyph.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal("OPTIONS", decoded.Text);
    }

    [Fact]
    public void Decode_RespectsCancellationToken() {
        var qr = QrCodeEncoder.EncodeText("CANCEL");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 4, QuietZone = 4 });
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var options = new CodeGlyphDecodeOptions {
            Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust },
            CancellationToken = cts.Token
        };

        Assert.False(CodeGlyph.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out _, options));
    }

    [Fact]
    public void Options_ProxyProperties_CreateQrOptions() {
        var options = new CodeGlyphDecodeOptions {
            Profile = QrDecodeProfile.Fast,
            MaxMilliseconds = 321,
            MaxDimension = 1234,
            MaxScale = 3,
            DisableTransforms = true,
            AggressiveSampling = true
        };

        Assert.NotNull(options.Qr);
        Assert.Equal(QrDecodeProfile.Fast, options.Qr!.Profile);
        Assert.Equal(321, options.Qr.MaxMilliseconds);
        Assert.Equal(1234, options.Qr.MaxDimension);
        Assert.Equal(3, options.Qr.MaxScale);
        Assert.True(options.Qr.DisableTransforms);
        Assert.True(options.Qr.AggressiveSampling);
    }

    [Fact]
    public void ScreenPreset_SetsQrBudget() {
        var options = CodeGlyphDecodeOptions.Screen(maxMilliseconds: 250, maxDimension: 900);

        Assert.NotNull(options.Qr);
        Assert.Equal(QrDecodeProfile.Balanced, options.Qr!.Profile);
        Assert.Equal(250, options.Qr.MaxMilliseconds);
        Assert.Equal(900, options.Qr.MaxDimension);
    }

    [Fact]
    public void Decode_UsesBarcodeOptions() {
        var barcode = BarcodeEncoder.EncodeCode39("ABC123", includeChecksum: true, fullAsciiMode: false);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        var options = new CodeGlyphDecodeOptions {
            ExpectedBarcode = BarcodeType.Code39,
            Barcode = new BarcodeDecodeOptions { Code39Checksum = Code39ChecksumPolicy.StripIfValid },
            PreferBarcode = true
        };

        Assert.True(CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal(BarcodeType.Code39, decoded.Barcode!.Type);
        Assert.Equal("ABC123", decoded.Text);
    }

    [Fact]
    public void Presets_SetExpectedProfiles() {
        Assert.Equal(QrDecodeProfile.Fast, CodeGlyphDecodeOptions.Fast().Qr!.Profile);
        Assert.Equal(QrDecodeProfile.Balanced, CodeGlyphDecodeOptions.Balanced().Qr!.Profile);
        Assert.Equal(QrDecodeProfile.Robust, CodeGlyphDecodeOptions.Robust().Qr!.Profile);
        Assert.True(CodeGlyphDecodeOptions.Stylized().Qr!.AggressiveSampling);
    }

    [Fact]
    public void BarcodeProxyProperties_CreateBarcodeOptions() {
        var options = new CodeGlyphDecodeOptions {
            Code39Checksum = Code39ChecksumPolicy.StripIfValid,
            MsiChecksum = MsiChecksumPolicy.RequireValid,
            Code11Checksum = Code11ChecksumPolicy.StripIfValid,
            PlesseyChecksum = PlesseyChecksumPolicy.None
        };

        Assert.NotNull(options.Barcode);
        Assert.Equal(Code39ChecksumPolicy.StripIfValid, options.Barcode!.Code39Checksum);
        Assert.Equal(MsiChecksumPolicy.RequireValid, options.Barcode.MsiChecksum);
        Assert.Equal(Code11ChecksumPolicy.StripIfValid, options.Barcode.Code11Checksum);
        Assert.Equal(PlesseyChecksumPolicy.None, options.Barcode.PlesseyChecksum);
    }
}

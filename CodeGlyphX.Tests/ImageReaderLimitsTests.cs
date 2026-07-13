using System;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class ImageReaderLimitsTests {
    [Fact]
    public void TryDecodeRgba32_Respects_Global_MaxPixels() {
        var png = QrCode.Render("LIMIT", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var previous = ImageReader.MaxPixels;
        try {
            ImageReader.MaxPixels = 1;
            Assert.False(ImageReader.TryDecodeRgba32(png, out _, out _, out _));
        } finally {
            ImageReader.MaxPixels = previous;
        }
    }

    [Fact]
    public void TryDecodeRgba32_Respects_Option_MaxBytes() {
        var png = QrCode.Render("LIMIT", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var options = new ImageDecodeOptions { MaxBytes = png.Length - 1 };
        Assert.False(ImageReader.TryDecodeRgba32(png, options, out _, out _, out _));
    }

    [Fact]
    public void DecodeRgba32_Resizes_Output_To_MaxDimension() {
        var png = QrCode.Render("RESIZE", OutputFormat.Png, new QrEasyOptions { ModuleSize = 12, QuietZone = 4 }).Data;
        var options = new ImageDecodeOptions { MaxDimension = 32 };

        var rgba = ImageReader.DecodeRgba32(png, options, out var width, out var height);

        Assert.True(width <= 32);
        Assert.True(height <= 32);
        Assert.Equal(width * height * 4, rgba.Length);
    }

    [Fact]
    public void Option_Zero_Disables_Global_MaxPixels() {
        var png = QrCode.Render("UNLIMITED", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var previous = ImageReader.MaxPixels;
        try {
            ImageReader.MaxPixels = 1;
            var options = new ImageDecodeOptions { MaxPixels = 0 };
            Assert.True(ImageReader.TryDecodeRgba32(png, options, out _, out _, out _));
        } finally {
            ImageReader.MaxPixels = previous;
        }
    }

    [Fact]
    public void DecodeResult_Option_Zero_Disables_Global_MaxBytes() {
        var png = QrCode.Render("UNLIMITED-BYTES", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var previous = ImageReader.MaxImageBytes;
        try {
            ImageReader.MaxImageBytes = 1;
            var options = new CodeGlyphDecodeOptions {
                Image = new ImageDecodeOptions { MaxBytes = 0 },
                Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast, BudgetMilliseconds = 1000 }
            };

            var result = CodeGlyph.DecodeImageResult(png, options);

            Assert.True(result.IsSuccess, result.Message);
            Assert.Equal("UNLIMITED-BYTES", result.Value?.Text);
        } finally {
            ImageReader.MaxImageBytes = previous;
        }
    }

    [Fact]
    public void BarcodePng_Option_Zero_Disables_Global_MaxBytes() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "UNLIMITED-BARCODE");
        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 6,
            QuietZone = 10,
            HeightModules = 40
        });
        var previous = ImageReader.MaxImageBytes;
        try {
            ImageReader.MaxImageBytes = 1;

            var success = Barcode.TryDecodePng(
                png,
                BarcodeType.Code128,
                new ImageDecodeOptions { MaxBytes = 0 },
                out var decoded);

            Assert.True(success);
            Assert.Equal("UNLIMITED-BARCODE", decoded.Text);
        } finally {
            ImageReader.MaxImageBytes = previous;
        }
    }

    [Fact]
    public void LimitViolation_Fires_OnMaxPixels() {
        var png = QrCode.Render("LIMIT", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var options = new ImageDecodeOptions { MaxPixels = 1 };
        ImageDecodeLimitViolation? violation = null;
        void Handler(ImageDecodeLimitViolation v) => violation = v;

        ImageReader.LimitViolation += Handler;
        try {
            Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(png, options, out _, out _));
        } finally {
            ImageReader.LimitViolation -= Handler;
        }

        Assert.True(violation.HasValue);
        Assert.Equal(ImageDecodeLimitKind.MaxPixels, violation!.Value.Kind);
    }
}

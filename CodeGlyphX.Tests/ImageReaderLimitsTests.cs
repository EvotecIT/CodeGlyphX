using System;
using System.IO;
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BarcodeTryDecodePng_ReturnsFalse_WhenOptionLimitRejectsImage(bool limitBytes) {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "BARCODE-LIMIT");
        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 6,
            QuietZone = 10,
            HeightModules = 40
        });
        var options = limitBytes
            ? new ImageDecodeOptions { MaxBytes = png.Length - 1 }
            : new ImageDecodeOptions { MaxPixels = 1 };

        var success = Barcode.TryDecodePng(png, BarcodeType.Code128, options, out var decoded);

        Assert.False(success);
        Assert.Null(decoded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CodeGlyphPngTryApis_ReturnFalse_WhenOptionLimitRejectsImage(bool limitBytes) {
        var png = QrCode.Render("CODEGLYPH-LIMIT", OutputFormat.Png, new QrEasyOptions { ModuleSize = 6, QuietZone = 2 }).Data;
        var options = new CodeGlyphDecodeOptions {
            Image = limitBytes
                ? new ImageDecodeOptions { MaxBytes = png.Length - 1 }
                : new ImageDecodeOptions { MaxPixels = 1 }
        };
        var path = Path.GetTempFileName();

        try {
            File.WriteAllBytes(path, png);

            Assert.False(CodeGlyph.TryDecodePng(png, out var direct, options));
            Assert.Null(direct);

            Assert.False(CodeGlyph.TryDecodePng(png, out var diagnosed, out var diagnostics, options));
            Assert.Null(diagnosed);
            Assert.Equal(DecodeFailureReason.InvalidInput, diagnostics.FailureReason);

            Assert.False(CodeGlyph.TryDecodeAllPng(png, out var allDirect, options));
            Assert.Empty(allDirect);

            using (var stream = new MemoryStream(png, writable: false)) {
                Assert.False(CodeGlyph.TryDecodePng(stream, out var streamed, options));
                Assert.Null(streamed);
            }

            using (var stream = new MemoryStream(png, writable: false)) {
                Assert.False(CodeGlyph.TryDecodeAllPng(stream, out var allStreamed, options));
                Assert.Empty(allStreamed);
            }

            Assert.False(CodeGlyph.TryDecodePngFile(path, out var fromFile, options));
            Assert.Null(fromFile);
            Assert.False(CodeGlyph.TryDecodeAllPngFile(path, out var allFromFile, options));
            Assert.Empty(allFromFile);
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void CodeGlyphPngTryApis_ReturnFalse_ForInvalidPng() {
        var invalidPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        Assert.False(CodeGlyph.TryDecodePng(invalidPng, out var decoded));
        Assert.Null(decoded);
        Assert.False(CodeGlyph.TryDecodeAllPng(invalidPng, out var allDecoded));
        Assert.Empty(allDecoded);
        Assert.False(CodeGlyph.TryDecodePng(invalidPng, out var diagnosed, out var diagnostics, options: null));
        Assert.Null(diagnosed);
        Assert.Equal(DecodeFailureReason.InvalidInput, diagnostics.FailureReason);
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

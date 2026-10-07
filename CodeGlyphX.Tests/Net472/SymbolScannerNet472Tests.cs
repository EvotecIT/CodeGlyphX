using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering;
using System.IO;
using Xunit;

namespace CodeGlyphX.Tests.Net472;

public sealed class SymbolScannerNet472Tests {
    [Fact]
    public void Scanner_StreamUsesLegacyQrImageFallbackFromCurrentPosition() {
        var png = QR.Render("NET472-STREAM", OutputFormat.Png,
            new QrRenderOptions { ModuleSize = 14, QuietZone = 6 }).Data.ToArray();
        var prefixed = new byte[png.Length + 3];
        System.Array.Copy(png, 0, prefixed, 3, png.Length);
        using var stream = new MemoryStream(prefixed);
        stream.Position = 3;
        var result = SymbolScanner.Scan(stream, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode }, MaxSymbols = 1, TimeoutMilliseconds = 5000
        });
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.QrCode, symbol.Format);
        Assert.Equal("NET472-STREAM", symbol.Text);
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void Scanner_DecodesQrAndExposesCapabilityRegistry() {
        var pixels = QR.RenderPixels("NET472-SCANNER", out var width, out var height, out _);
        var frame = ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32);

        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            TimeoutMilliseconds = 5000
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal("NET472-SCANNER", Assert.Single(result.Symbols).Text);
        Assert.True(SymbolCapabilities.Get(SymbolFormat.QrCode).CanScanImages);
        Assert.True(SymbolCapabilities.Get(SymbolFormat.MicroQrCode).CanScanImages);
    }

    [Fact]
    public void Scanner_DecodesMicroQrPixelsAndReportsGeometry() {
        var micro = MicroQrCodeEncoder.EncodeAlphanumeric("NET472", minVersion: 4, maxVersion: 4);
        var pixels = MatrixPngRenderer.RenderPixels(
            micro.Modules,
            new MatrixPngRenderOptions { ModuleSize = 8, QuietZone = 2 },
            out var width,
            out var height,
            out _);

        var result = SymbolScanner.Scan(
            ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
            new ScanOptions {
                Formats = new[] { SymbolFormat.MicroQrCode },
                TimeoutMilliseconds = 5000
            });

        Assert.Equal(ScanStatus.Success, result.Status);
        var detected = Assert.Single(result.Symbols);
        Assert.Equal("NET472", detected.Text);
        Assert.IsType<MicroQrSymbolMetadata>(detected.Metadata);
        Assert.NotNull(detected.Geometry);
    }
}

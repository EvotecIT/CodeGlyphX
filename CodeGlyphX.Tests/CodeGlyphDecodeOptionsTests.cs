using CodeGlyphX.Rendering;
using System;
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

        var options = new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode }, MaxSymbols = 1,
            Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast }
        };

        var result = SymbolScanner.Scan(new ImageFrame(rgba, width, height, stride, PixelFormat.Rgba32), options);
        Assert.True(result.IsSuccess, result.Failure);
        var decoded = Assert.Single(result.Symbols);
        Assert.Equal("OPTIONS", decoded.Text);
    }

    [Fact]
    public void Decode_RespectsCancellationToken() {
        var qr = QrCodeEncoder.EncodeText("CANCEL");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 4, QuietZone = 4 });
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var options = new ScanOptions {
            Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust },
            CancellationToken = cts.Token
        };

        Assert.Equal(ScanStatus.Cancelled, SymbolScanner.Scan(new ImageFrame(rgba, width, height, stride, PixelFormat.Rgba32), options).Status);
    }

    [Fact]
    public void Decode_UsesBarcodeOptions() {
        var barcode = BarcodeEncoder.EncodeCode39("ABC123", includeChecksum: true, fullAsciiMode: false);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        var options = new ScanOptions {
            Formats = new[] { SymbolFormat.Code39 },
            Barcode = new BarcodeDecodeOptions { Code39Checksum = Code39ChecksumPolicy.StripIfValid },
        };

        var result = SymbolScanner.Scan(new ImageFrame(pixels, width, height, stride, PixelFormat.Rgba32), options);
        Assert.True(result.IsSuccess, result.Failure);
        var decoded = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Code39, decoded.Format);
        Assert.Equal("ABC123", decoded.Text);
    }

    [Fact]
    public void Scanner_TotalDeadline_AllowsEveryBarcode() {
        var top = RenderBarcode("BUDGET-TOP", out var topWidth, out var topHeight, out var topStride);
        var bottom = RenderBarcode("BUDGET-BOTTOM", out var bottomWidth, out var bottomHeight, out var bottomStride);
        var pixels = StackVertically(
            top,
            topWidth,
            topHeight,
            topStride,
            bottom,
            bottomWidth,
            bottomHeight,
            bottomStride,
            out var width,
            out var height,
            out var stride);
        var options = new ScanOptions {
            Formats = new[] { SymbolFormat.Code128 },
            TimeoutMilliseconds = TestBudget.Adjust(4000)
        };

        var result = SymbolScanner.Scan(new ImageFrame(pixels, width, height, stride, PixelFormat.Rgba32), options);
        Assert.True(result.IsSuccess, result.Failure);
        var decoded = result.Symbols;
        Assert.Contains(decoded, item => item.Format == SymbolFormat.Code128 && item.Text == "BUDGET-TOP");
        Assert.Contains(decoded, item => item.Format == SymbolFormat.Code128 && item.Text == "BUDGET-BOTTOM");
    }

    private static byte[] RenderBarcode(string text, out int width, out int height, out int stride) {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, text);
        return BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out width, out height, out stride);
    }

    private static byte[] StackVertically(
        byte[] top,
        int topWidth,
        int topHeight,
        int topStride,
        byte[] bottom,
        int bottomWidth,
        int bottomHeight,
        int bottomStride,
        out int width,
        out int height,
        out int stride) {
        const int gap = 24;
        width = Math.Max(topWidth, bottomWidth);
        height = topHeight + gap + bottomHeight;
        stride = width * 4;
        var pixels = new byte[stride * height];
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = 255;
            pixels[i + 1] = 255;
            pixels[i + 2] = 255;
            pixels[i + 3] = 255;
        }

        BlitRows(top, topHeight, topStride, pixels, stride, offsetY: 0);
        BlitRows(bottom, bottomHeight, bottomStride, pixels, stride, offsetY: topHeight + gap);
        return pixels;
    }

    private static void BlitRows(byte[] source, int sourceHeight, int sourceStride, byte[] destination, int destinationStride, int offsetY) {
        for (var y = 0; y < sourceHeight; y++) {
            Buffer.BlockCopy(source, y * sourceStride, destination, (offsetY + y) * destinationStride, sourceStride);
        }
    }
}

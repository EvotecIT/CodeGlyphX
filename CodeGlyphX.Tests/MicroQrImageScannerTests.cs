using System;
using System.Diagnostics;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MicroQrImageScannerTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void PixelDecoder_RecognizesEveryMicroQrVersion(int version) {
        var code = MicroQrCodeEncoder.EncodeNumeric("1", QrErrorCorrectionLevel.L, version, version);
        var pixels = RenderPixels(code, moduleSize: 7, quietZone: 2, out var width, out var height);

        Assert.True(MicroQrDecoder.TryDecode(
            pixels, width, height, width * 4, PixelFormat.Rgba32, out var decoded, out var info));

        Assert.Equal(version, decoded.Version);
        Assert.Equal("1", decoded.Text);
        Assert.False(info.IsInverted);
        Assert.False(info.IsMirrored);
        Assert.InRange(info.Geometry.Bounds.Width, code.Size * 6.5, code.Size * 7.5);
        Assert.InRange(info.Geometry.Bounds.Height, code.Size * 6.5, code.Size * 7.5);
    }

    [Fact]
    public void PixelDecoder_RecognizesInvertedAndQuarterTurnedSymbols() {
        var code = MicroQrCodeEncoder.EncodeAlphanumeric("MICRO-QR", minVersion: 4, maxVersion: 4);
        var pixels = RenderPixels(code, moduleSize: 8, quietZone: 3, out var width, out var height);
        Invert(pixels);
        var rotated = Rotate90(pixels, width, height, out var rotatedWidth, out var rotatedHeight);

        Assert.True(MicroQrDecoder.TryDecode(
            rotated,
            rotatedWidth,
            rotatedHeight,
            rotatedWidth * 4,
            PixelFormat.Rgba32,
            out var decoded,
            out var info));

        Assert.Equal("MICRO-QR", decoded.Text);
        Assert.True(info.IsInverted);
        Assert.InRange(info.Geometry.RotationDegrees, 89, 91);
    }

    [Fact]
    public void PixelDecoder_RecognizesArbitraryRotationWithoutDeskewingTheSource() {
        var code = MicroQrCodeEncoder.EncodeAlphanumeric("ROTATED", minVersion: 4, maxVersion: 4);
        var pixels = RenderPixels(code, moduleSize: 9, quietZone: 3, out var width, out var height);
        var rotated = Rotate(pixels, width, height, 17, padding: 30, out var rotatedWidth, out var rotatedHeight);

        Assert.True(MicroQrDecoder.TryDecode(
            rotated,
            rotatedWidth,
            rotatedHeight,
            rotatedWidth * 4,
            PixelFormat.Rgba32,
            out var decoded,
            out var info));

        Assert.Equal("ROTATED", decoded.Text);
        Assert.InRange(info.Geometry.RotationDegrees, 15, 19);
    }

    [Fact]
    public void PixelDecoder_RecognizesMirroredBgraPixels() {
        var code = MicroQrCodeEncoder.EncodeAlphanumeric("MIRRORED", minVersion: 4, maxVersion: 4);
        var pixels = MatrixPngRenderer.RenderPixels(
            code.Modules,
            new MatrixPngRenderOptions {
                ModuleSize = 8,
                QuietZone = 2,
                Foreground = new Rgba32(12, 35, 92, 255),
                Background = new Rgba32(244, 232, 210, 255)
            },
            out var width,
            out var height,
            out _);
        ConvertRgbaToBgra(pixels);
        var mirrored = MirrorX(pixels, width, height);

        Assert.True(MicroQrDecoder.TryDecode(
            mirrored, width, height, width * 4, PixelFormat.Bgra32, out var decoded, out var info));

        Assert.Equal("MIRRORED", decoded.Text);
        Assert.True(info.IsMirrored);
    }

    [Fact]
    public void UnifiedScanner_FindsEmbeddedMicroQrAndReturnsGlobalGeometryAndBytes() {
        var code = MicroQrCodeEncoder.EncodeBytes(new byte[] { 65, 66, 67, 49, 50, 51 }, minVersion: 4, maxVersion: 4);
        var symbol = RenderPixels(code, moduleSize: 6, quietZone: 2, out var symbolWidth, out var symbolHeight);
        var canvas = CreateWhiteCanvas(280, 190);
        Blit(symbol, symbolWidth, symbolHeight, canvas, 280, 190, 43, 27);
        FillRectangle(canvas, 280, 190, 240, 145, 12, 12, 0);

        var result = SymbolScanner.Scan(
            ImageFrame.Packed(canvas, 280, 190, PixelFormat.Rgba32),
            new ScanOptions {
                Formats = new[] { SymbolFormat.MicroQrCode },
                Region = new ImageRegion(35, 20, 230, 160),
                TimeoutMilliseconds = 2500
            });

        Assert.True(result.IsSuccess, result.Failure);
        var detected = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.MicroQrCode, detected.Format);
        Assert.Equal("ABC123", detected.Text);
        Assert.Equal(CodeGlyphKind.MicroQr, detected.LegacyResult.Kind);
        Assert.NotNull(detected.LegacyResult.MicroQr);
        Assert.True(detected.HasRawBytes);
        Assert.Equal(new byte[] { 65, 66, 67, 49, 50, 51 }, detected.RawBytes.ToArray());
        Assert.NotNull(detected.Geometry);
        Assert.InRange(detected.Geometry!.Bounds.X, 54, 58);
        Assert.InRange(detected.Geometry.Bounds.Y, 38, 42);
    }

    [Fact]
    public void EncodedImageFacadeAndUnifiedScanner_DecodePng() {
        var code = MicroQrCodeEncoder.EncodeAlphanumeric("PNG-MICRO", minVersion: 4, maxVersion: 4);
        var png = MatrixPngRenderer.Render(code.Modules, new MatrixPngRenderOptions { ModuleSize = 7, QuietZone = 2 });

        Assert.True(MicroQrDecoder.TryDecodeImage(png, out var decoded, out var info));
        Assert.Equal("PNG-MICRO", decoded.Text);
        Assert.NotNull(info.Geometry);

        var result = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.MicroQrCode } });
        Assert.True(result.IsSuccess, result.Failure);
        Assert.Equal("PNG-MICRO", Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void Decoder_RecognizesIndependentBwippImage() {
        // Generated by the BWIPP-backed bwip-js API using:
        // bcid=microqrcode&text=ORACLE42&scale=3&padding=6
        // SHA-256: ACEDD95C39413D20E70EE68BDD99F36A383668D46AF890BEEF136577B3883332
        const string pngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAH4AAAB+CAYAAADiI6WIAAAAHnRFWHRTb2Z0d2FyZQBid2lwLWpzLm1ldGFmbG9vci5jb21Tnbi0AAACDUlEQVR4nO3XQU4EMQxFQe5/adiwRahbdvw9qZJmFxHHrzd8fQEAAAAAAAAAABm+l/yezr/l/JjpoMIPmQ4q/JDpoMIPmQ4q/JDpoMIPSRu0atFVH1b3/GPSBhX+kLRBhT8kbVDhD0kbVPhD0gYV/pC0Raedr9pDHOGFF/4B4YUXXnjhhRd+JmR3mDWB/5K26LR5quaMk7botHmq5oyTtui0earmjJO26LR5quaMk7botHmq5oyTtuipMOtDPiV87b1rCF977xrC1967hvC1964hfO29a6Q9uHueLR9uu7RBhT8kbVDhD0kbVPhD0gYV/pC0QYU/pGoR3b+qd/FrOqjwQ6aDCj9kOqjwQ6aDCj9kOqjwlHoavvs8hwh/KeEvJfylhL+U8P/ofkD3v2dVwbrnjCO88MILn/f3hS8mvPDC3xT+L90L3RJgav4xwte+aw3ha9+1hvC171pD+Np3rSF87bviVAWemqf7XuFfnu+ep/te4V+e756n+17hX57vnqf7XuFfnu+ep/te4V+e756n+96PDf+pqgIIvIzwlxL+UsJfSvhLCf+Ppw+YOl8VIO38mLRFCH9I2iKEPyRtEcIfkrYI4Q9JW4Tww7of0B2y+wN6Os8awgsvfMG71hBeeOEL3rWG8B8evntx3Qvd/qGMEV544YXPW5DwxYQXXvibwgMAAAAAAAAAwH1+AE+g1P5xcUsqAAAAAElFTkSuQmCC";
        var png = Convert.FromBase64String(pngBase64);

        Assert.True(MicroQrDecoder.TryDecodeImage(png, out var decoded, out _));
        Assert.Equal("ORACLE42", decoded.Text);

        var scan = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.MicroQrCode } });
        Assert.True(scan.IsSuccess, scan.Failure);
        Assert.Equal("ORACLE42", Assert.Single(scan.Symbols).Text);
    }

    [Fact]
    public void Decoder_HonorsCancellationAndRejectsBlankFrames() {
        var code = MicroQrCodeEncoder.EncodeNumeric("1234");
        var pixels = RenderPixels(code, moduleSize: 8, quietZone: 2, out var width, out var height);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.False(MicroQrDecoder.TryDecode(
            pixels,
            width,
            height,
            width * 4,
            PixelFormat.Rgba32,
            cancellation.Token,
            out _));

        var blank = CreateWhiteCanvas(120, 90);
        Assert.False(MicroQrDecoder.TryDecode(blank, 120, 90, 120 * 4, PixelFormat.Rgba32, out _));
    }

    [Fact]
    public void Decoder_RejectsModel2QrWithoutExhaustiveMicroQrSearch() {
        var png = QrCode.Render("MODEL2-NOT-MICRO", OutputFormat.Png).Data;
        var stopwatch = Stopwatch.StartNew();

        Assert.False(MicroQrDecoder.TryDecodeImage(png, out _, out _));

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Model 2 rejection took {stopwatch.Elapsed}.");
    }

    [Fact]
    public void CapabilityCatalog_AdvertisesSingleResultImageScanningWithGeometry() {
        var capability = SymbolCapabilities.Get(SymbolFormat.MicroQrCode);

        Assert.True(capability.CanEncode);
        Assert.True(capability.CanDecodeModules);
        Assert.True(capability.CanScanImages);
        Assert.False(capability.CanScanMultiple);
        Assert.True(capability.ReportsGeometry);
        Assert.Contains(SymbolFormat.MicroQrCode, SymbolCapabilities.ImageScannableFormats);
    }

    private static byte[] RenderPixels(MicroQrCode code, int moduleSize, int quietZone, out int width, out int height) {
        return MatrixPngRenderer.RenderPixels(
            code.Modules,
            new MatrixPngRenderOptions { ModuleSize = moduleSize, QuietZone = quietZone },
            out width,
            out height,
            out _);
    }

    private static byte[] CreateWhiteCanvas(int width, int height) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = 255;
            pixels[i + 1] = 255;
            pixels[i + 2] = 255;
            pixels[i + 3] = 255;
        }
        return pixels;
    }

    private static void Invert(byte[] pixels) {
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = (byte)(255 - pixels[i]);
            pixels[i + 1] = (byte)(255 - pixels[i + 1]);
            pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }
    }

    private static byte[] Rotate90(byte[] source, int width, int height, out int rotatedWidth, out int rotatedHeight) {
        rotatedWidth = height;
        rotatedHeight = width;
        var result = new byte[rotatedWidth * rotatedHeight * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                CopyPixel(source, width, x, y, result, rotatedWidth, height - 1 - y, x);
            }
        }
        return result;
    }

    private static byte[] MirrorX(byte[] source, int width, int height) {
        var result = new byte[source.Length];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                CopyPixel(source, width, x, y, result, width, width - 1 - x, y);
            }
        }
        return result;
    }

    private static void ConvertRgbaToBgra(byte[] pixels) {
        for (var i = 0; i < pixels.Length; i += 4) {
            var red = pixels[i];
            pixels[i] = pixels[i + 2];
            pixels[i + 2] = red;
        }
    }

    private static byte[] Rotate(byte[] source, int width, int height, double angleDegrees, int padding, out int targetWidth, out int targetHeight) {
        var radians = angleDegrees * Math.PI / 180.0;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        targetWidth = (int)Math.Ceiling(Math.Abs(width * cosine) + Math.Abs(height * sine)) + padding * 2;
        targetHeight = (int)Math.Ceiling(Math.Abs(width * sine) + Math.Abs(height * cosine)) + padding * 2;
        var result = CreateWhiteCanvas(targetWidth, targetHeight);
        var sourceCenterX = (width - 1) * 0.5;
        var sourceCenterY = (height - 1) * 0.5;
        var targetCenterX = (targetWidth - 1) * 0.5;
        var targetCenterY = (targetHeight - 1) * 0.5;

        for (var y = 0; y < targetHeight; y++) {
            for (var x = 0; x < targetWidth; x++) {
                var dx = x - targetCenterX;
                var dy = y - targetCenterY;
                var sourceX = (int)Math.Round(sourceCenterX + dx * cosine + dy * sine);
                var sourceY = (int)Math.Round(sourceCenterY - dx * sine + dy * cosine);
                if ((uint)sourceX >= (uint)width || (uint)sourceY >= (uint)height) continue;
                CopyPixel(source, width, sourceX, sourceY, result, targetWidth, x, y);
            }
        }
        return result;
    }

    private static void Blit(byte[] source, int sourceWidth, int sourceHeight, byte[] target, int targetWidth, int targetHeight, int left, int top) {
        for (var y = 0; y < sourceHeight; y++) {
            if (top + y < 0 || top + y >= targetHeight) continue;
            for (var x = 0; x < sourceWidth; x++) {
                if (left + x < 0 || left + x >= targetWidth) continue;
                CopyPixel(source, sourceWidth, x, y, target, targetWidth, left + x, top + y);
            }
        }
    }

    private static void FillRectangle(byte[] pixels, int width, int height, int left, int top, int rectangleWidth, int rectangleHeight, byte value) {
        for (var y = Math.Max(0, top); y < Math.Min(height, top + rectangleHeight); y++) {
            for (var x = Math.Max(0, left); x < Math.Min(width, left + rectangleWidth); x++) {
                var offset = (y * width + x) * 4;
                pixels[offset] = value;
                pixels[offset + 1] = value;
                pixels[offset + 2] = value;
                pixels[offset + 3] = 255;
            }
        }
    }

    private static void CopyPixel(byte[] source, int sourceWidth, int sourceX, int sourceY, byte[] target, int targetWidth, int targetX, int targetY) {
        var sourceOffset = (sourceY * sourceWidth + sourceX) * 4;
        var targetOffset = (targetY * targetWidth + targetX) * 4;
        Buffer.BlockCopy(source, sourceOffset, target, targetOffset, 4);
    }
}

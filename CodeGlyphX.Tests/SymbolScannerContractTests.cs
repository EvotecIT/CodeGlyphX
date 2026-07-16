using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SymbolScannerContractTests {
    [Fact]
    public void CapabilityCatalog_CoversEveryPublicFormatAndLegacyBarcodeType() {
        var formats = Enum.GetValues<SymbolFormat>();
        var legacyTypes = Enum.GetValues<BarcodeType>();

        Assert.Equal(formats.Length, SymbolCapabilities.All.Count);
        Assert.Equal(formats.OrderBy(value => value), SymbolCapabilities.All.Select(item => item.Format).OrderBy(value => value));
        Assert.Equal(legacyTypes.OrderBy(value => value),
            SymbolCapabilities.All.Where(item => item.LegacyBarcodeType.HasValue).Select(item => item.LegacyBarcodeType!.Value).OrderBy(value => value));
        Assert.All(SymbolCapabilities.ImageScannableFormats, format => Assert.True(SymbolCapabilities.Get(format).CanScanImages));
    }

    [Fact]
    public void CapabilityCatalog_AdvertisesMicroQrImageSupportAndDistinguishesModuleOnlyFormats() {
        var qr = SymbolCapabilities.Get(SymbolFormat.QrCode);
        var microQr = SymbolCapabilities.Get(SymbolFormat.MicroQrCode);
        var dataMatrix = SymbolCapabilities.Get(SymbolFormat.DataMatrix);
        var microPdf417 = SymbolCapabilities.Get(SymbolFormat.MicroPdf417);

        Assert.True(qr.CanEncode);
        Assert.True(qr.CanDecodeModules);
        Assert.True(qr.CanScanImages);
        Assert.True(qr.CanScanMultiple);
        Assert.True(qr.Has(SymbolCapabilityFlags.EciEncode));
        Assert.True(qr.Has(SymbolCapabilityFlags.StructuredAppendDecode));
        Assert.False(qr.Has(SymbolCapabilityFlags.StructuredAppendEncode));

        Assert.True(microQr.CanEncode);
        Assert.True(microQr.CanDecodeModules);
        Assert.True(microQr.CanScanImages);
        Assert.True(microQr.ReportsGeometry);
        Assert.True(dataMatrix.CanScanImages);
        Assert.False(dataMatrix.CanScanMultiple);
        Assert.False(microPdf417.CanScanImages);
    }

    [Fact]
    public void ImageFrame_ValidatesPackedBufferContract() {
        Assert.Equal(12, ImageFrame.GetMinimumRowBytes(4, PixelFormat.Rgb24));
        Assert.Equal(8, ImageFrame.GetMinimumRowBytes(4, PixelFormat.Gray16LittleEndian));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFrame(new byte[12], 4, 1, 11, PixelFormat.Rgb24));
        Assert.Throws<ArgumentException>(() => new ImageFrame(new byte[11], 4, 1, 12, PixelFormat.Rgb24));
    }

    [Fact]
    public void Geometry_ComputesBoundsAndClockwiseRotation() {
        var geometry = new SymbolGeometry(
            new SymbolPoint(10, 20),
            new SymbolPoint(20, 30),
            new SymbolPoint(10, 40),
            new SymbolPoint(0, 30));

        Assert.Equal(new SymbolBounds(0, 20, 20, 20), geometry.Bounds);
        Assert.Equal(45d, geometry.RotationDegrees, precision: 8);
    }

    [Fact]
    public void ScanOptions_ScreenCoordinatesTotalRecognitionAndImageBudgets() {
        var options = ScanOptions.Screen(timeoutMilliseconds: 420, maxDimension: 1024);

        Assert.Equal(ScanProfile.Screen, options.Profile);
        Assert.Equal(420, options.TimeoutMilliseconds);
        Assert.NotNull(options.Qr);
        Assert.Equal(420, options.Qr!.BudgetMilliseconds);
        Assert.Equal(1024, options.Qr.MaxDimension);
        Assert.NotNull(options.Image);
        Assert.Equal(420, options.Image!.RecognitionBudgetMilliseconds);
        Assert.Equal(1024, options.Image.MaxDimension);
    }

    [Fact]
    public void Scan_DecodesQrFromGray8StrideAndRegionOfInterest() {
        var qr = QrEasy.RenderPixels("GRAY-ROI", out var qrWidth, out var qrHeight, out var qrStride);
        const int offsetX = 11;
        const int offsetY = 13;
        var width = qrWidth + offsetX + 17;
        var height = qrHeight + offsetY + 19;
        var stride = width + 7;
        var gray = Enumerable.Repeat((byte)255, stride * height).ToArray();
        for (var y = 0; y < qrHeight; y++) {
            for (var x = 0; x < qrWidth; x++) {
                gray[(offsetY + y) * stride + offsetX + x] = qr[y * qrStride + x * 4];
            }
        }

        var frame = new ImageFrame(gray, width, height, stride, PixelFormat.Gray8);
        var region = new ImageRegion(offsetX, offsetY, qrWidth, qrHeight);
        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            Region = region,
            Profile = ScanProfile.Robust,
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.QrCode, symbol.Format);
        Assert.Equal("GRAY-ROI", symbol.Text);
        Assert.True(symbol.HasRawBytes);
        Assert.NotEmpty(symbol.RawBytes.ToArray());
        Assert.Equal(region, symbol.SearchRegion);
        Assert.Equal(CodeGlyphKind.Qr, symbol.LegacyResult.Kind);
    }

    [Theory]
    [InlineData(PixelFormat.Rgba32)]
    [InlineData(PixelFormat.Bgra32)]
    [InlineData(PixelFormat.Rgb24)]
    [InlineData(PixelFormat.Argb32)]
    [InlineData(PixelFormat.Abgr32)]
    [InlineData(PixelFormat.Gray16LittleEndian)]
    [InlineData(PixelFormat.Rgb565LittleEndian)]
    public void Scan_DecodesQrFromEveryAdditionalPackedPixelFormat(PixelFormat format) {
        var rgba = QrEasy.RenderPixels("PACKED-FORMAT", out var width, out var height, out var rgbaStride);
        var converted = ConvertRgba(rgba, width, height, rgbaStride, format, out var stride);
        var frame = new ImageFrame(converted, width, height, stride, format);

        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            Profile = ScanProfile.Fast,
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal("PACKED-FORMAT", Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void Scan_DecodesCode128FromBottomUpBgr24() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "BOTTOM-UP-BGR");
        var rgba = Rendering.Png.BarcodePngRenderer.RenderPixels(barcode, new Rendering.Png.BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var rgbaStride);
        var stride = width * 3 + 5;
        var bgr = new byte[stride * height];
        for (var y = 0; y < height; y++) {
            var destinationY = height - 1 - y;
            for (var x = 0; x < width; x++) {
                var source = y * rgbaStride + x * 4;
                var destination = destinationY * stride + x * 3;
                bgr[destination] = rgba[source + 2];
                bgr[destination + 1] = rgba[source + 1];
                bgr[destination + 2] = rgba[source];
            }
        }

        var frame = new ImageFrame(bgr, width, height, stride, PixelFormat.Bgr24, ImageRowOrder.BottomUp);
        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.Code128 },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Code128, symbol.Format);
        Assert.Equal("BOTTOM-UP-BGR", symbol.Text);
    }

    [Theory]
    [InlineData(SymbolFormat.DataMatrix, "SCANNER-DATA-MATRIX")]
    [InlineData(SymbolFormat.Aztec, "SCANNER-AZTEC")]
    [InlineData(SymbolFormat.Pdf417, "SCANNER-PDF417")]
    public void Scan_DecodesEverySupportedMatrixRouteFromGeneratedPixels(SymbolFormat format, string payload) {
        BitMatrix matrix;
        switch (format) {
            case SymbolFormat.DataMatrix:
                matrix = DataMatrix.DataMatrixEncoder.Encode(payload);
                break;
            case SymbolFormat.Aztec:
                matrix = AztecCode.Encode(payload);
                break;
            case SymbolFormat.Pdf417:
                matrix = Pdf417.Pdf417Encoder.Encode(payload);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }

        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 3 },
            out var width,
            out var height,
            out _);

        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { format },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(format, symbol.Format);
        Assert.Equal(payload, symbol.Text);
    }

    [Fact]
    public void Scan_ReturnsMultipleQrCodesFromOneFrame() {
        var left = QrEasy.RenderPixels("SCANNER-LEFT", out var leftWidth, out var leftHeight, out var leftStride);
        var right = QrEasy.RenderPixels("SCANNER-RIGHT", out var rightWidth, out var rightHeight, out var rightStride);
        const int padding = 16;
        var cellWidth = Math.Max(leftWidth, rightWidth);
        var cellHeight = Math.Max(leftHeight, rightHeight);
        var width = cellWidth * 2 + padding * 3;
        var height = cellHeight * 2 + padding * 3;
        var stride = width * 4;
        var canvas = CreateWhiteRgba(width, height);
        Blit(left, leftWidth, leftHeight, leftStride, canvas, stride, padding, padding);
        Blit(right, rightWidth, rightHeight, rightStride, canvas, stride, cellWidth + padding * 2, cellHeight + padding * 2);

        var result = SymbolScanner.Scan(ImageFrame.Packed(canvas, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            Qr = new QrPixelDecodeOptions {
                Profile = QrDecodeProfile.Fast,
                MaxScale = 1,
                DisableTransforms = true,
                EnableTileScan = true,
                TileGrid = 2
            }
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.False(result.IsPartial);
        Assert.Contains(result.Symbols, symbol => symbol.Text == "SCANNER-LEFT");
        Assert.Contains(result.Symbols, symbol => symbol.Text == "SCANNER-RIGHT");
    }

    [Fact]
    public void Scan_ReportsModuleOnlyFormatsWithoutHidingSupportedResults() {
        var png = QrCode.Render("SUPPORTED-PLUS-UNSUPPORTED", OutputFormat.Png).Data;
        var result = SymbolScanner.Scan(png, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode, SymbolFormat.MicroPdf417 },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal("SUPPORTED-PLUS-UNSUPPORTED", Assert.Single(result.Symbols).Text);
        Assert.Equal(SymbolFormat.MicroPdf417, Assert.Single(result.UnsupportedFormats));
    }

    [Fact]
    public void Scan_ReturnsUnsupportedWhenNoRequestedFormatCanScanImages() {
        var frame = ImageFrame.Packed(new byte[16], 2, 2, PixelFormat.Rgba32);
        var result = SymbolScanner.Scan(frame, new ScanOptions { Formats = new[] { SymbolFormat.MicroPdf417 } });

        Assert.Equal(ScanStatus.UnsupportedFormats, result.Status);
        Assert.Empty(result.Symbols);
        Assert.Equal(SymbolFormat.MicroPdf417, Assert.Single(result.UnsupportedFormats));
    }

    [Fact]
    public void Scan_RespectsCallerCancellationBeforeWorkStarts() {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var frame = ImageFrame.Packed(new byte[16], 2, 2, PixelFormat.Rgba32);

        var result = SymbolScanner.Scan(frame, new ScanOptions { CancellationToken = source.Token });

        Assert.Equal(ScanStatus.Cancelled, result.Status);
        Assert.Empty(result.Symbols);
    }

    [Fact]
    public void Scan_EnforcesTotalDeadlineAcrossFrameConversion() {
        const int width = 2048;
        const int height = 2048;
        var frame = ImageFrame.Packed(new byte[width * height], width, height, PixelFormat.Gray8);

        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            TimeoutMilliseconds = 1
        });

        Assert.Equal(ScanStatus.DeadlineExceeded, result.Status);
        Assert.Empty(result.Symbols);
        Assert.True(result.Elapsed >= TimeSpan.FromMilliseconds(1));
    }

    private static byte[] CreateWhiteRgba(int width, int height) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = 255;
            pixels[i + 1] = 255;
            pixels[i + 2] = 255;
            pixels[i + 3] = 255;
        }
        return pixels;
    }

    private static byte[] ConvertRgba(byte[] source, int width, int height, int sourceStride, PixelFormat format, out int stride) {
        stride = ImageFrame.GetMinimumRowBytes(width, format);
        var result = new byte[stride * height];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var sourceOffset = y * sourceStride + x * 4;
                var red = source[sourceOffset];
                var green = source[sourceOffset + 1];
                var blue = source[sourceOffset + 2];
                var alpha = source[sourceOffset + 3];
                var target = y * stride + x * ImageFrame.GetBytesPerPixel(format);
                switch (format) {
                    case PixelFormat.Rgba32:
                        result[target] = red;
                        result[target + 1] = green;
                        result[target + 2] = blue;
                        result[target + 3] = alpha;
                        break;
                    case PixelFormat.Bgra32:
                        result[target] = blue;
                        result[target + 1] = green;
                        result[target + 2] = red;
                        result[target + 3] = alpha;
                        break;
                    case PixelFormat.Rgb24:
                        result[target] = red;
                        result[target + 1] = green;
                        result[target + 2] = blue;
                        break;
                    case PixelFormat.Argb32:
                        result[target] = alpha;
                        result[target + 1] = red;
                        result[target + 2] = green;
                        result[target + 3] = blue;
                        break;
                    case PixelFormat.Abgr32:
                        result[target] = alpha;
                        result[target + 1] = blue;
                        result[target + 2] = green;
                        result[target + 3] = red;
                        break;
                    case PixelFormat.Gray16LittleEndian: {
                        var gray = (ushort)((red * 77 + green * 150 + blue * 29) * 257 / 256);
                        result[target] = (byte)gray;
                        result[target + 1] = (byte)(gray >> 8);
                        break;
                    }
                    case PixelFormat.Rgb565LittleEndian: {
                        var packed = (ushort)(((red >> 3) << 11) | ((green >> 2) << 5) | (blue >> 3));
                        result[target] = (byte)packed;
                        result[target + 1] = (byte)(packed >> 8);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(format));
                }
            }
        }
        return result;
    }

    private static void Blit(byte[] source, int width, int height, int sourceStride, byte[] destination, int destinationStride, int offsetX, int offsetY) {
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(source, y * sourceStride, destination, (offsetY + y) * destinationStride + offsetX * 4, width * 4);
        }
    }
}

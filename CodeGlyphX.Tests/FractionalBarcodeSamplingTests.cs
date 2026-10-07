using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class FractionalBarcodeSamplingTests {
    [Theory]
    [InlineData(BarcodeType.Code128, "LOT-2026-1042", "LOT-2026-1042", 0.75)]
    [InlineData(BarcodeType.Code128, "LOT-2026-1042", "LOT-2026-1042", 1.25)]
    [InlineData(BarcodeType.Code93, "SHIP-1042", "SHIP-1042", 0.75)]
    [InlineData(BarcodeType.Code39, "SHIP-1042", "SHIP-1042", 0.85)]
    [InlineData(BarcodeType.EAN, "590123412345", "5901234123457", 0.75)]
    [InlineData(BarcodeType.ITF14, "1001234500001", "10012345000017", 1.25)]
    public void Decode_FractionallyResizedPixels_PreservesLinearPayload(
        BarcodeType type, string input, string expected, double scale) {
        var pixels = Resize(BarcodeEncoder.Encode(type, input), scale, out var width, out var height);

        var options = new BarcodeDecodeOptions { Code39Checksum = Code39ChecksumPolicy.RequireValid };
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, type, options, out var decoded));
        Assert.Equal(type, decoded.Type);
        Assert.Equal(expected, decoded.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Decode_FractionallyResizedCode128_PreservesRotationAndPolarity(bool inverted) {
        var pixels = Resize(BarcodeEncoder.Encode(BarcodeType.Code128, "ROTATE-128"), 0.75, out var width, out var height);
        if (inverted) {
            for (var p = 0; p < pixels.Length; p += 4) {
                pixels[p] = pixels[p + 1] = pixels[p + 2] = (byte)(255 - pixels[p]);
            }
        }
        var rotated = new byte[pixels.Length];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            }
        }

        Assert.True(BarcodeDecoder.TryDecode(rotated, height, width, height * 4, PixelFormat.Rgba32, BarcodeType.Code128, out var decoded));
        Assert.Equal("ROTATE-128", decoded.Text);
    }

    [Fact]
    public void Decode_NonuniformNarrowRuns_PreservesPreviouslyDecodablePixels() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "NARROW-128");
        var firstNarrow = true;
        var sampledRuns = barcode.Segments.Select(segment => {
            var width = segment.Modules * 3;
            if (segment.Modules == 1) {
                if (!firstNarrow) width++;
                firstNarrow = false;
            }
            return new BarSegment(segment.IsBar, width);
        });
        // A narrow run can gain one pixel at a threshold boundary without a
        // corresponding change to wider runs. The established sampling accepts it.
        var pixels = BarcodePngRenderer.RenderPixels(new Barcode1D(sampledRuns), new BarcodePngRenderOptions {
            ModuleSize = 1, QuietZone = 30, HeightModules = 90
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, BarcodeType.Code128, out var decoded));
        Assert.Equal("NARROW-128", decoded.Text);
    }

    [Theory]
    [InlineData(0.95)]
    [InlineData(1.25)]
    public void DecodeAll_FractionalPharmacode_SelectsOneCorrectPhysicalSymbol(double scale) {
        var pixels = Resize(BarcodeEncoder.Encode(BarcodeType.Pharmacode, "91"), scale, out var width, out var height);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Pharmacode, out var first));
        Assert.Equal("91", first.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var decoded, BarcodeType.Pharmacode));
        Assert.Equal("91", Assert.Single(decoded).Text);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Pharmacode, null, default, out var diagnosticDecoded, out var diagnostics));
        Assert.Equal("91", diagnosticDecoded.Text);
        Assert.True(diagnostics.Success);
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecode((ReadOnlySpan<byte>)pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Pharmacode, null, default, out var spanFirst, out var spanDiagnostics));
        Assert.Equal("91", spanFirst.Text);
        Assert.True(spanDiagnostics.Success);
        Assert.True(BarcodeDecoder.TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, width * 4, PixelFormat.Rgba32, out var spanDecoded, BarcodeType.Pharmacode));
        Assert.Equal("91", Assert.Single(spanDecoded).Text);
#endif
        var result = SymbolScanner.Scan(EncodePng(pixels, width, height), new ScanOptions {
            Formats = new[] { SymbolFormat.Pharmacode }, MaxSymbols = 0, EnableTileScan = false, TimeoutMilliseconds = 0
        });
        Assert.Equal("91", Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void DecodeAll_FractionalPatchCode_PreservesCanonicalFormatPriorityAcrossPitches() {
        var pixels = Resize(BarcodeEncoder.Encode(BarcodeType.PatchCode, "T"), 0.85, out var width, out var height);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, out var first));
        Assert.Equal(BarcodeType.PatchCode, first.Type);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var decoded));
        var symbol = Assert.Single(decoded);
        Assert.Equal(BarcodeType.PatchCode, symbol.Type);
        Assert.Equal("T", symbol.Text);
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, width * 4, PixelFormat.Rgba32, out var spanDecoded));
        Assert.Equal(BarcodeType.PatchCode, Assert.Single(spanDecoded).Type);
#endif
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var typed, BarcodeType.PatchCode));
        Assert.Equal("T", Assert.Single(typed).Text);
        var linearFormats = SymbolCapabilities.ImageScannableFormats
            .Where(format => SymbolCapabilities.Get(format).Family == SymbolFamily.Linear).ToArray();
        var scanned = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = linearFormats, MaxSymbols = 0, EnableTileScan = false, TimeoutMilliseconds = 0
        });
        var scannedSymbol = Assert.Single(scanned.Symbols);
        Assert.Equal(SymbolFormat.PatchCode, scannedSymbol.Format);
        Assert.Equal("T", scannedSymbol.Text);
    }

    [Fact]
    public void Scan_FractionallyResizedCode128_UsesSameSamplingForPixelsBytesAndFiles() {
        var pixels = Resize(BarcodeEncoder.Encode(BarcodeType.Code128, "LOT-2026-1042"), 0.75, out var width, out var height);
        var png = EncodePng(pixels, width, height);
        var options = new ScanOptions { Formats = new[] { SymbolFormat.Code128 }, TimeoutMilliseconds = 0, MaxSymbols = 1 };

        Assert.True(Barcode.TryDecodePng(png, SymbolFormat.Code128, out var decoded));
        Assert.Equal("LOT-2026-1042", decoded.Text);
        Assert.Equal("LOT-2026-1042", Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols).Text);
        Assert.Equal("LOT-2026-1042", Assert.Single(SymbolScanner.Scan(png, options).Symbols).Text);

        var path = Path.Combine(Path.GetTempPath(), "CodeGlyphX-linear-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            File.WriteAllBytes(path, png);
            Assert.Equal("LOT-2026-1042", Assert.Single(SymbolScanner.ScanFile(path, options).Symbols).Text);
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Decode_FractionalCode128_StillRejectsInvalidChecksumAndMissingStop(bool missingStop) {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "GUARD-128");
        var segments = barcode.Segments.ToArray();
        // Code128's final six-run checksum precedes the seven-run stop. Replacing it
        // with a different data character preserves valid geometry but invalidates the checksum.
        if (missingStop) {
            barcode = new Barcode1D(segments.Take(segments.Length - 7));
        } else {
            Array.Copy(segments, 6, segments, segments.Length - 13, 6);
            barcode = new Barcode1D(segments);
        }
        var pixels = Resize(barcode, 0.75, out var width, out var height);

        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code128, out _));
        Assert.Empty(SymbolScanner.Scan(EncodePng(pixels, width, height), new ScanOptions {
            Formats = new[] { SymbolFormat.Code128 }, TimeoutMilliseconds = 0
        }).Symbols);
    }

    private static byte[] Resize(Barcode1D barcode, double scale, out int width, out int height) {
        var original = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 10, HeightModules = 30
        }, out var sourceWidth, out var sourceHeight, out var sourceStride);
        width = (int)Math.Round(sourceWidth * scale);
        height = (int)Math.Round(sourceHeight * scale);
        var resized = new byte[width * height * 4];
        // Nearest-neighbor resizing models a document/image pipeline without deriving
        // run widths or module pitch using the decoder's assumptions.
        for (var y = 0; y < height; y++) {
            var sourceY = Math.Min(sourceHeight - 1, (int)((y + 0.5) * sourceHeight / height));
            for (var x = 0; x < width; x++) {
                var sourceX = Math.Min(sourceWidth - 1, (int)((x + 0.5) * sourceWidth / width));
                Buffer.BlockCopy(original, sourceY * sourceStride + sourceX * 4, resized, (y * width + x) * 4, 4);
            }
        }
        return resized;
    }

    private static byte[] EncodePng(byte[] pixels, int width, int height) {
        var rowLength = width * 4 + 1;
        var scanlines = new byte[rowLength * height];
        for (var y = 0; y < height; y++) Buffer.BlockCopy(pixels, y * width * 4, scanlines, y * rowLength + 1, width * 4);
        return PngWriter.WriteRgba8(width, height, scanlines);
    }
}

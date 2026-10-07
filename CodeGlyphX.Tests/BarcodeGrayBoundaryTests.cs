using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeGrayBoundaryTests {
    [Fact]
    public void Scan_SavedSpreadsheet_PreservesCode128AcrossImageAndRegionInputs() {
        // A saved spreadsheet raster at scale 1.5 retains opposite barcode edges
        // at gray 127 and 128. Its native embedded symbol decodes as AT-1042.
        using var fixture = typeof(BarcodeGrayBoundaryTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear.excel-asset-register.png");
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        var png = buffer.ToArray();
        Assert.True(ImageReader.TryDecodeRgba32(png, out var pixels, out var width, out var height));
        var options = new ScanOptions {
            Formats = new[] { SymbolFormat.Code128 }, MaxSymbols = 8,
            TimeoutMilliseconds = 0, Profile = ScanProfile.Robust
        };
        AssertCode128(SymbolScanner.Scan(png, options));
        AssertCode128(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options));

        foreach (var region in new[] {
            new ImageRegion(950, 60, 450, 180),
            new ImageRegion(970, 60, 360, 110),
            new ImageRegion(970, 60, 360, 150)
        }) {
            var regionOptions = new ScanOptions {
                Formats = new[] { SymbolFormat.Code128 }, Region = region,
                MaxSymbols = 8, EnableTileScan = false, TimeoutMilliseconds = 0,
                Profile = ScanProfile.Robust
            };
            AssertCode128(SymbolScanner.Scan(png, regionOptions));
            AssertCode128(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), regionOptions));
        }

        var path = Path.Combine(Path.GetTempPath(), "CodeGlyphX-gray-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            File.WriteAllBytes(path, png);
            AssertCode128(SymbolScanner.ScanFile(path, options));
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData(BarcodeType.Code128, "GRAY-128")]
    [InlineData(BarcodeType.Code93, "GRAY-93")]
    public void Decode_AdjacentGrayEdgeValues_PreservesOneValidatedSymbol(BarcodeType type, string text) {
        var pixels = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(type, text), new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 10, HeightModules = 30
        }, out var width, out var height, out var stride);
        for (var y = 0; y < height; y++) {
            var previousBar = false;
            for (var x = 0; x < width; x++) {
                var offset = y * stride + x * 4;
                var isBar = pixels[offset] == 0;
                if (isBar != previousBar) {
                    // Half-intensity interpolation places the dark edge just below
                    // the midpoint and the light edge just above it.
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = isBar ? (byte)127 : (byte)128;
                }
                previousBar = isBar;
            }
        }

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, type, out var typed));
        Assert.Equal(text, typed.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        var symbol = Assert.Single(decoded);
        Assert.Equal(type, symbol.Type);
        Assert.Equal(text, symbol.Text);
    }

    private static void AssertCode128(ScanResult result) {
        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Code128, symbol.Format);
        Assert.Equal("AT-1042", symbol.Text);
    }
}

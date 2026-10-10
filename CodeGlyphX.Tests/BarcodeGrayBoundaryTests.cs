using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeGrayBoundaryTests {
    [Fact]
    public void InvertedItfSymbols_KeepDarkQuietZonesDuringTileScanning() {
        var left = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(BarcodeType.ITF, "12345678"),
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 20, HeightModules = 30 }, out var lw, out var lh, out _);
        var right = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(BarcodeType.ITF, "87654321"),
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 20, HeightModules = 30 }, out var rw, out var rh, out _);
        var width = lw + rw; var height = Math.Max(lh, rh);
        var pixels = new byte[width * height * 4];
        for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
        PlaceInverted(left, lw, lh, 0); PlaceInverted(right, rw, rh, lw);
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
            new ScanOptions { Formats = new[] { SymbolFormat.Itf }, MaxSymbols = 0, EnableTileScan = true, TileGrid = 2, TimeoutMilliseconds = 0 });
        Assert.Equal(new[] { "12345678", "87654321" }, result.Symbols.Select(symbol => symbol.Text).OrderBy(text => text).ToArray());
        void PlaceInverted(byte[] source, int sw, int sh, int x0) {
            for (var y = 0; y < sh; y++) for (var x = 0; x < sw; x++) {
                var offset = (y * width + x0 + x) * 4;
                pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = (byte)(255 - source[(y * sw + x) * 4]);
            }
        }
    }

    [Theory]
    [InlineData(BarcodeType.EAN, "590123412345+12", 95)]
    [InlineData(BarcodeType.EAN, "1234567+12", 67)]
    [InlineData(BarcodeType.UPCA, "01234567890+12", 95)]
    [InlineData(BarcodeType.UPCE, "123456+12", 51)]
    public void LocalRetailCandidates_PreserveValidatedSupplement(BarcodeType type, string input, int baseLength) {
        var encoded = BarcodeEncoder.Encode(type, input);
        var modules = encoded.Segments.SelectMany(segment => Enumerable.Repeat(segment.IsBar, segment.Modules)).ToList();
        modules.InsertRange(baseLength, Enumerable.Repeat(false, 8));
        Assert.True(BarcodeDecoder.TryDecode(modules.ToArray(), type, out var expected));
        Assert.EndsWith("+12", expected.Text);
        var runs = new System.Collections.Generic.List<BarSegment>();
        var start = 0;
        while (start < modules.Count) {
            var end = start + 1;
            while (end < modules.Count && modules[end] == modules[start]) end++;
            // A one-pixel threshold perturbation keeps the complete global decode valid,
            // while a base-only local candidate otherwise receives a lower residual.
            runs.Add(new BarSegment(modules[start], (end - start) * 4 + (start == baseLength + 9 ? 1 : 0)));
            start = end;
        }
        var pixels = BarcodePngRenderer.RenderPixels(new Barcode1D(runs), new BarcodePngRenderOptions {
            ModuleSize = 1, QuietZone = 40, HeightModules = 80
        }, out var width, out var height, out var stride);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, type, out var actual));
        Assert.Equal(expected.Text, actual.Text);
    }

    [Theory]
    [InlineData("zxing-code128-photo.png", SymbolFormat.Code128, "005-3379497200006")]
    [InlineData("zxing-code39-photo.png", SymbolFormat.Code39, " WWW.CITRONSOFT.COM ")]
    [InlineData("zxing-upca-photo.png", SymbolFormat.UpcA, "071831007995")]
    public void IndependentPhotographs_PreserveCompletePayload(string name, SymbolFormat format, string expected) {
        using var fixture = typeof(BarcodeGrayBoundaryTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream(); fixture!.CopyTo(buffer);
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.Formats = new[] { format }; options.MaxSymbols = 1;
        Assert.Equal(expected, Assert.Single(SymbolScanner.Scan(buffer.ToArray(), options).Symbols).Text);
    }

    [Fact]
    public void ItfPhotograph_DoesNotAdmitShortPayloadFromClippedTile() {
        using var fixture = typeof(BarcodeGrayBoundaryTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Linear.zxing-itf-clipped.png");
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream(); fixture!.CopyTo(buffer);
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.Formats = new[] { SymbolFormat.Itf }; options.MaxSymbols = 32;
        var result = SymbolScanner.Scan(buffer.ToArray(), options);
        Assert.All(result.Symbols, symbol => Assert.Equal("00012345678905", symbol.Text));
    }

    [Fact]
    public void InvalidEan13Parity_IsRejectedWithoutThrowing() {
        // Valid digit and guard patterns do not imply a valid first-digit parity.
        var bits = "101";
        foreach (var parity in "010010") bits += parity == '0' ? "0001101" : "0100111";
        bits += "01010" + string.Concat(Enumerable.Repeat("1110010", 6)) + "101";
        Assert.False(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(), BarcodeType.EAN, out _));
    }

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

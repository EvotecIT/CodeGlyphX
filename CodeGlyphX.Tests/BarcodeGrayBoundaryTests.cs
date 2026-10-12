using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeGrayBoundaryTests {
    [Theory]
    [InlineData("91", false, false)]
    [InlineData("91", false, true)]
    [InlineData("91", true, false)]
    [InlineData("91", true, true)]
    [InlineData("104", false, false)]
    [InlineData("104", false, true)]
    [InlineData("104", true, false)]
    [InlineData("104", true, true)]
    public void PharmacodeWithoutQuietMargin_PreservesCompletePayload(string text, bool inverted, bool vertical) {
        // 91 starts/ends with narrow bars; 104 starts/ends with wide bars.
        // Both can be tightly cropped without making those bars background.
        var runs = BarcodeEncoder.Encode(BarcodeType.Pharmacode, text).Segments
            .Select(run => new BarSegment(run.IsBar, text == "104" && run.IsBar && run.Modules == 2 ? 6 : run.Modules)).ToArray();
        var pixels = BarcodePngRenderer.RenderPixels(new Barcode1D(runs), new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 0, HeightModules = 30
        }, out var width, out var height, out _);
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var c = 0; c < 3; c++) pixels[at + c] = (byte)(255 - pixels[at + c]);
        if (vertical) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated; (width, height) = (height, width);
        }
        var stride = width * 4;
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, BarcodeType.Pharmacode, out var single));
        Assert.Equal(text, single.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var all, BarcodeType.Pharmacode));
        Assert.Equal(text, Assert.Single(all).Text);
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecode(pixels.AsSpan(), width, height, stride, PixelFormat.Rgba32, BarcodeType.Pharmacode, out var spanSingle));
        Assert.Equal(text, spanSingle.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels.AsSpan(), width, height, stride, PixelFormat.Rgba32, out var spanAll, BarcodeType.Pharmacode));
        Assert.Equal(text, Assert.Single(spanAll).Text);
#endif
        foreach (var tileScan in new[] { false, true }) {
            var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
                Formats = new[] { SymbolFormat.Pharmacode }, MaxSymbols = 0, EnableTileScan = tileScan, TimeoutMilliseconds = 0
            });
            Assert.Equal(ScanCompletionReason.Completed, scan.CompletionReason);
            Assert.Equal(text, Assert.Single(scan.Symbols).Text);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PharmacodePixelRoundedWideBars_PreserveCompletePayload(bool inverted, bool vertical) {
        // A 0.4/2.5/1 mm narrow/wide/space symbol at 3 pixels/mm and
        // a 0.2-pixel phase has these independently rounded pixel runs.
        var widths = new[] { 1, 3, 8, 3, 7, 3, 8, 3, 1, 3, 1 };
        var runs = widths.Select((width, index) => new BarSegment(index % 2 == 0, width)).ToArray();
        var modules = runs.SelectMany(run => Enumerable.Repeat(run.IsBar, run.Modules)).ToArray();
        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.Pharmacode, out var raw));
        Assert.Equal("91", raw.Text);
        var pixels = BarcodePngRenderer.RenderPixels(new Barcode1D(runs), new BarcodePngRenderOptions {
            ModuleSize = 1, QuietZone = 20, HeightModules = 50
        }, out var width, out var height, out _);
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var c = 0; c < 3; c++) pixels[at + c] = (byte)(255 - pixels[at + c]);
        if (vertical) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated; (width, height) = (height, width);
        }
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var decoded, BarcodeType.Pharmacode));
        Assert.Equal("91", Assert.Single(decoded).Text);
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.Pharmacode }, MaxSymbols = 0, EnableTileScan = false, TimeoutMilliseconds = 0
        });
        Assert.Equal(ScanCompletionReason.Completed, scan.CompletionReason);
        Assert.Equal("91", Assert.Single(scan.Symbols).Text);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(6, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(6, true)]
    public void PharmacodeWidthRatios_PreserveCompletePayload(int wideWidth, bool inverted) {
        var runs = BarcodeEncoder.Encode(BarcodeType.Pharmacode, "91").Segments
            .Select(run => new BarSegment(run.IsBar, run.IsBar && run.Modules == 2 ? wideWidth : run.Modules)).ToArray();
        var barcode = new Barcode1D(runs);
        var modules = runs.SelectMany(run => Enumerable.Repeat(run.IsBar, run.Modules)).ToArray();
        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.Pharmacode, out var raw));
        Assert.Equal("91", raw.Text);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 20, HeightModules = 30
        }, out var width, out var height, out var stride);
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var c = 0; c < 3; c++) pixels[at + c] = (byte)(255 - pixels[at + c]);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, BarcodeType.Pharmacode));
        Assert.Equal("91", Assert.Single(decoded).Text);
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.Pharmacode }, MaxSymbols = 0, TimeoutMilliseconds = 0
        });
        Assert.Equal(ScanCompletionReason.Completed, scan.CompletionReason);
        Assert.Equal("91", Assert.Single(scan.Symbols).Text);
    }

    [Theory]
    [InlineData(BarcodeType.Industrial2of5, 0, false)]
    [InlineData(BarcodeType.Industrial2of5, 0, true)]
    [InlineData(BarcodeType.Industrial2of5, 128, false)]
    [InlineData(BarcodeType.Industrial2of5, 128, true)]
    [InlineData(BarcodeType.ITF, 0, false)]
    [InlineData(BarcodeType.ITF, 0, true)]
    [InlineData(BarcodeType.ITF, 128, false)]
    [InlineData(BarcodeType.ITF, 128, true)]
    [InlineData(BarcodeType.Pharmacode, 0, false)]
    [InlineData(BarcodeType.Pharmacode, 0, true)]
    [InlineData(BarcodeType.Pharmacode, 128, false)]
    [InlineData(BarcodeType.Pharmacode, 128, true)]
    [InlineData(BarcodeType.Pharmacode, 0, false, true)]
    [InlineData(BarcodeType.Pharmacode, 0, true, true)]
    [InlineData(BarcodeType.Pharmacode, 128, false, true)]
    [InlineData(BarcodeType.Pharmacode, 128, true, true)]
    [InlineData(BarcodeType.Pharmacode, 0, false, false, 2)]
    [InlineData(BarcodeType.Pharmacode, 0, true, false, 2)]
    [InlineData(BarcodeType.Pharmacode, 0, false, true, 2)]
    [InlineData(BarcodeType.Pharmacode, 0, true, true, 2)]
    public void TransparentTileMargins_PreserveBothOriginalPayloads(BarcodeType type, byte alpha, bool vertical, bool inverted = false, int quietZone = 20) {
        var expected = type == BarcodeType.ITF ? new[] { "12345678", "87654321" }
            : type == BarcodeType.Pharmacode ? new[] { "91", "123" } : new[] { "2", "8" };
        var format = type == BarcodeType.ITF ? SymbolFormat.Itf
            : type == BarcodeType.Pharmacode ? SymbolFormat.Pharmacode : SymbolFormat.Industrial2Of5;
        var render = new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = quietZone, HeightModules = 30,
            Background = new Rgba32(0, 0, 0, alpha) };
        var left = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(type, expected[0]), render, out var lw, out var lh, out _);
        var right = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(type, expected[1]), render, out var rw, out var rh, out _);
        var width = lw + rw; var height = Math.Max(lh, rh);
        var pixels = new byte[width * height * 4];
        for (var at = 3; at < pixels.Length; at += 4) pixels[at] = alpha;
        for (var y = 0; y < lh; y++) Array.Copy(left, y * lw * 4, pixels, y * width * 4, lw * 4);
        for (var y = 0; y < rh; y++) Array.Copy(right, y * rw * 4, pixels, (y * width + lw) * 4, rw * 4);
        if (inverted) for (var at = 0; at < pixels.Length; at += 4) {
            // Invert the composited visible pixel, including transparent padding.
            var opacity = pixels[at + 3];
            for (var c = 0; c < 3; c++) pixels[at + c] = (byte)(255 - (pixels[at + c] * opacity + 255 * (255 - opacity) + 127) / 255);
            pixels[at + 3] = 255;
        }
        if (vertical) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated; (width, height) = (height, width);
        }
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { format }, EnableTileScan = true, TileGrid = 2, MaxSymbols = 0, TimeoutMilliseconds = 0
        });
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        foreach (var text in expected) Assert.Contains(result.Symbols, hit => hit.Format == format && hit.Text == text);
        Assert.Equal(2, result.Symbols.Count);
        if (type == BarcodeType.Pharmacode) {
            var decodeOptions = new BarcodeDecodeOptions { EnableTileScan = true, TileGrid = 2 };
            Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var all, type, decodeOptions));
            Assert.Equal(expected.OrderBy(text => text, StringComparer.Ordinal), all.Select(hit => hit.Text).OrderBy(text => text, StringComparer.Ordinal));
#if NET8_0_OR_GREATER
            Assert.True(BarcodeDecoder.TryDecodeAll(pixels.AsSpan(), width, height, width * 4, PixelFormat.Rgba32, out var spanAll, type, decodeOptions));
            Assert.Equal(expected.OrderBy(text => text, StringComparer.Ordinal), spanAll.Select(hit => hit.Text).OrderBy(text => text, StringComparer.Ordinal));
#endif
        }
    }

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
        Assert.NotEmpty(result.Symbols);
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

    [Theory]
    [InlineData(BarcodeType.EAN, "590123412345+12", 95)]
    [InlineData(BarcodeType.EAN, "1234567+12", 67)]
    [InlineData(BarcodeType.UPCA, "01234567890+12", 95)]
    [InlineData(BarcodeType.UPCE, "123456+12", 51)]
    public void Decode_GraySupplementSurvivesConfirmationOfUnchangedBase(BarcodeType type, string input, int baseLength) {
        var encoded = BarcodeEncoder.Encode(type, input);
        Assert.True(BarcodeDecoder.TryDecode(encoded.Segments.SelectMany(segment => Enumerable.Repeat(segment.IsBar, segment.Modules)).ToArray(), type, out var expected));
        var pixels = BarcodePngRenderer.RenderPixels(encoded,
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 12, HeightModules = 30 },
            out var width, out var height, out var stride);
        // Base modules are identical at cutoffs 127 and 128. Only the latter
        // includes the gray add-on, which must survive neighboring-row confirmation.
        for (var y = 0; y < height; y++) for (var x = (12 + baseLength) * 3; x < width; x++) {
            var at = y * stride + x * 4;
            if (pixels[at] == 0) pixels[at] = pixels[at + 1] = pixels[at + 2] = 127;
        }
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, type, out var actual));
        Assert.Equal(expected.Text, actual.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var all, type));
        Assert.Equal(expected.Text, Assert.Single(all).Text);
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecode(pixels.AsSpan(), width, height, stride, PixelFormat.Rgba32, type, out var span));
        Assert.Equal(expected.Text, span.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels.AsSpan(), width, height, stride, PixelFormat.Rgba32, out var spanAll, type));
        Assert.Equal(expected.Text, Assert.Single(spanAll).Text);
#endif
        var format = type == BarcodeType.EAN ? SymbolFormat.Ean : type == BarcodeType.UPCA ? SymbolFormat.UpcA : SymbolFormat.UpcE;
        var options = new ScanOptions { Formats = new[] { format }, Profile = ScanProfile.Fast,
            MaxSymbols = 0, EnableTileScan = false, TimeoutMilliseconds = 0 };
        Assert.Equal(expected.Text, Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols).Text);
    }

    private static void AssertCode128(ScanResult result) {
        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Code128, symbol.Format);
        Assert.Equal("AT-1042", symbol.Text);
    }
}

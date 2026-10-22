using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Ultracode;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class UltracodeImageTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void IndependentPngUsesTheNormalColourAwareScanner(int revision) {
        using var stream = typeof(UltracodeImageTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-ultracode-rev" + revision + ".png");
        Assert.NotNull(stream);
        using var output = new MemoryStream(); stream!.CopyTo(output);
        var scan = SymbolScanner.Scan(output.ToArray(), Options());
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal(SymbolFormat.Ultracode, symbol.Format);
        Assert.Equal("Case 123!", symbol.Text);
        Assert.Equal(UltracodeErrorCorrectionLevel.EC3, Assert.IsType<UltracodeSymbolMetadata>(symbol.Metadata).ErrorCorrectionLevel);
        Assert.NotNull(symbol.Geometry);
        Assert.Empty(scan.UnsupportedFormats);
    }

    [Theory]
    [InlineData(UltracodeErrorCorrectionLevel.EC1)]
    [InlineData(UltracodeErrorCorrectionLevel.EC2)]
    [InlineData(UltracodeErrorCorrectionLevel.EC3)]
    [InlineData(UltracodeErrorCorrectionLevel.EC4)]
    [InlineData(UltracodeErrorCorrectionLevel.EC5)]
    public void RenderedRotationsPreservePayloadAndDetectedGeometry(UltracodeErrorCorrectionLevel level) {
        var encoded = UltracodeEncoder.EncodeText("Case 123!", level);
        var cells = encoded.Cells;
        for (var rotation = 0; rotation < 4; rotation++) {
            Assert.True(UltracodeDecoder.TryDecode(cells, out var sampled));
            Assert.Equal(rotation * 90, sampled!.RotationDegrees);
            var pixels = UltracodeRenderer.RenderPixels(cells, new UltracodeRenderOptions { CellSize = 6, QuietZone = 2 }, out var width, out var height);
            var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), Options());
            var decoded = Assert.Single(result.Symbols);
            Assert.Equal("Case 123!", decoded.Text);
            Assert.Equal(rotation * 90, decoded.Geometry!.RotationDegrees, 6);
            Assert.Equal(12, decoded.Geometry.Bounds.X, 6);
            Assert.Equal(12, decoded.Geometry.Bounds.Y, 6);
            Assert.Equal(cells.Width * 6, decoded.Geometry.Bounds.Width, 6);
            Assert.Equal(cells.Height * 6, decoded.Geometry.Bounds.Height, 6);
            cells = Rotate(cells);
        }
    }

    [Fact]
    public void RepeatedSymbolsRemainDistinctAndGrayscaleIsNotInventedAsColour() {
        var encoded = UltracodeEncoder.EncodeText("Repeated");
        var rgba = PngReader.DecodeRgba32(encoded.RenderPng(), out var width, out var height);
        var canvasWidth = width * 2 + 24;
        var canvas = new byte[canvasWidth * height * 4];
        for (var i = 0; i < canvas.Length; i++) canvas[i] = 255;
        for (var y = 0; y < height; y++) {
            Buffer.BlockCopy(rgba, y * width * 4, canvas, y * canvasWidth * 4, width * 4);
            Buffer.BlockCopy(rgba, y * width * 4, canvas, (y * canvasWidth + width + 24) * 4, width * 4);
        }
        var options = Options(); options.MaxSymbols = 4; options.EnableTileScan = false; options.Deduplicate = false;
        var scan = SymbolScanner.Scan(ImageFrame.Packed(canvas, canvasWidth, height, PixelFormat.Rgba32), options);
        Assert.Equal(2, scan.Symbols.Count);
        Assert.All(scan.Symbols, symbol => Assert.Equal("Repeated", symbol.Text));
        Assert.NotEqual(scan.Symbols[0].Geometry!.Bounds.X, scan.Symbols[1].Geometry!.Bounds.X);
        options.Deduplicate = true;
        Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(canvas, canvasWidth, height, PixelFormat.Rgba32), options).Symbols);
        for (var at = 0; at < rgba.Length; at += 4) {
            var gray = (byte)((rgba[at] * 77 + rgba[at + 1] * 150 + rgba[at + 2] * 29) >> 8);
            rgba[at] = rgba[at + 1] = rgba[at + 2] = gray;
        }
        Assert.Empty(SymbolScanner.Scan(ImageFrame.Packed(rgba, width, height, PixelFormat.Rgba32), Options()).Symbols);
    }

    [Theory]
    [InlineData(-15, 0)]
    [InlineData(15, 0)]
    [InlineData(0, .05)]
    [InlineData(15, .05)]
    public void ModerateSkewAndPerspectiveRetainColourAndPayload(double angle, double perspective) {
        var encoded = UltracodeEncoder.EncodeText("Colour 123", UltracodeErrorCorrectionLevel.EC3);
        var pixels = PngReader.DecodeRgba32(encoded.RenderPng(), out var width, out var height);
        var warped = Warp(pixels, width, height, angle, perspective, out var targetWidth, out var targetHeight);
        var scan = SymbolScanner.Scan(ImageFrame.Packed(warped, targetWidth, targetHeight, PixelFormat.Rgba32), Options());
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal("Colour 123", symbol.Text);
        Assert.NotNull(symbol.Geometry);
        if (perspective == 0) Assert.InRange(symbol.Geometry!.RotationDegrees, angle < 0 ? 343 : 13, angle < 0 ? 347 : 17);
    }

    [Fact]
    public void DefaultFormatSelectionRecognizesColourBeforeMonochromeSampling() {
        var encoded = UltracodeEncoder.EncodeText("Default scan");
        var scan = SymbolScanner.Scan(encoded.RenderPng(), new ScanOptions { MaxSymbols = 1, TimeoutMilliseconds = TestBudget.Adjust(5000) });
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal(SymbolFormat.Ultracode, symbol.Format);
        Assert.Equal("Default scan", symbol.Text);
    }

    [Fact]
    public void CandidateExhaustionIsReportedAsAPartialSearch() {
        const int width = 440, height = 560;
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        for (var rectangle = 0; rectangle < 130; rectangle++) {
            var left = rectangle % 10 * 42 + 4; var top = rectangle / 10 * 42 + 4;
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) {
                if (x != 0 && x != 31 && y != 0 && y != 31) continue;
                var at = ((top + y) * width + left + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
            }
        }
        pixels[((10 * width) + 10) * 4] = 0; // A cyan pixel makes this a colour-image candidate.
        var options = Options(); options.MaxSymbols = 0; options.TimeoutMilliseconds = 0; options.EnableTileScan = false;
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Empty(scan.Symbols);
        Assert.True(scan.IsPartial);
        Assert.Equal(ScanCompletionReason.ResourceLimitReached, scan.CompletionReason);
    }

    [Fact]
    public void ColourGridHasItsOwnCapabilityAndUnifiedDecodeOverload() {
        var encoded = UltracodeEncoder.EncodeText("Palette");
        var capability = SymbolCapabilities.Get(SymbolFormat.Ultracode);
        Assert.True(capability.CanDecodeColorCells);
        Assert.False(capability.CanDecodeModules);
        var decoded = SymbolDecoder.Decode(encoded.Cells);
        Assert.Equal(SymbolFormat.Ultracode, decoded.Format);
        Assert.Equal("Palette", decoded.Text);
    }

    private static ScanOptions Options() => new() {
        Formats = new[] { SymbolFormat.Ultracode }, MaxSymbols = 1, TimeoutMilliseconds = TestBudget.Adjust(5000)
    };

    private static UltracodeGrid Rotate(UltracodeGrid grid) {
        var rotated = new UltracodeGrid(grid.Height, grid.Width);
        for (var y = 0; y < grid.Height; y++) for (var x = 0; x < grid.Width; x++) rotated[grid.Height - y - 1, x] = grid[x, y];
        return rotated;
    }

    private static byte[] Warp(byte[] source, int width, int height, double angle, double perspective,
        out int targetWidth, out int targetHeight) {
        var radians = angle * Math.PI / 180;
        var cos = Math.Cos(radians); var sin = Math.Sin(radians);
        targetWidth = (int)Math.Ceiling(width * Math.Abs(cos) + height * Math.Abs(sin)) + 48;
        targetHeight = (int)Math.Ceiling(height * Math.Abs(cos) + width * Math.Abs(sin)) + 48;
        var corners = new[] { new SymbolPoint(0, 0), new SymbolPoint(width * (1 - perspective), height * perspective),
            new SymbolPoint(width, height), new SymbolPoint(width * perspective, height) };
        for (var i = 0; i < corners.Length; i++) {
            var x = corners[i].X - width / 2D; var y = corners[i].Y - height / 2D;
            corners[i] = new SymbolPoint(targetWidth / 2D + cos * x - sin * y, targetHeight / 2D + sin * x + cos * y);
        }
        var transform = CodeGlyphX.Qr.QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            corners[0].X, corners[0].Y, corners[1].X, corners[1].Y,
            corners[2].X, corners[2].Y, corners[3].X, corners[3].Y, 0, 0, width, 0, width, height, 0, height);
        var result = new byte[targetWidth * targetHeight * 4];
        for (var at = 0; at < result.Length; at++) result[at] = 255;
        for (var y = 0; y < targetHeight; y++) for (var x = 0; x < targetWidth; x++) {
            transform.Transform(x + .5, y + .5, out var sx, out var sy);
            if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
            Buffer.BlockCopy(source, ((int)sy * width + (int)sx) * 4, result, (y * targetWidth + x) * 4, 4);
        }
        return result;
    }
}

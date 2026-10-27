using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class CompositeCarrierImageTests {
    [Fact]
    public void CheckedStandaloneComponentSurvivesCancelledAdditionalCarrierSearch() {
        var component = Gs1CompositeComponents.Encode("(17)251231", Gs1CompositeComponent.CcA, 4);
        // Invoke the image-grid boundary directly to stop only the additional carrier search,
        // after its first checked component decode. This avoids a timing race or clock hook.
        var decode = typeof(SpecialistImageDecoder).GetMethod("TryDecodeCompositeImageGrid", BindingFlags.Static | BindingFlags.NonPublic)!;
        object?[] args = { component.Modules, null, new CancellationToken(true), null };
        Assert.True((bool)decode.Invoke(null, args)!);
        var decoded = Assert.IsType<DetectedSymbol>(args[3]);
        Assert.Equal("17251231", decoded.Text);
        Assert.True(Assert.IsType<Gs1CompositeSymbolMetadata>(decoded.Metadata).IsStandalone);
    }

    [Fact]
    public void IndependentPrintedCarriersRecoverBothMessagesThroughTheScanner() {
        var errors = new List<string>();
        var index = 0;
        foreach (var parts in CompositeCarrierConformanceTests.Vectors()) {
            if (parts[7] != "bwipp") continue;
            var carrier = (Gs1CompositeCarrier)Enum.Parse(typeof(Gs1CompositeCarrier), parts[0]);
            using var stream = typeof(CompositeCarrierImageTests).Assembly.GetManifestResourceStream(
                "CodeGlyphX.Tests.Fixtures.Composite.bwipp-composite-" + index + ".png");
            Assert.NotNull(stream);
            using var buffer = new MemoryStream();
            stream!.CopyTo(buffer);
            var pixels = PngReader.DecodeRgba32(buffer.ToArray(), out var width, out var height);
            for (var rotation = 0; rotation < 4; rotation++) {
                var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
                    Formats = new[] { SymbolFormat.Gs1Composite }, CompositeCarrier = carrier,
                    MaxSymbols = 1, EnableTileScan = false, TimeoutMilliseconds = TestBudget.Adjust(5000)
                });
                if (result.Symbols.Count != 1 || result.Symbols[0].Metadata is not Gs1CompositeSymbolMetadata metadata
                    || metadata.IsStandalone || metadata.LinearText != Gs1.ElementString(parts[2])
                    || metadata.CompositeText != Gs1.ElementString(parts[3]) || metadata.Carrier != carrier) {
                    errors.Add($"Image {index} {carrier}/{parts[1]} rotation {rotation * 90}: {result.Status}/{result.CompletionReason}, "
                        + (result.Symbols.Count == 0 ? "no symbol" : result.Symbols[0].Text));
                } else {
                    Assert.NotNull(result.Symbols[0].Geometry);
                    Assert.NotNull(metadata.ComponentGeometry);
                    Assert.NotNull(metadata.LinearGeometry);
                }
                pixels = Rotate(pixels, width, height);
                var oldWidth = width; width = height; height = oldWidth;
            }
            index++;
        }
        Assert.Equal(42, index);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public void ComponentCornersFollowPrintedRowHeightsRotationsAndSourceRegions() {
        var pixels = Load(0, out var width, out var height);
        var component = new[] { new SymbolPoint(30, 30), new SymbolPoint(327, 30), new SymbolPoint(327, 48), new SymbolPoint(30, 48) };
        var linear = new[] { new SymbolPoint(39, 66), new SymbolPoint(324, 66), new SymbolPoint(324, 282), new SymbolPoint(39, 282) };
        for (var rotation = 0; rotation < 4; rotation++) {
            const int left = 41, top = 29;
            var canvasWidth = width + 90;
            var canvasHeight = height + 80;
            var canvas = new byte[canvasWidth * canvasHeight * 4];
            for (var i = 0; i < canvas.Length; i++) canvas[i] = 255;
            for (var y = 0; y < height; y++)
                Buffer.BlockCopy(pixels, y * width * 4, canvas, ((top + y) * canvasWidth + left) * 4, width * 4);
            var options = Options();
            options.Region = new ImageRegion(20, 10, width + 50, height + 60);
            var result = Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(canvas, canvasWidth, canvasHeight, PixelFormat.Rgba32), options).Symbols);
            var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(result.Metadata);
            AssertCorners(metadata.ComponentGeometry, component, left, top);
            AssertCorners(metadata.LinearGeometry, linear, left, top);
            pixels = Rotate(pixels, width, height);
            for (var i = 0; i < 4; i++) {
                component[i] = new SymbolPoint(height - component[i].Y, component[i].X);
                linear[i] = new SymbolPoint(height - linear[i].Y, linear[i].X);
            }
            var previousWidth = width; width = height; height = previousWidth;
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void ACompleteUpperComponentSurvivesACroppedOrDamagedCarrier(int index, bool crop) {
        var pixels = Load(index, out var width, out var height);
        var componentBottom = index == 0 ? 48 : 54;
        if (crop) {
            height = componentBottom + 4;
            Array.Resize(ref pixels, width * height * 4);
        } else {
            for (var y = componentBottom + 18; y < height; y++) for (var x = 39; x < 63; x++) {
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 255;
            }
        }
        var decoded = Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), Options()).Symbols);
        var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(decoded.Metadata);
        Assert.True(metadata.IsStandalone);
        Assert.Null(metadata.Carrier);
        Assert.Empty(metadata.LinearText);
        Assert.Equal("11990102", metadata.CompositeText);
        Assert.Null(metadata.LinearGeometry);
        AssertCorners(metadata.ComponentGeometry, new[] { new SymbolPoint(30, 30), new SymbolPoint(327, 30),
            new SymbolPoint(327, componentBottom), new SymbolPoint(30, componentBottom) }, 0, 0);
        Assert.Equal(metadata.ComponentGeometry!.Bounds, decoded.Geometry!.Bounds);
    }

    [Fact]
    public void ASeparateUnlinkedRetailBarcodeIsNotReportedAsTheComponentCarrier() {
        var pixels = Load(0, out var width, out var height);
        // Keep the complete checked component and retail bars; remove all three linkage separator rows.
        for (var y = 48; y < 66; y++) for (var x = 0; x < width; x++) {
            var at = (y * width + x) * 4;
            pixels[at] = pixels[at + 1] = pixels[at + 2] = 255;
        }
        var options = Options();
        options.Formats = new[] { SymbolFormat.Gs1Composite, SymbolFormat.Mailmark4State };
        options.MaxSymbols = 8;
        options.Deduplicate = false;
        var decoded = Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols);
        var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(decoded.Metadata);
        Assert.True(metadata.IsStandalone);
        Assert.Empty(metadata.LinearText);
        Assert.Equal("11990102", decoded.Text);
        Assert.Null(metadata.LinearGeometry);
    }

    [Theory]
    [InlineData(Gs1CompositeComponent.CcA)]
    [InlineData(Gs1CompositeComponent.CcB)]
    public void ImageFramingRecoveryIsBoundedAndDoesNotWeakenSampledModuleValidation(Gs1CompositeComponent component) {
        var encoded = Gs1CompositeEncoder.Encode("9520123456788", "(11)990102",
            new Gs1CompositeEncodingOptions { Carrier = Gs1CompositeCarrier.Ean13, Component = component });
        var sampled = SymbolDecoder.Decode(encoded.Modules, SymbolFormat.Gs1Composite);
        var sampledMetadata = Assert.IsType<Gs1CompositeSymbolMetadata>(sampled.Metadata);
        Assert.Null(sampledMetadata.ComponentGeometry);
        Assert.Null(sampledMetadata.LinearGeometry);
        var damaged = encoded.Modules.Clone();
        // These are the three framing-cell changes observed after independent-image perspective resampling.
        damaged[90, 0] = !damaged[90, 0];
        damaged[91, 1] = !damaged[91, 1];
        damaged[92, 1] = !damaged[92, 1];
        Assert.False(Gs1CompositeDecoder.TryDecode(damaged, Gs1CompositeCarrier.Ean13, out _));
        Assert.False(SymbolDecoder.TryDecodeComposite(damaged, out _, Gs1CompositeCarrier.Ean13));
        var image = new MatrixSymbol(SymbolFormat.Gs1Composite, damaged).Render(OutputFormat.Png, new MatrixOptions { ModuleSize = 6, QuietZone = 2 }).ToArray();
        var decoded = Assert.Single(SymbolScanner.Scan(image, Options()).Symbols);
        var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(decoded.Metadata);
        Assert.False(metadata.IsStandalone);
        Assert.Equal(encoded.LinearText, metadata.LinearText);
        Assert.Equal(encoded.CompositeText, metadata.CompositeText);
        for (var y = 0; y < encoded.ComponentRows; y++) {
            damaged[2, y] = !damaged[2, y];
            damaged[90, y] = !encoded.Modules[90, y];
        }
        image = new MatrixSymbol(SymbolFormat.Gs1Composite, damaged).Render(OutputFormat.Png, new MatrixOptions { ModuleSize = 6, QuietZone = 2 }).ToArray();
        Assert.Empty(SymbolScanner.Scan(image, Options()).Symbols);
    }

    private static ScanOptions Options() => new() {
        Formats = new[] { SymbolFormat.Gs1Composite }, CompositeCarrier = Gs1CompositeCarrier.Ean13,
        MaxSymbols = 1, EnableTileScan = false, TimeoutMilliseconds = TestBudget.Adjust(5000)
    };

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(10, 0)]
    [InlineData(0, .03)]
    [InlineData(10, .03)]
    public void IndependentLinkedSymbolsRetainBothMessagesAfterSkewAndPerspective(double angle, double perspective) {
        var pixels = Load(0, out var width, out var height);
        pixels = Warp(pixels, width, height, angle, perspective, out width, out height);
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), Options());
        Assert.True(scan.Symbols.Count == 1, $"{scan.Status}/{scan.CompletionReason} for {angle}/{perspective}");
        var symbol = Assert.Single(scan.Symbols);
        var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(symbol.Metadata);
        Assert.False(metadata.IsStandalone);
        Assert.Equal("9520123456788", metadata.LinearText);
        Assert.Equal("11990102", metadata.CompositeText);
        Assert.NotNull(metadata.ComponentGeometry);
        Assert.NotNull(metadata.LinearGeometry);
    }

    [Fact]
    public void EncodedImageDownscalingKeepsComponentGeometryInSourcePixels() {
        var pixels = Load(0, out var width, out var height);
        var doubled = new byte[pixels.Length * 4];
        for (var y = 0; y < height * 2; y++) for (var x = 0; x < width * 2; x++)
            Buffer.BlockCopy(pixels, ((y / 2) * width + x / 2) * 4, doubled, (y * width * 2 + x) * 4, 4);
        var png = PngImageEncoder.EncodeRgba32(doubled, width * 2, height * 2);
        var options = Options();
        options.Image = new ImageDecodeOptions { MaxDimension = width };
        var symbol = Assert.Single(SymbolScanner.Scan(png, options).Symbols);
        var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(symbol.Metadata);
        AssertCorners(metadata.ComponentGeometry, new[] { new SymbolPoint(60, 60), new SymbolPoint(654, 60),
            new SymbolPoint(654, 96), new SymbolPoint(60, 96) }, 0, 0);
        AssertCorners(metadata.LinearGeometry, new[] { new SymbolPoint(78, 132), new SymbolPoint(648, 132),
            new SymbolPoint(648, 564), new SymbolPoint(78, 564) }, 0, 0);
    }

    private static byte[] Warp(byte[] pixels, int width, int height, double angle, double perspective,
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
            corners[0].X, corners[0].Y, corners[1].X, corners[1].Y, corners[2].X, corners[2].Y,
            corners[3].X, corners[3].Y, 0, 0, width, 0, width, height, 0, height);
        var result = new byte[targetWidth * targetHeight * 4];
        for (var at = 0; at < result.Length; at++) result[at] = 255;
        for (var y = 0; y < targetHeight; y++) for (var x = 0; x < targetWidth; x++) {
            transform.Transform(x + .5, y + .5, out var sx, out var sy);
            if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
            Buffer.BlockCopy(pixels, ((int)sy * width + (int)sx) * 4, result, (y * targetWidth + x) * 4, 4);
        }
        return result;
    }

    private static byte[] Load(int index, out int width, out int height) {
        using var stream = typeof(CompositeCarrierImageTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Composite.bwipp-composite-" + index + ".png");
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);
        return PngReader.DecodeRgba32(buffer.ToArray(), out width, out height);
    }

    private static void AssertCorners(SymbolGeometry? actual, SymbolPoint[] expected, int left, int top) {
        Assert.NotNull(actual);
        var points = new[] { actual!.TopLeft, actual.TopRight, actual.BottomRight, actual.BottomLeft };
        for (var i = 0; i < points.Length; i++) {
            Assert.InRange(points[i].X, expected[i].X + left - 1, expected[i].X + left + 1);
            Assert.InRange(points[i].Y, expected[i].Y + top - 1, expected[i].Y + top + 1);
        }
    }

    private static byte[] Rotate(byte[] pixels, int width, int height) {
        var result = new byte[pixels.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            Buffer.BlockCopy(pixels, (y * width + x) * 4, result, (x * height + height - y - 1) * 4, 4);
        return result;
    }
}

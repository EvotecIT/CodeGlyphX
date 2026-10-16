using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed partial class SpecialistImageScannerTests {
    public static IEnumerable<object[]> ExternalImages() {
        yield return new object[] { SymbolFormat.RmQrCode, "RMQR-42" };
        yield return new object[] { SymbolFormat.HanXin, "1234567890" };
        yield return new object[] { SymbolFormat.DotCode, "ABC123" };
        yield return new object[] { SymbolFormat.Code16K, "HELLO" };
        yield return new object[] { SymbolFormat.Code49, "ABC123" };
        yield return new object[] { SymbolFormat.CodablockF, "AB1234567890xy" };
        yield return new object[] { SymbolFormat.MicroPdf417, "ABC123" };
        yield return new object[] { SymbolFormat.Gs1Composite, "21A1B2C3D4E5F6G7H8" };
        yield return new object[] { SymbolFormat.UspsIntelligentMail, "01234567094987654321-01234567891" };
        yield return new object[] { SymbolFormat.RoyalMail4State, "LE28HS9Z" };
        yield return new object[] { SymbolFormat.AustraliaPost, "56439111ABA9X" };
        yield return new object[] { SymbolFormat.JapanPost, "15400233-16-4" };
        yield return new object[] { SymbolFormat.KixCode, "1231FZ13XHS" };
        yield return new object[] { SymbolFormat.Postnet, "0123456784" };
        yield return new object[] { SymbolFormat.Planet, "012345678905" };
        yield return new object[] { SymbolFormat.PharmacodeTwoTrack, "117480" };
        yield return new object[] { SymbolFormat.FacingIdentificationMark, "A" };
        yield return new object[] { SymbolFormat.Daft, "FATDAFTD" };
    }

    [Theory]
    [MemberData(nameof(ExternalImages))]
    public void EncodedImages_DecodeIndependentBwippPixels(SymbolFormat format, string expected) {
        var png = Resource("ImageSamples.bwipp-" + format + ".png");
        var result = SymbolScanner.Scan(png, Options(format));
        Assert.True(result.IsSuccess, $"{format}: {result.Status}/{result.CompletionReason}/{result.Failure}");
        var hit = Assert.Single(result.Symbols);
        Assert.Equal(format, hit.Format);
        Assert.Equal(expected, hit.Text);
        Assert.NotNull(hit.Geometry);
        Assert.NotNull(hit.SearchRegion);
        Assert.Empty(result.UnsupportedFormats);
    }

    [Theory]
    [MemberData(nameof(ExternalImages))]
    public void ExternalPixels_RetainPayloadAfterQuarterTurnAndInversion(SymbolFormat format, string expected) {
        Assert.True(ImageReader.TryDecodeRgba32(Resource("ImageSamples.bwipp-" + format + ".png"),
            out var rgba, out var width, out var height));
        var rotated = new byte[rgba.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var source = (y * width + x) * 4;
            var target = (x * height + height - 1 - y) * 4;
            for (var channel = 0; channel < 3; channel++) rotated[target + channel] = (byte)(255 - rgba[source + channel]);
            rotated[target + 3] = 255;
        }
        var result = SymbolScanner.Scan(ImageFrame.Packed(rotated, height, width, PixelFormat.Rgba32), Options(format));
        Assert.True(result.IsSuccess, $"{format}: {result.Status}/{result.CompletionReason}");
        var hit = Assert.Single(result.Symbols);
        Assert.Equal(expected, hit.Text);
        Assert.True(hit.IsInverted);
    }

    [Theory]
    [InlineData("bwipp-current.json", "d3aqr", SymbolFormat.D3Aqr)]
    [InlineData("bwipp-current.json", "mailmark4state", SymbolFormat.Mailmark4State)]
    [InlineData("bwipp-linear.json", "code16k", SymbolFormat.Code16K)]
    [InlineData("bwipp-linear.json", "code49", SymbolFormat.Code49)]
    [InlineData("bwipp-linear.json", "codablockf", SymbolFormat.CodablockF)]
    [InlineData("zint-micropdf.json", "hibcmicropdf417", SymbolFormat.MicroPdf417)]
    [InlineData("bwipp-current.json", "posicode", SymbolFormat.PosiCode)]
    public void ExternalGrids_DecodeThroughTheImageBoundary(string file, string bcid, SymbolFormat format) {
        using var document = JsonDocument.Parse(Resource("Conformance." + file));
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != bcid) continue;
            var width = fixture.GetProperty("width").GetInt32();
            var height = fixture.GetProperty("height").GetInt32();
            var bits = fixture.GetProperty("modules").GetString()!;
            var modules = new BitMatrix(width, height);
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) modules[x, y] = bits[y * width + x] == '1';
            var expected = SymbolDecoder.Decode(modules, format);
            for (var transform = 0; transform < (format == SymbolFormat.D3Aqr ? 8 : 3); transform++) {
                var imageModules = transform == 1 ? CodeGlyphX.Internal.BitMatrixTransforms.Rotate90(modules)
                    : transform == 2 ? CodeGlyphX.Internal.BitMatrixTransforms.MirrorHorizontal(modules) : modules;
                if (format == SymbolFormat.D3Aqr) {
                    imageModules = transform >= 4 ? CodeGlyphX.Internal.BitMatrixTransforms.MirrorHorizontal(modules) : modules;
                    for (var turn = 0; turn < transform % 4; turn++) imageModules = CodeGlyphX.Internal.BitMatrixTransforms.Rotate90(imageModules);
                }
                var render = new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 10 };
                if (transform == 1) { render.Foreground = new Rgba32(255, 255, 255); render.Background = new Rgba32(0, 0, 0); }
                var png = MatrixPngRenderer.Render(imageModules, render);
                var result = SymbolScanner.Scan(png, Options(format));
                Assert.True(result.IsSuccess, $"{file}/{bcid}/{count}/transform{transform}: {width}x{height} {result.Status}/{result.CompletionReason}");
                var hit = Assert.Single(result.Symbols);
                Assert.Equal(expected.Text, hit.Text);
                Assert.Equal(expected.PayloadProfile, hit.PayloadProfile);
                Assert.Equal(expected.Metadata?.GetType(), hit.Metadata?.GetType());
                if (format == SymbolFormat.D3Aqr) {
                    var metadata = Assert.IsType<D3AqrSymbolMetadata>(hit.Metadata);
                    var expectedMetadata = Assert.IsType<D3AqrSymbolMetadata>(expected.Metadata);
                    Assert.Equal(expectedMetadata.Category, metadata.Category);
                    Assert.Equal(expectedMetadata.HostRatio, metadata.HostRatio);
                    Assert.Equal(expectedMetadata.Host.Version, metadata.Host.Version);
                    Assert.NotNull(hit.Geometry);
                    Assert.Equal(new SymbolBounds(30, 30, width * 3, height * 3), hit.Geometry!.Bounds);
                    Assert.Equal((transform % 4 * 90 + (transform >= 4 ? 180 : 0)) % 360, hit.Geometry.RotationDegrees);
                    Assert.Equal(transform >= 4, hit.IsMirrored);
                    var calibration = Options(format);
                    calibration.Region = new ImageRegion(30, 30, width * 3, height * 3);
                    calibration.SamplingGrid = new ImageSamplingGrid(width, height, transform % 4 * 90);
                    var calibrated = Assert.Single(SymbolScanner.Scan(png, calibration).Symbols);
                    Assert.Equal(expected.Text, calibrated.Text);
                    Assert.Equal(expected.PayloadProfile, calibrated.PayloadProfile);
                    Assert.Equal(metadata.Category, Assert.IsType<D3AqrSymbolMetadata>(calibrated.Metadata).Category);
                    Assert.Equal(hit.Geometry.Bounds, calibrated.Geometry!.Bounds);
                }
            }
            count++;
        }
        Assert.True(count > 0);
    }

    [Fact]
    public void RawCollationMarks_PreserveWhiteDataCellsWithExplicitCalibration() {
        const string expected = "001109900";
        var barcode = BarcodeEncoder.Encode(BarcodeType.Flattermarken, expected);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 0, HeightModules = 12
        }, out var width, out var height, out _);
        var options = Options(SymbolFormat.Flattermarken);
        options.SamplingGrid = new ImageSamplingGrid(expected.Length * 9, 1);
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
        options.SamplingGrid = null;
        Assert.Empty(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols);
    }

    private static ScanOptions Options(SymbolFormat format) => new() {
        Formats = new[] { format }, TimeoutMilliseconds = 10000, MaxSymbols = 1, EnableTileScan = false
    };

    private static byte[] Resource(string name) {
        using var stream = typeof(SpecialistImageScannerTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures." + name);
        Assert.NotNull(stream);
        using var output = new MemoryStream(); stream!.CopyTo(output); return output.ToArray();
    }
}

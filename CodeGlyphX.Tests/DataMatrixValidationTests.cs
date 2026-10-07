using System;
using System.IO;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataMatrixValidationTests {
    [Fact]
    public void EmptyGrid_IsNotADataMatrixEvenWhenZeroCodewordsPassEcc() {
        AssertRejected(new BitMatrix(24, 24));
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(32, 32)]
    [InlineData(8, 48)]
    public void MissingRegionFinder_IsRejectedWithIntactDataAndEcc(int rows, int cols) {
        var modules = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions { Rows = rows, Columns = cols });
        Assert.True(DataMatrixSymbolInfo.TryGetForSize(rows, cols, out var size));
        // Damage only a finder edge in the final region, including internal
        // region boundaries in multi-region square and DMRE symbols.
        var left = cols - size.RegionTotalCols;
        var top = rows - size.RegionTotalRows;
        for (var y = 0; y < size.RegionTotalRows; y++) modules[left, top + y] = false;

        AssertRejected(modules);
    }

    [Fact]
    public void SmallFinderSamplingErrors_AndCorrectableDataDamage_AreAccepted() {
        var modules = DataMatrixEncoder.Encode("PART-0042");
        modules[0, 7] = !modules[0, 7];
        modules[7, 0] = !modules[7, 0];
        modules[modules.Width - 1, 7] = !modules[modules.Width - 1, 7];
        modules[7, modules.Height - 1] = !modules[7, modules.Height - 1];
        modules[2, 2] = !modules[2, 2];

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(modules, out var decoded));
        Assert.Equal("PART-0042", decoded.Text);
    }

    [Theory]
    [InlineData(0, 129, 71)]
    [InlineData(242, 129, 71)]
    [InlineData(255, 129, 71)]
    [InlineData(254, 66, 129)]
    [InlineData(235, 129, 71)]
    [InlineData(241, 0, 129)]
    [InlineData(66, 67, 235)]
    [InlineData(66, 67, 231)]
    public void IllegalAsciiCodeword_IsRejectedDespiteValidFramingAndEcc(byte a, byte b, byte c) {
        AssertRejected(CreateTenByTen(a, b, c));
    }

    [Theory]
    [InlineData(230)] // C40
    [InlineData(239)] // Text
    [InlineData(238)] // X12
    public void InvalidPackedTriplet_IsRejectedAfterAsciiLatch(byte latch) {
        AssertRejected(CreateTenByTen(latch, 0, 0));
    }

    [Theory]
    [InlineData(66, 129, 0, "A")]
    [InlineData(235, 128, 129, "\u00ff")]
    [InlineData(66, 67, 254, "AB")]
    public void LegalAsciiTransitions_AndIgnoredPadding_RemainAccepted(byte a, byte b, byte c, string expected) {
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(CreateTenByTen(a, b, c), out var decoded));
        Assert.Equal(expected, decoded.Text);
    }

    [Theory]
    [InlineData(DataMatrixEncodingMode.Ascii)]
    [InlineData(DataMatrixEncodingMode.Base256)]
    public void BinaryZeroAnd255_RemainValidPayloadBytes(DataMatrixEncodingMode mode) {
        var symbol = DataMatrixCode.EncodeBytes(new byte[] { 0, 255, 128, 129, 242, 254 }, mode);

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("\0\u00ff\u0080\u0081\u00f2\u00fe", decoded.Text);
    }

    [Fact]
    public void Base256ZeroLengthField_ConsumesRemainingBinaryData() {
        // Base256 latch, randomized length zero at position 2, and binary
        // zero randomized at position 3. The length is not an empty payload.
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(CreateTenByTen(231, 44, 193), out var decoded));
        Assert.Equal("\0", decoded.Text);
        AssertRejected(CreateTenByTen(231, 46, 193)); // Declares two bytes but only one remains.
    }

    [Theory]
    [InlineData(DataMatrixEncodingMode.Auto)]
    [InlineData(DataMatrixEncodingMode.Base256)]
    [InlineData(DataMatrixEncodingMode.Ascii)]
    public void EmptyBytePayload_RemainsEmptyAcrossModuleAndImageEntryPoints(DataMatrixEncodingMode mode) {
        AssertEmptyPayload(DataMatrixCode.EncodeBytes(Array.Empty<byte>(), mode).Modules);
    }

    [Fact]
    public void EmptyStringPayload_InBase256_RemainsEmpty() {
        AssertEmptyPayload(DataMatrixCode.Encode(string.Empty, DataMatrixEncodingMode.Base256).Modules);
    }

    [Theory]
    [InlineData(DataMatrixEncodingMode.Auto)]
    [InlineData(DataMatrixEncodingMode.Base256)]
    public void EmptyBinarySegment_PreservesControlMetadata(DataMatrixEncodingMode mode) {
        var symbol = DataMatrixCode.EncodeBytes(Array.Empty<byte>(), new DataMatrixEncodingOptions {
            Mode = mode, ReaderProgramming = true, EciAssignmentNumber = 26
        });

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Empty(decoded.Text);
        Assert.True(decoded.ReaderProgramming);
        Assert.Equal(26, Assert.Single(decoded.EciAssignments));
        var pixels = MatrixPngRenderer.RenderPixels(symbol.Modules,
            new MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 }, out var width, out var height, out var stride);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, PixelFormat.Rgba32, out decoded));
        Assert.Empty(decoded.Text);
        Assert.True(decoded.ReaderProgramming);
        Assert.Equal(26, Assert.Single(decoded.EciAssignments));
    }

    [Fact]
    public void DamagedClockTracks_RetainTheValidOriginalPixelGrid() {
        var modules = DataMatrixEncoder.Encode("ORDER-1042", new DataMatrixEncodingOptions { Rows = 14, Columns = 14 });
        modules[7, 0] = !modules[7, 0];
        modules[13, 7] = !modules[13, 7];

        Assert.True(DataMatrixDecoder.TryDecode(modules, out var text));
        Assert.Equal("ORDER-1042", text);
        AssertPixelPayload(modules, 4, text);
        AssertPngPayload(MatrixPngRenderer.Render(modules, new MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 }), text);
    }

    [Fact]
    public void OnePixelModules_DoNotInferAnInteriorDataGrid() {
        foreach (var size in new[] { 12, 14, 16, 18, 20, 22, 24, 26 }) {
            foreach (var text in new[] { "A", "AB", "ABC", "42", "TEST", "abc", "0", "Z", "1", "X", "Hi", "12" }) {
                var modules = DataMatrixEncoder.Encode(text, new DataMatrixEncodingOptions { Rows = size, Columns = size });
                AssertPixelPayload(modules, 1, text);
            }
        }
    }

    [Fact]
    public void FractionalPitch_CanRecoverWhenTheIntegerEstimateIsAnotherSupportedSize() {
        var modules = DataMatrixEncoder.Encode("PART-0042", new DataMatrixEncodingOptions { Rows = 16, Columns = 16 });
        // The 72-pixel bounds contain 16 modules at pitch 4.5. A shortest run
        // of four pixels estimates 18 modules, which is also a supported size.
        const double pitch = 4.5;
        const int quietZone = 2;
        var width = (int)((modules.Width + quietZone * 2) * pitch);
        var height = (int)((modules.Height + quietZone * 2) * pitch);
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var col = (int)Math.Floor((x + 0.5) / pitch) - quietZone;
                var row = (int)Math.Floor((y + 0.5) / pitch) - quietZone;
                var dark = col >= 0 && col < modules.Width && row >= 0 && row < modules.Height && modules[col, row];
                var value = dark ? (byte)0 : (byte)255;
                var p = (y * width + x) * 4;
                pixels[p] = pixels[p + 1] = pixels[p + 2] = value;
                pixels[p + 3] = 255;
            }
        }

        var decoded = AssertPixelPayload(pixels, width, height, width * 4, "PART-0042");
        Assert.Equal(16, decoded.Rows);
        Assert.Equal(16, decoded.Columns);
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.DataMatrix }, TimeoutMilliseconds = TestBudget.Adjust(5000)
        });
        Assert.Equal(ScanCompletionReason.Completed, scan.CompletionReason);
        Assert.Equal("PART-0042", Assert.Single(scan.Symbols).Text);
    }

    [Fact]
    public void SpreadsheetGridAndMissingImagePlaceholders_DoNotProduceTileHits() {
        var png = ReadFixture("data-matrix-spreadsheet-placeholders.png");
        var result = SymbolScanner.Scan(png, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode, SymbolFormat.Code128, SymbolFormat.DataMatrix },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.NoSymbolFound, result.Status);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        Assert.Empty(result.Symbols);
    }

    [Theory]
    [InlineData("data-matrix-fractional-small.png")]
    [InlineData("data-matrix-fractional-transparent-edge.png")]
    public void FractionalModulePitch_WithPaddedCanvas_DecodesThroughImageAndScanner(string fixture) {
        AssertPngPayload(ReadFixture(fixture), "ORDER-1042");
    }

    private static void AssertPngPayload(byte[] png, string expected) {
        Assert.True(DataMatrixCode.TryDecodePng(png, out var text, out var diagnostics), diagnostics.Failure);
        Assert.Equal(expected, text);
        Assert.True(diagnostics.Success);
        Assert.Null(diagnostics.Failure);

        var result = SymbolScanner.Scan(png, new ScanOptions {
            Formats = new[] { SymbolFormat.DataMatrix },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });
        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
    }

    private static void AssertEmptyPayload(BitMatrix modules) {
        Assert.True(DataMatrixDecoder.TryDecode(modules, out var text));
        Assert.Empty(text);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(modules, out var decoded));
        Assert.Empty(decoded.Text);
        AssertPixelPayload(modules, 4, string.Empty);
        AssertPngPayload(MatrixPngRenderer.Render(modules, new MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 }), string.Empty);
    }

    private static void AssertPixelPayload(BitMatrix modules, int moduleSize, string expected) {
        var pixels = MatrixPngRenderer.RenderPixels(modules,
            new MatrixPngRenderOptions { ModuleSize = moduleSize, QuietZone = 2 }, out var width, out var height, out var stride);
        AssertPixelPayload(pixels, width, height, stride, expected);
    }

    private static DataMatrixDecoded AssertPixelPayload(byte[] pixels, int width, int height, int stride, string expected) {
        Assert.True(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var text),
            $"Failed to decode {width}x{height} pixels as '{expected}'.");
        Assert.Equal(expected, text);
        Assert.True(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out text, out var diagnostics), diagnostics.Failure);
        Assert.Equal(expected, text);
        Assert.True(diagnostics.Success);
        Assert.Null(diagnostics.Failure);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(expected, decoded.Text);
        return decoded;
    }

    private static void AssertRejected(BitMatrix modules) {
        Assert.False(DataMatrixDecoder.TryDecode(modules, out var text));
        Assert.Empty(text);
        Assert.False(DataMatrixDecoder.TryDecodeDetailed(modules, out _));
        Assert.False(SymbolDecoder.TryDecode(modules, out _, SymbolFormat.DataMatrix));
    }

    private static BitMatrix CreateTenByTen(byte a, byte b, byte c) {
        var data = new[] { a, b, c };
        var ecc = DataMatrixReedSolomon.ComputeRemainder(data, DataMatrixReedSolomon.ComputeDivisor(5));
        var codewords = new byte[8];
        Array.Copy(data, codewords, 3);
        Array.Copy(ecc, 0, codewords, 3, 5);
        var region = DataMatrixPlacement.PlaceCodewords(codewords, 8, 8);
        var matrix = new BitMatrix(10, 10);
        for (var y = 0; y < 10; y++) {
            matrix[0, y] = true;
            matrix[9, y] = (y & 1) != 0;
        }
        for (var x = 0; x < 10; x++) {
            matrix[x, 0] = (x & 1) == 0;
            matrix[x, 9] = true;
        }
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++) matrix[x + 1, y + 1] = region[x, y];
        return matrix;
    }

    private static byte[] ReadFixture(string name) {
        using var stream = typeof(DataMatrixValidationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.DataMatrix." + name);
        Assert.NotNull(stream);
        using var copy = new MemoryStream();
        stream!.CopyTo(copy);
        return copy.ToArray();
    }
}

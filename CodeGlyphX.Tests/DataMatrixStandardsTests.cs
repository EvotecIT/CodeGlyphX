using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DataMatrixStandardsTests {
    public static IEnumerable<object[]> RectangularSymbols => new[] {
        Symbol(8, 18, 1, 1, 5, 7, DataMatrixSymbolFamily.OriginalRectangular),
        Symbol(8, 32, 1, 2, 10, 11, DataMatrixSymbolFamily.OriginalRectangular),
        Symbol(12, 26, 1, 1, 16, 14, DataMatrixSymbolFamily.OriginalRectangular),
        Symbol(12, 36, 1, 2, 22, 18, DataMatrixSymbolFamily.OriginalRectangular),
        Symbol(16, 36, 1, 2, 32, 24, DataMatrixSymbolFamily.OriginalRectangular),
        Symbol(16, 48, 1, 2, 49, 28, DataMatrixSymbolFamily.OriginalRectangular),

        Symbol(8, 48, 1, 2, 18, 15, DataMatrixSymbolFamily.Dmre),
        Symbol(8, 64, 1, 4, 24, 18, DataMatrixSymbolFamily.Dmre),
        Symbol(8, 80, 1, 4, 32, 22, DataMatrixSymbolFamily.Dmre),
        Symbol(8, 96, 1, 4, 38, 28, DataMatrixSymbolFamily.Dmre),
        Symbol(8, 120, 1, 6, 49, 32, DataMatrixSymbolFamily.Dmre),
        Symbol(8, 144, 1, 6, 63, 36, DataMatrixSymbolFamily.Dmre),
        Symbol(12, 64, 1, 4, 43, 27, DataMatrixSymbolFamily.Dmre),
        Symbol(12, 88, 1, 4, 64, 36, DataMatrixSymbolFamily.Dmre),
        Symbol(16, 64, 1, 4, 62, 36, DataMatrixSymbolFamily.Dmre),
        Symbol(20, 36, 1, 2, 44, 28, DataMatrixSymbolFamily.Dmre),
        Symbol(20, 44, 1, 2, 56, 34, DataMatrixSymbolFamily.Dmre),
        Symbol(20, 64, 1, 4, 84, 42, DataMatrixSymbolFamily.Dmre),
        Symbol(22, 48, 1, 2, 72, 38, DataMatrixSymbolFamily.Dmre),
        Symbol(24, 48, 1, 2, 80, 41, DataMatrixSymbolFamily.Dmre),
        Symbol(24, 64, 1, 4, 108, 46, DataMatrixSymbolFamily.Dmre),
        Symbol(26, 40, 1, 2, 70, 38, DataMatrixSymbolFamily.Dmre),
        Symbol(26, 48, 1, 2, 90, 42, DataMatrixSymbolFamily.Dmre),
        Symbol(26, 64, 1, 4, 118, 50, DataMatrixSymbolFamily.Dmre)
    };

    public static IEnumerable<object[]> RectangularSizes => RectangularSymbols.Select(symbol => new[] { symbol[0], symbol[1] });

    // Row-major module hashes generated with bwip-js 4.11.2 / BWIPP 2026-05-28
    // using the explicit version and the ASCII payload "A".
    public static IEnumerable<object[]> IndependentConformanceFixtures => new[] {
        Fixture(8, 18, "9492d8bb6e552d8a1b9c8d3c90a61ebdc952d053fba2e6370b1f2f66857f14a5"),
        Fixture(8, 32, "7e108d6ee91f4933d0e26032059268d6a52e6d9e51d360f28eabd37dc4317f30"),
        Fixture(12, 26, "5377466eeef0fb193df68230ad6fd175772ec14d1f65313b71bd59d58000b0c0"),
        Fixture(12, 36, "7d73e8a21a25e6a373489c6f4f72f1fec51eb8bce8a70d4b267a152cb9f4c02d"),
        Fixture(16, 36, "eb358c283f6bc7f5eb57188bd2784f18608a993a100788bf159968fb16876ed9"),
        Fixture(16, 48, "3309fa7a6d3c7ea70a440a3f1bf5bba08cf8c42e7305296016865655ebe2d13f"),
        Fixture(8, 48, "0dca2b284fb9b0735400312ce03aa867fb455b360823a9ff91bf6257e6c786bb"),
        Fixture(8, 64, "68a46200b0c843a4cd599cb03d46301c7e8480413af9b604c34309b3891bd030"),
        Fixture(8, 80, "5e257d8e616971a872115b436832bd529570497974bd4cf4458afe897c866eb7"),
        Fixture(8, 96, "b5e1fd5c65f41cfa36db0096b4520c5e8745f6adea7152c6814f07ac317032af"),
        Fixture(8, 120, "87da2bcf28b03af1a9b634b3ebe3ac2bc181f75cd60cf08c33872158a08ec4b3"),
        Fixture(8, 144, "b39a5b152c9f6de681f208768ad59310f955cdefb1d1749b4c6ecfe985acf14e"),
        Fixture(12, 64, "2d5d40ff842e51230562a10b2dbc24c433aa151c0c163bc829e6190cb5074cd2"),
        Fixture(12, 88, "238df9a26f9ca550b80e3d9544d077dde959197942d6a8ad5359f212cfcdd6c9"),
        Fixture(16, 64, "4dcdec7adbccdb63581e0911c84e4192dc74d627efc81368d2bd1867fd29ab5c"),
        Fixture(20, 36, "3b6cf66f54aaa8849210df8d46b2f9608181989e9fa8c5214ced63d458819baa"),
        Fixture(20, 44, "74a7717c0ba24804ea0ab5bc83f0f23e6cb22edca5d9502b53813c17a2f3d937"),
        Fixture(20, 64, "53ea5763e31c642d999a1d9b43facc8d9bcec04b790eb4be41ad5c24445ad9c5"),
        Fixture(22, 48, "20191b104ac77672cf7a52e0ff5f98f21ffa6e399d3d55760a80f54dd966d0fb"),
        Fixture(24, 48, "e158653f67f4ff62e54346a0de4e8146348ca6aa7f1172c573c581f581179dc3"),
        Fixture(24, 64, "0d05a131aef19f47eb48284ec462f44fc252f74e415a31f516ed31417f19c2a2"),
        Fixture(26, 40, "a8ecee425784b23432f5bc2ef7ff010c9fa6f4ea3d386fd74803e02779910307"),
        Fixture(26, 48, "f5b91337bfea09d4ee2b46bad67a11d767fd0a9c51a875752057a2863dbacb45"),
        Fixture(26, 64, "7e436829ed7f385b4e72ddfb8d9bd50644eff13d4263bbbf169b14ed4ba193a2")
    };

    [Fact]
    public void SymbolCatalog_CoversCurrentEcc200AndDmreModels() {
        Assert.Equal(48, DataMatrixSymbolInfo.All.Count);
        Assert.Equal(24, DataMatrixSymbolInfo.All.Count(symbol => symbol.Family == DataMatrixSymbolFamily.Square));
        Assert.Equal(6, DataMatrixSymbolInfo.All.Count(symbol => symbol.Family == DataMatrixSymbolFamily.OriginalRectangular));
        Assert.Equal(18, DataMatrixSymbolInfo.All.Count(symbol => symbol.Family == DataMatrixSymbolFamily.Dmre));
    }

    [Theory]
    [MemberData(nameof(RectangularSymbols))]
    public void RectangularCatalog_MatchesStandardMetrics(
        int rows,
        int columns,
        int regionRows,
        int regionColumns,
        int dataCodewords,
        int eccCodewords,
        int family) {
        Assert.True(DataMatrixSymbolInfo.TryGetForSize(rows, columns, out var symbol));
        Assert.Equal(regionRows, symbol.RegionRows);
        Assert.Equal(regionColumns, symbol.RegionCols);
        Assert.Equal(dataCodewords, symbol.DataCodewords);
        Assert.Equal(eccCodewords, symbol.EccCodewords);
        Assert.Equal((DataMatrixSymbolFamily)family, symbol.Family);
        Assert.Single(symbol.DataBlockSizes);
        Assert.Equal(dataCodewords, symbol.DataBlockSizes[0]);
        Assert.Equal(eccCodewords, symbol.EccBlockSize);
    }

    [Theory]
    [MemberData(nameof(RectangularSizes))]
    public void EveryRectangularModel_EncodesAndDecodes(int rows, int columns) {
        var matrix = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions { Rows = rows, Columns = columns });

        Assert.Equal(columns, matrix.Width);
        Assert.Equal(rows, matrix.Height);
        Assert.True(DataMatrixDecoder.TryDecode(matrix, out var decoded));
        Assert.Equal("A", decoded);
    }

    [Theory]
    [MemberData(nameof(RectangularSizes))]
    public void EveryRectangularModel_RoundTripsRenderedPixels(int rows, int columns) {
        var matrix = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions { Rows = rows, Columns = columns });
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        Assert.True(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal("A", decoded);
    }

    [Fact]
    public void DmrePng_DetailedDecodePreservesGs1AndSymbolMetadata() {
        var payload = "0109501101020917" + Gs1.GroupSeparator + "10LOT42";
        var encoding = new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Ascii,
            Shape = DataMatrixShape.Dmre,
            IsGs1 = true
        };
        var png = DataMatrixCode.Render(
            payload,
            Rendering.OutputFormat.Png,
            encoding,
            new MatrixOptions { ModuleSize = 5, QuietZone = 2 }).Data;

        Assert.True(DataMatrixCode.TryDecodePngDetailed(png, out var decoded));
        Assert.Equal(payload, decoded.Text);
        Assert.True(decoded.IsGs1);
        Assert.True(decoded.IsDmre);
        Assert.NotEqual(decoded.Rows, decoded.Columns);
    }

    [Theory]
    [MemberData(nameof(IndependentConformanceFixtures))]
    public void EveryRectangularModel_MatchesIndependentBwippFixture(int rows, int columns, string expectedHash) {
        var matrix = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions { Rows = rows, Columns = columns });
        Assert.Equal(expectedHash, ComputeModuleHash(matrix));
    }

    [Fact]
    public void Gs1_Fnc1HeaderAndSeparators_RoundTripWithMetadata() {
        var elementString = "0109501101020917" + Gs1.GroupSeparator + "10ABC123";
        var options = new DataMatrixEncodingOptions { Mode = DataMatrixEncodingMode.Ascii, Rows = 26, Columns = 26 };
        var matrix = DataMatrixEncoder.EncodeGs1(elementString, options);

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.Equal(elementString, decoded.Text);
        Assert.True(decoded.IsGs1);
        Assert.Equal("9a39b9bd054c55a97b816cd7ab034a25f2b1f9b14e44e711f19947b906dbffa9", ComputeModuleHash(matrix));
    }

    [Theory]
    [InlineData(DataMatrixEncodingMode.C40, 'A', 230)]
    [InlineData(DataMatrixEncodingMode.Text, 'a', 239)]
    [InlineData(DataMatrixEncodingMode.Auto, 'A', 230)]
    [InlineData(DataMatrixEncodingMode.Auto, 'a', 239)]
    public void Gs1_C40TextSeparators_UseTheStandardShift2Fnc1Value(
        DataMatrixEncodingMode mode,
        char compactCharacter,
        int latch) {
        var payload = new string(compactCharacter, 10) + Gs1.GroupSeparator;
        var matrix = DataMatrixEncoder.EncodeGs1(payload, new DataMatrixEncodingOptions {
            Mode = mode,
            Rows = 16,
            Columns = 16
        });

        var codewords = ReadRawCodewords(matrix);
        // ISO/IEC 16022 C40/Text FNC1 is Shift 2 followed by value 27; the last packed pair is 87,196.
        Assert.Equal(
            new byte[] { 232, (byte)latch, 89, 191, 89, 191, 89, 191, 87, 196, 254 },
            codewords.Take(11));
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.True(decoded.IsGs1);
        Assert.Equal(payload, decoded.Text);
    }

    [Theory]
    [InlineData(DataMatrixEncodingMode.C40)]
    [InlineData(DataMatrixEncodingMode.Text)]
    [InlineData(DataMatrixEncodingMode.X12)]
    [InlineData(DataMatrixEncodingMode.Edifact)]
    public void EmptyForcedCompaction_FallsBackToAsciiPadding(DataMatrixEncodingMode mode) {
        var matrix = DataMatrixEncoder.Encode(string.Empty, mode);

        Assert.Equal(129, ReadRawCodewords(matrix)[0]);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.Equal(string.Empty, decoded.Text);
    }

    [Theory]
    [InlineData(126, "15ab41a02f81f88fb3ed13adffe43ed3ef753bcab34a47874d5ea55a64017ec8")]
    [InlineData(127, "267411b814ad1236c294d420104afe3ff8b5f977d572b4f9f83917f61e4a84ac")]
    [InlineData(16382, "cc6e06bd2059b2fdda7d64e5d9be1a4b1f2727452488586b0ad579d4560f012e")]
    [InlineData(16383, "a4f4cec171e116700e74e838e64ee6955425b67a5484601587a5b21d15f8486f")]
    [InlineData(999999, "438b471113aeb64ca1c5a6fb697a0b20cb662db4477f612809a2768c41d88241")]
    public void Eci_AllWireLengthBoundaries_RoundTripMetadataAndMatchBwipp(int assignmentNumber, string expectedHash) {
        var options = new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Ascii,
            EciAssignmentNumber = assignmentNumber,
            Rows = 14,
            Columns = 14
        };
        var matrix = DataMatrixEncoder.Encode("A", options);

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.Equal("A", decoded.Text);
        Assert.Equal(new[] { assignmentNumber }, decoded.EciAssignments);
        Assert.Equal(expectedHash, ComputeModuleHash(matrix));
    }

    [Fact]
    public void Eci_Utf8Base256_UsesAbsoluteRandomizationPositions() {
        var options = new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Base256,
            EciAssignmentNumber = 26
        };
        var matrix = DataMatrixEncoder.Encode("Grüße", options);

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.Equal("Grüße", decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
    }

    [Fact]
    public void Eci_AsciiFixture_MatchesIndependentBwippOutput() {
        var matrix = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Ascii,
            EciAssignmentNumber = 26,
            Rows = 14,
            Columns = 14
        });

        Assert.Equal("3147ed8cd1c668edf6de0c6505bdb76f4a1219b416c426cff4bacd21d6bf9138", ComputeModuleHash(matrix));
    }

    [Fact]
    public void Macro05_CompressesEnvelopeAndRoundTripsMetadata() {
        var matrix = DataMatrixEncoder.EncodeMacro05("ABC", new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Ascii,
            Rows = 14,
            Columns = 14
        });

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.Equal("[)>\u001E05\u001DABC\u001E\u0004", decoded.Text);
        Assert.Equal(DataMatrixMacro.Macro05, decoded.Macro);
        Assert.Equal("284ad92c1ac7094422c27394590b31d30d00d032bea434f156c3e6dfd3dc95fe", ComputeModuleHash(matrix));
    }

    [Fact]
    public void MacroEnvelope_IsDetectedAndCompressedAutomatically() {
        const string envelope = "[)>\u001E06\u001DABC\u001E\u0004";
        var options = new DataMatrixEncodingOptions { Mode = DataMatrixEncodingMode.Ascii, Rows = 14, Columns = 14 };

        var automatic = DataMatrixEncoder.Encode(envelope, options);
        var explicitMacro = DataMatrixEncoder.EncodeMacro06("ABC", options);

        Assert.Equal(ComputeModuleHash(explicitMacro), ComputeModuleHash(automatic));
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(automatic, out var decoded));
        Assert.Equal(envelope, decoded.Text);
        Assert.Equal(DataMatrixMacro.Macro06, decoded.Macro);
    }

    [Fact]
    public void ReaderProgramming_RoundTripsMetadataAndMatchesIndependentBwippOutput() {
        var matrix = DataMatrixEncoder.Encode("ABC", new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Ascii,
            ReaderProgramming = true,
            Rows = 14,
            Columns = 14
        });

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.Equal("ABC", decoded.Text);
        Assert.True(decoded.ReaderProgramming);
        Assert.Equal("9abdccabab9f6594bc89d3c1b6ff7f32f54b6fbff8b04e30ca0b304ed5e7a55b", ComputeModuleHash(matrix));
    }

    [Fact]
    public void StructuredAppend_PreservesSequenceAndFileIdentifiers() {
        var symbols = DataMatrixEncoder.EncodeStructuredAppend(
            new[] { "A", "B", "C" },
            fileId1: 7,
            fileId2: 9,
            options: new DataMatrixEncodingOptions { Mode = DataMatrixEncodingMode.Ascii });

        Assert.Equal(3, symbols.Length);
        for (var i = 0; i < symbols.Length; i++) {
            Assert.True(DataMatrixDecoder.TryDecodeDetailed(symbols[i], out var decoded));
            Assert.Equal(((char)('A' + i)).ToString(), decoded.Text);
            Assert.Equal(i + 1, decoded.StructuredAppend?.Index);
            Assert.Equal(3, decoded.StructuredAppend?.Total);
            Assert.Equal(7, decoded.StructuredAppend?.FileId1);
            Assert.Equal(9, decoded.StructuredAppend?.FileId2);
        }
    }

    [Fact]
    public void StructuredAppend_WireHeader_MatchesIndependentBwippOutput() {
        var matrix = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Ascii,
            StructuredAppend = new DataMatrixStructuredAppend(2, 15, 7, 9),
            Rows = 14,
            Columns = 14
        });

        Assert.Equal("62883490d3c49b81a42512882ceb0f49c036b7e1383c6edf4011bdbda9e4ebbe", ComputeModuleHash(matrix));
    }

    [Fact]
    public void ControlOptions_RejectForbiddenCombinations() {
        Assert.Throws<ArgumentException>(() => DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions {
            IsGs1 = true,
            ReaderProgramming = true
        }));
        Assert.Throws<ArgumentException>(() => DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions {
            Macro = DataMatrixMacro.Macro06,
            StructuredAppend = new DataMatrixStructuredAppend(1, 2)
        }));
    }

    [Fact]
    public void AutoPlanner_MixesEncodationsAndUsesASmallerSymbolThanForcedModes() {
        const string payload = "HELLO-THIS-IS-UPPERCASE-lowercase-lowercase-1234567890";

        var optimized = DataMatrixEncoder.Encode(payload);
        var ascii = DataMatrixEncoder.Encode(payload, DataMatrixEncodingMode.Ascii);
        var c40 = DataMatrixEncoder.Encode(payload, DataMatrixEncodingMode.C40);
        var text = DataMatrixEncoder.Encode(payload, DataMatrixEncodingMode.Text);
        var base256 = DataMatrixEncoder.Encode(payload, DataMatrixEncodingMode.Base256);

        Assert.Equal((26, 26), (optimized.Height, optimized.Width));
        Assert.True(optimized.Width < ascii.Width);
        Assert.True(optimized.Width < c40.Width);
        Assert.True(optimized.Width < text.Width);
        Assert.True(optimized.Width < base256.Width);
        Assert.True(DataMatrixDecoder.TryDecode(optimized, out var decoded));
        Assert.Equal(payload, decoded);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("abc")]
    public void AutoPlanner_PreservesAsciiForShortSingleCasePayloads(string payload) {
        var matrix = DataMatrixEncoder.Encode(payload);

        Assert.Equal(10, matrix.Width);
        Assert.Equal(10, matrix.Height);
        Assert.True(DataMatrixDecoder.TryDecode(matrix, out var decoded));
        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void AutoPlanner_ShortUppercasePayloadFitsAnExactAsciiSizedSymbol() {
        const string payload = "ABCDE";
        var matrix = DataMatrixEncoder.Encode(payload, new DataMatrixEncodingOptions {
            Rows = 12,
            Columns = 12
        });

        Assert.True(DataMatrixDecoder.TryDecode(matrix, out var decoded));
        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void AutoPlanner_RoundTripsUnicodeIslandBetweenCompactAsciiRuns() {
        const string payload = "AAAAAAAAAAAA😀BBBBBBBBBBBB12345678901234567890";

        var matrix = DataMatrixEncoder.Encode(payload, new DataMatrixEncodingOptions { EciAssignmentNumber = 26 });

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.Equal(payload, decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
    }

    [Fact]
    public void AutoPlanner_PreservesGs1SeparatorAcrossMixedTextRuns() {
        var payload = "10UPPERCASE" + Gs1.GroupSeparator + "21lowercase1234567890";

        var matrix = DataMatrixEncoder.EncodeGs1(payload);

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(matrix, out var decoded));
        Assert.True(decoded.IsGs1);
        Assert.Equal(payload, decoded.Text);
    }

    [Fact]
    public void Edifact_UsesTheStandardSixBitCharacterMapping() {
        var matrix = DataMatrixEncoder.Encode("ABC^", new DataMatrixEncodingOptions {
            Mode = DataMatrixEncodingMode.Edifact,
            Rows = 14,
            Columns = 14
        });

        Assert.True(DataMatrixDecoder.TryDecode(matrix, out var decoded));
        Assert.Equal("ABC^", decoded);
        // BWIPP raw fixture: 240,4,32,222,124,0,0 (latch, ABC^, unlatch group).
        Assert.Equal("745c98a0bccf05c2d9911b85470f735ff0502f64f0d4d672a0f9136ecb206b9f", ComputeModuleHash(matrix));
        Assert.Throws<ArgumentException>(() => DataMatrixEncoder.Encode("_", DataMatrixEncodingMode.Edifact));
    }

    [Fact]
    public void ShapeSelection_PreservesSquareDefaultAndSelectsRequestedFamilies() {
        var defaultSymbol = DataMatrixEncoder.Encode("A");
        var originalRectangle = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions { Shape = DataMatrixShape.OriginalRectangular });
        var dmre = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions { Shape = DataMatrixShape.Dmre });
        var any = DataMatrixEncoder.Encode("A", new DataMatrixEncodingOptions { Shape = DataMatrixShape.Any });

        Assert.Equal((10, 10), (defaultSymbol.Height, defaultSymbol.Width));
        Assert.Equal((8, 18), (originalRectangle.Height, originalRectangle.Width));
        Assert.Equal((8, 48), (dmre.Height, dmre.Width));
        Assert.Equal((10, 10), (any.Height, any.Width));
    }

    [Fact]
    public void ExactSize_RejectsUnsupportedAndOverCapacityRequests() {
        var unsupported = new DataMatrixEncodingOptions { Rows = 10, Columns = 12 };
        var tooSmall = new DataMatrixEncodingOptions { Rows = 8, Columns = 18 };

        Assert.Throws<ArgumentException>(() => DataMatrixEncoder.Encode("A", unsupported));
        Assert.Throws<ArgumentException>(() => DataMatrixEncoder.Encode("ABCDEFGHIJ", tooSmall));
    }

    [Fact]
    public void ExactSize_RequiresRowsAndColumnsTogether() {
        var options = new DataMatrixEncodingOptions { Rows = 8 };

        Assert.Throws<ArgumentException>(() => DataMatrixEncoder.Encode("A", options));
    }

    [Fact]
    public void FluentBuilder_ExposesShapeAndExactSizeSelection() {
        var rectangle = DataMatrixCode.Create("A").WithShape(DataMatrixShape.Dmre).Encode();
        var exact = DataMatrixCode.Create("A").WithSize(12, 88).Encode();

        Assert.Equal((8, 48), (rectangle.Height, rectangle.Width));
        Assert.Equal((12, 88), (exact.Height, exact.Width));
    }

    private static object[] Symbol(
        int rows,
        int columns,
        int regionRows,
        int regionColumns,
        int dataCodewords,
        int eccCodewords,
        DataMatrixSymbolFamily family) {
        return new object[] { rows, columns, regionRows, regionColumns, dataCodewords, eccCodewords, (int)family };
    }

    private static object[] Fixture(int rows, int columns, string hash) {
        return new object[] { rows, columns, hash };
    }

    private static string ComputeModuleHash(BitMatrix matrix) {
        var modules = new StringBuilder(matrix.Width * matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) modules.Append(matrix[x, y] ? '1' : '0');
        }

        using var sha256 = SHA256.Create();
        return BitConverter.ToString(sha256.ComputeHash(Encoding.ASCII.GetBytes(modules.ToString())))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    private static byte[] ReadRawCodewords(BitMatrix matrix) {
        Assert.True(DataMatrixSymbolInfo.TryGetForSize(matrix.Height, matrix.Width, out var symbol));
        var dataRegion = new BitMatrix(symbol.DataRegionCols, symbol.DataRegionRows);
        for (var regionRow = 0; regionRow < symbol.RegionRows; regionRow++) {
            for (var regionColumn = 0; regionColumn < symbol.RegionCols; regionColumn++) {
                var sourceRow = regionRow * symbol.RegionTotalRows;
                var sourceColumn = regionColumn * symbol.RegionTotalCols;
                for (var y = 0; y < symbol.RegionDataRows; y++) {
                    for (var x = 0; x < symbol.RegionDataCols; x++) {
                        dataRegion[regionColumn * symbol.RegionDataCols + x, regionRow * symbol.RegionDataRows + y] =
                            matrix[sourceColumn + 1 + x, sourceRow + 1 + y];
                    }
                }
            }
        }
        return DataMatrixPlacement.ReadCodewords(dataRegion, symbol.CodewordCount);
    }
}

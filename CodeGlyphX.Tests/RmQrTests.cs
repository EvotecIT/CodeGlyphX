using System;
using System.Linq;
using System.Text;
using CodeGlyphX.RmQr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RmQrTests {
    private static readonly string[] ZintVersion20MediumFixture = {
        "FE AA AA EA AA AA BA AA AA B8",
        "82 38 F2 B9 4C 35 EF C7 75 C8",
        "BA 29 7E F2 09 74 B9 7F D6 98",
        "BA 4D 86 80 AF D9 20 2A B7 C0",
        "BA 5D F7 E6 BA 8C 7A A9 B3 A8",
        "82 76 01 BD A0 36 27 FE 09 60",
        "FE C7 43 42 F1 77 55 50 EC 48",
        "00 F6 85 8C 5D D5 E0 A5 1D 70",
        "FC B3 BA 53 09 80 FB 6E DB F8",
        "59 7E 65 9A 63 DA 26 3A 81 88",
        "B6 97 94 E1 B2 8F 7E 01 B7 A8",
        "A5 9D 8A B7 11 27 EF F5 DA 88",
        "EA AA AA EA AA AA BA AA AA F8"
    };

    [Theory]
    [InlineData(QrErrorCorrectionLevel.M)]
    [InlineData(QrErrorCorrectionLevel.H)]
    public void EveryVersion_RoundTripsNumericPayload(QrErrorCorrectionLevel ecc) {
        for (var version = 1; version <= 32; version++) {
            var code = RmQrCodeEncoder.EncodeNumeric("1", ecc, version, version);

            Assert.Equal(version, code.Version);
            Assert.Equal($"R{code.Height}x{code.Width}", code.VersionName);
            Assert.True(RmQrDecoder.TryDecode(code.Modules, out var decoded));
            Assert.Equal("1", decoded.Text);
            Assert.Equal(ecc, decoded.ErrorCorrectionLevel);
            Assert.Equal(version, decoded.Version);
        }
    }

    [Fact]
    public void AlphanumericAndBinaryModes_RoundTrip() {
        var alpha = RmQrCodeEncoder.EncodeAlphanumeric("RMQR-42", minimumVersion: 20, maximumVersion: 20);
        Assert.True(RmQrDecoder.TryDecode(alpha.Modules, out var alphaDecoded));
        Assert.Equal("RMQR-42", alphaDecoded.Text);

        var bytes = new byte[] { 0, 1, 2, 127, 128, 255 };
        var binary = RmQrCodeEncoder.EncodeBytes(bytes, new RmQrEncodingOptions {
            Mode = RmQrEncodingMode.Byte,
            MinimumVersion = 20,
            MaximumVersion = 20
        });
        Assert.True(RmQrDecoder.TryDecode(binary.Modules, out var binaryDecoded));
        Assert.Equal(bytes, binaryDecoded.Bytes);
    }

    [Fact]
    public void Utf8EciAndKanji_RoundTrip() {
        var utf8 = RmQrCodeEncoder.EncodeText("Zażółć", new RmQrEncodingOptions {
            Mode = RmQrEncodingMode.Byte,
            TextEncoding = QrTextEncoding.Utf8,
            MinimumVersion = 25,
            MaximumVersion = 25
        });
        Assert.True(RmQrDecoder.TryDecode(utf8.Modules, out var utf8Decoded));
        Assert.Equal("Zażółć", utf8Decoded.Text);
        Assert.Equal(26, utf8Decoded.EciAssignmentNumber);

        var kanji = RmQrCodeEncoder.EncodeKanji("漢字", minimumVersion: 20, maximumVersion: 20);
        Assert.True(RmQrDecoder.TryDecode(kanji.Modules, out var kanjiDecoded));
        Assert.Equal("漢字", kanjiDecoded.Text);
    }

    [Fact]
    public void ExplicitEci_SelectsItsMatchingTextEncoding() {
        var code = RmQrCodeEncoder.EncodeText("é", new RmQrEncodingOptions {
            Mode = RmQrEncodingMode.Byte,
            EciAssignmentNumber = 26,
            MinimumVersion = 20,
            MaximumVersion = 20
        });

        Assert.True(RmQrDecoder.TryDecode(code.Modules, out var decoded));
        Assert.Equal("é", decoded.Text);
        Assert.Equal(26, decoded.EciAssignmentNumber);
        Assert.Equal(Encoding.UTF8.GetBytes("é"), decoded.Bytes);
    }

    [Fact]
    public void UnknownTextEci_IsRejectedBeforeEncoding() {
        Assert.Throws<InvalidOperationException>(() => RmQrCodeEncoder.EncodeText("A", new RmQrEncodingOptions {
            Mode = RmQrEncodingMode.Byte,
            EciAssignmentNumber = 899
        }));
    }

    [Fact]
    public void Gs1Fnc1_RoundTripsValidatedElementString() {
        var code = RmQrCodeEncoder.EncodeGs1("(01)09506000134352(10)LOT42(17)300101");

        Assert.True(RmQrDecoder.TryDecode(code.Modules, out var decoded));
        Assert.True(decoded.IsGs1);
        Assert.Equal("010950600013435210LOT42" + Gs1.GroupSeparator + "17300101", decoded.Text);
        Assert.True(Gs1.Validate(decoded.Text).IsValid);
    }

    [Fact]
    public void ReedSolomon_CorrectsDamagedDataModules() {
        var code = RmQrCodeEncoder.EncodeAlphanumeric(
            "INDUSTRIAL-RMQR",
            QrErrorCorrectionLevel.H,
            minimumVersion: 25,
            maximumVersion: 25);
        var damaged = code.Modules.Clone();
        var function = new BitMatrix(damaged.Width, damaged.Height);
        RmQrMatrix.SetupFunctionPatterns(new BitMatrix(damaged.Width, damaged.Height), function);
        Assert.True(FlipFirstPlacedDataBit(damaged, function));
        Assert.True(RmQrDecoder.TryDecode(damaged, out var decoded));
        Assert.Equal("INDUSTRIAL-RMQR", decoded.Text);
    }

    [Fact]
    public void InvalidOptionsAndRandomMatrix_AreRejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => RmQrCodeEncoder.EncodeText(
            "A",
            new RmQrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.L }));
        Assert.Throws<ArgumentException>(() => RmQrCodeEncoder.EncodeNumeric("ABC"));
        Assert.False(RmQrDecoder.TryDecode(new BitMatrix(27, 11), out _));
        Assert.False(RmQrDecoder.TryDecode(new BitMatrix(28, 11), out _));
    }

    [Fact]
    public void AutomaticSelection_ChoosesSmallestFootprintThatFits() {
        var code = RmQrCodeEncoder.EncodeText(new string('A', 50));
        var allFitting = Enumerable.Range(1, 32)
            .Where(version => CanEncodeAtVersion(new string('A', 50), version))
            .Select(version => (Version: version, Area: GetArea(version)))
            .ToArray();

        Assert.NotEmpty(allFitting);
        Assert.Equal(allFitting.Min(item => item.Area), code.Width * code.Height);
    }

    [Fact]
    public void Version20Medium_MatchesIndependentZintModuleFixture() {
        // Zint backend 4e534c3a0982ac9f284f5035181c7032fa9f8c8f:
        // zint -b RMQR --vers=20 --secure=2 -d "RMQR-42" --filetype=TXT
        var code = RmQrCodeEncoder.EncodeAlphanumeric(
            "RMQR-42",
            QrErrorCorrectionLevel.M,
            minimumVersion: 20,
            maximumVersion: 20);

        Assert.Equal(77, code.Width);
        Assert.Equal(13, code.Height);
        var oracle = ParsePackedFixture(ZintVersion20MediumFixture, 77);
        Assert.True(RmQrDecoder.TryDecode(oracle, out var oracleDecoded));
        Assert.Equal("RMQR-42", oracleDecoded.Text);
        Assert.Equal(QrErrorCorrectionLevel.M, oracleDecoded.ErrorCorrectionLevel);
        AssertMatrixEqual(oracle, code.Modules);

        Assert.True(CodeGlyph.TryDecode(oracle, out var unified));
        Assert.Equal(CodeGlyphKind.RmQr, unified.Kind);
        Assert.Equal("RMQR-42", unified.Text);
        Assert.NotNull(unified.RmQr);
        Assert.Equal(20, unified.RmQr!.Version);
    }

    [Fact]
    public void CapabilityCatalog_AdvertisesModuleEncodingAndDecodingBoundaries() {
        var capability = SymbolCapabilities.Get(SymbolFormat.RmQrCode);

        Assert.True(capability.CanEncode);
        Assert.True(capability.CanDecodeModules);
        Assert.False(capability.CanScanImages);
        Assert.True(capability.Has(SymbolCapabilityFlags.EciEncode));
        Assert.True(capability.Has(SymbolCapabilityFlags.EciDecode));
        Assert.True(capability.Has(SymbolCapabilityFlags.Gs1Encode));
        Assert.True(capability.Has(SymbolCapabilityFlags.Gs1Decode));
    }

    [Theory]
    [InlineData(RmQrEncodingMode.Auto)]
    [InlineData(RmQrEncodingMode.Byte)]
    public void Utf8ByteMode_RejectsMalformedUtf16InsteadOfReplacingIt(RmQrEncodingMode mode) {
        Assert.Throws<ArgumentException>(() => RmQrCodeEncoder.EncodeText("\uD800", new RmQrEncodingOptions {
            Mode = mode,
            TextEncoding = QrTextEncoding.Utf8
        }));
    }

    private static bool CanEncodeAtVersion(string value, int version) {
        try {
            RmQrCodeEncoder.EncodeAlphanumeric(value, minimumVersion: version, maximumVersion: version);
            return true;
        } catch (ArgumentException) {
            return false;
        }
    }

    private static int GetArea(int version) {
        var one = RmQrCodeEncoder.EncodeNumeric("1", minimumVersion: version, maximumVersion: version);
        return one.Width * one.Height;
    }

    private static BitMatrix ParsePackedFixture(string[] rows, int width) {
        var matrix = new BitMatrix(width, rows.Length);
        for (var y = 0; y < rows.Length; y++) {
            var packed = rows[y].Split(' ').Select(value => Convert.ToByte(value, 16)).ToArray();
            for (var x = 0; x < width; x++) matrix[x, y] = (packed[x >> 3] & (0x80 >> (x & 7))) != 0;
        }
        return matrix;
    }

    private static void AssertMatrixEqual(BitMatrix expected, BitMatrix actual) {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        for (var y = 0; y < expected.Height; y++) {
            for (var x = 0; x < expected.Width; x++) Assert.Equal(expected[x, y], actual[x, y]);
        }
    }

    private static bool FlipFirstPlacedDataBit(BitMatrix modules, BitMatrix function) {
        var pair = 0;
        var y = modules.Height - 1;
        var upward = true;
        while (pair * 2 < modules.Width) {
            var x = modules.Width - 3 - pair * 2;
            if (!function[x + 1, y]) {
                modules[x + 1, y] = !modules[x + 1, y];
                return true;
            }
            if (!function[x, y]) {
                modules[x, y] = !modules[x, y];
                return true;
            }
            if (upward) {
                if (--y < 0) { pair++; y = 0; upward = false; }
            } else if (++y == modules.Height) {
                pair++;
                y = modules.Height - 1;
                upward = true;
            }
        }
        return false;
    }

}

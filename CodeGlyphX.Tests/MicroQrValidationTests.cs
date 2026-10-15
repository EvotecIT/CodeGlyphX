using System.IO;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class MicroQrValidationTests {
    [Theory]
    [InlineData(10, 10)] // First data bit.
    [InlineData(1, 9)] // Error-detection codeword bit.
    public void M1_RejectsDamagedCodewordsWithoutRepair(int x, int y) {
        var code = MicroQrCodeEncoder.EncodeNumeric("12345", minVersion: 1, maxVersion: 1);
        Assert.True(MicroQrDecoder.TryDecode(code.Modules, out var clean));
        Assert.Equal("12345", clean.Text);

        var damaged = code.Modules.Clone();
        damaged[x, y] = !damaged[x, y];

        Assert.False(MicroQrDecoder.TryDecode(damaged, out _));
    }

    [Theory]
    [InlineData(2, QrErrorCorrectionLevel.L, 1)]
    [InlineData(2, QrErrorCorrectionLevel.M, 2)]
    [InlineData(3, QrErrorCorrectionLevel.L, 2)]
    [InlineData(3, QrErrorCorrectionLevel.M, 4)]
    [InlineData(4, QrErrorCorrectionLevel.L, 3)]
    [InlineData(4, QrErrorCorrectionLevel.M, 5)]
    [InlineData(4, QrErrorCorrectionLevel.Q, 7)]
    public void LargerVersions_CorrectOnlyWithinTheSymbolCapacity(
        int version, QrErrorCorrectionLevel ecc, int capacity) {
        var code = MicroQrCodeEncoder.EncodeNumeric("12345", ecc, minVersion: version, maxVersion: version);
        var damaged = code.Modules.Clone();
        // One bit in each of eight distinct codewords, following the two-column
        // placement from the bottom right. M3 ends its first stripe halfway
        // through a codeword, so the fifth observation starts farther down.
        var positions = version == 2
            ? new[] { (12, 12), (12, 8), (12, 4) }
            : version == 3
                ? new[] { (14, 14), (14, 10), (14, 6), (14, 2), (12, 5) }
                : new[] { (16, 16), (16, 12), (16, 8), (16, 4), (14, 1), (14, 5), (14, 9), (14, 13) };
        for (var i = 0; i < capacity; i++) {
            var (x, y) = positions[i];
            damaged[x, y] = !damaged[x, y];
        }

        Assert.True(MicroQrDecoder.TryDecode(damaged, out var decoded));
        Assert.Equal(version, decoded.Version);
        Assert.Equal("12345", decoded.Text);

        var (extraX, extraY) = positions[capacity];
        damaged[extraX, extraY] = !damaged[extraX, extraY];
        Assert.False(MicroQrDecoder.TryDecode(damaged, out _));
    }

    [Fact]
    public void IndependentM1Image_PreservesValidDetectionOnlyPayload() {
        using var stream = typeof(MicroQrValidationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.MicroQr.independent-m1.png");
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);

        Assert.True(MicroQrDecoder.TryDecodeImage(buffer.ToArray(), out var decoded));
        Assert.Equal(1, decoded.Version);
        Assert.Equal("12345", decoded.Text);
    }

    [Fact]
    public void MicroQrImageDecoder_RejectsAztecPhotograph() {
        Assert.False(MicroQrDecoder.TryDecodeImage(ReadAztecFixture(), out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    public void DefaultScanner_ReturnsOnlyTheAztecPayload(int maxSymbols) {
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.MaxSymbols = maxSymbols;

        var result = SymbolScanner.Scan(ReadAztecFixture(), options);

        Assert.True(result.IsSuccess, result.Failure);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Aztec, symbol.Format);
        Assert.Equal("Hist\u00f3rico", symbol.Text);
    }

    private static byte[] ReadAztecFixture() {
        using var stream = typeof(MicroQrValidationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Aztec.historico.png");
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);
        return buffer.ToArray();
    }
}

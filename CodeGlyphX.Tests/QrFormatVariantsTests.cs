using System;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrFormatVariantsTests {
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void ErasedFormatCopiesRecoverAfterOtherCorrectionLevelsFail(int version) {
        const string text = "DAMAGED FORMAT";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.L, minVersion: version, maxVersion: version, forceMask: 0);
        var modules = qr.Modules.Clone();
        // Erase both physical copies. Recovery must try mask zero with M and H
        // before reaching L, including single-block correction that mutates its input.
        EraseFormatCopies(modules);

        Assert.True(QrDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(version, decoded.Version);
        Assert.Equal(QrErrorCorrectionLevel.L, decoded.ErrorCorrectionLevel);
        Assert.Equal(0, decoded.Mask);
    }

    [Fact]
    public void ErasedFormatCopiesPreservePayloadFailureFromClosestCandidate() {
        var qr = QrCodeEncoder.EncodeText("INVALID PAYLOAD", QrErrorCorrectionLevel.M, minVersion: 1, maxVersion: 1, forceMask: 0);
        var modules = qr.Modules.Clone();
        var functions = QrStructureAnalysis.BuildFunctionMask(1, modules.Width);
        var data = new byte[16];
        // A version-one M block with valid parity but an unsupported mode indicator.
        // M/mask zero belongs to the closest candidates when both format copies vanish.
        data[0] = 0xF0;
        var parity = QrReedSolomon.ComputeRemainder(data, QrReedSolomon.ComputeDivisor(10));
        var codewords = new byte[26];
        Array.Copy(data, codewords, data.Length);
        Array.Copy(parity, 0, codewords, data.Length, parity.Length);
        var bit = 0;
        var upward = true;
        for (var right = modules.Width - 1; right >= 1; right -= 2) {
            if (right == 6) right = 5;
            for (var row = 0; row < modules.Height; row++) {
                var y = upward ? modules.Height - 1 - row : row;
                for (var offset = 0; offset < 2; offset++) {
                    var x = right - offset;
                    if (functions[x, y]) continue;
                    modules[x, y] = (((codewords[bit >> 3] >> (7 - (bit & 7))) & 1) != 0) ^ QrMask.ShouldInvert(0, x, y);
                    bit++;
                }
            }
            upward = !upward;
        }
        Assert.Equal(codewords.Length * 8, bit);
        Assert.False(QrDecoder.TryDecode(modules, out _, out var originalInfo));
        Assert.Equal(QrDecodeFailureReason.Payload, originalInfo.Failure);

        EraseFormatCopies(modules);
        Assert.False(QrDecoder.TryDecode(modules, out _, out var fallbackInfo));
        Assert.Equal(QrDecodeFailureReason.Payload, fallbackInfo.Failure);
    }

    private static void EraseFormatCopies(BitMatrix modules) {
        var size = modules.Width;
        for (var i = 0; i <= 5; i++) modules[8, i] = false;
        modules[8, 7] = modules[8, 8] = modules[7, 8] = false;
        for (var i = 9; i < 15; i++) modules[14 - i, 8] = false;
        for (var i = 0; i < 8; i++) modules[size - 1 - i, 8] = false;
        for (var i = 8; i < 15; i++) modules[8, size - 15 + i] = false;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Decode_ForAllMasks_PreservesFormatInfo(int mask) {
        var text = "FORMAT-MASK";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, minVersion: 1, maxVersion: 1, forceMask: mask);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(1, decoded.Version);
        Assert.Equal(QrErrorCorrectionLevel.M, decoded.ErrorCorrectionLevel);
        Assert.Equal(mask, decoded.Mask);
    }
}

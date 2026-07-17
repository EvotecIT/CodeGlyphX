using System;
using System.Linq;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrReedSolomonCorrectionTests {
    [Theory]
    [InlineData(19, 7, 3)]
    [InlineData(34, 10, 5)]
    [InlineData(10, 24, 8)]
    public void Decoder_CorrectsErrorsUpToTheExercisedBound(int dataLength, int eccLength, int errors) {
        var original = CreateCodewordBlock(dataLength, eccLength);
        var damaged = (byte[])original.Clone();
        DamageDistinctCodewords(damaged, errors);

        Assert.True(QrReedSolomonDecoder.TryCorrectInPlace(damaged, eccLength));
        Assert.Equal(original, damaged);
    }

    [Fact]
    public void CancellationAwareDecoder_UsesTheSameCorrectionContract() {
        var original = CreateCodewordBlock(32, 18);
        var damaged = (byte[])original.Clone();
        DamageDistinctCodewords(damaged, 7);

        Assert.True(QrReedSolomonDecoder.TryCorrectInPlace(damaged, 18, () => false));
        Assert.Equal(original, damaged);
    }

    private static byte[] CreateCodewordBlock(int dataLength, int eccLength) {
        var data = Enumerable.Range(0, dataLength).Select(value => (byte)(value * 37 + 11)).ToArray();
        var remainder = QrReedSolomon.ComputeRemainder(data, QrReedSolomon.ComputeDivisor(eccLength));
        return data.Concat(remainder).ToArray();
    }

    private static void DamageDistinctCodewords(byte[] codewords, int errors) {
        for (var i = 0; i < errors; i++) {
            var position = i * (codewords.Length - 1) / Math.Max(1, errors - 1);
            codewords[position] ^= (byte)(0x5A + i);
        }
    }
}

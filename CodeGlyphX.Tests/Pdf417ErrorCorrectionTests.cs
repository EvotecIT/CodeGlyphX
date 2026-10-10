using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Pdf417.Ec;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417ErrorCorrectionTests {
    [Fact]
    public void ErrorsAndErasures_AtEveryCorrectionLevel_RecoverAllCodewords() {
        var random = new Random(417929);
        for (var level = 0; level <= 8; level++) {
            var parity = 1 << (level + 1);
            for (var sample = 0; sample < 16; sample++) {
                var data = Enumerable.Range(0, 97).Select(_ => random.Next(929)).ToList();
                var expected = data.Concat(Pdf417ErrorCorrection.GenerateErrorCorrection(data, level)).ToArray();
                var received = (int[])expected.Clone();
                var positions = Enumerable.Range(0, received.Length).OrderBy(_ => random.Next()).ToArray();
                var erasureCount = sample == 0 ? parity : sample == 1 ? 0 : random.Next(1, parity + 1);
                var errorCount = (parity - erasureCount) / 2;
                var erasures = positions.Take(erasureCount).ToArray();
                // Arbitrary placeholders include already-correct erased values;
                // callers must not depend on erased data containing zero.
                for (var i = 0; i < erasureCount; i++) received[positions[i]] = random.Next(929);
                for (var i = erasureCount; i < erasureCount + errorCount; i++)
                    received[positions[i]] = (received[positions[i]] + random.Next(1, 929)) % 929;

                Assert.True(new ErrorCorrection().Decode(received, parity, erasures.Length == 0 ? null : erasures), $"level={level}, sample={sample}");
                Assert.Equal(expected, received);
            }
        }
    }

    [Fact]
    public void InvalidErasurePositionsAndExcessiveDamage_AreRejected() {
        var data = Enumerable.Range(1, 23).ToList();
        var original = data.Concat(Pdf417ErrorCorrection.GenerateErrorCorrection(data, 2)).ToArray();
        var ec = new ErrorCorrection();
        foreach (var positions in new[] { new[] { -1 }, new[] { original.Length }, new[] { 2, 2 }, Enumerable.Range(0, 9).ToArray() }) {
            var received = (int[])original.Clone();
            Assert.False(ec.Decode(received, 8, positions));
            Assert.Equal(original, received);
        }
        var damaged = (int[])original.Clone();
        for (var i = 0; i < 8; i++) damaged[i] = (damaged[i] + 37) % 929;
        Assert.False(ec.Decode(damaged, 8, new[] { 0, 1 }));
    }

    [Fact]
    public void CancelledCorrection_DoesNotChangeCodewords() {
        var data = Enumerable.Range(1, 23).ToList();
        var received = data.Concat(Pdf417ErrorCorrection.GenerateErrorCorrection(data, 2)).ToArray();
        received[0] = 0;
        var original = (int[])received.Clone();
        Assert.False(new ErrorCorrection().Decode(received, 8, new[] { 0 }, new CancellationToken(true)));
        Assert.Equal(original, received);
    }

    [Fact]
    public void ErrorCorrection_CorrectsSingleError() {
        var data = new List<int> { 3, 65, 66 };
        var ecc = Pdf417ErrorCorrection.GenerateErrorCorrection(data, 2); // 8 codewords
        var received = new int[data.Count + ecc.Length];
        data.CopyTo(received, 0);
        ecc.CopyTo(received, data.Count);

        received[1] = (received[1] + 7) % 929;

        var ec = new ErrorCorrection();
        Assert.True(ec.Decode(received, ecc.Length));
        Assert.Equal(65, received[1]);
    }
}

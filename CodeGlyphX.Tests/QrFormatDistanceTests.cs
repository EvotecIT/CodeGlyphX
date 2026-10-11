using System;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrFormatDistanceTests {
    [Fact]
    public void EveryFormatObservation_UsesNearestBchWordAcrossBothCopies() {
        var expected = new int[1 << 15];
        var words = BuildFormatWords();
        for (var bits = 0; bits < expected.Length; bits++) {
            expected[bits] = NearestDistance(bits, words);
            Assert.Equal(expected[bits], QrDecoder.GetBestFormatDistance(bits, bits));
        }

        for (var bits = 0; bits < expected.Length; bits++) {
            var other = (bits * 4051 + 31) & 0x7FFF;
            Assert.Equal(Math.Min(expected[bits], expected[other]), QrDecoder.GetBestFormatDistance(bits, other));
        }
    }

    [Fact]
    public void ObservationsOutsideFifteenBits_PreserveFullIntegerDistance() {
        var words = BuildFormatWords();
        var observations = new[] { int.MinValue, -32768, -1, 0, 1, 0x5412, 0x7FFF, 0x8000, 0xFFFF, 1 << 24, int.MaxValue };
        foreach (var first in observations) {
            foreach (var second in observations) {
                Assert.Equal(Math.Min(NearestDistance(first, words), NearestDistance(second, words)),
                    QrDecoder.GetBestFormatDistance(first, second));
            }
        }
    }

    private static int[] BuildFormatWords() {
        var words = new int[32];
        for (var data = 0; data < words.Length; data++) {
            var remainder = data << 10;
            for (var bit = 14; bit >= 10; bit--) {
                if ((remainder & (1 << bit)) != 0) remainder ^= 0x537 << (bit - 10);
            }
            words[data] = ((data << 10) | remainder) ^ 0x5412;
        }
        return words;
    }

    private static int NearestDistance(int bits, int[] words) {
        var best = 32;
        foreach (var word in words) {
            var difference = unchecked((uint)(bits ^ word));
            var distance = 0;
            while (difference != 0) {
                difference &= difference - 1;
                distance++;
            }
            best = Math.Min(best, distance);
        }
        return best;
    }
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DirectPartMarkWorkspaceTests {
    [Fact]
    public void Retries_RetainIndependentPixelsAfterAdvancingAndDisposingTheIterator() {
        var rgba = Pixels(23, 17, 19);
        var original = (byte[])rgba.Clone();
        var retained = new List<byte[]>();
        var snapshots = new List<byte[]>();
        using (var iterator = DirectPartMarkPreprocessor.CreateVariants(rgba, 23, 17, Options(), CancellationToken.None).GetEnumerator()) {
            while (iterator.MoveNext()) {
                Assert.Equal(23 * 17, iterator.Current.Length);
                foreach (var prior in retained) Assert.NotSame(prior, iterator.Current);
                retained.Add(iterator.Current);
                snapshots.Add((byte[])iterator.Current.Clone());
                for (var i = 0; i < retained.Count; i++) Assert.Equal(snapshots[i], retained[i]);
            }
        }
        Assert.Equal(6, retained.Count);
        PoisonReleasedBuffers(23 * 17, () => {
            for (var i = 0; i < retained.Count; i++) Assert.Equal(snapshots[i], retained[i]);
        });
        Assert.Equal(original, rgba);
    }

    [Fact]
    public void ChangingAnEarlierRetry_DoesNotChangeLaterPreprocessing() {
        var rgba = Pixels(23, 17, 41);
        var expected = DirectPartMarkPreprocessor.CreateVariants(rgba, 23, 17, Options(), CancellationToken.None).ToArray();
        var index = 0;
        foreach (var retry in DirectPartMarkPreprocessor.CreateVariants(rgba, 23, 17, Options(), CancellationToken.None)) {
            Assert.Equal(expected[index++], retry);
            for (var i = 0; i < retry.Length; i++) retry[i] = 73;
        }
        Assert.Equal(6, index);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void CancellationBetweenRetries_PreservesEarlierImagesAndSubsequentCalls(int yielded) {
        var rgba = Pixels(23, 17, 67);
        var expected = DirectPartMarkPreprocessor.CreateVariants(rgba, 23, 17, Options(), CancellationToken.None).ToArray();
        var retained = new List<byte[]>();
        using var cancellation = new CancellationTokenSource();
        using (var iterator = DirectPartMarkPreprocessor.CreateVariants(rgba, 23, 17, Options(), cancellation.Token).GetEnumerator()) {
            for (var i = 0; i < yielded; i++) {
                Assert.True(iterator.MoveNext());
                Assert.Equal(expected[i], iterator.Current);
                retained.Add(iterator.Current);
            }
            cancellation.Cancel();
            Assert.False(iterator.MoveNext());
        }
        PoisonReleasedBuffers(23 * 17, () => {
            for (var i = 0; i < retained.Count; i++) Assert.Equal(expected[i], retained[i]);
        });
        var fresh = DirectPartMarkPreprocessor.CreateVariants(rgba, 23, 17, Options(), CancellationToken.None).ToArray();
        for (var i = 0; i < fresh.Length; i++) Assert.Equal(expected[i], fresh[i]);
    }

    [Fact]
    public void ConcurrentPreprocessing_PreservesEachInputsSequentialPixels() {
        var inputs = Enumerable.Range(0, 16).Select(i => Pixels(23, 17, i + 11)).ToArray();
        var expected = inputs.Select(pixels => DirectPartMarkPreprocessor.CreateVariants(pixels, 23, 17, Options(), CancellationToken.None).ToArray()).ToArray();
        Parallel.For(0, inputs.Length, index => {
            var actual = DirectPartMarkPreprocessor.CreateVariants(inputs[index], 23, 17, Options(), CancellationToken.None).ToArray();
            Assert.Equal(expected[index].Length, actual.Length);
            for (var i = 0; i < actual.Length; i++) Assert.Equal(expected[index][i], actual[i]);
        });
    }

    private static DirectPartMarkOptions Options() => new() { AdaptiveWindowRadius = 2, MorphologyRadius = 3, MaxAttempts = 6 };

    private static byte[] Pixels(int width, int height, int seed) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = (byte)((i * 73 + seed * 31) % 256);
        return pixels;
    }

    private static void PoisonReleasedBuffers(int length, Action assertion) {
        var leases = new List<byte[]>();
        try {
            for (var count = 0; count < 8; count++) {
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                leases.Add(buffer);
                for (var i = 0; i < buffer.Length; i++) buffer[i] = 201;
            }
            assertion();
        } finally {
            foreach (var buffer in leases) ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

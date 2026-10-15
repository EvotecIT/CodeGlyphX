using System;
using System.Reflection;
using System.Threading;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrComponentForegroundTests {
    private delegate bool FillMask(QrGrayImage image, bool invert, QrPixelDecoder.DecodeBudget budget, byte[] mask);
    private static readonly FillMask Fill = typeof(QrPixelDecoder)
        .GetMethod("TryFillComponentForeground", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<FillMask>();
    private static readonly ConstructorInfo ImageConstructor = typeof(QrGrayImage)
        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(byte[]), typeof(byte), typeof(byte), typeof(byte), typeof(byte[]) }, null)!;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ComponentMaskMatchesSelectedPixelsAndPreservesUnusedPooledTail(bool adaptive, bool invert) {
        uint random = 0x8A31C2D7;
        foreach (var width in new[] { 1, 15, 16, 17, 31, 32, 33, 63, 64, 65, 255, 256, 257 }) {
            const int height = 7;
            var count = width * height;
            foreach (var pattern in new[] { 0, 1, 2, 3 }) {
                var gray = new byte[count + 67];
                var map = adaptive ? new byte[gray.Length] : null;
                for (var at = 0; at < gray.Length; at++) {
                    random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                    gray[at] = pattern switch { 0 => 0, 1 => 255, 2 => (byte)at, _ => (byte)random };
                    if (map is not null) map[at] = (byte)(at * 19);
                }
                foreach (byte threshold in new byte[] { 0, 127, 128, 255 }) {
                    var image = CreateImage(width, height, gray, threshold, map);
                    var mask = new byte[count + 67];
                    Array.Fill(mask, (byte)0xA5);
                    Assert.True(Fill(image, invert, new QrPixelDecoder.DecodeBudget(0, CancellationToken.None), mask));
                    for (var at = 0; at < count; at++) {
                        var selected = gray[at] <= (map is null ? threshold : map[at]);
                        Assert.Equal((byte)(selected != invert ? 1 : 0), mask[at]);
                    }
                    for (var at = count; at < mask.Length; at++) Assert.Equal((byte)0xA5, mask[at]);
                }
            }
        }
    }

    [Fact]
    public void CancelledAndExpiredMasksStopBeforeWritesAndUnboundedMasksComplete() {
        const int count = 65 * 7;
        var image = CreateImage(65, 7, new byte[count], 127, null);
        var mask = new byte[count];
        Array.Fill(mask, (byte)0xA5);
        Assert.False(Fill(image, false, new QrPixelDecoder.DecodeBudget(0, new CancellationToken(true)), mask));
        Assert.All(mask, pixel => Assert.Equal((byte)0xA5, pixel));
        var deadline = new QrPixelDecoder.DecodeBudget(1, CancellationToken.None);
        Assert.True(SpinWait.SpinUntil(() => deadline.IsExpired, TimeSpan.FromSeconds(1)));
        Assert.False(Fill(image, false, deadline, mask));
        Assert.All(mask, pixel => Assert.Equal((byte)0xA5, pixel));
        Assert.True(Fill(image, false, new QrPixelDecoder.DecodeBudget(0, CancellationToken.None), mask));
        Assert.All(mask, pixel => Assert.Equal((byte)1, pixel));
    }

    private static QrGrayImage CreateImage(int width, int height, byte[] gray, byte threshold, byte[]? map) =>
        (QrGrayImage)ImageConstructor.Invoke(new object?[] { width, height, gray, (byte)0, (byte)255, threshold, map });
}

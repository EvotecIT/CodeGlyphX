using System;
using System.Reflection;
using System.Threading;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrForegroundBoundsTests {
    private delegate bool FindBounds(QrGrayImage image, bool invert, QrPixelDecoder.DecodeBudget budget,
        int left, int top, int right, int bottom, out int minX, out int minY, out int maxX, out int maxY);
    private static readonly FindBounds Find = typeof(QrPixelDecoder)
        .GetMethod("TryFindForegroundBounds", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<FindBounds>();
    private static readonly ConstructorInfo ImageConstructor = typeof(QrGrayImage)
        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(byte[]), typeof(byte), typeof(byte), typeof(byte), typeof(byte[]) }, null)!;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CroppedForegroundExtremaMatchEverySelectedPixel(bool adaptive, bool invert) {
        uint random = 0x3912ABCD;
        foreach (var width in new[] { 15, 16, 17, 31, 32, 33, 63, 64, 65, 257 }) {
            const int height = 7;
            foreach (var pattern in new[] { 0, 1, 2, 3 }) {
                var gray = new byte[width * height];
                var map = adaptive ? new byte[gray.Length] : null;
                for (var at = 0; at < gray.Length; at++) {
                    random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                    gray[at] = pattern switch { 0 => 0, 1 => 255, 2 => (byte)(at * 17), _ => (byte)random };
                    if (map is not null) map[at] = (byte)(at * 19);
                }
                foreach (byte threshold in new byte[] { 0, 127, 128, 255 }) {
                    var image = CreateImage(width, height, gray, threshold, map);
                    foreach (var region in new[] { (0, 0, width - 1, height - 1), (1, 1, width - 2, height - 2),
                             (width - 1, 0, width - 1, height - 1), (0, height - 1, width - 1, height - 1) }) {
                        var (left, top, right, bottom) = region;
                        var expected = Reference(gray, map, width, threshold, invert, left, top, right, bottom);
                        var found = Find(image, invert, new QrPixelDecoder.DecodeBudget(0, CancellationToken.None),
                            left, top, right, bottom, out var minX, out var minY, out var maxX, out var maxY);
                        Assert.Equal(expected is not null, found);
                        if (expected is not null) Assert.Equal(expected, new[] { minX, minY, maxX, maxY });
                    }
                }
            }
        }
    }

    [Fact]
    public void CancelledAndExpiredBoundsStopWhileUnboundedInputRemainsReadable() {
        var image = CreateImage(65, 7, new byte[65 * 7], 127, null);
        Assert.False(Find(image, false, new QrPixelDecoder.DecodeBudget(0, new CancellationToken(true)),
            0, 0, 64, 6, out _, out _, out _, out _));
        var deadline = new QrPixelDecoder.DecodeBudget(1, CancellationToken.None);
        Assert.True(SpinWait.SpinUntil(() => deadline.IsExpired, TimeSpan.FromSeconds(1)));
        Assert.False(Find(image, false, deadline, 0, 0, 64, 6, out _, out _, out _, out _));
        Assert.True(Find(image, false, new QrPixelDecoder.DecodeBudget(0, CancellationToken.None),
            0, 0, 64, 6, out var minX, out var minY, out var maxX, out var maxY));
        Assert.Equal(new[] { 0, 0, 64, 6 }, new[] { minX, minY, maxX, maxY });
    }

    private static QrGrayImage CreateImage(int width, int height, byte[] gray, byte threshold, byte[]? map) =>
        (QrGrayImage)ImageConstructor.Invoke(new object?[] { width, height, gray, (byte)0, (byte)255, threshold, map });

    private static int[]? Reference(byte[] gray, byte[]? map, int width, byte threshold, bool invert,
        int left, int top, int right, int bottom) {
        var minX = int.MaxValue; var minY = int.MaxValue; var maxX = -1; var maxY = -1;
        for (var y = top; y <= bottom; y++) for (var x = left; x <= right; x++) {
            var at = y * width + x;
            if ((gray[at] <= (map is null ? threshold : map[at])) == invert) continue;
            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
        }
        return maxX < 0 ? null : new[] { minX, minY, maxX, maxY };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CodeGlyphX.Internal;
#if NET8_0_OR_GREATER
using CodeGlyphX.Qr;
#endif
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageComponentCoordinateTests {
    [Fact]
    public void CoordinatesMatchDivisionAtRowBoundariesAndAcrossInt32Offsets() {
        uint random = 0xCA7318F5;
        var widths = new List<int> { 1, 2, 3, 7, 15, 16, 17, 31, 32, 33, 255, 256, 257,
            1023, 1024, 1025, 65535, 65536, 65537, 1048575, 1048576, int.MaxValue - 1, int.MaxValue };
        for (var i = 0; i < 1000; i++) widths.Add((int)(Next(ref random) % int.MaxValue) + 1);
        foreach (var width in widths) {
            var divider = new ImageCoordinateDivider(width);
            Check(0); Check(int.MaxValue);
            foreach (var row in new[] { 0, 1, 2, 3, 127, int.MaxValue / width }) {
                var boundary = (long)row * width;
                for (var delta = -2; delta <= 2; delta++) {
                    var at = boundary + delta;
                    if (at >= 0 && at <= int.MaxValue) Check((int)at);
                }
            }
            for (var i = 0; i < 100; i++) Check((int)(Next(ref random) & int.MaxValue));
            void Check(int at) {
                divider.GetCoordinates(at, out var x, out var y);
                Assert.Equal(at % width, x);
                Assert.Equal(at / width, y);
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BinaryComponentsPreserveQueueOrderEdgesAndVisitorStop(bool invert, bool stop) {
        foreach (var width in new[] { 7, 31, 65, 257 }) {
            const int height = 43;
            for (var pattern = 0; pattern < 8; pattern++) {
                var mask = Pattern(width, height, pattern);
                var expected = Reference(mask, width, height, invert);
                if (stop && expected.Count > 2) expected.RemoveRange(2, expected.Count - 2);
                var visited = new List<int[]>();
                var edge = new List<bool>();
                var stopped = BinaryImageComponents.Visit(mask, width, height, invert, CancellationToken.None,
                    (queue, length, touchesEdge) => {
                        visited.Add(queue.Take(length).ToArray());
                        edge.Add(touchesEdge);
                        return stop && visited.Count == 2;
                    });
                Assert.Equal(stop && expected.Count == 2, stopped);
                Assert.Equal(expected.Count, visited.Count);
                var expectedMask = Pattern(width, height, pattern);
                for (var i = 0; i < expected.Count; i++) {
                    Assert.Equal(expected[i], visited[i]);
                    Assert.Equal(expected[i].Any(at => at % width == 0 || at % width == width - 1 ||
                        at / width == 0 || at / width == height - 1), edge[i]);
                    foreach (var at in expected[i]) expectedMask[at] |= 2;
                }
                Assert.Equal(expectedMask, mask);
            }
        }
    }

#if NET8_0_OR_GREATER
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void QrComponentsMatchIndependentGraphBoundsAreasAndOrder(bool adaptive, bool invert) {
        var find = typeof(QrPixelDecoder).GetMethod("FindComponents", BindingFlags.NonPublic | BindingFlags.Static)!;
        var constructor = typeof(QrGrayImage).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(byte[]), typeof(byte), typeof(byte), typeof(byte), typeof(byte[]) }, null)!;
        foreach (var width in new[] { 7, 31, 65, 257 }) {
            const int height = 43;
            for (var pattern = 0; pattern < 8; pattern++) {
                var mask = Pattern(width, height, pattern);
                var gray = mask.Select(x => x == 0 ? (byte)255 : (byte)0).ToArray();
                var thresholds = adaptive ? Enumerable.Range(0, gray.Length).Select(x => (byte)(1 + x % 254)).ToArray() : null;
                var image = constructor.Invoke(new object?[] { width, height, gray, (byte)0, (byte)255, (byte)127, thresholds });
                var expected = Reference(mask, width, height, invert).Select(points => new[] {
                    points.Min(at => at % width), points.Min(at => at / width),
                    points.Max(at => at % width), points.Max(at => at / width), points.Length
                }).Where(c => c[4] >= Math.Max(16, width * height / 400) && c[2] - c[0] + 1 >= 6 && c[3] - c[1] + 1 >= 6)
                    .Where(c => Math.Max((c[2] - c[0] + 1.0) / (c[3] - c[1] + 1.0),
                        (c[3] - c[1] + 1.0) / (c[2] - c[0] + 1.0)) <= 2.2).ToArray();
                var actual = find.Invoke(null, new[] { image, (object)invert,
                    new QrPixelDecoder.DecodeBudget(0, CancellationToken.None), 400, 6 })!;
                using ((IDisposable)actual) {
                    var count = (int)actual.GetType().GetProperty("Count")!.GetValue(actual)!;
                    Assert.Equal(expected.Length, count);
                    var indexer = actual.GetType().GetProperty("Item")!;
                    for (var i = 0; i < count; i++) {
                        var component = indexer.GetValue(actual, new object[] { i })!;
                        Assert.Equal(expected[i], new[] { "MinX", "MinY", "MaxX", "MaxY", "Area" }
                            .Select(name => (int)component.GetType().GetProperty(name)!.GetValue(component)!).ToArray());
                    }
                }
            }
        }
    }
#endif

    private static byte[] Pattern(int width, int height, int pattern) {
        uint random = 0xD17A943B;
        var mask = new byte[width * height];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var selected = pattern switch {
                0 => false,
                1 => true,
                2 => ((x + y) & 1) == 0,
                3 => x % 4 < 2,
                4 => y % 4 < 2,
                5 => (x > 1 && x < width - 2 && (y == 2 || y == height - 3)) ||
                     (y > 1 && y < height - 2 && (x == 2 || x == width - 3)),
                6 => Next(ref random) % 100 < 65,
                _ => Next(ref random) % 100 < 15
            };
            mask[y * width + x] = selected ? (byte)1 : (byte)0;
        }
        return mask;
    }

    // Independent graph walk retains the neighbour order used by component
    // visitors, whose tied extrema can depend on the first pixel encountered.
    private static List<int[]> Reference(byte[] mask, int width, int height, bool invert) {
        var seen = new bool[mask.Length];
        var result = new List<int[]>();
        for (var seed = 0; seed < mask.Length; seed++) {
            if (seen[seed] || ((mask[seed] & 1) != 0) == invert) continue;
            var queue = new Queue<int>();
            var points = new List<int>();
            seen[seed] = true; queue.Enqueue(seed);
            while (queue.Count > 0) {
                var at = queue.Dequeue(); points.Add(at);
                var x = at % width; var y = at / width;
                if (x > 0) Add(at - 1);
                if (x + 1 < width) Add(at + 1);
                if (y > 0) Add(at - width);
                if (y + 1 < height) Add(at + width);
                void Add(int next) {
                    if (seen[next] || ((mask[next] & 1) != 0) == invert) return;
                    seen[next] = true; queue.Enqueue(next);
                }
            }
            result.Add(points.ToArray());
        }
        return result;
    }

    private static uint Next(ref uint value) {
        value ^= value << 13; value ^= value >> 17; value ^= value << 5;
        return value;
    }
}

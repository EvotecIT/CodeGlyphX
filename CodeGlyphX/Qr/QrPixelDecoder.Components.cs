#if NET8_0_OR_GREATER
using System;
using System.Buffers;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>
    /// Finds the same four-connected foreground components in row-major seed
    /// order, using horizontal runs for broad regions and pixels for narrow
    /// strokes and fragmented foreground.
    /// </summary>
    private static PooledList<Component> FindComponents(QrGrayImage image, bool invert,
        DecodeBudget budget, int minAreaDivisor, int minComponentSize) {
        var width = image.Width;
        var height = image.Height;
        var components = new PooledList<Component>(8);
        if (width <= 0 || height <= 0) return components;
        var total = checked(width * height);
        var coordinates = new global::CodeGlyphX.Internal.ImageCoordinateDivider(width);
        if (minAreaDivisor <= 0) minAreaDivisor = 400;
        if (minComponentSize < 6) minComponentSize = 6;
        var minArea = Math.Max(16, total / minAreaDivisor);
        var foreground = ArrayPool<byte>.Shared.Rent(total);
        int[]? stack = null;
        try {
            stack = ArrayPool<int>.Shared.Rent(Math.Max(64, Math.Min(4096, total / 16)));
            // Every byte is written before traversal. Zero marks background or
            // consumed pixels; one is unqueued foreground and two is queued.
            if (!TryFillComponentForeground(image, invert, budget, foreground)) return components;
            var useRuns = HasBroadComponentRuns(foreground, width, height);
            var runCount = 0;
            for (var y = 0; y < height; y++) {
                if (budget.IsExpired) return components;
                var row = y * width;
                for (var x = 0; x < width; x++) {
                    var first = row + x;
                    if (foreground[first] != 1) {
                        // Vector search pays off on cleared regions. Leave
                        // short alternating gaps to the scalar loop.
                        if ((x & 15) != 0) continue;
                        var offset = foreground.AsSpan(first, width - x).IndexOf((byte)1);
                        if (offset < 0) break;
                        x += offset;
                        first += offset;
                    }
                    // A row-major seed has no unconsumed neighbour above or to
                    // its left. Isolated pixels cannot meet the minimum area.
                    if ((x + 1 == width || foreground[first + 1] == 0) &&
                        (y + 1 == height || foreground[first + width] == 0)) {
                        foreground[first] = 0;
                        continue;
                    }
                    var minX = x;
                    var maxX = x;
                    var minY = y;
                    var maxY = y;
                    var area = 0;
                    var count = 0;
                    foreground[first] = 2;
                    stack[count++] = first;
                    while (count > 0) {
                        if ((runCount++ & 63) == 0 && budget.IsExpired) return components;
                        var seed = stack[--count];
                        if (foreground[seed] == 0) continue;
                        coordinates.GetCoordinates(seed, out var cx, out var cy);
                        var rowStart = seed - cx;
                        if (!useRuns) {
                            foreground[seed] = 0;
                            area++;
                            minX = Math.Min(minX, cx);
                            maxX = Math.Max(maxX, cx);
                            minY = Math.Min(minY, cy);
                            maxY = Math.Max(maxY, cy);
                            if (count + 4 > stack.Length) GrowStack(ref stack, count + 4);
                            if (cx > 0) AddComponentPixel(foreground, seed - 1, stack, ref count);
                            if (cx + 1 < width) AddComponentPixel(foreground, seed + 1, stack, ref count);
                            if (cy > 0) AddComponentPixel(foreground, seed - width, stack, ref count);
                            if (cy + 1 < height) AddComponentPixel(foreground, seed + width, stack, ref count);
                            continue;
                        }
                        var left = seed;
                        var right = seed;
                        while (left > rowStart && foreground[left - 1] != 0) left--;
                        var rowEnd = rowStart + width;
                        while (right + 1 < rowEnd && foreground[right + 1] != 0) right++;
                        var length = right - left + 1;
                        foreground.AsSpan(left, length).Clear();
                        area += length;
                        var firstX = left - rowStart;
                        var lastX = right - rowStart;
                        minX = Math.Min(minX, firstX);
                        maxX = Math.Max(maxX, lastX);
                        minY = Math.Min(minY, cy);
                        maxY = Math.Max(maxY, cy);
                        // Only overlapping runs in the adjacent rows connect.
                        // A queued seed may already be consumed by another path;
                        // the zero check above prevents counting it twice.
                        if (length == 1) {
                            if (count + 2 > stack.Length) GrowStack(ref stack, count + 2);
                            if (cy > 0 && foreground[left - width] == 1) {
                                foreground[left - width] = 2;
                                stack[count++] = left - width;
                            }
                            if (cy + 1 < height && foreground[left + width] == 1) {
                                foreground[left + width] = 2;
                                stack[count++] = left + width;
                            }
                        } else {
                            if (cy > 0) AddComponentRowSeeds(foreground, left - width, length, ref stack, ref count);
                            if (cy + 1 < height) AddComponentRowSeeds(foreground, left + width, length, ref stack, ref count);
                        }
                    }
                    var componentWidth = maxX - minX + 1;
                    var componentHeight = maxY - minY + 1;
                    if (area >= minArea && componentWidth >= minComponentSize && componentHeight >= minComponentSize) {
                        var ratio = componentWidth > componentHeight
                            ? (double)componentWidth / componentHeight : (double)componentHeight / componentWidth;
                        if (ratio <= 2.2) components.Add(new Component(minX, minY, maxX, maxY, area));
                    }
                }
            }
            return components;
        } catch {
            components.Dispose();
            throw;
        } finally {
            ArrayPool<byte>.Shared.Return(foreground);
            if (stack is not null) ArrayPool<int>.Shared.Return(stack);
        }
    }

    // Broad runs benefit from span traversal. Narrow vertical strokes and
    // fragmented noise are cheaper as pixels. This chooses traversal only;
    // both paths visit exactly the same four-connected foreground.
    private static bool HasBroadComponentRuns(byte[] foreground, int width, int height) {
        long pixels = 0;
        long runs = 0;
        var rows = Math.Min(16, height);
        for (var sample = 0; sample < rows; sample++) {
            var row = (int)((long)sample * height / rows) * width;
            var previous = false;
            for (var x = 0; x < width; x++) {
                var black = foreground[row + x] != 0;
                if (black) {
                    pixels++;
                    if (!previous) runs++;
                }
                previous = black;
            }
        }
        return pixels > runs * 2;
    }

    private static void AddComponentPixel(byte[] foreground, int pixel, int[] stack, ref int count) {
        if (foreground[pixel] != 1) return;
        foreground[pixel] = 2;
        stack[count++] = pixel;
    }

    private static void AddComponentRowSeeds(byte[] foreground, int start, int length, ref int[] stack, ref int count) {
        var remaining = foreground.AsSpan(start, length);
        while (!remaining.IsEmpty) {
            var offset = remaining.IndexOf((byte)1);
            if (offset < 0) return;
            if (count == stack.Length) GrowStack(ref stack, count + 1);
            foreground[start + offset] = 2;
            stack[count++] = start + offset;
            remaining = remaining.Slice(offset);
            start += offset;
            var runLength = remaining.IndexOf((byte)0);
            if (runLength < 0) return;
            remaining = remaining.Slice(runLength);
            start += runLength;
        }
    }

    private static void GrowStack(ref int[] stack, int size) {
        var larger = ArrayPool<int>.Shared.Rent(Math.Max(size, stack.Length * 2));
        Array.Copy(stack, larger, stack.Length);
        ArrayPool<int>.Shared.Return(stack);
        stack = larger;
    }
}
#endif

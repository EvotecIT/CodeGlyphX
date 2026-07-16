using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>
/// Distinguishes the physically identical GS1 DataBar-14 horizontal payloads by measured bar height.
/// </summary>
internal static class DataBar14ImageClassifier {
    private const double OmnidirectionalMinimumHeightModules = 33.0;

    internal static bool TryIsOmnidirectional(
        byte[] rgba,
        int width,
        int height,
        BarcodeImageCandidate candidate,
        CancellationToken cancellationToken,
        out bool isOmnidirectional) {
        isOmnidirectional = false;
        if (rgba is null || candidate is null || width <= 0 || height <= 0 || rgba.LongLength < (long)width * height * 4) return false;

        var region = candidate.SearchRegion;
        var right = Math.Min(width, region.X + region.Width);
        var bottom = Math.Min(height, region.Y + region.Height);
        if (region.X >= right || region.Y >= bottom) return false;

        var minimum = 255;
        var maximum = 0;
        for (var y = region.Y; y < bottom; y++) {
            if ((y & 31) == 0 && cancellationToken.IsCancellationRequested) return false;
            for (var x = region.X; x < right; x++) {
                var luminance = GetLuminance(rgba, width, x, y);
                if (luminance < minimum) minimum = luminance;
                if (luminance > maximum) maximum = luminance;
            }
        }
        if (maximum <= minimum) return false;

        var threshold = (minimum + maximum) / 2;
        var vertical = candidate.Scanline.IsVertical;
        var scanPosition = vertical
            ? region.X + candidate.Scanline.Position
            : region.Y + candidate.Scanline.Position;
        if (vertical && (scanPosition < region.X || scanPosition >= right)
            || !vertical && (scanPosition < region.Y || scanPosition >= bottom)) return false;

        var longStart = vertical ? region.Y : region.X;
        var longEnd = vertical ? bottom : right;
        var first = longStart;
        while (first < longEnd && !IsDark(rgba, width, vertical, scanPosition, first, threshold)) first++;
        if (first == longEnd) return false;
        var last = longEnd - 1;
        while (last > first && !IsDark(rgba, width, vertical, scanPosition, last, threshold)) last--;

        var longSpan = last - first + 1;
        if (longSpan < 32 || candidate.Scanline.Modules.Length < 32) return false;

        var runs = new List<int>(longSpan / 2);
        // A tile can contain the complete encoded scanline while clipping the physical bar height.
        // Follow the connected bars in the original frame so tile overlap cannot turn one Omni
        // result into an additional false Truncated identity.
        var shortStart = 0;
        var shortEnd = vertical ? width : height;
        for (var position = first; position <= last; position++) {
            if ((position & 127) == 0 && cancellationToken.IsCancellationRequested) return false;
            if (!IsDark(rgba, width, vertical, scanPosition, position, threshold)) continue;

            var before = scanPosition;
            while (before > shortStart && IsDark(rgba, width, vertical, before - 1, position, threshold)) before--;
            var after = scanPosition;
            while (after + 1 < shortEnd && IsDark(rgba, width, vertical, after + 1, position, threshold)) after++;
            runs.Add(after - before + 1);
        }
        if (runs.Count == 0) return false;

        runs.Sort();
        var barHeightPixels = runs[runs.Count / 2];
        var heightModules = barHeightPixels * (double)candidate.Scanline.Modules.Length / longSpan;
        isOmnidirectional = heightModules >= OmnidirectionalMinimumHeightModules;
        return true;
    }

    private static bool IsDark(byte[] rgba, int width, bool vertical, int shortPosition, int longPosition, int threshold) {
        var x = vertical ? shortPosition : longPosition;
        var y = vertical ? longPosition : shortPosition;
        return GetLuminance(rgba, width, x, y) < threshold;
    }

    private static int GetLuminance(byte[] rgba, int width, int x, int y) {
        var pixel = (y * width + x) * 4;
        return (rgba[pixel] * 54 + rgba[pixel + 1] * 183 + rgba[pixel + 2] * 19) >> 8;
    }
}

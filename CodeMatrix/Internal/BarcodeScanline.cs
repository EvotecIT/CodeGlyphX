using System;
using System.Collections.Generic;

namespace CodeMatrix.Internal;

internal static class BarcodeScanline {
    public static bool TryGetModules(byte[] pixels, int width, int height, int stride, PixelFormat format, out bool[] modules) {
        modules = Array.Empty<bool>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;

        var y = height / 2;
        var offset = y * stride;
        var min = 255;
        var max = 0;
        var luminance = new byte[width];
        for (var x = 0; x < width; x++) {
            var p = offset + x * 4;
            byte r;
            byte g;
            byte b;
            if (format == PixelFormat.Rgba32) {
                r = pixels[p + 0];
                g = pixels[p + 1];
                b = pixels[p + 2];
            } else {
                b = pixels[p + 0];
                g = pixels[p + 1];
                r = pixels[p + 2];
            }
            var lum = (byte)((r * 54 + g * 183 + b * 19) >> 8); // approx 0.2126/0.7152/0.0722
            luminance[x] = lum;
            if (lum < min) min = lum;
            if (lum > max) max = lum;
        }

        if (max - min < 8) return false;
        var threshold = (min + max) / 2;

        var runs = new List<(bool isBar, int len)>(width / 2);
        var current = luminance[0] < threshold;
        var runLen = 1;
        for (var x = 1; x < width; x++) {
            var isBar = luminance[x] < threshold;
            if (isBar == current) {
                runLen++;
            } else {
                runs.Add((current, runLen));
                current = isBar;
                runLen = 1;
            }
        }
        runs.Add((current, runLen));

        // Trim quiet zones (white runs).
        var start = 0;
        while (start < runs.Count && !runs[start].isBar) start++;
        var end = runs.Count - 1;
        while (end >= start && !runs[end].isBar) end--;
        if (start > end) return false;

        var trimmed = runs.GetRange(start, end - start + 1);

        var minRun = int.MaxValue;
        for (var i = 0; i < trimmed.Count; i++) {
            if (trimmed[i].len < minRun) minRun = trimmed[i].len;
        }
        if (minRun <= 0) return false;

        var moduleBits = new List<bool>(width);
        for (var i = 0; i < trimmed.Count; i++) {
            var run = trimmed[i];
            var modulesCount = (int)Math.Round(run.len / (double)minRun);
            if (modulesCount < 1) modulesCount = 1;
            for (var m = 0; m < modulesCount; m++) moduleBits.Add(run.isBar);
        }

        if (moduleBits.Count == 0) return false;
        modules = moduleBits.ToArray();
        return true;
    }
}

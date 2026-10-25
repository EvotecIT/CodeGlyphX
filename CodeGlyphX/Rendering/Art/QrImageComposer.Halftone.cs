using System;
using System.Threading;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    // Error diffusion samples the image once per sub-cell. The painter replaces only
    // centre sub-cells with data, and keeps every functional pattern completely solid.
    private static bool[] DitherSubCells(QrImageSampler image, BitMatrix modules, int moduleSize,
        int originX, int originY, double strength, CancellationToken cancellationToken) {
        var side = modules.Width * 3;
        var result = new bool[side * side];
        var current = new double[side + 2];
        var next = new double[side + 2];
        for (var y = 0; y < side; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < side; x++) {
                image.Sample(originX + (x + 0.5) * moduleSize / 3.0 - 0.5,
                    originY + (y + 0.5) * moduleSize / 3.0 - 0.5, out var r, out var g, out var b);
                var tone = (0.299 * r + 0.587 * g + 0.114 * b) * strength
                    + (modules[x / 3, y / 3] ? 0 : 255) * (1 - strength);
                var value = Math.Max(0, Math.Min(255, tone + current[x + 1]));
                var dark = value < 128;
                result[y * side + x] = dark;
                var error = value - (dark ? 0 : 255);
                current[x + 2] += error * (7.0 / 16);
                next[x] += error * (3.0 / 16);
                next[x + 1] += error * (5.0 / 16);
                next[x + 2] += error * (1.0 / 16);
            }
            var swap = current; current = next; next = swap;
            Array.Clear(next, 0, next.Length);
        }
        return result;
    }
}

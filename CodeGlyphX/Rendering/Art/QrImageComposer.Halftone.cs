using System;
using System.Threading;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    // Above this weight a module's average tone no longer follows its bit, and downscaled or slightly blurred
    // copies of dense codes stop decoding. Higher Strength values are therefore capped here.
    private const double MaxHalftoneImageWeight = 0.7;
    private const double MonoToneExponent = 0.65;
    private const double DuotoneToneExponent = 0.5;
    private const double MinHalftoneTone = 56;
    private const double MaxHalftoneTone = 250;

    // Error diffusion samples the image once per sub-cell. The painter replaces only
    // centre sub-cells with data, and keeps every functional pattern completely solid.
    private static bool[] DitherSubCells(QrImageSampler image, BitMatrix modules, int moduleSize,
        int originX, int originY, double strength, bool duotone, CancellationToken cancellationToken) {
        var side = modules.Width * 3;
        var tones = new double[side * side];
        var histogram = new int[256];
        for (var y = 0; y < side; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < side; x++) {
                image.Sample(originX + (x + 0.5) * moduleSize / 3.0 - 0.5,
                    originY + (y + 0.5) * moduleSize / 3.0 - 0.5, out var r, out var g, out var b);
                var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
                tones[y * side + x] = luminance;
                histogram[(int)Math.Round(luminance)]++;
            }
        }
        // Mid-tone photos dither into an even grey noise. Stretching the 2nd..98th percentile to the full range
        // restores the image's light and dark masses; nearly flat images are left alone.
        var low = Percentile(histogram, tones.Length, 0.02);
        var high = Percentile(histogram, tones.Length, 0.98);
        var stretch = high - low >= 32 && high - low < 255;
        // A lift curve keeps mid-tones (a blue sky, say) as an open mid-grey pattern instead of near-solid ink. Duotone
        // ink reads heavier than black on white paper, so it is lifted further.
        var exponent = duotone ? DuotoneToneExponent : MonoToneExponent;
        var result = new bool[side * side];
        var current = new double[side + 2];
        var next = new double[side + 2];
        for (var y = 0; y < side; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            // Serpentine scanning avoids the directional worms and clumps of one-way error diffusion.
            var reverse = (y & 1) == 1;
            for (var step = 0; step < side; step++) {
                var x = reverse ? side - 1 - step : step;
                var luminance = tones[y * side + x];
                if (stretch) luminance = Math.Max(0, Math.Min(255, (luminance - low) * 255.0 / (high - low)));
                luminance = 255 * Math.Pow(luminance / 255, exponent);
                // Even the deepest shadow keeps some paper, so no area becomes a solid run of ink.
                luminance = MinHalftoneTone + luminance * (MaxHalftoneTone - MinHalftoneTone) / 255;
                var tone = luminance * strength + (modules[x / 3, y / 3] ? 0 : 255) * (1 - strength);
                var value = Math.Max(0, Math.Min(255, tone + current[x + 1]));
                var dark = value < 128;
                result[y * side + x] = dark;
                var error = value - (dark ? 0 : 255);
                var ahead = reverse ? x : x + 2;
                var behind = reverse ? x + 2 : x;
                current[ahead] += error * (7.0 / 16);
                next[behind] += error * (3.0 / 16);
                next[x + 1] += error * (5.0 / 16);
                next[ahead] += error * (1.0 / 16);
            }
            var swap = current; current = next; next = swap;
            Array.Clear(next, 0, next.Length);
        }
        EnsureModuleMajority(result, modules, side);
        return result;
    }

    // Edge neighbours of the centre first, so a corrected module reads as a small plus rather than a solid block.
    private static readonly int[] MajorityOrder = { 1, 3, 5, 7, 0, 2, 6, 8 };

    // At small print or display sizes a reader sees each module's average tone. Where the image strongly opposes
    // the encoded bit, at least five of the nine sub-cells (the forced centre included) are given the bit's colour.
    private static void EnsureModuleMajority(bool[] subCells, BitMatrix modules, int side) {
        const int minimumMatching = 5;
        for (var my = 0; my < modules.Height; my++) for (var mx = 0; mx < modules.Width; mx++) {
            var dark = modules[mx, my];
            var matching = 1; // The centre sub-cell is always painted with the encoded bit.
            for (var i = 0; i < 9; i++)
                if (i != 4 && subCells[(my * 3 + i / 3) * side + mx * 3 + i % 3] == dark) matching++;
            foreach (var i in MajorityOrder) {
                if (matching >= minimumMatching) break;
                var index = (my * 3 + i / 3) * side + mx * 3 + i % 3;
                if (subCells[index] == dark) continue;
                subCells[index] = dark;
                matching++;
            }
        }
    }

    private static int Percentile(int[] histogram, int count, double fraction) {
        var target = count * fraction;
        var seen = 0;
        for (var i = 0; i < histogram.Length; i++) {
            seen += histogram[i];
            if (seen >= target) return i;
        }
        return histogram.Length - 1;
    }
}

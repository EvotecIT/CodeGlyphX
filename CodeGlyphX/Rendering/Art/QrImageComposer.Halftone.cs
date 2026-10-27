using System;
using System.Threading;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    // Above this weight a module's average tone no longer follows its bit, and downscaled or slightly blurred
    // copies of dense codes stop decoding. Higher Strength values are therefore capped here.
    private const double MaxHalftoneImageWeight = 0.7;
    private const double MonoToneExponent = 0.65;
    private const double DuotoneToneExponent = 0.6;
    private const double MinHalftoneTone = 56;
    private const double LocalContrast = 0.5;
    private const double Equalisation = 0.3;
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
        SeparateTones(tones, histogram, side, duotone ? DuotoneToneExponent : MonoToneExponent, cancellationToken);
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

    // Mid-tone photos dither into an even grey noise, so the tones are separated before thresholding:
    //  1. the 2nd..98th percentile is stretched to the full range (nearly flat images are left alone);
    //  2. local contrast (unsharp mask on luminance, radius about 1/16 of the symbol) separates neighbouring regions
    //     of similar tone, such as hills against sky;
    //  3. half-strength histogram equalisation spreads the tones the image actually uses;
    //  4. a lift curve keeps mid-tones open (duotone ink reads heavier than black, so it is lifted further), and the
    //     range is clamped so even deep shadow keeps some paper and highlights keep a few dots.
    private static void SeparateTones(double[] tones, int[] histogram, int side, double exponent, CancellationToken cancellationToken) {
        var low = Percentile(histogram, tones.Length, 0.02);
        var high = Percentile(histogram, tones.Length, 0.98);
        if (high - low >= 32 && high - low < 255)
            for (var i = 0; i < tones.Length; i++) tones[i] = Clamp255((tones[i] - low) * 255.0 / (high - low));
        cancellationToken.ThrowIfCancellationRequested();
        var radius = Math.Max(2, side / 16);
        var integral = new double[(side + 1) * (side + 1)];
        for (var y = 0; y < side; y++) {
            var row = 0.0;
            for (var x = 0; x < side; x++) {
                row += tones[y * side + x];
                integral[(y + 1) * (side + 1) + x + 1] = integral[y * (side + 1) + x + 1] + row;
            }
        }
        var sharpened = new double[tones.Length];
        for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) {
            int x0 = Math.Max(0, x - radius), x1 = Math.Min(side, x + radius + 1);
            int y0 = Math.Max(0, y - radius), y1 = Math.Min(side, y + radius + 1);
            var sum = integral[y1 * (side + 1) + x1] - integral[y0 * (side + 1) + x1] - integral[y1 * (side + 1) + x0] + integral[y0 * (side + 1) + x0];
            var mean = sum / ((x1 - x0) * (y1 - y0));
            var value = tones[y * side + x];
            sharpened[y * side + x] = Clamp255(value + LocalContrast * (value - mean));
        }
        cancellationToken.ThrowIfCancellationRequested();
        Array.Clear(histogram, 0, histogram.Length);
        foreach (var value in sharpened) histogram[(int)Math.Round(value)]++;
        var cumulative = new double[256];
        var running = 0;
        for (var i = 0; i < 256; i++) { running += histogram[i]; cumulative[i] = 255.0 * running / sharpened.Length; }
        for (var i = 0; i < tones.Length; i++) {
            var value = sharpened[i];
            value = (1 - Equalisation) * value + Equalisation * cumulative[(int)Math.Round(value)];
            value = 255 * Math.Pow(value / 255, exponent);
            tones[i] = MinHalftoneTone + value * (MaxHalftoneTone - MinHalftoneTone) / 255;
        }
    }

    private static double Clamp255(double value) => value < 0 ? 0 : value > 255 ? 255 : value;

    // Edge neighbours of the centre first, so a corrected module reads as a small plus rather than a solid block.
    private static readonly int[] MajorityOrder = { 1, 3, 5, 7, 0, 2, 6, 8 };

    // At small print or display sizes a reader sees each module's average tone. Where the image strongly opposes
    // the encoded bit, at least five of the nine sub-cells (the forced centre included) are given the bit's colour.
    private static void EnsureModuleMajority(bool[] subCells, BitMatrix modules, int side) {
        const int minimumMatching = 4;
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

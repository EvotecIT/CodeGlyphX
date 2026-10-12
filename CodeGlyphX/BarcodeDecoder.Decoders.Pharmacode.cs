using CodeGlyphX.Pharmacode;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryDecodePharmacode(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 3 || !modules[0]) return false;

        var runs = GetRuns(modules);
        if ((runs.Length & 1) == 0) return false;
        var barCount = (runs.Length + 1) / 2;
        if (barCount < PharmacodeEncoder.MinBars || barCount > PharmacodeEncoder.MaxBars) return false;

        var minimumBar = int.MaxValue;
        var maximumBar = 0;
        for (var i = 0; i < runs.Length; i += 2) {
            minimumBar = System.Math.Min(minimumBar, runs[i]);
            maximumBar = System.Math.Max(maximumBar, runs[i]);
        }
        // Laetus's standard and miniature width ranges allow at most 6.25:1.
        // Leave room for pixel rounding while rejecting grossly oversized runs,
        // including quiet zones interpreted as bars after polarity inversion.
        if ((long)maximumBar > (long)minimumBar * 7) return false;
        if (!TryGetRunThreshold(runs, startIndex: 0, step: 2, out var threshold)) return false;
        if (!AreRunsBelowThreshold(runs, startIndex: 1, step: 2, threshold)) return false;

        var value = 0;
        for (var i = 0; i < barCount; i++) {
            var wide = runs[i * 2] > threshold;
            var weight = 1 << (barCount - 1 - i);
            value += (wide ? 2 : 1) * weight;
        }
        if (value < PharmacodeEncoder.MinValue || value > PharmacodeEncoder.MaxValue) return false;
        text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }
}

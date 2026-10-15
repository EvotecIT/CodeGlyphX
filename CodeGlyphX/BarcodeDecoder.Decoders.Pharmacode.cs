using CodeGlyphX.Pharmacode;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryDecodePharmacodeWithBackground(ref ModuleRunWorkspace runWorkspace, bool[] physicalModules,
        bool? backgroundIsDark, bool allowPharmacode, out string text) {
        if (!TryDecodePharmacode(ref runWorkspace, out text)) return false;
        // Perform the additional image check only for a valid interpretation
        // that conflicts with the proposed endpoint polarity. Other formats and
        // ordinary successful Pharmacodes retain their existing search cost.
        if (allowPharmacode || !backgroundIsDark.HasValue ||
            !HasPharmacodeBackgroundEvidence(physicalModules, backgroundIsDark.Value)) return true;
        text = string.Empty;
        return false;
    }

    /// <summary>
    /// Confirms one or both quiet margins around a valid foreground symbol, or
    /// around valid neighbors separated by a wider background gap. An endpoint
    /// can instead be an actual outer bar in a tightly cropped image.
    /// </summary>
    private static bool HasPharmacodeBackgroundEvidence(bool[] modules, bool backgroundIsDark) {
        var first = 0;
        while (first < modules.Length && modules[first] == backgroundIsDark) first++;
        var last = modules.Length - 1;
        while (last >= first && modules[last] == backgroundIsDark) last--;
        if (last < first) return false;
        var foreground = new bool[last - first + 1];
        for (var i = 0; i < foreground.Length; i++) foreground[i] = modules[first + i] != backgroundIsDark;
        var runWorkspace = new ModuleRunWorkspace(foreground);
        try {
            var runs = runWorkspace.Runs;
            var minimumBar = int.MaxValue;
            for (var i = 0; i < runs.Length; i += 2) {
                minimumBar = System.Math.Min(minimumBar, runs[i]);
            }
            if (first == 0 && last == modules.Length - 1) return false;
            if (first > 0 && (long)first < (long)minimumBar * 2) return false;
            if (last < modules.Length - 1 && (long)modules.Length - last - 1 < (long)minimumBar * 2) return false;
            if (TryDecodePharmacode(ref runWorkspace, out _)) return true;
            var partStart = 0;
            var offset = 0;
            var parts = 0;
            for (var i = 0; i < runs.Length; i++) {
                var gap = runs[i];
                if ((i & 1) != 0 && (long)gap >= (long)minimumBar * 4) {
                    if (!ValidPart(partStart, offset)) return false;
                    partStart = offset + gap;
                    parts++;
                }
                offset += gap;
            }
            return parts > 0 && ValidPart(partStart, foreground.Length);

            bool ValidPart(int start, int end) {
                var part = new bool[end - start];
                System.Array.Copy(foreground, start, part, 0, part.Length);
                return TryDecodePharmacode(part, out _);
            }
        } finally {
            runWorkspace.Dispose();
        }
    }

    private static bool TryDecodePharmacode(bool[] modules, out string text) {
        var runWorkspace = new ModuleRunWorkspace(modules);
        try {
            return TryDecodePharmacode(ref runWorkspace, out text);
        } finally {
            runWorkspace.Dispose();
        }
    }

    private static bool TryDecodePharmacode(ref ModuleRunWorkspace runWorkspace, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 3 || !modules[0]) return false;

        var runs = runWorkspace.Runs;
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
        // Allow one sampled module at both ends for raster and pitch rounding.
        // Image candidates also retain background polarity: a width ratio alone
        // cannot distinguish a short joining quiet zone from a real wide bar.
        if ((long)(maximumBar - 1) * 4 > ((long)minimumBar + 1) * 25) return false;
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

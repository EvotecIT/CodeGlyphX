using System;

namespace CodeGlyphX;

internal static class QrDecodeConfidence {
    public static double Estimate(in QrPixelDecodeInfo info) {
        if (!info.IsSuccess) return 0.0;

        var score = 1.0;

        if (info.Scale > 1) score *= 0.97;
        if (info.Dimension <= 0) score *= 0.9;

        var dist = info.Module.FormatBestDistance;
        if (dist >= 0) {
            score *= Clamp(1.0 - dist * 0.12, 0.2, 1.0);
        } else {
            score *= 0.9;
        }

        var cand = info.CandidateCount;
        score *= cand switch {
            <= 0 => 0.7,
            <= 2 => 0.8,
            <= 4 => 0.9,
            _ => 1.0
        };

        return Clamp(score, 0.0, 1.0);
    }

    private static double Clamp(double value, double min, double max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

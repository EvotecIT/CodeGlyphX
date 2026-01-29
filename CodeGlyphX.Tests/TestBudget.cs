using System;

namespace CodeGlyphX.Tests;

internal static class TestBudget {
    private const int DefaultCoverageMultiplier = 4;

    public static int Adjust(int milliseconds) {
        if (milliseconds <= 0) return milliseconds;
        var multiplier = GetMultiplier();
        if (multiplier <= 1) return milliseconds;
        var scaled = (long)milliseconds * multiplier;
        return scaled > int.MaxValue ? int.MaxValue : (int)scaled;
    }

    private static int GetMultiplier() {
        var raw = Environment.GetEnvironmentVariable("CODEGLYPHX_TEST_BUDGET_MULTIPLIER");
        if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var configured) && configured > 1) {
            return configured;
        }

        var coverage = Environment.GetEnvironmentVariable("CODEGLYPHX_COVERAGE");
        return string.Equals(coverage, "1", StringComparison.OrdinalIgnoreCase) ? DefaultCoverageMultiplier : 1;
    }
}

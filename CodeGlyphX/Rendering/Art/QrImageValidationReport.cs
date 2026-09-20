using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Observed decode results for an exported image and a small set of deterministic transformations.</summary>
public sealed class QrImageValidationReport {
    /// <summary>Results for the original, half-size, and lightly blurred image.</summary>
    public IReadOnlyList<QrImageValidationCheck> Checks { get; }

    /// <summary>Whether all tested images decoded to the exact expected text. This is not a camera/print guarantee.</summary>
    public bool AllPassed { get; }

    internal QrImageValidationReport(QrImageValidationCheck[] checks) {
        Checks = System.Array.AsReadOnly(checks);
        AllPassed = true;
        foreach (var check in checks) if (!check.Passed) AllPassed = false;
    }
}

/// <summary>A single observed decode attempt, including an unsuccessful or budget-limited attempt.</summary>
public sealed class QrImageValidationCheck {
    /// <summary>The applied transformation: Original, HalfSize, or BoxBlur.</summary>
    public string Name { get; }
    /// <summary>Tested image width in pixels.</summary>
    public int Width { get; }
    /// <summary>Tested image height in pixels.</summary>
    public int Height { get; }
    /// <summary>Decoded text, or null when no symbol was recovered within the decode budget.</summary>
    public string? DecodedText { get; }
    /// <summary>Whether the recovered text exactly matches the expected payload.</summary>
    public bool Passed { get; }

    internal QrImageValidationCheck(string name, int width, int height, string? decodedText, string expected) {
        Name = name;
        Width = width;
        Height = height;
        DecodedText = decodedText;
        Passed = decodedText is not null && string.Equals(decodedText, expected, System.StringComparison.Ordinal);
    }
}

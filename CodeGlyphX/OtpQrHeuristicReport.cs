using System;

namespace CodeGlyphX;

/// <summary>
/// Result of static OTP QR rendering heuristics.
/// This report does not decode the rendered artifact.
/// </summary>
public sealed class OtpQrHeuristicReport {
    /// <summary>
    /// Gets the color contrast ratio.
    /// </summary>
    public double ContrastRatio { get; }
    /// <summary>
    /// Gets whether contrast meets the minimum recommendation.
    /// </summary>
    public bool HasSufficientContrast { get; }
    /// <summary>
    /// Gets whether quiet zone meets the minimum recommendation.
    /// </summary>
    public bool HasSufficientQuietZone { get; }
    /// <summary>
    /// Gets whether module size meets the minimum recommendation.
    /// </summary>
    public bool HasSufficientModuleSize { get; }
    /// <summary>
    /// Gets whether colors are fully opaque.
    /// </summary>
    public bool HasOpaqueColors { get; }
    /// <summary>
    /// Gets whether error correction level matches OTP recommendation.
    /// </summary>
    public bool HasRecommendedErrorCorrection { get; }
    /// <summary>
    /// Gets the recommended module size.
    /// </summary>
    public int RecommendedModuleSize { get; }
    /// <summary>
    /// Gets the recommended quiet zone size in modules.
    /// </summary>
    public int RecommendedQuietZone { get; }

    /// <summary>
    /// Gets whether the configuration passes the static OTP checks.
    /// Passing does not guarantee scanner or authenticator interoperability.
    /// </summary>
    public bool PassesHeuristics =>
        HasSufficientContrast &&
        HasSufficientQuietZone &&
        HasSufficientModuleSize &&
        HasOpaqueColors &&
        HasRecommendedErrorCorrection;

    /// <summary>
    /// Gets a 0-100 score (higher is better).
    /// </summary>
    public int Score { get; }
    /// <summary>
    /// Gets any warnings or suggestions.
    /// </summary>
    public string[] Issues { get; }

    internal OtpQrHeuristicReport(OtpQrHeuristicAssessment assessment) {
        ContrastRatio = assessment.ContrastRatio;
        HasSufficientContrast = assessment.HasSufficientContrast;
        HasSufficientQuietZone = assessment.HasSufficientQuietZone;
        HasSufficientModuleSize = assessment.HasSufficientModuleSize;
        HasOpaqueColors = assessment.HasOpaqueColors;
        HasRecommendedErrorCorrection = assessment.HasRecommendedErrorCorrection;
        RecommendedModuleSize = assessment.RecommendedModuleSize;
        RecommendedQuietZone = assessment.RecommendedQuietZone;
        Score = assessment.Score;
        Issues = assessment.Issues ?? Array.Empty<string>();
    }
}

internal struct OtpQrHeuristicAssessment {
    public double ContrastRatio { get; set; }
    public bool HasSufficientContrast { get; set; }
    public bool HasSufficientQuietZone { get; set; }
    public bool HasSufficientModuleSize { get; set; }
    public bool HasOpaqueColors { get; set; }
    public bool HasRecommendedErrorCorrection { get; set; }
    public int RecommendedModuleSize { get; set; }
    public int RecommendedQuietZone { get; set; }
    public int Score { get; set; }
    public string[]? Issues { get; set; }
}
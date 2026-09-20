using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Bounded artwork search over all eight QR masks, error correction, and nearby versions.</summary>
public sealed class QrImageSearchOptions {
    /// <summary>Composition applied to every candidate.</summary>
    public QrImageCompositionOptions Composition { get; set; } = new QrImageCompositionOptions { Art = new QrImageArtOptions() };
    /// <summary>Also screen four deterministic crop/placement/scale variations around the configured layout.</summary>
    public bool ExploreLayouts { get; set; }
    /// <summary>Try Q as well as H error correction. H is always included.</summary>
    public bool IncludeQuartileErrorCorrection { get; set; } = true;
    /// <summary>Additional versions above the smallest fitting version for each ECC level (0..2).</summary>
    public int AdditionalVersions { get; set; } = 1;
    /// <summary>Highest-fidelity candidates to decode before ranking (1..16).</summary>
    public int ValidationCandidates { get; set; } = 6;
    /// <summary>Number of measured alternatives to return (1..ValidationCandidates).</summary>
    public int Results { get; set; } = 3;
    /// <summary>Cooperative recognition budget for each original, half-size and blurred check (milliseconds).</summary>
    public int DecodeBudgetMilliseconds { get; set; } = 1000;
    internal void Validate() {
        if (Composition is null) throw new ArgumentNullException(nameof(Composition));
        Composition.Validate();
        if (AdditionalVersions < 0 || AdditionalVersions > 2) throw new ArgumentOutOfRangeException(nameof(AdditionalVersions));
        if (ValidationCandidates < 1 || ValidationCandidates > 16) throw new ArgumentOutOfRangeException(nameof(ValidationCandidates));
        if (Results < 1 || Results > ValidationCandidates) throw new ArgumentOutOfRangeException(nameof(Results));
        if (DecodeBudgetMilliseconds < 1 || DecodeBudgetMilliseconds > 10000) throw new ArgumentOutOfRangeException(nameof(DecodeBudgetMilliseconds));
    }
}

/// <summary>A rendered alternative with measured image fidelity and observed recognition results.</summary>
public sealed class QrImageCandidate {
    /// <summary>Encoded symbol, including its version, ECC level and mask.</summary>
    public QrCode Code { get; }
    /// <summary>Measured crop and placement used to render this alternative.</summary>
    public QrImageLayout Layout { get; }
    /// <summary>Final artwork, including its canvas.</summary>
    public QrImageComposition Image { get; }
    /// <summary>Source-image fidelity score (0..100), weighted toward the protected subject. Not a scan-confidence score.</summary>
    public double Fidelity { get; }
    /// <summary>Observed exact-payload decode results.</summary>
    public QrImageValidationReport Validation { get; }
    internal QrImageCandidate(QrCode code, QrImageComposition image, double fidelity, QrImageValidationReport validation, QrImageCompositionOptions composition) {
        Layout = new QrImageLayout(composition);
        Code = code; Image = image; Fidelity = fidelity; Validation = validation;
    }
}

/// <summary>Search results: measured candidates first by passed decode checks, then image fidelity.</summary>
public sealed class QrImageSearchResult {
    /// <summary>Number of mask/version/ECC combinations scored for fidelity.</summary>
    public int EvaluatedCandidates { get; }
    /// <summary>Number of candidates actually decoded; others were screened by fidelity only.</summary>
    public int ValidatedCandidates { get; }
    /// <summary>Ranked measured alternatives. Inspect Validation; results may include failed checks.</summary>
    public IReadOnlyList<QrImageCandidate> Candidates { get; }
    internal QrImageSearchResult(int evaluated, int validated, QrImageCandidate[] candidates) {
        EvaluatedCandidates = evaluated; ValidatedCandidates = validated; Candidates = Array.AsReadOnly(candidates);
    }
}

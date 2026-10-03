using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A bounded search which changes only QR scale and optionally its data-module shape.</summary>
public sealed class QrSceneSearchOptions {
    /// <summary>Maximum candidate renders and complete delivery reports (1..6).</summary>
    public int MaxCandidates { get; set; } = 6;
    /// <summary>Largest QR scale to try, including its quiet zone (0.25..0.8). It cannot shrink the requested QR.</summary>
    public double MaxQrScale { get; set; } = 0.8;
    /// <summary>Whether to try square data modules when the requested artistic shape performs poorly.</summary>
    public bool AllowSquareFallback { get; set; } = true;
    /// <summary>Requested screen, print, JPEG and perspective delivery simulations.</summary>
    public QrImageDeliveryOptions Delivery { get; set; } = new();
    internal QrSceneSearchOptions Copy() {
        if (MaxCandidates < 1 || MaxCandidates > 6) throw new ArgumentOutOfRangeException(nameof(MaxCandidates));
        if (double.IsNaN(MaxQrScale) || MaxQrScale < .25 || MaxQrScale > .8) throw new ArgumentOutOfRangeException(nameof(MaxQrScale));
        if (Delivery is null) throw new ArgumentNullException(nameof(Delivery));
        var d = new QrImageDeliveryOptions { ScreenSize = Delivery.ScreenSize, JpegQuality = Delivery.JpegQuality,
            PerspectiveInset = Delivery.PerspectiveInset, PrintMillimeters = Delivery.PrintMillimeters,
            PrintDpi = Delivery.PrintDpi, DecodeBudgetMilliseconds = Delivery.DecodeBudgetMilliseconds };
        d.Validate();
        return new() { MaxCandidates = MaxCandidates, MaxQrScale = MaxQrScale, AllowSquareFallback = AllowSquareFallback, Delivery = d };
    }
}

/// <summary>One retained scene with a delivery report measured against its exact PNG and payload.</summary>
public sealed class QrSceneCandidate {
    /// <summary>The candidate's immutable scene, including a copied editable design.</summary>
    public QrSceneComposition Scene { get; }
    /// <summary>Exact-payload observations for this candidate's PNG and delivery simulations.</summary>
    public QrImageValidationReport Validation { get; }
    /// <summary>Number of successful checks, used for stable ranking. Equal scores prefer fewer design changes.</summary>
    public int PassedChecks { get; }
    internal QrSceneCandidate(QrSceneComposition scene, QrImageValidationReport validation) {
        Scene = scene; Validation = validation;
        foreach (var check in validation.Checks) if (check.Passed) PassedChecks++;
    }
}

/// <summary>Bounded delivery-aware scene selection. A best candidate can still have failed checks.</summary>
public sealed class QrSceneSearchResult {
    /// <summary>Best observed candidate. Equal scores retain the earliest, least changed design.</summary>
    public QrSceneCandidate Best { get; }
    /// <summary>Candidate settings considered, including placements rejected before rendering.</summary>
    public int AttemptedCandidates { get; }
    /// <summary>Number of PNGs rendered and validated. Search stops once every check passes.</summary>
    public int ValidatedCandidates { get; }
    /// <summary>Placements rejected because the enlarged QR or its quiet zone could not fit.</summary>
    public int RejectedPlacements => AttemptedCandidates - ValidatedCandidates;
    internal QrSceneSearchResult(QrSceneCandidate best, int attempted, int validated) { Best = best; AttemptedCandidates = attempted; ValidatedCandidates = validated; }
}

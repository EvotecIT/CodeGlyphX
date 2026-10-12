#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>
    /// Fits all three observed finder centers and the alignment center to one
    /// projective mapping. Estimating only an outer corner can move the finder
    /// centers off their grid coordinates when perspective changes local pitch.
    /// </summary>
    private static bool TrySampleWithAlignmentLandmarks(QrGrayImage image, bool invert, int dimension,
        global::CodeGlyphX.BitMatrix scratch, QrFinderPatternDetector.FinderPattern tl,
        QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl,
        int alignmentPosition, double alignmentX, double alignmentY, double moduleSize,
        Func<QrDecoded, bool>? accept, bool aggressive, bool stylized, DecodeBudget budget,
        out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;
        if (budget.IsExpired) return false;

        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            3.5, 3.5,
            dimension - 3.5, 3.5,
            alignmentPosition + 0.5, alignmentPosition + 0.5,
            3.5, dimension - 3.5,
            tl.X, tl.Y,
            tr.X, tr.Y,
            alignmentX, alignmentY,
            bl.X, bl.Y);
        transform.Transform(0, 0, out var tlX, out var tlY);
        transform.Transform(dimension, 0, out var trX, out var trY);
        transform.Transform(dimension, dimension, out var brX, out var brY);
        transform.Transform(0, dimension, out var blX, out var blY);
        if (!double.IsFinite(tlX) || !double.IsFinite(tlY) || !double.IsFinite(trX) || !double.IsFinite(trY) ||
            !double.IsFinite(brX) || !double.IsFinite(brY) || !double.IsFinite(blX) || !double.IsFinite(blY)) return false;

        // The transform already anchors the observed centers. Affine phase offsets
        // would shift those anchors a second time. Keep the existing sampler and
        // its structural, error-correction, payload, acceptance and budget checks.
        return TrySampleWithCorners(image, invert, 0, 0, dimension, scratch,
            tlX, tlY, trX, trY, brX, brY, blX, blY, moduleSize, accept,
            aggressive, stylized, budget, out result, out diagnostics);
    }
}
#endif

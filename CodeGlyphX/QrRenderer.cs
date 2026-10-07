using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Vector;

namespace CodeGlyphX;

/// <summary>Canonical adapter from QR appearance settings to the output renderers.</summary>
internal static partial class QrRenderer {
    internal static QrArtHeuristicReport EvaluateScanHeuristics(QrCode qr, QrRenderOptions? options) {
        var opts = options is null ? new QrRenderOptions() : CloneOptions(options);
        ValidateOptions(opts);
        return QrArtHeuristics.Evaluate(qr, BuildPngOptions(opts, qr));
    }

    private static void ValidateOptions(QrRenderOptions opts) {
        if (opts.Style is < QrRenderStyle.Default or > QrRenderStyle.Fancy) throw new ArgumentOutOfRangeException(nameof(opts.Style));
        if (opts.ModuleSize < 1) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.TargetSizePx < 0) throw new ArgumentOutOfRangeException(nameof(opts.TargetSizePx));
        if (opts.ArtGuardrailMinimumScore is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(opts.ArtGuardrailMinimumScore));
    }

    private static void ValidateFormatStyles(OutputFormat format, QrRenderOptions opts) =>
        QrVectorStyleValidation.Validate(format, opts);
}

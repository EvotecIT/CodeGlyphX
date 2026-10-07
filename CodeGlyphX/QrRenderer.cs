using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

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

    private static void ValidateFormatStyles(OutputFormat format, QrRenderOptions opts) {
        if (format != OutputFormat.Svg && format != OutputFormat.Svgz && format != OutputFormat.Html) return;
        var unsupported = new List<string>();
        if (opts.Art is not null) unsupported.Add(nameof(opts.Art));
        if (opts.BackgroundGradient is not null) unsupported.Add(nameof(opts.BackgroundGradient));
        if (opts.BackgroundPattern is not null) unsupported.Add(nameof(opts.BackgroundPattern));
        if (opts.ForegroundPalette is not null) unsupported.Add(nameof(opts.ForegroundPalette));
        if (opts.ForegroundPattern is not null) unsupported.Add(nameof(opts.ForegroundPattern));
        if (opts.ForegroundPaletteZones is not null) unsupported.Add(nameof(opts.ForegroundPaletteZones));
        if (opts.ModuleScaleMap is not null) unsupported.Add(nameof(opts.ModuleScaleMap));
        if (opts.ModuleShapeMap is not null) unsupported.Add(nameof(opts.ModuleShapeMap));
        if (opts.ModuleJitter is not null) unsupported.Add(nameof(opts.ModuleJitter));
        if (opts.Canvas is not null) unsupported.Add(nameof(opts.Canvas));
        if (opts.Debug is not null) unsupported.Add(nameof(opts.Debug));
        if (opts.ModuleShape is QrPngModuleShape.ConnectedRounded or QrPngModuleShape.ConnectedSquircle) unsupported.Add(nameof(opts.ModuleShape));
        var eyes = opts.Eyes;
        if (eyes is not null) {
            if (eyes.FrameStyle != QrPngEyeFrameStyle.Single) unsupported.Add("Eyes.FrameStyle");
            if (eyes.OuterColors is not null) unsupported.Add("Eyes.OuterColors");
            if (eyes.InnerColors is not null) unsupported.Add("Eyes.InnerColors");
            if (eyes.OuterGradients is not null) unsupported.Add("Eyes.OuterGradients");
            if (eyes.InnerGradients is not null) unsupported.Add("Eyes.InnerGradients");
            if (eyes.GlowRadiusPx > 0) unsupported.Add("Eyes.GlowRadiusPx");
            if (eyes.SparkleCount > 0) unsupported.Add("Eyes.SparkleCount");
            if (eyes.AccentRingCount > 0) unsupported.Add("Eyes.AccentRingCount");
            if (eyes.AccentRayCount > 0) unsupported.Add("Eyes.AccentRayCount");
            if (eyes.AccentStripeCount > 0) unsupported.Add("Eyes.AccentStripeCount");
            if (eyes.OuterShape is QrPngModuleShape.ConnectedRounded or QrPngModuleShape.ConnectedSquircle) unsupported.Add("Eyes.OuterShape");
            if (eyes.InnerShape is QrPngModuleShape.ConnectedRounded or QrPngModuleShape.ConnectedSquircle) unsupported.Add("Eyes.InnerShape");
        }
        if (unsupported.Count > 0) {
            throw new NotSupportedException($"{format} cannot represent these QR appearance settings: {string.Join(", ", unsupported)}. Use PNG or another raster output format.");
        }
    }
}

using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Vector;

/// <summary>Rejects QR appearance settings that the SVG and HTML renderers cannot preserve.</summary>
internal static class QrVectorStyleValidation {
    internal static void Validate(OutputFormat format, QrRenderOptions options) {
        if (format != OutputFormat.Svg && format != OutputFormat.Svgz && format != OutputFormat.Html) return;
        var unsupported = new List<string>();
        if (options.Art is not null) unsupported.Add(nameof(options.Art));
        if (options.BackgroundGradient is not null) unsupported.Add(nameof(options.BackgroundGradient));
        if (options.BackgroundPattern is not null) unsupported.Add(nameof(options.BackgroundPattern));
        if (options.ForegroundPalette is not null) unsupported.Add(nameof(options.ForegroundPalette));
        if (options.ForegroundPattern is not null) unsupported.Add(nameof(options.ForegroundPattern));
        if (options.ForegroundPaletteZones is not null) unsupported.Add(nameof(options.ForegroundPaletteZones));
        if (options.ModuleScaleMap is not null) unsupported.Add(nameof(options.ModuleScaleMap));
        if (options.ModuleShapeMap is not null) unsupported.Add(nameof(options.ModuleShapeMap));
        if (options.ModuleJitter is not null) unsupported.Add(nameof(options.ModuleJitter));
        if (options.Canvas is not null) unsupported.Add(nameof(options.Canvas));
        if (options.Debug is not null) unsupported.Add(nameof(options.Debug));
        AddUnsupportedModuleAndEyeStyles(unsupported, options.ModuleShape, options.Eyes);
        ThrowIfUnsupported(format, unsupported);
    }

    internal static void ValidateSvg(QrModuleShape moduleShape, QrEyeOptions? eyes) {
        var unsupported = new List<string>();
        AddUnsupportedModuleAndEyeStyles(unsupported, moduleShape, eyes);
        ThrowIfUnsupported(OutputFormat.Svg, unsupported);
    }

    private static void AddUnsupportedModuleAndEyeStyles(List<string> unsupported, QrModuleShape? moduleShape, QrEyeOptions? eyes) {
        if (moduleShape is QrModuleShape.ConnectedRounded or QrModuleShape.ConnectedSquircle) unsupported.Add(nameof(QrRenderOptions.ModuleShape));
        if (eyes is null) return;
        if (eyes.FrameStyle != QrEyeFrameStyle.Single) unsupported.Add("Eyes.FrameStyle");
        if (eyes.OuterColors is not null) unsupported.Add("Eyes.OuterColors");
        if (eyes.InnerColors is not null) unsupported.Add("Eyes.InnerColors");
        if (eyes.OuterGradients is not null) unsupported.Add("Eyes.OuterGradients");
        if (eyes.InnerGradients is not null) unsupported.Add("Eyes.InnerGradients");
        if (eyes.GlowRadiusPx > 0) unsupported.Add("Eyes.GlowRadiusPx");
        if (eyes.SparkleCount > 0) unsupported.Add("Eyes.SparkleCount");
        if (eyes.AccentRingCount > 0) unsupported.Add("Eyes.AccentRingCount");
        if (eyes.AccentRayCount > 0) unsupported.Add("Eyes.AccentRayCount");
        if (eyes.AccentStripeCount > 0) unsupported.Add("Eyes.AccentStripeCount");
        if (eyes.OuterShape is QrModuleShape.ConnectedRounded or QrModuleShape.ConnectedSquircle) unsupported.Add("Eyes.OuterShape");
        if (eyes.InnerShape is QrModuleShape.ConnectedRounded or QrModuleShape.ConnectedSquircle) unsupported.Add("Eyes.InnerShape");
    }

    private static void ThrowIfUnsupported(OutputFormat format, List<string> unsupported) {
        if (unsupported.Count > 0) {
            throw new NotSupportedException($"{format} cannot represent these QR appearance settings: {string.Join(", ", unsupported)}. Use PNG or another raster output format.");
        }
    }
}

using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

internal static partial class QrRenderer {
    internal static QrRenderOptions CloneOptions(QrRenderOptions opts) {
        return new QrRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            TargetSizePx = opts.TargetSizePx,
            TargetSizeIncludesQuietZone = opts.TargetSizeIncludesQuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background,
            BackgroundGradient = CloneGradient(opts.BackgroundGradient),
            BackgroundPattern = CloneBackgroundPattern(opts.BackgroundPattern),
            BackgroundSupersample = opts.BackgroundSupersample,
            Style = opts.Style,
            Art = CloneArt(opts.Art),
            ArtGuardrailsEnabled = opts.ArtGuardrailsEnabled,
            ArtGuardrailMinimumScore = opts.ArtGuardrailMinimumScore,
            ModuleShape = opts.ModuleShape,
            ModuleScale = opts.ModuleScale,
            ModuleScaleMap = CloneScaleMap(opts.ModuleScaleMap),
            ModuleShapeMap = CloneShapeMap(opts.ModuleShapeMap),
            ModuleJitter = CloneJitter(opts.ModuleJitter),
            ProtectFunctionalPatterns = opts.ProtectFunctionalPatterns,
            ProtectQuietZone = opts.ProtectQuietZone,
            ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx,
            ForegroundGradient = CloneGradient(opts.ForegroundGradient),
            ForegroundPalette = ClonePalette(opts.ForegroundPalette),
            ForegroundPattern = CloneForegroundPattern(opts.ForegroundPattern),
            ForegroundPaletteZones = ClonePaletteZones(opts.ForegroundPaletteZones),
            Eyes = CloneEyes(opts.Eyes),
            Canvas = CloneCanvas(opts.Canvas),
            Debug = CloneDebug(opts.Debug),
            LogoPng = opts.LogoPng is null ? null : (byte[])opts.LogoPng.Clone(),
            LogoScale = opts.LogoScale,
            LogoPaddingPx = opts.LogoPaddingPx,
            LogoDrawBackground = opts.LogoDrawBackground,
            LogoBackground = opts.LogoBackground,
            LogoCornerRadiusPx = opts.LogoCornerRadiusPx,
        };
    }

    private static QrPngGradientOptions? CloneGradient(QrPngGradientOptions? gradient) {
        if (gradient is null) return null;
        return new QrPngGradientOptions {
            Type = gradient.Type,
            StartColor = gradient.StartColor,
            EndColor = gradient.EndColor,
            CenterX = gradient.CenterX,
            CenterY = gradient.CenterY,
        };
    }

    private static QrPngBackgroundPatternOptions? CloneBackgroundPattern(QrPngBackgroundPatternOptions? pattern) {
        if (pattern is null) return null;
        return new QrPngBackgroundPatternOptions {
            Type = pattern.Type,
            Color = pattern.Color,
            SizePx = pattern.SizePx,
            ThicknessPx = pattern.ThicknessPx,
            SnapToModuleSize = pattern.SnapToModuleSize,
            ModuleStep = pattern.ModuleStep,
        };
    }

    private static QrPngForegroundPatternOptions? CloneForegroundPattern(QrPngForegroundPatternOptions? pattern) {
        if (pattern is null) return null;
        return new QrPngForegroundPatternOptions {
            Type = pattern.Type,
            Color = pattern.Color,
            SizePx = pattern.SizePx,
            ThicknessPx = pattern.ThicknessPx,
            Seed = pattern.Seed,
            Variation = pattern.Variation,
            Density = pattern.Density,
            SnapToModuleSize = pattern.SnapToModuleSize,
            ModuleStep = pattern.ModuleStep,
            ApplyToModules = pattern.ApplyToModules,
            ApplyToEyes = pattern.ApplyToEyes,
            BlendMode = pattern.BlendMode,
        };
    }

    private static QrPngModuleScaleMapOptions? CloneScaleMap(QrPngModuleScaleMapOptions? map) {
        if (map is null) return null;
        return new QrPngModuleScaleMapOptions {
            Mode = map.Mode,
            MinScale = map.MinScale,
            MaxScale = map.MaxScale,
            RingSize = map.RingSize,
            Seed = map.Seed,
            ApplyToEyes = map.ApplyToEyes,
        };
    }

    private static QrPngModuleShapeMapOptions? CloneShapeMap(QrPngModuleShapeMapOptions? map) {
        if (map is null) return null;
        return new QrPngModuleShapeMapOptions {
            Mode = map.Mode,
            PrimaryShape = map.PrimaryShape,
            SecondaryShape = map.SecondaryShape,
            Split = map.Split,
            RingSize = map.RingSize,
            Seed = map.Seed,
            SecondaryChance = map.SecondaryChance,
            CornerSize = map.CornerSize,
            ApplyToEyes = map.ApplyToEyes,
            ProtectFunctionalPatterns = map.ProtectFunctionalPatterns,
        };
    }

    private static QrPngModuleJitterOptions? CloneJitter(QrPngModuleJitterOptions? jitter) {
        if (jitter is null) return null;
        return new QrPngModuleJitterOptions {
            MaxOffsetPx = jitter.MaxOffsetPx,
            Seed = jitter.Seed,
            ApplyToEyes = jitter.ApplyToEyes,
            ProtectFunctionalPatterns = jitter.ProtectFunctionalPatterns,
            ClampToShape = jitter.ClampToShape,
        };
    }


    private static QrPngPaletteOptions? ClonePalette(QrPngPaletteOptions? palette) {
        if (palette is null) return null;
        var colors = palette.Colors;
        var colorsCopy = colors is null ? null : (Rgba32[])colors.Clone();
        return new QrPngPaletteOptions {
            Colors = colorsCopy ?? new[] { Rgba32.Black },
            Mode = palette.Mode,
            Seed = palette.Seed,
            RingSize = palette.RingSize,
            ApplyToEyes = palette.ApplyToEyes,
        };
    }

    private static QrPngPaletteZoneOptions? ClonePaletteZones(QrPngPaletteZoneOptions? zones) {
        if (zones is null) return null;
        return new QrPngPaletteZoneOptions {
            CenterPalette = ClonePalette(zones.CenterPalette),
            CenterSize = zones.CenterSize,
            CornerPalette = ClonePalette(zones.CornerPalette),
            CornerSize = zones.CornerSize,
        };
    }

    private static QrPngEyeOptions? CloneEyes(QrPngEyeOptions? eyes) {
        if (eyes is null) return null;
        return new QrPngEyeOptions {
            UseFrame = eyes.UseFrame,
            FrameStyle = eyes.FrameStyle,
            OuterShape = eyes.OuterShape,
            InnerShape = eyes.InnerShape,
            OuterScale = eyes.OuterScale,
            InnerScale = eyes.InnerScale,
            OuterCornerRadiusPx = eyes.OuterCornerRadiusPx,
            InnerCornerRadiusPx = eyes.InnerCornerRadiusPx,
            OuterColor = eyes.OuterColor,
            OuterColors = eyes.OuterColors is null ? null : (Rgba32[])eyes.OuterColors.Clone(),
            InnerColor = eyes.InnerColor,
            InnerColors = eyes.InnerColors is null ? null : (Rgba32[])eyes.InnerColors.Clone(),
            OuterGradient = CloneGradient(eyes.OuterGradient),
            OuterGradients = CloneGradientArray(eyes.OuterGradients),
            InnerGradient = CloneGradient(eyes.InnerGradient),
            InnerGradients = CloneGradientArray(eyes.InnerGradients),
            GlowRadiusPx = eyes.GlowRadiusPx,
            GlowColor = eyes.GlowColor,
            GlowAlpha = eyes.GlowAlpha,
            SparkleCount = eyes.SparkleCount,
            SparkleRadiusPx = eyes.SparkleRadiusPx,
            SparkleSpreadPx = eyes.SparkleSpreadPx,
            SparkleColor = eyes.SparkleColor,
            SparkleSeed = eyes.SparkleSeed,
            SparkleProtectQrArea = eyes.SparkleProtectQrArea,
            SparkleAllowOnQrBackground = eyes.SparkleAllowOnQrBackground,
            AccentRingCount = eyes.AccentRingCount,
            AccentRingThicknessPx = eyes.AccentRingThicknessPx,
            AccentRingSpreadPx = eyes.AccentRingSpreadPx,
            AccentRingJitterPx = eyes.AccentRingJitterPx,
            AccentRingColor = eyes.AccentRingColor,
            AccentRingSeed = eyes.AccentRingSeed,
            AccentRingProtectQrArea = eyes.AccentRingProtectQrArea,
            AccentRingAllowOnQrBackground = eyes.AccentRingAllowOnQrBackground,
            AccentRayCount = eyes.AccentRayCount,
            AccentRayLengthPx = eyes.AccentRayLengthPx,
            AccentRayThicknessPx = eyes.AccentRayThicknessPx,
            AccentRaySpreadPx = eyes.AccentRaySpreadPx,
            AccentRayJitterPx = eyes.AccentRayJitterPx,
            AccentRayLengthJitterPx = eyes.AccentRayLengthJitterPx,
            AccentRayColor = eyes.AccentRayColor,
            AccentRaySeed = eyes.AccentRaySeed,
            AccentRayProtectQrArea = eyes.AccentRayProtectQrArea,
            AccentRayAllowOnQrBackground = eyes.AccentRayAllowOnQrBackground,
            AccentStripeCount = eyes.AccentStripeCount,
            AccentStripeLengthPx = eyes.AccentStripeLengthPx,
            AccentStripeThicknessPx = eyes.AccentStripeThicknessPx,
            AccentStripeSpreadPx = eyes.AccentStripeSpreadPx,
            AccentStripeJitterPx = eyes.AccentStripeJitterPx,
            AccentStripeLengthJitterPx = eyes.AccentStripeLengthJitterPx,
            AccentStripeColor = eyes.AccentStripeColor,
            AccentStripeSeed = eyes.AccentStripeSeed,
            AccentStripeProtectQrArea = eyes.AccentStripeProtectQrArea,
            AccentStripeAllowOnQrBackground = eyes.AccentStripeAllowOnQrBackground,
        };
    }

    private static QrPngGradientOptions[]? CloneGradientArray(QrPngGradientOptions[]? gradients) {
        if (gradients is null) return null;
        var copy = new QrPngGradientOptions[gradients.Length];
        for (var i = 0; i < gradients.Length; i++) {
            copy[i] = CloneGradient(gradients[i])!;
        }
        return copy;
    }

    private static QrPngCanvasOptions? CloneCanvas(QrPngCanvasOptions? canvas) {
        if (canvas is null) return null;
        return new QrPngCanvasOptions {
            PaddingPx = canvas.PaddingPx,
            CornerRadiusPx = canvas.CornerRadiusPx,
            Background = canvas.Background,
            BackgroundGradient = CloneGradient(canvas.BackgroundGradient),
            Pattern = CloneBackgroundPattern(canvas.Pattern),
            Splash = CloneSplash(canvas.Splash),
            Halo = CloneHalo(canvas.Halo),
            Vignette = CloneVignette(canvas.Vignette),
            Grain = CloneGrain(canvas.Grain),
            Frame = CloneFrame(canvas.Frame),
            Band = CloneBand(canvas.Band),
            Badge = CloneBadge(canvas.Badge),
            BorderPx = canvas.BorderPx,
            BorderColor = canvas.BorderColor,
            ShadowOffsetX = canvas.ShadowOffsetX,
            ShadowOffsetY = canvas.ShadowOffsetY,
            ShadowColor = canvas.ShadowColor,
        };
    }

    private static QrPngCanvasBadgeOptions? CloneBadge(QrPngCanvasBadgeOptions? badge) {
        if (badge is null) return null;
        return new QrPngCanvasBadgeOptions {
            Shape = badge.Shape,
            Position = badge.Position,
            WidthPx = badge.WidthPx,
            HeightPx = badge.HeightPx,
            GapPx = badge.GapPx,
            OffsetPx = badge.OffsetPx,
            CornerRadiusPx = badge.CornerRadiusPx,
            Color = badge.Color,
            Gradient = CloneGradient(badge.Gradient),
            EdgePattern = CloneEdgePattern(badge.EdgePattern),
            TailPx = badge.TailPx,
        };
    }

    private static QrPngCanvasFrameOptions? CloneFrame(QrPngCanvasFrameOptions? frame) {
        if (frame is null) return null;
        return new QrPngCanvasFrameOptions {
            ThicknessPx = frame.ThicknessPx,
            GapPx = frame.GapPx,
            RadiusPx = frame.RadiusPx,
            Color = frame.Color,
            Gradient = CloneGradient(frame.Gradient),
            EdgePattern = CloneEdgePattern(frame.EdgePattern),
            InnerThicknessPx = frame.InnerThicknessPx,
            InnerGapPx = frame.InnerGapPx,
            InnerColor = frame.InnerColor,
            InnerGradient = CloneGradient(frame.InnerGradient),
            InnerEdgePattern = CloneEdgePattern(frame.InnerEdgePattern),
        };
    }

    private static QrPngCanvasBandOptions? CloneBand(QrPngCanvasBandOptions? band) {
        if (band is null) return null;
        return new QrPngCanvasBandOptions {
            BandPx = band.BandPx,
            GapPx = band.GapPx,
            RadiusPx = band.RadiusPx,
            Color = band.Color,
            Gradient = CloneGradient(band.Gradient),
            EdgePattern = CloneEdgePattern(band.EdgePattern),
        };
    }

    private static QrPngCanvasEdgePatternOptions? CloneEdgePattern(QrPngCanvasEdgePatternOptions? pattern) {
        if (pattern is null) return null;
        return new QrPngCanvasEdgePatternOptions {
            Type = pattern.Type,
            Color = pattern.Color,
            ThicknessPx = pattern.ThicknessPx,
            SpacingPx = pattern.SpacingPx,
            DashPx = pattern.DashPx,
            InsetPx = pattern.InsetPx,
        };
    }

    private static QrPngCanvasSplashOptions? CloneSplash(QrPngCanvasSplashOptions? splash) {
        if (splash is null) return null;
        return new QrPngCanvasSplashOptions {
            Color = splash.Color,
            Colors = splash.Colors is null ? null : (Rgba32[])splash.Colors.Clone(),
            Count = splash.Count,
            MinRadiusPx = splash.MinRadiusPx,
            MaxRadiusPx = splash.MaxRadiusPx,
            SpreadPx = splash.SpreadPx,
            Placement = splash.Placement,
            EdgeBandPx = splash.EdgeBandPx,
            Seed = splash.Seed,
            DripChance = splash.DripChance,
            DripLengthPx = splash.DripLengthPx,
            DripWidthPx = splash.DripWidthPx,
            ProtectQrArea = splash.ProtectQrArea,
            QrAreaAlphaMax = splash.QrAreaAlphaMax,
        };
    }

    private static QrPngCanvasHaloOptions? CloneHalo(QrPngCanvasHaloOptions? halo) {
        if (halo is null) return null;
        return new QrPngCanvasHaloOptions {
            Color = halo.Color,
            RadiusPx = halo.RadiusPx,
            ProtectQrArea = halo.ProtectQrArea,
            QrAreaAlphaMax = halo.QrAreaAlphaMax,
        };
    }

    private static QrPngCanvasVignetteOptions? CloneVignette(QrPngCanvasVignetteOptions? vignette) {
        if (vignette is null) return null;
        return new QrPngCanvasVignetteOptions {
            Color = vignette.Color,
            BandPx = vignette.BandPx,
            Strength = vignette.Strength,
            ProtectQrArea = vignette.ProtectQrArea,
            QrAreaAlphaMax = vignette.QrAreaAlphaMax,
        };
    }

    private static QrPngCanvasGrainOptions? CloneGrain(QrPngCanvasGrainOptions? grain) {
        if (grain is null) return null;
        return new QrPngCanvasGrainOptions {
            Color = grain.Color,
            Density = grain.Density,
            PixelSizePx = grain.PixelSizePx,
            AlphaJitter = grain.AlphaJitter,
            Seed = grain.Seed,
            BandPx = grain.BandPx,
            ProtectQrArea = grain.ProtectQrArea,
            QrAreaAlphaMax = grain.QrAreaAlphaMax,
        };
    }

    private static QrArtOptions? CloneArt(QrArtOptions? art) => art is null ? null : new QrArtOptions {
        Theme = art.Theme, Variant = art.Variant, Intensity = art.Intensity, GuardrailMode = art.GuardrailMode
    };

    private static QrPngDebugOptions? CloneDebug(QrPngDebugOptions? debug) => debug is null ? null : new QrPngDebugOptions {
        ShowQuietZone = debug.ShowQuietZone, ShowQrBounds = debug.ShowQrBounds,
        ShowEyeBounds = debug.ShowEyeBounds, ShowLogoBounds = debug.ShowLogoBounds,
        StrokePx = debug.StrokePx, QuietZoneColor = debug.QuietZoneColor,
        QrBoundsColor = debug.QrBoundsColor, EyeBoundsColor = debug.EyeBoundsColor, LogoBoundsColor = debug.LogoBoundsColor
    };
}

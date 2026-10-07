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

    private static QrGradientOptions? CloneGradient(QrGradientOptions? gradient) {
        if (gradient is null) return null;
        return new QrGradientOptions {
            Type = gradient.Type,
            StartColor = gradient.StartColor,
            EndColor = gradient.EndColor,
            CenterX = gradient.CenterX,
            CenterY = gradient.CenterY,
        };
    }

    private static QrBackgroundPatternOptions? CloneBackgroundPattern(QrBackgroundPatternOptions? pattern) {
        if (pattern is null) return null;
        return new QrBackgroundPatternOptions {
            Type = pattern.Type,
            Color = pattern.Color,
            SizePx = pattern.SizePx,
            ThicknessPx = pattern.ThicknessPx,
            SnapToModuleSize = pattern.SnapToModuleSize,
            ModuleStep = pattern.ModuleStep,
        };
    }

    private static QrForegroundPatternOptions? CloneForegroundPattern(QrForegroundPatternOptions? pattern) {
        if (pattern is null) return null;
        return new QrForegroundPatternOptions {
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

    private static QrModuleScaleMapOptions? CloneScaleMap(QrModuleScaleMapOptions? map) {
        if (map is null) return null;
        return new QrModuleScaleMapOptions {
            Mode = map.Mode,
            MinScale = map.MinScale,
            MaxScale = map.MaxScale,
            RingSize = map.RingSize,
            Seed = map.Seed,
            ApplyToEyes = map.ApplyToEyes,
        };
    }

    private static QrModuleShapeMapOptions? CloneShapeMap(QrModuleShapeMapOptions? map) {
        if (map is null) return null;
        return new QrModuleShapeMapOptions {
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

    private static QrModuleJitterOptions? CloneJitter(QrModuleJitterOptions? jitter) {
        if (jitter is null) return null;
        return new QrModuleJitterOptions {
            MaxOffsetPx = jitter.MaxOffsetPx,
            Seed = jitter.Seed,
            ApplyToEyes = jitter.ApplyToEyes,
            ProtectFunctionalPatterns = jitter.ProtectFunctionalPatterns,
            ClampToShape = jitter.ClampToShape,
        };
    }


    private static QrPaletteOptions? ClonePalette(QrPaletteOptions? palette) {
        if (palette is null) return null;
        var colors = palette.Colors;
        var colorsCopy = colors is null ? null : (Rgba32[])colors.Clone();
        return new QrPaletteOptions {
            Colors = colorsCopy ?? new[] { Rgba32.Black },
            Mode = palette.Mode,
            Seed = palette.Seed,
            RingSize = palette.RingSize,
            ApplyToEyes = palette.ApplyToEyes,
        };
    }

    private static QrPaletteZoneOptions? ClonePaletteZones(QrPaletteZoneOptions? zones) {
        if (zones is null) return null;
        return new QrPaletteZoneOptions {
            CenterPalette = ClonePalette(zones.CenterPalette),
            CenterSize = zones.CenterSize,
            CornerPalette = ClonePalette(zones.CornerPalette),
            CornerSize = zones.CornerSize,
        };
    }

    private static QrEyeOptions? CloneEyes(QrEyeOptions? eyes) {
        if (eyes is null) return null;
        return new QrEyeOptions {
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

    private static QrGradientOptions[]? CloneGradientArray(QrGradientOptions[]? gradients) {
        if (gradients is null) return null;
        var copy = new QrGradientOptions[gradients.Length];
        for (var i = 0; i < gradients.Length; i++) {
            copy[i] = CloneGradient(gradients[i])!;
        }
        return copy;
    }

    private static QrCanvasOptions? CloneCanvas(QrCanvasOptions? canvas) {
        if (canvas is null) return null;
        return new QrCanvasOptions {
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

    private static QrCanvasBadgeOptions? CloneBadge(QrCanvasBadgeOptions? badge) {
        if (badge is null) return null;
        return new QrCanvasBadgeOptions {
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

    private static QrCanvasFrameOptions? CloneFrame(QrCanvasFrameOptions? frame) {
        if (frame is null) return null;
        return new QrCanvasFrameOptions {
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

    private static QrCanvasBandOptions? CloneBand(QrCanvasBandOptions? band) {
        if (band is null) return null;
        return new QrCanvasBandOptions {
            BandPx = band.BandPx,
            GapPx = band.GapPx,
            RadiusPx = band.RadiusPx,
            Color = band.Color,
            Gradient = CloneGradient(band.Gradient),
            EdgePattern = CloneEdgePattern(band.EdgePattern),
        };
    }

    private static QrCanvasEdgePatternOptions? CloneEdgePattern(QrCanvasEdgePatternOptions? pattern) {
        if (pattern is null) return null;
        return new QrCanvasEdgePatternOptions {
            Type = pattern.Type,
            Color = pattern.Color,
            ThicknessPx = pattern.ThicknessPx,
            SpacingPx = pattern.SpacingPx,
            DashPx = pattern.DashPx,
            InsetPx = pattern.InsetPx,
        };
    }

    private static QrCanvasSplashOptions? CloneSplash(QrCanvasSplashOptions? splash) {
        if (splash is null) return null;
        return new QrCanvasSplashOptions {
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

    private static QrCanvasHaloOptions? CloneHalo(QrCanvasHaloOptions? halo) {
        if (halo is null) return null;
        return new QrCanvasHaloOptions {
            Color = halo.Color,
            RadiusPx = halo.RadiusPx,
            ProtectQrArea = halo.ProtectQrArea,
            QrAreaAlphaMax = halo.QrAreaAlphaMax,
        };
    }

    private static QrCanvasVignetteOptions? CloneVignette(QrCanvasVignetteOptions? vignette) {
        if (vignette is null) return null;
        return new QrCanvasVignetteOptions {
            Color = vignette.Color,
            BandPx = vignette.BandPx,
            Strength = vignette.Strength,
            ProtectQrArea = vignette.ProtectQrArea,
            QrAreaAlphaMax = vignette.QrAreaAlphaMax,
        };
    }

    private static QrCanvasGrainOptions? CloneGrain(QrCanvasGrainOptions? grain) {
        if (grain is null) return null;
        return new QrCanvasGrainOptions {
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

    private static QrRasterDebugOptions? CloneDebug(QrRasterDebugOptions? debug) => debug is null ? null : new QrRasterDebugOptions {
        ShowQuietZone = debug.ShowQuietZone, ShowQrBounds = debug.ShowQrBounds,
        ShowEyeBounds = debug.ShowEyeBounds, ShowLogoBounds = debug.ShowLogoBounds,
        StrokePx = debug.StrokePx, QuietZoneColor = debug.QuietZoneColor,
        QrBoundsColor = debug.QrBoundsColor, EyeBoundsColor = debug.EyeBoundsColor, LogoBoundsColor = debug.LogoBoundsColor
    };
}

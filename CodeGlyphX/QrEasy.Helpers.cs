using System;
using System.Globalization;
using System.IO;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class QrEasy {
    private static QrPngRenderOptions BuildPngOptions(QrEasyOptions opts, QrCode qr) {
        var moduleCount = qr.Modules.Width;
        var render = new QrPngRenderOptions {
            ModuleSize = ResolveModuleSize(opts, moduleCount),
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background,
            BackgroundGradient = opts.BackgroundGradient,
            BackgroundPattern = opts.BackgroundPattern,
            BackgroundSupersample = opts.BackgroundSupersample,
            ProtectFunctionalPatterns = opts.ProtectFunctionalPatterns,
            ProtectQuietZone = opts.ProtectQuietZone,
            Canvas = opts.Canvas,
            Debug = opts.Debug,
        };

        if (opts.Style == QrRenderStyle.Rounded) {
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.9;
            render.ModuleCornerRadiusPx = 2;
        } else if (opts.Style == QrRenderStyle.Fancy) {
            var start = opts.Foreground;
            var end = Blend(opts.Foreground, Rgba32.White, 0.35);
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.85;
            render.ModuleCornerRadiusPx = 3;
            render.ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = start,
                EndColor = end,
            };
            render.Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterCornerRadiusPx = 5,
                InnerCornerRadiusPx = 4,
                OuterGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.Radial,
                    StartColor = start,
                    EndColor = end,
                    CenterX = 0.35,
                    CenterY = 0.35,
                },
                InnerColor = start,
            };
        }

        if (opts.Art is not null) {
            opts.Art.Validate();
            ApplyArt(render, opts.Art);
        }

        if (opts.ModuleShape.HasValue) render.ModuleShape = opts.ModuleShape.Value;
        if (opts.ModuleScale.HasValue) render.ModuleScale = opts.ModuleScale.Value;
        if (opts.ModuleCornerRadiusPx.HasValue) render.ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx.Value;
        if (opts.ForegroundGradient is not null) render.ForegroundGradient = opts.ForegroundGradient;
        if (opts.ForegroundPalette is not null) render.ForegroundPalette = opts.ForegroundPalette;
        if (opts.ForegroundPattern is not null) render.ForegroundPattern = opts.ForegroundPattern;
        if (opts.ForegroundPaletteZones is not null) render.ForegroundPaletteZones = opts.ForegroundPaletteZones;
        if (opts.ModuleScaleMap is not null) render.ModuleScaleMap = opts.ModuleScaleMap;
        if (opts.Canvas is not null) render.Canvas = opts.Canvas;
        if (opts.Eyes is not null) render.Eyes = opts.Eyes;

        var logo = BuildPngLogo(opts);
        if (logo is not null) render.Logo = logo;

        ApplyArtAutoTune(qr, render, opts);

        return render;
    }

    private static IcoRenderOptions BuildIcoOptions(QrEasyOptions opts) {
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static QrSvgRenderOptions BuildSvgOptions(QrEasyOptions opts, int moduleCount) {
        var render = new QrSvgRenderOptions {
            ModuleSize = ResolveModuleSize(opts, moduleCount),
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background),
        };

        if (opts.Style == QrRenderStyle.Rounded) {
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.9;
            render.ModuleCornerRadiusPx = 2;
        } else if (opts.Style == QrRenderStyle.Fancy) {
            var start = opts.Foreground;
            var end = Blend(opts.Foreground, Rgba32.White, 0.35);
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.85;
            render.ModuleCornerRadiusPx = 3;
            render.ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = start,
                EndColor = end,
            };
            render.Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterCornerRadiusPx = 5,
                InnerCornerRadiusPx = 4,
                OuterGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.Radial,
                    StartColor = start,
                    EndColor = end,
                    CenterX = 0.35,
                    CenterY = 0.35,
                },
                InnerColor = start,
            };
        }

        if (opts.ModuleShape.HasValue) render.ModuleShape = opts.ModuleShape.Value;
        if (opts.ModuleScale.HasValue) render.ModuleScale = opts.ModuleScale.Value;
        if (opts.ModuleCornerRadiusPx.HasValue) render.ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx.Value;
        if (opts.ForegroundGradient is not null) render.ForegroundGradient = opts.ForegroundGradient;
        if (opts.Eyes is not null) render.Eyes = opts.Eyes;

        var logo = BuildLogoOptions(opts);
        if (logo is not null) render.Logo = logo;

        return render;
    }

    private static int ResolveModuleSize(QrEasyOptions opts, int moduleCount) {
        if (moduleCount <= 0) return opts.ModuleSize;
        if (opts.TargetSizePx <= 0) return opts.ModuleSize;

        var targetModules = moduleCount;
        if (opts.TargetSizeIncludesQuietZone) {
            targetModules += opts.QuietZone * 2;
        }
        if (targetModules <= 0) return opts.ModuleSize;

        var moduleSize = opts.TargetSizePx / targetModules;
        if (moduleSize < 1) moduleSize = 1;
        return moduleSize;
    }

    private static MatrixAsciiRenderOptions BuildAsciiOptions(MatrixAsciiRenderOptions? asciiOptions, QrEasyOptions opts) {
        var resolved = asciiOptions ?? new MatrixAsciiRenderOptions();
        if (asciiOptions is null || asciiOptions.QuietZone == RenderDefaults.QrQuietZone) {
            resolved.QuietZone = opts.QuietZone;
        }
        return resolved;
    }

    private static QrCode EncodePayload(string payload, QrEasyOptions opts) {
        opts = ApplyArtAutoTunePreEncode(opts);
        opts = ApplyLogoBackgroundVersionBump(opts);
        var ecc = opts.ErrorCorrectionLevel ?? GuessEcc(payload, opts.LogoPng is { Length: > 0 });
        if (opts.TextEncoding.HasValue) {
            return QrCodeEncoder.EncodeText(payload, opts.TextEncoding.Value, ecc, opts.MinVersion, opts.MaxVersion, opts.ForceMask, opts.IncludeEci);
        }
        return QrCodeEncoder.EncodeText(payload, ecc, opts.MinVersion, opts.MaxVersion, opts.ForceMask);
    }

    private static QrEasyOptions MergeOptions(QrPayloadData payload, QrEasyOptions? options) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        if (!opts.RespectPayloadDefaults) return opts;

        if (opts.ErrorCorrectionLevel is null && payload.ErrorCorrectionLevel.HasValue) {
            opts.ErrorCorrectionLevel = payload.ErrorCorrectionLevel;
        }
        if (opts.TextEncoding is null && payload.TextEncoding.HasValue) {
            opts.TextEncoding = payload.TextEncoding;
        }
        if (payload.MinVersion.HasValue) {
            opts.MinVersion = Math.Max(opts.MinVersion, payload.MinVersion.Value);
        }
        if (payload.MaxVersion.HasValue) {
            opts.MaxVersion = Math.Min(opts.MaxVersion, payload.MaxVersion.Value);
        }
        if (opts.MinVersion > opts.MaxVersion) {
            throw new ArgumentOutOfRangeException(nameof(options), "QR version range is invalid for the payload.");
        }
        return opts;
    }

    private static QrEasyOptions CloneOptions(QrEasyOptions opts) {
        return new QrEasyOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            TargetSizePx = opts.TargetSizePx,
            TargetSizeIncludesQuietZone = opts.TargetSizeIncludesQuietZone,
            ErrorCorrectionLevel = opts.ErrorCorrectionLevel,
            TextEncoding = opts.TextEncoding,
            IncludeEci = opts.IncludeEci,
            RespectPayloadDefaults = opts.RespectPayloadDefaults,
            MinVersion = opts.MinVersion,
            MaxVersion = opts.MaxVersion,
            ForceMask = opts.ForceMask,
            Foreground = opts.Foreground,
            Background = opts.Background,
            BackgroundGradient = CloneGradient(opts.BackgroundGradient),
            BackgroundPattern = CloneBackgroundPattern(opts.BackgroundPattern),
            BackgroundSupersample = opts.BackgroundSupersample,
            Style = opts.Style,
            Art = opts.Art,
            ArtAutoTune = opts.ArtAutoTune,
            ArtAutoTuneMinScore = opts.ArtAutoTuneMinScore,
            ModuleShape = opts.ModuleShape,
            ModuleScale = opts.ModuleScale,
            ModuleScaleMap = CloneScaleMap(opts.ModuleScaleMap),
            ProtectFunctionalPatterns = opts.ProtectFunctionalPatterns,
            ProtectQuietZone = opts.ProtectQuietZone,
            ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx,
            ForegroundGradient = CloneGradient(opts.ForegroundGradient),
            ForegroundPalette = ClonePalette(opts.ForegroundPalette),
            ForegroundPattern = CloneForegroundPattern(opts.ForegroundPattern),
            ForegroundPaletteZones = ClonePaletteZones(opts.ForegroundPaletteZones),
            Eyes = CloneEyes(opts.Eyes),
            Canvas = CloneCanvas(opts.Canvas),
            Debug = opts.Debug,
            LogoPng = opts.LogoPng is null ? null : (byte[])opts.LogoPng.Clone(),
            LogoScale = opts.LogoScale,
            LogoPaddingPx = opts.LogoPaddingPx,
            LogoDrawBackground = opts.LogoDrawBackground,
            AutoBumpVersionForLogoBackground = opts.AutoBumpVersionForLogoBackground,
            LogoBackgroundMinVersion = opts.LogoBackgroundMinVersion,
            LogoBackground = opts.LogoBackground,
            LogoCornerRadiusPx = opts.LogoCornerRadiusPx,
            JpegQuality = opts.JpegQuality,
            IcoSizes = opts.IcoSizes is null ? null : (int[])opts.IcoSizes.Clone(),
            IcoPreserveAspectRatio = opts.IcoPreserveAspectRatio,
            HtmlEmailSafeTable = opts.HtmlEmailSafeTable,
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
            BorderPx = canvas.BorderPx,
            BorderColor = canvas.BorderColor,
            ShadowOffsetX = canvas.ShadowOffsetX,
            ShadowOffsetY = canvas.ShadowOffsetY,
            ShadowColor = canvas.ShadowColor,
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

    private static QrEasyOptions ApplyLogoBackgroundVersionBump(QrEasyOptions opts) {
        if (!opts.AutoBumpVersionForLogoBackground) return opts;
        if (opts.LogoBackgroundMinVersion <= 0) return opts;
        if (!opts.LogoDrawBackground) return opts;
        if (opts.LogoPng is null || opts.LogoPng.Length == 0) return opts;

        var safeMin = Math.Max(opts.MinVersion, opts.LogoBackgroundMinVersion);
        if (safeMin == opts.MinVersion && opts.MaxVersion >= safeMin) return opts;

        var bumped = CloneOptions(opts);
        safeMin = Math.Max(bumped.MinVersion, bumped.LogoBackgroundMinVersion);
        bumped.MinVersion = safeMin;
        if (bumped.MaxVersion < safeMin) bumped.MaxVersion = safeMin;
        return bumped;
    }

    private static QrPngLogoOptions? BuildPngLogo(QrEasyOptions opts) {
        if (opts.LogoPng is null || opts.LogoPng.Length == 0) return null;
        var logo = QrPngLogoOptions.FromPng(opts.LogoPng);
        logo.Scale = opts.LogoScale;
        logo.PaddingPx = opts.LogoPaddingPx;
        logo.DrawBackground = opts.LogoDrawBackground;
        logo.Background = opts.LogoBackground ?? opts.Background;
        logo.CornerRadiusPx = opts.LogoCornerRadiusPx;
        return logo;
    }

    private static QrLogoOptions? BuildLogoOptions(QrEasyOptions opts) {
        if (opts.LogoPng is null || opts.LogoPng.Length == 0) return null;
        return new QrLogoOptions(opts.LogoPng) {
            Scale = opts.LogoScale,
            PaddingPx = opts.LogoPaddingPx,
            DrawBackground = opts.LogoDrawBackground,
            Background = opts.LogoBackground ?? opts.Background,
            CornerRadiusPx = opts.LogoCornerRadiusPx,
        };
    }

    private static void ApplyArt(QrPngRenderOptions render, QrArtOptions art) {
        var preset = SelectArtPreset(art);
        ApplyArtDefaults(render, preset);
        ApplyArtIntensity(render, art);
        ApplyArtSafetyMode(render, art);
    }

    private static QrEasyOptions SelectArtPreset(QrArtOptions art) {
#pragma warning disable CS0618 // QrArtPresets is deprecated in favor of the high-level art API.
        return art.Theme switch {
            QrArtTheme.NeonGlow => art.Variant == QrArtVariant.Bold ? QrArtPresets.NeonGlowBold() : QrArtPresets.NeonGlowSafe(),
            QrArtTheme.LiquidGlass => art.Variant == QrArtVariant.Bold ? QrArtPresets.LiquidGlassBold() : QrArtPresets.LiquidGlassSafe(),
            QrArtTheme.ConnectedSquircleGlow => art.Variant == QrArtVariant.Bold ? QrArtPresets.ConnectedSquircleGlowBold() : QrArtPresets.ConnectedSquircleGlowSafe(),
            QrArtTheme.CutCornerTech => art.Variant == QrArtVariant.Bold ? QrArtPresets.CutCornerTechBold() : QrArtPresets.CutCornerTechSafe(),
            QrArtTheme.InsetRings => art.Variant == QrArtVariant.Bold ? QrArtPresets.InsetRingsBold() : QrArtPresets.InsetRingsSafe(),
            QrArtTheme.StripeEyes => art.Variant == QrArtVariant.Bold ? QrArtPresets.StripeEyesBold() : QrArtPresets.StripeEyesSafe(),
            QrArtTheme.PaintSplash => art.Variant switch {
                QrArtVariant.Pastel => art.SafetyMode == QrArtSafetyMode.Bold ? QrArtPresets.PaintSplashPastelBold() : QrArtPresets.PaintSplashPastelSafe(),
                QrArtVariant.Bold => QrArtPresets.PaintSplashBold(),
                _ => QrArtPresets.PaintSplashSafe(),
            },
            _ => QrArtPresets.PaintSplashSafe(),
        };
#pragma warning restore CS0618
    }

    private static void ApplyArtDefaults(QrPngRenderOptions render, QrEasyOptions preset) {
        render.Foreground = preset.Foreground;
        render.Background = preset.Background;
        if (preset.BackgroundGradient is not null) render.BackgroundGradient = preset.BackgroundGradient;
        if (preset.BackgroundPattern is not null) render.BackgroundPattern = preset.BackgroundPattern;
        if (preset.ModuleShape.HasValue) render.ModuleShape = preset.ModuleShape.Value;
        if (preset.ModuleScale.HasValue) render.ModuleScale = preset.ModuleScale.Value;
        if (preset.ModuleCornerRadiusPx.HasValue) render.ModuleCornerRadiusPx = preset.ModuleCornerRadiusPx.Value;
        if (preset.ModuleScaleMap is not null) render.ModuleScaleMap = preset.ModuleScaleMap;
        if (preset.ForegroundGradient is not null) render.ForegroundGradient = preset.ForegroundGradient;
        if (preset.ForegroundPalette is not null) render.ForegroundPalette = preset.ForegroundPalette;
        if (preset.ForegroundPattern is not null) render.ForegroundPattern = preset.ForegroundPattern;
        if (preset.ForegroundPaletteZones is not null) render.ForegroundPaletteZones = preset.ForegroundPaletteZones;
        if (preset.Eyes is not null) render.Eyes = preset.Eyes;
        if (preset.Canvas is not null) render.Canvas = preset.Canvas;
        render.ProtectFunctionalPatterns = preset.ProtectFunctionalPatterns;
        render.ProtectQuietZone = preset.ProtectQuietZone;
    }

    private static void ApplyArtIntensity(QrPngRenderOptions render, QrArtOptions art) {
        var t = Clamp01(art.Intensity / 100.0);
        var scaleDelta = (t - 0.5) * 0.04;
        render.ModuleScale = Clamp(render.ModuleScale + scaleDelta, 0.88, 1.0);

        if (render.ModuleScaleMap is not null) {
            var min = Clamp(render.ModuleScaleMap.MinScale + scaleDelta, 0.85, 1.0);
            var max = Clamp(render.ModuleScaleMap.MaxScale + scaleDelta * 0.5, 0.9, 1.0);
            if (min > max) min = max;
            render.ModuleScaleMap.MinScale = min;
            render.ModuleScaleMap.MaxScale = max;
        }

        if (render.ForegroundPattern is not null) {
            var alphaFactor = Lerp(0.75, 1.35, t);
            var newAlpha = ClampToByte(render.ForegroundPattern.Color.A * alphaFactor);
            render.ForegroundPattern.Color = WithAlpha(render.ForegroundPattern.Color, newAlpha);
            if (t > 0.66) {
                render.ForegroundPattern.ThicknessPx = Math.Min(render.ForegroundPattern.ThicknessPx + 1, Math.Max(2, render.ForegroundPattern.SizePx / 2));
            } else if (t < 0.33 && render.ForegroundPattern.ThicknessPx > 1) {
                render.ForegroundPattern.ThicknessPx -= 1;
            }
        }

        if (render.Canvas?.Splash is not null) {
            var splash = render.Canvas.Splash;
            splash.Count = Math.Max(0, (int)Math.Round(Lerp(splash.Count * 0.7, splash.Count * 1.6, t)));
            splash.DripChance = Clamp(Lerp(splash.DripChance * 0.8, splash.DripChance * 1.4, t), 0, 1);

            var splashAlphaFactor = Lerp(0.85, 1.25, t);
            splash.Color = WithAlpha(splash.Color, ClampToByte(splash.Color.A * splashAlphaFactor));
            if (splash.Colors is { Length: > 0 }) {
                for (var i = 0; i < splash.Colors.Length; i++) {
                    var color = splash.Colors[i];
                    splash.Colors[i] = WithAlpha(color, ClampToByte(color.A * splashAlphaFactor));
                }
            }
        }
    }

    private static void ApplyArtSafetyMode(QrPngRenderOptions render, QrArtOptions art) {
        render.ProtectFunctionalPatterns = true;
        render.ProtectQuietZone = true;
        render.QuietZone = Math.Max(render.QuietZone, 4);

        var minScale = art.SafetyMode switch {
            QrArtSafetyMode.Safe => 0.94,
            QrArtSafetyMode.Balanced => 0.9,
            _ => 0.86,
        };

        render.ModuleScale = Math.Max(render.ModuleScale, minScale);
        if (render.ModuleScaleMap is not null) {
            render.ModuleScaleMap.MinScale = Math.Max(render.ModuleScaleMap.MinScale, minScale);
            if (render.ModuleScaleMap.MinScale > render.ModuleScaleMap.MaxScale) {
                render.ModuleScaleMap.MaxScale = render.ModuleScaleMap.MinScale;
            }
        }

        if (render.ForegroundPattern is not null && art.SafetyMode == QrArtSafetyMode.Safe) {
            render.ForegroundPattern.Color = WithAlpha(render.ForegroundPattern.Color, Math.Min(render.ForegroundPattern.Color.A, (byte)140));
        }

        if (render.Canvas?.Splash is not null) {
            render.Canvas.Splash.ProtectQrArea = true;
            if (art.SafetyMode == QrArtSafetyMode.Safe) {
                render.Canvas.Splash.Count = Math.Min(render.Canvas.Splash.Count, 20);
                render.Canvas.Splash.DripChance = Math.Min(render.Canvas.Splash.DripChance, 0.6);
            }
        }
    }

    private static QrEasyOptions ApplyArtAutoTunePreEncode(QrEasyOptions opts) {
        if (!opts.ArtAutoTune) return opts;

        var hasLogo = opts.LogoPng is { Length: > 0 };
        var paletteIsArt = opts.ForegroundPalette?.Colors is { Length: > 2 } || opts.ForegroundPaletteZones is not null;
        var canvasIsArt = opts.Canvas?.Splash is not null
            || opts.Canvas?.Halo is not null
            || opts.Canvas?.Pattern is not null
            || opts.Canvas?.Vignette is not null
            || opts.Canvas?.Grain is not null;
        var eyes = opts.Eyes;
        var eyesIsArt = eyes is not null && (
            eyes.SparkleCount > 0
            || eyes.AccentRingCount > 0
            || eyes.AccentRayCount > 0
            || eyes.AccentStripeCount > 0);
        var hasArtHints = opts.Art is not null
            || opts.ForegroundPattern is not null
            || canvasIsArt
            || eyesIsArt
            || paletteIsArt;

        if (!hasLogo && !hasArtHints) return opts;

        var changed = false;
        var tuned = opts;

        if (tuned.ErrorCorrectionLevel is null) {
            tuned = changed ? tuned : CloneOptions(opts);
            tuned.ErrorCorrectionLevel = QrErrorCorrectionLevel.H;
            changed = true;
        }

        // Nudge very small versions upward when art is enabled (when allowed by the range).
        const int artMinVersion = 3;
        if (tuned.MinVersion < artMinVersion && tuned.MaxVersion >= artMinVersion) {
            tuned = changed ? tuned : CloneOptions(opts);
            tuned.MinVersion = artMinVersion;
            if (tuned.MaxVersion < tuned.MinVersion) tuned.MaxVersion = tuned.MinVersion;
            changed = true;
        }

        return changed ? tuned : opts;
    }

    private static void ApplyArtAutoTune(QrCode qr, QrPngRenderOptions render, QrEasyOptions opts) {
        if (!opts.ArtAutoTune) return;

        var paletteIsArt = render.ForegroundPalette?.Colors is { Length: > 2 } || render.ForegroundPaletteZones is not null;
        var canvasIsArt = render.Canvas?.Splash is not null
            || render.Canvas?.Halo is not null
            || render.Canvas?.Pattern is not null
            || render.Canvas?.Vignette is not null
            || render.Canvas?.Grain is not null;
        var eyes = render.Eyes;
        var eyesIsArt = eyes is not null && (
            eyes.SparkleCount > 0
            || eyes.AccentRingCount > 0
            || eyes.AccentRayCount > 0
            || eyes.AccentStripeCount > 0);
        var hasArtHints = opts.Art is not null
            || render.ForegroundPattern is not null
            || canvasIsArt
            || eyesIsArt
            || paletteIsArt;

        if (!hasArtHints) return;

        var safetyMode = opts.Art?.SafetyMode ?? QrArtSafetyMode.Safe;
        var targetScore = Math.Max(0, Math.Min(opts.ArtAutoTuneMinScore, 100));

        // Always enforce the core scan guardrails when art is active.
        render.ProtectFunctionalPatterns = true;
        render.ProtectQuietZone = true;
        render.QuietZone = Math.Max(render.QuietZone, 4);

        var minScale = safetyMode switch {
            QrArtSafetyMode.Safe => 0.94,
            QrArtSafetyMode.Balanced => 0.9,
            _ => 0.86,
        };

        render.ModuleShape = MapToConnectedShape(render.ModuleShape);
        render.ModuleScale = Math.Max(render.ModuleScale, minScale);

        if (render.ModuleScaleMap is not null) {
            render.ModuleScaleMap.MinScale = Math.Max(render.ModuleScaleMap.MinScale, minScale);
            if (render.ModuleScaleMap.MinScale > render.ModuleScaleMap.MaxScale) {
                render.ModuleScaleMap.MaxScale = render.ModuleScaleMap.MinScale;
            }
        }

        if (render.Canvas?.Splash is not null) {
            render.Canvas.Splash.ProtectQrArea = true;
        }
        if (render.Canvas?.Halo is not null) {
            var halo = render.Canvas.Halo;
            halo.ProtectQrArea = true;
            var haloRadiusMax = safetyMode switch {
                QrArtSafetyMode.Safe => 48,
                QrArtSafetyMode.Balanced => 64,
                _ => 80,
            };
            halo.RadiusPx = Math.Min(halo.RadiusPx, haloRadiusMax);
            var haloAlphaCap = safetyMode switch {
                QrArtSafetyMode.Safe => (byte)96,
                QrArtSafetyMode.Balanced => (byte)120,
                _ => (byte)160,
            };
            if (halo.QrAreaAlphaMax == 0 || halo.QrAreaAlphaMax > haloAlphaCap) {
                halo.QrAreaAlphaMax = haloAlphaCap;
            }
        }
        if (render.Eyes is not null) {
            var eye = render.Eyes;
            eye.SparkleProtectQrArea = true;
            eye.AccentRingProtectQrArea = true;
            eye.AccentRayProtectQrArea = true;
            eye.AccentStripeProtectQrArea = true;

            eye.SparkleCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.SparkleCount, 28),
                QrArtSafetyMode.Balanced => Math.Min(eye.SparkleCount, 36),
                _ => Math.Min(eye.SparkleCount, 44),
            };
            eye.AccentRingCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.AccentRingCount, 6),
                QrArtSafetyMode.Balanced => Math.Min(eye.AccentRingCount, 8),
                _ => Math.Min(eye.AccentRingCount, 10),
            };
            eye.AccentRayCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.AccentRayCount, 18),
                QrArtSafetyMode.Balanced => Math.Min(eye.AccentRayCount, 26),
                _ => Math.Min(eye.AccentRayCount, 34),
            };
            eye.AccentStripeCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.AccentStripeCount, 22),
                QrArtSafetyMode.Balanced => Math.Min(eye.AccentStripeCount, 30),
                _ => Math.Min(eye.AccentStripeCount, 38),
            };

            var eyeAlphaCap = safetyMode switch {
                QrArtSafetyMode.Safe => (byte)128,
                QrArtSafetyMode.Balanced => (byte)148,
                _ => (byte)176,
            };
            if (eye.SparkleColor is not null) {
                eye.SparkleColor = WithAlpha(eye.SparkleColor.Value, Math.Min(eye.SparkleColor.Value.A, eyeAlphaCap));
            }
            if (eye.AccentRingColor is not null) {
                eye.AccentRingColor = WithAlpha(eye.AccentRingColor.Value, Math.Min(eye.AccentRingColor.Value.A, eyeAlphaCap));
            }
            if (eye.AccentRayColor is not null) {
                eye.AccentRayColor = WithAlpha(eye.AccentRayColor.Value, Math.Min(eye.AccentRayColor.Value.A, eyeAlphaCap));
            }
            if (eye.AccentStripeColor is not null) {
                eye.AccentStripeColor = WithAlpha(eye.AccentStripeColor.Value, Math.Min(eye.AccentStripeColor.Value.A, eyeAlphaCap));
            }
        }

        var report = QrArtSafety.Evaluate(qr, render);
        if (HasLowContrastWarnings(report)) {
            ApplyLowContrastFallback(render);
            report = QrArtSafety.Evaluate(qr, render);
        }

        if (report.Score >= targetScore) return;

        ApplyStrongSafetyClamp(render, safetyMode);
    }

    private static bool HasLowContrastWarnings(QrArtSafetyReport report) {
        foreach (var warning in report.Warnings) {
            if (warning.Kind == QrArtWarningKind.LowContrast
                || warning.Kind == QrArtWarningKind.LowContrastGradient
                || warning.Kind == QrArtWarningKind.LowContrastPalette) {
                return true;
            }
        }
        return false;
    }

    private static void ApplyLowContrastFallback(QrPngRenderOptions render) {
        render.Foreground = RenderDefaults.QrForeground;
        render.Background = RenderDefaults.QrBackground;
        render.ForegroundGradient = null;
        render.ForegroundPalette = null;
        render.ForegroundPaletteZones = null;
        render.BackgroundGradient = null;

        if (render.ForegroundPattern is not null) {
            var pattern = render.ForegroundPattern;
            var alpha = Math.Min(pattern.Color.A, (byte)96);
            pattern.Color = WithAlpha(pattern.Color, alpha);
            pattern.ThicknessPx = Math.Min(pattern.ThicknessPx, 1);
        }
    }

    private static void ApplyStrongSafetyClamp(QrPngRenderOptions render, QrArtSafetyMode safetyMode) {
        var strongMinScale = safetyMode switch {
            QrArtSafetyMode.Safe => 0.97,
            QrArtSafetyMode.Balanced => 0.94,
            _ => 0.9,
        };

        render.ModuleScale = Math.Max(render.ModuleScale, strongMinScale);
        if (render.ModuleScaleMap is not null) {
            render.ModuleScaleMap.MinScale = Math.Max(render.ModuleScaleMap.MinScale, strongMinScale);
            if (render.ModuleScaleMap.MinScale > render.ModuleScaleMap.MaxScale) {
                render.ModuleScaleMap.MaxScale = render.ModuleScaleMap.MinScale;
            }
        }

        if (render.ForegroundPattern is not null) {
            var pattern = render.ForegroundPattern;
            pattern.Color = WithAlpha(pattern.Color, Math.Min(pattern.Color.A, (byte)88));
            pattern.ThicknessPx = 1;
        }

        if (render.Canvas?.Splash is not null) {
            var splash = render.Canvas.Splash;
            splash.ProtectQrArea = true;
            splash.Count = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(splash.Count, 16),
                QrArtSafetyMode.Balanced => Math.Min(splash.Count, 22),
                _ => Math.Min(splash.Count, 28),
            };
            splash.DripChance = safetyMode == QrArtSafetyMode.Safe ? Math.Min(splash.DripChance, 0.45) : splash.DripChance;
            splash.Color = WithAlpha(splash.Color, Math.Min(splash.Color.A, (byte)96));
            if (splash.Colors is { Length: > 0 }) {
                for (var i = 0; i < splash.Colors.Length; i++) {
                    var color = splash.Colors[i];
                    splash.Colors[i] = WithAlpha(color, Math.Min(color.A, (byte)96));
                }
            }
        }

        if (render.Canvas?.Halo is not null) {
            var halo = render.Canvas.Halo;
            halo.ProtectQrArea = true;
            halo.RadiusPx = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(halo.RadiusPx, 40),
                QrArtSafetyMode.Balanced => Math.Min(halo.RadiusPx, 56),
                _ => Math.Min(halo.RadiusPx, 72),
            };
            var alphaCap = safetyMode switch {
                QrArtSafetyMode.Safe => (byte)80,
                QrArtSafetyMode.Balanced => (byte)104,
                _ => (byte)140,
            };
            if (halo.QrAreaAlphaMax == 0 || halo.QrAreaAlphaMax > alphaCap) {
                halo.QrAreaAlphaMax = alphaCap;
            }
        }
        if (render.Eyes is not null) {
            var eye = render.Eyes;
            eye.SparkleProtectQrArea = true;
            eye.AccentRingProtectQrArea = true;
            eye.AccentRayProtectQrArea = true;
            eye.AccentStripeProtectQrArea = true;

            eye.SparkleCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.SparkleCount, 22),
                QrArtSafetyMode.Balanced => Math.Min(eye.SparkleCount, 30),
                _ => Math.Min(eye.SparkleCount, 38),
            };
            eye.AccentRingCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.AccentRingCount, 4),
                QrArtSafetyMode.Balanced => Math.Min(eye.AccentRingCount, 6),
                _ => Math.Min(eye.AccentRingCount, 8),
            };
            eye.AccentRayCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.AccentRayCount, 14),
                QrArtSafetyMode.Balanced => Math.Min(eye.AccentRayCount, 22),
                _ => Math.Min(eye.AccentRayCount, 30),
            };
            eye.AccentStripeCount = safetyMode switch {
                QrArtSafetyMode.Safe => Math.Min(eye.AccentStripeCount, 18),
                QrArtSafetyMode.Balanced => Math.Min(eye.AccentStripeCount, 26),
                _ => Math.Min(eye.AccentStripeCount, 34),
            };

            var eyeAlphaCap = safetyMode switch {
                QrArtSafetyMode.Safe => (byte)112,
                QrArtSafetyMode.Balanced => (byte)132,
                _ => (byte)160,
            };
            if (eye.SparkleColor is not null) {
                eye.SparkleColor = WithAlpha(eye.SparkleColor.Value, Math.Min(eye.SparkleColor.Value.A, eyeAlphaCap));
            }
            if (eye.AccentRingColor is not null) {
                eye.AccentRingColor = WithAlpha(eye.AccentRingColor.Value, Math.Min(eye.AccentRingColor.Value.A, eyeAlphaCap));
            }
            if (eye.AccentRayColor is not null) {
                eye.AccentRayColor = WithAlpha(eye.AccentRayColor.Value, Math.Min(eye.AccentRayColor.Value.A, eyeAlphaCap));
            }
            if (eye.AccentStripeColor is not null) {
                eye.AccentStripeColor = WithAlpha(eye.AccentStripeColor.Value, Math.Min(eye.AccentStripeColor.Value.A, eyeAlphaCap));
            }
        }
    }

    private static QrPngModuleShape MapToConnectedShape(QrPngModuleShape shape) {
        return shape switch {
            QrPngModuleShape.Rounded => QrPngModuleShape.ConnectedRounded,
            QrPngModuleShape.Squircle => QrPngModuleShape.ConnectedSquircle,
            QrPngModuleShape.Circle => QrPngModuleShape.ConnectedRounded,
            QrPngModuleShape.Dot => QrPngModuleShape.ConnectedRounded,
            QrPngModuleShape.DotGrid => QrPngModuleShape.ConnectedRounded,
            QrPngModuleShape.Leaf => QrPngModuleShape.ConnectedRounded,
            QrPngModuleShape.Wave => QrPngModuleShape.ConnectedRounded,
            QrPngModuleShape.Blob => QrPngModuleShape.ConnectedRounded,
            _ => shape,
        };
    }

    private static QrErrorCorrectionLevel GuessEcc(string payload, bool hasLogo) {
        if (hasLogo) return QrErrorCorrectionLevel.H;
        return payload.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase)
            ? QrErrorCorrectionLevel.H
            : QrErrorCorrectionLevel.M;
    }

    private static double Clamp01(double value) => Clamp(value, 0, 1);

    private static double Clamp(double value, double min, double max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static byte ClampToByte(double value) {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)Math.Round(value);
    }

    private static double Lerp(double a, double b, double t) {
        if (t <= 0) return a;
        if (t >= 1) return b;
        return a + (b - a) * t;
    }

    private static Rgba32 WithAlpha(Rgba32 color, byte alpha) => new(color.R, color.G, color.B, alpha);

    private static Rgba32 Blend(Rgba32 a, Rgba32 b, double t) {
        if (t <= 0) return a;
        if (t >= 1) return b;
        var r = (byte)Math.Round(a.R + (b.R - a.R) * t);
        var g = (byte)Math.Round(a.G + (b.G - a.G) * t);
        var bch = (byte)Math.Round(a.B + (b.B - a.B) * t);
        var aCh = (byte)Math.Round(a.A + (b.A - a.A) * t);
        return new Rgba32(r, g, bch, aCh);
    }

    private static string ToCss(Rgba32 color) {
        if (color.A == 255) return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        var a = color.A / 255.0;
        return $"rgba({color.R},{color.G},{color.B},{a.ToString("0.###", CultureInfo.InvariantCulture)})";
    }
}

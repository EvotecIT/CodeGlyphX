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
    private static QrPngRenderOptions BuildPngOptions(QrEasyOptions opts, string payload, int moduleCount) {
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
            BackgroundGradient = opts.BackgroundGradient,
            BackgroundPattern = opts.BackgroundPattern,
            BackgroundSupersample = opts.BackgroundSupersample,
            Style = opts.Style,
            ModuleShape = opts.ModuleShape,
            ModuleScale = opts.ModuleScale,
            ModuleScaleMap = opts.ModuleScaleMap,
            ProtectFunctionalPatterns = opts.ProtectFunctionalPatterns,
            ProtectQuietZone = opts.ProtectQuietZone,
            ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx,
            ForegroundGradient = opts.ForegroundGradient,
            ForegroundPalette = opts.ForegroundPalette,
            ForegroundPattern = opts.ForegroundPattern,
            ForegroundPaletteZones = opts.ForegroundPaletteZones,
            Eyes = opts.Eyes,
            Canvas = opts.Canvas,
            Debug = opts.Debug,
            LogoPng = opts.LogoPng,
            LogoScale = opts.LogoScale,
            LogoPaddingPx = opts.LogoPaddingPx,
            LogoDrawBackground = opts.LogoDrawBackground,
            AutoBumpVersionForLogoBackground = opts.AutoBumpVersionForLogoBackground,
            LogoBackgroundMinVersion = opts.LogoBackgroundMinVersion,
            LogoBackground = opts.LogoBackground,
            LogoCornerRadiusPx = opts.LogoCornerRadiusPx,
            JpegQuality = opts.JpegQuality,
            HtmlEmailSafeTable = opts.HtmlEmailSafeTable
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

    private static QrErrorCorrectionLevel GuessEcc(string payload, bool hasLogo) {
        if (hasLogo) return QrErrorCorrectionLevel.H;
        return payload.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase)
            ? QrErrorCorrectionLevel.H
            : QrErrorCorrectionLevel.M;
    }

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

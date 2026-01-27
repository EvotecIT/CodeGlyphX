using System;
using System.Collections.Generic;
using System.IO;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    internal static int GetScanlineLength(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.ModuleScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleScale));
        opts.BackgroundGradient?.Validate();
        opts.BackgroundPattern?.Validate();
        if (opts.BackgroundSupersample < QrPngRenderOptions.BackgroundSupersampleMin ||
            opts.BackgroundSupersample > QrPngRenderOptions.BackgroundSupersampleMax) {
            throw new ArgumentOutOfRangeException(nameof(opts.BackgroundSupersample));
        }
        opts.ForegroundGradient?.Validate();
        opts.ForegroundPalette?.Validate();
        opts.ForegroundPattern?.Validate();
        opts.ForegroundPaletteZones?.Validate();
        opts.ModuleScaleMap?.Validate();
        opts.Eyes?.Validate();
        opts.Canvas?.Validate();
        opts.Debug?.Validate();

        var size = modules.Width;
        ComputeLayout(size, opts, out widthPx, out heightPx, out _, out _, out _, out _);
        stride = widthPx * 4;
        return heightPx * (stride + 1);
    }

    internal static byte[] RenderScanlines(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride) {
        return RenderScanlines(modules, opts, out widthPx, out heightPx, out stride, scanlines: null);
    }

    internal static byte[] RenderScanlines(BitMatrix modules, QrPngRenderOptions opts, out int widthPx, out int heightPx, out int stride, byte[]? scanlines) {
        var length = GetScanlineLength(modules, opts, out widthPx, out heightPx, out stride);
        var buffer = scanlines ?? new byte[length];
        if (buffer.Length < length) throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlines));

        var size = modules.Width;
        ComputeLayout(size, opts, out widthPx, out heightPx, out var qrOffsetX, out var qrOffsetY, out var qrFullPx, out var qrSizePx);
        var qrOriginX = qrOffsetX + opts.QuietZone * opts.ModuleSize;
        var qrOriginY = qrOffsetY + opts.QuietZone * opts.ModuleSize;

        if (opts.BackgroundSupersample > 1) {
            RenderBackgroundSupersampled(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx);
        } else if (opts.Canvas is null) {
            if (opts.BackgroundGradient is null) {
                PngRenderHelpers.FillBackground(buffer, widthPx, heightPx, stride, opts.Background);
            } else {
                FillBackgroundGradient(buffer, widthPx, heightPx, stride, opts.BackgroundGradient);
            }
            if (opts.BackgroundPattern is not null) {
                var quietZonePx = opts.QuietZone * opts.ModuleSize;
                DrawCanvasPattern(
                    buffer,
                    widthPx,
                    heightPx,
                    stride,
                    qrOffsetX,
                    qrOffsetY,
                    qrFullPx,
                    qrFullPx,
                    opts.ModuleSize,
                    0,
                    quietZonePx,
                    qrSizePx,
                    opts.ProtectQuietZone,
                    opts.BackgroundPattern);
            }
        } else {
            PngRenderHelpers.FillBackground(buffer, widthPx, heightPx, stride, Rgba32.Transparent);
            DrawCanvas(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx);
            FillQrBackground(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx);
        }

        if (opts.Eyes is { SparkleCount: > 0 } eyesSparkle
            && (opts.Canvas is not null || eyesSparkle.SparkleAllowOnQrBackground)) {
            DrawEyeSparkles(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx, qrSizePx, size);
        }
        if (opts.Eyes is { AccentRingCount: > 0 } eyesAccents
            && (opts.Canvas is not null || eyesAccents.AccentRingAllowOnQrBackground)) {
            DrawEyeAccentRings(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx, qrSizePx, size);
        }
        if (opts.Eyes is { AccentRayCount: > 0 } eyesRays
            && (opts.Canvas is not null || eyesRays.AccentRayAllowOnQrBackground)) {
            DrawEyeAccentRays(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx, qrSizePx, size);
        }

        var connectedShape = opts.ModuleShape;
        var connectedMode = connectedShape is QrPngModuleShape.ConnectedRounded or QrPngModuleShape.ConnectedSquircle;
        var baseModuleShape = connectedMode
            ? connectedShape == QrPngModuleShape.ConnectedRounded ? QrPngModuleShape.Rounded : QrPngModuleShape.Squircle
            : opts.ModuleShape;
        var mask = BuildModuleMask(opts.ModuleSize, baseModuleShape, opts.ModuleScale, opts.ModuleCornerRadiusPx);
        var maskSolid = IsSolidMask(mask);
        var eyeOuterMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.OuterShape, opts.Eyes.OuterScale, opts.Eyes.OuterCornerRadiusPx);
        var eyeOuterSolid = eyeOuterMask == mask ? maskSolid : IsSolidMask(eyeOuterMask);
        var eyeInnerMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.InnerShape, opts.Eyes.InnerScale, opts.Eyes.InnerCornerRadiusPx);
        var eyeInnerSolid = eyeInnerMask == mask ? maskSolid : IsSolidMask(eyeInnerMask);
        var functionalMask = opts.ProtectFunctionalPatterns
            ? BuildModuleMask(opts.ModuleSize, QrPngModuleShape.Square, 1.0, cornerRadiusPx: 0)
            : mask;
        var functionalMaskSolid = functionalMask == mask ? maskSolid : IsSolidMask(functionalMask);
        var functionMask = opts.ProtectFunctionalPatterns && QrStructureAnalysis.TryGetVersionFromSize(size, out var version)
            ? QrStructureAnalysis.BuildFunctionMask(version, size)
            : null;
        var useFrame = opts.Eyes is not null && opts.Eyes.UseFrame;
        var background = opts.Background;
        var gradientInfo = opts.ForegroundGradient is null
            ? (GradientInfo?)null
            : new GradientInfo(opts.ForegroundGradient, qrSizePx - 1, qrSizePx - 1);
        var paletteInfo = opts.ForegroundPalette is null ? (PaletteInfo?)null : new PaletteInfo(opts.ForegroundPalette, size);
        var foregroundPattern = opts.ForegroundPattern;
        var zoneInfo = opts.ForegroundPaletteZones is null ? (PaletteZoneInfo?)null : new PaletteZoneInfo(opts.ForegroundPaletteZones, size);
        var scaleMapInfo = opts.ModuleScaleMap is null ? (ModuleScaleMapInfo?)null : new ModuleScaleMapInfo(opts.ModuleScaleMap, size);
        var scaleMaskCache = scaleMapInfo.HasValue ? new Dictionary<int, MaskInfo>(8) : null;
        var connectedMaskCache = connectedMode ? new Dictionary<int, MaskInfo>(32) : null;
        // Eye detection is needed for styling decisions (eyes/palettes/scale maps),
        // but we keep it decoupled from functional protection so gradients can still
        // apply to eyes when no eye-specific styling is configured.
        var detectEyesForStyling = opts.Eyes is not null
            || paletteInfo.HasValue
            || zoneInfo.HasValue
            || scaleMapInfo.HasValue
            || (foregroundPattern is not null && foregroundPattern.ApplyToEyes);
        var eyeOuterGradient = opts.Eyes?.OuterGradient;
        var eyeInnerGradient = opts.Eyes?.InnerGradient;
        var eyeOuterGradientInfo = eyeOuterGradient is null ? (GradientInfo?)null : new GradientInfo(eyeOuterGradient, 7 * opts.ModuleSize - 1, 7 * opts.ModuleSize - 1);
        var eyeInnerGradientInfo = eyeInnerGradient is null ? (GradientInfo?)null : new GradientInfo(eyeInnerGradient, 3 * opts.ModuleSize - 1, 3 * opts.ModuleSize - 1);
        var eyeOuterGradientInfos = BuildEyeGradientInfos(opts.Eyes?.OuterGradients, opts.ModuleSize, eyeModules: 7);
        var eyeInnerGradientInfos = BuildEyeGradientInfos(opts.Eyes?.InnerGradients, opts.ModuleSize, eyeModules: 3);

        for (var my = 0; my < size; my++) {
            for (var mx = 0; mx < size; mx++) {
                if (!modules[mx, my]) continue;
                var eyeKind = EyeKind.None;
                var eyeX = 0;
                var eyeY = 0;
                var isEye = false;
                if (detectEyesForStyling && TryGetEye(mx, my, size, out eyeX, out eyeY, out var kind)) {
                    eyeKind = kind;
                    isEye = true;
                } else if (functionMask is not null && TryGetEye(mx, my, size, out _, out _, out var protectKind)) {
                    // When only functional protection is enabled, still recognize eyes
                    // so we do not apply non-eye protection to finder patterns.
                    isEye = protectKind != EyeKind.None;
                }

                if (useFrame && eyeKind != EyeKind.None) continue;

                var isFunctional = functionMask is not null && functionMask[mx, my];
                var protectFunctional = isFunctional && !isEye;
                var eyeIndex = eyeKind == EyeKind.None ? -1 : GetEyeIndex(eyeX, eyeY, size);

                var useMask = protectFunctional
                    ? functionalMask
                    : eyeKind == EyeKind.Outer ? eyeOuterMask : eyeKind == EyeKind.Inner ? eyeInnerMask : mask;
                var useMaskSolid = protectFunctional
                    ? functionalMaskSolid
                    : eyeKind == EyeKind.Outer ? eyeOuterSolid : eyeKind == EyeKind.Inner ? eyeInnerSolid : maskSolid;
                var useColor = protectFunctional
                    ? opts.Foreground
                    : eyeKind switch {
                        EyeKind.Outer => GetEyeOuterColor(opts, eyeIndex),
                        EyeKind.Inner => GetEyeInnerColor(opts, eyeIndex),
                        _ => opts.Foreground,
                    };
                PaletteInfo? palette = null;
                if (!protectFunctional) {
                    if (zoneInfo.HasValue
                        && zoneInfo.Value.TryGetPalette(mx, my, out var zonePalette)
                        && (eyeKind == EyeKind.None || zonePalette.ApplyToEyes)) {
                        palette = zonePalette;
                    } else if (paletteInfo.HasValue && (eyeKind == EyeKind.None || paletteInfo.Value.ApplyToEyes)) {
                        palette = paletteInfo;
                    }
                }
                var usePalette = palette.HasValue;
                if (usePalette) {
                    useColor = GetPaletteColor(palette!.Value, mx, my);
                }

                if (!protectFunctional && connectedMode && eyeKind == EyeKind.None) {
                    var scale = opts.ModuleScale;
                    if (scaleMapInfo.HasValue) {
                        scale *= GetScaleFactor(scaleMapInfo.Value, mx, my);
                    }
                    scale = ClampScale(scale);
                    var neighborMask = GetNeighborMask(modules, mx, my);
                    var maskInfo = GetConnectedMask(
                        connectedMaskCache!,
                        opts.ModuleSize,
                        scale,
                        opts.ModuleCornerRadiusPx,
                        neighborMask,
                        connectedShape);
                    useMask = maskInfo.Mask;
                    useMaskSolid = maskInfo.IsSolid;
                } else if (!protectFunctional
                           && scaleMapInfo.HasValue
                           && (eyeKind == EyeKind.None || scaleMapInfo.Value.ApplyToEyes)) {
                    var scale = ClampScale(opts.ModuleScale * GetScaleFactor(scaleMapInfo.Value, mx, my));
                    var maskInfo = GetScaleMask(scaleMaskCache!, opts.ModuleSize, baseModuleShape, scale, opts.ModuleCornerRadiusPx);
                    useMask = maskInfo.Mask;
                    useMaskSolid = maskInfo.IsSolid;
                }

                QrPngForegroundPatternOptions? usePattern = null;
                if (!protectFunctional
                    && foregroundPattern is not null
                    && foregroundPattern.Color.A != 0
                    && foregroundPattern.ThicknessPx > 0) {
                    var applyToEyes = eyeKind != EyeKind.None && foregroundPattern.ApplyToEyes;
                    var applyToModules = eyeKind == EyeKind.None && foregroundPattern.ApplyToModules;
                    if (applyToEyes || applyToModules) {
                        usePattern = foregroundPattern;
                    }
                }

                if (!useFrame && eyeKind != EyeKind.None) {
                    var eyeGrad = eyeKind == EyeKind.Outer
                        ? GetEyeGradientInfo(eyeOuterGradientInfos, eyeOuterGradientInfo, eyeIndex)
                        : GetEyeGradientInfo(eyeInnerGradientInfos, eyeInnerGradientInfo, eyeIndex);
                    if (eyeGrad is not null) {
                        var boxX = qrOffsetX + (eyeX + opts.QuietZone) * opts.ModuleSize;
                        var boxY = qrOffsetY + (eyeY + opts.QuietZone) * opts.ModuleSize;
                        DrawModuleInBox(
                            buffer,
                            stride,
                            opts.ModuleSize,
                            mx,
                            my,
                            opts.QuietZone,
                            qrOffsetX,
                            qrOffsetY,
                            eyeGrad.Value,
                            usePattern,
                            useMask,
                            boxX,
                            boxY,
                            qrOriginX,
                            qrOriginY,
                            qrSizePx);
                        continue;
                    }
                }

                var useGradient = !protectFunctional && !usePalette && eyeKind == EyeKind.None ? gradientInfo : null;
                if (useGradient is null && useMaskSolid && usePattern is null) {
                    var solid = useColor.A == 255 ? useColor : CompositeColor(useColor, background);
                    DrawModuleSolid(buffer, stride, opts.ModuleSize, mx, my, opts.QuietZone, qrOffsetX, qrOffsetY, solid);
                    continue;
                }

                DrawModule(
                    buffer,
                    stride,
                    opts.ModuleSize,
                    mx,
                    my,
                    opts.QuietZone,
                    qrOffsetX,
                    qrOffsetY,
                    useColor,
                    useGradient,
                    usePattern,
                    useMask,
                    qrOriginX,
                    qrOriginY,
                    qrSizePx);
            }
        }

        if (useFrame && opts.Eyes is not null) {
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrOriginX, qrOriginY, qrSizePx, 0, 0);
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrOriginX, qrOriginY, qrSizePx, size - 7, 0);
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrOriginX, qrOriginY, qrSizePx, 0, size - 7);
        }

        if (opts.Logo is not null) {
            ApplyLogo(buffer, widthPx, heightPx, stride, qrOriginX, qrOriginY, qrSizePx, opts.Logo);
        }

        if (opts.Debug is not null && opts.Debug.HasOverlay) {
            DrawDebugOverlay(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrOriginX, qrOriginY, qrFullPx, qrSizePx, size);
        }

        return buffer;
    }

    private static GradientInfo?[]? BuildEyeGradientInfos(QrPngGradientOptions[]? gradients, int moduleSize, int eyeModules) {
        if (gradients is null || gradients.Length != 3 || moduleSize <= 0) return null;
        var sizePx = eyeModules * moduleSize - 1;
        var infos = new GradientInfo?[3];
        for (var i = 0; i < 3; i++) {
            var gradient = gradients[i];
            infos[i] = gradient is null ? null : new GradientInfo(gradient, sizePx, sizePx);
        }
        return infos;
    }

    private static GradientInfo? GetEyeGradientInfo(GradientInfo?[]? infos, GradientInfo? fallback, int eyeIndex) {
        if (eyeIndex is >= 0 and <= 2 && infos is { Length: 3 }) {
            return infos[eyeIndex] ?? fallback;
        }
        return fallback;
    }

    private static void DrawModuleSolid(byte[] scanlines, int stride, int moduleSize, int mx, int my, int quietZone, int offsetX, int offsetY, Rgba32 color) {
        var x0 = offsetX + (mx + quietZone) * moduleSize;
        var y0 = offsetY + (my + quietZone) * moduleSize;
        for (var sy = 0; sy < moduleSize; sy++) {
            var rowStart = (y0 + sy) * (stride + 1) + 1 + x0 * 4;
            PngRenderHelpers.FillRowPixels(scanlines, rowStart, moduleSize, color);
        }
    }

    private static void DrawModule(
        byte[] scanlines,
        int stride,
        int moduleSize,
        int mx,
        int my,
        int quietZone,
        int offsetX,
        int offsetY,
        Rgba32 color,
        GradientInfo? gradient,
        QrPngForegroundPatternOptions? pattern,
        bool[] mask,
        int originX,
        int originY,
        int qrSizePx) {
        var x0 = offsetX + (mx + quietZone) * moduleSize;
        var y0 = offsetY + (my + quietZone) * moduleSize;
        for (var sy = 0; sy < moduleSize; sy++) {
            var rowStart = (y0 + sy) * (stride + 1) + 1 + x0 * 4;
            var maskRow = sy * moduleSize;
            for (var sx = 0; sx < moduleSize; sx++) {
                if (!mask[maskRow + sx]) {
                    rowStart += 4;
                    continue;
                }
                var outColor = gradient is null
                    ? color
                    : GetGradientColor(gradient.Value, x0 + sx, y0 + sy, originX, originY);

                if (pattern is not null && ShouldDrawForegroundPattern(pattern, moduleSize, x0 + sx, y0 + sy, originX, originY, qrSizePx)) {
                    outColor = ComposeOver(pattern.Color, outColor);
                }

                if (outColor.A == 255) {
                    scanlines[rowStart + 0] = outColor.R;
                    scanlines[rowStart + 1] = outColor.G;
                    scanlines[rowStart + 2] = outColor.B;
                    scanlines[rowStart + 3] = 255;
                } else {
                    var dr = scanlines[rowStart + 0];
                    var dg = scanlines[rowStart + 1];
                    var db = scanlines[rowStart + 2];
                    var da = scanlines[rowStart + 3];
                    var sa = outColor.A;
                    var inv = 255 - sa;
                    scanlines[rowStart + 0] = (byte)((outColor.R * sa + dr * inv + 127) / 255);
                    scanlines[rowStart + 1] = (byte)((outColor.G * sa + dg * inv + 127) / 255);
                    scanlines[rowStart + 2] = (byte)((outColor.B * sa + db * inv + 127) / 255);
                    scanlines[rowStart + 3] = (byte)((sa + da * inv + 127) / 255);
                }
                rowStart += 4;
            }
        }
    }

    private static void DrawModuleInBox(
        byte[] scanlines,
        int stride,
        int moduleSize,
        int mx,
        int my,
        int quietZone,
        int offsetX,
        int offsetY,
        GradientInfo gradient,
        QrPngForegroundPatternOptions? pattern,
        bool[] mask,
        int boxX,
        int boxY,
        int originX,
        int originY,
        int qrSizePx) {
        var x0 = offsetX + (mx + quietZone) * moduleSize;
        var y0 = offsetY + (my + quietZone) * moduleSize;
        for (var sy = 0; sy < moduleSize; sy++) {
            var rowStart = (y0 + sy) * (stride + 1) + 1 + x0 * 4;
            var maskRow = sy * moduleSize;
            for (var sx = 0; sx < moduleSize; sx++) {
                if (!mask[maskRow + sx]) {
                    rowStart += 4;
                    continue;
                }
                var outColor = GetGradientColorInBox(gradient, x0 + sx, y0 + sy, boxX, boxY);
                if (pattern is not null && ShouldDrawForegroundPattern(pattern, moduleSize, x0 + sx, y0 + sy, originX, originY, qrSizePx)) {
                    outColor = ComposeOver(pattern.Color, outColor);
                }

                if (outColor.A == 255) {
                    scanlines[rowStart + 0] = outColor.R;
                    scanlines[rowStart + 1] = outColor.G;
                    scanlines[rowStart + 2] = outColor.B;
                    scanlines[rowStart + 3] = 255;
                } else {
                    var dr = scanlines[rowStart + 0];
                    var dg = scanlines[rowStart + 1];
                    var db = scanlines[rowStart + 2];
                    var da = scanlines[rowStart + 3];
                    var sa = outColor.A;
                    var inv = 255 - sa;
                    scanlines[rowStart + 0] = (byte)((outColor.R * sa + dr * inv + 127) / 255);
                    scanlines[rowStart + 1] = (byte)((outColor.G * sa + dg * inv + 127) / 255);
                    scanlines[rowStart + 2] = (byte)((outColor.B * sa + db * inv + 127) / 255);
                    scanlines[rowStart + 3] = (byte)((sa + da * inv + 127) / 255);
                }
                rowStart += 4;
            }
        }
    }

    private static bool IsSolidMask(bool[] mask) {
        for (var i = 0; i < mask.Length; i++) {
            if (!mask[i]) return false;
        }
        return true;
    }

    private static Rgba32 CompositeColor(Rgba32 foreground, Rgba32 background) {
        if (foreground.A == 255) return foreground;
        var sa = foreground.A;
        var inv = 255 - sa;
        var r = (byte)((foreground.R * sa + background.R * inv + 127) / 255);
        var g = (byte)((foreground.G * sa + background.G * inv + 127) / 255);
        var b = (byte)((foreground.B * sa + background.B * inv + 127) / 255);
        var a = (byte)((sa + background.A * inv + 127) / 255);
        return new Rgba32(r, g, b, a);
    }

    private static void ComputeLayout(
        int size,
        QrPngRenderOptions opts,
        out int widthPx,
        out int heightPx,
        out int qrOffsetX,
        out int qrOffsetY,
        out int qrFullPx,
        out int qrSizePx) {
        qrSizePx = size * opts.ModuleSize;
        qrFullPx = (size + opts.QuietZone * 2) * opts.ModuleSize;
        if (opts.Canvas is null) {
            widthPx = qrFullPx;
            heightPx = qrFullPx;
            qrOffsetX = 0;
            qrOffsetY = 0;
            return;
        }

        var canvas = opts.Canvas;
        var pad = Math.Max(0, canvas.PaddingPx);
        var canvasW = qrFullPx + pad * 2;
        var canvasH = canvasW;

        var shadowX = canvas.ShadowOffsetX;
        var shadowY = canvas.ShadowOffsetY;
        var extraLeft = Math.Max(0, -shadowX);
        var extraRight = Math.Max(0, shadowX);
        var extraTop = Math.Max(0, -shadowY);
        var extraBottom = Math.Max(0, shadowY);

        widthPx = canvasW + extraLeft + extraRight;
        heightPx = canvasH + extraTop + extraBottom;
        qrOffsetX = extraLeft + pad;
        qrOffsetY = extraTop + pad;
    }

    private static void DrawCanvas(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int qrOffsetX,
        int qrOffsetY,
        int qrFullPx) {
        var canvas = opts.Canvas;
        if (canvas is null) return;

        var pad = Math.Max(0, canvas.PaddingPx);
        var canvasW = qrFullPx + pad * 2;
        var canvasH = canvasW;
        var canvasX = qrOffsetX - pad;
        var canvasY = qrOffsetY - pad;
        var radius = Math.Max(0, canvas.CornerRadiusPx);

        if (canvas.ShadowColor.A > 0 && (canvas.ShadowOffsetX != 0 || canvas.ShadowOffsetY != 0)) {
            FillRoundedRect(
                scanlines,
                widthPx,
                heightPx,
                stride,
                canvasX + canvas.ShadowOffsetX,
                canvasY + canvas.ShadowOffsetY,
                canvasW,
                canvasH,
                canvas.ShadowColor,
                radius);
        }

        if (canvas.BorderPx > 0) {
            var borderColor = canvas.BorderColor ?? canvas.Background;
            FillRoundedRect(scanlines, widthPx, heightPx, stride, canvasX, canvasY, canvasW, canvasH, borderColor, radius);
            var inner = Math.Max(0, canvas.BorderPx);
            DrawCanvasFill(scanlines, widthPx, heightPx, stride, canvas, canvasX + inner, canvasY + inner, canvasW - inner * 2, canvasH - inner * 2, opts.ModuleSize, Math.Max(0, radius - inner));
        } else {
            DrawCanvasFill(scanlines, widthPx, heightPx, stride, canvas, canvasX, canvasY, canvasW, canvasH, opts.ModuleSize, radius);
        }

        if (canvas.Vignette is { BandPx: > 0 } vignette && vignette.Color.A != 0 && vignette.Strength > 0) {
            DrawCanvasVignette(
                scanlines,
                stride,
                canvasX,
                canvasY,
                canvasW,
                canvasH,
                radius,
                qrOffsetX,
                qrOffsetY,
                qrFullPx,
                vignette);
        }

        if (canvas.Halo is { RadiusPx: > 0 } halo && halo.Color.A != 0) {
            DrawCanvasHalo(
                scanlines,
                stride,
                canvasX,
                canvasY,
                canvasW,
                canvasH,
                radius,
                qrOffsetX,
                qrOffsetY,
                qrFullPx,
                halo);
        }

        if (canvas.Splash is not null && canvas.Splash.Color.A != 0 && canvas.Splash.Count > 0) {
            DrawCanvasSplash(
                scanlines,
                stride,
                canvasX,
                canvasY,
                canvasW,
                canvasH,
                radius,
                qrOffsetX,
                qrOffsetY,
                qrFullPx,
                canvas.Splash);
        }
    }

    private static void DrawCanvasFill(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngCanvasOptions canvas,
        int x,
        int y,
        int w,
        int h,
        int moduleSize,
        int radius) {
        if (canvas.BackgroundGradient is null) {
            FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y, w, h, canvas.Background, radius);
        } else {
            FillRoundedRectGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, canvas.BackgroundGradient, radius);
        }

        if (canvas.Pattern is not null) {
            DrawCanvasPattern(scanlines, widthPx, heightPx, stride, x, y, w, h, moduleSize, radius, 0, 0, protectQuietZone: false, canvas.Pattern);
        }
    }

    private static void FillQrBackground(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int qrOffsetX,
        int qrOffsetY,
        int qrFullPx) {
        if (opts.BackgroundGradient is null) {
            FillRoundedRect(scanlines, widthPx, heightPx, stride, qrOffsetX, qrOffsetY, qrFullPx, qrFullPx, opts.Background, 0);
        } else {
            FillRoundedRectGradient(scanlines, widthPx, heightPx, stride, qrOffsetX, qrOffsetY, qrFullPx, qrFullPx, opts.BackgroundGradient, 0);
        }

        if (opts.BackgroundPattern is not null) {
            var quietZonePx = opts.QuietZone * opts.ModuleSize;
            var qrSizePx = Math.Max(0, qrFullPx - quietZonePx * 2);
            DrawCanvasPattern(
                scanlines,
                widthPx,
                heightPx,
                stride,
                qrOffsetX,
                qrOffsetY,
                qrFullPx,
                qrFullPx,
                opts.ModuleSize,
                0,
                quietZonePx,
                qrSizePx,
                opts.ProtectQuietZone,
                opts.BackgroundPattern);
        }
    }

    private static void DrawEyeSparkles(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int qrOffsetX,
        int qrOffsetY,
        int qrFullPx,
        int qrSizePx,
        int size) {
        var eyes = opts.Eyes;
        if (eyes is null || eyes.SparkleCount <= 0 || eyes.SparkleRadiusPx <= 0) return;

        var moduleSize = opts.ModuleSize;
        if (moduleSize <= 0) return;

        var pad = opts.Canvas is null ? 0 : Math.Max(0, opts.Canvas.PaddingPx);
        var canvasX = qrOffsetX - pad;
        var canvasY = qrOffsetY - pad;
        var canvasW = qrFullPx + pad * 2;
        var canvasH = canvasW;
        var canvasX1 = canvasX + canvasW - 1;
        var canvasY1 = canvasY + canvasH - 1;
        var canvasRadius = opts.Canvas is null ? 0 : Math.Max(0, opts.Canvas.CornerRadiusPx);
        var canvasRadiusSq = canvasRadius * canvasRadius;

        var qrX0 = qrOffsetX;
        var qrY0 = qrOffsetY;
        var qrX1 = qrOffsetX + qrFullPx - 1;
        var qrY1 = qrOffsetY + qrFullPx - 1;

        var seed = eyes.SparkleSeed != 0
            ? eyes.SparkleSeed
            : unchecked(Environment.TickCount ^ Hash(qrOffsetX, qrOffsetY, qrFullPx, size));
        var rand = new Random(seed);

        var sparkleOverride = eyes.SparkleColor;
        if (sparkleOverride is not null && sparkleOverride.Value.A == 0) return;

        var sparkleRadius = Math.Max(1, eyes.SparkleRadiusPx);
        var sparkleSpread = Math.Max(0, eyes.SparkleSpreadPx);
        var eyeRadiusPx = (int)Math.Round(3.5 * moduleSize);
        var baseMinRing = eyeRadiusPx + sparkleRadius;

        for (var i = 0; i < eyes.SparkleCount; i++) {
            var eyeIndex = rand.Next(3);
            var eyeModuleX = eyeIndex switch {
                0 => 0,
                1 => size - 7,
                _ => 0,
            };
            var eyeModuleY = eyeIndex switch {
                0 => 0,
                1 => 0,
                _ => size - 7,
            };

            var eyeBaseX = qrOffsetX + (eyeModuleX + opts.QuietZone) * moduleSize;
            var eyeBaseY = qrOffsetY + (eyeModuleY + opts.QuietZone) * moduleSize;
            var eyeCenterX = eyeBaseX + 3 * moduleSize + moduleSize / 2;
            var eyeCenterY = eyeBaseY + 3 * moduleSize + moduleSize / 2;

            var sparkleColor = sparkleOverride ?? GetEyeOuterColor(opts, eyeIndex);
            if (sparkleOverride is null && sparkleColor.A > 180) {
                sparkleColor = new Rgba32(sparkleColor.R, sparkleColor.G, sparkleColor.B, 160);
            }
            if (sparkleColor.A == 0) continue;

            var minRing = baseMinRing;
            if (opts.Canvas is not null && eyes.SparkleProtectQrArea) {
                var edgeDistance = Math.Min(
                    Math.Min(eyeCenterX - qrX0, qrX1 - eyeCenterX),
                    Math.Min(eyeCenterY - qrY0, qrY1 - eyeCenterY));
                minRing = Math.Max(minRing, edgeDistance + sparkleRadius + 2);
            }
            var maxRing = minRing + sparkleSpread;

            var angle = rand.NextDouble() * Math.PI * 2.0;
            var ring = NextBetween(rand, minRing, maxRing);
            var cx = (int)Math.Round(eyeCenterX + Math.Cos(angle) * ring);
            var cy = (int)Math.Round(eyeCenterY + Math.Sin(angle) * ring);

            cx = Clamp(cx, canvasX + sparkleRadius, canvasX1 - sparkleRadius);
            cy = Clamp(cy, canvasY + sparkleRadius, canvasY1 - sparkleRadius);

            FillSplashCircle(
                scanlines,
                stride,
                cx,
                cy,
                sparkleRadius,
                sparkleColor,
                canvasX,
                canvasY,
                canvasX1,
                canvasY1,
                canvasRadius,
                canvasRadiusSq,
                eyes.SparkleProtectQrArea,
                qrX0,
                qrY0,
                qrX1,
                qrY1,
                qrAreaAlphaMax: 0);
        }
    }

    private static void DrawEyeAccentRings(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int qrOffsetX,
        int qrOffsetY,
        int qrFullPx,
        int qrSizePx,
        int size) {
        var eyes = opts.Eyes;
        if (eyes is null || eyes.AccentRingCount <= 0 || eyes.AccentRingThicknessPx <= 0) return;

        var moduleSize = opts.ModuleSize;
        if (moduleSize <= 0) return;

        var pad = opts.Canvas is null ? 0 : Math.Max(0, opts.Canvas.PaddingPx);
        var canvasX = qrOffsetX - pad;
        var canvasY = qrOffsetY - pad;
        var canvasW = qrFullPx + pad * 2;
        var canvasH = canvasW;
        var canvasX1 = canvasX + canvasW - 1;
        var canvasY1 = canvasY + canvasH - 1;
        var canvasRadius = opts.Canvas is null ? 0 : Math.Max(0, opts.Canvas.CornerRadiusPx);
        var canvasRadiusSq = canvasRadius * canvasRadius;

        var qrX0 = qrOffsetX;
        var qrY0 = qrOffsetY;
        var qrX1 = qrOffsetX + qrFullPx - 1;
        var qrY1 = qrOffsetY + qrFullPx - 1;

        var seed = eyes.AccentRingSeed != 0
            ? eyes.AccentRingSeed
            : unchecked(Environment.TickCount ^ Hash(qrOffsetX, qrOffsetY, qrFullPx, size ^ 0x5f3759df));
        var rand = new Random(seed);

        var accentOverride = eyes.AccentRingColor;
        if (accentOverride is not null && accentOverride.Value.A == 0) return;

        var thickness = Math.Max(1, eyes.AccentRingThicknessPx);
        var halfThickness = Math.Max(1, thickness / 2);
        var spread = Math.Max(0, eyes.AccentRingSpreadPx);
        var jitter = Math.Max(0, eyes.AccentRingJitterPx);
        var eyeRadiusPx = (int)Math.Round(3.5 * moduleSize);
        var baseMinRing = eyeRadiusPx + halfThickness + 1;

        for (var i = 0; i < eyes.AccentRingCount; i++) {
            var eyeIndex = rand.Next(3);
            var eyeModuleX = eyeIndex switch {
                0 => 0,
                1 => size - 7,
                _ => 0,
            };
            var eyeModuleY = eyeIndex switch {
                0 => 0,
                1 => 0,
                _ => size - 7,
            };

            var eyeBaseX = qrOffsetX + (eyeModuleX + opts.QuietZone) * moduleSize;
            var eyeBaseY = qrOffsetY + (eyeModuleY + opts.QuietZone) * moduleSize;
            var eyeCenterX = eyeBaseX + 3 * moduleSize + moduleSize / 2;
            var eyeCenterY = eyeBaseY + 3 * moduleSize + moduleSize / 2;

            var accentColor = accentOverride ?? GetEyeOuterColor(opts, eyeIndex);
            if (accentOverride is null && accentColor.A > 150) {
                accentColor = new Rgba32(accentColor.R, accentColor.G, accentColor.B, 120);
            }
            if (accentColor.A == 0) continue;

            var minRing = baseMinRing;
            if (opts.Canvas is not null && eyes.AccentRingProtectQrArea) {
                var edgeDistance = Math.Min(
                    Math.Min(eyeCenterX - qrX0, qrX1 - eyeCenterX),
                    Math.Min(eyeCenterY - qrY0, qrY1 - eyeCenterY));
                minRing = Math.Max(minRing, edgeDistance + halfThickness + 2);
            }
            var maxRing = minRing + spread;

            var ring = NextBetween(rand, minRing, maxRing);
            if (jitter > 0) {
                ring += rand.Next(-jitter, jitter + 1);
            }
            ring = Math.Max(minRing, ring);

            DrawRingStroke(
                scanlines,
                stride,
                eyeCenterX,
                eyeCenterY,
                ring,
                thickness,
                accentColor,
                canvasX,
                canvasY,
                canvasX1,
                canvasY1,
                canvasRadius,
                canvasRadiusSq,
                eyes.AccentRingProtectQrArea,
                qrX0,
                qrY0,
                qrX1,
                qrY1);
        }
    }

    private static void DrawRingStroke(
        byte[] scanlines,
        int stride,
        int cx,
        int cy,
        int radius,
        int thickness,
        Rgba32 color,
        int canvasX0,
        int canvasY0,
        int canvasX1,
        int canvasY1,
        int canvasRadius,
        int canvasRadiusSq,
        bool protectQr,
        int qrX0,
        int qrY0,
        int qrX1,
        int qrY1) {
        var outer = Math.Max(1, radius);
        var inner = Math.Max(0, outer - Math.Max(1, thickness));
        var outerSq = outer * outer;
        var innerSq = inner * inner;

        var x0 = Math.Max(canvasX0, cx - outer);
        var y0 = Math.Max(canvasY0, cy - outer);
        var x1 = Math.Min(canvasX1, cx + outer);
        var y1 = Math.Min(canvasY1, cy + outer);

        for (var y = y0; y <= y1; y++) {
            var dy = y - cy;
            var dy2 = dy * dy;
            for (var x = x0; x <= x1; x++) {
                var dx = x - cx;
                var dist2 = dx * dx + dy2;
                if (dist2 > outerSq || dist2 < innerSq) continue;
                if (canvasRadius > 0 && !InsideRounded(x, y, canvasX0, canvasY0, canvasX1, canvasY1, canvasRadius, canvasRadiusSq)) continue;
                if (protectQr && x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                BlendPixel(scanlines, stride, x, y, color);
            }
        }
    }

    private static void DrawEyeAccentRays(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int qrOffsetX,
        int qrOffsetY,
        int qrFullPx,
        int qrSizePx,
        int size) {
        var eyes = opts.Eyes;
        if (eyes is null || eyes.AccentRayCount <= 0 || eyes.AccentRayThicknessPx <= 0 || eyes.AccentRayLengthPx <= 0) return;

        var moduleSize = opts.ModuleSize;
        if (moduleSize <= 0) return;

        var pad = opts.Canvas is null ? 0 : Math.Max(0, opts.Canvas.PaddingPx);
        var canvasX = qrOffsetX - pad;
        var canvasY = qrOffsetY - pad;
        var canvasW = qrFullPx + pad * 2;
        var canvasH = canvasW;
        var canvasX1 = canvasX + canvasW - 1;
        var canvasY1 = canvasY + canvasH - 1;
        var canvasRadius = opts.Canvas is null ? 0 : Math.Max(0, opts.Canvas.CornerRadiusPx);
        var canvasRadiusSq = canvasRadius * canvasRadius;

        var qrX0 = qrOffsetX;
        var qrY0 = qrOffsetY;
        var qrX1 = qrOffsetX + qrFullPx - 1;
        var qrY1 = qrOffsetY + qrFullPx - 1;

        var seed = eyes.AccentRaySeed != 0
            ? eyes.AccentRaySeed
            : unchecked(Environment.TickCount ^ Hash(qrOffsetX, qrOffsetY, qrFullPx, size ^ 0x6a09e667));
        var rand = new Random(seed);

        var accentOverride = eyes.AccentRayColor;
        if (accentOverride is not null && accentOverride.Value.A == 0) return;

        var thickness = Math.Max(1, eyes.AccentRayThicknessPx);
        var halfThickness = Math.Max(1, thickness / 2);
        var baseLength = Math.Max(1, eyes.AccentRayLengthPx);
        var spread = Math.Max(0, eyes.AccentRaySpreadPx);
        var jitter = Math.Max(0, eyes.AccentRayJitterPx);
        var lengthJitter = Math.Max(0, eyes.AccentRayLengthJitterPx);
        var eyeRadiusPx = (int)Math.Round(3.5 * moduleSize);
        var baseMinRing = eyeRadiusPx + halfThickness + 2;

        for (var i = 0; i < eyes.AccentRayCount; i++) {
            var eyeIndex = rand.Next(3);
            var eyeModuleX = eyeIndex switch {
                0 => 0,
                1 => size - 7,
                _ => 0,
            };
            var eyeModuleY = eyeIndex switch {
                0 => 0,
                1 => 0,
                _ => size - 7,
            };

            var eyeBaseX = qrOffsetX + (eyeModuleX + opts.QuietZone) * moduleSize;
            var eyeBaseY = qrOffsetY + (eyeModuleY + opts.QuietZone) * moduleSize;
            var eyeCenterX = eyeBaseX + 3 * moduleSize + moduleSize / 2;
            var eyeCenterY = eyeBaseY + 3 * moduleSize + moduleSize / 2;

            var accentColor = accentOverride ?? GetEyeOuterColor(opts, eyeIndex);
            if (accentOverride is null && accentColor.A > 160) {
                accentColor = new Rgba32(accentColor.R, accentColor.G, accentColor.B, 128);
            }
            if (accentColor.A == 0) continue;

            var minRing = baseMinRing;
            if (opts.Canvas is not null && eyes.AccentRayProtectQrArea) {
                var edgeDistance = Math.Min(
                    Math.Min(eyeCenterX - qrX0, qrX1 - eyeCenterX),
                    Math.Min(eyeCenterY - qrY0, qrY1 - eyeCenterY));
                minRing = Math.Max(minRing, edgeDistance + halfThickness + 3);
            }
            var maxRing = minRing + spread;

            var ring = NextBetween(rand, minRing, maxRing);
            if (jitter > 0) {
                ring += rand.Next(-jitter, jitter + 1);
            }
            ring = Math.Max(minRing, ring);

            var angle = rand.NextDouble() * Math.PI * 2.0;
            var dirX = Math.Cos(angle);
            var dirY = Math.Sin(angle);

            var startX = (int)Math.Round(eyeCenterX + dirX * ring);
            var startY = (int)Math.Round(eyeCenterY + dirY * ring);

            var rayLength = baseLength;
            if (lengthJitter > 0) {
                rayLength += rand.Next(-lengthJitter, lengthJitter + 1);
            }
            rayLength = Math.Max(halfThickness + 2, rayLength);

            var endX = (int)Math.Round(startX + dirX * rayLength);
            var endY = (int)Math.Round(startY + dirY * rayLength);

            startX = Clamp(startX, canvasX + halfThickness, canvasX1 - halfThickness);
            startY = Clamp(startY, canvasY + halfThickness, canvasY1 - halfThickness);
            endX = Clamp(endX, canvasX + halfThickness, canvasX1 - halfThickness);
            endY = Clamp(endY, canvasY + halfThickness, canvasY1 - halfThickness);

            DrawRayStroke(
                scanlines,
                stride,
                startX,
                startY,
                endX,
                endY,
                thickness,
                accentColor,
                canvasX,
                canvasY,
                canvasX1,
                canvasY1,
                canvasRadius,
                canvasRadiusSq,
                eyes.AccentRayProtectQrArea,
                qrX0,
                qrY0,
                qrX1,
                qrY1);
        }
    }

    private static void DrawRayStroke(
        byte[] scanlines,
        int stride,
        int x0,
        int y0,
        int x1,
        int y1,
        int thickness,
        Rgba32 color,
        int canvasX0,
        int canvasY0,
        int canvasX1,
        int canvasY1,
        int canvasRadius,
        int canvasRadiusSq,
        bool protectQr,
        int qrX0,
        int qrY0,
        int qrX1,
        int qrY1) {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var dist = Math.Sqrt(dx * (double)dx + dy * (double)dy);
        var brush = Math.Max(1, thickness / 2);
        var stepDenom = Math.Max(1.0, brush * 0.6);
        var steps = Math.Max(1, (int)Math.Ceiling(dist / stepDenom));

        for (var i = 0; i <= steps; i++) {
            var t = i / (double)steps;
            var px = (int)Math.Round(x0 + dx * t);
            var py = (int)Math.Round(y0 + dy * t);

            FillSplashCircle(
                scanlines,
                stride,
                px,
                py,
                brush,
                color,
                canvasX0,
                canvasY0,
                canvasX1,
                canvasY1,
                canvasRadius,
                canvasRadiusSq,
                protectQr,
                qrX0,
                qrY0,
                qrX1,
                qrY1,
                qrAreaAlphaMax: 0);
        }
    }

    private static void DrawCanvasVignette(
        byte[] scanlines,
        int stride,
        int canvasX,
        int canvasY,
        int canvasW,
        int canvasH,
        int canvasRadius,
        int qrX,
        int qrY,
        int qrSize,
        QrPngCanvasVignetteOptions vignette) {
        if (vignette.BandPx <= 0 || vignette.Color.A == 0 || vignette.Strength <= 0) return;

        var canvasX1 = canvasX + canvasW - 1;
        var canvasY1 = canvasY + canvasH - 1;
        var canvasR = Math.Max(0, canvasRadius);
        var canvasR2 = canvasR * canvasR;
        var band = Math.Max(1, vignette.BandPx);
        var strength = vignette.Strength <= 0 ? 0 : vignette.Strength >= 2 ? 2 : vignette.Strength;
        if (strength <= 0) return;

        var qrX0 = qrX;
        var qrY0 = qrY;
        var qrX1 = qrX + qrSize - 1;
        var qrY1 = qrY + qrSize - 1;

        var baseColor = vignette.Color;
        var baseAlpha = baseColor.A * strength;
        if (baseAlpha <= 0) return;

        for (var y = canvasY; y <= canvasY1; y++) {
            for (var x = canvasX; x <= canvasX1; x++) {
                if (canvasR > 0 && !InsideRounded(x, y, canvasX, canvasY, canvasX1, canvasY1, canvasR, canvasR2)) continue;

                var distLeft = x - canvasX;
                var distRight = canvasX1 - x;
                var distTop = y - canvasY;
                var distBottom = canvasY1 - y;
                var distToEdge = Math.Min(Math.Min(distLeft, distRight), Math.Min(distTop, distBottom));
                if (distToEdge >= band) continue;

                var t = 1.0 - distToEdge / (double)band;
                var falloff = t * t;
                var alpha = baseAlpha * falloff;
                if (alpha <= 0.5) continue;

                var insideQr = x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1;
                if (insideQr && vignette.ProtectQrArea) continue;

                var cappedAlpha = alpha > 255 ? 255 : alpha;
                if (insideQr && vignette.QrAreaAlphaMax > 0 && cappedAlpha > vignette.QrAreaAlphaMax) {
                    cappedAlpha = vignette.QrAreaAlphaMax;
                }
                if (cappedAlpha <= 0) continue;

                var drawColor = new Rgba32(baseColor.R, baseColor.G, baseColor.B, (byte)cappedAlpha);
                BlendPixel(scanlines, stride, x, y, drawColor);
            }
        }
    }

    private static void DrawCanvasHalo(
        byte[] scanlines,
        int stride,
        int canvasX,
        int canvasY,
        int canvasW,
        int canvasH,
        int canvasRadius,
        int qrX,
        int qrY,
        int qrSize,
        QrPngCanvasHaloOptions halo) {
        if (halo.RadiusPx <= 0 || halo.Color.A == 0) return;

        var canvasX1 = canvasX + canvasW - 1;
        var canvasY1 = canvasY + canvasH - 1;
        var radius = Math.Max(1, halo.RadiusPx);
        var radiusSq = radius * radius;
        var canvasR = Math.Max(0, canvasRadius);
        var canvasR2 = canvasR * canvasR;

        var qrX0 = qrX;
        var qrY0 = qrY;
        var qrX1 = qrX + qrSize - 1;
        var qrY1 = qrY + qrSize - 1;

        var minX = Math.Max(canvasX, qrX0 - radius);
        var minY = Math.Max(canvasY, qrY0 - radius);
        var maxX = Math.Min(canvasX1, qrX1 + radius);
        var maxY = Math.Min(canvasY1, qrY1 + radius);
        if (minX > maxX || minY > maxY) return;

        for (var y = minY; y <= maxY; y++) {
            for (var x = minX; x <= maxX; x++) {
                if (canvasR > 0 && !InsideRounded(x, y, canvasX, canvasY, canvasX1, canvasY1, canvasR, canvasR2)) continue;

                var insideQr = x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1;
                if (insideQr && halo.ProtectQrArea) continue;

                var nearestX = x < qrX0 ? qrX0 : x > qrX1 ? qrX1 : x;
                var nearestY = y < qrY0 ? qrY0 : y > qrY1 ? qrY1 : y;
                var dx = x - nearestX;
                var dy = y - nearestY;
                var distSq = dx * dx + dy * dy;
                if (distSq > radiusSq) continue;

                var dist = Math.Sqrt(distSq);
                var t = 1.0 - dist / radius;
                var alpha = (int)Math.Round(halo.Color.A * t * t);
                if (alpha <= 0) continue;

                if (insideQr && halo.QrAreaAlphaMax > 0 && alpha > halo.QrAreaAlphaMax) {
                    alpha = halo.QrAreaAlphaMax;
                }

                var drawColor = new Rgba32(halo.Color.R, halo.Color.G, halo.Color.B, (byte)Math.Min(255, alpha));
                BlendPixel(scanlines, stride, x, y, drawColor);
            }
        }
    }

    private static void DrawCanvasSplash(
        byte[] scanlines,
        int stride,
        int canvasX,
        int canvasY,
        int canvasW,
        int canvasH,
        int canvasRadius,
        int qrX,
        int qrY,
        int qrSize,
        QrPngCanvasSplashOptions splash) {
        if (splash.Count <= 0) return;

        var canvasX1 = canvasX + canvasW - 1;
        var canvasY1 = canvasY + canvasH - 1;
        var qrX0 = qrX;
        var qrY0 = qrY;
        var qrX1 = qrX + qrSize - 1;
        var qrY1 = qrY + qrSize - 1;
        var radius = Math.Max(0, canvasRadius);
        var radiusSq = radius * radius;

        var minR = Math.Max(1, splash.MinRadiusPx);
        var maxR = Math.Max(minR, splash.MaxRadiusPx);
        var spread = Math.Max(0, splash.SpreadPx);
        var seed = splash.Seed != 0
            ? splash.Seed
            : unchecked(Environment.TickCount ^ Hash(canvasX, canvasY, canvasW, canvasH));
        var rand = new Random(seed);
        var palette = splash.Colors;
        var paletteLen = palette?.Length ?? 0;

        var bandX0 = qrX0 - spread;
        var bandX1 = qrX1 + spread;
        var bandY0 = qrY0 - spread;
        var bandY1 = qrY1 + spread;

        for (var i = 0; i < splash.Count; i++) {
            var blobColor = paletteLen > 0 ? palette![rand.Next(paletteLen)] : splash.Color;
            var blobR = NextBetween(rand, minR, maxR);
            var side = rand.Next(4);

            var cx = 0;
            var cy = 0;
            switch (side) {
                case 0: // north
                    cx = NextBetween(rand, bandX0, bandX1);
                    cy = qrY0 - NextBetween(rand, blobR, blobR + spread);
                    break;
                case 1: // east
                    cx = qrX1 + NextBetween(rand, blobR, blobR + spread);
                    cy = NextBetween(rand, bandY0, bandY1);
                    break;
                case 2: // south
                    cx = NextBetween(rand, bandX0, bandX1);
                    cy = qrY1 + NextBetween(rand, blobR, blobR + spread);
                    break;
                default: // west
                    cx = qrX0 - NextBetween(rand, blobR, blobR + spread);
                    cy = NextBetween(rand, bandY0, bandY1);
                    break;
            }

            cx = Clamp(cx, canvasX + blobR, canvasX1 - blobR);
            cy = Clamp(cy, canvasY + blobR, canvasY1 - blobR);

            FillSplashCircle(
                scanlines,
                stride,
                cx,
                cy,
                blobR,
                blobColor,
                canvasX,
                canvasY,
                canvasX1,
                canvasY1,
                radius,
                radiusSq,
                splash.ProtectQrArea,
                qrX0,
                qrY0,
                qrX1,
                qrY1,
                splash.QrAreaAlphaMax);

            if (splash.DripChance > 0 && splash.DripLengthPx > 0 && rand.NextDouble() < splash.DripChance) {
                var dripLength = NextBetween(rand, Math.Max(4, splash.DripLengthPx / 2), splash.DripLengthPx);
                var dripWidth = Math.Max(1, splash.DripWidthPx);
                var dripCx = cx + rand.Next(-blobR / 3, blobR / 3 + 1);
                var dripCy = cy + blobR / 2 + dripLength / 2;
                var rx = Math.Max(1, dripWidth / 2);
                var ry = Math.Max(2, dripLength / 2);

                FillSplashEllipse(
                    scanlines,
                    stride,
                    dripCx,
                    dripCy,
                    rx,
                    ry,
                    blobColor,
                    canvasX,
                    canvasY,
                    canvasX1,
                    canvasY1,
                    radius,
                    radiusSq,
                    splash.ProtectQrArea,
                    qrX0,
                    qrY0,
                    qrX1,
                    qrY1,
                    splash.QrAreaAlphaMax);
            }
        }
    }

    private static void FillSplashCircle(
        byte[] scanlines,
        int stride,
        int cx,
        int cy,
        int radius,
        Rgba32 color,
        int canvasX0,
        int canvasY0,
        int canvasX1,
        int canvasY1,
        int canvasRadius,
        int canvasRadiusSq,
        bool protectQr,
        int qrX0,
        int qrY0,
        int qrX1,
        int qrY1,
        int qrAreaAlphaMax) {
        var r = Math.Max(1, radius);
        var r2 = r * r;
        var x0 = Math.Max(canvasX0, cx - r);
        var y0 = Math.Max(canvasY0, cy - r);
        var x1 = Math.Min(canvasX1, cx + r);
        var y1 = Math.Min(canvasY1, cy + r);

        for (var y = y0; y <= y1; y++) {
            var dy = y - cy;
            var dy2 = dy * dy;
            for (var x = x0; x <= x1; x++) {
                var dx = x - cx;
                if (dx * dx + dy2 > r2) continue;
                if (canvasRadius > 0 && !InsideRounded(x, y, canvasX0, canvasY0, canvasX1, canvasY1, canvasRadius, canvasRadiusSq)) continue;
                var insideQr = x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1;
                if (insideQr && protectQr) continue;

                var drawColor = color;
                if (insideQr && qrAreaAlphaMax > 0 && drawColor.A > qrAreaAlphaMax) {
                    drawColor = new Rgba32(drawColor.R, drawColor.G, drawColor.B, (byte)qrAreaAlphaMax);
                }

                BlendPixel(scanlines, stride, x, y, drawColor);
            }
        }
    }

    private static void FillSplashEllipse(
        byte[] scanlines,
        int stride,
        int cx,
        int cy,
        int rx,
        int ry,
        Rgba32 color,
        int canvasX0,
        int canvasY0,
        int canvasX1,
        int canvasY1,
        int canvasRadius,
        int canvasRadiusSq,
        bool protectQr,
        int qrX0,
        int qrY0,
        int qrX1,
        int qrY1,
        int qrAreaAlphaMax) {
        var safeRx = Math.Max(1, rx);
        var safeRy = Math.Max(1, ry);
        var x0 = Math.Max(canvasX0, cx - safeRx);
        var y0 = Math.Max(canvasY0, cy - safeRy);
        var x1 = Math.Min(canvasX1, cx + safeRx);
        var y1 = Math.Min(canvasY1, cy + safeRy);
        var rx2 = safeRx * (double)safeRx;
        var ry2 = safeRy * (double)safeRy;

        for (var y = y0; y <= y1; y++) {
            var dy = y - cy;
            var dy2 = dy * (double)dy;
            for (var x = x0; x <= x1; x++) {
                var dx = x - cx;
                var dx2 = dx * (double)dx;
                if (dx2 / rx2 + dy2 / ry2 > 1.0) continue;
                if (canvasRadius > 0 && !InsideRounded(x, y, canvasX0, canvasY0, canvasX1, canvasY1, canvasRadius, canvasRadiusSq)) continue;
                var insideQr = x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1;
                if (insideQr && protectQr) continue;

                var drawColor = color;
                if (insideQr && qrAreaAlphaMax > 0 && drawColor.A > qrAreaAlphaMax) {
                    drawColor = new Rgba32(drawColor.R, drawColor.G, drawColor.B, (byte)qrAreaAlphaMax);
                }

                BlendPixel(scanlines, stride, x, y, drawColor);
            }
        }
    }

    private static int NextBetween(Random rand, int minInclusive, int maxInclusive) {
        if (minInclusive > maxInclusive) (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
        if (minInclusive == maxInclusive) return minInclusive;
        return rand.Next(minInclusive, maxInclusive + 1);
    }

    private static int Clamp(int value, int min, int max) {
        if (min > max) return value;
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static void DrawCanvasPattern(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        int moduleSize,
        int radius,
        int quietZonePx,
        int qrSizePx,
        bool protectQuietZone,
        QrPngBackgroundPatternOptions pattern) {
        if (pattern.Color.A == 0) return;
        if (pattern.ThicknessPx <= 0) return;
        var size = Math.Max(1, pattern.SizePx);
        if (pattern.SnapToModuleSize && moduleSize > 0) {
            var step = Math.Max(1, pattern.ModuleStep);
            size = Math.Max(1, moduleSize * step);
        }
        var thickness = Math.Max(1, pattern.ThicknessPx);
        var x1 = x + w;
        var y1 = y + h;
        var r = Math.Max(0, radius);
        var r2 = r * r;
        var protectQuiet = protectQuietZone && quietZonePx > 0 && qrSizePx > 0;
        var qrX0 = x + quietZonePx;
        var qrY0 = y + quietZonePx;
        var qrX1 = qrX0 + qrSizePx;
        var qrY1 = qrY0 + qrSizePx;

        for (var py = y; py < y1; py++) {
            var localY = py - y;
            for (var px = x; px < x1; px++) {
                if (r > 0 && !InsideRounded(px, py, x, y, x1 - 1, y1 - 1, r, r2)) continue;
                if (protectQuiet && (px < qrX0 || px >= qrX1 || py < qrY0 || py >= qrY1)) continue;

                var localX = px - x;
                var draw = pattern.Type switch {
                    QrPngBackgroundPatternType.Grid => (localX % size) < thickness || (localY % size) < thickness,
                    QrPngBackgroundPatternType.Checker => (((localX / size) + (localY / size)) & 1) == 0,
                    _ => IsDot(localX, localY, size, thickness),
                };

                if (!draw) continue;
                BlendPixel(scanlines, stride, px, py, pattern.Color);
            }
        }
    }

    private static bool IsDot(int localX, int localY, int cellSize, int radius) {
        var cx = cellSize / 2;
        var cy = cellSize / 2;
        var dx = localX % cellSize - cx;
        var dy = localY % cellSize - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool ShouldDrawForegroundPattern(QrPngForegroundPatternOptions pattern, int moduleSize, int px, int py, int originX, int originY, int qrSizePx) {
        var size = Math.Max(1, pattern.SizePx);
        if (pattern.SnapToModuleSize && moduleSize > 0) {
            var step = Math.Max(1, pattern.ModuleStep);
            size = Math.Max(1, moduleSize * step);
        }

        var thickness = Math.Max(1, pattern.ThicknessPx);
        if (thickness > size) thickness = size;

        var relX = px - originX;
        var relY = py - originY;
        var localX = PositiveMod(relX, size);
        var localY = PositiveMod(relY, size);

        return pattern.Type switch {
            QrPngForegroundPatternType.SpeckleDots => ShouldDrawSpeckleDot(pattern, px, py, size, thickness, localX, localY, originX, originY),
            QrPngForegroundPatternType.HalftoneDots => ShouldDrawHalftoneDot(pattern, px, py, size, thickness, localX, localY, originX, originY, qrSizePx),
            QrPngForegroundPatternType.DiagonalStripes => PositiveMod(localX + localY, size) < thickness,
            QrPngForegroundPatternType.Crosshatch =>
                PositiveMod(localX + localY, size) < thickness || PositiveMod(localX - localY, size) < thickness,
            QrPngForegroundPatternType.Starburst => IsStarburst(localX, localY, size, thickness),
            _ => IsForegroundDot(localX, localY, size, thickness),
        };
    }

    private static bool IsStarburst(int localX, int localY, int cellSize, int thicknessPx) {
        var center = (cellSize - 1) / 2.0;
        var dx = Math.Abs(localX - center);
        var dy = Math.Abs(localY - center);
        var t = Math.Max(0.5, thicknessPx);

        // Cardinal rays and diagonal rays.
        if (dx <= t || dy <= t) return true;
        return Math.Abs(dx - dy) <= t;
    }

    private static bool IsForegroundDot(int localX, int localY, int cellSize, int radiusPx) {
        var center = (cellSize - 1) / 2.0;
        var dx = localX - center;
        var dy = localY - center;
        var radius = Math.Min(radiusPx, cellSize / 2.0);
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool ShouldDrawSpeckleDot(QrPngForegroundPatternOptions pattern, int px, int py, int size, int thickness, int localX, int localY, int originX, int originY) {
        var density = Clamp01(pattern.Density);
        if (density <= 0) return false;

        var variation = Clamp01(pattern.Variation);
        var relX = px - originX;
        var relY = py - originY;
        var cellX = (relX - localX) / size;
        var cellY = (relY - localY) / size;
        var seed = pattern.Seed;

        var presenceHash = (uint)Hash(cellX, cellY, seed);
        var presence = presenceHash / (double)uint.MaxValue;
        if (presence > density) return false;

        var jitterHashX = (uint)Hash(cellX, cellY, seed ^ unchecked((int)0x9e3779b9));
        var jitterHashY = (uint)Hash(cellX, cellY, seed ^ unchecked((int)0x7f4a7c15));
        var radiusHash = (uint)Hash(cellX, cellY, seed ^ unchecked((int)0x85ebca6b));

        var jitterRange = variation * size * 0.35;
        var jitterX = (jitterHashX / (double)uint.MaxValue * 2.0 - 1.0) * jitterRange;
        var jitterY = (jitterHashY / (double)uint.MaxValue * 2.0 - 1.0) * jitterRange;

        var center = (size - 1) / 2.0;
        var cx = center + jitterX;
        var cy = center + jitterY;

        var baseRadius = Math.Max(0.75, thickness);
        var radiusScale = 1.0 + (variation * 0.9) * ((radiusHash / (double)uint.MaxValue) - 0.5);
        var radius = Math.Max(0.75, baseRadius * radiusScale);

        var dx = localX - cx;
        var dy = localY - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool ShouldDrawHalftoneDot(QrPngForegroundPatternOptions pattern, int px, int py, int size, int thickness, int localX, int localY, int originX, int originY, int qrSizePx) {
        var density = Clamp01(pattern.Density);
        if (density <= 0) return false;

        var strength = Clamp01(pattern.Variation);
        var relX = px - originX;
        var relY = py - originY;
        var cellX = (relX - localX) / size;
        var cellY = (relY - localY) / size;
        var seed = pattern.Seed;

        var radialT = ComputeRadialT(px, py, originX, originY, qrSizePx);
        var densityScaled = density * (1.0 - 0.55 * strength * radialT);
        if (densityScaled <= 0.02) return false;

        var presenceHash = (uint)Hash(cellX, cellY, seed ^ unchecked((int)0x27d4eb2f));
        var presence = presenceHash / (double)uint.MaxValue;
        if (presence > densityScaled) return false;

        var jitterHashX = (uint)Hash(cellX, cellY, seed ^ unchecked((int)0x9e3779b9));
        var jitterHashY = (uint)Hash(cellX, cellY, seed ^ unchecked((int)0x7f4a7c15));
        var radiusHash = (uint)Hash(cellX, cellY, seed ^ unchecked((int)0x85ebca6b));

        var jitterRange = strength * size * 0.22;
        var jitterX = (jitterHashX / (double)uint.MaxValue * 2.0 - 1.0) * jitterRange;
        var jitterY = (jitterHashY / (double)uint.MaxValue * 2.0 - 1.0) * jitterRange;

        var center = (size - 1) / 2.0;
        var cx = center + jitterX;
        var cy = center + jitterY;

        var baseRadius = Math.Max(0.75, thickness);
        var radialScale = 1.0 - 0.65 * strength * radialT;
        if (radialScale < 0.35) radialScale = 0.35;
        var jitterScale = 1.0 + strength * 0.6 * ((radiusHash / (double)uint.MaxValue) - 0.5);
        var radius = baseRadius * radialScale * jitterScale;
        if (radius < 0.75) radius = 0.75;

        var dx = localX - cx;
        var dy = localY - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static double ComputeRadialT(int px, int py, int originX, int originY, int qrSizePx) {
        if (qrSizePx <= 0) return 0;
        var centerX = originX + qrSizePx / 2.0;
        var centerY = originY + qrSizePx / 2.0;
        var dx = px - centerX;
        var dy = py - centerY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var maxDist = Math.Sqrt(2.0) * (qrSizePx / 2.0);
        if (maxDist <= 0) return 0;
        return Clamp01(dist / maxDist);
    }

    private static double Clamp01(double value) {
        if (value <= 0) return 0;
        if (value >= 1) return 1;
        return value;
    }

    private static int PositiveMod(int value, int modulus) {
        if (modulus <= 0) return 0;
        var m = value % modulus;
        return m < 0 ? m + modulus : m;
    }

    private static Rgba32 ComposeOver(Rgba32 top, Rgba32 bottom) {
        var ta = top.A;
        if (ta == 0) return bottom;
        if (ta == 255 && bottom.A == 255) return top;

        var ba = bottom.A;
        var inv = 255 - ta;
        var outA = ta + (ba * inv + 127) / 255;
        if (outA <= 0) return new Rgba32(0, 0, 0, 0);

        var outRPre = top.R * ta + (int)((bottom.R * ba * (long)inv + 127) / 255);
        var outGPre = top.G * ta + (int)((bottom.G * ba * (long)inv + 127) / 255);
        var outBPre = top.B * ta + (int)((bottom.B * ba * (long)inv + 127) / 255);

        var outR = (outRPre + outA / 2) / outA;
        var outG = (outGPre + outA / 2) / outA;
        var outB = (outBPre + outA / 2) / outA;

        return new Rgba32((byte)outR, (byte)outG, (byte)outB, (byte)outA);
    }

    private static void BlendPixel(byte[] scanlines, int stride, int x, int y, Rgba32 color) {
        if (color.A == 255) {
            var p = y * (stride + 1) + 1 + x * 4;
            scanlines[p + 0] = color.R;
            scanlines[p + 1] = color.G;
            scanlines[p + 2] = color.B;
            scanlines[p + 3] = 255;
            return;
        }

        var rowStart = y * (stride + 1) + 1 + x * 4;
        var dr = scanlines[rowStart + 0];
        var dg = scanlines[rowStart + 1];
        var db = scanlines[rowStart + 2];
        var da = scanlines[rowStart + 3];
        var sa = color.A;
        var invSa = 255 - sa;
        var outA = sa + (da * invSa + 127) / 255;
        if (outA == 0) {
            scanlines[rowStart + 0] = 0;
            scanlines[rowStart + 1] = 0;
            scanlines[rowStart + 2] = 0;
            scanlines[rowStart + 3] = 0;
            return;
        }
        scanlines[rowStart + 0] = (byte)((color.R * sa + dr * da * invSa / 255 + outA / 2) / outA);
        scanlines[rowStart + 1] = (byte)((color.G * sa + dg * da * invSa / 255 + outA / 2) / outA);
        scanlines[rowStart + 2] = (byte)((color.B * sa + db * da * invSa / 255 + outA / 2) / outA);
        scanlines[rowStart + 3] = (byte)outA;
    }

    private static void FillBackgroundGradient(byte[] scanlines, int widthPx, int heightPx, int stride, QrPngGradientOptions gradient) {
        var rowStride = stride + 1;
        var info = new GradientInfo(gradient, widthPx - 1, heightPx - 1);
        for (var y = 0; y < heightPx; y++) {
            var rowStart = y * rowStride;
            scanlines[rowStart] = 0;
            var p = rowStart + 1;
            for (var x = 0; x < widthPx; x++, p += 4) {
                var color = GetGradientColorInBox(info, x, y, 0, 0);
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static MaskInfo GetScaleMask(Dictionary<int, MaskInfo> cache, int moduleSize, QrPngModuleShape shape, double scale, int radius) {
        var key = QuantizeScaleKey(scale);
        if (!cache.TryGetValue(key, out var info)) {
            var mask = BuildModuleMask(moduleSize, shape, scale, radius);
            info = new MaskInfo(mask, IsSolidMask(mask));
            cache[key] = info;
        }
        return info;
    }

    private static int QuantizeScaleKey(double scale) {
        const double step = 0.01;
        return (int)Math.Round(scale / step);
    }

    private static double ClampScale(double scale) {
        if (scale < 0.1) return 0.1;
        if (scale > 1.0) return 1.0;
        return scale;
    }

    private static double GetScaleFactor(in ModuleScaleMapInfo map, int mx, int my) {
        switch (map.Mode) {
            case QrPngModuleScaleMode.Checker:
                return ((mx + my) & 1) == 0 ? map.MaxScale : map.MinScale;
            case QrPngModuleScaleMode.Random:
                var hash = (uint)Hash(mx, my, map.Seed);
                var tRand = hash / (double)uint.MaxValue;
                return Lerp(map.MaxScale, map.MinScale, tRand);
            case QrPngModuleScaleMode.Radial:
                var dx = mx - map.Center;
                var dy = my - map.Center;
                var dist = Math.Sqrt((double)dx * dx + (double)dy * dy);
                var tRad = map.MaxDist <= 0 ? 0 : dist / map.MaxDist;
                return Lerp(map.MaxScale, map.MinScale, tRad);
            default:
                var ring = Math.Max(Math.Abs(mx - map.Center), Math.Abs(my - map.Center));
                if (map.RingSize > 1) ring /= map.RingSize;
                var maxRing = map.RingSize > 1 ? map.MaxRing / map.RingSize : map.MaxRing;
                var tRing = maxRing <= 0 ? 0 : ring / (double)maxRing;
                return Lerp(map.MaxScale, map.MinScale, tRing);
        }
    }

    private static double Lerp(double a, double b, double t) {
        if (t <= 0) return a;
        if (t >= 1) return b;
        return a + (b - a) * t;
    }

    private static Rgba32 GetPaletteColor(in PaletteInfo palette, int mx, int my) {
        var colors = palette.Colors;
        var count = colors.Length;
        if (count == 1) return colors[0];

        var index = palette.Mode switch {
            QrPngPaletteMode.Checker => (mx + my) & 1,
            QrPngPaletteMode.Random => (int)((uint)Hash(mx, my, palette.Seed) % (uint)count),
            QrPngPaletteMode.Rings => GetRingIndex(mx, my, palette.Center, palette.RingSize) % count,
            _ => (mx + my) % count,
        };

        if (index < 0) index = -index;
        return colors[index % count];
    }

    private static bool IsInCornerZone(int mx, int my, int size, int cornerSize) {
        if (cornerSize <= 0) return false;
        if (mx < cornerSize && my < cornerSize) return true;
        if (mx >= size - cornerSize && my < cornerSize) return true;
        if (mx < cornerSize && my >= size - cornerSize) return true;
        return mx >= size - cornerSize && my >= size - cornerSize;
    }

    private static int GetRingIndex(int x, int y, int center, int ringSize) {
        var dx = Math.Abs(x - center);
        var dy = Math.Abs(y - center);
        var ring = dx > dy ? dx : dy;
        return ringSize <= 1 ? ring : ring / ringSize;
    }

    private static int Hash(int x, int y, int seed) {
        unchecked {
            var h = (uint)seed;
            h = (h * 31u) ^ (uint)x;
            h = (h * 31u) ^ (uint)y;
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            return (int)h;
        }
    }

    private static int Hash(int a, int b, int c, int d) {
        unchecked {
            var h = (uint)Hash(a, b, c);
            h = (h * 31u) ^ (uint)d;
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            return (int)h;
        }
    }

    private const int NeighborNorth = 1 << 0;
    private const int NeighborEast = 1 << 1;
    private const int NeighborSouth = 1 << 2;
    private const int NeighborWest = 1 << 3;

    private static int GetNeighborMask(BitMatrix modules, int mx, int my) {
        var mask = 0;
        if (my > 0 && modules[mx, my - 1]) mask |= NeighborNorth;
        if (mx + 1 < modules.Width && modules[mx + 1, my]) mask |= NeighborEast;
        if (my + 1 < modules.Height && modules[mx, my + 1]) mask |= NeighborSouth;
        if (mx > 0 && modules[mx - 1, my]) mask |= NeighborWest;
        return mask;
    }

    private static MaskInfo GetConnectedMask(
        Dictionary<int, MaskInfo> cache,
        int moduleSize,
        double scale,
        int cornerRadiusPx,
        int neighborMask,
        QrPngModuleShape shape) {
        var key = Hash(QuantizeScaleKey(scale), cornerRadiusPx, neighborMask & 0xF, (int)shape);
        if (!cache.TryGetValue(key, out var info)) {
            var mask = BuildConnectedMask(moduleSize, scale, cornerRadiusPx, neighborMask, shape);
            info = new MaskInfo(mask, IsSolidMask(mask));
            cache[key] = info;
        }
        return info;
    }

    private static bool[] BuildConnectedMask(
        int moduleSize,
        double scale,
        int cornerRadiusPx,
        int neighborMask,
        QrPngModuleShape shape) {
        return shape switch {
            QrPngModuleShape.ConnectedRounded => BuildConnectedRoundedMask(moduleSize, scale, cornerRadiusPx, neighborMask),
            QrPngModuleShape.ConnectedSquircle => BuildConnectedSquircleMask(moduleSize, scale, neighborMask),
            _ => BuildConnectedRoundedMask(moduleSize, scale, cornerRadiusPx, neighborMask),
        };
    }

    private static bool[] BuildConnectedRoundedMask(int moduleSize, double scale, int cornerRadiusPx, int neighborMask) {
        var mask = new bool[moduleSize * moduleSize];
        if (moduleSize <= 0) return mask;

        if (scale < 0.1) scale = 0.1;
        if (scale > 1.0) scale = 1.0;

        var inset = (int)Math.Round((moduleSize - moduleSize * scale) / 2.0);
        if (inset < 0) inset = 0;
        if (inset > moduleSize / 2) inset = moduleSize / 2;

        var insetTop = (neighborMask & NeighborNorth) != 0 ? 0 : inset;
        var insetRight = (neighborMask & NeighborEast) != 0 ? 0 : inset;
        var insetBottom = (neighborMask & NeighborSouth) != 0 ? 0 : inset;
        var insetLeft = (neighborMask & NeighborWest) != 0 ? 0 : inset;

        var x0 = insetLeft;
        var y0 = insetTop;
        var x1 = moduleSize - insetRight - 1;
        var y1 = moduleSize - insetBottom - 1;
        if (x1 < x0 || y1 < y0) return mask;

        var innerW = x1 - x0 + 1;
        var innerH = y1 - y0 + 1;
        var minInner = innerW < innerH ? innerW : innerH;
        if (minInner <= 0) return mask;

        var radius = cornerRadiusPx;
        if (radius <= 0) radius = minInner / 4;
        if (radius > minInner / 2) radius = minInner / 2;
        if (radius < 0) radius = 0;

        var roundTl = (neighborMask & (NeighborNorth | NeighborWest)) == 0;
        var roundTr = (neighborMask & (NeighborNorth | NeighborEast)) == 0;
        var roundBr = (neighborMask & (NeighborSouth | NeighborEast)) == 0;
        var roundBl = (neighborMask & (NeighborSouth | NeighborWest)) == 0;

        for (var y = 0; y < moduleSize; y++) {
            var row = y * moduleSize;
            for (var x = 0; x < moduleSize; x++) {
                mask[row + x] = InsideRoundedConnected(x, y, x0, y0, x1, y1, radius, roundTl, roundTr, roundBr, roundBl);
            }
        }

        return mask;
    }

    private static bool[] BuildConnectedSquircleMask(int moduleSize, double scale, int neighborMask) {
        var mask = new bool[moduleSize * moduleSize];
        if (moduleSize <= 0) return mask;

        if (scale < 0.1) scale = 0.1;
        if (scale > 1.0) scale = 1.0;

        var inset = (int)Math.Round((moduleSize - moduleSize * scale) / 2.0);
        if (inset < 0) inset = 0;
        if (inset > moduleSize / 2) inset = moduleSize / 2;

        var insetTop = (neighborMask & NeighborNorth) != 0 ? 0 : inset;
        var insetRight = (neighborMask & NeighborEast) != 0 ? 0 : inset;
        var insetBottom = (neighborMask & NeighborSouth) != 0 ? 0 : inset;
        var insetLeft = (neighborMask & NeighborWest) != 0 ? 0 : inset;

        var x0 = insetLeft;
        var y0 = insetTop;
        var x1 = moduleSize - insetRight - 1;
        var y1 = moduleSize - insetBottom - 1;
        if (x1 < x0 || y1 < y0) return mask;

        var innerW = x1 - x0 + 1;
        var innerH = y1 - y0 + 1;
        if (innerW <= 0 || innerH <= 0) return mask;

        var centerX = x0 + (innerW - 1) / 2.0;
        var centerY = y0 + (innerH - 1) / 2.0;
        var halfW = innerW / 2.0;
        var halfH = innerH / 2.0;
        if (halfW <= 0 || halfH <= 0) return mask;

        var roundTl = (neighborMask & (NeighborNorth | NeighborWest)) == 0;
        var roundTr = (neighborMask & (NeighborNorth | NeighborEast)) == 0;
        var roundBr = (neighborMask & (NeighborSouth | NeighborEast)) == 0;
        var roundBl = (neighborMask & (NeighborSouth | NeighborWest)) == 0;

        for (var y = 0; y < moduleSize; y++) {
            var row = y * moduleSize;
            for (var x = 0; x < moduleSize; x++) {
                mask[row + x] = InsideSquircleConnected(
                    x,
                    y,
                    x0,
                    y0,
                    x1,
                    y1,
                    centerX,
                    centerY,
                    halfW,
                    halfH,
                    roundTl,
                    roundTr,
                    roundBr,
                    roundBl);
            }
        }

        return mask;
    }

    private static bool InsideRoundedConnected(
        int x,
        int y,
        int x0,
        int y0,
        int x1,
        int y1,
        int radius,
        bool roundTl,
        bool roundTr,
        bool roundBr,
        bool roundBl) {
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
        if (radius <= 0) return true;

        var r2 = radius * radius;
        var tlX = x0 + radius;
        var tlY = y0 + radius;
        var brX = x1 - radius;
        var brY = y1 - radius;

        if (roundTl && x < tlX && y < tlY) {
            var cx = tlX - 1;
            var cy = tlY - 1;
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= r2;
        }

        if (roundTr && x > brX && y < tlY) {
            var cx = brX + 1;
            var cy = tlY - 1;
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= r2;
        }

        if (roundBr && x > brX && y > brY) {
            var cx = brX + 1;
            var cy = brY + 1;
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= r2;
        }

        if (roundBl && x < tlX && y > brY) {
            var cx = tlX - 1;
            var cy = brY + 1;
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= r2;
        }

        return true;
    }

    private static bool InsideSquircleConnected(
        int x,
        int y,
        int x0,
        int y0,
        int x1,
        int y1,
        double centerX,
        double centerY,
        double halfW,
        double halfH,
        bool roundTl,
        bool roundTr,
        bool roundBr,
        bool roundBl) {
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
        if (halfW <= 0 || halfH <= 0) return true;

        var isLeft = x < centerX;
        var isTop = y < centerY;
        if (isLeft && isTop && !roundTl) return true;
        if (!isLeft && isTop && !roundTr) return true;
        if (!isLeft && !isTop && !roundBr) return true;
        if (isLeft && !isTop && !roundBl) return true;

        var dx = Math.Abs(x - centerX) / halfW;
        var dy = Math.Abs(y - centerY) / halfH;
        var dx2 = dx * dx;
        var dy2 = dy * dy;
        return dx2 * dx2 + dy2 * dy2 <= 1.0;
    }

    private static bool[] BuildModuleMask(
        int moduleSize,
        QrPngModuleShape shape,
        double scale,
        int cornerRadiusPx) {
        var mask = new bool[moduleSize * moduleSize];
        if (moduleSize <= 0) return mask;

        if (shape == QrPngModuleShape.ConnectedRounded) shape = QrPngModuleShape.Rounded;
        if (shape == QrPngModuleShape.ConnectedSquircle) shape = QrPngModuleShape.Squircle;
        if (scale < 0.1) scale = 0.1;
        if (scale > 1.0) scale = 1.0;
        if (shape == QrPngModuleShape.Dot) scale *= QrPngShapeDefaults.DotScale;
        if (shape == QrPngModuleShape.DotGrid) scale *= QrPngShapeDefaults.DotGridScale;

        var inset = (int)Math.Round((moduleSize - moduleSize * scale) / 2.0);
        if (inset < 0) inset = 0;
        if (inset > moduleSize / 2) inset = moduleSize / 2;
        var inner = moduleSize - inset * 2;
        if (inner <= 0) return mask;

        var radius = cornerRadiusPx;
        if (radius <= 0) radius = inner / 4;
        if (radius > inner / 2) radius = inner / 2;
        var r2 = radius * radius;
        var center = (inner - 1) / 2.0;
        var circleR = inner / 2.0;
        var circleR2 = circleR * circleR;

        var dotGridCenter0 = 0.0;
        var dotGridCenter1 = 0.0;
        var dotGridRadius = 0.0;
        var dotGridRadiusSq = 0.0;
        if (shape == QrPngModuleShape.DotGrid) {
            dotGridCenter0 = (inner - 1) * QrPngShapeDefaults.DotGridCenterFactor;
            dotGridCenter1 = (inner - 1) * (1.0 - QrPngShapeDefaults.DotGridCenterFactor);
            dotGridRadius = Math.Max(QrPngShapeDefaults.DotGridMinRadius, inner * QrPngShapeDefaults.DotGridRadiusFactor);
            dotGridRadiusSq = dotGridRadius * dotGridRadius;
        }

        for (var y = 0; y < moduleSize; y++) {
            for (var x = 0; x < moduleSize; x++) {
                if (x < inset || x >= inset + inner || y < inset || y >= inset + inner) {
                    mask[y * moduleSize + x] = false;
                    continue;
                }
                var lx = x - inset;
                var ly = y - inset;
                var inside = shape switch {
                    QrPngModuleShape.Square => true,
                    QrPngModuleShape.Circle => InsideCircle(lx, ly, center, circleR2),
                    QrPngModuleShape.Rounded => InsideRoundedLocal(lx, ly, inner, radius, r2),
                    QrPngModuleShape.Diamond => InsideDiamond(lx, ly, center, circleR),
                    QrPngModuleShape.SoftDiamond => InsideSoftDiamond(lx, ly, center, circleR),
                    QrPngModuleShape.Squircle => InsideSquircle(lx, ly, center, circleR),
                    QrPngModuleShape.Leaf => InsideLeaf(lx, ly, center, circleR),
                    QrPngModuleShape.Wave => InsideWave(lx, ly, center, circleR),
                    QrPngModuleShape.Blob => InsideBlob(lx, ly, center, circleR),
                    QrPngModuleShape.Dot => InsideCircle(lx, ly, center, circleR2),
                    QrPngModuleShape.DotGrid => InsideDotGrid(lx, ly, dotGridCenter0, dotGridCenter1, dotGridRadiusSq),
                    _ => true,
                };
                mask[y * moduleSize + x] = inside;
            }
        }

        return mask;
    }

    private static bool InsideCircle(int x, int y, double center, double radiusSq) {
        var dx = x - center;
        var dy = y - center;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static bool InsideDiamond(int x, int y, double center, double radius) {
        var dx = Math.Abs(x - center);
        var dy = Math.Abs(y - center);
        return dx + dy <= radius;
    }

    private static bool InsideSoftDiamond(int x, int y, double center, double radius) {
        if (radius <= 0) return false;
        var dx = Math.Abs(x - center);
        var dy = Math.Abs(y - center);
        var p = QrPngShapeDefaults.SoftDiamondExponent;
        return Math.Pow(dx, p) + Math.Pow(dy, p) <= Math.Pow(radius, p);
    }

    private static bool InsideSquircle(int x, int y, double center, double radius) {
        if (radius <= 0) return false;
        var dx = Math.Abs(x - center) / radius;
        var dy = Math.Abs(y - center) / radius;
        var dx2 = dx * dx;
        var dy2 = dy * dy;
        return dx2 * dx2 + dy2 * dy2 <= 1.0;
    }

    private static bool InsideLeaf(int x, int y, double center, double radius) {
        if (radius <= 0) return false;
        var r = radius * QrPngShapeDefaults.LeafRadiusFactor;
        var d = radius * QrPngShapeDefaults.LeafOffsetFactor;
        if (r <= 0) return false;
        var dx = x - center;
        var dy = y - center;
        var r2 = r * r;
        var dx1 = dx + d;
        var dx2 = dx - d;
        return dx1 * dx1 + dy * dy <= r2 && dx2 * dx2 + dy * dy <= r2;
    }

    private static bool InsideWave(int x, int y, double center, double radius) {
        if (radius <= 0) return false;
        var dx = x - center;
        var dy = y - center;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var angle = Math.Atan2(dy, dx);
        var boundary = radius * (1.0 + QrPngShapeDefaults.WaveAmplitude * Math.Sin(QrPngShapeDefaults.WaveFrequency * angle));
        var min = radius * 0.2;
        if (boundary < min) boundary = min;
        return dist <= boundary;
    }

    private static bool InsideBlob(int x, int y, double center, double radius) {
        if (radius <= 0) return false;
        var dx = x - center;
        var dy = y - center;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var angle = Math.Atan2(dy, dx);
        var wave = Math.Sin(QrPngShapeDefaults.BlobFrequencyA * angle) +
                   0.5 * Math.Sin(QrPngShapeDefaults.BlobFrequencyB * angle);
        var boundary = radius * (1.0 + QrPngShapeDefaults.BlobAmplitude * (wave / 1.5));
        var min = radius * 0.2;
        if (boundary < min) boundary = min;
        return dist <= boundary;
    }

    private static bool InsideDotGrid(int x, int y, double c0, double c1, double radiusSq) {
        return InsideCircle(x, y, c0, radiusSq) ||
               InsideCircle(x, y, c1, radiusSq) ||
               InsideCircle(x, y, c0, radiusSq, c1) ||
               InsideCircle(x, y, c1, radiusSq, c0);
    }

    private static bool InsideCircle(int x, int y, double cx, double radiusSq, double cy) {
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static bool InsideRoundedLocal(int x, int y, int size, int radius, int radiusSq) {
        if (radius <= 0) return true;
        if (x >= radius && x < size - radius) return true;
        if (y >= radius && y < size - radius) return true;

        var cx = x < radius ? radius - 1 : size - radius;
        var cy = y < radius ? radius - 1 : size - radius;
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static Rgba32 GetGradientColor(GradientInfo gradient, int px, int py, int originX, int originY) {
        var u = (px - originX) * gradient.InvSizeX;
        var v = (py - originY) * gradient.InvSizeY;
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;

        double t = gradient.Type switch {
            QrPngGradientType.Horizontal => u,
            QrPngGradientType.Vertical => v,
            QrPngGradientType.DiagonalDown => (u + v) * 0.5,
            QrPngGradientType.DiagonalUp => (u + (1 - v)) * 0.5,
            QrPngGradientType.Radial => GetRadialT(u, v, gradient.CenterX, gradient.CenterY, gradient.MaxDist),
            _ => u,
        };

        return Lerp(gradient, t);
    }

    private static Rgba32 GetGradientColorInBox(GradientInfo gradient, int px, int py, int x, int y) {
        var u = (px - x) * gradient.InvSizeX;
        var v = (py - y) * gradient.InvSizeY;
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;

        double t = gradient.Type switch {
            QrPngGradientType.Horizontal => u,
            QrPngGradientType.Vertical => v,
            QrPngGradientType.DiagonalDown => (u + v) * 0.5,
            QrPngGradientType.DiagonalUp => (u + (1 - v)) * 0.5,
            QrPngGradientType.Radial => GetRadialT(u, v, gradient.CenterX, gradient.CenterY, gradient.MaxDist),
            _ => u,
        };

        return Lerp(gradient, t);
    }

    private static double GetRadialT(double u, double v, double cx, double cy, double maxDist) {
        var dx = u - cx;
        var dy = v - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (maxDist <= 0) return 0;
        var t = dist / maxDist;
        if (t < 0) return 0;
        if (t > 1) return 1;
        return t;
    }

    private static Rgba32 Lerp(GradientInfo gradient, double t) {
        if (t <= 0) return gradient.StartColor;
        if (t >= 1) return gradient.EndColor;
        var r = (byte)Math.Round(gradient.StartColor.R + gradient.Dr * t);
        var g = (byte)Math.Round(gradient.StartColor.G + gradient.Dg * t);
        var b = (byte)Math.Round(gradient.StartColor.B + gradient.Db * t);
        var a = (byte)Math.Round(gradient.StartColor.A + gradient.Da * t);
        return new Rgba32(r, g, b, a);
    }

    private readonly struct GradientInfo {
        public QrPngGradientType Type { get; }
        public Rgba32 StartColor { get; }
        public Rgba32 EndColor { get; }
        public double CenterX { get; }
        public double CenterY { get; }
        public double InvSizeX { get; }
        public double InvSizeY { get; }
        public double MaxDist { get; }
        public int Dr { get; }
        public int Dg { get; }
        public int Db { get; }
        public int Da { get; }

        public GradientInfo(QrPngGradientOptions gradient, int sizeX, int sizeY) {
            Type = gradient.Type;
            StartColor = gradient.StartColor;
            EndColor = gradient.EndColor;
            CenterX = gradient.CenterX;
            CenterY = gradient.CenterY;
            var sx = Math.Max(1, sizeX);
            var sy = Math.Max(1, sizeY);
            InvSizeX = 1.0 / sx;
            InvSizeY = 1.0 / sy;
            Dr = EndColor.R - StartColor.R;
            Dg = EndColor.G - StartColor.G;
            Db = EndColor.B - StartColor.B;
            Da = EndColor.A - StartColor.A;
            MaxDist = Type == QrPngGradientType.Radial ? ComputeMaxDist(CenterX, CenterY) : 0.0;
        }
    }

    private readonly struct ModuleScaleMapInfo {
        public QrPngModuleScaleMode Mode { get; }
        public double MinScale { get; }
        public double MaxScale { get; }
        public int RingSize { get; }
        public int Seed { get; }
        public bool ApplyToEyes { get; }
        public int Center { get; }
        public int MaxRing { get; }
        public double MaxDist { get; }

        public ModuleScaleMapInfo(QrPngModuleScaleMapOptions options, int size) {
            Mode = options.Mode;
            MinScale = options.MinScale;
            MaxScale = options.MaxScale;
            RingSize = options.RingSize;
            Seed = options.Seed;
            ApplyToEyes = options.ApplyToEyes;
            Center = (size - 1) / 2;
            MaxRing = Math.Max(Center, size - 1 - Center);
            MaxDist = Math.Sqrt((double)MaxRing * MaxRing + (double)MaxRing * MaxRing);
        }
    }

    private readonly struct PaletteZoneInfo {
        public PaletteInfo? CenterPalette { get; }
        public PaletteInfo? CornerPalette { get; }
        public int CenterStart { get; }
        public int CenterEnd { get; }
        public int CornerSize { get; }
        public int Size { get; }

        public PaletteZoneInfo(QrPngPaletteZoneOptions options, int size) {
            Size = size;
            var centerSize = Math.Min(options.CenterSize, size);
            if (centerSize > 0 && options.CenterPalette is not null) {
                CenterPalette = new PaletteInfo(options.CenterPalette, size);
                CenterStart = (size - centerSize) / 2;
                CenterEnd = CenterStart + centerSize;
            } else {
                CenterPalette = null;
                CenterStart = 0;
                CenterEnd = 0;
            }

            var cornerSize = Math.Min(options.CornerSize, size);
            if (cornerSize > 0 && options.CornerPalette is not null) {
                CornerPalette = new PaletteInfo(options.CornerPalette, size);
                CornerSize = cornerSize;
            } else {
                CornerPalette = null;
                CornerSize = 0;
            }
        }

        public bool TryGetPalette(int mx, int my, out PaletteInfo palette) {
            if (CenterPalette.HasValue && mx >= CenterStart && mx < CenterEnd && my >= CenterStart && my < CenterEnd) {
                palette = CenterPalette.Value;
                return true;
            }

            if (CornerPalette.HasValue && IsInCornerZone(mx, my, Size, CornerSize)) {
                palette = CornerPalette.Value;
                return true;
            }

            palette = default;
            return false;
        }
    }

    private readonly struct MaskInfo {
        public bool[] Mask { get; }
        public bool IsSolid { get; }

        public MaskInfo(bool[] mask, bool isSolid) {
            Mask = mask;
            IsSolid = isSolid;
        }
    }

    private readonly struct PaletteInfo {
        public QrPngPaletteMode Mode { get; }
        public Rgba32[] Colors { get; }
        public int Seed { get; }
        public int RingSize { get; }
        public int Center { get; }
        public bool ApplyToEyes { get; }

        public PaletteInfo(QrPngPaletteOptions options, int size) {
            Mode = options.Mode;
            Colors = options.Colors;
            Seed = options.Seed;
            RingSize = options.RingSize;
            Center = (size - 1) / 2;
            ApplyToEyes = options.ApplyToEyes;
        }
    }

    private static double ComputeMaxDist(double cx, double cy) {
        var maxDist = 0.0;
        maxDist = Math.Max(maxDist, Distance(0, 0, cx, cy));
        maxDist = Math.Max(maxDist, Distance(1, 0, cx, cy));
        maxDist = Math.Max(maxDist, Distance(0, 1, cx, cy));
        maxDist = Math.Max(maxDist, Distance(1, 1, cx, cy));
        return maxDist;
    }

    private static double Distance(double x, double y, double cx, double cy) {
        var dx = x - cx;
        var dy = y - cy;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private enum EyeKind {
        None,
        Outer,
        Inner,
    }

    private static EyeKind GetEyeKind(int x, int y, int size) {
        if (IsInEye(x, y, 0, 0)) return GetEyeModuleKind(x, y, 0, 0);
        if (IsInEye(x, y, size - 7, 0)) return GetEyeModuleKind(x, y, size - 7, 0);
        if (IsInEye(x, y, 0, size - 7)) return GetEyeModuleKind(x, y, 0, size - 7);
        return EyeKind.None;
    }

}

using System;
using System.Collections.Generic;
using System.IO;
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
        if (opts.BackgroundSupersample < 1 || opts.BackgroundSupersample > 4) throw new ArgumentOutOfRangeException(nameof(opts.BackgroundSupersample));
        opts.ForegroundGradient?.Validate();
        opts.ForegroundPalette?.Validate();
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
                DrawCanvasPattern(buffer, widthPx, heightPx, stride, qrOffsetX, qrOffsetY, qrFullPx, qrFullPx, opts.ModuleSize, 0, opts.BackgroundPattern);
            }
        } else {
            PngRenderHelpers.FillBackground(buffer, widthPx, heightPx, stride, Rgba32.Transparent);
            DrawCanvas(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx);
            FillQrBackground(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrFullPx);
        }
        var mask = BuildModuleMask(opts.ModuleSize, opts.ModuleShape, opts.ModuleScale, opts.ModuleCornerRadiusPx);
        var maskSolid = IsSolidMask(mask);
        var eyeOuterMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.OuterShape, opts.Eyes.OuterScale, opts.Eyes.OuterCornerRadiusPx);
        var eyeOuterSolid = eyeOuterMask == mask ? maskSolid : IsSolidMask(eyeOuterMask);
        var eyeInnerMask = opts.Eyes is null
            ? mask
            : BuildModuleMask(opts.ModuleSize, opts.Eyes.InnerShape, opts.Eyes.InnerScale, opts.Eyes.InnerCornerRadiusPx);
        var eyeInnerSolid = eyeInnerMask == mask ? maskSolid : IsSolidMask(eyeInnerMask);
        var useFrame = opts.Eyes is not null && opts.Eyes.UseFrame;
        var background = opts.Background;
        var gradientInfo = opts.ForegroundGradient is null
            ? (GradientInfo?)null
            : new GradientInfo(opts.ForegroundGradient, qrSizePx - 1, qrSizePx - 1);
        var paletteInfo = opts.ForegroundPalette is null ? (PaletteInfo?)null : new PaletteInfo(opts.ForegroundPalette, size);
        var zoneInfo = opts.ForegroundPaletteZones is null ? (PaletteZoneInfo?)null : new PaletteZoneInfo(opts.ForegroundPaletteZones, size);
        var scaleMapInfo = opts.ModuleScaleMap is null ? (ModuleScaleMapInfo?)null : new ModuleScaleMapInfo(opts.ModuleScaleMap, size);
        var scaleMaskCache = scaleMapInfo.HasValue ? new Dictionary<int, MaskInfo>(8) : null;
        var detectEyes = opts.Eyes is not null || paletteInfo.HasValue || zoneInfo.HasValue || scaleMapInfo.HasValue;
        var eyeOuterGradient = opts.Eyes?.OuterGradient;
        var eyeInnerGradient = opts.Eyes?.InnerGradient;
        var eyeOuterGradientInfo = eyeOuterGradient is null ? (GradientInfo?)null : new GradientInfo(eyeOuterGradient, 7 * opts.ModuleSize - 1, 7 * opts.ModuleSize - 1);
        var eyeInnerGradientInfo = eyeInnerGradient is null ? (GradientInfo?)null : new GradientInfo(eyeInnerGradient, 3 * opts.ModuleSize - 1, 3 * opts.ModuleSize - 1);

        for (var my = 0; my < size; my++) {
            for (var mx = 0; mx < size; mx++) {
                if (!modules[mx, my]) continue;
                var eyeKind = EyeKind.None;
                var eyeX = 0;
                var eyeY = 0;
                if (detectEyes && TryGetEye(mx, my, size, out eyeX, out eyeY, out var kind)) {
                    eyeKind = kind;
                }

                if (useFrame && eyeKind != EyeKind.None) continue;

                var useMask = eyeKind == EyeKind.Outer ? eyeOuterMask : eyeKind == EyeKind.Inner ? eyeInnerMask : mask;
                var useMaskSolid = eyeKind == EyeKind.Outer ? eyeOuterSolid : eyeKind == EyeKind.Inner ? eyeInnerSolid : maskSolid;
                var useColor = eyeKind switch {
                    EyeKind.Outer => opts.Eyes?.OuterColor ?? opts.Foreground,
                    EyeKind.Inner => opts.Eyes?.InnerColor ?? opts.Foreground,
                    _ => opts.Foreground,
                };
                PaletteInfo? palette = null;
                if (zoneInfo.HasValue && zoneInfo.Value.TryGetPalette(mx, my, out var zonePalette)) {
                    if (eyeKind == EyeKind.None || zonePalette.ApplyToEyes) {
                        palette = zonePalette;
                    }
                }
                if (!palette.HasValue && paletteInfo.HasValue) {
                    if (eyeKind == EyeKind.None || paletteInfo.Value.ApplyToEyes) {
                        palette = paletteInfo;
                    }
                }
                var usePalette = palette.HasValue;
                if (usePalette) {
                    useColor = GetPaletteColor(palette!.Value, mx, my);
                }

                if (scaleMapInfo.HasValue && (eyeKind == EyeKind.None || (scaleMapInfo.Value.ApplyToEyes && opts.Eyes is null))) {
                    var scale = ClampScale(opts.ModuleScale * GetScaleFactor(scaleMapInfo.Value, mx, my));
                    var maskInfo = GetScaleMask(scaleMaskCache!, opts.ModuleSize, opts.ModuleShape, scale, opts.ModuleCornerRadiusPx);
                    useMask = maskInfo.Mask;
                    useMaskSolid = maskInfo.IsSolid;
                }

                if (!useFrame && eyeKind != EyeKind.None) {
                    var eyeGrad = eyeKind == EyeKind.Outer ? eyeOuterGradientInfo : eyeInnerGradientInfo;
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
                            useMask,
                            boxX,
                            boxY);
                        continue;
                    }
                }

                var useGradient = !usePalette && eyeKind == EyeKind.None ? gradientInfo : null;
                if (useGradient is null && useMaskSolid) {
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
                    useMask,
                    qrOriginX,
                    qrOriginY,
                    qrSizePx);
            }
        }

        if (useFrame && opts.Eyes is not null) {
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, 0, 0);
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, size - 7, 0);
            DrawEyeFrame(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, 0, size - 7);
        }

        if (opts.Logo is not null) {
            ApplyLogo(buffer, widthPx, heightPx, stride, qrOriginX, qrOriginY, qrSizePx, opts.Logo);
        }

        if (opts.Debug is not null && opts.Debug.HasOverlay) {
            DrawDebugOverlay(buffer, widthPx, heightPx, stride, opts, qrOffsetX, qrOffsetY, qrOriginX, qrOriginY, qrFullPx, qrSizePx, size);
        }

        return buffer;
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
        bool[] mask,
        int boxX,
        int boxY) {
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
                var color = GetGradientColorInBox(gradient, x0 + sx, y0 + sy, boxX, boxY);
                scanlines[rowStart + 0] = color.R;
                scanlines[rowStart + 1] = color.G;
                scanlines[rowStart + 2] = color.B;
                scanlines[rowStart + 3] = color.A;
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
            DrawCanvasPattern(scanlines, widthPx, heightPx, stride, x, y, w, h, moduleSize, radius, canvas.Pattern);
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
            DrawCanvasPattern(scanlines, widthPx, heightPx, stride, qrOffsetX, qrOffsetY, qrFullPx, qrFullPx, opts.ModuleSize, 0, opts.BackgroundPattern);
        }
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
        QrPngBackgroundPatternOptions pattern) {
        if (pattern.Color.A == 0) return;
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

        for (var py = y; py < y1; py++) {
            var localY = py - y;
            for (var px = x; px < x1; px++) {
                if (r > 0 && !InsideRounded(px, py, x, y, x1 - 1, y1 - 1, r, r2)) continue;

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
        var inv = 255 - sa;
        scanlines[rowStart + 0] = (byte)((color.R * sa + dr * inv + 127) / 255);
        scanlines[rowStart + 1] = (byte)((color.G * sa + dg * inv + 127) / 255);
        scanlines[rowStart + 2] = (byte)((color.B * sa + db * inv + 127) / 255);
        scanlines[rowStart + 3] = (byte)((sa + da * inv + 127) / 255);
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
                var dist = Math.Sqrt(dx * dx + dy * dy);
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

    private static bool[] BuildModuleMask(
        int moduleSize,
        QrPngModuleShape shape,
        double scale,
        int cornerRadiusPx) {
        var mask = new bool[moduleSize * moduleSize];
        if (moduleSize <= 0) return mask;

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
            MaxDist = Math.Sqrt(MaxRing * MaxRing + MaxRing * MaxRing);
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

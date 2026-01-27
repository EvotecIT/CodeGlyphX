using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static bool TryGetEye(int x, int y, int size, out int ex, out int ey, out EyeKind kind) {
        if (IsInEye(x, y, 0, 0)) {
            ex = 0;
            ey = 0;
            kind = GetEyeModuleKind(x, y, 0, 0);
            return true;
        }
        if (IsInEye(x, y, size - 7, 0)) {
            ex = size - 7;
            ey = 0;
            kind = GetEyeModuleKind(x, y, size - 7, 0);
            return true;
        }
        if (IsInEye(x, y, 0, size - 7)) {
            ex = 0;
            ey = size - 7;
            kind = GetEyeModuleKind(x, y, 0, size - 7);
            return true;
        }
        ex = 0;
        ey = 0;
        kind = EyeKind.None;
        return false;
    }

    private static bool IsInEye(int x, int y, int ex, int ey) {
        return x >= ex && x < ex + 7 && y >= ey && y < ey + 7;
    }

    private static EyeKind GetEyeModuleKind(int x, int y, int ex, int ey) {
        var lx = x - ex;
        var ly = y - ey;
        if (lx >= 2 && lx <= 4 && ly >= 2 && ly <= 4) return EyeKind.Inner;
        return EyeKind.Outer;
    }

    private static void DrawEyeFrame(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int offsetX,
        int offsetY,
        int qrOriginX,
        int qrOriginY,
        int qrSizePx,
        int ex,
        int ey) {
        var moduleSize = opts.ModuleSize;
        var x0 = offsetX + (ex + opts.QuietZone) * moduleSize;
        var y0 = offsetY + (ey + opts.QuietZone) * moduleSize;

        var outerSize = 7 * moduleSize;
        var innerSize = 5 * moduleSize;
        var dotSize = 3 * moduleSize;

        var eye = opts.Eyes!;
        var outerScaled = ScaleSize(outerSize, eye.OuterScale);
        var innerScaled = ScaleSize(innerSize, eye.OuterScale);
        var innerRingScaled = ScaleSize(innerSize, eye.InnerScale);
        var dotScaled = ScaleSize(dotSize, eye.InnerScale);

        var outerX = x0 + (outerSize - outerScaled) / 2;
        var outerY = y0 + (outerSize - outerScaled) / 2;
        var innerX = x0 + (outerSize - innerScaled) / 2;
        var innerY = y0 + (outerSize - innerScaled) / 2;
        var innerRingX = x0 + (outerSize - innerRingScaled) / 2;
        var innerRingY = y0 + (outerSize - innerRingScaled) / 2;
        var dotX = x0 + (outerSize - dotScaled) / 2;
        var dotY = y0 + (outerSize - dotScaled) / 2;

        var outerColor = eye.OuterColor ?? opts.Foreground;
        var innerColor = eye.InnerColor ?? opts.Foreground;
        var outerGradient = eye.OuterGradient;
        var innerGradient = eye.InnerGradient;
        var qrX0 = qrOriginX;
        var qrY0 = qrOriginY;
        var qrX1 = qrOriginX + qrSizePx;
        var qrY1 = qrOriginY + qrSizePx;

        switch (eye.FrameStyle) {
            case QrPngEyeFrameStyle.Glow:
                var glowRadiusPx = eye.GlowRadiusPx > 0 ? eye.GlowRadiusPx : Math.Max(moduleSize * 2, moduleSize + 2);
                var glowBase = eye.GlowColor ?? outerColor;
                var glowAlpha = (int)eye.GlowAlpha;
                if (glowBase.A < 255) {
                    glowAlpha = glowAlpha * glowBase.A / 255;
                }
                if (glowRadiusPx > 0 && glowAlpha > 0) {
                    var glowColor = new Rgba32(glowBase.R, glowBase.G, glowBase.B, (byte)Math.Min(255, glowAlpha));
                    DrawEyeGlow(
                        scanlines,
                        widthPx,
                        heightPx,
                        stride,
                        outerX,
                        outerY,
                        outerScaled,
                        glowRadiusPx,
                        glowColor,
                        qrX0,
                        qrY0,
                        qrX1,
                        qrY1);
                }
                goto default;
            case QrPngEyeFrameStyle.InsetRing:
                if (outerGradient is null) {
                    FillShape(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerColor, eye.OuterShape, eye.OuterCornerRadiusPx);
                } else {
                    FillShapeGradient(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerGradient, eye.OuterShape, eye.OuterCornerRadiusPx);
                }
                FillShape(scanlines, widthPx, heightPx, stride, innerX, innerY, innerScaled, innerScaled, opts.Background, eye.OuterShape, eye.InnerCornerRadiusPx);

                var insetPadding = Math.Max(1, (int)Math.Round(moduleSize * 0.8));
                var insetSize = Math.Max(1, innerScaled - insetPadding * 2);
                var insetX = innerX + (innerScaled - insetSize) / 2;
                var insetY = innerY + (innerScaled - insetSize) / 2;
                if (insetSize > 0) {
                    if (innerGradient is null) {
                        FillShape(scanlines, widthPx, heightPx, stride, insetX, insetY, insetSize, insetSize, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
                    } else {
                        FillShapeGradient(scanlines, widthPx, heightPx, stride, insetX, insetY, insetSize, insetSize, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
                    }
                }

                var maxHoleSize = insetSize - 2;
                if (maxHoleSize > 0) {
                    var holeSize = Math.Max(1, Math.Min(dotScaled, maxHoleSize));
                    var holeX = x0 + (outerSize - holeSize) / 2;
                    var holeY = y0 + (outerSize - holeSize) / 2;
                    FillShape(scanlines, widthPx, heightPx, stride, holeX, holeY, holeSize, holeSize, opts.Background, eye.InnerShape, eye.InnerCornerRadiusPx);
                }
                break;
            case QrPngEyeFrameStyle.CutCorner:
                if (outerGradient is null) {
                    FillShape(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerColor, eye.OuterShape, eye.OuterCornerRadiusPx);
                } else {
                    FillShapeGradient(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerGradient, eye.OuterShape, eye.OuterCornerRadiusPx);
                }
                FillShape(scanlines, widthPx, heightPx, stride, innerX, innerY, innerScaled, innerScaled, opts.Background, eye.OuterShape, eye.InnerCornerRadiusPx);

                if (dotScaled > 0) {
                    if (innerGradient is null) {
                        FillShape(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
                    } else {
                        FillShapeGradient(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
                    }
                }

                var cutSize = Math.Max(1, (int)Math.Round(moduleSize * 1.6));
                cutSize = Math.Min(cutSize, Math.Max(1, outerScaled / 3));
                CutCorners(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, cutSize, opts.Background);
                break;
            case QrPngEyeFrameStyle.DoubleRing:
            case QrPngEyeFrameStyle.Target:
                if (outerGradient is null) {
                    FillShape(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerColor, eye.OuterShape, eye.OuterCornerRadiusPx);
                } else {
                    FillShapeGradient(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerGradient, eye.OuterShape, eye.OuterCornerRadiusPx);
                }
                FillShape(scanlines, widthPx, heightPx, stride, innerX, innerY, innerScaled, innerScaled, opts.Background, eye.OuterShape, eye.InnerCornerRadiusPx);

                if (innerRingScaled > 0) {
                    if (innerGradient is null) {
                        FillShape(scanlines, widthPx, heightPx, stride, innerRingX, innerRingY, innerRingScaled, innerRingScaled, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
                    } else {
                        FillShapeGradient(scanlines, widthPx, heightPx, stride, innerRingX, innerRingY, innerRingScaled, innerRingScaled, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
                    }
                }

                if (dotScaled > 0) {
                    FillShape(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, opts.Background, eye.InnerShape, eye.InnerCornerRadiusPx);
                }

                if (eye.FrameStyle == QrPngEyeFrameStyle.Target && dotScaled > 0) {
                    if (innerGradient is null) {
                        FillShape(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
                    } else {
                        FillShapeGradient(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
                    }
                }
                break;
            case QrPngEyeFrameStyle.Bracket:
                DrawBracketFrame(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerColor, eye.OuterCornerRadiusPx, eye.OuterScale);
                if (dotScaled > 0) {
                    if (innerGradient is null) {
                        FillShape(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
                    } else {
                        FillShapeGradient(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
                    }
                }
                break;
            case QrPngEyeFrameStyle.Badge:
                if (outerGradient is null) {
                    FillShape(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerColor, eye.OuterShape, eye.OuterCornerRadiusPx);
                } else {
                    FillShapeGradient(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerGradient, eye.OuterShape, eye.OuterCornerRadiusPx);
                }
                var badgeInset = Math.Max(1, (int)Math.Round(moduleSize * 0.6));
                var badgeX = outerX + badgeInset;
                var badgeY = outerY + badgeInset;
                var badgeW = Math.Max(1, outerScaled - badgeInset * 2);
                var badgeH = Math.Max(1, outerScaled - badgeInset * 2);
                FillShape(scanlines, widthPx, heightPx, stride, badgeX, badgeY, badgeW, badgeH, opts.Background, eye.OuterShape, Math.Max(0, eye.InnerCornerRadiusPx));
                if (dotScaled > 0) {
                    if (innerGradient is null) {
                        FillShape(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
                    } else {
                        FillShapeGradient(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
                    }
                }
                break;
            default:
                if (outerGradient is null) {
                    FillShape(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerColor, eye.OuterShape, eye.OuterCornerRadiusPx);
                } else {
                    FillShapeGradient(scanlines, widthPx, heightPx, stride, outerX, outerY, outerScaled, outerScaled, outerGradient, eye.OuterShape, eye.OuterCornerRadiusPx);
                }
                FillShape(scanlines, widthPx, heightPx, stride, innerX, innerY, innerScaled, innerScaled, opts.Background, eye.OuterShape, eye.InnerCornerRadiusPx);

                if (dotScaled > 0) {
                    if (innerGradient is null) {
                        FillShape(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerColor, eye.InnerShape, eye.InnerCornerRadiusPx);
                    } else {
                        FillShapeGradient(scanlines, widthPx, heightPx, stride, dotX, dotY, dotScaled, dotScaled, innerGradient, eye.InnerShape, eye.InnerCornerRadiusPx);
                    }
                }
                break;
        }
    }

    private static void DrawEyeGlow(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int size,
        int glowRadiusPx,
        Rgba32 glowColor,
        int qrX0,
        int qrY0,
        int qrX1,
        int qrY1) {
        if (size <= 0 || glowRadiusPx <= 0 || glowColor.A == 0) return;

        var rectX0 = x;
        var rectY0 = y;
        var rectX1 = x + size - 1;
        var rectY1 = y + size - 1;

        var minX = Math.Max(qrX0, rectX0 - glowRadiusPx);
        var minY = Math.Max(qrY0, rectY0 - glowRadiusPx);
        var maxX = Math.Min(qrX1 - 1, rectX1 + glowRadiusPx);
        var maxY = Math.Min(qrY1 - 1, rectY1 + glowRadiusPx);
        if (minX > maxX || minY > maxY) return;

        minX = Math.Max(0, minX);
        minY = Math.Max(0, minY);
        maxX = Math.Min(widthPx - 1, maxX);
        maxY = Math.Min(heightPx - 1, maxY);
        if (minX > maxX || minY > maxY) return;

        for (var py = minY; py <= maxY; py++) {
            for (var px = minX; px <= maxX; px++) {
                var cx = px < rectX0 ? rectX0 : px > rectX1 ? rectX1 : px;
                var cy = py < rectY0 ? rectY0 : py > rectY1 ? rectY1 : py;
                var dx = px - cx;
                var dy = py - cy;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > glowRadiusPx) continue;

                var t = 1.0 - dist / glowRadiusPx;
                var a = (int)Math.Round(glowColor.A * t);
                if (a <= 0) continue;

                BlendPixel(scanlines, stride, px, py, new Rgba32(glowColor.R, glowColor.G, glowColor.B, (byte)Math.Min(255, a)));
            }
        }
    }

    private static void CutCorners(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int size,
        int cutSize,
        Rgba32 background) {
        if (size <= 0 || cutSize <= 0) return;
        var cut = Math.Min(cutSize, size / 2);
        if (cut <= 0) return;

        for (var dy = 0; dy < cut; dy++) {
            var rowLimit = cut - dy;
            for (var dx = 0; dx < rowLimit; dx++) {
                var tlX = x + dx;
                var tlY = y + dy;
                var trX = x + size - 1 - dx;
                var trY = y + dy;
                var blX = x + dx;
                var blY = y + size - 1 - dy;
                var brX = x + size - 1 - dx;
                var brY = y + size - 1 - dy;

                if ((uint)tlX < (uint)widthPx && (uint)tlY < (uint)heightPx) BlendPixel(scanlines, stride, tlX, tlY, background);
                if ((uint)trX < (uint)widthPx && (uint)trY < (uint)heightPx) BlendPixel(scanlines, stride, trX, trY, background);
                if ((uint)blX < (uint)widthPx && (uint)blY < (uint)heightPx) BlendPixel(scanlines, stride, blX, blY, background);
                if ((uint)brX < (uint)widthPx && (uint)brY < (uint)heightPx) BlendPixel(scanlines, stride, brX, brY, background);
            }
        }
    }

    private static void DrawBracketFrame(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color,
        int radius,
        double scale) {
        var thickness = Math.Max(1, (int)Math.Round(w * Math.Min(scale, 0.35)));
        if (thickness > w / 2) thickness = w / 2;

        // Top-left
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y, thickness, h / 2, color, radius);
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y, w / 2, thickness, color, radius);
        // Top-right
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x + w - thickness, y, thickness, h / 2, color, radius);
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x + w / 2, y, w / 2, thickness, color, radius);
        // Bottom-left
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y + h / 2, thickness, h / 2, color, radius);
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y + h - thickness, w / 2, thickness, color, radius);
        // Bottom-right
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x + w - thickness, y + h / 2, thickness, h / 2, color, radius);
        FillRoundedRect(scanlines, widthPx, heightPx, stride, x + w / 2, y + h - thickness, w / 2, thickness, color, radius);
    }

    private static void FillShape(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color,
        QrPngModuleShape shape,
        int radius) {
        switch (shape) {
            case QrPngModuleShape.Circle:
                FillEllipse(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.Rounded:
            case QrPngModuleShape.ConnectedRounded:
                FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y, w, h, color, radius);
                return;
            case QrPngModuleShape.Diamond:
                FillDiamond(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.SoftDiamond:
            case QrPngModuleShape.Leaf:
            case QrPngModuleShape.Wave:
            case QrPngModuleShape.Blob:
                FillMaskShape(scanlines, widthPx, heightPx, stride, x, y, w, h, color, shape, radius);
                return;
            case QrPngModuleShape.Squircle:
            case QrPngModuleShape.ConnectedSquircle:
                FillSquircle(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.Dot:
                FillDot(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            case QrPngModuleShape.DotGrid:
                FillDotGrid(scanlines, widthPx, heightPx, stride, x, y, w, h, color);
                return;
            default:
                FillRoundedRect(scanlines, widthPx, heightPx, stride, x, y, w, h, color, 0);
                return;
        }
    }

    private static void FillShapeGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient,
        QrPngModuleShape shape,
        int radius) {
        switch (shape) {
            case QrPngModuleShape.Circle:
                FillEllipseGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.Rounded:
            case QrPngModuleShape.ConnectedRounded:
                FillRoundedRectGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient, radius);
                return;
            case QrPngModuleShape.Diamond:
                FillDiamondGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.SoftDiamond:
            case QrPngModuleShape.Leaf:
            case QrPngModuleShape.Wave:
            case QrPngModuleShape.Blob:
                FillMaskShapeGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient, shape, radius);
                return;
            case QrPngModuleShape.Squircle:
            case QrPngModuleShape.ConnectedSquircle:
                FillSquircleGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.Dot:
                FillDotGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            case QrPngModuleShape.DotGrid:
                FillDotGridGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient);
                return;
            default:
                FillRoundedRectGradient(scanlines, widthPx, heightPx, stride, x, y, w, h, gradient, 0);
                return;
        }
    }

    private static void FillMaskShape(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color,
        QrPngModuleShape shape,
        int radius) {
        var size = Math.Min(w, h);
        if (size <= 0) return;
        var offsetX = x + (w - size) / 2;
        var offsetY = y + (h - size) / 2;

        var x0 = Math.Max(0, offsetX);
        var y0 = Math.Max(0, offsetY);
        var x1 = Math.Min(widthPx, offsetX + size);
        var y1 = Math.Min(heightPx, offsetY + size);
        if (x1 <= x0 || y1 <= y0) return;

        var mask = BuildModuleMask(size, shape, 1.0, radius);
        var rowStride = stride + 1;

        for (var py = y0; py < y1; py++) {
            var my = py - offsetY;
            var maskRow = my * size;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var mx = px - offsetX;
                if (!mask[maskRow + mx]) continue;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillMaskShapeGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient,
        QrPngModuleShape shape,
        int radius) {
        var size = Math.Min(w, h);
        if (size <= 0) return;
        var offsetX = x + (w - size) / 2;
        var offsetY = y + (h - size) / 2;

        var x0 = Math.Max(0, offsetX);
        var y0 = Math.Max(0, offsetY);
        var x1 = Math.Min(widthPx, offsetX + size);
        var y1 = Math.Min(heightPx, offsetY + size);
        if (x1 <= x0 || y1 <= y0) return;

        var mask = BuildModuleMask(size, shape, 1.0, radius);
        var rowStride = stride + 1;
        var gradientInfo = new GradientInfo(gradient, size - 1, size - 1);

        for (var py = y0; py < y1; py++) {
            var my = py - offsetY;
            var maskRow = my * size;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var mx = px - offsetX;
                if (!mask[maskRow + mx]) continue;
                var color = GetGradientColorInBox(gradientInfo, px, py, offsetX, offsetY);
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillEllipse(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;
        var rowStride = stride + 1;

        for (var py = y0; py < y1; py++) {
            var dy = (py + 0.5 - cy) / ry;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var dx = (px + 0.5 - cx) / rx;
                if (dx * dx + dy * dy > 1.0) continue;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillDiamond(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;
        var rowStride = stride + 1;

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                if (dx + dy > 1.0) continue;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillEllipseGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;
        var rowStride = stride + 1;
        var gradientInfo = new GradientInfo(gradient, x1 - x0 - 1, y1 - y0 - 1);

        for (var py = y0; py < y1; py++) {
            var dy = (py + 0.5 - cy) / ry;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var dx = (px + 0.5 - cx) / rx;
                if (dx * dx + dy * dy > 1.0) continue;
                var color = GetGradientColorInBox(gradientInfo, px, py, x0, y0);
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillDot(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        var size = Math.Min(w, h);
        var dotSize = (int)Math.Round(size * QrPngShapeDefaults.DotScale);
        if (dotSize <= 0) return;
        var insetX = (w - dotSize) / 2;
        var insetY = (h - dotSize) / 2;
        FillEllipse(scanlines, widthPx, heightPx, stride, x + insetX, y + insetY, dotSize, dotSize, color);
    }

    private static void FillDotGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        var size = Math.Min(w, h);
        var dotSize = (int)Math.Round(size * QrPngShapeDefaults.DotScale);
        if (dotSize <= 0) return;
        var insetX = (w - dotSize) / 2;
        var insetY = (h - dotSize) / 2;
        FillEllipseGradient(scanlines, widthPx, heightPx, stride, x + insetX, y + insetY, dotSize, dotSize, gradient);
    }

    private static void FillDiamondGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;
        var rowStride = stride + 1;
        var gradientInfo = new GradientInfo(gradient, x1 - x0 - 1, y1 - y0 - 1);

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                if (dx + dy > 1.0) continue;
                var color = GetGradientColorInBox(gradientInfo, px, py, x0, y0);
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillSquircle(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;
        var rowStride = stride + 1;

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            var dy2 = dy * dy;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                var dx2 = dx * dx;
                if (dx2 * dx2 + dy2 * dy2 > 1.0) continue;
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillSquircleGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var rx = w / 2.0;
        var ry = h / 2.0;
        if (rx <= 0 || ry <= 0) return;
        var cx = x + rx;
        var cy = y + ry;
        var rowStride = stride + 1;
        var gradientInfo = new GradientInfo(gradient, x1 - x0 - 1, y1 - y0 - 1);

        for (var py = y0; py < y1; py++) {
            var dy = Math.Abs(py + 0.5 - cy) / ry;
            var dy2 = dy * dy;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var dx = Math.Abs(px + 0.5 - cx) / rx;
                var dx2 = dx * dx;
                if (dx2 * dx2 + dy2 * dy2 > 1.0) continue;
                var color = GetGradientColorInBox(gradientInfo, px, py, x0, y0);
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillDotGrid(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color) {
        var size = Math.Min(w, h);
        var gridSize = size * QrPngShapeDefaults.DotGridScale;
        if (gridSize <= 0) return;
        var insetX = (w - gridSize) / 2.0;
        var insetY = (h - gridSize) / 2.0;
        var baseX = x + insetX;
        var baseY = y + insetY;

        var radius = Math.Max(QrPngShapeDefaults.DotGridMinRadius, gridSize * QrPngShapeDefaults.DotGridRadiusFactor);
        var c0 = QrPngShapeDefaults.DotGridCenterFactor;
        var c1 = 1.0 - QrPngShapeDefaults.DotGridCenterFactor;

        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c0 - radius), (int)Math.Round(baseY + gridSize * c0 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c1 - radius), (int)Math.Round(baseY + gridSize * c0 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c0 - radius), (int)Math.Round(baseY + gridSize * c1 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
        FillEllipse(scanlines, widthPx, heightPx, stride, (int)Math.Round(baseX + gridSize * c1 - radius), (int)Math.Round(baseY + gridSize * c1 - radius), (int)Math.Round(radius * 2), (int)Math.Round(radius * 2), color);
    }

    private static void FillDotGridGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient) {
        if (w <= 0 || h <= 0) return;
        var size = Math.Min(w, h);
        var gridSize = size * QrPngShapeDefaults.DotGridScale;
        if (gridSize <= 0) return;
        var insetX = (w - gridSize) / 2.0;
        var insetY = (h - gridSize) / 2.0;
        var baseX = x + insetX;
        var baseY = y + insetY;

        var radius = Math.Max(QrPngShapeDefaults.DotGridMinRadius, gridSize * QrPngShapeDefaults.DotGridRadiusFactor);
        var r2 = radius * radius;
        var c0 = gridSize * QrPngShapeDefaults.DotGridCenterFactor;
        var c1 = gridSize * (1.0 - QrPngShapeDefaults.DotGridCenterFactor);

        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        var rowStride = stride + 1;
        var gradientInfo = new GradientInfo(gradient, x1 - x0 - 1, y1 - y0 - 1);

        for (var py = y0; py < y1; py++) {
            var localY = py - baseY;
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                var localX = px - baseX;
                var inside = InsideCircleLocal(localX, localY, c0, r2) ||
                             InsideCircleLocal(localX, localY, c1, r2) ||
                             InsideCircleLocal(localX, localY, c0, r2, c1) ||
                             InsideCircleLocal(localX, localY, c1, r2, c0);
                if (!inside) continue;
                var color = GetGradientColorInBox(gradientInfo, px, py, x0, y0);
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static bool InsideCircleLocal(double x, double y, double center, double radiusSq) {
        var dx = x - center;
        var dy = y - center;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static bool InsideCircleLocal(double x, double y, double cx, double radiusSq, double cy) {
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= radiusSq;
    }

    private static int ScaleSize(int size, double scale) {
        if (scale <= 0) return 0;
        var scaled = (int)Math.Round(size * scale);
        if (scaled < 1) scaled = 1;
        return scaled;
    }

    private static void ApplyLogo(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int originX,
        int originY,
        int qrSizePx,
        QrPngLogoOptions logo) {
        if (logo.Rgba.Length == 0) return;
        if (logo.Scale <= 0) return;

        var maxLogoPx = (int)Math.Round(qrSizePx * logo.Scale);
        if (maxLogoPx <= 0) return;
        if (maxLogoPx > qrSizePx) maxLogoPx = qrSizePx;

        var scale = Math.Min(maxLogoPx / (double)logo.Width, maxLogoPx / (double)logo.Height);
        if (scale <= 0) return;

        var targetW = Math.Max(1, (int)Math.Round(logo.Width * scale));
        var targetH = Math.Max(1, (int)Math.Round(logo.Height * scale));
        if (targetW <= 0 || targetH <= 0) return;

        var x0 = originX + (qrSizePx - targetW) / 2;
        var y0 = originY + (qrSizePx - targetH) / 2;

        if (logo.DrawBackground) {
            var pad = Math.Max(0, logo.PaddingPx);
            var bgX = x0 - pad;
            var bgY = y0 - pad;
            var bgW = targetW + pad * 2;
            var bgH = targetH + pad * 2;
            FillRoundedRect(scanlines, widthPx, heightPx, stride, bgX, bgY, bgW, bgH, logo.Background, logo.CornerRadiusPx);
        }

        BlitLogo(scanlines, widthPx, heightPx, stride, x0, y0, targetW, targetH, logo);
    }

    private static void FillRoundedRect(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        Rgba32 color,
        int radius) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var r = Math.Max(0, radius);
        var maxR = Math.Min((x1 - x0) / 2, (y1 - y0) / 2);
        if (r > maxR) r = maxR;
        var r2 = r * r;
        var rowStride = stride + 1;

        for (var py = y0; py < y1; py++) {
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                if (r > 0 && !InsideRounded(px, py, x0, y0, x1 - 1, y1 - 1, r, r2)) {
                    continue;
                }
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static void FillRoundedRectGradient(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x,
        int y,
        int w,
        int h,
        QrPngGradientOptions gradient,
        int radius) {
        if (w <= 0 || h <= 0) return;
        var x0 = Math.Max(0, x);
        var y0 = Math.Max(0, y);
        var x1 = Math.Min(widthPx, x + w);
        var y1 = Math.Min(heightPx, y + h);
        if (x1 <= x0 || y1 <= y0) return;

        var r = Math.Max(0, radius);
        var maxR = Math.Min((x1 - x0) / 2, (y1 - y0) / 2);
        if (r > maxR) r = maxR;
        var r2 = r * r;
        var rowStride = stride + 1;
        var gradientInfo = new GradientInfo(gradient, x1 - x0 - 1, y1 - y0 - 1);

        for (var py = y0; py < y1; py++) {
            var p = py * rowStride + 1 + x0 * 4;
            for (var px = x0; px < x1; px++, p += 4) {
                if (r > 0 && !InsideRounded(px, py, x0, y0, x1 - 1, y1 - 1, r, r2)) {
                    continue;
                }
                var color = GetGradientColorInBox(gradientInfo, px, py, x0, y0);
                scanlines[p + 0] = color.R;
                scanlines[p + 1] = color.G;
                scanlines[p + 2] = color.B;
                scanlines[p + 3] = color.A;
            }
        }
    }

    private static bool InsideRounded(int px, int py, int x0, int y0, int x1, int y1, int r, int r2) {
        if (px >= x0 + r && px <= x1 - r) return true;
        if (py >= y0 + r && py <= y1 - r) return true;

        var cx = px < x0 + r ? x0 + r - 1 : x1 - r + 1;
        var cy = py < y0 + r ? y0 + r - 1 : y1 - r + 1;
        var dx = px - cx;
        var dy = py - cy;
        return dx * dx + dy * dy <= r2;
    }

    private static void BlitLogo(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        int x0,
        int y0,
        int targetW,
        int targetH,
        QrPngLogoOptions logo) {
        var x1 = Math.Min(widthPx, x0 + targetW);
        var y1 = Math.Min(heightPx, y0 + targetH);
        if (x1 <= x0 || y1 <= y0) return;

        for (var y = y0; y < y1; y++) {
            var ty = (y - y0) * logo.Height / targetH;
            var srcRow = ty * logo.Width * 4;
            for (var x = x0; x < x1; x++) {
                var tx = (x - x0) * logo.Width / targetW;
                var src = srcRow + tx * 4;

                var sr = logo.Rgba[src + 0];
                var sg = logo.Rgba[src + 1];
                var sb = logo.Rgba[src + 2];
                var sa = logo.Rgba[src + 3];
                if (sa == 0) continue;

                var dst = y * (stride + 1) + 1 + x * 4;
                if (sa == 255) {
                    scanlines[dst + 0] = sr;
                    scanlines[dst + 1] = sg;
                    scanlines[dst + 2] = sb;
                    scanlines[dst + 3] = 255;
                    continue;
                }

                var dr = scanlines[dst + 0];
                var dg = scanlines[dst + 1];
                var db = scanlines[dst + 2];
                var da = scanlines[dst + 3];

                var inv = 255 - sa;
                scanlines[dst + 0] = (byte)((sr * sa + dr * inv + 127) / 255);
                scanlines[dst + 1] = (byte)((sg * sa + dg * inv + 127) / 255);
                scanlines[dst + 2] = (byte)((sb * sa + db * inv + 127) / 255);
                scanlines[dst + 3] = (byte)((sa + da * inv + 127) / 255);
            }
        }
    }

    private static void DrawDebugOverlay(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int qrOffsetX,
        int qrOffsetY,
        int qrOriginX,
        int qrOriginY,
        int qrFullPx,
        int qrSizePx,
        int moduleCount) {
        var debug = opts.Debug;
        if (debug is null || !debug.HasOverlay) return;

        var stroke = debug.StrokePx;

        if (debug.ShowQuietZone) {
            DrawRectOutline(scanlines, widthPx, heightPx, stride, qrOffsetX, qrOffsetY, qrFullPx, qrFullPx, debug.QuietZoneColor, stroke);
        }

        if (debug.ShowQrBounds) {
            DrawRectOutline(scanlines, widthPx, heightPx, stride, qrOriginX, qrOriginY, qrSizePx, qrSizePx, debug.QrBoundsColor, stroke);
        }

        if (debug.ShowEyeBounds) {
            var eyeSize = 7 * opts.ModuleSize;
            var rightX = qrOriginX + (moduleCount - 7) * opts.ModuleSize;
            var bottomY = qrOriginY + (moduleCount - 7) * opts.ModuleSize;
            DrawRectOutline(scanlines, widthPx, heightPx, stride, qrOriginX, qrOriginY, eyeSize, eyeSize, debug.EyeBoundsColor, stroke);
            DrawRectOutline(scanlines, widthPx, heightPx, stride, rightX, qrOriginY, eyeSize, eyeSize, debug.EyeBoundsColor, stroke);
            DrawRectOutline(scanlines, widthPx, heightPx, stride, qrOriginX, bottomY, eyeSize, eyeSize, debug.EyeBoundsColor, stroke);
        }

        if (debug.ShowLogoBounds
            && opts.Logo is not null
            && TryGetLogoBounds(opts.Logo, qrOriginX, qrOriginY, qrSizePx, out var x, out var y, out var w, out var h)) {
            DrawRectOutline(scanlines, widthPx, heightPx, stride, x, y, w, h, debug.LogoBoundsColor, stroke);
        }
    }

    private static bool TryGetLogoBounds(QrPngLogoOptions logo, int originX, int originY, int qrSizePx, out int x, out int y, out int w, out int h) {
        x = 0;
        y = 0;
        w = 0;
        h = 0;

        if (logo.Rgba.Length == 0) return false;
        if (logo.Scale <= 0) return false;

        var maxLogoPx = (int)Math.Round(qrSizePx * logo.Scale);
        if (maxLogoPx <= 0) return false;
        if (maxLogoPx > qrSizePx) maxLogoPx = qrSizePx;

        var scale = Math.Min(maxLogoPx / (double)logo.Width, maxLogoPx / (double)logo.Height);
        if (scale <= 0) return false;

        var targetW = Math.Max(1, (int)Math.Round(logo.Width * scale));
        var targetH = Math.Max(1, (int)Math.Round(logo.Height * scale));
        if (targetW <= 0 || targetH <= 0) return false;

        var x0 = originX + (qrSizePx - targetW) / 2;
        var y0 = originY + (qrSizePx - targetH) / 2;

        var pad = logo.DrawBackground ? Math.Max(0, logo.PaddingPx) : 0;
        x = x0 - pad;
        y = y0 - pad;
        w = targetW + pad * 2;
        h = targetH + pad * 2;
        return w > 0 && h > 0;
    }

    private static void DrawRectOutline(byte[] scanlines, int widthPx, int heightPx, int stride, int x, int y, int w, int h, Rgba32 color, int stroke) {
        if (w <= 0 || h <= 0) return;
        var s = Math.Max(1, stroke);
        for (var t = 0; t < s; t++) {
            var xt = x + t;
            var yt = y + t;
            var wt = w - t * 2;
            var ht = h - t * 2;
            if (wt <= 0 || ht <= 0) break;

            var yTop = yt;
            var yBottom = yt + ht - 1;
            var xLeft = xt;
            var xRight = xt + wt - 1;

            DrawHLine(scanlines, widthPx, heightPx, stride, xLeft, xRight, yTop, color);
            if (yBottom != yTop) {
                DrawHLine(scanlines, widthPx, heightPx, stride, xLeft, xRight, yBottom, color);
            }
            DrawVLine(scanlines, widthPx, heightPx, stride, xLeft, yTop, yBottom, color);
            if (xRight != xLeft) {
                DrawVLine(scanlines, widthPx, heightPx, stride, xRight, yTop, yBottom, color);
            }
        }
    }

    private static void DrawHLine(byte[] scanlines, int widthPx, int heightPx, int stride, int x0, int x1, int y, Rgba32 color) {
        if (y < 0 || y >= heightPx) return;
        if (x0 > x1) (x0, x1) = (x1, x0);
        if (x1 < 0 || x0 >= widthPx) return;
        var start = Math.Max(0, x0);
        var end = Math.Min(widthPx - 1, x1);
        var row = y * (stride + 1) + 1 + start * 4;
        for (var x = start; x <= end; x++) {
            scanlines[row + 0] = color.R;
            scanlines[row + 1] = color.G;
            scanlines[row + 2] = color.B;
            scanlines[row + 3] = color.A;
            row += 4;
        }
    }

    private static void DrawVLine(byte[] scanlines, int widthPx, int heightPx, int stride, int x, int y0, int y1, Rgba32 color) {
        if (x < 0 || x >= widthPx) return;
        if (y0 > y1) (y0, y1) = (y1, y0);
        if (y1 < 0 || y0 >= heightPx) return;
        var start = Math.Max(0, y0);
        var end = Math.Min(heightPx - 1, y1);
        for (var y = start; y <= end; y++) {
            var row = y * (stride + 1) + 1 + x * 4;
            scanlines[row + 0] = color.R;
            scanlines[row + 1] = color.G;
            scanlines[row + 2] = color.B;
            scanlines[row + 3] = color.A;
        }
    }

    private static void RenderBackgroundSupersampled(
        byte[] scanlines,
        int widthPx,
        int heightPx,
        int stride,
        QrPngRenderOptions opts,
        int qrOffsetX,
        int qrOffsetY,
        int qrFullPx) {
        var scale = Math.Max(1, opts.BackgroundSupersample);
        if (scale <= 1) return;

        var scaledWidthLong = (long)widthPx * scale;
        var scaledHeightLong = (long)heightPx * scale;
        if (scaledWidthLong > int.MaxValue || scaledHeightLong > int.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(opts.BackgroundSupersample), "Background supersample exceeds maximum bitmap size.");
        }

        var scaledStrideLong = scaledWidthLong * 4;
        if (scaledStrideLong > int.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(opts.BackgroundSupersample), "Background supersample exceeds maximum bitmap size.");
        }

        var lengthLong = scaledHeightLong * (scaledStrideLong + 1);
        if (lengthLong > int.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(opts.BackgroundSupersample), "Background supersample exceeds maximum bitmap size.");
        }

        var scaledWidth = (int)scaledWidthLong;
        var scaledHeight = (int)scaledHeightLong;
        var scaledStride = (int)scaledStrideLong;
        var length = (int)lengthLong;
        var temp = ArrayPool<byte>.Shared.Rent(length);

        try {
            var scaledOpts = CreateScaledBackgroundOptions(opts, scale);
            var scaledQrOffsetX = checked(qrOffsetX * scale);
            var scaledQrOffsetY = checked(qrOffsetY * scale);
            var scaledQrFullPx = checked(qrFullPx * scale);

            if (scaledOpts.Canvas is null) {
                if (scaledOpts.BackgroundGradient is null) {
                    PngRenderHelpers.FillBackground(temp, scaledWidth, scaledHeight, scaledStride, scaledOpts.Background);
                } else {
                    FillBackgroundGradient(temp, scaledWidth, scaledHeight, scaledStride, scaledOpts.BackgroundGradient);
                }
                if (scaledOpts.BackgroundPattern is not null) {
                    var quietZonePx = scaledOpts.QuietZone * scaledOpts.ModuleSize;
                    var qrSizePx = Math.Max(0, scaledQrFullPx - quietZonePx * 2);
                    DrawCanvasPattern(
                        temp,
                        scaledWidth,
                        scaledHeight,
                        scaledStride,
                        scaledQrOffsetX,
                        scaledQrOffsetY,
                        scaledQrFullPx,
                        scaledQrFullPx,
                        scaledOpts.ModuleSize,
                        0,
                        quietZonePx,
                        qrSizePx,
                        scaledOpts.ProtectQuietZone,
                        scaledOpts.BackgroundPattern);
                }
            } else {
                PngRenderHelpers.FillBackground(temp, scaledWidth, scaledHeight, scaledStride, Rgba32.Transparent);
                DrawCanvas(temp, scaledWidth, scaledHeight, scaledStride, scaledOpts, scaledQrOffsetX, scaledQrOffsetY, scaledQrFullPx);
                FillQrBackground(temp, scaledWidth, scaledHeight, scaledStride, scaledOpts, scaledQrOffsetX, scaledQrOffsetY, scaledQrFullPx);
            }

            DownsampleScanlines(temp, scaledWidth, scaledHeight, scaledStride, scanlines, widthPx, heightPx, stride, scale);
        } finally {
            ArrayPool<byte>.Shared.Return(temp);
        }
    }

    private static QrPngRenderOptions CreateScaledBackgroundOptions(QrPngRenderOptions opts, int scale) {
        return new QrPngRenderOptions {
            ModuleSize = Math.Max(1, opts.ModuleSize * scale),
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background,
            BackgroundGradient = opts.BackgroundGradient,
            BackgroundPattern = ScalePattern(opts.BackgroundPattern, scale),
            BackgroundSupersample = 1,
            ProtectFunctionalPatterns = opts.ProtectFunctionalPatterns,
            ProtectQuietZone = opts.ProtectQuietZone,
            Canvas = ScaleCanvas(opts.Canvas, scale),
        };
    }

    private static QrPngBackgroundPatternOptions? ScalePattern(QrPngBackgroundPatternOptions? pattern, int scale) {
        if (pattern is null) return null;
        return new QrPngBackgroundPatternOptions {
            Type = pattern.Type,
            Color = pattern.Color,
            SizePx = Math.Max(1, pattern.SizePx * scale),
            ThicknessPx = pattern.ThicknessPx <= 0 ? 0 : Math.Max(1, pattern.ThicknessPx * scale),
            SnapToModuleSize = pattern.SnapToModuleSize,
            ModuleStep = pattern.ModuleStep
        };
    }

    private static QrPngCanvasOptions? ScaleCanvas(QrPngCanvasOptions? canvas, int scale) {
        if (canvas is null) return null;
        return new QrPngCanvasOptions {
            PaddingPx = canvas.PaddingPx * scale,
            CornerRadiusPx = canvas.CornerRadiusPx * scale,
            Background = canvas.Background,
            BackgroundGradient = canvas.BackgroundGradient,
            Pattern = ScalePattern(canvas.Pattern, scale),
            Splash = ScaleSplash(canvas.Splash, scale),
            BorderPx = canvas.BorderPx * scale,
            BorderColor = canvas.BorderColor,
            ShadowOffsetX = canvas.ShadowOffsetX * scale,
            ShadowOffsetY = canvas.ShadowOffsetY * scale,
            ShadowColor = canvas.ShadowColor
        };
    }

    private static QrPngCanvasSplashOptions? ScaleSplash(QrPngCanvasSplashOptions? splash, int scale) {
        if (splash is null) return null;
        return new QrPngCanvasSplashOptions {
            Color = splash.Color,
            Colors = splash.Colors,
            Count = splash.Count,
            MinRadiusPx = Math.Max(1, splash.MinRadiusPx * scale),
            MaxRadiusPx = Math.Max(1, splash.MaxRadiusPx * scale),
            SpreadPx = Math.Max(0, splash.SpreadPx * scale),
            Seed = splash.Seed,
            DripChance = splash.DripChance,
            DripLengthPx = Math.Max(0, splash.DripLengthPx * scale),
            DripWidthPx = Math.Max(0, splash.DripWidthPx * scale),
            ProtectQrArea = splash.ProtectQrArea,
        };
    }

    private static void DownsampleScanlines(
        byte[] src,
        int srcWidth,
        int srcHeight,
        int srcStride,
        byte[] dst,
        int dstWidth,
        int dstHeight,
        int dstStride,
        int scale) {
        var rowStride = dstStride + 1;
        for (var y = 0; y < dstHeight; y++) {
            var dstRow = y * rowStride;
            dst[dstRow] = 0;
            var dstIndex = dstRow + 1;
            var srcY0 = y * scale;
            for (var x = 0; x < dstWidth; x++) {
                var srcX0 = x * scale;
                var r = 0;
                var g = 0;
                var b = 0;
                var a = 0;
                for (var sy = 0; sy < scale; sy++) {
                    var srcRow = (srcY0 + sy) * (srcStride + 1) + 1 + srcX0 * 4;
                    for (var sx = 0; sx < scale; sx++) {
                        var p = srcRow + sx * 4;
                        r += src[p + 0];
                        g += src[p + 1];
                        b += src[p + 2];
                        a += src[p + 3];
                    }
                }
                var div = scale * scale;
                dst[dstIndex + 0] = (byte)(r / div);
                dst[dstIndex + 1] = (byte)(g / div);
                dst[dstIndex + 2] = (byte)(b / div);
                dst[dstIndex + 3] = (byte)(a / div);
                dstIndex += 4;
            }
        }
    }

}

using System;
using System.Globalization;
using System.Text;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Svg;

/// <summary>
/// Renders QR modules to SVG.
/// </summary>
public static class SvgQrRenderer {
    /// <summary>
    /// Renders the QR module matrix to an SVG string.
    /// </summary>
    public static string Render(BitMatrix modules, QrSvgRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.ModuleScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleScale));
        opts.ForegroundGradient?.Validate();
        opts.Eyes?.Validate();

        var size = modules.Width;
        var outSize = size + opts.QuietZone * 2;
        var px = outSize * opts.ModuleSize;

        var sb = new StringBuilder(outSize * outSize * 2);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(px).Append("\" height=\"").Append(px)
            .Append("\" viewBox=\"0 0 ").Append(outSize).Append(' ').Append(outSize)
            .Append("\" shape-rendering=\"crispEdges\">");

        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(opts.LightColor).Append("\"/>");

        var hasAdvanced = opts.ModuleShape != QrPngModuleShape.Square ||
                          Math.Abs(opts.ModuleScale - 1.0) > 0.0001 ||
                          opts.ModuleCornerRadiusPx != 0 ||
                          opts.ForegroundGradient is not null ||
                          opts.Eyes is not null;

        var fgGradId = opts.ForegroundGradient is not null ? "fg" : null;
        var eyeOuterIds = opts.Eyes?.OuterGradient is not null ? new[] { "eye-outer-0", "eye-outer-1", "eye-outer-2" } : null;
        var eyeInnerIds = opts.Eyes?.InnerGradient is not null ? new[] { "eye-inner-0", "eye-inner-1", "eye-inner-2" } : null;

        if (fgGradId is not null || eyeOuterIds is not null || eyeInnerIds is not null) {
            sb.Append("<defs>");
            if (fgGradId is not null) {
                AppendGradientDef(sb, fgGradId, opts.ForegroundGradient!, opts.QuietZone, opts.QuietZone, size, size);
            }
            if (eyeOuterIds is not null || eyeInnerIds is not null) {
                for (var i = 0; i < 3; i++) {
                    GetEyeOrigin(i, size, out var ex, out var ey);
                    if (eyeOuterIds is not null) {
                        AppendGradientDef(sb, eyeOuterIds[i], opts.Eyes!.OuterGradient!, ex, ey, 7, 7);
                    }
                    if (eyeInnerIds is not null) {
                        AppendGradientDef(sb, eyeInnerIds[i], opts.Eyes!.InnerGradient!, ex + 2, ey + 2, 3, 3);
                    }
                }
            }
            sb.Append("</defs>");
        }

        var useFrame = opts.Eyes is not null && opts.Eyes.UseFrame;
        var usePath = !hasAdvanced && !useFrame;

        if (usePath) {
            var fill = fgGradId is null ? opts.DarkColor : $"url(#{fgGradId})";
            sb.Append("<path fill=\"").Append(fill).Append("\" d=\"");
            for (var y = 0; y < size; y++) {
                var outY = y + opts.QuietZone;
                var runStart = -1;
                for (var x = 0; x < size; x++) {
                    var dark = modules[x, y];
                    if (dark && runStart < 0) runStart = x;
                    if ((!dark || x == size - 1) && runStart >= 0) {
                        var runEnd = dark && x == size - 1 ? x + 1 : x;
                        sb.Append('M').Append(runStart + opts.QuietZone).Append(' ').Append(outY)
                            .Append('h').Append(runEnd - runStart).Append("v1h-").Append(runEnd - runStart).Append('z');
                        runStart = -1;
                    }
                }
            }
            sb.Append("\"/>");
        } else {
            for (var my = 0; my < size; my++) {
                for (var mx = 0; mx < size; mx++) {
                    if (!modules[mx, my]) continue;
                    if (useFrame && IsInEye(mx, my, size)) continue;

                    var eyeKind = EyeKind.None;
                    var eyeIndex = -1;
                    var eyeX = 0;
                    var eyeY = 0;
                    if (opts.Eyes is not null && TryGetEye(mx, my, size, out eyeX, out eyeY, out eyeKind, out eyeIndex)) {
                        // Use eye overrides.
                    }

                    var shape = eyeKind switch {
                        EyeKind.Outer => opts.Eyes!.OuterShape,
                        EyeKind.Inner => opts.Eyes!.InnerShape,
                        _ => opts.ModuleShape,
                    };
                    var scale = eyeKind switch {
                        EyeKind.Outer => opts.Eyes!.OuterScale,
                        EyeKind.Inner => opts.Eyes!.InnerScale,
                        _ => opts.ModuleScale,
                    };
                    var radiusPx = eyeKind switch {
                        EyeKind.Outer => opts.Eyes!.OuterCornerRadiusPx,
                        EyeKind.Inner => opts.Eyes!.InnerCornerRadiusPx,
                        _ => opts.ModuleCornerRadiusPx,
                    };

                    var fill = GetFill(opts, fgGradId, eyeOuterIds, eyeInnerIds, eyeKind, eyeIndex);
                    AppendModuleShape(sb, mx + opts.QuietZone, my + opts.QuietZone, 1.0, shape, scale, radiusPx, opts.ModuleSize, fill);
                }
            }
        }

        if (useFrame && opts.Eyes is not null) {
            DrawEyeFrame(sb, opts, 0, 0, eyeOuterIds, eyeInnerIds, size);
            DrawEyeFrame(sb, opts, size - 7, 0, eyeOuterIds, eyeInnerIds, size);
            DrawEyeFrame(sb, opts, 0, size - 7, eyeOuterIds, eyeInnerIds, size);
        }

        if (opts.Logo is not null) {
            AppendLogo(sb, opts.Logo, size, opts.ModuleSize, opts.QuietZone);
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders the QR module matrix to an SVG stream.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    public static void RenderToStream(BitMatrix modules, QrSvgRenderOptions opts, Stream stream) {
        var svg = Render(modules, opts);
        RenderIO.WriteText(stream, svg);
    }

    /// <summary>
    /// Renders the QR module matrix to an SVG file.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrSvgRenderOptions opts, string path) {
        var svg = Render(modules, opts);
        return RenderIO.WriteText(path, svg);
    }

    /// <summary>
    /// Renders the QR module matrix to an SVG file under the specified directory.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrSvgRenderOptions opts, string directory, string fileName) {
        var svg = Render(modules, opts);
        return RenderIO.WriteText(directory, fileName, svg);
    }

    private enum EyeKind {
        None,
        Outer,
        Inner,
    }

    private static bool IsInEye(int mx, int my, int size) {
        return TryGetEye(mx, my, size, out _, out _, out _, out _);
    }

    private static bool TryGetEye(int mx, int my, int size, out int eyeX, out int eyeY, out EyeKind kind, out int eyeIndex) {
        eyeX = 0;
        eyeY = 0;
        kind = EyeKind.None;
        eyeIndex = -1;

        if (mx < 7 && my < 7) {
            eyeX = 0; eyeY = 0; eyeIndex = 0;
        } else if (mx >= size - 7 && my < 7) {
            eyeX = size - 7; eyeY = 0; eyeIndex = 1;
        } else if (mx < 7 && my >= size - 7) {
            eyeX = 0; eyeY = size - 7; eyeIndex = 2;
        } else {
            return false;
        }

        if (mx >= eyeX + 2 && mx <= eyeX + 4 && my >= eyeY + 2 && my <= eyeY + 4) {
            kind = EyeKind.Inner;
        } else {
            kind = EyeKind.Outer;
        }
        return true;
    }

    private static void GetEyeOrigin(int index, int size, out int x, out int y) {
        switch (index) {
            case 0:
                x = 0; y = 0;
                break;
            case 1:
                x = size - 7; y = 0;
                break;
            default:
                x = 0; y = size - 7;
                break;
        }
    }

    private static string GetFill(
        QrSvgRenderOptions opts,
        string? fgGradId,
        string[]? eyeOuterIds,
        string[]? eyeInnerIds,
        EyeKind eyeKind,
        int eyeIndex) {
        if (eyeKind == EyeKind.Outer) {
            if (eyeOuterIds is not null && eyeIndex >= 0) return $"url(#{eyeOuterIds[eyeIndex]})";
            if (opts.Eyes!.OuterColor.HasValue) return ToCssColor(opts.Eyes.OuterColor.Value);
            return opts.DarkColor;
        }
        if (eyeKind == EyeKind.Inner) {
            if (eyeInnerIds is not null && eyeIndex >= 0) return $"url(#{eyeInnerIds[eyeIndex]})";
            if (opts.Eyes!.InnerColor.HasValue) return ToCssColor(opts.Eyes.InnerColor.Value);
            return opts.DarkColor;
        }

        if (fgGradId is not null) return $"url(#{fgGradId})";
        return opts.DarkColor;
    }

    private static void AppendModuleShape(
        StringBuilder sb,
        double cellX,
        double cellY,
        double cellSize,
        QrPngModuleShape shape,
        double scale,
        int cornerRadiusPx,
        int moduleSizePx,
        string fill) {
        if (scale <= 0) return;
        if (scale > 1.0) scale = 1.0;

        if (shape == QrPngModuleShape.Dot) scale *= QrPngShapeDefaults.DotScale;
        if (shape == QrPngModuleShape.DotGrid) {
            AppendDotGrid(sb, cellX, cellY, cellSize, scale, fill);
            return;
        }

        var size = cellSize * scale;
        var inset = (cellSize - size) / 2.0;
        var x = cellX + inset;
        var y = cellY + inset;

        if (shape == QrPngModuleShape.Circle) {
            var r = size / 2.0;
            sb.Append("<circle cx=\"").Append(Format(x + r)).Append("\" cy=\"").Append(Format(y + r))
                .Append("\" r=\"").Append(Format(r)).Append("\" fill=\"").Append(fill).Append("\"/>");
            return;
        }
        if (shape == QrPngModuleShape.Diamond) {
            var cx = x + size / 2.0;
            var cy = y + size / 2.0;
            sb.Append("<polygon points=\"")
                .Append(Format(cx)).Append(',').Append(Format(y)).Append(' ')
                .Append(Format(x + size)).Append(',').Append(Format(cy)).Append(' ')
                .Append(Format(cx)).Append(',').Append(Format(y + size)).Append(' ')
                .Append(Format(x)).Append(',').Append(Format(cy))
                .Append("\" fill=\"").Append(fill).Append("\"/>");
            return;
        }
        if (shape == QrPngModuleShape.Squircle) {
            AppendSquircle(sb, x, y, size, fill);
            return;
        }

        var radius = 0.0;
        if (shape == QrPngModuleShape.Rounded) {
            radius = cornerRadiusPx > 0 ? cornerRadiusPx / (double)moduleSizePx : size / 4.0;
            if (radius > size / 2.0) radius = size / 2.0;
        }

        sb.Append("<rect x=\"").Append(Format(x)).Append("\" y=\"").Append(Format(y))
            .Append("\" width=\"").Append(Format(size)).Append("\" height=\"").Append(Format(size))
            .Append("\" fill=\"").Append(fill).Append('"');
        if (radius > 0) {
            sb.Append(" rx=\"").Append(Format(radius)).Append("\" ry=\"").Append(Format(radius)).Append('"');
        }
        sb.Append("/>");
    }

    private static void AppendDotGrid(StringBuilder sb, double cellX, double cellY, double cellSize, double scale, string fill) {
        if (scale <= 0) return;
        if (scale > 1.0) scale = 1.0;

        scale *= QrPngShapeDefaults.DotGridScale;
        var gridSize = cellSize * scale;
        var inset = (cellSize - gridSize) / 2.0;
        var baseX = cellX + inset;
        var baseY = cellY + inset;

        var dotRadius = Math.Max(QrPngShapeDefaults.DotGridMinRadius, gridSize * QrPngShapeDefaults.DotGridRadiusFactor);
        var c0 = baseX + gridSize * QrPngShapeDefaults.DotGridCenterFactor;
        var c1 = baseX + gridSize * (1.0 - QrPngShapeDefaults.DotGridCenterFactor);
        var r = dotRadius;

        AppendCircle(sb, c0, baseY + gridSize * QrPngShapeDefaults.DotGridCenterFactor, r, fill);
        AppendCircle(sb, c1, baseY + gridSize * QrPngShapeDefaults.DotGridCenterFactor, r, fill);
        AppendCircle(sb, c0, baseY + gridSize * (1.0 - QrPngShapeDefaults.DotGridCenterFactor), r, fill);
        AppendCircle(sb, c1, baseY + gridSize * (1.0 - QrPngShapeDefaults.DotGridCenterFactor), r, fill);
    }

    private static void AppendCircle(StringBuilder sb, double cx, double cy, double radius, string fill) {
        if (radius <= 0) return;
        sb.Append("<circle cx=\"").Append(Format(cx)).Append("\" cy=\"").Append(Format(cy))
            .Append("\" r=\"").Append(Format(radius)).Append("\" fill=\"").Append(fill).Append("\"/>");
    }

    private static void AppendSquircle(StringBuilder sb, double x, double y, double size, string fill) {
        const int steps = 32;
        var cx = x + size / 2.0;
        var cy = y + size / 2.0;
        var r = size / 2.0;

        sb.Append("<polygon points=\"");
        for (var i = 0; i < steps; i++) {
            var angle = i * (Math.PI * 2.0 / steps);
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var dx = Math.Sign(cos) * Math.Sqrt(Math.Abs(cos)) * r;
            var dy = Math.Sign(sin) * Math.Sqrt(Math.Abs(sin)) * r;
            var px = cx + dx;
            var py = cy + dy;
            if (i > 0) sb.Append(' ');
            sb.Append(Format(px)).Append(',').Append(Format(py));
        }
        sb.Append("\" fill=\"").Append(fill).Append("\"/>");
    }

    private static void DrawEyeFrame(
        StringBuilder sb,
        QrSvgRenderOptions opts,
        int eyeX,
        int eyeY,
        string[]? eyeOuterIds,
        string[]? eyeInnerIds,
        int size) {
        var eyes = opts.Eyes ?? throw new ArgumentNullException(nameof(opts.Eyes));
        var eyeIndex = eyeX == 0 && eyeY == 0 ? 0 : eyeX == size - 7 ? 1 : 2;
        var outerFill = eyeOuterIds is not null ? $"url(#{eyeOuterIds[eyeIndex]})"
            : eyes.OuterColor.HasValue ? ToCssColor(eyes.OuterColor.Value) : opts.DarkColor;
        var innerFill = eyeInnerIds is not null ? $"url(#{eyeInnerIds[eyeIndex]})"
            : eyes.InnerColor.HasValue ? ToCssColor(eyes.InnerColor.Value) : opts.DarkColor;

        AppendModuleShape(sb, eyeX, eyeY, 7.0, eyes.OuterShape, eyes.OuterScale, eyes.OuterCornerRadiusPx, opts.ModuleSize, outerFill);
        AppendModuleShape(sb, eyeX + 2, eyeY + 2, 3.0, eyes.InnerShape, 1.0, 0, opts.ModuleSize, opts.LightColor);
        AppendModuleShape(sb, eyeX + 2, eyeY + 2, 3.0, eyes.InnerShape, eyes.InnerScale, eyes.InnerCornerRadiusPx, opts.ModuleSize, innerFill);
    }

    private static void AppendGradientDef(
        StringBuilder sb,
        string id,
        QrPngGradientOptions gradient,
        double x,
        double y,
        double w,
        double h) {
        if (gradient.Type == QrPngGradientType.Radial) {
            var cx = x + gradient.CenterX * w;
            var cy = y + gradient.CenterY * h;
            var r = Math.Max(w, h) / 2.0;
            sb.Append("<radialGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\"")
                .Append(" cx=\"").Append(Format(cx)).Append("\" cy=\"").Append(Format(cy))
                .Append("\" r=\"").Append(Format(r)).Append("\">");
        } else {
            GetLinearPoints(gradient.Type, x, y, w, h, out var x1, out var y1, out var x2, out var y2);
            sb.Append("<linearGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\"")
                .Append(" x1=\"").Append(Format(x1)).Append("\" y1=\"").Append(Format(y1))
                .Append("\" x2=\"").Append(Format(x2)).Append("\" y2=\"").Append(Format(y2)).Append("\">");
        }

        sb.Append("<stop offset=\"0%\" stop-color=\"").Append(ToCssColor(gradient.StartColor)).Append("\"/>");
        sb.Append("<stop offset=\"100%\" stop-color=\"").Append(ToCssColor(gradient.EndColor)).Append("\"/>");

        if (gradient.Type == QrPngGradientType.Radial) sb.Append("</radialGradient>");
        else sb.Append("</linearGradient>");
    }

    private static void GetLinearPoints(QrPngGradientType type, double x, double y, double w, double h, out double x1, out double y1, out double x2, out double y2) {
        switch (type) {
            case QrPngGradientType.Vertical:
                x1 = x; y1 = y; x2 = x; y2 = y + h;
                break;
            case QrPngGradientType.DiagonalDown:
                x1 = x; y1 = y; x2 = x + w; y2 = y + h;
                break;
            case QrPngGradientType.DiagonalUp:
                x1 = x; y1 = y + h; x2 = x + w; y2 = y;
                break;
            default:
                x1 = x; y1 = y; x2 = x + w; y2 = y;
                break;
        }
    }

    private static void AppendLogo(StringBuilder sb, QrLogoOptions logo, int qrModules, int moduleSize, int quietZone) {
        if (logo is null) return;
        logo.Validate();

        if (!QrLogoOptions.TryReadPngSize(logo.Png, out var logoW, out var logoH)) {
            throw new ArgumentException("Invalid PNG logo data.", nameof(logo));
        }

        var maxLogoPx = (int)Math.Round(qrModules * moduleSize * logo.Scale);
        if (maxLogoPx <= 0) return;
        var scale = Math.Min(maxLogoPx / (double)logoW, maxLogoPx / (double)logoH);
        if (scale <= 0) return;

        var targetWpx = Math.Max(1, (int)Math.Round(logoW * scale));
        var targetHpx = Math.Max(1, (int)Math.Round(logoH * scale));
        var targetW = targetWpx / (double)moduleSize;
        var targetH = targetHpx / (double)moduleSize;

        var outModules = qrModules + quietZone * 2;
        var x = outModules / 2.0 - targetW / 2.0;
        var y = outModules / 2.0 - targetH / 2.0;

        var pad = Math.Max(0, logo.PaddingPx) / (double)moduleSize;
        var rectX = x - pad;
        var rectY = y - pad;
        var rectW = targetW + pad * 2;
        var rectH = targetH + pad * 2;
        var radius = Math.Max(0, logo.CornerRadiusPx) / (double)moduleSize;

        var dataUri = "data:image/png;base64," + Convert.ToBase64String(logo.Png);
        var bg = ToCssColor(logo.Background);

        sb.Append("<g>");
        if (logo.DrawBackground) {
            sb.Append("<rect x=\"").Append(Format(rectX)).Append("\" y=\"").Append(Format(rectY))
                .Append("\" width=\"").Append(Format(rectW)).Append("\" height=\"").Append(Format(rectH))
                .Append("\" fill=\"").Append(bg).Append('"');
            if (radius > 0) {
                sb.Append(" rx=\"").Append(Format(radius)).Append("\" ry=\"").Append(Format(radius)).Append('"');
            }
            sb.Append("/>");
        }

        sb.Append("<image x=\"").Append(Format(x)).Append("\" y=\"").Append(Format(y))
            .Append("\" width=\"").Append(Format(targetW)).Append("\" height=\"").Append(Format(targetH))
            .Append("\" href=\"").Append(dataUri).Append("\"/>");
        sb.Append("</g>");
    }

    private static string ToCssColor(CodeGlyphX.Rendering.Png.Rgba32 color) {
        if (color.A == 255) {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        var a = color.A / 255.0;
        return $"rgba({color.R},{color.G},{color.B},{a.ToString("0.###", CultureInfo.InvariantCulture)})";
    }

    private static string Format(double value) {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

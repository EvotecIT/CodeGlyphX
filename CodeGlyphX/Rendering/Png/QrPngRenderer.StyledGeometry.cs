using System;
using System.Collections.Generic;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static bool HasStandardFinders(BitMatrix modules) {
        return Matches(0, 0) && Matches(modules.Width - 7, 0) && Matches(0, modules.Height - 7);
        bool Matches(int left, int top) {
            for (var y = 0; y < 7; y++)
                for (var x = 0; x < 7; x++) {
                    var ink = x == 0 || y == 0 || x == 6 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                    if (modules[left + x, top + y] != ink) return false;
                }
            return true;
        }
    }

    private static List<(int X, int Y)> GetStyledAlignmentCenters(int size) {
        var result = new List<(int, int)>();
        if (!QrStructureAnalysis.TryGetVersionFromSize(size, out var version)) return result;
        var positions = QrTables.GetAlignmentPatternPositions(version);
        for (var i = 0; i < positions.Length; i++)
            for (var j = 0; j < positions.Length; j++) {
                if ((i == 0 && j == 0) || (i == 0 && j == positions.Length - 1) || (j == 0 && i == positions.Length - 1)) continue;
                result.Add((positions[i], positions[j]));
            }
        return result;
    }

    private static bool IsStyledAlignmentCell(int x, int y, List<(int X, int Y)> centers) {
        foreach (var c in centers) if (Math.Abs(x - c.X) <= 2 && Math.Abs(y - c.Y) <= 2) return true;
        return false;
    }

    // Coverage is evaluated against continuous geometry at the requested pixel grid. No enlarged bitmap is allocated.
    private static void DrawCoverageModule(byte[] buffer, int stride, int moduleSize, int mx, int my, int quiet,
        int offsetX, int offsetY, Rgba32 color, GradientInfo? gradient, QrForegroundPatternOptions? pattern,
        QrModuleShape shape, double scale, int radius, int neighbours, int originX, int originY, int qrSize,
        int jitterX, int jitterY, double timingThickness = 0, bool moduleOn = true) {
        var x0 = offsetX + (mx + quiet) * moduleSize;
        var y0 = offsetY + (my + quiet) * moduleSize;
        var horizontalTiming = timingThickness > 0 && my == 6 && mx >= 8 && mx < qrSize / moduleSize - 8;
        var verticalTiming = timingThickness > 0 && mx == 6 && my >= 8 && my < qrSize / moduleSize - 8;
        for (var y = 0; y < moduleSize; y++) {
            for (var x = 0; x < moduleSize; x++) {
                var hits = 0;
                var lineHits = 0;
                for (var sy = 0; sy < 4; sy++)
                    for (var sx = 0; sx < 4; sx++) {
                        var u = (x - jitterX + (sx + .5) / 4) / moduleSize;
                        var v = (y - jitterY + (sy + .5) / 4) / moduleSize;
                        if (moduleOn && QrShapeGeometry.Contains(shape, u, v, scale, radius / (double)moduleSize, neighbours)) hits++;
                        else if (horizontalTiming && Math.Abs(v - .5) <= timingThickness / 2
                            || verticalTiming && Math.Abs(u - .5) <= timingThickness / 2) lineHits++;
                    }
                if (hits == 0 && lineHits == 0) continue;
                var ink = gradient.HasValue ? GetGradientColor(gradient.Value, x0 + x, y0 + y, originX, originY) : color;
                if (!ApplyStyledPattern(pattern, moduleSize, x0 + x, y0 + y, originX, originY, qrSize, ref ink)) continue;
                // Keep alternating timing dots legible over the continuous hairline.
                // Replacing them with a solid line erases the timing signal on dense codes.
                var covered = lineHits == 0 ? CoverageColor(ink, hits)
                    : new Rgba32(ink.R, ink.G, ink.B, (byte)((ink.A * (hits + lineHits * .25) + 8) / 16));
                BlendPixelAt(buffer, (y0 + y) * (stride + 1) + 1 + (x0 + x) * 4, covered);
            }
        }
    }

    private static Rgba32 CoverageColor(Rgba32 ink, int hits) => new(ink.R, ink.G, ink.B, (byte)((ink.A * hits + 8) / 16));

    private static QrModuleShape FinderShape(QrModuleShape shape) => shape switch {
        QrModuleShape.Dot or QrModuleShape.Circle => QrModuleShape.Circle,
        QrModuleShape.ConnectedSquircle => QrModuleShape.Squircle,
        QrModuleShape.Liquid or QrModuleShape.ConnectedRounded or QrModuleShape.BarsHorizontal or QrModuleShape.BarsVertical => QrModuleShape.Rounded,
        _ => shape,
    };

    private static void DrawStyledFinder(byte[] buffer, int stride, QrPngRenderOptions opts, BitMatrix modules,
        int originX, int originY, int qrSize, int mx, int my, int cells, int index) {
        var m = opts.ModuleSize;
        var eye = opts.Eyes;
        var defaultShape = opts.ModuleShape == QrModuleShape.Square ? QrModuleShape.Square : QrModuleShape.Rounded;
        var outerShape = FinderShape(eye?.OuterShape ?? defaultShape);
        var ballShape = FinderShape(eye?.InnerShape ?? defaultShape);
        var frame = eye?.UseFrame == true ? eye.FrameStyle : QrEyeFrameStyle.Single;
        if (frame == QrEyeFrameStyle.CutCorner) outerShape = QrModuleShape.CutCorner;
        if (frame == QrEyeFrameStyle.Badge) outerShape = QrModuleShape.Squircle;
        var outer = GetEyeOuterColor(opts, index);
        var inner = GetEyeInnerColor(opts, index);
        var outerGrad = GetEyeOuterGradient(opts, index);
        var innerGrad = GetEyeInnerGradient(opts, index);
        var global = opts.ForegroundGradient is null ? (GradientInfo?)null : new GradientInfo(opts.ForegroundGradient, qrSize - 1, qrSize - 1);
        var og = outerGrad is null ? (GradientInfo?)null : new GradientInfo(outerGrad, cells * m - 1, cells * m - 1);
        var ig = innerGrad is null ? (GradientInfo?)null : new GradientInfo(innerGrad, (cells - 4) * m - 1, (cells - 4) * m - 1);
        var left = originX + mx * m;
        var top = originY + my * m;
        var pixels = cells * m;
        var palette = opts.ForegroundPalette is null ? (PaletteInfo?)null : new PaletteInfo(opts.ForegroundPalette, qrSize / m);
        var zones = opts.ForegroundPaletteZones is null ? (PaletteZoneInfo?)null : new PaletteZoneInfo(opts.ForegroundPaletteZones, qrSize / m);
        var pattern = opts.ForegroundPattern?.ApplyToEyes == true ? opts.ForegroundPattern : null;
        var outerRadius = (eye?.OuterCornerRadiusPx ?? 0) / (double)(cells * m);
        var holeRadius = eye?.OuterCornerRadiusPx > 0 ? Math.Max(1e-9, (eye.OuterCornerRadiusPx - m) / (double)((cells - 2) * m)) : 0;
        var ballRadius = (eye?.InnerCornerRadiusPx ?? 0) / (double)((cells - 4) * m);
        if (index >= 0 && frame == QrEyeFrameStyle.Glow && eye is not null) {
            var glowRadius = eye.GlowRadiusPx > 0 ? eye.GlowRadiusPx : m * 2;
            var glow = eye.GlowColor ?? outer;
            var glowInk = new Rgba32(glow.R, glow.G, glow.B, (byte)(glow.A * eye.GlowAlpha / 255));
            DrawEyeGlow(buffer, stride / 4, buffer.Length / (stride + 1), stride, left, top, pixels,
                glowRadius, glowInk, originX, originY, originX + qrSize, originY + qrSize);
        }
        // The separator remains one full module; the ball is exactly 3x3 (1x1 in a 5x5 alignment).
        for (var y = 0; y < pixels; y++)
            for (var x = 0; x < pixels; x++) {
                var ringHits = 0;
                var ballHits = 0;
                var bodyHits = 0;
                for (var sy = 0; sy < 4; sy++)
                    for (var sx = 0; sx < 4; sx++) {
                        var xx = (x + (sx + 0.5) / 4) / m;
                        var yy = (y + (sy + 0.5) / 4) / m;
                        if (index < 0 && opts.ModuleShape == QrModuleShape.Dot && eye?.OuterShape == QrModuleShape.Rounded) {
                            var cx = (int)xx; var cy = (int)yy;
                            var dot = QrShapeGeometry.Contains(QrModuleShape.Circle, xx - cx, yy - cy, scale: .9);
                            if (dot && (cx == 0 || cy == 0 || cx == cells - 1 || cy == cells - 1)) ringHits++;
                            if (cx == 2 && cy == 2 && QrShapeGeometry.Contains(QrModuleShape.Circle, xx - cx, yy - cy)) ballHits++;
                            continue;
                        }
                        var thickness = eye?.RingThicknessModules ?? 1;
                        // Keep a thin stroke centred on the standard ring's centreline.
                        // Expanding only its aperture shifts the ring and breaks finder run ratios.
                        var outerInset = (1 - thickness) / 2;
                        var holeInset = 1 - outerInset;
                        var outerSpan = cells - 2 * outerInset;
                        var holeSpan = cells - 2 * holeInset;
                        if (index < 0 && !QrShapeGeometry.ContainsEye(outerShape, (xx - outerInset) / outerSpan, (yy - outerInset) / outerSpan, radius: outerRadius, outwardCorner: eye?.MirrorOutward == true ? (index < 0 ? 3 : index) : -1)) {
                            var cellX = Math.Min(cells - 1, (int)xx); var cellY = Math.Min(cells - 1, (int)yy);
                            var qx = mx + cellX; var qy = my + cellY;
                            if (modules[qx, qy] && QrShapeGeometry.Contains(opts.ModuleShape, xx - cellX, yy - cellY,
                                opts.ModuleScale, opts.ModuleCornerRadiusPx / (double)m, GetNeighborMask(modules, qx, qy))) bodyHits++;
                        }
                        if (QrShapeGeometry.ContainsEye(outerShape, (xx - outerInset) / outerSpan, (yy - outerInset) / outerSpan, radius: outerRadius, outwardCorner: eye?.MirrorOutward == true ? (index < 0 ? 3 : index) : -1)
                            && !QrShapeGeometry.ContainsEye(outerShape, (xx - holeInset) / holeSpan, (yy - holeInset) / holeSpan, radius: holeRadius, outwardCorner: eye?.MirrorOutward == true ? (index < 0 ? 3 : index) : -1)
                            && StyledFrameRingVisible(frame, xx, yy, cells)) ringHits++;
                        if (QrShapeGeometry.ContainsEye(ballShape, (xx - 2) / (cells - 4), (yy - 2) / (cells - 4), radius: ballRadius, outwardCorner: eye?.MirrorOutward == true ? (index < 0 ? 3 : index) : -1)) ballHits++;
                    }
                var p = (top + y) * (stride + 1) + 1 + (left + x) * 4;
                var modulePalette = ResolveStyledPalette(palette, zones, mx + x / m, my + y / m, true);
                var paletteInk = modulePalette.HasValue ? GetPaletteColor(modulePalette.Value, mx + x / m, my + y / m) : (Rgba32?)null;
                var bodyInk = paletteInk ?? (global.HasValue ? GetGradientColor(global.Value, left + x, top + y, originX, originY) : opts.Foreground);
                if (!ApplyStyledPattern(opts.ForegroundPattern?.ApplyToModules == true ? opts.ForegroundPattern : null,
                    m, left + x, top + y, originX, originY, qrSize, ref bodyInk)) bodyHits = 0;
                var red = bodyInk.R * bodyInk.A * bodyHits;
                var green = bodyInk.G * bodyInk.A * bodyHits;
                var blue = bodyInk.B * bodyInk.A * bodyHits;
                var alpha = bodyInk.A * bodyHits;
                if (ringHits > 0) {
                    var ink = og.HasValue ? GetGradientColorInBox(og.Value, left + x, top + y, left, top)
                        : paletteInk ?? (global.HasValue && eye?.OuterColor is null && eye?.OuterColors is null
                            ? GetGradientColor(global.Value, left + x, top + y, originX, originY) : outer);
                    if (ApplyStyledPattern(pattern, m, left + x, top + y, originX, originY, qrSize, ref ink)) {
                        red += ink.R * ink.A * ringHits; green += ink.G * ink.A * ringHits;
                        blue += ink.B * ink.A * ringHits; alpha += ink.A * ringHits;
                    }
                }
                if (ballHits > 0) {
                    var ink = ig.HasValue ? GetGradientColorInBox(ig.Value, left + x, top + y, left + 2 * m, top + 2 * m)
                        : paletteInk ?? (global.HasValue && eye?.InnerColor is null && eye?.InnerColors is null
                            ? GetGradientColor(global.Value, left + x, top + y, originX, originY) : inner);
                    if (ApplyStyledPattern(pattern, m, left + x, top + y, originX, originY, qrSize, ref ink)) {
                        red += ink.R * ink.A * ballHits; green += ink.G * ink.A * ballHits;
                        blue += ink.B * ink.A * ballHits; alpha += ink.A * ballHits;
                    }
                }
                if (alpha > 0) BlendPixelAt(buffer, p, new Rgba32((byte)(red / alpha), (byte)(green / alpha),
                    (byte)(blue / alpha), (byte)((alpha + 8) / 16)));
            }
    }

    private static void DrawLiquidInnerCorners(byte[] buffer, int stride, BitMatrix modules, QrPngRenderOptions opts,
        int originX, int originY, int qrSize, List<(int X, int Y)> alignments) {
        var m = opts.ModuleSize;
        var gradient = opts.ForegroundGradient is null ? (GradientInfo?)null : new GradientInfo(opts.ForegroundGradient, qrSize - 1, qrSize - 1);
        var structural = QrStructureAnalysis.TryGetVersionFromSize(modules.Width, out var version)
            ? QrStructureAnalysis.BuildFunctionMask(version, modules.Width) : null;
        var palette = opts.ForegroundPalette is null ? (PaletteInfo?)null : new PaletteInfo(opts.ForegroundPalette, modules.Width);
        var zones = opts.ForegroundPaletteZones is null ? (PaletteZoneInfo?)null : new PaletteZoneInfo(opts.ForegroundPaletteZones, modules.Width);
        var pattern = opts.ForegroundPattern?.ApplyToModules == true ? opts.ForegroundPattern : null;
        var shapeMap = opts.ModuleShapeMap is null ? (ModuleShapeMapInfo?)null : new ModuleShapeMapInfo(opts.ModuleShapeMap, modules.Width);
        for (var my = 1; my < modules.Height - 1; my++)
            for (var mx = 1; mx < modules.Width - 1; mx++) {
                if (modules[mx, my] || TryGetEye(mx, my, modules.Width, out _, out _, out _) || IsStyledAlignmentCell(mx, my, alignments)) continue;
                // Leave timing and encoded format/version bits untouched, including their light cells.
                if (structural is not null && structural[mx, my]) continue;
                for (var cy = -1; cy <= 1; cy += 2)
                    for (var cx = -1; cx <= 1; cx += 2) {
                        if (!modules[mx + cx, my] || !modules[mx, my + cy] || !modules[mx + cx, my + cy]) continue;
                        if (!LiquidAt(mx + cx, my) || !LiquidAt(mx, my + cy) || !LiquidAt(mx + cx, my + cy)) continue;
                        var r = m * 0.4;
                        for (var y = 0; y < Math.Ceiling(r); y++)
                            for (var x = 0; x < Math.Ceiling(r); x++) {
                                var hits = 0;
                                for (var sy = 0; sy < 4; sy++)
                                    for (var sx = 0; sx < 4; sx++) {
                                        var dx = r - x - (sx + 0.5) / 4;
                                        var dy = r - y - (sy + 0.5) / 4;
                                        if (dx > 0 && dy > 0 && dx * dx + dy * dy >= r * r) hits++;
                                    }
                                var px = originX + mx * m + (cx < 0 ? x : m - 1 - x);
                                var py = originY + my * m + (cy < 0 ? y : m - 1 - y);
                                var selected = ResolveStyledPalette(palette, zones, mx + cx, my + cy, false);
                                var ink = selected.HasValue ? GetPaletteColor(selected.Value, mx + cx, my + cy)
                                    : gradient.HasValue ? GetGradientColor(gradient.Value, px, py, originX, originY) : opts.Foreground;
                                if (!ApplyStyledPattern(pattern, m, px, py, originX, originY, qrSize, ref ink)) continue;
                                if (hits > 0) BlendPixelAt(buffer, py * (stride + 1) + 1 + px * 4, CoverageColor(ink, hits));
                            }
                    }
            }
        bool LiquidAt(int x, int y) => (shapeMap.HasValue ? GetShapeForModule(shapeMap.Value, x, y, modules.Width) : opts.ModuleShape) == QrModuleShape.Liquid;
    }
}

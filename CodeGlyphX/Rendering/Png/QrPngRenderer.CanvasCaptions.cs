using System;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static void DrawCaptionCanvas(byte[] pixels, int width, int height, int stride, QrCanvasOptions canvas,
        int x, int y, int w, int h, int qx, int qy, int qr, int moduleSize, int quiet) {
        var caption = canvas.Caption;
        var plan = MeasureCaption(canvas, qr, moduleSize, quiet);
        var top = caption?.Position == QrCanvasCaptionPosition.Top;
        var sy = top ? qy - plan.Height - plan.Gap : qy + qr + plan.Gap;
        var defaultInk = IsDark(canvas.Background) ? new Rgba32(230, 232, 244) : new Rgba32(20, 24, 40);
        var ink = canvas.StrokeColor ?? canvas.Frame?.Color ?? defaultInk;
        var band = canvas.BandColor ?? ink;
        var foreground = caption?.Color ?? ink;
        var t = CaptionStrokeWidth(canvas, moduleSize);
        var safe = FrameOuterSafeMargin(canvas, qr, moduleSize, quiet);
        var left = x + safe; var right = x + w - safe;
        var upper = y + safe; var lower = y + h - safe;
        var radius = canvas.Template == QrCanvasTemplate.PhoneOutline ? w / 9 : (int)Math.Round(w * .06);
        var pillWidth = Math.Min(plan.CodeWidth, plan.Width + 2 * plan.HorizontalClearance);
        var pillLeft = qx + (qr - pillWidth) / 2;
        bool Rounded(double px, double py, double rx, double ry, double rw, double rh, double rr) {
            if (px < rx || px >= rx + rw || py < ry || py >= ry + rh) return false;
            rr = Math.Max(0, Math.Min(rr, Math.Min(rw, rh) / 2));
            var dx = Math.Max(0, Math.Abs(px - rx - rw / 2) - rw / 2 + rr);
            var dy = Math.Max(0, Math.Abs(py - ry - rh / 2) - rh / 2 + rr);
            return dx * dx + dy * dy <= rr * rr;
        }
        var template = canvas.Template;
        var notch = Math.Max(8, w * .045);
        // Keep the complete stroked arc outside the square quiet-zone corner. Moving
        // the divider within its gap preserves both code position and canvas dimensions.
        var sideClearance = qx - left - t / 2.0;
        var notchReach = notch + t / 2.0 + 2;
        var notchGap = Math.Sqrt(Math.Max(0, notchReach * notchReach - sideClearance * sideClearance));
        var dividerInset = Math.Max(0, Math.Min(plan.Gap / 2.0, plan.Gap - notchGap));
        var divider = top ? sy + plan.Height + dividerInset : sy - dividerInset;
        // Leave a complete dash-gap between each notch arc and the perforation.
        var dashStart = left + notch + 3 * t;
        var dashEnd = right - notch - 3 * t;
        var dashCount = Math.Max(0, (int)Math.Floor((dashEnd - dashStart + 2 * t) / (4 * t)));
        var dashOrigin = (dashStart + dashEnd - Math.Max(0, dashCount * 4 - 2) * t) / 2;
        if (template != QrCanvasTemplate.None) for (var py = y; py < y + h; py++) for (var px = x; px < x + w; px++) {
            if (px >= qx && px < qx + qr && py >= qy && py < qy + qr) continue;
            var hits = 0; var bandHits = 0;
            for (var iy = 0; iy < 2; iy++) for (var ix = 0; ix < 2; ix++) {
                var xx = px + (ix + .5) / 2; var yy = py + (iy + .5) / 2;
                var outer = Rounded(xx, yy, left, upper, right - left, lower - upper, radius);
                var inner = Rounded(xx, yy, left + t, upper + t, right - left - 2 * t, lower - upper - 2 * t, radius - t);
                var paint = false; var fill = false;
                switch (template) {
                    case QrCanvasTemplate.ScanMePill:
                    case QrCanvasTemplate.SpeechBubble:
                        if (plan.Height > 0) {
                            fill = Rounded(xx, yy, pillLeft, sy, pillWidth, plan.Height, plan.Height * (plan.Lines.Count > 1 ? .35 : .5));
                            if (template == QrCanvasTemplate.SpeechBubble || canvas.CaptionTail) {
                                var tail = plan.CapHeight;
                                var distance = top ? yy - sy - plan.Height : sy - yy;
                                var dxTail = xx - (qx + qr / 2.0);
                                // Tangent sides meet a circular tip without a pointed triangle showing through.
                                var tipRadius = tail * .15;
                                var tangentY = tail * .9336;
                                var halfWidth = tail * .75;
                                fill |= distance >= 0 && distance <= tangentY && Math.Abs(dxTail) <= halfWidth + (tail * .1249 - halfWidth) * distance / tangentY;
                                fill |= dxTail * dxTail + Math.Pow(distance - tail * .85, 2) <= tipRadius * tipRadius;
                            }
                        }
                        break;
                    case QrCanvasTemplate.TicketStub:
                        outer = Rounded(xx, yy, left, upper, right - left, lower - upper, notch * 1.1);
                        inner = Rounded(xx, yy, left + t, upper + t, right - left - 2 * t, lower - upper - 2 * t, notch * 1.1 - t);
                        var dx = Math.Min(Math.Abs(xx - left - t / 2.0), Math.Abs(xx - right + t / 2.0));
                        var d = Math.Sqrt(dx * dx + (yy - divider) * (yy - divider));
                        // Distance to one continuous centreline: side, inward semicircle, side.
                        // Round joins at the arc endpoints use the same half-stroke radius.
                        paint = outer && !inner && Math.Abs(yy - divider) >= notch;
                        paint |= xx >= left + t / 2.0 && xx <= right - t / 2.0 && Math.Abs(d - notch) <= t / 2.0;
                        paint |= dx * dx + Math.Pow(Math.Abs(yy - divider) - notch, 2) <= t * t / 4.0;
                        var dash = (xx - dashOrigin) % (t * 4);
                        paint |= dashCount > 0 && xx >= dashOrigin && xx < dashOrigin + (dashCount * 4 - 2) * t
                            && Rounded(dash, yy, 0, divider - t / 2.0, t * 2, t, t / 2.0);
                        break;
                    case QrCanvasTemplate.PhoneOutline:
                        var bezel = Math.Max(t * 2, qr / 65);
                        paint = outer && !Rounded(xx, yy, left + bezel, upper + bezel, right - left - 2 * bezel, lower - upper - 2 * bezel, radius - bezel);
                        paint |= Rounded(xx, yy, x + w * .36, upper + bezel * 2, w * .28, bezel * 1.6, bezel * .8);
                        paint |= Rounded(xx, yy, x + w * .39, lower - bezel * 3, w * .22, bezel / 2.0, bezel / 4.0);
                        break;
                    case QrCanvasTemplate.RoundedCardHeader:
                        paint = outer && !inner;
                        fill = outer && plan.Height > 0 && yy >= (top ? upper : sy) && yy < (top ? sy + plan.Height : lower);
                        break;
                    case QrCanvasTemplate.CornerViewfinder:
                        var gap = Math.Max(3, t * 1.5);
                        var l = qx - gap - t; var r = qx + qr + gap + t;
                        var u = qy - gap - t; var b = qy + qr + gap + t;
                        var length = qr * .13;
                        paint = Rounded(xx, yy, l, u, length, t, t / 2.0) || Rounded(xx, yy, l, u, t, length, t / 2.0)
                            || Rounded(xx, yy, r - length, u, length, t, t / 2.0) || Rounded(xx, yy, r - t, u, t, length, t / 2.0)
                            || Rounded(xx, yy, l, b - t, length, t, t / 2.0) || Rounded(xx, yy, l, b - length, t, length, t / 2.0)
                            || Rounded(xx, yy, r - length, b - t, length, t, t / 2.0) || Rounded(xx, yy, r - t, b - length, t, length, t / 2.0);
                        break;
                }
                if (paint) hits++;
                if (fill) bandHits++;
            }
            if (hits > 0) BlendPixel(pixels, stride, px, py, new Rgba32(ink.R, ink.G, ink.B, (byte)(ink.A * hits / 4)));
            if (bandHits > 0) BlendPixel(pixels, stride, px, py, new Rgba32(band.R, band.G, band.B, (byte)(band.A * bandHits / 4)));
        }
        if (caption is null || plan.Height == 0 && plan.UrlShape is null) return;
        if (template is QrCanvasTemplate.ScanMePill or QrCanvasTemplate.SpeechBubble or QrCanvasTemplate.RoundedCardHeader)
            foreground = caption.Color ?? (IsDark(band) ? Rgba32.White : Rgba32.Black);
        var textY = sy + plan.Clearance;
        for (var i = 0; i < plan.Lines.Count; i++) {
            var line = plan.Lines[i];
            var lineWidth = (int)Math.Ceiling(line.Shape.InkBounds.Width);
            var iconWidth = i == 0 ? plan.IconWidth : 0;
            var tx = qx + (qr - lineWidth - iconWidth) / 2 + iconWidth;
            PaintCaptionShape(pixels, width, height, stride, line.Shape, tx, textY, foreground);
            textY += (int)Math.Ceiling(line.Shape.InkBounds.Height) + plan.LineGap;
        }
        if (plan.IconWidth > 0) {
            var first = plan.Lines[0];
            var xHeight = caption.Font.Shape("x", first.Size, strict: false);
            var iconSize = plan.IconSize;
            var centre = xHeight.MissingGlyphs.Count == 0
                ? xHeight.InkBounds.Y - first.Shape.InkBounds.Y + xHeight.InkBounds.Height / 2
                : first.Shape.InkBounds.Height / 2;
            var headlineWidth = (int)Math.Ceiling(first.Shape.InkBounds.Width) + plan.IconWidth;
            DrawCaptionIcon(pixels, width, height, stride, canvas, qx + (qr - headlineWidth) / 2,
                sy + plan.Clearance + (int)Math.Round(centre - iconSize / 2.0 - (caption.Icon == QrCanvasCaptionIcon.Camera ? first.Size * .05 : 0)), iconSize, foreground);
        }
        if (plan.UrlShape is { } url) {
            var uy = qy + qr + plan.Gap + (top ? 0 : plan.Height) + plan.Clearance;
            PaintCaptionShape(pixels, width, height, stride, url, qx + (qr - (int)Math.Ceiling(url.InkBounds.Width)) / 2, uy, ink);
        }
    }

    private static bool IsDark(Rgba32 c) => c.R * 299 + c.G * 587 + c.B * 114 < 128000;

}

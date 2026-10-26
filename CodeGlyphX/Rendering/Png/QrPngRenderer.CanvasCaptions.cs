using System;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static void DrawCaptionCanvas(byte[] pixels, int width, int height, int stride, QrCanvasOptions canvas,
        int x, int y, int w, int h, int qx, int qy, int qr) {
        var caption = canvas.Caption;
        var plan = MeasureCaption(canvas, qr);
        var top = caption?.Position == QrCanvasCaptionPosition.Top;
        var sy = top ? qy - plan.Height - plan.Gap : qy + qr + plan.Gap;
        var defaultInk = IsDark(canvas.Background) ? new Rgba32(230, 232, 244) : new Rgba32(20, 24, 40);
        var ink = canvas.StrokeColor ?? canvas.Frame?.Color ?? defaultInk;
        var band = canvas.BandColor ?? ink;
        var foreground = caption?.Color ?? ink;
        var pad = CaptionCanvasPadding(canvas);
        var t = canvas.StrokeWidthPx > 0 ? canvas.StrokeWidthPx : Math.Max(3 * (caption?.RasterScale ?? 1), qr / 150);
        var left = x + pad / 2; var right = x + w - pad / 2;
        var upper = y + pad / 2; var lower = y + h - pad / 2;
        var radius = canvas.Template == QrCanvasTemplate.PhoneOutline ? w / 9 : Math.Max(pad, qr / 20);
        var pillWidth = Math.Min(qr, plan.Width + 2 * plan.Clearance);
        var pillLeft = qx + (qr - pillWidth) / 2;
        bool Rounded(double px, double py, double rx, double ry, double rw, double rh, double rr) {
            if (px < rx || px >= rx + rw || py < ry || py >= ry + rh) return false;
            rr = Math.Max(0, Math.Min(rr, Math.Min(rw, rh) / 2));
            var dx = Math.Max(0, Math.Abs(px - rx - rw / 2) - rw / 2 + rr);
            var dy = Math.Max(0, Math.Abs(py - ry - rh / 2) - rh / 2 + rr);
            return dx * dx + dy * dy <= rr * rr;
        }
        var template = canvas.Template;
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
                            fill = Rounded(xx, yy, pillLeft, sy, pillWidth, plan.Height, plan.Height / 2.0);
                            if (template == QrCanvasTemplate.SpeechBubble || canvas.CaptionTail) {
                                var tail = Math.Max(1, (int)Math.Ceiling(qr * .025));
                                var distance = top ? yy - sy - plan.Height : sy - yy;
                                fill |= distance >= 0 && distance < tail && Math.Abs(xx - (qx + qr / 2.0)) <= (tail - distance) * .65;
                            }
                        }
                        break;
                    case QrCanvasTemplate.TicketStub:
                        var divider = top ? sy + plan.Height + plan.Gap / 2 : sy - plan.Gap / 2;
                        var notch = Math.Max(8, qr * .022);
                        var dx = Math.Min(Math.Abs(xx - left), Math.Abs(xx - right));
                        var d = Math.Sqrt(dx * dx + (yy - divider) * (yy - divider));
                        paint = d >= notch && outer && !inner;
                        paint |= d >= notch && d <= notch + t && xx >= left && xx <= right;
                        paint |= Math.Abs(yy - divider) < t / 2.0 && xx > left + notch && xx < right - notch && (px - left) % Math.Max(4, t * 4) < t * 2;
                        break;
                    case QrCanvasTemplate.PhoneOutline:
                        var bezel = Math.Max(t * 2, qr / 65);
                        paint = outer && !Rounded(xx, yy, left + bezel, upper + bezel, right - left - 2 * bezel, lower - upper - 2 * bezel, radius - bezel);
                        paint |= Rounded(xx, yy, x + w * .36, upper + bezel, w * .28, bezel * 2.5, bezel);
                        paint |= Rounded(xx, yy, x + w * .39, lower - bezel * 3, w * .22, bezel / 2.0, bezel / 4.0);
                        break;
                    case QrCanvasTemplate.RoundedCardHeader:
                        paint = outer && !inner;
                        fill = outer && plan.Height > 0 && yy >= sy && yy < sy + plan.Height;
                        break;
                    case QrCanvasTemplate.CornerViewfinder:
                        var gap = Math.Max(3, t * 1.5);
                        var l = qx - gap - t; var r = qx + qr + gap + t;
                        var u = qy - gap - t; var b = qy + qr + gap + t;
                        var length = qr * .13;
                        paint = xx >= l && xx < r && yy >= u && yy < b &&
                            ((xx < l + t || xx >= r - t) && (yy < u + length || yy >= b - length)
                            || (yy < u + t || yy >= b - t) && (xx < l + length || xx >= r - length));
                        break;
                }
                if (paint) hits++;
                if (fill) bandHits++;
            }
            if (hits > 0) BlendPixel(pixels, stride, px, py, new Rgba32(ink.R, ink.G, ink.B, (byte)(ink.A * hits / 4)));
            if (bandHits > 0) BlendPixel(pixels, stride, px, py, new Rgba32(band.R, band.G, band.B, (byte)(band.A * bandHits / 4)));
        }
        if (caption is null || plan.Height == 0) return;
        if (template is QrCanvasTemplate.ScanMePill or QrCanvasTemplate.SpeechBubble or QrCanvasTemplate.RoundedCardHeader)
            foreground = caption.Color ?? (IsDark(band) ? Rgba32.White : Rgba32.Black);
        var textY = sy + plan.Clearance;
        foreach (var line in plan.Lines) {
            var lineWidth = (int)Math.Ceiling(line.Shape.InkBounds.Width);
            var tx = qx + (qr - lineWidth - plan.IconWidth) / 2 + plan.IconWidth;
            PaintCaptionShape(pixels, width, height, stride, line.Shape, tx, textY, foreground);
            textY += (int)Math.Ceiling(line.Shape.InkBounds.Height) + plan.LineGap;
        }
        if (plan.IconWidth > 0) DrawCaptionIcon(pixels, width, height, stride, canvas, qx + (qr - plan.Width) / 2,
            sy + plan.Clearance, Math.Max(4, plan.IconWidth * 3 / 4), foreground);
        if (caption.Url.Length > 0) {
            var size = plan.Lines[0].Size * .5;
            var url = caption.Font.Shape(caption.Url, size, caption.StrictText);
            size *= Math.Min(1, (qr - 2 * plan.Clearance) / Math.Max(1, url.InkBounds.Width));
            url = caption.Font.Shape(caption.Url, size, caption.StrictText);
            var uy = qy + qr + plan.Gap + (top ? 0 : plan.Height) + plan.Clearance;
            PaintCaptionShape(pixels, width, height, stride, url, qx + (qr - (int)Math.Ceiling(url.InkBounds.Width)) / 2, uy, ink);
        }
    }

    private static bool IsDark(Rgba32 c) => c.R * 299 + c.G * 587 + c.B * 114 < 128000;

    private static void DrawCaptionIcon(byte[] pixels, int width, int height, int stride, QrCanvasOptions canvas, int x, int y, int size, Rgba32 ink) {
        if (canvas.BandLogoPng is not null) {
            var logo = QrRasterLogoOptions.FromPng(canvas.BandLogoPng);
            BlitLogo(pixels, width, height, stride, x, y, size, size, logo);
            return;
        }
        var phone = canvas.Caption?.Icon == QrCanvasCaptionIcon.Phone;
        var t = Math.Max(1, size / 12);
        for (var yy = 0; yy < size; yy++) for (var xx = 0; xx < size; xx++) {
            var ring = phone ? xx >= size / 4 && xx < size * 3 / 4 && (xx < size / 4 + t || xx >= size * 3 / 4 - t || yy < t || yy >= size - t)
                : yy >= size / 5 && yy < size * 4 / 5 && (xx < t || xx >= size - t || yy < size / 5 + t || yy >= size * 4 / 5 - t);
            var dx = xx - size / 2.0; var dy = yy - size / 2.0;
            if (!phone) ring |= Math.Abs(Math.Sqrt(dx * dx + dy * dy) - size / 5.0) <= t;
            if (ring) BlendPixel(pixels, stride, x + xx, y + yy, ink);
        }
    }
}

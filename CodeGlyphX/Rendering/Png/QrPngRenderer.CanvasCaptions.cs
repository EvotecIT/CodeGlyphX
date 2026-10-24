using System;
using System.Collections.Generic;
using CodeGlyphX.Printing;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static int CaptionCanvasPadding(QrCanvasOptions canvas) => canvas.Template == QrCanvasTemplate.None
        ? canvas.PaddingPx : Math.Max(canvas.PaddingPx, 32);

    private static int CaptionCanvasHeight(QrCanvasOptions canvas) {
        var caption = canvas.Caption;
        if (caption is null || caption.Text.Length == 0) return 0;
        var shape = caption.Font.Shape(caption.Text, caption.FontSizePx, caption.StrictText);
        var height = Math.Ceiling(Math.Max(shape.InkBounds.Height, caption.FontSizePx) + caption.FontSizePx * .04);
        if (height > 4096) throw new ArgumentException("Caption font metrics exceed the supported height.");
        var strip = checked((int)height + caption.PaddingPx * 2);
        return canvas.Template == QrCanvasTemplate.None ? strip : Math.Max(strip, CaptionCanvasPadding(canvas));
    }

    private static void DrawCaptionCanvas(byte[] pixels, int width, int height, int stride, QrCanvasOptions canvas,
        int x, int y, int w, int h, int qx, int qy, int qr) {
        var caption = canvas.Caption;
        var strip = CaptionCanvasHeight(canvas);
        var top = caption?.Position == QrCanvasCaptionPosition.Top;
        var sy = top ? y + CaptionCanvasPadding(canvas) / 2 : y + h - strip - CaptionCanvasPadding(canvas) / 2;
        var ink = canvas.Frame?.Color ?? new Rgba32(20, 24, 40);
        var foreground = caption?.Color ?? ink;
        var pad = CaptionCanvasPadding(canvas);
        var t = Math.Max(2, pad / 10);
        var left = x + pad / 2; var right = x + w - pad / 2;
        var upper = y + pad / 2; var lower = y + h - pad / 2;
        var radius = pad * 3 / 4;
        bool Rounded(int px, int py, int rx, int ry, int rw, int rh, int rr) =>
            px >= rx && px < rx + rw && py >= ry && py < ry + rh &&
            InsideRounded(px, py, rx, ry, rx + rw - 1, ry + rh - 1, rr, rr * rr);
        var template = canvas.Template;
        // Paint outside the entire symbol rectangle, including its quiet zone.
        if (template != QrCanvasTemplate.None) for (var py = y; py < y + h; py++) for (var px = x; px < x + w; px++) {
            if (px >= qx && px < qx + qr && py >= qy && py < qy + qr) continue;
            var outer = Rounded(px, py, left, upper, right - left, lower - upper, radius);
            var inner = Rounded(px, py, left + t, upper + t, right - left - 2 * t, lower - upper - 2 * t, radius - t);
            var paint = false;
            switch (template) {
                case QrCanvasTemplate.ScanMePill:
                case QrCanvasTemplate.SpeechBubble:
                    if (strip > 0) {
                        var gap = Math.Max(4, caption!.PaddingPx / 2);
                        paint = Rounded(px, py, left, sy + gap, right - left, strip - gap * 2, (strip - gap * 2) / 2);
                        if (template == QrCanvasTemplate.SpeechBubble) {
                            var tipY = top ? sy + strip : sy;
                            var dy = top ? tipY - py : py - tipY;
                            paint |= dy >= 0 && dy < gap * 2 && Math.Abs(px - (x + w / 2)) <= dy;
                        }
                    }
                    break;
                case QrCanvasTemplate.TicketStub:
                    var divider = top ? sy + strip : sy;
                    var notch = Math.Max(5, pad / 4);
                    var notchCut = Math.Abs(py - divider) < notch &&
                        (px < left + notch || px >= right - notch);
                    paint = !notchCut && ((outer && !inner) || (Math.Abs(py - divider) < t && px >= left && px < right && (px - left) % 12 < 6));
                    break;
                case QrCanvasTemplate.PhoneOutline:
                    paint = outer && !inner;
                    paint |= Math.Abs(px - (x + w / 2)) < w / 10 &&
                        ((py >= upper + t * 2 && py < upper + t * 3) || (py >= lower - t * 3 && py < lower - t * 2));
                    break;
                case QrCanvasTemplate.RoundedCardHeader:
                    paint = outer && (!inner || (strip > 0 && py >= sy && py < sy + strip));
                    break;
                case QrCanvasTemplate.CornerViewfinder:
                    var length = Math.Max(18, qr / 7);
                    paint = (outer && !inner) && ((px < left + length || px >= right - length) && (py < upper + length || py >= lower - length));
                    break;
            }
            if (paint) BlendPixel(pixels, stride, px, py, ink);
        }
        if (caption is null || strip == 0) return;
        if (template is QrCanvasTemplate.ScanMePill or QrCanvasTemplate.SpeechBubble or QrCanvasTemplate.RoundedCardHeader)
            foreground = caption.Color ?? (ink.R * 299 + ink.G * 587 + ink.B * 114 < 128000 ? Rgba32.White : Rgba32.Black);
        var size = (double)caption.FontSizePx;
        var shape = caption.Font.Shape(caption.Text, size, caption.StrictText);
        if (caption.Weight == QrCanvasCaptionWeight.Bold) shape = PrintLayoutBuilder.Strengthen(shape, size * .02);
        var available = w - 2 * pad - 2 * caption.PaddingPx;
        var ratio = Math.Min(1, available / Math.Max(1, shape.InkBounds.Width));
        if (available <= 0 || size * ratio < 8 * caption.RasterScale) throw new ArgumentException("Caption cannot fit readably. Shorten it, reduce padding or enlarge the code.");
        var tw = Math.Max(1, (int)Math.Ceiling(shape.InkBounds.Width * ratio));
        var th = Math.Max(1, (int)Math.Ceiling(shape.InkBounds.Height * ratio));
        var contours = new List<IReadOnlyList<BarcodePoint>>();
        foreach (var contour in shape.Contours) {
            var points = new List<BarcodePoint>();
            foreach (var p in contour) points.Add(new BarcodePoint((p.X - shape.InkBounds.X) * ratio / 10, (p.Y - shape.InkBounds.Y) * ratio / 10));
            contours.Add(points);
        }
        // Reuse the print owner's compound-contour antialiasing and resource guards.
        var paths = new List<BarcodePrintPath> { new(contours, BarcodePaint.Solid(foreground), "caption") };
        var layout = new BarcodePrintLayout(null, 254, .1, new BarcodeBounds(0, 0, tw / 10D, th / 10D), paths,
            new List<BarcodePrintText>(), new List<BarcodePrintFinding>(), BarcodePaint.Solid(Rgba32.Transparent));
        var textPixels = BarcodePrintRasterizer.Render(layout, null, out var rw, out var rh);
        var tx = x + (w - tw) / 2; var ty = sy + (strip - th) / 2;
        for (var py = 0; py < rh; py++) for (var px = 0; px < rw; px++) {
            var at = (py * rw + px) * 4;
            if (tx + px < 0 || ty + py < 0 || tx + px >= width || ty + py >= height) continue;
            BlendPixel(pixels, stride, tx + px, ty + py, new Rgba32(textPixels[at], textPixels[at + 1], textPixels[at + 2], textPixels[at + 3]));
        }
    }
}

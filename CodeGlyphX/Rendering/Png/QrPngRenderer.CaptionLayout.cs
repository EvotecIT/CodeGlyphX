using System;
using System.Collections.Generic;
using CodeGlyphX.Printing;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private sealed class CaptionPlan {
        internal readonly List<(BarcodeTextShape Shape, double Size)> Lines = new();
        internal int Width, Height, Clearance, Gap, IconWidth, UrlHeight, LineGap, CapHeight;
    }

    private static int CaptionCanvasPadding(QrCanvasOptions canvas) {
        var minimum = canvas.Template == QrCanvasTemplate.None ? canvas.PaddingPx : Math.Max(canvas.PaddingPx, 32 * (canvas.Caption?.RasterScale ?? 1));
        minimum = Math.Max(minimum, canvas.PanelGlowRadiusPx > 0 ? checked(canvas.PanelGlowRadiusPx + 2) : 0);
        minimum = Math.Max(minimum, canvas.SecondaryPanelGlowRadiusPx > 0 ? checked(canvas.SecondaryPanelGlowRadiusPx + 2) : 0);
        return Math.Max(minimum, canvas.StackedPanelOffsetPx > 0 ? checked(canvas.StackedPanelOffsetPx + canvas.ShadowBlurPx + 2) : 0);
    }

    private static int CaptionCanvasPadding(QrCanvasOptions canvas, int qr) => canvas.Template switch {
        QrCanvasTemplate.PhoneOutline => Math.Max(CaptionCanvasPadding(canvas), (int)Math.Ceiling(qr * .161)),
        // The inner edge of the corner stroke must clear the entire square quiet zone.
        QrCanvasTemplate.TicketStub => Math.Max(CaptionCanvasPadding(canvas), (int)Math.Ceiling(qr * .066)),
        QrCanvasTemplate.RoundedCardHeader => Math.Max(CaptionCanvasPadding(canvas), (int)Math.Ceiling(qr * .065)),
        _ => CaptionCanvasPadding(canvas)
    };

    private static CaptionPlan MeasureCaption(QrCanvasOptions canvas, int qr) {
        var plan = new CaptionPlan();
        var caption = canvas.Caption;
        if (caption is null || caption.Text.Length == 0) return plan;
        var capShape = caption.Font.Shape("H", 1, strict: false);
        var capUnit = Math.Max(.1, capShape.MissingGlyphs.Count == 0 ? capShape.InkBounds.Height
            : caption.Font.Shape(caption.Text, 1, caption.StrictText).InkBounds.Height);
        var size = caption.FontSizePx > 0 ? caption.FontSizePx : qr * .045 / capUnit;
        var cap = size * capUnit;
        plan.Clearance = Math.Max(caption.PaddingPx, (int)Math.Ceiling(cap));
        plan.Gap = Math.Max(4 * caption.RasterScale, (int)Math.Ceiling(qr * .012));
        if (canvas.Template == QrCanvasTemplate.SpeechBubble || canvas.CaptionTail) plan.Gap = Math.Max(plan.Gap, (int)Math.Ceiling(cap * 1.2));
        if (canvas.Template == QrCanvasTemplate.TicketStub) plan.Gap = Math.Max(plan.Gap, (int)Math.Ceiling(qr * .07));
        if (canvas.Template == QrCanvasTemplate.CornerViewfinder) plan.Gap = Math.Max(plan.Gap, (int)Math.Ceiling(qr * .05));
        plan.IconWidth = caption.Icon != QrCanvasCaptionIcon.None || canvas.BandLogoPng is not null ? (int)Math.Ceiling(cap * 1.6) : 0;
        var available = qr - 2 * plan.Clearance - plan.IconWidth;
        if (available <= 0) throw new ArgumentException("Caption padding leaves no readable text area.");
        BarcodeTextShape Shape(string text, double em) {
            var shaped = caption.Font.Shape(text, em, caption.StrictText);
            return caption.Weight == QrCanvasCaptionWeight.Bold ? PrintLayoutBuilder.Strengthen(shaped, em * .02) : shaped;
        }
        var whole = Shape(caption.Text, size);
        var floor = Math.Max(8 * caption.RasterScale, size * .75);
        if (whole.InkBounds.Width > available) {
            var fitted = size * available / whole.InkBounds.Width;
            if (fitted >= floor) size = fitted;
            else if (caption.Subline.Length == 0) {
                // Try word boundaries once, choosing the most balanced pair at the readable floor.
                var best = double.MaxValue; var split = -1;
                for (var i = 1; i < caption.Text.Length - 1; i++) if (caption.Text[i] == ' ') {
                    var a = Shape(caption.Text.Substring(0, i).Trim(), floor);
                    var b = Shape(caption.Text.Substring(i + 1).Trim(), floor);
                    var widest = Math.Max(a.InkBounds.Width, b.InkBounds.Width);
                    if (widest <= available && widest < best) { best = widest; split = i; }
                }
                if (split < 0) throw new ArgumentException("Caption cannot fit readably in two lines. Shorten it or enlarge the code.");
                plan.Lines.Add((Shape(caption.Text.Substring(0, split).Trim(), floor), floor));
                plan.Lines.Add((Shape(caption.Text.Substring(split + 1).Trim(), floor), floor));
            } else throw new ArgumentException("Caption headline cannot fit above its subline. Shorten it or enlarge the code.");
        }
        if (plan.Lines.Count == 0) plan.Lines.Add((Shape(caption.Text, size), size));
        plan.CapHeight = Math.Max(1, (int)Math.Ceiling(plan.Lines[0].Size * capUnit));
        if (caption.Subline.Length > 0) {
            var subSize = size * .65;
            var sub = Shape(caption.Subline, subSize);
            if (sub.InkBounds.Width > available) {
                subSize *= available / sub.InkBounds.Width;
                if (subSize < 8 * caption.RasterScale) throw new ArgumentException("Caption subline cannot fit readably.");
                sub = Shape(caption.Subline, subSize);
            }
            plan.Lines.Add((sub, subSize));
        }
        var textHeight = 0.0;
        foreach (var line in plan.Lines) {
            plan.Width = Math.Max(plan.Width, (int)Math.Ceiling(line.Shape.InkBounds.Width));
            textHeight += Math.Ceiling(line.Shape.InkBounds.Height) + Math.Ceiling(cap * .35);
        }
        plan.Width += plan.IconWidth;
        plan.LineGap = (int)Math.Ceiling(cap * .35);
        plan.Height = checked((int)Math.Ceiling(textHeight - Math.Ceiling(cap * .35)) + 2 * plan.Clearance);
        if (caption.Url.Length > 0) {
            var urlSize = size * .5;
            var url = Shape(caption.Url, urlSize);
            urlSize *= Math.Min(1, (qr - 2 * plan.Clearance) / Math.Max(1, url.InkBounds.Width));
            if (urlSize < 8 * caption.RasterScale) throw new ArgumentException("Fallback URL cannot fit readably. Shorten it or enlarge the code.");
            plan.UrlHeight = (int)Math.Ceiling(Shape(caption.Url, urlSize).InkBounds.Height) + 2 * plan.Clearance;
        }
        if (plan.Height > 4096 * caption.RasterScale) throw new ArgumentException("Caption font metrics exceed the supported height.");
        return plan;
    }

    private static int CaptionCanvasHeight(QrCanvasOptions canvas, int qr) {
        var plan = MeasureCaption(canvas, qr);
        return plan.Height == 0 ? 0 : checked(plan.Height + plan.Gap + plan.UrlHeight);
    }

    private static int CaptionCanvasBodyHeight(QrCanvasOptions canvas, int qr) {
        var ordinary = qr + 2 * CaptionCanvasPadding(canvas, qr) + CaptionCanvasHeight(canvas, qr);
        return canvas.Template == QrCanvasTemplate.PhoneOutline
            ? Math.Max(ordinary, (int)Math.Ceiling((qr + 2 * CaptionCanvasPadding(canvas, qr)) * 19.5 / 9)) : ordinary;
    }

    private static void PaintCaptionShape(byte[] pixels, int width, int height, int stride, BarcodeTextShape shape, int tx, int ty, Rgba32 color) {
        var tw = Math.Max(1, (int)Math.Ceiling(shape.InkBounds.Width));
        var th = Math.Max(1, (int)Math.Ceiling(shape.InkBounds.Height));
        var contours = new List<IReadOnlyList<BarcodePoint>>();
        foreach (var contour in shape.Contours) {
            var points = new List<BarcodePoint>();
            foreach (var p in contour) points.Add(new BarcodePoint((p.X - shape.InkBounds.X) / 10, (p.Y - shape.InkBounds.Y) / 10));
            contours.Add(points);
        }
        var layout = new BarcodePrintLayout(null, 254, .1, new BarcodeBounds(0, 0, tw / 10D, th / 10D),
            new List<BarcodePrintPath> { new(contours, BarcodePaint.Solid(color), "caption") },
            new List<BarcodePrintText>(), new List<BarcodePrintFinding>(), BarcodePaint.Solid(Rgba32.Transparent));
        var text = BarcodePrintRasterizer.Render(layout, null, out var rw, out var rh);
        for (var y = 0; y < rh; y++) for (var x = 0; x < rw; x++) {
            if (tx + x < 0 || ty + y < 0 || tx + x >= width || ty + y >= height) continue;
            var at = (y * rw + x) * 4;
            BlendPixel(pixels, stride, tx + x, ty + y, new Rgba32(text[at], text[at + 1], text[at + 2], text[at + 3]));
        }
    }
}

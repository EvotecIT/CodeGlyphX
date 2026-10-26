using System;
using System.Collections.Generic;
using CodeGlyphX.Printing;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    // Icons are independent outlines, so caller fonts need no private-use or symbol glyphs.
    private static void DrawCaptionIcon(byte[] pixels, int width, int height, int stride,
        QrCanvasOptions canvas, int x, int y, int size, Rgba32 ink) {
        if (canvas.BandLogoPng is not null) {
            BlitLogo(pixels, width, height, stride, x, y, size, size, QrRasterLogoOptions.FromPng(canvas.BandLogoPng));
            return;
        }
        var contours = new List<IReadOnlyList<BarcodePoint>>();
        var path = new List<BarcodePoint>();
        void Point(double px, double py) => path.Add(new BarcodePoint(px * size, py * size));
        void Curve(double ax, double ay, double bx, double by, double ex, double ey) {
            var start = path[path.Count - 1];
            for (var i = 1; i <= 16; i++) {
                var t = i / 16D; var u = 1 - t;
                path.Add(new BarcodePoint(u * u * u * start.X + (3 * u * u * t * ax + 3 * u * t * t * bx + t * t * t * ex) * size,
                    u * u * u * start.Y + (3 * u * u * t * ay + 3 * u * t * t * by + t * t * t * ey) * size));
            }
        }
        if (canvas.Caption?.Icon == QrCanvasCaptionIcon.Phone) {
            Point(.06, .02); Point(.29, .02); Curve(.32, .02, .34, .04, .34, .08);
            Curve(.34, .18, .36, .26, .39, .33); Point(.24, .48);
            Curve(.32, .65, .46, .79, .63, .86); Point(.78, .71);
            Curve(.85, .74, .92, .76, .98, .76); Point(.98, .96);
            Curve(.98, 1, .95, 1, .92, 1); Curve(.40, 1, 0, .59, 0, .08);
            Curve(0, .04, .02, .02, .06, .02);
            contours.Add(path);
        } else {
            // Camera body, raised shutter housing and a circular lens cut-out.
            Point(0, .22); Point(.24, .22); Point(.32, .08); Point(.66, .08);
            Point(.74, .22); Point(1, .22); Point(1, .92); Point(0, .92);
            contours.Add(path);
            var lens = new List<BarcodePoint>();
            for (var i = 0; i < 64; i++) {
                var a = -i * Math.PI * 2 / 64;
                lens.Add(new BarcodePoint((.5 + .23 * Math.Cos(a)) * size, (.56 + .23 * Math.Sin(a)) * size));
            }
            contours.Add(lens);
        }
        var shape = new BarcodeTextShape("", size, size, 0, contours,
            new List<int>(), false);
        var scale = size / shape.InkBounds.Height;
        var fitted = new List<IReadOnlyList<BarcodePoint>>();
        foreach (var contour in shape.Contours) {
            var points = new List<BarcodePoint>();
            foreach (var point in contour) points.Add(new BarcodePoint(point.X * scale, point.Y * scale));
            fitted.Add(points);
        }
        shape = new BarcodeTextShape("", size, size, 0, fitted);
        PaintCaptionShape(pixels, width, height, stride, shape, x, y, ink);
    }
}

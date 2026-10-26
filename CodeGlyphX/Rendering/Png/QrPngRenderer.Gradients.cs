using System;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static Rgba32 GetGradientColor(GradientInfo gradient, int px, int py, int originX, int originY) {
        var u = (px - originX) * gradient.InvSizeX;
        var v = (py - originY) * gradient.InvSizeY;
        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;

        double t = gradient.Type switch {
            QrGradientType.Horizontal => u,
            QrGradientType.Vertical => v,
            QrGradientType.DiagonalDown => (u + v) * 0.5,
            QrGradientType.DiagonalUp => (u + (1 - v)) * 0.5,
            QrGradientType.Radial => GetRadialT(u, v, gradient.CenterX, gradient.CenterY, gradient.MaxDist),
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
            QrGradientType.Horizontal => u,
            QrGradientType.Vertical => v,
            QrGradientType.DiagonalDown => (u + v) * 0.5,
            QrGradientType.DiagonalUp => (u + (1 - v)) * 0.5,
            QrGradientType.Radial => GetRadialT(u, v, gradient.CenterX, gradient.CenterY, gradient.MaxDist),
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
        return gradient.Samples[(int)Math.Round(t * 1024)];
    }

    private readonly struct GradientInfo {
        public Rgba32[] Samples { get; }
        public QrGradientType Type { get; }
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

        public GradientInfo(QrGradientOptions gradient, int sizeX, int sizeY) {
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
            Samples = new Rgba32[1025];
            for (var i = 0; i < Samples.Length; i++) Samples[i] = QrColorInterpolation.Interpolate(StartColor, EndColor, i / 1024.0, gradient.Interpolation);
            MaxDist = Type == QrGradientType.Radial ? ComputeMaxDist(CenterX, CenterY) : 0.0;
        }
    }

}

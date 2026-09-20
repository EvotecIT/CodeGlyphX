using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Aspect-preserving image mapping and bilinear sampling with alpha flattened before interpolation.</summary>
internal sealed class QrImageSampler {
    private readonly byte[] _rgba;
    private readonly int _width;
    private readonly int _height;
    private readonly double _scale;
    private readonly double _offsetX;
    private readonly double _offsetY;

    internal QrImageSampler(byte[] rgba, int width, int height, int areaSize, QrImageCompositionOptions options) {
        _rgba = rgba;
        _width = width;
        _height = height;
        _scale = (options.Fit == QrImageFit.Cover
            ? Math.Max(areaSize / (double)width, areaSize / (double)height)
            : Math.Min(areaSize / (double)width, areaSize / (double)height)) * options.ImageZoom;
        _offsetX = (areaSize - width * _scale) * options.ImagePositionX;
        _offsetY = (areaSize - height * _scale) * options.ImagePositionY;
    }

    internal void Sample(double px, double py, out double r, out double g, out double b) {
        var x = (px + 0.5 - _offsetX) / _scale - 0.5;
        var y = (py + 0.5 - _offsetY) / _scale - 0.5;
        if (x < -0.5 || y < -0.5 || x >= _width - 0.5 || y >= _height - 0.5) {
            r = g = b = 255;
            return;
        }
        x = Math.Max(0, Math.Min(_width - 1, x));
        y = Math.Max(0, Math.Min(_height - 1, y));
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(_width - 1, x0 + 1);
        var y1 = Math.Min(_height - 1, y0 + 1);
        var fx = x - x0;
        var fy = y - y0;
        var p00 = (y0 * _width + x0) * 4;
        var p10 = (y0 * _width + x1) * 4;
        var p01 = (y1 * _width + x0) * 4;
        var p11 = (y1 * _width + x1) * 4;
        r = Channel(0);
        g = Channel(1);
        b = Channel(2);
        double Channel(int channel) =>
            (Flatten(p00, channel) * (1 - fx) + Flatten(p10, channel) * fx) * (1 - fy)
            + (Flatten(p01, channel) * (1 - fx) + Flatten(p11, channel) * fx) * fy;
        double Flatten(int index, int channel) => 255 + (_rgba[index + channel] - 255) * (_rgba[index + 3] / 255.0);
    }
}

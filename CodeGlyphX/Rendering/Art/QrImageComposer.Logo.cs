using System;
using System.Threading;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// Draws a requested logo on a rounded plate over the composed symbol. The plate covers exactly the knockout chosen by
    /// the shared logo budget (function patterns excluded, codewords counted per block), so composition throws rather than
    /// placing a logo the correction budget cannot carry.
    /// </summary>
    private sealed class LogoPainter {
        private readonly QrLogoBudget _budget;
        private readonly byte[] _rgba;
        private readonly int _width, _height, _moduleSize;
        private readonly Rgba32 _plate;

        internal LogoPainter(QrCode qr, QrImageLogoOptions options, int moduleSize, Rgba32 plate) {
            _budget = QrArt.FindLogoBudget(qr, options.MaximumSideModules, options.PaddingModules,
                options.ReservedErrorsPerBlock, options.MinimumLogoSideModules, LogoPlacement.Auto)
                ?? throw new InvalidOperationException("No readable logo fits the functional-pattern and per-block correction budgets. "
                    + "Reduce MinimumLogoSideModules or ReservedErrorsPerBlock, or use a longer payload or higher MinimumVersion.");
            _rgba = ImageReader.DecodeRgba32(options.Image!, out _width, out _height);
            _moduleSize = moduleSize;
            _plate = plate;
        }

        internal void Paint(byte[] pixels, int side, int symbolX, int symbolY, CancellationToken cancellationToken) {
            var m = _moduleSize;
            var plateX = symbolX + _budget.XModules * m;
            var plateY = symbolY + _budget.YModules * m;
            var plateSide = _budget.KnockoutSideModules * m;
            var radius = Math.Min(plateSide / 2.0, 0.75 * m);
            var logoSide = _budget.LogoSideModules * m;
            var scale = Math.Min(logoSide / (double)_width, logoSide / (double)_height);
            var drawWidth = _width * scale;
            var drawHeight = _height * scale;
            var logoX = plateX + (plateSide - drawWidth) / 2;
            var logoY = plateY + (plateSide - drawHeight) / 2;
            for (var y = plateY; y < plateY + plateSide; y++) {
                cancellationToken.ThrowIfCancellationRequested();
                for (var x = plateX; x < plateX + plateSide; x++) {
                    // Rounded plate corners, antialiased over one pixel.
                    var qx = Math.Abs(x + 0.5 - plateX - plateSide / 2.0) - (plateSide / 2.0 - radius);
                    var qy = Math.Abs(y + 0.5 - plateY - plateSide / 2.0) - (plateSide / 2.0 - radius);
                    var ox = Math.Max(qx, 0); var oy = Math.Max(qy, 0);
                    var distance = Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - radius;
                    var plateWeight = Math.Max(0, Math.Min(1, 0.5 - distance));
                    if (plateWeight <= 0) continue;
                    double r = _plate.R, g = _plate.G, b = _plate.B;
                    var lx = (x + 0.5 - logoX) / scale - 0.5;
                    var ly = (y + 0.5 - logoY) / scale - 0.5;
                    if (lx >= -0.5 && ly >= -0.5 && lx < _width - 0.5 && ly < _height - 0.5) Over(lx, ly, ref r, ref g, ref b);
                    var i = (y * side + x) * 4;
                    pixels[i] = ToByte(pixels[i] + (r - pixels[i]) * plateWeight);
                    pixels[i + 1] = ToByte(pixels[i + 1] + (g - pixels[i + 1]) * plateWeight);
                    pixels[i + 2] = ToByte(pixels[i + 2] + (b - pixels[i + 2]) * plateWeight);
                }
            }
        }

        // Bilinear logo sample with premultiplied alpha, composed over the plate colour.
        private void Over(double x, double y, ref double r, ref double g, ref double b) {
            x = Math.Max(0, Math.Min(_width - 1, x));
            y = Math.Max(0, Math.Min(_height - 1, y));
            var x0 = (int)x; var y0 = (int)y;
            var x1 = Math.Min(_width - 1, x0 + 1); var y1 = Math.Min(_height - 1, y0 + 1);
            var fx = x - x0; var fy = y - y0;
            double sr = 0, sg = 0, sb = 0, sa = 0;
            Add(x0, y0, (1 - fx) * (1 - fy));
            Add(x1, y0, fx * (1 - fy));
            Add(x0, y1, (1 - fx) * fy);
            Add(x1, y1, fx * fy);
            r = sr + r * (1 - sa);
            g = sg + g * (1 - sa);
            b = sb + b * (1 - sa);

            void Add(int px, int py, double weight) {
                var i = (py * _width + px) * 4;
                var alpha = _rgba[i + 3] / 255.0 * weight;
                sr += _rgba[i] * alpha;
                sg += _rgba[i + 1] * alpha;
                sb += _rgba[i + 2] * alpha;
                sa += alpha;
            }
        }

        private static byte ToByte(double value) => (byte)Math.Round(value < 0 ? 0 : value > 255 ? 255 : value);
    }
}

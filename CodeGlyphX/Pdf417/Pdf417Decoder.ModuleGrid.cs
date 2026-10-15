using System;
using System.Threading;
using CodeGlyphX.Internal;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Pixel spans remain on the synchronous decoder's stack. Only sampled bits
    // and sampling parameters are shared by the crop/orientation views.
    internal readonly ref struct ModuleGrid {
        private readonly PixelSpan _pixels;
        private readonly BitMatrix? _matrix;
        private readonly GridSamples? _samples;
        private readonly BitMatrixRegion _region;

        internal ModuleGrid(BitMatrix matrix) {
            if (matrix is null) throw new ArgumentNullException(nameof(matrix));
            _matrix = matrix;
            _pixels = default!;
            _samples = null;
            _region = new BitMatrixRegion(matrix.Width, matrix.Height);
        }

        private ModuleGrid(PixelSpan pixels, GridSamples samples) {
            _pixels = pixels;
            _samples = samples;
            _matrix = null;
            _region = new BitMatrixRegion(samples.WidthModules, samples.HeightModules);
        }

        private ModuleGrid(ModuleGrid source, BitMatrixRegion region) {
            _pixels = source._pixels;
            _matrix = source._matrix;
            _samples = source._samples;
            _region = region;
        }

        internal int Width => _region.Width;
        internal int Height => _region.Height;
        internal bool IsAborted => _samples?.Aborted ?? false;
        internal ModuleGrid Rotate(int turns) => new(this, _region.Rotate(turns));
        internal ModuleGrid Mirror() => new(this, _region.Mirror());
        internal ModuleGrid Crop(int left, int top, int width, int height) => new(this, _region.Crop(left, top, width, height));
        internal bool IsSameRegion(ModuleGrid other) => ReferenceEquals(_matrix, other._matrix) &&
            ReferenceEquals(_samples, other._samples) && _region.IsSame(other._region);

        internal bool this[int x, int y] {
            get {
                _region.GetCoordinates(x, y, out var sourceX, out var sourceY);
                if (_matrix is not null) return _matrix.Get(sourceX, sourceY);
                var samples = _samples!;
                // Poll even on cache hits: retrying an orientation must not hide
                // cancellation behind an already sampled candidate.
                if ((samples.ReadCount++ & 31) == 0 && samples.Budget.ShouldAbort(samples.Token)) samples.Aborted = true;
                if (samples.Aborted) return false;
                var index = sourceY * samples.WidthModules + sourceX;
                var word = index >> 5;
                var mask = 1u << (index & 31);
                if ((samples.Known[word] & mask) == 0) {
                    if (Sample(sourceX, sourceY, samples)) samples.Values[word] |= mask;
                    samples.Known[word] |= mask;
                }
                return (samples.Values[word] & mask) != 0;
            }
        }

        private bool Sample(int x, int y, GridSamples s) {
            bool dark;
#if NET8_0_OR_GREATER
            if (s.Perspective) {
                s.Transform.Transform(x + 0.5 + s.Left, y + 0.5 + s.Top, out var px, out var py);
                // The eager sampler leaves NaN cells light, including inverted grids.
                if (double.IsNaN(px) || double.IsNaN(py)) return false;
                dark = IsDarkAverage(_pixels, s.Width, s.Height, s.Stride, s.Format, px, py, s.Threshold);
            } else
#endif
            {
                double px;
                double py;
                if (s.Sheared) {
                    // Keep the original sequence of operations and midpoint rounding.
                    var rowShift = (y - (s.HeightModules - 1) / 2.0) * s.Shear * s.StepX;
                    px = s.Left + rowShift + x * s.StepX + s.StepX / 2.0;
                    py = s.Top + y * s.StepY + s.StepY / 2.0;
                } else {
                    px = s.Left + (x + 0.5) * s.StepX;
                    py = s.Top + (y + 0.5) * s.StepY;
                }
                var sx = (int)(s.RoundCenters ? Math.Round(px) : Math.Floor(px));
                var sy = (int)(s.RoundCenters ? Math.Round(py) : Math.Floor(py));
                sx = Clamp(sx, 0, s.Width - 1);
                sy = Clamp(sy, 0, s.Height - 1);
                dark = IsDark(_pixels, s.Width, s.Height, s.Stride, s.Format, sx, sy, s.Threshold);
            }
            return s.Invert ? !dark : dark;
        }

        internal static ModuleGrid Regular(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
            double left, double top, int widthModules, int heightModules, double stepX, double stepY,
            bool roundCenters, int threshold, bool invert, CancellationToken token, bool sheared = false, double shear = 0) {
            var samples = new GridSamples(width, height, stride, format, widthModules, heightModules, threshold, invert, token) {
                Left = left, Top = top, StepX = stepX, StepY = stepY, RoundCenters = roundCenters, Sheared = sheared, Shear = shear
            };
            return new ModuleGrid(pixels, samples);
        }

#if NET8_0_OR_GREATER
        internal static ModuleGrid Projected(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
            int widthModules, int heightModules, int threshold, bool invert, Qr.QrPerspectiveTransform transform,
            double offsetX, double offsetY, CancellationToken token) {
            var samples = new GridSamples(width, height, stride, format, widthModules, heightModules, threshold, invert, token) {
                Perspective = true, Transform = transform, Left = offsetX, Top = offsetY
            };
            return new ModuleGrid(pixels, samples);
        }
#endif

        public static implicit operator ModuleGrid(BitMatrix matrix) => new(matrix);
    }

    private sealed class GridSamples {
        internal readonly int Width, Height, Stride, WidthModules, HeightModules, Threshold;
        internal readonly PixelFormat Format;
        internal readonly bool Invert;
        internal readonly uint[] Known, Values;
        internal readonly CancellationToken Token;
        internal readonly DecodeBudgetState Budget;
        internal double Left, Top, StepX, StepY, Shear;
        internal bool RoundCenters, Sheared, Aborted;
        internal int ReadCount;
#if NET8_0_OR_GREATER
        internal bool Perspective;
        internal Qr.QrPerspectiveTransform Transform;
#endif

        internal GridSamples(int width, int height, int stride, PixelFormat format, int widthModules, int heightModules,
            int threshold, bool invert, CancellationToken token) {
            Width = width; Height = height; Stride = stride; Format = format;
            WidthModules = widthModules; HeightModules = heightModules; Threshold = threshold; Invert = invert;
            var cells = checked(widthModules * heightModules);
            Known = new uint[(cells + 31) / 32];
            Values = new uint[Known.Length];
            Token = token;
            Budget = DecodeBudget.Capture();
        }
    }
}

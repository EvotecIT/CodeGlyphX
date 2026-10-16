using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

/// <summary>Locates and samples the matrix, stacked-row and bar-state families handled by the module codecs.</summary>
internal static partial class SpecialistImageDecoder {
    internal static readonly SymbolFormat[] Formats = {
        SymbolFormat.D3Aqr, SymbolFormat.RmQrCode, SymbolFormat.HanXin, SymbolFormat.DotCode,
        SymbolFormat.Code16K, SymbolFormat.Code49, SymbolFormat.CodablockF, SymbolFormat.MicroPdf417,
        SymbolFormat.Gs1Composite, SymbolFormat.Mailmark4State, SymbolFormat.UspsIntelligentMail,
        SymbolFormat.RoyalMail4State, SymbolFormat.AustraliaPost, SymbolFormat.JapanPost, SymbolFormat.KixCode,
        SymbolFormat.Postnet, SymbolFormat.Planet, SymbolFormat.PharmacodeTwoTrack,
        SymbolFormat.FacingIdentificationMark, SymbolFormat.Daft, SymbolFormat.Flattermarken, SymbolFormat.PosiCode
    };

    internal static bool RequiresSpecialistScanner(SymbolFormat format) => format != SymbolFormat.PosiCode && Array.IndexOf(Formats, format) >= 0;

    internal sealed class ImageContext {
        internal readonly byte[] Mask;
        internal readonly int Width;
        internal readonly int Height;
        internal readonly List<ImageRegion>?[] Regions = new List<ImageRegion>?[8];
        internal ImageContext? Rectified;
        internal bool RectificationChecked;
        internal double Cos = 1, Sin, SourceCenterX, SourceCenterY, TargetCenterX, TargetCenterY;
        internal ImageContext(byte[] mask, int width, int height) { Mask = mask; Width = width; Height = height; }
    }

    internal static bool TryPrepare(byte[] rgba, int width, int height, bool allowUniform,
        CancellationToken token, out ImageContext context) {
        context = null!;
        if (!TryBinarize(rgba, width, height, token, allowUniform, out var mask)) return false;
        context = new ImageContext(mask, width, height);
        return true;
    }

    internal static bool TryDecode(ImageContext context, SymbolFormat format,
        ImageSamplingGrid? calibration, CancellationToken token, out DetectedSymbol symbol,
        out SymbolGeometry geometry, out bool inverted, out bool mirrored) {
        if (TryDecodeAligned(context, format, calibration, token, out symbol, out geometry, out inverted, out mirrored)) return true;
        if (calibration is not null || DecodeBudget.ShouldAbort(token)) return false;
        if (!context.RectificationChecked) {
            context.Rectified = Rectify(context, token);
            if (DecodeBudget.ShouldAbort(token)) return false;
            context.RectificationChecked = true;
        }
        var rectified = context.Rectified;
        if (rectified is null || !TryDecodeAligned(rectified, format, null, token, out symbol, out geometry, out inverted, out mirrored)) return false;
        geometry = new SymbolGeometry(MapRectified(rectified, geometry.TopLeft), MapRectified(rectified, geometry.TopRight),
            MapRectified(rectified, geometry.BottomRight), MapRectified(rectified, geometry.BottomLeft));
        return true;
    }

    private static bool TryDecodeAligned(ImageContext context, SymbolFormat format,
        ImageSamplingGrid? calibration, CancellationToken token, out DetectedSymbol symbol,
        out SymbolGeometry geometry, out bool inverted, out bool mirrored) {
        symbol = null!; geometry = null!; inverted = mirrored = false;
        var mask = context.Mask; var width = context.Width; var height = context.Height;
        for (var polarity = 0; polarity < 2; polarity++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var view = new MaskView(mask, width, height, 0, polarity != 0);
            if (calibration is not null) {
                view = new MaskView(mask, width, height, (4 - calibration.RotationDegrees / 90) & 3, polarity != 0);
                var full = new ImageRegion(0, 0, view.Width, view.Height);
                if (TrySample(view, full, calibration.Columns, calibration.Rows, token, out var grid)
                    && TryDecodeCalibratedGrid(grid, format, token, out symbol)) {
                    geometry = Geometry(view, full, 0, false); inverted = polarity != 0; return true;
                }
                continue;
            }
            // Flattermarken has no start/stop marks: trimming white cells destroys information.
            // Never manufacture a payload by guessing its cell origin or length.
            if (format == SymbolFormat.Flattermarken) continue;
            for (var rotation = 0; rotation < (format == SymbolFormat.D3Aqr ? 4 : 2); rotation++) {
                var viewRotation = (4 - rotation) & 3;
                view = new MaskView(mask, width, height, viewRotation, polarity != 0);
                var cacheIndex = polarity * 4 + viewRotation;
                var regions = context.Regions[cacheIndex] ?? FindRegions(view, token);
                if (DecodeBudget.ShouldAbort(token)) return false;
                context.Regions[cacheIndex] = regions;
                foreach (var region in regions) {
                    if (DecodeBudget.ShouldAbort(token)) return false;
                    if (TryRegion(view, region, format, token, out var sampledRegion, out symbol, out var turn, out mirrored)) {
                        geometry = Geometry(view, sampledRegion, turn, mirrored); inverted = polarity != 0; return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool TryRegion(MaskView view, ImageRegion region, SymbolFormat format,
        CancellationToken token, out ImageRegion sampledRegion, out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        sampledRegion = region; symbol = null!; rotation = 0; mirrored = false;
        if (IsBarState(format)) return TryBarStates(view, region, format, token, out symbol, out rotation, out mirrored);
        if (format == SymbolFormat.FacingIdentificationMark) {
            return TrySample(view, region, 17, 1, token, out var mark)
                && TryOrientations(mark, format, token, out symbol, out rotation, out mirrored);
        }
        if (format == SymbolFormat.Code16K || format == SymbolFormat.Code49 || format == SymbolFormat.CodablockF
            || format == SymbolFormat.MicroPdf417 || format == SymbolFormat.Gs1Composite || format == SymbolFormat.PosiCode)
            return TryStacked(view, region, format, token, out symbol, out rotation, out mirrored);
        return TryMatrix(view, region, format, token, out sampledRegion, out symbol, out rotation, out mirrored);
    }

    private static bool TryOrientations(BitMatrix modules, SymbolFormat format, CancellationToken token,
        out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        symbol = null!; rotation = 0; mirrored = false;
        for (var mirror = 0; mirror < 2; mirror++) {
            var grid = mirror == 0 ? modules : BitMatrixTransforms.MirrorHorizontal(modules);
            for (var turn = 0; turn < 4; turn++) {
                if (DecodeBudget.ShouldAbort(token)) return false;
                var success = format == SymbolFormat.D3Aqr
                    ? TryDecodeD3Canonical(grid, token, out symbol) : SymbolDecoder.TryDecode(grid, out symbol, format);
                if (success) {
                    rotation = turn; mirrored = mirror != 0; return true;
                }
                grid = BitMatrixTransforms.Rotate90(grid);
            }
        }
        return false;
    }

    private static bool TryDecodeD3Canonical(BitMatrix modules, CancellationToken token, out DetectedSymbol symbol) {
        symbol = null!;
        if (!D3Aqr.D3AqrDecoder.TryDecodeCanonical(modules, token, out var value)) return false;
        symbol = SymbolResultFactory.From(value);
        return true;
    }

    private static bool TryDecodeCalibratedGrid(BitMatrix modules, SymbolFormat format, CancellationToken token,
        out DetectedSymbol symbol) {
        if (format != SymbolFormat.D3Aqr) return SymbolDecoder.TryDecode(modules, out symbol, format);
        symbol = null!;
        // Preserve the calibrated path's module-decoder orientation support while forwarding its attempt token.
        if (!D3Aqr.D3AqrDecoder.TryDecode(modules, token, out var value)) return false;
        symbol = SymbolResultFactory.From(value);
        return true;
    }

    private static bool TryBinarize(byte[] rgba, int width, int height, CancellationToken token, bool allowUniform, out byte[] mask) {
        mask = new byte[checked(width * height)];
        var histogram = new int[256];
        for (var i = 0; i < mask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return false;
            var p = i * 4;
            var gray = BarcodeScanline.GetLuminance(rgba[p], rgba[p + 1], rgba[p + 2], rgba[p + 3]);
            mask[i] = gray;
            histogram[gray]++;
        }
        long sum = 0;
        for (var i = 0; i < 256; i++) sum += (long)i * histogram[i];
        long lowerSum = 0;
        var lowerCount = 0;
        var threshold = -1;
        var best = -1d;
        for (var i = 0; i < 255; i++) {
            lowerCount += histogram[i]; lowerSum += (long)i * histogram[i];
            var upperCount = mask.Length - lowerCount;
            if (lowerCount == 0 || upperCount == 0) continue;
            var difference = lowerSum / (double)lowerCount - (sum - lowerSum) / (double)upperCount;
            var variance = (double)lowerCount * upperCount * difference * difference;
            if (variance > best) { best = variance; threshold = i; }
        }
        if (threshold < 0) {
            if (!allowUniform) return false;
            threshold = 127;
        }
        for (var i = 0; i < mask.Length; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return false;
            mask[i] = (byte)(mask[i] <= threshold ? 1 : 0);
        }
        return true;
    }

    private static bool TrySample(MaskView view, ImageRegion region, int columns, int rows,
        CancellationToken token, out BitMatrix modules) {
        modules = null!;
        if (columns <= 0 || rows <= 0 || columns > region.Width || rows > region.Height
            || (long)columns * rows > 1048576) return false;
        modules = new BitMatrix(columns, rows);
        for (var y = 0; y < rows; y++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            for (var x = 0; x < columns; x++) {
                var dark = 0;
                // Interior samples tolerate fractional resampling and avoid antialiased cell edges.
                for (var sy = 0; sy < 3; sy++) for (var sx = 0; sx < 3; sx++) {
                    var px = region.X + Math.Min(region.Width - 1, (int)((x + .3 + sx * .2) * region.Width / columns));
                    var py = region.Y + Math.Min(region.Height - 1, (int)((y + .3 + sy * .2) * region.Height / rows));
                    if (view.Get(px, py)) dark++;
                }
                modules[x, y] = dark >= 5;
            }
        }
        return true;
    }

    private readonly struct MaskView {
        private readonly byte[] _mask;
        private readonly int _width;
        private readonly int _height;
        private readonly int _rotation;
        private readonly bool _invert;
        internal int Width => (_rotation & 1) == 0 ? _width : _height;
        internal int Height => (_rotation & 1) == 0 ? _height : _width;
        internal MaskView(byte[] mask, int width, int height, int rotation, bool invert) {
            _mask = mask; _width = width; _height = height; _rotation = rotation; _invert = invert;
        }
        internal bool Get(int x, int y) {
            var at = _rotation switch {
                0 => y * _width + x,
                1 => (_height - 1 - x) * _width + y,
                2 => (_height - 1 - y) * _width + _width - 1 - x,
                _ => x * _width + _width - 1 - y
            };
            return ((_mask[at] & 1) != 0) != _invert;
        }
        internal SymbolPoint Map(double x, double y) => _rotation switch {
            0 => new SymbolPoint(x, y), 1 => new SymbolPoint(y, _height - x),
            2 => new SymbolPoint(_width - x, _height - y), _ => new SymbolPoint(_width - y, x)
        };
    }

    private static SymbolGeometry Geometry(MaskView view, ImageRegion region, int turn, bool mirror) {
        var points = new[] { view.Map(region.X, region.Y), view.Map(region.X + region.Width, region.Y),
            view.Map(region.X + region.Width, region.Y + region.Height), view.Map(region.X, region.Y + region.Height) };
        if (mirror) points = new[] { points[1], points[0], points[3], points[2] };
        var start = (4 - turn) & 3;
        return new SymbolGeometry(points[start], points[(start + 1) & 3], points[(start + 2) & 3], points[(start + 3) & 3]);
    }
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixDecoder {
    private sealed class PerspectiveGrid {
        public readonly DataMatrixSymbolInfo Symbol;
        public readonly double Score;
        public readonly ImagePoint[] Corners;
        public PerspectiveGrid(DataMatrixSymbolInfo symbol, double score, ImagePoint[] corners) {
            Symbol = symbol;
            Score = score;
            Corners = corners;
        }
    }

    /// <summary>
    /// Tries a finder L and two timing edges after the cheap axis-aligned grids.
    /// Detection uses at most one megapixel, eight components per polarity,
    /// three grid dimensions per L, and a fixed set of corner refinements.
    /// All sampled grids still require region framing, ECC, and payload validity.
    /// </summary>
    private static bool TryDecodePerspective(PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, CancellationToken cancellationToken, out DataMatrixDecoded decoded,
        DataMatrixDecodeDiagnostics? diagnostics = null) {
        decoded = null!;
        if (DecodeBudget.ShouldAbort(cancellationToken) || width < 20 || height < 20 ||
            !HasValidPixelLayout(pixels.Length, width, height, stride, format)) return false;
        var scale = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((double)width * height / 1048576)));
        scale = Math.Max(scale, (int)Math.Ceiling(Math.Max(width, height) / 1024.0));
        var sampleWidth = (width - 1) / scale + 1;
        var sampleHeight = (height - 1) / scale + 1;
        var count = sampleWidth * sampleHeight;
        var mask = ArrayPool<byte>.Shared.Rent(count);
        try {
            for (var y = 0; y < sampleHeight; y++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                for (var x = 0; x < sampleWidth; x++) {
                    if ((x & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    mask[y * sampleWidth + x] = IsDark(pixels, width, height, stride, format, x * scale, y * scale) ? (byte)1 : (byte)0;
                }
            }
            for (var polarity = 0; polarity < 2; polarity++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var invert = polarity != 0;
                if (invert) for (var at = 0; at < count; at++) mask[at] &= 1;
                var components = FindImageComponents(mask, sampleWidth, sampleHeight, invert, cancellationToken);
                foreach (var component in components) {
                    for (var corner = 0; corner < 4; corner++) {
                        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                        var bl = component.Corner(corner);
                        var a = component.Corner((corner + 1) % 4);
                        var b = component.Corner((corner + 3) % 4);
                        if (ImageDistance(bl, a) < 20 || ImageDistance(bl, b) < 20 ||
                            !HasSolidImageEdge(mask, sampleWidth, sampleHeight, bl, a, invert) ||
                            !HasSolidImageEdge(mask, sampleWidth, sampleHeight, bl, b, invert)) continue;
                        for (var mirrored = 0; mirrored < 2; mirrored++) {
                            var tl = mirrored == 0 ? a : b;
                            var br = mirrored == 0 ? b : a;
                            var grids = FindPerspectiveGrids(mask, sampleWidth, sampleHeight, tl, bl, br, invert, cancellationToken);
                            foreach (var grid in grids) {
                                if (TryPerspectiveGrid(mask, sampleWidth, sampleHeight, grid, invert, cancellationToken, out decoded, diagnostics)) return true;
                            }
                        }
                    }
                }
            }
            return false;
        } finally {
            ArrayPool<byte>.Shared.Return(mask);
        }
    }

    private static List<PerspectiveGrid> FindPerspectiveGrids(byte[] mask, int width, int height,
        ImagePoint tl, ImagePoint bl, ImagePoint br, bool invert, CancellationToken cancellationToken) {
        var grids = new List<PerspectiveGrid>(3);
        var lengthX = ImageDistance(bl, br);
        var lengthY = ImageDistance(bl, tl);
        var predicted = new ImagePoint(tl.X + br.X - bl.X, tl.Y + br.Y - bl.Y);
        foreach (var symbol in DataMatrixSymbolInfo.All) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) break;
            var pitchX = lengthX / symbol.SymbolCols;
            var pitchY = lengthY / symbol.SymbolRows;
            if (pitchX < 1.5 || pitchY < 1.5 || pitchX / pitchY < 0.6 || pitchX / pitchY > 1.7) continue;
            var bestScore = 0.0;
            var bestCorner = predicted;
            for (var dx = -4; dx <= 4; dx++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return grids;
                for (var dy = -4; dy <= 4; dy++) {
                    var tr = new ImagePoint(predicted.X + dx * lengthX * 0.025, predicted.Y + dy * lengthY * 0.025);
                    if (!IsUsableQuadrilateral(tl, tr, br, bl, width, height)) continue;
                    var transform = PerspectiveTransform(symbol, tl, tr, br, bl);
                    var score = ScorePerspectiveClock(mask, width, height, transform, symbol, invert);
                    if (score > bestScore) { bestScore = score; bestCorner = tr; }
                }
            }
            if (bestScore < 0.8) continue;
            var position = 0;
            while (position < grids.Count && grids[position].Score >= bestScore) position++;
            if (position >= 3) continue;
            grids.Insert(position, new PerspectiveGrid(symbol, bestScore, new[] { tl, bestCorner, br, bl }));
            if (grids.Count > 3) grids.RemoveAt(3);
        }
        return grids;
    }

    private static QrPerspectiveTransform PerspectiveTransform(DataMatrixSymbolInfo symbol,
        ImagePoint tl, ImagePoint tr, ImagePoint br, ImagePoint bl) {
        return QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0, symbol.SymbolCols, 0, symbol.SymbolCols, symbol.SymbolRows, 0, symbol.SymbolRows,
            tl.X, tl.Y, tr.X, tr.Y, br.X, br.Y, bl.X, bl.Y);
    }

    private static bool IsUsableQuadrilateral(ImagePoint tl, ImagePoint tr, ImagePoint br, ImagePoint bl,
        int width, int height) {
        if (tr.X < 0 || tr.Y < 0 || tr.X > width - 1 || tr.Y > height - 1) return false;
        var sign = ImageCross(tl, tr, br);
        if (Math.Abs(sign) < 1) return false;
        return ImageCross(tr, br, bl) * sign > 0 && ImageCross(br, bl, tl) * sign > 0 && ImageCross(bl, tl, tr) * sign > 0;
    }

    private static double ImageCross(ImagePoint a, ImagePoint b, ImagePoint c) =>
        (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);

    private static double ScorePerspectiveClock(byte[] mask, int width, int height,
        QrPerspectiveTransform transform, DataMatrixSymbolInfo symbol, bool invert) {
        var hits = 0;
        for (var i = 0; i < symbol.SymbolCols; i++) {
            transform.Transform(i + 0.5, 0.5, out var x, out var y);
            if (ImageBit(mask, width, height, x, y, invert) == ((i & 1) == 0)) hits++;
        }
        for (var i = 0; i < symbol.SymbolRows; i++) {
            transform.Transform(symbol.SymbolCols - 0.5, i + 0.5, out var x, out var y);
            if (ImageBit(mask, width, height, x, y, invert) == ((i & 1) != 0)) hits++;
        }
        return (double)hits / (symbol.SymbolRows + symbol.SymbolCols);
    }

    private static bool TryPerspectiveGrid(byte[] mask, int width, int height, PerspectiveGrid grid,
        bool invert, CancellationToken cancellationToken, out DataMatrixDecoded decoded,
        DataMatrixDecodeDiagnostics? diagnostics) {
        decoded = null!;
        // Refine only the inferred timing corner; the three solid-L extrema
        // remain anchored in observed pixels. No data or parity bits are forced.
        for (var dx = -2; dx <= 2; dx++) {
            for (var dy = -2; dy <= 2; dy++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var q = grid.Corners;
                var tr = new ImagePoint(q[1].X + dx, q[1].Y + dy);
                if (!IsUsableQuadrilateral(q[0], tr, q[2], q[3], width, height)) continue;
                var transform = PerspectiveTransform(grid.Symbol, q[0], tr, q[2], q[3]);
                var modules = new BitMatrix(grid.Symbol.SymbolCols, grid.Symbol.SymbolRows);
                for (var y = 0; y < modules.Height; y++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    for (var x = 0; x < modules.Width; x++) {
                        transform.Transform(x + 0.5, y + 0.5, out var px, out var py);
                        modules[x, y] = ImageBit(mask, width, height, px, py, invert);
                    }
                }
                if (TryDecodeDetailedCore(new BitMatrixView(modules), cancellationToken, out decoded, diagnostics)) {
                    if (diagnostics is not null && (q[2].X - q[3].X) * (q[0].Y - q[3].Y) -
                        (q[2].Y - q[3].Y) * (q[0].X - q[3].X) > 0) diagnostics.MirroredTried = true;
                    return true;
                }
            }
        }
        return false;
    }
}

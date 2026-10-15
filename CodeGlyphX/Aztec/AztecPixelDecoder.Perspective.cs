using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Aztec;

internal static partial class AztecPixelDecoder {
    private sealed class BullseyeGrid {
        internal readonly QrPerspectiveTransform Transform;
        internal readonly int Side;
        internal readonly double Score;
        internal BullseyeGrid(QrPerspectiveTransform transform, int side, double score) {
            Transform = transform; Side = side; Score = score;
        }
    }

    /// <summary>
    /// Finds nested square rings away from the image centre and projects their
    /// data grid. Search uses at most 1024 pixels per axis, four thresholds, two
    /// polarities and eight ring candidates per pass. Metadata and payload ECC
    /// remain authoritative; ring similarity alone never produces a result.
    /// </summary>
    private static bool TryDecodePerspective(AztecGrayImage image, CancellationToken cancellationToken,
        out string value, AztecDecodeDiagnostics? diagnostics = null) {
        value = string.Empty;
        if (image.Width < 15 || image.Height < 15 || DecodeBudget.ShouldAbort(cancellationToken)) return false;
        var scale = Math.Max(1, (int)Math.Ceiling(Math.Max(image.Width, image.Height) / 1024.0));
        var width = (image.Width - 1) / scale + 1;
        var height = (image.Height - 1) / scale + 1;
        var count = width * height;
        var mask = ArrayPool<byte>.Shared.Rent(count);
        var extrema = ArrayPool<int>.Shared.Rent(2 * (width + height));
        try {
            var thresholds = BuildPerspectiveThresholds(image);
            for (var threshold = 0; threshold < thresholds.Length; threshold++) {
                for (var y = 0; y < height; y++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    for (var x = 0; x < width; x++)
                        mask[y * width + x] = image.Gray[y * scale * image.Width + x * scale] <= thresholds[threshold] ? (byte)1 : (byte)0;
                }
                for (var polarity = 0; polarity < 2; polarity++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    var invert = polarity != 0;
                    if (diagnostics is not null) {
                        diagnostics.AttemptCount++;
                        if (invert) diagnostics.InvertedTried = true;
                    }
                    if (invert) for (var i = 0; i < count; i++) mask[i] &= 1;
                    var grids = FindBullseyeGrids(mask, width, height, invert, extrema, cancellationToken);
                    foreach (var grid in grids) {
                        foreach (var candidate in Candidates) {
                            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                            if (candidate.Compact && grid.Side == 13) continue;
                            var modules = ProjectBullseyeGrid(mask, width, height, invert, grid, candidate.Size, cancellationToken);
                            if (TryDecodeSampledOrientations(modules, cancellationToken, out value, diagnostics)) {
                                if (diagnostics is not null) diagnostics.Success = true;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        } finally {
            ArrayPool<int>.Shared.Return(extrema);
            ArrayPool<byte>.Shared.Return(mask);
        }
    }

    // Perspective extrapolation is sensitive to where a blurred ring edge is
    // thresholded. Probe on either side of the contrast midpoint as well as Otsu.
    private static byte[] BuildPerspectiveThresholds(AztecGrayImage image) {
        Span<byte> values = stackalloc byte[4];
        var count = 0;
        var range = image.Max - image.Min;
        var midpoint = image.Min + range / 2;
        AddThreshold(ref values, ref count, image.Threshold);
        AddThreshold(ref values, ref count, (byte)midpoint);
        AddThreshold(ref values, ref count, (byte)(midpoint + range / 12));
        AddThreshold(ref values, ref count, (byte)(midpoint - range / 12));
        var result = new byte[count];
        values.Slice(0, count).CopyTo(result);
        return result;
    }

    private static List<BullseyeGrid> FindBullseyeGrids(byte[] mask, int width, int height, bool invert,
        int[] extrema, CancellationToken cancellationToken) {
        var grids = new List<BullseyeGrid>(8);
        BinaryImageComponents.Visit(mask, width, height, invert, cancellationToken, (pixels, length, touchesEdge) => {
            if (touchesEdge || length < 30 || length > width * height / 2) return false;
            if (!TryFitRing(pixels, length, width, height, extrema, cancellationToken, out var corners)) return false;
            for (var side = 5; side <= 13; side += 4) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
                    0, 0, side, 0, side, side, 0, side,
                    corners[0].X, corners[0].Y, corners[1].X, corners[1].Y,
                    corners[2].X, corners[2].Y, corners[3].X, corners[3].Y);
                var hits = 0;
                for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) {
                    transform.Transform(x + 0.5, y + 0.5, out var px, out var py);
                    var radius = Math.Max(Math.Abs(x - side / 2), Math.Abs(y - side / 2));
                    if (PerspectiveBit(mask, width, height, px, py, invert) == ((radius & 1) == 0)) hits++;
                }
                var score = hits / (double)(side * side);
                if (score < 0.93) continue;
                var position = 0;
                while (position < grids.Count && (grids[position].Score > score ||
                    grids[position].Score.CompareTo(score) == 0 && grids[position].Side >= side)) position++;
                if (position >= 8) continue;
                grids.Insert(position, new BullseyeGrid(transform, side, score));
                if (grids.Count > 8) grids.RemoveAt(8);
            }
            return false;
        });
        return grids;
    }

    private static BitMatrixView ProjectBullseyeGrid(byte[] mask, int width, int height, bool invert,
        BullseyeGrid grid, int size, CancellationToken cancellationToken) {
        var samples = 0;
        return new BitMatrixView(size, size, (x, y) => {
            if ((samples++ & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken))
                throw new OperationCanceledException(cancellationToken);
            grid.Transform.Transform(x - size / 2 + grid.Side / 2.0, y - size / 2 + grid.Side / 2.0, out var px, out var py);
            return PerspectiveBit(mask, width, height, px, py, invert);
        });
    }

    private static bool PerspectiveBit(byte[] mask, int width, int height, double x, double y, bool invert) {
        if (!(x >= 0 && x <= width - 1 && y >= 0 && y <= height - 1)) return false;
        return ((mask[(int)Math.Round(y) * width + (int)Math.Round(x)] & 1) != 0) != invert;
    }
}

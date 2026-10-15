using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.MaxiCode;

/// <summary>Locates circular finders and samples the staggered MaxiCode lattice.</summary>
internal static partial class MaxiCodeImageDecoder {
    private const int MaximumFinders = 16;
    private const double RowSpacing = 0.8660254037844386;
    private static readonly int[] MarkerX = { 28, 29, 10, 11, 11, 7, 8, 20, 20, 10, 10, 17, 17 };
    private static readonly int[] MarkerY = { 0, 0, 9, 9, 10, 15, 16, 16, 17, 22, 23, 22, 23 };
    private static readonly double[] ScaleAdjustments = { 1, 0.985, 1.015, 0.97, 1.03 };

    internal static bool TryDecode(byte[] rgba, int width, int height, CancellationToken token,
        out MaxiCodeDecoded decoded, out bool inverted) {
        decoded = null!;
        inverted = false;
        var budget = DecodeBudget.Capture();
        if (!TryCreateGray(rgba, width, height, token, budget, out var image)) return false;
        var finders = FindFinders(image, token, budget);
        foreach (var finder in finders) {
            if (Aborted(token, budget)) return false;
            if (!TrySample(image, finder, token, budget, out decoded)) continue;
            inverted = finder.Inverted;
            return true;
        }
        return false;
    }

    private static bool Aborted(CancellationToken token, DecodeBudgetState budget) =>
        token.IsCancellationRequested || budget.IsExpired;

    private static bool TrySample(GrayImage image, Finder finder, CancellationToken token,
        DecodeBudgetState budget, out MaxiCodeDecoded decoded) {
        decoded = null!;
        var attempts = 0;
        // Cardinal orientations first; the remaining angles handle labels placed at arbitrary rotation.
        for (var rotation = 0; rotation < 360; rotation++) {
            if (Aborted(token, budget)) return false;
            // Each following block contains the 89 angles between adjacent cardinal orientations.
            var degrees = rotation < 4 ? rotation * 90 : rotation - 4 + (rotation - 4) / 89 + 1;
            var radians = degrees * Math.PI / 180;
            var cosine = Math.Cos(radians);
            var sine = Math.Sin(radians);
            foreach (var scale in ScaleAdjustments) {
                var pitchX = finder.DiameterX / 9 * scale;
                var pitchY = finder.DiameterY / 9 * scale;
                bool Sample(int x, int y, out bool dark) {
                    var dx = x - 14 + (y & 1) * 0.5;
                    var dy = (y - 16) * RowSpacing;
                    var px = (int)Math.Floor(finder.X + (dx * cosine - dy * sine) * pitchX + 0.5);
                    var py = (int)Math.Floor(finder.Y + (dx * sine + dy * cosine) * pitchY + 0.5);
                    dark = false;
                    if ((uint)px >= (uint)image.Width || (uint)py >= (uint)image.Height) return false;
                    dark = image.Dark(px, py) != finder.Inverted;
                    return true;
                }
                var missingMarkers = 0;
                for (var i = 0; i < MarkerX.Length; i++) {
                    if (Sample(MarkerX[i], MarkerY[i], out var dark) && dark) continue;
                    if (++missingMarkers > 2) break;
                }
                if (missingMarkers > 2) continue;
                if (++attempts > 64 || Aborted(token, budget)) return false;
                var modules = new BitMatrix(30, 33);
                var inside = true;
                for (var y = 0; y < 33 && inside; y++) {
                    if (Aborted(token, budget)) return false;
                    for (var x = 0; x < 30 - (y & 1); x++) {
                        if (!Sample(x, y, out var dark)) { inside = false; break; }
                        modules[x, y] = dark;
                    }
                }
                // These fixed orientation modules carry no payload. Small physical damage need not
                // prevent the primary and secondary Reed-Solomon blocks from validating the symbol.
                for (var i = 0; i < MarkerX.Length; i++) modules[MarkerX[i], MarkerY[i]] = true;
                if (inside && MaxiCodeDecoder.TryDecodeDetailed(modules, out decoded)) return true;
            }
        }
        return false;
    }

    private readonly struct Finder {
        internal readonly double X, Y, DiameterX, DiameterY;
        internal readonly bool Inverted;
        internal Finder(double x, double y, double diameterX, double diameterY, bool inverted) {
            X = x; Y = y; DiameterX = diameterX; DiameterY = diameterY; Inverted = inverted;
        }
    }
}

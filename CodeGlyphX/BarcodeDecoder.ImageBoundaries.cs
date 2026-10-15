using System;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    /// <summary>Rejects unchecked symbols clipped by image-search region boundaries.</summary>
    private static bool IsBoundaryClippedUncheckedBarcode(BarcodeImageCandidate candidate, byte[] rgba,
        int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken) {
        if (candidate.Decoded.Type != BarcodeType.Pharmacode && candidate.Decoded.Type != BarcodeType.ITF &&
            candidate.Decoded.Type != BarcodeType.Industrial2of5) return false;
        var region = candidate.SearchRegion;
        if (region.X == 0 && region.Y == 0 && region.Width == width && region.Height == height) return false;

        // Pharmacode, ordinary ITF and Industrial 2 of 5 have no mandatory checksum. A tile boundary through
        // a bar can turn a fragment into another valid number, even with ITF's short guards.
        // Require background at both ends of the actual sampled scanline.
        var vertical = candidate.Scanline.IsVertical;
        var position = vertical ? region.X + candidate.Scanline.Position : region.Y + candidate.Scanline.Position;
        var start = vertical ? region.Y : region.X;
        var end = vertical ? region.Bottom : region.Right;
        int Luminance(int at) {
            var x = vertical ? position : at;
            var y = vertical ? at : position;
            var offset = y * stride + x * 4;
            var red = format == PixelFormat.Rgba32 ? rgba[offset] : rgba[offset + 2];
            var blue = format == PixelFormat.Rgba32 ? rgba[offset + 2] : rgba[offset];
            return BarcodeScanline.GetLuminance(red, rgba[offset + 1], blue, rgba[offset + 3]);
        }
        var minimum = 255;
        var maximum = 0;
        for (var at = start; at < end; at++) {
            if ((at & 127) == 0 && cancellationToken.IsCancellationRequested) return true;
            var luminance = Luminance(at);
            minimum = Math.Min(minimum, luminance);
            maximum = Math.Max(maximum, luminance);
        }
        var threshold = (minimum + maximum) / 2;
        if (minimum >= maximum) return true;
        // These formats also support white bars on a dark quiet background. Require
        // the same background polarity at both edges; the ten-module check below
        // rejects internal gaps and clipped edge bars for either polarity.
        var darkBackground = Luminance(start) < threshold && Luminance(end - 1) < threshold;
        bool IsBackground(int at) => darkBackground ? Luminance(at) < threshold : Luminance(at) >= threshold;
        var first = start;
        while (first < end && IsBackground(first)) first++;
        var last = end - 1;
        while (last > first && IsBackground(last)) last--;
        // Inverted whole-row hypotheses retain the dark quiet zones in their
        // module buffer. Exclude those ends when estimating foreground pitch;
        // otherwise an internal gap can look like a sufficiently wide margin.
        var modules = candidate.Scanline.Modules;
        var firstModule = 0;
        while (firstModule < modules.Length && modules[firstModule] == darkBackground) firstModule++;
        var lastModule = modules.Length - 1;
        while (lastModule >= firstModule && modules[lastModule] == darkBackground) lastModule--;
        if (lastModule < firstModule) return true;
        if (candidate.Decoded.Type == BarcodeType.Pharmacode) {
            // A long outer bar in a tight crop can resemble a quiet margin.
            // The foreground between the proposed margins must itself decode
            // to this payload before those margins can validate a tile.
            var foreground = new bool[lastModule - firstModule + 1];
            for (var i = 0; i < foreground.Length; i++) foreground[i] = modules[firstModule + i] != darkBackground;
            if (!TryDecodePharmacode(foreground, out var text) || text != candidate.Decoded.Text) return true;
        }
        var quietModules = candidate.Decoded.Type == BarcodeType.Pharmacode ? 2 : 10;
        var quietPixels = Math.Max(2, (long)quietModules * (last - first + 1) / (lastModule - firstModule + 1));
        return first - start < quietPixels || end - last - 1 < quietPixels;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>Encodes caller-defined bar/space widths and recovers them from exact modules or calibrated image bounds.</summary>
/// <remarks>Raw marks have no application alphabet, checksum or automatic identification. Their result is a sequence of runs.</remarks>
public static class RawLinear {
    /// <summary>Gets the maximum number of alternating widths.</summary>
    public const int MaximumLength = 2500;

    /// <summary>Encodes digits one through nine as alternating bar/space widths, beginning with a bar and retaining a trailing space.</summary>
    public static Barcode1D Encode(string widths) {
        if (widths is null) throw new ArgumentNullException(nameof(widths));
        if (widths.Length < 1 || widths.Length > MaximumLength) throw new ArgumentException("Supply one through 2500 run widths.", nameof(widths));
        var runs = new BarSegment[widths.Length];
        for (var i = 0; i < runs.Length; i++) {
            if (widths[i] < '1' || widths[i] > '9') throw new ArgumentException("Raw widths require ASCII digits one through nine.", nameof(widths));
            runs[i] = new BarSegment((i & 1) == 0, widths[i] - '0');
        }
        return new Barcode1D(runs);
    }

    /// <summary>Returns a canonical raw definition for alternating runs whose widths fit one through nine modules.</summary>
    public static string GetWidths(Barcode1D symbol) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (symbol.Segments.Count > MaximumLength) throw new ArgumentException("The symbol has too many raw runs.", nameof(symbol));
        var result = new StringBuilder(symbol.Segments.Count);
        for (var i = 0; i < symbol.Segments.Count; i++) {
            var run = symbol.Segments[i];
            if (run.IsBar != ((i & 1) == 0) || run.Modules > 9) throw new ArgumentException("Raw runs must alternate from a bar and have widths one through nine.", nameof(symbol));
            result.Append((char)('0' + run.Modules));
        }
        return result.ToString();
    }

    /// <summary>Recovers bounded alternating widths from complete one-module samples, including any declared trailing white modules.</summary>
    public static bool TryDecode(ReadOnlySpan<bool> modules, out Barcode1D symbol) {
        symbol = null!;
        if (modules.Length < 1 || modules.Length > MaximumLength * 9 || !modules[0]) return false;
        var runs = new List<BarSegment>();
        var value = true; var width = 0;
        foreach (var module in modules) {
            if (module != value) {
                if (runs.Count >= MaximumLength) return false;
                runs.Add(new BarSegment(value, width)); value = module; width = 0;
            }
            if (++width > 9) return false;
        }
        if (runs.Count >= MaximumLength) return false;
        runs.Add(new BarSegment(value, width));
        symbol = new Barcode1D(runs);
        return true;
    }

    /// <summary>Samples an explicitly calibrated raw mark; bounds exclude captions and include every encoded trailing space.</summary>
    /// <param name="frame">Caller-owned pixel memory in any supported packed format.</param>
    /// <param name="bounds">The entire printed symbol rectangle, without quiet zones or captions.</param>
    /// <param name="moduleCount">Known total module count, one through 22500. At least one source pixel per module is required.</param>
    /// <param name="symbol">Recovered run widths; no application text is inferred.</param>
    /// <param name="rotationDegrees">Known clockwise print rotation: zero, 90, 180 or 270.</param>
    /// <param name="inverted">Whether light bars on a dark background were printed.</param>
    /// <param name="threshold">White-composited luminance below this value is dark.</param>
    /// <param name="cancellationToken">Stops sampling without returning a partially sampled definition.</param>
    public static bool TryDecode(ImageFrame frame, ImageRegion bounds, int moduleCount, out Barcode1D symbol,
        int rotationDegrees = 0, bool inverted = false, byte threshold = 128, CancellationToken cancellationToken = default) {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        if (bounds.Width < 1 || bounds.Height < 1 || bounds.Right > frame.Width || bounds.Bottom > frame.Height)
            throw new ArgumentOutOfRangeException(nameof(bounds), "Calibrated bounds must fit completely inside the source image.");
        if (moduleCount < 1 || moduleCount > MaximumLength * 9) throw new ArgumentOutOfRangeException(nameof(moduleCount));
        if (rotationDegrees is not (0 or 90 or 180 or 270)) throw new ArgumentOutOfRangeException(nameof(rotationDegrees));
        symbol = null!;
        if (cancellationToken.IsCancellationRequested || (rotationDegrees % 180 == 0 ? bounds.Width : bounds.Height) < moduleCount) return false;
        var modules = new bool[moduleCount];
        Span<byte> rgba = stackalloc byte[4];
        for (var column = 0; column < moduleCount; column++) {
            if ((column & 63) == 0 && cancellationToken.IsCancellationRequested) return false;
            var u = (column + .5) / moduleCount; var dark = 0;
            for (var sample = 0; sample < 3; sample++) {
                var v = (sample + 1) / 4D;
                var x = rotationDegrees switch { 90 => 1 - v, 180 => 1 - u, 270 => v, _ => u };
                var y = rotationDegrees switch { 90 => u, 180 => 1 - v, 270 => 1 - u, _ => v };
                ImageFrameConverter.ReadPixel(frame, bounds.X + (int)(x * bounds.Width), bounds.Y + (int)(y * bounds.Height), rgba);
                if ((BarcodeScanline.GetLuminance(rgba[0], rgba[1], rgba[2], rgba[3]) < threshold) ^ inverted) dark++;
            }
            modules[column] = dark >= 2;
        }
        return !cancellationToken.IsCancellationRequested && TryDecode(modules, out symbol);
    }
}

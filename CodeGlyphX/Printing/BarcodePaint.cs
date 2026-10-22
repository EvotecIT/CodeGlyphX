using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

/// <summary>An immutable solid, two-colour linear gradient, or repeating RGBA tile in physical coordinates.</summary>
/// <remarks>Decorative paints can impair recognition. Required Ultracode data colours cannot be replaced.</remarks>
public sealed class BarcodePaint {
    internal enum PaintKind { Solid, Gradient, Pattern }
    internal PaintKind Kind { get; }
    internal Rgba32 StartColor { get; }
    internal Rgba32 EndColor { get; }
    internal BarcodePoint Start { get; }
    internal BarcodePoint End { get; }
    internal byte[]? Pixels { get; }
    internal int PixelWidth { get; }
    internal int PixelHeight { get; }
    internal double TileWidth { get; }
    internal double TileHeight { get; }
    internal bool IsOpaque { get; }

    private BarcodePaint(Rgba32 color) { StartColor = EndColor = color; IsOpaque = color.A == 255; }
    private BarcodePaint(BarcodePoint start, BarcodePoint end, Rgba32 first, Rgba32 last) {
        if (Math.Abs(start.X) > 1000000 || Math.Abs(start.Y) > 1000000 || Math.Abs(end.X) > 1000000 || Math.Abs(end.Y) > 1000000)
            throw new ArgumentOutOfRangeException(nameof(end), "Paint coordinates must be within one kilometre of the layout origin.");
        if (Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y) < 1e-6) throw new ArgumentException("A gradient must have a nonzero direction of at least one nanometre.", nameof(end));
        Kind = PaintKind.Gradient; Start = start; End = end; StartColor = first; EndColor = last;
        IsOpaque = first.A == 255 && last.A == 255;
    }
    private BarcodePaint(byte[] pixels, int width, int height, double tileWidth, double tileHeight) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (width < 1 || height < 1 || (long)width * height > 1048576 || (long)width * height * 4 != pixels.Length)
            throw new ArgumentException("A pattern requires one to 1048576 tightly packed RGBA pixels.", nameof(pixels));
        if (!BarcodeBounds.Finite(tileWidth) || tileWidth <= 0 || tileWidth > 10000) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (!BarcodeBounds.Finite(tileHeight) || tileHeight <= 0 || tileHeight > 10000) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        Kind = PaintKind.Pattern; Pixels = (byte[])pixels.Clone(); PixelWidth = width; PixelHeight = height;
        TileWidth = tileWidth; TileHeight = tileHeight;
        IsOpaque = true;
        for (var i = 3; i < Pixels.Length; i += 4) if (Pixels[i] != 255) { IsOpaque = false; break; }
    }
    /// <summary>Creates a constant colour.</summary>
    public static BarcodePaint Solid(Rgba32 color) => new(color);
    /// <summary>Creates a clamped gradient between two millimetre coordinates.</summary>
    public static BarcodePaint LinearGradient(BarcodePoint start, BarcodePoint end, Rgba32 first, Rgba32 last) => new(start, end, first, last);
    /// <summary>Snapshots a repeating image tile whose physical size is independent of its pixel resolution.</summary>
    public static BarcodePaint Pattern(byte[] rgba, int width, int height, double tileWidthMm, double tileHeightMm) => new(rgba, width, height, tileWidthMm, tileHeightMm);

    internal Rgba32 Sample(double x, double y) {
        if (Kind == PaintKind.Solid) return StartColor;
        if (Kind == PaintKind.Pattern) {
            var px = (int)Math.Floor((x / TileWidth - Math.Floor(x / TileWidth)) * PixelWidth);
            var py = (int)Math.Floor((y / TileHeight - Math.Floor(y / TileHeight)) * PixelHeight);
            var at = (Math.Min(py, PixelHeight - 1) * PixelWidth + Math.Min(px, PixelWidth - 1)) * 4;
            return new Rgba32(Pixels![at], Pixels[at + 1], Pixels[at + 2], Pixels[at + 3]);
        }
        var dx = End.X - Start.X; var dy = End.Y - Start.Y;
        var t = Math.Max(0, Math.Min(1, ((x - Start.X) * dx + (y - Start.Y) * dy) / (dx * dx + dy * dy)));
        return new Rgba32(Mix(StartColor.R, EndColor.R, t), Mix(StartColor.G, EndColor.G, t),
            Mix(StartColor.B, EndColor.B, t), Mix(StartColor.A, EndColor.A, t));
    }
    private static byte Mix(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
}

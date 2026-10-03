using System;
using System.Threading;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Rasterizes procedural source artwork for the existing image composer.</summary>
internal sealed class QrPatternRasterizer {
    private readonly QrArtPattern _pattern;
    private readonly Rgba32[] _colors;
    private readonly Rgba32 _paper;
    private readonly int _seed;
    private readonly double _scale, _cos, _sin, _phase;

    internal QrPatternRasterizer(QrArtPatternOptions options) {
        options.Validate();
        _pattern = options.Pattern;
        _colors = (Rgba32[])options.Colors.Clone();
        _paper = options.Background;
        _seed = options.Seed;
        _scale = options.Scale;
        var angle = options.RotationDegrees % 360 * Math.PI / 180;
        _cos = Math.Cos(angle); _sin = Math.Sin(angle);
        _phase = Hash(0, 0) * Math.PI * 2;
    }

    internal byte[] Draw(int side, CancellationToken cancellationToken) {
        const string message = "Procedural artwork exceeds output limits.";
        RenderGuards.EnsureOutputPixels(side, side, message);
        var pixels = new byte[RenderGuards.EnsureOutputBytes((long)side * side * 4, message)];
        for (var y = 0; y < side; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < side; x++) {
                var px = ((x + 0.5) / side - 0.5) * _scale;
                var py = ((y + 0.5) / side - 0.5) * _scale;
                var color = Sample(px * _cos + py * _sin, py * _cos - px * _sin);
                var index = (y * side + x) * 4;
                pixels[index] = color.R; pixels[index + 1] = color.G; pixels[index + 2] = color.B;
                pixels[index + 3] = 255;
            }
        }
        return pixels;
    }

    private Rgba32 Sample(double x, double y) {
        switch (_pattern) {
            case QrArtPattern.Marble:
                return Palette(0.5 + 0.5 * Math.Sin(x * 17 + 2.4 * Math.Sin(y * 8 + _phase) + Math.Sin((x + y) * 11)));
            case QrArtPattern.Waves:
                return Palette(0.5 + 0.5 * Math.Sin(y * 19 + 1.8 * Math.Sin(x * 9 + _phase)));
            case QrArtPattern.Sunburst:
                return Palette(0.5 + 0.5 * Math.Sin(Math.Atan2(y, x) * 9 + Math.Sqrt(x * x + y * y) * 16 + _phase));
            default:
                return SampleTile(x, y);
        }
    }

    private Rgba32 SampleTile(double x, double y) {
        x *= 7; y *= 7;
        var tx = (int)Math.Floor(x); var ty = (int)Math.Floor(y);
        var u = x - tx - 0.5; var v = y - ty - 0.5;
        var random = Hash(tx, ty);
        var color = _colors[(int)(random * _colors.Length)];
        switch (_pattern) {
            case QrArtPattern.Geometric:
                if (random < 0.45) return u * u + v * v < 0.12 ? _paper : color;
                return (random < 0.7 ? u + v : u - v) > 0 ? color : _paper;
            case QrArtPattern.Botanical:
                var leaf = Math.Abs(v - 0.2 * Math.Sin(u * 5)) < 0.36 * Math.Max(0, 1 - Math.Abs(u) * 2);
                return leaf || Math.Abs(v - u * 0.35) < 0.025 ? color : _paper;
            default:
                var junction = u * u + v * v < 0.025;
                var trace = Math.Abs(u) < 0.035 || (Math.Abs(v) < 0.035 && (random < 0.5 || u > 0));
                return junction || trace ? color : _paper;
        }
    }

    private Rgba32 Palette(double amount) {
        var position = Math.Max(0, Math.Min(1, amount)) * (_colors.Length - 1);
        var index = Math.Min(_colors.Length - 2, (int)position);
        var blend = position - index;
        var first = _colors[index]; var second = _colors[index + 1];
        return new Rgba32(Mix(first.R, second.R, blend), Mix(first.G, second.G, blend), Mix(first.B, second.B, blend));
    }

    private static byte Mix(byte first, byte second, double amount) => (byte)Math.Round(first + (second - first) * amount);

    private double Hash(int x, int y) {
        unchecked {
            var value = (uint)_seed ^ (uint)x * 0x9e3779b9u ^ (uint)y * 0x85ebca6bu;
            value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; value ^= value >> 16;
            return (value & 0xffffff) / 16777216.0;
        }
    }
}

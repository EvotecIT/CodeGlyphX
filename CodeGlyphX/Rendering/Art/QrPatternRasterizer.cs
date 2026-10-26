using CodeGlyphX.Rendering;
using System;
using System.Threading;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Rasterizes procedural source artwork for the existing image composer.</summary>
/// <remarks>Motifs are described by signed distances, so edges are antialiased at any output size.
/// Palette entries are used by role (index modulo the palette length), so any two to sixteen colours work.</remarks>
internal sealed class QrPatternRasterizer {
    private readonly QrArtPattern _pattern;
    private readonly Rgba32[] _colors;
    private readonly Rgba32 _paper;
    private readonly int _seed;
    private readonly double _scale, _cos, _sin, _phase;
    private readonly double[] _depths = new double[9];
    private readonly int[] _order = new int[9];
    private double _pixel;

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
        _pixel = _scale / side;
        for (var y = 0; y < side; y++) {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < side; x++) {
                var px = ((x + 0.5) / side - 0.5) * _scale;
                var py = ((y + 0.5) / side - 0.5) * _scale;
                var color = Sample(px * _cos + py * _sin, py * _cos - px * _sin);
                var index = (y * side + x) * 4;
                pixels[index] = Byte(color.R); pixels[index + 1] = Byte(color.G); pixels[index + 2] = Byte(color.B);
                pixels[index + 3] = 255;
            }
        }
        return pixels;
    }

    private Color Sample(double x, double y) => _pattern switch {
        QrArtPattern.Marble => Marble(x, y),
        QrArtPattern.Waves => Waves(x, y),
        QrArtPattern.Sunburst => Sunburst(x, y),
        QrArtPattern.Geometric => Geometric(x, y),
        QrArtPattern.Botanical => Botanical(x, y),
        _ => Circuit(x, y)
    };

    // Veined stone: soft clouding, two families of turbulent veins and a sparse accent vein.
    private Color Marble(double x, double y) {
        var qx = x * 2.2 + _phase; var qy = y * 2.2 - _phase;
        Color color = Role(0);
        // Broad, soft clouding first.
        color = Color.Mix(color, Role(1), 0.6 * SmoothStep(-0.15, 0.5, Fbm(qx * 0.8 + 11.3, qy * 0.8 - 4.1, 5)));
        // Classic turbulence marble: sweeping diagonal veins with a soft halo and a fine darker core.
        var major = Math.Pow(1 - Math.Abs(Math.Sin((qx * 0.7 + qy * 0.45 + 1.7 * Turbulence(qx * 0.9, qy * 0.9, 6)) * Math.PI)), 1);
        var minor = 1 - Math.Abs(Math.Sin((qy * 1.1 - qx * 0.35 + 1.3 * Turbulence(qx * 1.3 + 5.2, qy * 1.3 + 1.7, 6)) * Math.PI * 1.3));
        var accent = 1 - Math.Abs(Math.Sin((qx * 0.3 - qy * 0.75 + 1.5 * Turbulence(qx * 0.8 - 2.2, qy * 0.8 + 8.1, 5)) * Math.PI * 0.7));
        var presence = SmoothStep(-0.25, 0.35, Fbm(qx * 0.45 - 7, qy * 0.45 + 2, 3));
        color = Color.Mix(color, Role(2), 0.28 * Math.Pow(minor, 9) * presence);
        color = Color.Mix(color, Role(2), 0.3 * Math.Pow(major, 4) + 0.62 * Math.Pow(major, 26));
        return Color.Mix(color, Role(3), 0.8 * Math.Pow(accent, 30) * SmoothStep(0.0, 0.3, Fbm(qx * 0.5 + 20, qy * 0.5 - 13, 2)));
    }

    private double Turbulence(double x, double y, int octaves) {
        var sum = 0.0; var amplitude = 0.5;
        for (var i = 0; i < octaves; i++) {
            sum += amplitude * Math.Abs(Noise(x, y));
            x = x * 2.03 + 17.1; y = y * 2.03 - 9.7; amplitude *= 0.5;
        }
        return sum;
    }

    // Layered swells: each band deepens toward its lower edge and carries a pale crest along its upper edge.
    private Color Waves(double x, double y) {
        var k0 = (int)Math.Floor(y / WaveBand);
        var band = k0 - 2;
        for (var k = k0 - 2; k <= k0 + 2; k++) if (WaveEdge(k, x, out _) <= y) band = k;
        var top = WaveEdge(band, x, out var slope);
        var bottom = WaveEdge(band + 1, x, out _);
        var depth = Math.Max(0, Math.Min(1, (y - top) / Math.Max(1e-6, bottom - top)));
        var color = WaveColor(band, depth);
        var distance = (y - top) / Math.Sqrt(1 + slope * slope);
        color = Color.Mix(color, Color.Mix(Role(band), White, 0.5), 0.75 * Coverage(distance - 0.004));
        // Antialias against the band above.
        return Color.Mix(color, WaveColor(band - 1, 1), Coverage(distance + _pixel / 2));
    }

    private const double WaveBand = 0.115;

    private Color WaveColor(int band, double depth) => Color.Mix(Role(band), Color.Mix(Role(band), Black, 0.2), depth * depth);

    private double WaveEdge(int k, double x, out double slope) {
        var offset = Hash(k, 91) * Math.PI * 2;
        const double a1 = 0.034, a2 = 0.011, f2 = 17;
        var f1 = 7.5 + 2 * Hash(k, 17);
        slope = a1 * f1 * Math.Cos(x * f1 + offset + _phase) + a2 * f2 * Math.Cos(x * f2 - offset * 1.3);
        return k * WaveBand + a1 * Math.Sin(x * f1 + offset + _phase) + a2 * Math.Sin(x * f2 - offset * 1.3);
    }

    // Straight rays from the canvas centre, which is the code centre for centred compositions.
    private Color Sunburst(double x, double y) {
        const int rays = 32;
        var radius = Length(x, y);
        var position = (Math.Atan2(y, x) + _phase) / (2 * Math.PI) * rays;
        var fraction = position - Math.Floor(position);
        var arc = 2 * Math.PI / rays * radius;
        var edge = Math.Min(Math.Min(fraction, Math.Abs(0.5 - fraction)), 1 - fraction) * arc;
        var signed = fraction < 0.5 ? edge : -edge;
        var t = SmoothStep(0.04, 0.72, radius);
        var light = Color.Mix(Role(0), Role(1), t);
        var deep = Color.Mix(Role(1), Role(2), t);
        var color = Color.Mix(deep, light, Coverage(-signed));
        color = Color.Mix(color, Role(3), 0.3 * SmoothStep(0.42, 0.8, radius));
        // A soft halo keeps the middle of the code calm.
        return Color.Mix(color, Role(0), 0.4 * (1 - SmoothStep(0, 0.22, radius)));
    }

    // Bauhaus tiles: quarter and half discs, triangles, rings and dots, each tile a two-colour composition.
    private Color Geometric(double x, double y) {
        const double cells = 7;
        x *= cells; y *= cells;
        var tx = (int)Math.Floor(x); var ty = (int)Math.Floor(y);
        var u = x - tx; var v = y - ty;
        var h = Hash(tx, ty);
        var backgroundRole = Hash(tx + 37, ty - 11) < 0.3 ? -1 : (int)(Hash(tx - 5, ty + 23) * 4);
        var foregroundRole = (int)(Hash(tx + 13, ty + 7) * 4);
        if (foregroundRole == backgroundRole) foregroundRole++;
        Color background = backgroundRole < 0 ? _paper : Role(backgroundRole);
        Color foreground = Role(foregroundRole);
        var corner = (int)(Hash(tx + 3, ty + 3) * 4);
        var cx = corner == 1 || corner == 2 ? 1.0 : 0.0; var cy = corner >= 2 ? 1.0 : 0.0;
        double distance;
        if (h < 0.3) distance = Length(u - cx, v - cy) - 1;
        else if (h < 0.5) distance = corner % 2 == 0 ? Length(u - 0.5, v - cy) - 0.5 : Length(u - cx, v - 0.5) - 0.5;
        else if (h < 0.72) distance = (corner % 2 == 0 ? u + v - 1 : u - v) * (corner < 2 ? 1 : -1) / Math.Sqrt(2);
        else if (h < 0.88) distance = Math.Abs(Length(u - 0.5, v - 0.5) - 0.3) - 0.11;
        else distance = Length(u - 0.5, v - 0.5) - 0.2;
        return Color.Mix(background, foreground, Math.Max(0, Math.Min(1, 0.5 - distance / (_pixel * cells))));
    }

    // Overlapping lance leaves with midribs on short stems, and a few berries.
    private Color Botanical(double x, double y) {
        const double cell = 1 / 5.5;
        var gx = (int)Math.Floor(x / cell); var gy = (int)Math.Floor(y / cell);
        var color = Color.Mix(_paper, Role(2), 0.16);
        for (var n = 0; n < 9; n++) {
            _order[n] = n;
            _depths[n] = Hash((gx - 1 + n % 3) * 7 + 1, (gy - 1 + n / 3) * 13 + 5);
        }
        for (var a = 1; a < 9; a++) for (var b = a; b > 0 && _depths[_order[b - 1]] > _depths[_order[b]]; b--)
            (_order[b - 1], _order[b]) = (_order[b], _order[b - 1]);
        for (var n = 0; n < 9; n++) {
            var i = gx - 1 + _order[n] % 3; var j = gy - 1 + _order[n] / 3;
            var cx = (i + 0.2 + 0.6 * Hash(i, j * 3 + 1)) * cell; var cy = (j + 0.2 + 0.6 * Hash(i * 5 + 2, j)) * cell;
            var angle = -0.9 + 0.8 * (Hash(i + 9, j - 4) - 0.5) + (Hash(i - 3, j + 8) < 0.5 ? 0 : Math.PI * 0.85);
            var length = cell * (0.95 + 0.45 * Hash(i + 21, j + 2)); var width = length * 0.36;
            var dx = x - cx; var dy = y - cy;
            var cos = Math.Cos(angle); var sin = Math.Sin(angle);
            var lx = dx * cos + dy * sin; var ly = -dx * sin + dy * cos;
            ly -= 0.35 * (Hash(i - 7, j - 7) - 0.5) / length * (lx * lx - length * length / 4);
            var leaf = Lens(lx, ly, length, width);
            var stem = SegmentDistance(lx, ly, -length * 0.66, 0, -length * 0.45, 0) - length * 0.016;
            Color tone = Role((int)(Hash(i + 4, j + 31) * 3));
            var coverage = Coverage(Math.Min(leaf, stem));
            if (coverage > 0) {
                var shaded = Color.Mix(tone, Black, 0.16 * SmoothStep(0, width / 2, ly));
                var rib = Coverage(Math.Max(Math.Abs(ly) - length * 0.012, Math.Abs(lx) - length * 0.42));
                shaded = Color.Mix(shaded, Color.Mix(tone, White, 0.45), 0.85 * rib);
                color = Color.Mix(color, shaded, coverage);
            }
            if (Hash(i - 13, j + 17) < 0.22) {
                var berry = Length(x - cx - Math.Cos(angle + 1.8) * length * 0.42, y - cy - Math.Sin(angle + 1.8) * length * 0.42) - cell * 0.075;
                color = Color.Mix(color, Role(3), Coverage(berry));
            }
        }
        return color;
    }

    // Intersection of two discs whose common chord is the leaf length: a pointed lance shape.
    private static double Lens(double x, double y, double length, double width) {
        var half = length / 2; var sagitta = width / 2;
        var radius = (half * half + sagitta * sagitta) / (2 * sagitta);
        var offset = radius - sagitta;
        return Math.Max(Length(x, y - offset) - radius, Length(x, y + offset) - radius);
    }

    // Printed-circuit traces: edge links meet at each cell centre with 45 degree chamfers and end in pads.
    private Color Circuit(double x, double y) {
        const double cells = 13, chamfer = 0.22, width = 0.07;
        var gx = x * cells; var gy = y * cells;
        var tx = (int)Math.Floor(gx); var ty = (int)Math.Floor(gy);
        var u = gx - tx - 0.5; var v = gy - ty - 0.5;
        var pixel = _pixel * cells;
        var color = Color.Mix(_paper, Role(0), 0.26);
        var grid = Math.Min(Math.Abs(u), Math.Abs(v));
        color = Color.Mix(color, Role(0), 0.06 * Math.Max(0, Math.Min(1, 0.5 - (grid - 0.006) / pixel)));
        var left = Link(tx, ty, true); var right = Link(tx + 1, ty, true);
        var up = Link(tx, ty, false); var down = Link(tx, ty + 1, false);
        var links = (left ? 1 : 0) + (right ? 1 : 0) + (up ? 1 : 0) + (down ? 1 : 0);
        var turn = links > 1 && !((left && right) || (up && down));
        var inset = turn ? chamfer : 0;
        var distance = double.MaxValue;
        if (left) distance = Math.Min(distance, SegmentDistance(u, v, -0.5, 0, -inset, 0));
        if (right) distance = Math.Min(distance, SegmentDistance(u, v, 0.5, 0, inset, 0));
        if (up) distance = Math.Min(distance, SegmentDistance(u, v, 0, -0.5, 0, -inset));
        if (down) distance = Math.Min(distance, SegmentDistance(u, v, 0, 0.5, 0, inset));
        if (turn) {
            if (left && up) distance = Math.Min(distance, SegmentDistance(u, v, -chamfer, 0, 0, -chamfer));
            if (left && down) distance = Math.Min(distance, SegmentDistance(u, v, -chamfer, 0, 0, chamfer));
            if (right && up) distance = Math.Min(distance, SegmentDistance(u, v, chamfer, 0, 0, -chamfer));
            if (right && down) distance = Math.Min(distance, SegmentDistance(u, v, chamfer, 0, 0, chamfer));
        }
        color = Color.Mix(color, Role(0), Math.Max(0, Math.Min(1, 0.5 - (distance - width) / pixel)));
        var centre = Length(u, v);
        double shape; int role;
        if (links == 1) { shape = Math.Abs(centre - 0.15) - 0.07; role = 1; }
        else if (links >= 3) { shape = centre - 0.13; role = 2; }
        else if (links == 0 && Hash(tx + 51, ty - 19) < 0.35) { shape = Math.Abs(centre - 0.08) - 0.035; role = 3; }
        else return color;
        return Color.Mix(color, Role(role), Math.Max(0, Math.Min(1, 0.5 - shape / pixel)));
    }

    private bool Link(int x, int y, bool horizontal) => Hash(x * 2 + (horizontal ? 0 : 1) + 101, y * 3 - 7) < (horizontal ? 0.58 : 0.34);

    private static readonly Color White = new(255, 255, 255);
    private static readonly Color Black = new(0, 0, 0);
    private Color Role(int index) => _colors[((index % _colors.Length) + _colors.Length) % _colors.Length];
    private double Coverage(double distance) => Math.Max(0, Math.Min(1, 0.5 - distance / _pixel));

    private static double SmoothStep(double edge0, double edge1, double value) {
        var t = Math.Max(0, Math.Min(1, (value - edge0) / (edge1 - edge0)));
        return t * t * (3 - 2 * t);
    }

    private static double Length(double x, double y) => Math.Sqrt(x * x + y * y);

    private static double SegmentDistance(double px, double py, double ax, double ay, double bx, double by) {
        var dx = bx - ax; var dy = by - ay;
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared == 0 ? 0 : Math.Max(0, Math.Min(1, ((px - ax) * dx + (py - ay) * dy) / lengthSquared));
        return Length(px - ax - t * dx, py - ay - t * dy);
    }

    private double Fbm(double x, double y, int octaves) {
        var sum = 0.0; var amplitude = 0.5;
        for (var i = 0; i < octaves; i++) {
            sum += amplitude * Noise(x, y);
            x = x * 2.03 + 17.1; y = y * 2.03 - 9.7; amplitude *= 0.5;
        }
        return sum;
    }

    // Seeded gradient noise in about -1..1, independent of framework random sequences.
    private double Noise(double x, double y) {
        var ix = (int)Math.Floor(x); var iy = (int)Math.Floor(y);
        var fx = x - ix; var fy = y - iy;
        var ux = fx * fx * fx * (fx * (fx * 6 - 15) + 10); var uy = fy * fy * fy * (fy * (fy * 6 - 15) + 10);
        var a = Gradient(ix, iy, fx, fy); var b = Gradient(ix + 1, iy, fx - 1, fy);
        var c = Gradient(ix, iy + 1, fx, fy - 1); var d = Gradient(ix + 1, iy + 1, fx - 1, fy - 1);
        return (a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy) * 1.4;
    }

    private double Gradient(int x, int y, double dx, double dy) {
        var angle = Hash(x + 7919, y - 104729) * Math.PI * 2;
        return Math.Cos(angle) * dx + Math.Sin(angle) * dy;
    }

    private static byte Byte(double value) => (byte)Math.Round(Math.Max(0, Math.Min(255, value)));

    private double Hash(int x, int y) {
        unchecked {
            var value = (uint)_seed ^ (uint)x * 0x9e3779b9u ^ (uint)y * 0x85ebca6bu;
            value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; value ^= value >> 16;
            return (value & 0xffffff) / 16777216.0;
        }
    }

    private readonly struct Color {
        internal readonly double R, G, B;
        internal Color(double r, double g, double b) { R = r; G = g; B = b; }
        public static implicit operator Color(Rgba32 value) => new(value.R, value.G, value.B);
        internal static Color Mix(Color a, Color b, double t) => new(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }
}

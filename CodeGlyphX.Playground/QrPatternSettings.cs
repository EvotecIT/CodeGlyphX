using System.Globalization;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Playground;

/// <summary>Editable browser controls, independent of retained result snapshots.</summary>
public sealed record QrPatternSettings(QrArtPattern Pattern, int Seed, double Density, double Rotation,
    string Color1, string Color2, string Color3, string Color4, string Paper) {
    public static QrPatternSettings FromPreset(QrArtPattern pattern, int seed = 2026) {
        var options = QrArtPatternPresets.CreatePatternOptions(pattern, seed);
        return new(pattern, seed, options.Scale, options.RotationDegrees, Hex(options.Colors[0]), Hex(options.Colors[1]),
            Hex(options.Colors[2]), Hex(options.Colors[3]), Hex(options.Background));
    }
    public QrArtPatternOptions ToOptions() => new() {
        Pattern = Pattern, Seed = Seed, Scale = Density, RotationDegrees = Rotation,
        Colors = [Parse(Color1), Parse(Color2), Parse(Color3), Parse(Color4)], Background = Parse(Paper)
    };
    private static string Hex(Rgba32 color) => $"#{color.R:x2}{color.G:x2}{color.B:x2}";
    private static Rgba32 Parse(string color) {
        if (color.Length != 7 || color[0] != '#') throw new ArgumentException("Choose a valid palette color.");
        var rgb = int.Parse(color.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new Rgba32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}

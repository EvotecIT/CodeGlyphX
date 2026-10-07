using CodeGlyphX.Rendering;
using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Locally drawn illustration families; equal inputs reproduce equal artwork.</summary>
public enum QrSceneStyle {
    /// <summary>Layered foliage, flowers and a warm sun.</summary>
    TropicalGarden,
    /// <summary>Night skyline, glowing windows and circuit traces.</summary>
    ElectricCity,
    /// <summary>Records, musical notes and rhythmic ribbons.</summary>
    MusicFestival,
    /// <summary>Waves, bubbles, fish and coral.</summary>
    OceanReef,
    /// <summary>Planets, stars, orbital paths and a rocket.</summary>
    CosmicOrbit,
    /// <summary>Pixel creatures, arcade tiles and geometric sparks.</summary>
    RetroArcade
}

/// <summary>Placement of one named scene layer in normalized canvas coordinates.</summary>
public sealed class QrSceneLayerOptions {
    /// <summary>Whether the layer is drawn.</summary>
    public bool Visible { get; set; } = true;
    /// <summary>Horizontal center (0..1).</summary>
    public double X { get; set; } = 0.5;
    /// <summary>Vertical center (0..1).</summary>
    public double Y { get; set; } = 0.5;
    /// <summary>Size relative to the canvas (0.05..2). QR size is additionally limited to 0.25..0.8.</summary>
    public double Scale { get; set; } = 1;
    /// <summary>Clockwise rotation in degrees. QR rotation must be a multiple of 90.</summary>
    public double RotationDegrees { get; set; }
    internal QrSceneLayerOptions Copy() => new() { Visible = Visible, X = X, Y = Y, Scale = Scale, RotationDegrees = RotationDegrees };
    internal void Validate(string name) {
        if (!Finite(X) || X < 0 || X > 1 || !Finite(Y) || Y < 0 || Y > 1)
            throw new ArgumentOutOfRangeException(name, "Layer centers must be between zero and one.");
        if (!Finite(Scale) || Scale < 0.05 || Scale > 2 || !Finite(RotationDegrees))
            throw new ArgumentOutOfRangeException(name, "Layer scale must be 0.05..2 and rotation must be finite.");
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

/// <summary>Editable scene settings, copied before rendering. No external images or services are required.</summary>
public sealed class QrSceneOptions {
    /// <summary>Illustration family.</summary>
    public QrSceneStyle Style { get; set; }
    /// <summary>Output width and height in pixels (256..4096), subject to shared output limits.</summary>
    public int Size { get; set; } = 1200;
    /// <summary>Seed controlling motif placement and colors.</summary>
    public int Seed { get; set; } = 17;
    /// <summary>Four opaque accent colors, copied during rendering.</summary>
    public Rgba32[] Colors { get; set; } = new Rgba32[] { new(246, 58, 119), new(23, 177, 164), new(255, 194, 61), new(103, 76, 202) };
    /// <summary>Canvas paper and light QR modules; must be opaque and have luminance at least 220.</summary>
    public Rgba32 Paper { get; set; } = new(255, 249, 237);
    /// <summary>Dark QR modules and caption ink; must be opaque and have luminance at most 80.</summary>
    public Rgba32 Ink { get; set; } = new(20, 32, 53);
    /// <summary>QR data module shape; functional patterns retain protected geometry.</summary>
    public QrModuleShape ModuleShape { get; set; } = QrModuleShape.ConnectedRounded;
    /// <summary>Portable outlined caption, up to 48 characters. Supports Latin letters, digits, space and -./:()+?.</summary>
    public string Caption { get; set; } = "SCAN ME";
    /// <summary>Optional encoded logo image, at most 1 MiB and one million decoded pixels. It stays outside the protected QR.</summary>
    public byte[]? LogoImage { get; set; }
    /// <summary>Background illustration layer.</summary>
    public QrSceneLayerOptions Backdrop { get; set; } = new();
    /// <summary>Foreground illustration layer.</summary>
    public QrSceneLayerOptions Motifs { get; set; } = new();
    /// <summary>QR placement. Its complete quiet zone must remain inside the canvas.</summary>
    public QrSceneLayerOptions Qr { get; set; } = new() { Scale = 0.6, Y = 0.49 };
    /// <summary>Caption placement and size.</summary>
    public QrSceneLayerOptions CaptionLayer { get; set; } = new() { Scale = 0.8, Y = 0.9 };
    /// <summary>Optional logo placement and size.</summary>
    public QrSceneLayerOptions Logo { get; set; } = new() { Scale = 0.1, Y = 0.12 };

    /// <summary>Returns a deep independent copy, suitable for undo history or reproducible rendering.</summary>
    public QrSceneOptions Clone() {
        Validate();
        return new() { Style = Style, Size = Size, Seed = Seed, Colors = (Rgba32[])Colors.Clone(), Paper = Paper, Ink = Ink,
            ModuleShape = ModuleShape, Caption = Caption, LogoImage = LogoImage is null ? null : (byte[])LogoImage.Clone(),
            Backdrop = Backdrop.Copy(), Motifs = Motifs.Copy(), Qr = Qr.Copy(), CaptionLayer = CaptionLayer.Copy(), Logo = Logo.Copy() };
    }

    internal void Validate() {
        if (!Enum.IsDefined(typeof(QrSceneStyle), Style)) throw new ArgumentOutOfRangeException(nameof(Style));
        if (Size < 256 || Size > 4096) throw new ArgumentOutOfRangeException(nameof(Size));
        if (Colors is null || Colors.Length != 4) throw new ArgumentException("Use exactly four opaque accent colors.", nameof(Colors));
        foreach (var c in Colors) if (c.A != 255) throw new ArgumentException("Accent colors must be opaque.", nameof(Colors));
        if (Paper.A != 255 || Luminance(Paper) < 220) throw new ArgumentException("Use light opaque paper (luminance at least 220).", nameof(Paper));
        if (Ink.A != 255 || Luminance(Ink) > 80) throw new ArgumentException("Use dark opaque ink (luminance at most 80).", nameof(Ink));
        if (!Enum.IsDefined(typeof(QrModuleShape), ModuleShape)) throw new ArgumentOutOfRangeException(nameof(ModuleShape));
        if (Caption is null || Caption.Length > 48) throw new ArgumentException("Captions must contain at most 48 characters.", nameof(Caption));
        foreach (var c in Caption) if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -./:()+?".IndexOf(char.ToUpperInvariant(c)) < 0)
            throw new ArgumentException("The portable caption font supports Latin letters, digits and -./:()+?.", nameof(Caption));
        if (LogoImage is not null && LogoImage.Length > 1024 * 1024) throw new ArgumentException("Logo input exceeds 1 MiB.", nameof(LogoImage));
        ValidateLayer(Backdrop, nameof(Backdrop)); ValidateLayer(Motifs, nameof(Motifs)); ValidateLayer(Qr, nameof(Qr));
        ValidateLayer(CaptionLayer, nameof(CaptionLayer)); ValidateLayer(Logo, nameof(Logo));
        if (!Qr.Visible || Qr.Scale < 0.25 || Qr.Scale > 0.8 || Math.Abs(Qr.RotationDegrees % 90) > 0.000001)
            throw new ArgumentException("The QR must be visible, sized 0.25..0.8, and rotated by quarter turns.", nameof(Qr));
    }
    private static void ValidateLayer(QrSceneLayerOptions layer, string name) {
        if (layer is null) throw new ArgumentNullException(name);
        layer.Validate(name);
    }
    private static double Luminance(Rgba32 c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
}

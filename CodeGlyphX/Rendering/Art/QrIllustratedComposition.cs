using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A finished illustrated composition with a raster export and a self-contained hybrid SVG.</summary>
public sealed class QrIllustratedComposition {
    /// <summary>Complete composition, including decorative framing.</summary>
    public QrImageComposition Image { get; }
    private readonly string _svg;
    internal QrIllustratedComposition(QrImageComposition image, string svg) { Image = image; _svg = svg; }
    /// <summary>Returns an SVG with vector ornaments and embedded QR artwork. Image detail remains limited by ModuleSize.</summary>
    public string ToSvg() => _svg;
    /// <summary>Saves the self-contained SVG.</summary>
    public string SaveSvg(string path) => RenderIO.WriteText(path, _svg);
}

/// <summary>Illustration framing families. All decoration stays outside the QR and its quiet zone.</summary>
public enum QrIllustratedStyle {
    /// <summary>Fine concentric engraving lines and a portrait medallion.</summary>
    EngravedPortrait,
    /// <summary>Branch and leaf framing around a botanical image.</summary>
    BotanicalBadge,
    /// <summary>Bold geometric corners and a restrained poster border.</summary>
    GeometricPoster
}

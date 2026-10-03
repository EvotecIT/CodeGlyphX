using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A finished illustrated QR scene. Inputs are snapshotted; later edits do not change this result.</summary>
public sealed partial class QrSceneComposition {
    /// <summary>Exact text encoded in the retained artwork.</summary>
    public string Payload { get; }
    /// <summary>Opaque rendered scene and its protected QR rectangle.</summary>
    public QrImageComposition Image { get; }
    /// <summary>QR version encoded into this result.</summary>
    public int QrVersion => Code.Version;
    /// <summary>Returns an independent copy of the settings used for this result.</summary>
    public QrSceneOptions Design => _design.Clone();
    internal QrCode Code { get; }
    internal QrSceneGeometry Geometry { get; }
    internal QrPngRenderOptions QrOptions { get; }
    internal int QuarterTurns { get; }
    private readonly QrSceneOptions _design;

    internal QrSceneComposition(string payload, QrCode code, QrSceneOptions design, QrSceneGeometry geometry,
        QrPngRenderOptions qrOptions, int turns, QrImageComposition image) {
        Payload = payload; Code = code; _design = design; Geometry = geometry; QrOptions = qrOptions; QuarterTurns = turns; Image = image;
    }
    /// <summary>Returns the final scene as a lossless PNG.</summary>
    public byte[] ToPng() => Image.ToPng();
    /// <summary>Saves the exact rendered scene as PNG.</summary>
    public string SavePng(string path) => Image.SavePng(path);
    /// <summary>Snapshots the exact payload and settings which produced this artwork.</summary>
    public QrSceneRecipe ToRecipe() => new(Payload, _design);
}

namespace CodeGlyphX.Rendering.Art;

/// <summary>The crop, zoom, canvas size and QR placement selected for an image candidate.</summary>
public sealed class QrImageLayout {
    /// <summary>Source crop alignment, from left (0) to right (1).</summary>
    public double ImageX { get; }
    /// <summary>Source crop alignment, from top (0) to bottom (1).</summary>
    public double ImageY { get; }
    /// <summary>Source magnification after fitting.</summary>
    public double Zoom { get; }
    /// <summary>Additional canvas border, in modules.</summary>
    public int PaddingModules { get; }
    /// <summary>QR horizontal placement within the canvas margin.</summary>
    public double QrX { get; }
    /// <summary>QR vertical placement within the canvas margin.</summary>
    public double QrY { get; }

    internal QrImageLayout(QrImageCompositionOptions options) {
        ImageX = options.ImagePositionX; ImageY = options.ImagePositionY; Zoom = options.ImageZoom;
        PaddingModules = options.Canvas?.PaddingModules ?? 0;
        QrX = options.Canvas?.PositionX ?? 0.5; QrY = options.Canvas?.PositionY ?? 0.5;
    }
}

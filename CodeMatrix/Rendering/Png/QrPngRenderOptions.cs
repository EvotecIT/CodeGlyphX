namespace CodeMatrix.Rendering.Png;

public sealed class QrPngRenderOptions {
    public int ModuleSize { get; set; } = 4;
    public int QuietZone { get; set; } = 4;
    public Rgba32 Foreground { get; set; } = Rgba32.Black;
    public Rgba32 Background { get; set; } = Rgba32.White;
}


namespace CodeMatrix.Rendering.Png;

public sealed class BarcodePngRenderOptions {
    public int ModuleSize { get; set; } = 2;
    public int QuietZone { get; set; } = 10;
    public int HeightModules { get; set; } = 40;
    public Rgba32 Foreground { get; set; } = Rgba32.Black;
    public Rgba32 Background { get; set; } = Rgba32.White;
}


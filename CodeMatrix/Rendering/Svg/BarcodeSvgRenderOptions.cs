namespace CodeMatrix.Rendering.Svg;

public sealed class BarcodeSvgRenderOptions {
    public int ModuleSize { get; set; } = 2;
    public int QuietZone { get; set; } = 10;
    public int HeightModules { get; set; } = 40;

    public string BarColor { get; set; } = "#000";
    public string BackgroundColor { get; set; } = "#fff";
}


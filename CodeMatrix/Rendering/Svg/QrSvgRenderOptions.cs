namespace CodeMatrix.Rendering.Svg;

public sealed class QrSvgRenderOptions {
    public int ModuleSize { get; set; } = 4;
    public int QuietZone { get; set; } = 4;

    public string DarkColor { get; set; } = "#000";
    public string LightColor { get; set; } = "#fff";
}


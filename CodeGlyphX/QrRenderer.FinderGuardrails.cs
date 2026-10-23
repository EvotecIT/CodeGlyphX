using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

internal static partial class QrRenderer {
    // Finder rings and balls are dark evidence, even when a decorative legacy frame used a white centre.
    private static void NormalizeFinderContrast(QrPngRenderOptions render) {
        if (Luminance(render.Background) < 160) render.Background = Rgba32.White;
        render.Foreground = DarkFinderColor(render.Foreground);
        NormalizeGradient(render.ForegroundGradient);
        Normalize(render.ForegroundPalette?.Colors);
        Normalize(render.ForegroundPaletteZones?.CenterPalette?.Colors);
        Normalize(render.ForegroundPaletteZones?.CornerPalette?.Colors);
        if (render.ForegroundPattern is not null) {
            var pattern = render.ForegroundPattern;
            pattern.Color = WithAlpha(pattern.Color, (byte)System.Math.Min(48, (int)pattern.Color.A));
            pattern.BlendMode = QrForegroundPatternBlendMode.Overlay;
        }
        var eye = render.Eyes;
        if (eye is null) return;
        if (eye.OuterColor.HasValue) eye.OuterColor = DarkFinderColor(eye.OuterColor.Value);
        if (eye.InnerColor.HasValue) eye.InnerColor = DarkFinderColor(eye.InnerColor.Value);
        Normalize(eye.OuterColors);
        Normalize(eye.InnerColors);
        NormalizeGradient(eye.OuterGradient);
        NormalizeGradient(eye.InnerGradient);
        if (eye.OuterGradients is not null) foreach (var gradient in eye.OuterGradients) NormalizeGradient(gradient);
        if (eye.InnerGradients is not null) foreach (var gradient in eye.InnerGradients) NormalizeGradient(gradient);
    }

    private static void Normalize(Rgba32[]? colors) {
        if (colors is null) return;
        for (var i = 0; i < colors.Length; i++) colors[i] = DarkFinderColor(colors[i]);
    }

    private static void NormalizeGradient(QrGradientOptions? gradient) {
        if (gradient is null) return;
        gradient.StartColor = DarkFinderColor(gradient.StartColor);
        gradient.EndColor = DarkFinderColor(gradient.EndColor);
    }

    private static Rgba32 DarkFinderColor(Rgba32 color) {
        var luminance = Luminance(color);
        if (luminance <= 90) return new Rgba32(color.R, color.G, color.B);
        var scale = 90.0 / luminance;
        return new Rgba32((byte)(color.R * scale), (byte)(color.G * scale), (byte)(color.B * scale));
    }

    private static int Luminance(Rgba32 color) => (299 * color.R + 587 * color.G + 114 * color.B) / 1000;
}

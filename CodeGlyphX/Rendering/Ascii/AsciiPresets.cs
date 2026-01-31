using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Ready-to-use ASCII render presets tuned for console readability.
/// </summary>
public static class AsciiPresets {
    /// <summary>
    /// Creates a console-friendly preset that is much easier to scan from a terminal.
    /// </summary>
    /// <param name="scale">Scale multiplier applied to module size (try 3-5 for phones).</param>
    /// <param name="useAnsiColors">Whether to emit ANSI color escape codes.</param>
    /// <param name="trueColor">Whether to use ANSI truecolor (24-bit) output.</param>
    /// <param name="darkColor">Optional ANSI color for dark modules.</param>
    /// <param name="lightColor">Optional ANSI color for light modules.</param>
    public static MatrixAsciiRenderOptions Console(
        int scale = 3,
        bool useAnsiColors = true,
        bool trueColor = true,
        Rgba32? darkColor = null,
        Rgba32? lightColor = null) {
        if (scale <= 0) scale = 1;

        return new MatrixAsciiRenderOptions {
            QuietZone = RenderDefaults.QrQuietZone,
            UseUnicodeBlocks = true,
            UseAnsiColors = useAnsiColors,
            UseAnsiTrueColor = trueColor,
            AnsiDarkColor = darkColor ?? new Rgba32(16, 16, 16),
            AnsiLightColor = lightColor ?? RenderDefaults.QrBackground,
            AnsiColorizeLight = true,
            Scale = scale,
            ModuleWidth = 2,
            ModuleHeight = 1,
        };
    }

    /// <summary>
    /// Creates a compact console preset that uses Unicode half-blocks.
    /// </summary>
    /// <param name="scale">Scale multiplier applied to module size (try 2-3 for screens).</param>
    /// <param name="useAnsiColors">Whether to emit ANSI color escape codes.</param>
    /// <param name="trueColor">Whether to use ANSI truecolor (24-bit) output.</param>
    /// <param name="darkColor">Optional ANSI color for dark modules.</param>
    /// <param name="lightColor">Optional ANSI color for light modules.</param>
    public static MatrixAsciiRenderOptions ConsoleCompact(
        int scale = 1,
        bool useAnsiColors = true,
        bool trueColor = true,
        Rgba32? darkColor = null,
        Rgba32? lightColor = null) {
        if (scale <= 0) scale = 1;

        return new MatrixAsciiRenderOptions {
            QuietZone = RenderDefaults.QrQuietZone,
            UseUnicodeBlocks = true,
            UseHalfBlocks = true,
            UseAnsiColors = useAnsiColors,
            UseAnsiTrueColor = trueColor,
            AnsiDarkColor = darkColor ?? new Rgba32(16, 16, 16),
            AnsiLightColor = lightColor ?? RenderDefaults.QrBackground,
            AnsiColorizeLight = true,
            Scale = scale,
            ModuleWidth = 1,
            ModuleHeight = 1,
        };
    }
}

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
    public static MatrixAsciiRenderOptions Console(
        int scale = 3,
        bool useAnsiColors = true,
        bool trueColor = true,
        Rgba32? darkColor = null) {
        if (scale <= 0) scale = 1;

        return new MatrixAsciiRenderOptions {
            QuietZone = RenderDefaults.QrQuietZone,
            UseUnicodeBlocks = true,
            UseAnsiColors = useAnsiColors,
            UseAnsiTrueColor = trueColor,
            AnsiDarkColor = darkColor ?? new Rgba32(16, 16, 16),
            Scale = scale,
            ModuleWidth = 2,
            ModuleHeight = 1,
        };
    }
}

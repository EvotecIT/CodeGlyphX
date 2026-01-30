using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Ready-to-use console presets for auto-fit rendering.
/// </summary>
public static class AsciiConsolePresets {
    /// <summary>
    /// Square-ish output (no half-blocks).
    /// </summary>
    public static AsciiConsoleOptions Square() {
        return new AsciiConsoleOptions {
            UseHalfBlocks = false,
            UseUnicodeBlocks = true,
            QuietZone = 2,
            MinScale = 1,
            MaxScale = 2,
            UseAnsiColors = true,
            ColorizeLight = true,
            CellAspectRatio = 0.5
        };
    }

    /// <summary>
    /// Compact output (half-blocks).
    /// </summary>
    public static AsciiConsoleOptions Compact() {
        return new AsciiConsoleOptions {
            UseHalfBlocks = true,
            HalfBlockUseBackground = true,
            UseUnicodeBlocks = true,
            QuietZone = 2,
            MinScale = 1,
            MaxScale = 2,
            UseAnsiColors = true,
            ColorizeLight = true,
            CellAspectRatio = 0.5
        };
    }

    /// <summary>
    /// Compact output with scan-friendly defaults.
    /// </summary>
    public static AsciiConsoleOptions ScanSafe() {
        return new AsciiConsoleOptions {
            UseHalfBlocks = true,
            HalfBlockUseBackground = true,
            UseUnicodeBlocks = true,
            QuietZone = 2,
            MinScale = 1,
            MaxScale = 2,
            UseAnsiColors = true,
            ColorizeLight = true,
            PreferScanReliability = true,
            CellAspectRatio = 0.5
        };
    }

    /// <summary>
    /// Tiny output for tight layouts (may be less scan-friendly).
    /// </summary>
    public static AsciiConsoleOptions Tiny() {
        return new AsciiConsoleOptions {
            UseHalfBlocks = true,
            HalfBlockUseBackground = true,
            UseUnicodeBlocks = true,
            QuietZone = 1,
            MinScale = 1,
            MaxScale = 1,
            UseAnsiColors = true,
            ColorizeLight = true,
            TargetWidth = 24,
            TargetHeight = 12,
            CellAspectRatio = 0.45
        };
    }

    /// <summary>
    /// Compact output tuned for dark terminals (foreground only).
    /// </summary>
    /// <param name="lightColor">Optional foreground color for dark modules.</param>
    public static AsciiConsoleOptions CompactDark(Rgba32? lightColor = null) {
        return BuildCompactDark(lightColor);
    }

    /// <summary>
    /// Compact output tuned for dark terminals (foreground only).
    /// </summary>
    /// <param name="foregroundColor">Optional foreground color for dark modules.</param>
    public static AsciiConsoleOptions CompactDarkForeground(Rgba32? foregroundColor = null) {
        return BuildCompactDark(foregroundColor);
    }

    private static AsciiConsoleOptions BuildCompactDark(Rgba32? foregroundColor) {
        return new AsciiConsoleOptions {
            UseHalfBlocks = true,
            HalfBlockUseBackground = false,
            UseUnicodeBlocks = true,
            QuietZone = 2,
            MinScale = 1,
            MaxScale = 2,
            UseAnsiColors = true,
            ColorizeLight = false,
            DarkColor = foregroundColor ?? new Rgba32(235, 235, 235),
            CellAspectRatio = 0.5
        };
    }
}

using System;
using System.Text;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Helpers for console-friendly ASCII rendering.
/// </summary>
public static class AsciiConsole {
    /// <summary>
    /// Builds ASCII render options that auto-fit a console window.
    /// </summary>
    public static MatrixAsciiRenderOptions Fit(BitMatrix modules, AsciiConsoleOptions? options = null) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        var opts = options ?? new AsciiConsoleOptions();

        var windowWidth = opts.WindowWidth ?? GetConsoleWidth();
        var windowHeight = opts.WindowHeight ?? GetConsoleHeight();
        if (windowWidth <= 0) windowWidth = 120;
        if (windowHeight <= 0) windowHeight = 40;
        if (opts.TargetWidth.HasValue && opts.TargetWidth.Value > 0) {
            windowWidth = opts.TargetWidth.Value + Math.Max(0, opts.PaddingColumns);
        }
        if (opts.TargetHeight.HasValue && opts.TargetHeight.Value > 0) {
            windowHeight = opts.TargetHeight.Value + Math.Max(0, opts.PaddingRows);
        }
        if (opts.MaxWindowWidth.HasValue && opts.MaxWindowWidth.Value > 0 && windowWidth > opts.MaxWindowWidth.Value) {
            windowWidth = opts.MaxWindowWidth.Value;
        }
        if (opts.MaxWindowHeight.HasValue && opts.MaxWindowHeight.Value > 0 && windowHeight > opts.MaxWindowHeight.Value) {
            windowHeight = opts.MaxWindowHeight.Value;
        }

        var requestedHalfBlocks = opts.UseHalfBlocks;
        var widthOverride = opts.ModuleWidth.HasValue;
        var heightOverride = opts.ModuleHeight.HasValue;
        var useAnsiColors = opts.UseAnsiColors ?? DetectAnsiSupport();
        var useTrueColor = opts.UseTrueColor ?? DetectTrueColorSupport();
        if (!useAnsiColors) {
            useTrueColor = false;
        }

        var baseOptions = opts.UseHalfBlocks
            ? AsciiPresets.ConsoleCompact(scale: 1, useAnsiColors: useAnsiColors, trueColor: useTrueColor, darkColor: opts.DarkColor, lightColor: opts.LightColor)
            : AsciiPresets.Console(scale: 1, useAnsiColors: useAnsiColors, trueColor: useTrueColor, darkColor: opts.DarkColor, lightColor: opts.LightColor);

        baseOptions.UseUnicodeBlocks = opts.UseUnicodeBlocks;
        baseOptions.UseHalfBlocks = opts.UseHalfBlocks;
        baseOptions.UseHalfBlockBackground = opts.HalfBlockUseBackground;
        if (opts.DarkGradient is not null) baseOptions.AnsiDarkGradient = opts.DarkGradient;
        if (opts.DarkPalette is not null) baseOptions.AnsiDarkPalette = opts.DarkPalette;
        baseOptions.EnsureDarkContrast = opts.EnsureDarkContrast || opts.PreferScanReliability;
        if (baseOptions.EnsureDarkContrast) {
            baseOptions.MaxDarkLuminance = opts.MaxDarkLuminance;
        }
        if (opts.ModuleWidth.HasValue) baseOptions.ModuleWidth = Math.Max(1, opts.ModuleWidth.Value);
        if (opts.ModuleHeight.HasValue) baseOptions.ModuleHeight = Math.Max(1, opts.ModuleHeight.Value);
        if (!string.IsNullOrEmpty(opts.Dark)) baseOptions.Dark = opts.Dark!;
        if (!string.IsNullOrEmpty(opts.Light)) baseOptions.Light = opts.Light!;
        if (!string.IsNullOrEmpty(opts.NewLine)) baseOptions.NewLine = opts.NewLine!;

        var outputEncoding = opts.OutputEncoding ?? GetConsoleEncoding();
        if (outputEncoding is not null) {
            var canBlocks = CanEncode(outputEncoding, "█");
            var canHalfBlocks = CanEncode(outputEncoding, "▀") && CanEncode(outputEncoding, "▄");
            if (baseOptions.UseHalfBlocks && !canHalfBlocks) {
                baseOptions.UseHalfBlocks = false;
            }
            if (baseOptions.UseUnicodeBlocks && !canBlocks) {
                baseOptions.UseUnicodeBlocks = false;
            }
        }

        var useHalfBlocks = baseOptions.UseHalfBlocks;
        if (!useHalfBlocks && requestedHalfBlocks && !widthOverride) {
            if (baseOptions.ModuleWidth < 2) {
                baseOptions.ModuleWidth = 2;
            }
            if (!heightOverride && baseOptions.ModuleHeight < 1) {
                baseOptions.ModuleHeight = 1;
            }
        }
        if (opts.PreferScanReliability) {
            baseOptions.UseHalfBlockBackground = true;
        }
        if (!baseOptions.UseHalfBlockBackground) {
            baseOptions.AnsiColorizeLight = false;
        } else if (opts.ColorizeLight.HasValue) {
            baseOptions.AnsiColorizeLight = opts.ColorizeLight.Value;
        }
        if (opts.PreferScanReliability) {
            baseOptions.AnsiColorizeLight = true;
        }
        if (opts.Invert.HasValue) baseOptions.Invert = opts.Invert.Value;

        var quiet = opts.QuietZone ?? baseOptions.QuietZone;
        var minQuiet = Math.Max(0, opts.MinQuietZone);
        if (opts.PreferScanReliability && minQuiet < 2) {
            minQuiet = 2;
        }
        if (quiet < minQuiet) quiet = minQuiet;
        var moduleWidth = Math.Max(1, baseOptions.ModuleWidth);
        var moduleHeight = Math.Max(1, baseOptions.ModuleHeight);
        var cellAspect = opts.CellAspectRatio;

        if (!widthOverride && cellAspect.HasValue && cellAspect.Value > 0) {
            moduleWidth = ComputeModuleWidthFromAspect(modules, quiet, moduleHeight, useHalfBlocks, cellAspect.Value);
            baseOptions.ModuleWidth = moduleWidth;
        }

        var usableWidth = Math.Max(10, windowWidth - Math.Max(0, opts.PaddingColumns));
        var usableHeight = Math.Max(5, windowHeight - Math.Max(0, opts.PaddingRows));

        var quietChanged = false;
        var allowQuietShrink = opts.AllowQuietZoneShrink && !opts.PreferScanReliability;
        if (allowQuietShrink) {
            var maxQuietWidth = (usableWidth / moduleWidth - modules.Width) / 2;
            var maxQuietHeight = ((usableHeight * (useHalfBlocks ? 2 : 1)) / moduleHeight - modules.Height) / 2;
            var maxQuiet = Math.Min(maxQuietWidth, maxQuietHeight);
            if (maxQuiet < 0) maxQuiet = 0;
            if (maxQuiet < quiet) {
                quiet = maxQuiet < minQuiet ? maxQuiet : Math.Max(minQuiet, maxQuiet);
                quietChanged = true;
            }
        }

        if (quiet < 0) quiet = 0;
        baseOptions.QuietZone = quiet;

        if (!widthOverride && quietChanged && cellAspect.HasValue && cellAspect.Value > 0) {
            moduleWidth = ComputeModuleWidthFromAspect(modules, quiet, moduleHeight, useHalfBlocks, cellAspect.Value);
            baseOptions.ModuleWidth = moduleWidth;
        }

        var widthModules = modules.Width + quiet * 2;
        var heightModules = modules.Height + quiet * 2;
        var heightRows = useHalfBlocks ? (heightModules + 1) / 2 : heightModules;

        if (!useHalfBlocks && !widthOverride && opts.AllowModuleWidthShrink && moduleWidth > 1) {
            var scaleFitWidthShrink = usableWidth / Math.Max(1, widthModules * moduleWidth);
            if (scaleFitWidthShrink < 1) {
                moduleWidth = 1;
                baseOptions.ModuleWidth = 1;
            }
        }

        var scaleFitWidth = widthModules > 0 ? usableWidth / Math.Max(1, widthModules * moduleWidth) : 1;
        var scaleFitHeight = heightRows > 0 ? usableHeight / Math.Max(1, heightRows * moduleHeight) : 1;
        var scaleFit = Math.Min(scaleFitWidth, scaleFitHeight);
        var minScale = Math.Max(1, opts.MinScale);
        var maxScale = Math.Max(minScale, opts.MaxScale);
        if (scaleFit < minScale) scaleFit = minScale;
        if (scaleFit > maxScale) scaleFit = maxScale;
        if (scaleFit < 1) scaleFit = 1;

        baseOptions.Scale = scaleFit;
        return baseOptions;
    }

    private static int ComputeModuleWidthFromAspect(BitMatrix modules, int quiet, int moduleHeight, bool useHalfBlocks, double cellAspect) {
        var widthModules = modules.Width + quiet * 2;
        var heightModules = modules.Height + quiet * 2;
        if (widthModules <= 0 || heightModules <= 0) return 1;

        var heightRows = useHalfBlocks ? (heightModules + 1) / 2.0 : heightModules;
        var targetWidth = (heightRows * moduleHeight) / widthModules / cellAspect;
        if (double.IsNaN(targetWidth) || double.IsInfinity(targetWidth)) return 1;
        var rounded = (int)Math.Round(targetWidth, MidpointRounding.AwayFromZero);
        if (rounded < 1) rounded = 1;
        return rounded;
    }

    /// <summary>
    /// Renders a matrix as console-friendly ASCII with auto-fit.
    /// </summary>
    public static string Render(BitMatrix modules, AsciiConsoleOptions? options = null) {
        var renderOptions = Fit(modules, options);
        return MatrixAsciiRenderer.Render(modules, renderOptions);
    }

    private static int GetConsoleWidth() {
        try {
            return Console.WindowWidth;
        } catch {
            return 0;
        }
    }

    private static int GetConsoleHeight() {
        try {
            return Console.WindowHeight;
        } catch {
            return 0;
        }
    }

    private static bool IsOutputRedirected() {
        try {
            return Console.IsOutputRedirected;
        } catch {
            return false;
        }
    }

    private static bool DetectAnsiSupport() {
        if (IsOutputRedirected()) return false;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"))) return false;

        var term = Environment.GetEnvironmentVariable("TERM");
        if (string.IsNullOrEmpty(term)) return true;
        if (string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    private static bool DetectTrueColorSupport() {
        var colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        if (!string.IsNullOrEmpty(colorTerm)) {
            if (colorTerm.IndexOf("truecolor", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (colorTerm.IndexOf("24bit", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION"))) return true;

        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        if (!string.IsNullOrEmpty(termProgram)) {
            if (termProgram.IndexOf("vscode", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (termProgram.IndexOf("wezterm", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (termProgram.IndexOf("iterm", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        var term = Environment.GetEnvironmentVariable("TERM");
        if (!string.IsNullOrEmpty(term)) {
            if (term.IndexOf("truecolor", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (term.IndexOf("direct", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        return false;
    }

    private static Encoding? GetConsoleEncoding() {
        try {
            return Console.OutputEncoding;
        } catch {
            return null;
        }
    }

    private static bool CanEncode(Encoding encoding, string value) {
        try {
            var bytes = encoding.GetBytes(value);
            var roundtrip = encoding.GetString(bytes);
            return string.Equals(roundtrip, value, StringComparison.Ordinal);
        } catch {
            return false;
        }
    }
}

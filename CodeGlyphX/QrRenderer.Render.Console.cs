using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX;

internal static partial class QrRenderer {
    private static AsciiConsoleOptions MergeConsoleOptions(AsciiConsoleOptions? consoleOptions, int quietZone) {
        if (consoleOptions is null) {
            return new AsciiConsoleOptions {
                QuietZone = quietZone
            };
        }

        return new AsciiConsoleOptions {
            WindowWidth = consoleOptions.WindowWidth,
            WindowHeight = consoleOptions.WindowHeight,
            MaxWindowWidth = consoleOptions.MaxWindowWidth,
            MaxWindowHeight = consoleOptions.MaxWindowHeight,
            TargetWidth = consoleOptions.TargetWidth,
            TargetHeight = consoleOptions.TargetHeight,
            PaddingColumns = consoleOptions.PaddingColumns,
            PaddingRows = consoleOptions.PaddingRows,
            MinScale = consoleOptions.MinScale,
            MaxScale = consoleOptions.MaxScale,
            MinQuietZone = consoleOptions.MinQuietZone,
            QuietZone = consoleOptions.QuietZone ?? quietZone,
            AllowQuietZoneShrink = consoleOptions.AllowQuietZoneShrink,
            AllowModuleWidthShrink = consoleOptions.AllowModuleWidthShrink,
            ModuleWidth = consoleOptions.ModuleWidth,
            ModuleHeight = consoleOptions.ModuleHeight,
            Dark = consoleOptions.Dark,
            Light = consoleOptions.Light,
            NewLine = consoleOptions.NewLine,
            UseHalfBlocks = consoleOptions.UseHalfBlocks,
            HalfBlockUseBackground = consoleOptions.HalfBlockUseBackground,
            UseUnicodeBlocks = consoleOptions.UseUnicodeBlocks,
            UseAnsiColors = consoleOptions.UseAnsiColors,
            UseTrueColor = consoleOptions.UseTrueColor,
            ColorizeLight = consoleOptions.ColorizeLight,
            CellAspectRatio = consoleOptions.CellAspectRatio,
            DarkGradient = consoleOptions.DarkGradient,
            DarkPalette = consoleOptions.DarkPalette,
            UseConservativeQrLayout = consoleOptions.UseConservativeQrLayout,
            EnsureDarkContrast = consoleOptions.EnsureDarkContrast,
            MaxDarkLuminance = consoleOptions.MaxDarkLuminance,
            DarkColor = consoleOptions.DarkColor,
            LightColor = consoleOptions.LightColor,
            OutputEncoding = consoleOptions.OutputEncoding,
            Invert = consoleOptions.Invert
        };
    }
}

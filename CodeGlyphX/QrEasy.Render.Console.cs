using System;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX;

public static partial class QrEasy {
    /// <summary>
    /// Renders a QR code as console-friendly ASCII with auto-fit.
    /// </summary>
    public static string RenderAsciiConsole(string payload, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = EncodePayload(payload, opts);
        var fitOptions = MergeConsoleOptions(consoleOptions, opts.QuietZone);
        return AsciiConsole.Render(qr.Modules, fitOptions);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as console-friendly ASCII with auto-fit.
    /// </summary>
    public static string RenderAsciiConsoleAuto(string payload, QrPayloadDetectOptions? detectOptions = null, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderAsciiConsole(detected, consoleOptions, options);
    }

    /// <summary>
    /// Renders a QR code as console-friendly ASCII with auto-fit for a payload with embedded defaults.
    /// </summary>
    public static string RenderAsciiConsole(QrPayloadData payload, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var fitOptions = MergeConsoleOptions(consoleOptions, opts.QuietZone);
        return AsciiConsole.Render(qr.Modules, fitOptions);
    }

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
            PreferScanReliability = consoleOptions.PreferScanReliability,
            EnsureDarkContrast = consoleOptions.EnsureDarkContrast,
            MaxDarkLuminance = consoleOptions.MaxDarkLuminance,
            DarkColor = consoleOptions.DarkColor,
            LightColor = consoleOptions.LightColor,
            OutputEncoding = consoleOptions.OutputEncoding,
            Invert = consoleOptions.Invert
        };
    }
}

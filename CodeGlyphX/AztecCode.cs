using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

/// <summary>
/// Aztec code helpers.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// AztecCode.Save("Ticket: CONF-2024", "ticket.png");
/// </code>
/// </example>
public static partial class AztecCode {
    /// <summary>
    /// Encodes a text payload as Aztec.
    /// </summary>
    public static BitMatrix Encode(string text, AztecEncodeOptions? options = null) {
        return AztecEncoder.Encode(text, options);
    }

    /// <summary>
    /// Encodes binary payload as Aztec.
    /// </summary>
    public static BitMatrix Encode(ReadOnlySpan<byte> data, AztecEncodeOptions? options = null) {
        if (data.Length == 0) return AztecEncoder.Encode(Array.Empty<byte>(), options?.ErrorCorrectionPercent ?? 33, 0).Matrix;
        var bytes = data.ToArray();
        var eccPercent = options?.ErrorCorrectionPercent ?? 33;
        var userSpecifiedLayers = 0;
        if (options?.Layers is int layers && layers > 0) {
            var compact = options.Compact ?? layers <= 4;
            userSpecifiedLayers = compact ? -layers : layers;
        }
        return AztecEncoder.Encode(bytes, eccPercent, userSpecifiedLayers).Matrix;
    }

    /// <summary>
    /// Saves Aztec to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }

    private static MatrixPngRenderOptions ToPngOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixPngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background
        };
    }

    private static IcoRenderOptions ToIcoOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static MatrixSvgRenderOptions ToSvgOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background)
        };
    }

    private static MatrixHtmlRenderOptions ToHtmlOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable
        };
    }

    private static string ToCss(Rgba32 color) {
        if (color.A == 255) {
            return $"rgb({color.R},{color.G},{color.B})";
        }
        var a = color.A / 255.0;
        return $"rgba({color.R},{color.G},{color.B},{a:0.###})";
    }
}

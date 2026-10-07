using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;

namespace CodeGlyphX;

public static partial class Barcode {
    private static BarcodePngRenderOptions BuildPngOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodePngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            Foreground = opts.Foreground,
            Background = opts.Background,
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = opts.LabelColor,
        };
    }

    private static IcoRenderOptions BuildIcoOptions(OutputOptions? outputOptions) {
        var opts = outputOptions ?? new OutputOptions();
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static BarcodeSvgRenderOptions BuildSvgOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodeSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            BarColor = ColorUtils.ToCss(opts.Foreground),
            BackgroundColor = ColorUtils.ToCss(opts.Background),
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = ColorUtils.ToCss(opts.LabelColor),
            LabelFontFamily = opts.LabelFontFamily,
        };
    }

    private static BarcodeHtmlRenderOptions BuildHtmlOptions(BarcodeOptions? options, OutputOptions? outputOptions) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodeHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            BarColor = ColorUtils.ToCss(opts.Foreground),
            BackgroundColor = ColorUtils.ToCss(opts.Background),
            EmailSafeTable = outputOptions?.HtmlEmailSafeTable ?? false,
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = ColorUtils.ToCss(opts.LabelColor),
            LabelFontFamily = opts.LabelFontFamily,
        };
    }

}

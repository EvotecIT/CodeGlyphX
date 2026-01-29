using System;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Renders Macro PDF417 as PNG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PngMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixPngRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as SVG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string SvgMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixSvgRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as SVGZ.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] SvgzMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixSvgzRenderer.Render(modules, BuildSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as HTML.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string HtmlMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as JPEG.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] JpegMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        var opts = BuildPngOptions(renderOptions);
        var quality = renderOptions?.JpegQuality ?? 85;
        return MatrixJpegRenderer.Render(modules, opts, quality);
    }

    /// <summary>
    /// Renders Macro PDF417 as BMP.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] BmpMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixBmpRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as PPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PpmMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixPpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as PBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PbmMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixPbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as PGM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PgmMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixPgmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as PAM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PamMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixPamRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as XBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string XbmMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixXbmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as XPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string XpmMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixXpmRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as TGA.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] TgaMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixTgaRenderer.Render(modules, BuildPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as ICO.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] IcoMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixIcoRenderer.Render(modules, BuildPngOptions(renderOptions), BuildIcoOptions(renderOptions));
    }

    /// <summary>
    /// Renders Macro PDF417 as PDF.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="macro">Macro PDF417 metadata.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] PdfMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixPdfRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Macro PDF417 as EPS.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="macro">Macro PDF417 metadata.</param>
    /// <param name="encodeOptions">Encoding options.</param>
    /// <param name="renderOptions">Rendering options.</param>
    /// <param name="renderMode">Vector or raster output.</param>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string EpsMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixEpsRenderer.Render(modules, BuildPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Macro PDF417 as ASCII.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string AsciiMacro(string text, Pdf417MacroOptions macro, Pdf417EncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? options = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, options);
    }
}

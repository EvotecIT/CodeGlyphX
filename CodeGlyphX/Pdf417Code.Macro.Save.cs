using System;
using System.IO;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Saves Macro PDF417 to a file based on extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveMacro(string text, Pdf417MacroOptions macro, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null, RenderMode renderMode = RenderMode.Vector) {
        return SaveByExtensionHelper.Save(path, new SaveByExtensionHandlers {
            Default = () => PngMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Png = () => PngMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Webp = () => PngMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Svg = () => SvgMacro(text, macro, encodeOptions, renderOptions).WriteText(path),
            Svgz = () => SvgzMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Html = () => {
                var html = HtmlMacro(text, macro, encodeOptions, renderOptions);
                if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
                return html.WriteText(path);
            },
            Jpeg = () => JpegMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Bmp = () => BmpMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Ppm = () => PpmMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Pbm = () => PbmMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Pgm = () => PgmMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Pam = () => PamMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Xbm = () => XbmMacro(text, macro, encodeOptions, renderOptions).WriteText(path),
            Xpm = () => XpmMacro(text, macro, encodeOptions, renderOptions).WriteText(path),
            Tga = () => TgaMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Ico = () => IcoMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path),
            Pdf = () => PdfMacro(text, macro, encodeOptions, renderOptions, renderMode).WriteBinary(path),
            Eps = () => EpsMacro(text, macro, encodeOptions, renderOptions, renderMode).WriteText(path)
        });
    }
}

using System;
using System.IO;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Saves Macro PDF417 to a file based on extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveMacro(string text, Pdf417MacroOptions macro, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null, RenderMode renderMode = RenderMode.Vector) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SvgzMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return PngMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return PngMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".svg":
                return SvgMacro(text, macro, encodeOptions, renderOptions).WriteText(path);
            case ".html":
            case ".htm":
                var html = HtmlMacro(text, macro, encodeOptions, renderOptions);
                if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
                return html.WriteText(path);
            case ".jpg":
            case ".jpeg":
                return JpegMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".bmp":
                return BmpMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".ppm":
                return PpmMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".pbm":
                return PbmMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".pgm":
                return PgmMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".pam":
                return PamMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".xbm":
                return XbmMacro(text, macro, encodeOptions, renderOptions).WriteText(path);
            case ".xpm":
                return XpmMacro(text, macro, encodeOptions, renderOptions).WriteText(path);
            case ".tga":
                return TgaMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".ico":
                return IcoMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".svgz":
                return SvgzMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
            case ".pdf":
                return PdfMacro(text, macro, encodeOptions, renderOptions, renderMode).WriteBinary(path);
            case ".eps":
            case ".ps":
                return EpsMacro(text, macro, encodeOptions, renderOptions, renderMode).WriteText(path);
            default:
                return PngMacro(text, macro, encodeOptions, renderOptions).WriteBinary(path);
        }
    }
}

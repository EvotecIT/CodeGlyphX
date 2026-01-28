using System;
using System.IO;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Saves PDF417 to a file based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(text, path, encodeOptions, renderOptions);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(text, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(text, path, encodeOptions, renderOptions);
            case ".webp":
                return SaveWebp(text, path, encodeOptions, renderOptions);
            case ".svg":
                return SaveSvg(text, path, encodeOptions, renderOptions);
            case ".html":
            case ".htm":
                return SaveHtml(text, path, encodeOptions, renderOptions, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(text, path, encodeOptions, renderOptions);
            case ".bmp":
                return SaveBmp(text, path, encodeOptions, renderOptions);
            case ".ppm":
                return SavePpm(text, path, encodeOptions, renderOptions);
            case ".pbm":
                return SavePbm(text, path, encodeOptions, renderOptions);
            case ".pgm":
                return SavePgm(text, path, encodeOptions, renderOptions);
            case ".pam":
                return SavePam(text, path, encodeOptions, renderOptions);
            case ".xbm":
                return SaveXbm(text, path, encodeOptions, renderOptions);
            case ".xpm":
                return SaveXpm(text, path, encodeOptions, renderOptions);
            case ".tga":
                return SaveTga(text, path, encodeOptions, renderOptions);
            case ".ico":
                return SaveIco(text, path, encodeOptions, renderOptions);
            case ".svgz":
                return SaveSvgz(text, path, encodeOptions, renderOptions);
            case ".pdf":
                return SavePdf(text, path, encodeOptions, renderOptions);
            case ".eps":
            case ".ps":
                return SaveEps(text, path, encodeOptions, renderOptions);
            default:
                return SavePng(text, path, encodeOptions, renderOptions);
        }
    }

    /// <summary>
    /// Saves PDF417 to a file for byte payloads based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(data, path, encodeOptions, renderOptions);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(data, path, encodeOptions, renderOptions);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(data, path, encodeOptions, renderOptions);
            case ".webp":
                return SaveWebp(data, path, encodeOptions, renderOptions);
            case ".svg":
                return SaveSvg(data, path, encodeOptions, renderOptions);
            case ".html":
            case ".htm":
                return SaveHtml(data, path, encodeOptions, renderOptions, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(data, path, encodeOptions, renderOptions);
            case ".bmp":
                return SaveBmp(data, path, encodeOptions, renderOptions);
            case ".ppm":
                return SavePpm(data, path, encodeOptions, renderOptions);
            case ".pbm":
                return SavePbm(data, path, encodeOptions, renderOptions);
            case ".pgm":
                return SavePgm(data, path, encodeOptions, renderOptions);
            case ".pam":
                return SavePam(data, path, encodeOptions, renderOptions);
            case ".xbm":
                return SaveXbm(data, path, encodeOptions, renderOptions);
            case ".xpm":
                return SaveXpm(data, path, encodeOptions, renderOptions);
            case ".tga":
                return SaveTga(data, path, encodeOptions, renderOptions);
            case ".ico":
                return SaveIco(data, path, encodeOptions, renderOptions);
            case ".svgz":
                return SaveSvgz(data, path, encodeOptions, renderOptions);
            case ".pdf":
                return SavePdf(data, path, encodeOptions, renderOptions);
            case ".eps":
            case ".ps":
                return SaveEps(data, path, encodeOptions, renderOptions);
            default:
                return SavePng(data, path, encodeOptions, renderOptions);
        }
    }

}

using System;
using System.IO;
using CodeGlyphX.DataMatrix;

namespace CodeGlyphX;

public static partial class DataMatrixCode {
    /// <summary>
    /// Saves Data Matrix to a file based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(text, path, mode, options);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(text, path, mode, options);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(text, path, mode, options);
            case ".webp":
                return SaveWebp(text, path, mode, options);
            case ".svg":
                return SaveSvg(text, path, mode, options);
            case ".html":
            case ".htm":
                return SaveHtml(text, path, mode, options, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(text, path, mode, options);
            case ".bmp":
                return SaveBmp(text, path, mode, options);
            case ".ppm":
                return SavePpm(text, path, mode, options);
            case ".pbm":
                return SavePbm(text, path, mode, options);
            case ".pgm":
                return SavePgm(text, path, mode, options);
            case ".pam":
                return SavePam(text, path, mode, options);
            case ".xbm":
                return SaveXbm(text, path, mode, options);
            case ".xpm":
                return SaveXpm(text, path, mode, options);
            case ".tga":
                return SaveTga(text, path, mode, options);
            case ".ico":
                return SaveIco(text, path, mode, options);
            case ".svgz":
                return SaveSvgz(text, path, mode, options);
            case ".pdf":
                return SavePdf(text, path, mode, options);
            case ".eps":
            case ".ps":
                return SaveEps(text, path, mode, options);
            default:
                return SavePng(text, path, mode, options);
        }
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(data, path, mode, options);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(data, path, mode, options);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(data, path, mode, options);
            case ".webp":
                return SaveWebp(data, path, mode, options);
            case ".svg":
                return SaveSvg(data, path, mode, options);
            case ".html":
            case ".htm":
                return SaveHtml(data, path, mode, options, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(data, path, mode, options);
            case ".bmp":
                return SaveBmp(data, path, mode, options);
            case ".ppm":
                return SavePpm(data, path, mode, options);
            case ".pbm":
                return SavePbm(data, path, mode, options);
            case ".pgm":
                return SavePgm(data, path, mode, options);
            case ".pam":
                return SavePam(data, path, mode, options);
            case ".xbm":
                return SaveXbm(data, path, mode, options);
            case ".xpm":
                return SaveXpm(data, path, mode, options);
            case ".tga":
                return SaveTga(data, path, mode, options);
            case ".ico":
                return SaveIco(data, path, mode, options);
            case ".svgz":
                return SaveSvgz(data, path, mode, options);
            case ".pdf":
                return SavePdf(data, path, mode, options);
            case ".eps":
            case ".ps":
                return SaveEps(data, path, mode, options);
            default:
                return SavePng(data, path, mode, options);
        }
    }

}

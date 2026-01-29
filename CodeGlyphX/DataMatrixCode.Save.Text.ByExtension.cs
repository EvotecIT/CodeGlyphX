using System;
using System.IO;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class DataMatrixCode {
    /// <summary>
    /// Saves Data Matrix to a file based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        return SaveByExtensionHelper.Save(path, new SaveByExtensionHandlers {
            Default = () => SavePng(text, path, mode, options),
            Png = () => SavePng(text, path, mode, options),
            Webp = () => SaveWebp(text, path, mode, options),
            Svg = () => SaveSvg(text, path, mode, options),
            Svgz = () => SaveSvgz(text, path, mode, options),
            Html = () => SaveHtml(text, path, mode, options, title),
            Jpeg = () => SaveJpeg(text, path, mode, options),
            Bmp = () => SaveBmp(text, path, mode, options),
            Ppm = () => SavePpm(text, path, mode, options),
            Pbm = () => SavePbm(text, path, mode, options),
            Pgm = () => SavePgm(text, path, mode, options),
            Pam = () => SavePam(text, path, mode, options),
            Xbm = () => SaveXbm(text, path, mode, options),
            Xpm = () => SaveXpm(text, path, mode, options),
            Tga = () => SaveTga(text, path, mode, options),
            Ico = () => SaveIco(text, path, mode, options),
            Pdf = () => SavePdf(text, path, mode, options),
            Eps = () => SaveEps(text, path, mode, options)
        });
    }

    /// <summary>
    /// Saves Data Matrix to a file for byte payloads based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, string? title = null) {
        return SaveByExtensionHelper.Save(path, new SaveByExtensionHandlers {
            Default = () => SavePng(data, path, mode, options),
            Png = () => SavePng(data, path, mode, options),
            Webp = () => SaveWebp(data, path, mode, options),
            Svg = () => SaveSvg(data, path, mode, options),
            Svgz = () => SaveSvgz(data, path, mode, options),
            Html = () => SaveHtml(data, path, mode, options, title),
            Jpeg = () => SaveJpeg(data, path, mode, options),
            Bmp = () => SaveBmp(data, path, mode, options),
            Ppm = () => SavePpm(data, path, mode, options),
            Pbm = () => SavePbm(data, path, mode, options),
            Pgm = () => SavePgm(data, path, mode, options),
            Pam = () => SavePam(data, path, mode, options),
            Xbm = () => SaveXbm(data, path, mode, options),
            Xpm = () => SaveXpm(data, path, mode, options),
            Tga = () => SaveTga(data, path, mode, options),
            Ico = () => SaveIco(data, path, mode, options),
            Pdf = () => SavePdf(data, path, mode, options),
            Eps = () => SaveEps(data, path, mode, options)
        });
    }

}

using System;
using System.IO;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Saves PDF417 to a file based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        return SaveByExtensionHelper.Save(path, new SaveByExtensionHandlers {
            Default = () => SavePng(text, path, encodeOptions, renderOptions),
            Png = () => SavePng(text, path, encodeOptions, renderOptions),
            Webp = () => SaveWebp(text, path, encodeOptions, renderOptions),
            Svg = () => SaveSvg(text, path, encodeOptions, renderOptions),
            Svgz = () => SaveSvgz(text, path, encodeOptions, renderOptions),
            Html = () => SaveHtml(text, path, encodeOptions, renderOptions, title),
            Jpeg = () => SaveJpeg(text, path, encodeOptions, renderOptions),
            Bmp = () => SaveBmp(text, path, encodeOptions, renderOptions),
            Ppm = () => SavePpm(text, path, encodeOptions, renderOptions),
            Pbm = () => SavePbm(text, path, encodeOptions, renderOptions),
            Pgm = () => SavePgm(text, path, encodeOptions, renderOptions),
            Pam = () => SavePam(text, path, encodeOptions, renderOptions),
            Xbm = () => SaveXbm(text, path, encodeOptions, renderOptions),
            Xpm = () => SaveXpm(text, path, encodeOptions, renderOptions),
            Tga = () => SaveTga(text, path, encodeOptions, renderOptions),
            Ico = () => SaveIco(text, path, encodeOptions, renderOptions),
            Pdf = () => SavePdf(text, path, encodeOptions, renderOptions),
            Eps = () => SaveEps(text, path, encodeOptions, renderOptions)
        });
    }

    /// <summary>
    /// Saves PDF417 to a file for byte payloads based on extension (.png/.webp/.svg/.svgz/.svg.gz/.html/.htm/.jpg/.jpeg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps/.ps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(byte[] data, string path, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, string? title = null) {
        return SaveByExtensionHelper.Save(path, new SaveByExtensionHandlers {
            Default = () => SavePng(data, path, encodeOptions, renderOptions),
            Png = () => SavePng(data, path, encodeOptions, renderOptions),
            Webp = () => SaveWebp(data, path, encodeOptions, renderOptions),
            Svg = () => SaveSvg(data, path, encodeOptions, renderOptions),
            Svgz = () => SaveSvgz(data, path, encodeOptions, renderOptions),
            Html = () => SaveHtml(data, path, encodeOptions, renderOptions, title),
            Jpeg = () => SaveJpeg(data, path, encodeOptions, renderOptions),
            Bmp = () => SaveBmp(data, path, encodeOptions, renderOptions),
            Ppm = () => SavePpm(data, path, encodeOptions, renderOptions),
            Pbm = () => SavePbm(data, path, encodeOptions, renderOptions),
            Pgm = () => SavePgm(data, path, encodeOptions, renderOptions),
            Pam = () => SavePam(data, path, encodeOptions, renderOptions),
            Xbm = () => SaveXbm(data, path, encodeOptions, renderOptions),
            Xpm = () => SaveXpm(data, path, encodeOptions, renderOptions),
            Tga = () => SaveTga(data, path, encodeOptions, renderOptions),
            Ico = () => SaveIco(data, path, encodeOptions, renderOptions),
            Pdf = () => SavePdf(data, path, encodeOptions, renderOptions),
            Eps = () => SaveEps(data, path, encodeOptions, renderOptions)
        });
    }

}

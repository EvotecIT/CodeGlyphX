using System;
using System.IO;
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
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

/// <summary>
/// Simple barcode helpers with fluent and static APIs.
/// </summary>
/// <remarks>
/// Use <see cref="Save(CodeGlyphX.BarcodeType,string,string,CodeGlyphX.BarcodeOptions)"/> to pick the output format by file extension.
/// </remarks>
/// <example>
/// <code>
/// using CodeGlyphX;
/// Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "barcode.png");
/// </code>
/// </example>
public static class Barcode {
    /// <summary>
    /// Starts a fluent barcode builder.
    /// </summary>
    public static BarcodeBuilder Create(BarcodeType type, string content, BarcodeOptions? options = null) {
        return new BarcodeBuilder(type, content, options);
    }

    /// <summary>
    /// Encodes a 1D barcode.
    /// </summary>
    public static Barcode1D Encode(BarcodeType type, string content) {
        return BarcodeEncoder.Encode(type, content);
    }

    /// <summary>
    /// Renders a barcode as PNG.
    /// </summary>
    public static byte[] Png(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePngRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as SVG.
    /// </summary>
    public static string Svg(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildSvgOptions(options);
        return SvgBarcodeRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as HTML.
    /// </summary>
    public static string Html(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildHtmlOptions(options);
        return HtmlBarcodeRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as JPEG.
    /// </summary>
    public static byte[] Jpeg(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 90;
        return BarcodeJpegRenderer.Render(barcode, opts, quality);
    }

    /// <summary>
    /// Renders a barcode as BMP.
    /// </summary>
    public static byte[] Bmp(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeBmpRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PPM.
    /// </summary>
    public static byte[] Ppm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePpmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PBM.
    /// </summary>
    public static byte[] Pbm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePbmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PGM.
    /// </summary>
    public static byte[] Pgm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePgmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PAM.
    /// </summary>
    public static byte[] Pam(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePamRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as XBM.
    /// </summary>
    public static string Xbm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeXbmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as XPM.
    /// </summary>
    public static string Xpm(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeXpmRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as TGA.
    /// </summary>
    public static byte[] Tga(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeTgaRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as ICO.
    /// </summary>
    public static byte[] Ico(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeIcoRenderer.Render(barcode, opts, BuildIcoOptions(options));
    }

    /// <summary>
    /// Renders a barcode as SVGZ.
    /// </summary>
    public static byte[] Svgz(BarcodeType type, string content, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildSvgOptions(options);
        return BarcodeSvgzRenderer.Render(barcode, opts);
    }

    /// <summary>
    /// Renders a barcode as PDF.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] Pdf(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodePdfRenderer.Render(barcode, opts, mode);
    }

    /// <summary>
    /// Renders a barcode as EPS.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string Eps(BarcodeType type, string content, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        return BarcodeEpsRenderer.Render(barcode, opts, mode);
    }

    /// <summary>
    /// Renders a barcode as ASCII text.
    /// </summary>
    public static string Ascii(BarcodeType type, string content, BarcodeAsciiRenderOptions? options = null) {
        var barcode = Encode(type, content);
        return BarcodeAsciiRenderer.Render(barcode, options);
    }

    /// <summary>
    /// Renders a barcode as PNG and writes it to a file.
    /// </summary>
    public static string SavePng(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var png = Png(type, content, options);
        return png.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PNG and writes it to a stream.
    /// </summary>
    public static void SavePng(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePngRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as SVG and writes it to a file.
    /// </summary>
    public static string SaveSvg(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var svg = Svg(type, content, options);
        return svg.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as SVG and writes it to a stream.
    /// </summary>
    public static void SaveSvg(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var svg = Svg(type, content, options);
        svg.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as HTML and writes it to a file.
    /// </summary>
    public static string SaveHtml(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        var html = Html(type, content, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        return html.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as HTML and writes it to a stream.
    /// </summary>
    public static void SaveHtml(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, string? title = null) {
        var html = Html(type, content, options);
        if (!string.IsNullOrEmpty(title)) {
            html = html.WrapHtml(title);
        }
        html.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as JPEG and writes it to a file.
    /// </summary>
    public static string SaveJpeg(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var jpeg = Jpeg(type, content, options);
        return jpeg.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as JPEG and writes it to a stream.
    /// </summary>
    public static void SaveJpeg(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        var quality = options?.JpegQuality ?? 90;
        BarcodeJpegRenderer.RenderToStream(barcode, opts, stream, quality);
    }

    /// <summary>
    /// Renders a barcode as BMP and writes it to a file.
    /// </summary>
    public static string SaveBmp(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var bmp = Bmp(type, content, options);
        return bmp.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as BMP and writes it to a stream.
    /// </summary>
    public static void SaveBmp(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodeBmpRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PPM and writes it to a file.
    /// </summary>
    public static string SavePpm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var ppm = Ppm(type, content, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PPM and writes it to a stream.
    /// </summary>
    public static void SavePpm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePpmRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PBM and writes it to a file.
    /// </summary>
    public static string SavePbm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var pbm = Pbm(type, content, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PBM and writes it to a stream.
    /// </summary>
    public static void SavePbm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePbmRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PGM and writes it to a file.
    /// </summary>
    public static string SavePgm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var pgm = Pgm(type, content, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PGM and writes it to a stream.
    /// </summary>
    public static void SavePgm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePgmRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PAM and writes it to a file.
    /// </summary>
    public static string SavePam(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var pam = Pam(type, content, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PAM and writes it to a stream.
    /// </summary>
    public static void SavePam(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePamRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as XBM and writes it to a file.
    /// </summary>
    public static string SaveXbm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var xbm = Xbm(type, content, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as XBM and writes it to a stream.
    /// </summary>
    public static void SaveXbm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var xbm = Xbm(type, content, options);
        xbm.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as XPM and writes it to a file.
    /// </summary>
    public static string SaveXpm(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var xpm = Xpm(type, content, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as XPM and writes it to a stream.
    /// </summary>
    public static void SaveXpm(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var xpm = Xpm(type, content, options);
        xpm.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as TGA and writes it to a file.
    /// </summary>
    public static string SaveTga(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var tga = Tga(type, content, options);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as TGA and writes it to a stream.
    /// </summary>
    public static void SaveTga(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodeTgaRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as ICO and writes it to a file.
    /// </summary>
    public static string SaveIco(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var ico = Ico(type, content, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as ICO and writes it to a stream.
    /// </summary>
    public static void SaveIco(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodeIcoRenderer.RenderToStream(barcode, opts, stream, BuildIcoOptions(options));
    }

    /// <summary>
    /// Renders a barcode as SVGZ and writes it to a file.
    /// </summary>
    public static string SaveSvgz(BarcodeType type, string content, string path, BarcodeOptions? options = null) {
        var svgz = Svgz(type, content, options);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as SVGZ and writes it to a stream.
    /// </summary>
    public static void SaveSvgz(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null) {
        var barcode = Encode(type, content);
        var opts = BuildSvgOptions(options);
        BarcodeSvgzRenderer.RenderToStream(barcode, opts, stream);
    }

    /// <summary>
    /// Renders a barcode as PDF and writes it to a file.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SavePdf(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var pdf = Pdf(type, content, options, mode);
        return pdf.WriteBinary(path);
    }

    /// <summary>
    /// Renders a barcode as PDF and writes it to a stream.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SavePdf(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var barcode = Encode(type, content);
        var opts = BuildPngOptions(options);
        BarcodePdfRenderer.RenderToStream(barcode, opts, stream, mode);
    }

    /// <summary>
    /// Renders a barcode as EPS and writes it to a file.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SaveEps(BarcodeType type, string content, string path, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var eps = Eps(type, content, options, mode);
        return eps.WriteText(path);
    }

    /// <summary>
    /// Renders a barcode as EPS and writes it to a stream.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="content">The barcode content.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SaveEps(BarcodeType type, string content, Stream stream, BarcodeOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var eps = Eps(type, content, options, mode);
        eps.WriteText(stream);
    }

    /// <summary>
    /// Renders a barcode as ASCII text and writes it to a file.
    /// </summary>
    public static string SaveAscii(BarcodeType type, string content, string path, BarcodeAsciiRenderOptions? options = null) {
        var ascii = Ascii(type, content, options);
        return ascii.WriteText(path);
    }

    /// <summary>
    /// Saves a barcode to a file based on the file extension (.png/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(BarcodeType type, string content, string path, BarcodeOptions? options = null, string? title = null) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return SaveSvgz(type, content, path, options);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return SavePng(type, content, path, options);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return SavePng(type, content, path, options);
            case ".svg":
                return SaveSvg(type, content, path, options);
            case ".html":
            case ".htm":
                return SaveHtml(type, content, path, options, title);
            case ".jpg":
            case ".jpeg":
                return SaveJpeg(type, content, path, options);
            case ".bmp":
                return SaveBmp(type, content, path, options);
            case ".ppm":
                return SavePpm(type, content, path, options);
            case ".pbm":
                return SavePbm(type, content, path, options);
            case ".pgm":
                return SavePgm(type, content, path, options);
            case ".pam":
                return SavePam(type, content, path, options);
            case ".xbm":
                return SaveXbm(type, content, path, options);
            case ".xpm":
                return SaveXpm(type, content, path, options);
            case ".tga":
                return SaveTga(type, content, path, options);
            case ".ico":
                return SaveIco(type, content, path, options);
            case ".svgz":
                return SaveSvgz(type, content, path, options);
            case ".pdf":
                return SavePdf(type, content, path, options);
            case ".eps":
            case ".ps":
                return SaveEps(type, content, path, options);
            default:
                // Fallback to PNG for unknown extensions to keep the API forgiving.
                return SavePng(type, content, path, options);
        }
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out BarcodeDecoded decoded) {
        return TryDecodePng(png, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from PNG bytes with an optional expected type hint.
    /// </summary>
    public static bool TryDecodePng(byte[] png, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out BarcodeDecoded decoded) {
        return TryDecodeImage(image, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint.
    /// </summary>
    public static bool TryDecodeImage(byte[] image, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out BarcodeDecoded decoded) {
        return TryDecodeImage(stream, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA) with an optional expected type hint.
    /// </summary>
    public static bool TryDecodeImage(Stream stream, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, expectedType, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out BarcodeDecoded decoded) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Attempts to decode a barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out BarcodeDecoded decoded) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out decoded);
    }

    /// <summary>
    /// Decodes a barcode from PNG bytes.
    /// </summary>
    public static BarcodeDecoded DecodePng(byte[] png) {
        if (!TryDecodePng(png, out var decoded)) {
            throw new FormatException("PNG does not contain a decodable barcode.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a barcode from a PNG file.
    /// </summary>
    public static BarcodeDecoded DecodePngFile(string path) {
        if (!TryDecodePngFile(path, out var decoded)) {
            throw new FormatException("PNG file does not contain a decodable barcode.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a barcode from a PNG stream.
    /// </summary>
    public static BarcodeDecoded DecodePng(Stream stream) {
        if (!TryDecodePng(stream, out var decoded)) {
            throw new FormatException("PNG stream does not contain a decodable barcode.");
        }
        return decoded;
    }

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

    private static IcoRenderOptions BuildIcoOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
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

    private static BarcodeHtmlRenderOptions BuildHtmlOptions(BarcodeOptions? options) {
        var opts = options ?? new BarcodeOptions();
        return new BarcodeHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            HeightModules = opts.HeightModules,
            BarColor = ColorUtils.ToCss(opts.Foreground),
            BackgroundColor = ColorUtils.ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable,
            LabelText = opts.LabelText,
            LabelFontSize = opts.LabelFontSize,
            LabelMargin = opts.LabelMargin,
            LabelColor = ColorUtils.ToCss(opts.LabelColor),
            LabelFontFamily = opts.LabelFontFamily,
        };
    }

    /// <summary>
    /// Fluent barcode builder.
    /// </summary>
    public sealed class BarcodeBuilder {
        private readonly BarcodeType _type;
        private readonly string _content;

        /// <summary>
        /// Rendering options used by this builder.
        /// </summary>
        public BarcodeOptions Options { get; }

        internal BarcodeBuilder(BarcodeType type, string content, BarcodeOptions? options) {
            _type = type;
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Options = options ?? new BarcodeOptions();
        }

        /// <summary>
        /// Updates rendering options.
        /// </summary>
        public BarcodeBuilder WithOptions(Action<BarcodeOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Options);
            return this;
        }

        /// <summary>
        /// Sets module size.
        /// </summary>
        public BarcodeBuilder WithModuleSize(int moduleSize) {
            Options.ModuleSize = moduleSize;
            return this;
        }

        /// <summary>
        /// Sets quiet zone size.
        /// </summary>
        public BarcodeBuilder WithQuietZone(int quietZone) {
            Options.QuietZone = quietZone;
            return this;
        }

        /// <summary>
        /// Sets barcode height in modules.
        /// </summary>
        public BarcodeBuilder WithHeight(int heightModules) {
            Options.HeightModules = heightModules;
            return this;
        }

        /// <summary>
        /// Sets foreground and background colors.
        /// </summary>
        public BarcodeBuilder WithColors(Rgba32 foreground, Rgba32 background) {
            Options.Foreground = foreground;
            Options.Background = background;
            return this;
        }

        /// <summary>
        /// Sets JPEG quality.
        /// </summary>
        public BarcodeBuilder WithJpegQuality(int quality) {
            Options.JpegQuality = quality;
            return this;
        }

        /// <summary>
        /// Sets label text rendered under bars.
        /// </summary>
        public BarcodeBuilder WithLabel(string? text) {
            Options.LabelText = text;
            return this;
        }

        /// <summary>
        /// Sets label font size in pixels.
        /// </summary>
        public BarcodeBuilder WithLabelFontSize(int fontSizePx) {
            Options.LabelFontSize = fontSizePx;
            return this;
        }

        /// <summary>
        /// Sets label margin in pixels.
        /// </summary>
        public BarcodeBuilder WithLabelMargin(int marginPx) {
            Options.LabelMargin = marginPx;
            return this;
        }

        /// <summary>
        /// Sets label color.
        /// </summary>
        public BarcodeBuilder WithLabelColor(Rgba32 color) {
            Options.LabelColor = color;
            return this;
        }

        /// <summary>
        /// Sets label font family (SVG/HTML).
        /// </summary>
        public BarcodeBuilder WithLabelFontFamily(string fontFamily) {
            Options.LabelFontFamily = fontFamily;
            return this;
        }

        /// <summary>
        /// Sets ICO output sizes (in pixels).
        /// </summary>
        public BarcodeBuilder WithIcoSizes(params int[] sizes) {
            Options.IcoSizes = sizes;
            return this;
        }

        /// <summary>
        /// Sets ICO aspect ratio preservation behavior.
        /// </summary>
        public BarcodeBuilder WithIcoPreserveAspectRatio(bool enabled = true) {
            Options.IcoPreserveAspectRatio = enabled;
            return this;
        }

        /// <summary>
        /// Encodes the barcode.
        /// </summary>
        public Barcode1D Encode() => Barcode.Encode(_type, _content);

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => Barcode.Png(_type, _content, Options);

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => Barcode.Svg(_type, _content, Options);

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => Barcode.Html(_type, _content, Options);

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => Barcode.Jpeg(_type, _content, Options);

        /// <summary>
        /// Renders BMP bytes.
        /// </summary>
        public byte[] Bmp() => Barcode.Bmp(_type, _content, Options);

        /// <summary>
        /// Renders PPM bytes.
        /// </summary>
        public byte[] Ppm() => Barcode.Ppm(_type, _content, Options);

        /// <summary>
        /// Renders PBM bytes.
        /// </summary>
        public byte[] Pbm() => Barcode.Pbm(_type, _content, Options);

        /// <summary>
        /// Renders PGM bytes.
        /// </summary>
        public byte[] Pgm() => Barcode.Pgm(_type, _content, Options);

        /// <summary>
        /// Renders PAM bytes.
        /// </summary>
        public byte[] Pam() => Barcode.Pam(_type, _content, Options);

        /// <summary>
        /// Renders XBM text.
        /// </summary>
        public string Xbm() => Barcode.Xbm(_type, _content, Options);

        /// <summary>
        /// Renders XPM text.
        /// </summary>
        public string Xpm() => Barcode.Xpm(_type, _content, Options);

        /// <summary>
        /// Renders TGA bytes.
        /// </summary>
        public byte[] Tga() => Barcode.Tga(_type, _content, Options);

        /// <summary>
        /// Renders ICO bytes.
        /// </summary>
        public byte[] Ico() => Barcode.Ico(_type, _content, Options);

        /// <summary>
        /// Renders SVGZ bytes.
        /// </summary>
        public byte[] Svgz() => Barcode.Svgz(_type, _content, Options);

        /// <summary>
        /// Renders PDF bytes.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public byte[] Pdf(RenderMode mode = RenderMode.Vector) => Barcode.Pdf(_type, _content, Options, mode);

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public string Eps(RenderMode mode = RenderMode.Vector) => Barcode.Eps(_type, _content, Options, mode);

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(BarcodeAsciiRenderOptions? options = null) => Barcode.Ascii(_type, _content, options);

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => Barcode.SavePng(_type, _content, path, Options);

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => Barcode.SavePng(_type, _content, stream, Options);

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => Barcode.SaveSvg(_type, _content, path, Options);

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => Barcode.SaveSvg(_type, _content, stream, Options);

        /// <summary>
        /// Saves SVGZ to a file.
        /// </summary>
        public string SaveSvgz(string path) => Barcode.SaveSvgz(_type, _content, path, Options);

        /// <summary>
        /// Saves SVGZ to a stream.
        /// </summary>
        public void SaveSvgz(Stream stream) => Barcode.SaveSvgz(_type, _content, stream, Options);

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) => Barcode.SaveHtml(_type, _content, path, Options, title);

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) => Barcode.SaveHtml(_type, _content, stream, Options, title);

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => Barcode.SaveJpeg(_type, _content, path, Options);

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => Barcode.SaveJpeg(_type, _content, stream, Options);

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) => Barcode.SaveBmp(_type, _content, path, Options);

        /// <summary>
        /// Saves BMP to a stream.
        /// </summary>
        public void SaveBmp(Stream stream) => Barcode.SaveBmp(_type, _content, stream, Options);

        /// <summary>
        /// Saves PGM to a file.
        /// </summary>
        public string SavePgm(string path) => Barcode.SavePgm(_type, _content, path, Options);

        /// <summary>
        /// Saves PGM to a stream.
        /// </summary>
        public void SavePgm(Stream stream) => Barcode.SavePgm(_type, _content, stream, Options);

        /// <summary>
        /// Saves PAM to a file.
        /// </summary>
        public string SavePam(string path) => Barcode.SavePam(_type, _content, path, Options);

        /// <summary>
        /// Saves PAM to a stream.
        /// </summary>
        public void SavePam(Stream stream) => Barcode.SavePam(_type, _content, stream, Options);

        /// <summary>
        /// Saves XBM to a file.
        /// </summary>
        public string SaveXbm(string path) => Barcode.SaveXbm(_type, _content, path, Options);

        /// <summary>
        /// Saves XBM to a stream.
        /// </summary>
        public void SaveXbm(Stream stream) => Barcode.SaveXbm(_type, _content, stream, Options);

        /// <summary>
        /// Saves XPM to a file.
        /// </summary>
        public string SaveXpm(string path) => Barcode.SaveXpm(_type, _content, path, Options);

        /// <summary>
        /// Saves XPM to a stream.
        /// </summary>
        public void SaveXpm(Stream stream) => Barcode.SaveXpm(_type, _content, stream, Options);

        /// <summary>
        /// Saves PPM to a file.
        /// </summary>
        public string SavePpm(string path) => Barcode.SavePpm(_type, _content, path, Options);

        /// <summary>
        /// Saves PPM to a stream.
        /// </summary>
        public void SavePpm(Stream stream) => Barcode.SavePpm(_type, _content, stream, Options);

        /// <summary>
        /// Saves PBM to a file.
        /// </summary>
        public string SavePbm(string path) => Barcode.SavePbm(_type, _content, path, Options);

        /// <summary>
        /// Saves PBM to a stream.
        /// </summary>
        public void SavePbm(Stream stream) => Barcode.SavePbm(_type, _content, stream, Options);

        /// <summary>
        /// Saves TGA to a file.
        /// </summary>
        public string SaveTga(string path) => Barcode.SaveTga(_type, _content, path, Options);

        /// <summary>
        /// Saves TGA to a stream.
        /// </summary>
        public void SaveTga(Stream stream) => Barcode.SaveTga(_type, _content, stream, Options);

        /// <summary>
        /// Saves ICO to a file.
        /// </summary>
        public string SaveIco(string path) => Barcode.SaveIco(_type, _content, path, Options);

        /// <summary>
        /// Saves ICO to a stream.
        /// </summary>
        public void SaveIco(Stream stream) => Barcode.SaveIco(_type, _content, stream, Options);

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SavePdf(string path, RenderMode mode = RenderMode.Vector) => Barcode.SavePdf(_type, _content, path, Options, mode);

        /// <summary>
        /// Saves PDF to a stream.
        /// </summary>
        /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SavePdf(Stream stream, RenderMode mode = RenderMode.Vector) => Barcode.SavePdf(_type, _content, stream, Options, mode);

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SaveEps(string path, RenderMode mode = RenderMode.Vector) => Barcode.SaveEps(_type, _content, path, Options, mode);

        /// <summary>
        /// Saves EPS to a stream.
        /// </summary>
        /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SaveEps(Stream stream, RenderMode mode = RenderMode.Vector) => Barcode.SaveEps(_type, _content, stream, Options, mode);

        /// <summary>
        /// Saves ASCII to a file.
        /// </summary>
        public string SaveAscii(string path, BarcodeAsciiRenderOptions? options = null) => Barcode.SaveAscii(_type, _content, path, options);

        /// <summary>
        /// Saves based on file extension (.png/.svg/.html/.jpg/.bmp/.ppm/.pbm/.tga/.ico/.pdf/.eps). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) => Barcode.Save(_type, _content, path, Options, title);
    }
}

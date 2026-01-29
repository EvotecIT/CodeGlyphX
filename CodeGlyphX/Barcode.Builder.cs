using System;
using System.IO;
using System.Threading;
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

public static partial class Barcode {
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
        /// Sets foreground color.
        /// </summary>
        public BarcodeBuilder WithForeground(Rgba32 color) {
            Options.Foreground = color;
            return this;
        }

        /// <summary>
        /// Sets background color.
        /// </summary>
        public BarcodeBuilder WithBackground(Rgba32 color) {
            Options.Background = color;
            return this;
        }

        /// <summary>
        /// Uses a transparent background (alpha = 0).
        /// </summary>
        public BarcodeBuilder WithTransparentBackground() {
            Options.Background = Rgba32.Transparent;
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

        private RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return Barcode.Render(_type, _content, format, Options, extras);
        }

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => Render(OutputFormat.Png).Data;

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => Render(OutputFormat.Svg).GetText();

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => Render(OutputFormat.Html).GetText();

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => Render(OutputFormat.Jpeg).Data;

        /// <summary>
        /// Renders BMP bytes.
        /// </summary>
        public byte[] Bmp() => Render(OutputFormat.Bmp).Data;

        /// <summary>
        /// Renders PPM bytes.
        /// </summary>
        public byte[] Ppm() => Render(OutputFormat.Ppm).Data;

        /// <summary>
        /// Renders PBM bytes.
        /// </summary>
        public byte[] Pbm() => Render(OutputFormat.Pbm).Data;

        /// <summary>
        /// Renders PGM bytes.
        /// </summary>
        public byte[] Pgm() => Render(OutputFormat.Pgm).Data;

        /// <summary>
        /// Renders PAM bytes.
        /// </summary>
        public byte[] Pam() => Render(OutputFormat.Pam).Data;

        /// <summary>
        /// Renders XBM text.
        /// </summary>
        public string Xbm() => Render(OutputFormat.Xbm).GetText();

        /// <summary>
        /// Renders XPM text.
        /// </summary>
        public string Xpm() => Render(OutputFormat.Xpm).GetText();

        /// <summary>
        /// Renders TGA bytes.
        /// </summary>
        public byte[] Tga() => Render(OutputFormat.Tga).Data;

        /// <summary>
        /// Renders ICO bytes.
        /// </summary>
        public byte[] Ico() => Render(OutputFormat.Ico).Data;

        /// <summary>
        /// Renders SVGZ bytes.
        /// </summary>
        public byte[] Svgz() => Render(OutputFormat.Svgz).Data;

        /// <summary>
        /// Renders PDF bytes.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public byte[] Pdf(RenderMode mode = RenderMode.Vector) => Render(OutputFormat.Pdf, new RenderExtras { VectorMode = mode }).Data;

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        /// <param name="mode">Vector or raster output.</param>
        public string Eps(RenderMode mode = RenderMode.Vector) => Render(OutputFormat.Eps, new RenderExtras { VectorMode = mode }).GetText();

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(BarcodeAsciiRenderOptions? options = null) => Render(OutputFormat.Ascii, new RenderExtras { BarcodeAscii = options }).GetText();

        private RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return Barcode.Render(_type, _content, format, Options, extras);
        }

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => OutputWriter.Write(path, Render(OutputFormat.Png));

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Png));

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => OutputWriter.Write(path, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves SVGZ to a file.
        /// </summary>
        public string SaveSvgz(string path) => OutputWriter.Write(path, Render(OutputFormat.Svgz));

        /// <summary>
        /// Saves SVGZ to a stream.
        /// </summary>
        public void SaveSvgz(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Svgz));

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            return OutputWriter.Write(path, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            OutputWriter.Write(stream, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => OutputWriter.Write(path, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves WebP to a file.
        /// </summary>
        public string SaveWebp(string path) => OutputWriter.Write(path, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves WebP to a stream.
        /// </summary>
        public void SaveWebp(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) => OutputWriter.Write(path, Render(OutputFormat.Bmp));

        /// <summary>
        /// Saves BMP to a stream.
        /// </summary>
        public void SaveBmp(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Bmp));

        /// <summary>
        /// Saves PGM to a file.
        /// </summary>
        public string SavePgm(string path) => OutputWriter.Write(path, Render(OutputFormat.Pgm));

        /// <summary>
        /// Saves PGM to a stream.
        /// </summary>
        public void SavePgm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Pgm));

        /// <summary>
        /// Saves PAM to a file.
        /// </summary>
        public string SavePam(string path) => OutputWriter.Write(path, Render(OutputFormat.Pam));

        /// <summary>
        /// Saves PAM to a stream.
        /// </summary>
        public void SavePam(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Pam));

        /// <summary>
        /// Saves XBM to a file.
        /// </summary>
        public string SaveXbm(string path) => OutputWriter.Write(path, Render(OutputFormat.Xbm));

        /// <summary>
        /// Saves XBM to a stream.
        /// </summary>
        public void SaveXbm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Xbm));

        /// <summary>
        /// Saves XPM to a file.
        /// </summary>
        public string SaveXpm(string path) => OutputWriter.Write(path, Render(OutputFormat.Xpm));

        /// <summary>
        /// Saves XPM to a stream.
        /// </summary>
        public void SaveXpm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Xpm));

        /// <summary>
        /// Saves PPM to a file.
        /// </summary>
        public string SavePpm(string path) => OutputWriter.Write(path, Render(OutputFormat.Ppm));

        /// <summary>
        /// Saves PPM to a stream.
        /// </summary>
        public void SavePpm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Ppm));

        /// <summary>
        /// Saves PBM to a file.
        /// </summary>
        public string SavePbm(string path) => OutputWriter.Write(path, Render(OutputFormat.Pbm));

        /// <summary>
        /// Saves PBM to a stream.
        /// </summary>
        public void SavePbm(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Pbm));

        /// <summary>
        /// Saves TGA to a file.
        /// </summary>
        public string SaveTga(string path) => OutputWriter.Write(path, Render(OutputFormat.Tga));

        /// <summary>
        /// Saves TGA to a stream.
        /// </summary>
        public void SaveTga(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Tga));

        /// <summary>
        /// Saves ICO to a file.
        /// </summary>
        public string SaveIco(string path) => OutputWriter.Write(path, Render(OutputFormat.Ico));

        /// <summary>
        /// Saves ICO to a stream.
        /// </summary>
        public void SaveIco(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Ico));

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SavePdf(string path, RenderMode mode = RenderMode.Vector) {
            return OutputWriter.Write(path, Render(OutputFormat.Pdf, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Saves PDF to a stream.
        /// </summary>
        /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SavePdf(Stream stream, RenderMode mode = RenderMode.Vector) {
            OutputWriter.Write(stream, Render(OutputFormat.Pdf, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="mode">Vector or raster output.</param>
        public string SaveEps(string path, RenderMode mode = RenderMode.Vector) {
            return OutputWriter.Write(path, Render(OutputFormat.Eps, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Saves EPS to a stream.
        /// </summary>
        /// <param name="stream">Destination stream.</param>
        /// <param name="mode">Vector or raster output.</param>
        public void SaveEps(Stream stream, RenderMode mode = RenderMode.Vector) {
            OutputWriter.Write(stream, Render(OutputFormat.Eps, new RenderExtras { VectorMode = mode }));
        }

        /// <summary>
        /// Saves ASCII to a file.
        /// </summary>
        public string SaveAscii(string path, BarcodeAsciiRenderOptions? options = null) {
            return OutputWriter.Write(path, Render(OutputFormat.Ascii, new RenderExtras { BarcodeAscii = options }));
        }

        /// <summary>
        /// Saves based on file extension (.png/.svg/.html/.jpg/.bmp/.ppm/.pbm/.tga/.ico/.pdf/.eps). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            return Barcode.Save(_type, _content, path, Options, extras);
        }
    }

}

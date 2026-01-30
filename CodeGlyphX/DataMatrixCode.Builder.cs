using System;
using System.IO;
using System.Threading;
using CodeGlyphX.DataMatrix;
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

public static partial class DataMatrixCode {
    /// <summary>
    /// Fluent Data Matrix builder.
    /// </summary>
    public sealed class DataMatrixBuilder {
        private readonly string? _text;
        private readonly byte[]? _bytes;
        private DataMatrixEncodingMode _mode;
        private readonly MatrixOptions _options;

        internal DataMatrixBuilder(string text, DataMatrixEncodingMode mode, MatrixOptions? options) {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _mode = mode;
            _options = options ?? new MatrixOptions();
        }

        internal DataMatrixBuilder(byte[] data, DataMatrixEncodingMode mode, MatrixOptions? options) {
            _bytes = data ?? throw new ArgumentNullException(nameof(data));
            _mode = mode;
            _options = options ?? new MatrixOptions();
        }

        /// <summary>
        /// Mutates rendering options.
        /// </summary>
        public DataMatrixBuilder WithOptions(Action<MatrixOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options);
            return this;
        }

        /// <summary>
        /// Sets the encoding mode.
        /// </summary>
        public DataMatrixBuilder WithMode(DataMatrixEncodingMode mode) {
            _mode = mode;
            return this;
        }

        /// <summary>
        /// Sets module size in pixels.
        /// </summary>
        public DataMatrixBuilder WithModuleSize(int moduleSize) {
            _options.ModuleSize = moduleSize;
            return this;
        }

        /// <summary>
        /// Sets quiet zone size in modules.
        /// </summary>
        public DataMatrixBuilder WithQuietZone(int quietZone) {
            _options.QuietZone = quietZone;
            return this;
        }

        /// <summary>
        /// Sets foreground/background colors.
        /// </summary>
        public DataMatrixBuilder WithColors(Rgba32 foreground, Rgba32 background) {
            _options.Foreground = foreground;
            _options.Background = background;
            return this;
        }

        /// <summary>
        /// Sets JPEG quality (1..100).
        /// </summary>
        public DataMatrixBuilder WithJpegQuality(int quality) {
            _options.JpegQuality = quality;
            return this;
        }

        /// <summary>
        /// Sets JPEG encoding options.
        /// </summary>
        public DataMatrixBuilder WithJpegOptions(JpegEncodeOptions options) {
            _options.JpegOptions = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        /// <summary>
        /// Enables HTML email-safe table rendering.
        /// </summary>
        public DataMatrixBuilder WithHtmlEmailSafeTable(bool enabled = true) {
            _options.HtmlEmailSafeTable = enabled;
            return this;
        }

        /// <summary>
        /// Sets ICO output sizes (in pixels).
        /// </summary>
        public DataMatrixBuilder WithIcoSizes(params int[] sizes) {
            _options.IcoSizes = sizes;
            return this;
        }

        /// <summary>
        /// Sets ICO aspect ratio preservation behavior.
        /// </summary>
        public DataMatrixBuilder WithIcoPreserveAspectRatio(bool enabled = true) {
            _options.IcoPreserveAspectRatio = enabled;
            return this;
        }

        /// <summary>
        /// Encodes the Data Matrix as a module matrix.
        /// </summary>
        public BitMatrix Encode() {
            return _text is not null ? DataMatrixCode.Encode(_text, _mode) : DataMatrixCode.EncodeBytes(_bytes!, _mode);
        }

        private RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return _text is not null
                ? DataMatrixCode.Render(_text, format, _mode, _options, extras)
                : DataMatrixCode.Render(_bytes!, format, _mode, _options, extras);
        }

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() {
            return Render(OutputFormat.Png).Data;
        }

        /// <summary>
        /// Renders SVG markup.
        /// </summary>
        public string Svg() {
            return Render(OutputFormat.Svg).GetText();
        }

        /// <summary>
        /// Renders HTML markup.
        /// </summary>
        public string Html() {
            return Render(OutputFormat.Html).GetText();
        }

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() {
            return Render(OutputFormat.Jpeg).Data;
        }

        /// <summary>
        /// Renders BMP bytes.
        /// </summary>
        public byte[] Bmp() {
            return Render(OutputFormat.Bmp).Data;
        }

        /// <summary>
        /// Renders PDF bytes.
        /// </summary>
        /// <param name="renderMode">Vector or raster output.</param>
        public byte[] Pdf(RenderMode renderMode = RenderMode.Vector) {
            return Render(OutputFormat.Pdf, new RenderExtras { VectorMode = renderMode }).Data;
        }

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        /// <param name="renderMode">Vector or raster output.</param>
        public string Eps(RenderMode renderMode = RenderMode.Vector) {
            return Render(OutputFormat.Eps, new RenderExtras { VectorMode = renderMode }).GetText();
        }

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(MatrixAsciiRenderOptions? options = null) {
            return Render(OutputFormat.Ascii, new RenderExtras { MatrixAscii = options }).GetText();
        }

        /// <summary>
        /// Saves output based on file extension.
        /// </summary>
        public string Save(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            if (_text is not null) return DataMatrixCode.Save(_text, path, _mode, _options, extras);
            return DataMatrixCode.Save(_bytes!, path, _mode, _options, extras);
        }

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) {
            return OutputWriter.Write(path, Render(OutputFormat.Png));
        }

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) {
            return OutputWriter.Write(path, Render(OutputFormat.Svg));
        }

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            return OutputWriter.Write(path, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) {
            return OutputWriter.Write(path, Render(OutputFormat.Jpeg));
        }

        /// <summary>
        /// Saves WebP to a file.
        /// </summary>
        public string SaveWebp(string path) {
            return OutputWriter.Write(path, Render(OutputFormat.Webp));
        }

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) {
            return OutputWriter.Write(path, Render(OutputFormat.Bmp));
        }

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="renderMode">Vector or raster output.</param>
        public string SavePdf(string path, RenderMode renderMode = RenderMode.Vector) {
            return OutputWriter.Write(path, Render(OutputFormat.Pdf, new RenderExtras { VectorMode = renderMode }));
        }

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="renderMode">Vector or raster output.</param>
        public string SaveEps(string path, RenderMode renderMode = RenderMode.Vector) {
            return OutputWriter.Write(path, Render(OutputFormat.Eps, new RenderExtras { VectorMode = renderMode }));
        }
    }

}

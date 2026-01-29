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

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() {
            return _text is not null ? DataMatrixCode.Png(_text, _mode, _options) : DataMatrixCode.Png(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders SVG markup.
        /// </summary>
        public string Svg() {
            return _text is not null ? DataMatrixCode.Svg(_text, _mode, _options) : DataMatrixCode.Svg(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders HTML markup.
        /// </summary>
        public string Html() {
            return _text is not null ? DataMatrixCode.Html(_text, _mode, _options) : DataMatrixCode.Html(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() {
            return _text is not null ? DataMatrixCode.Jpeg(_text, _mode, _options) : DataMatrixCode.Jpeg(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders BMP bytes.
        /// </summary>
        public byte[] Bmp() {
            return _text is not null ? DataMatrixCode.Bmp(_text, _mode, _options) : DataMatrixCode.Bmp(_bytes!, _mode, _options);
        }

        /// <summary>
        /// Renders PDF bytes.
        /// </summary>
        /// <param name="renderMode">Vector or raster output.</param>
        public byte[] Pdf(RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.Pdf(_text, _mode, _options, renderMode) : DataMatrixCode.Pdf(_bytes!, _mode, _options, renderMode);
        }

        /// <summary>
        /// Renders EPS text.
        /// </summary>
        /// <param name="renderMode">Vector or raster output.</param>
        public string Eps(RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.Eps(_text, _mode, _options, renderMode) : DataMatrixCode.Eps(_bytes!, _mode, _options, renderMode);
        }

        /// <summary>
        /// Renders ASCII text.
        /// </summary>
        public string Ascii(MatrixAsciiRenderOptions? options = null) {
            return _text is not null ? DataMatrixCode.Ascii(_text, _mode, options) : DataMatrixCode.Ascii(_bytes!, _mode, options);
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
            return _text is not null ? DataMatrixCode.SavePng(_text, path, _mode, _options) : DataMatrixCode.SavePng(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) {
            return _text is not null ? DataMatrixCode.SaveSvg(_text, path, _mode, _options) : DataMatrixCode.SaveSvg(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            return _text is not null ? DataMatrixCode.SaveHtml(_text, path, _mode, _options, title) : DataMatrixCode.SaveHtml(_bytes!, path, _mode, _options, title);
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) {
            return _text is not null ? DataMatrixCode.SaveJpeg(_text, path, _mode, _options) : DataMatrixCode.SaveJpeg(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves WebP to a file.
        /// </summary>
        public string SaveWebp(string path) {
            return _text is not null ? DataMatrixCode.SaveWebp(_text, path, _mode, _options) : DataMatrixCode.SaveWebp(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves BMP to a file.
        /// </summary>
        public string SaveBmp(string path) {
            return _text is not null ? DataMatrixCode.SaveBmp(_text, path, _mode, _options) : DataMatrixCode.SaveBmp(_bytes!, path, _mode, _options);
        }

        /// <summary>
        /// Saves PDF to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="renderMode">Vector or raster output.</param>
        public string SavePdf(string path, RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.SavePdf(_text, path, _mode, _options, renderMode) : DataMatrixCode.SavePdf(_bytes!, path, _mode, _options, renderMode);
        }

        /// <summary>
        /// Saves EPS to a file.
        /// </summary>
    /// <param name="path">Output file path.</param>
        /// <param name="renderMode">Vector or raster output.</param>
        public string SaveEps(string path, RenderMode renderMode = RenderMode.Vector) {
            return _text is not null ? DataMatrixCode.SaveEps(_text, path, _mode, _options, renderMode) : DataMatrixCode.SaveEps(_bytes!, path, _mode, _options, renderMode);
        }
    }

}

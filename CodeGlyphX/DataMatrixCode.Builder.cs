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

        /// <summary>
        /// Renders the configured Data Matrix to the requested output format.
        /// </summary>
        public RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return _text is not null
                ? DataMatrixCode.Render(_text, format, _mode, _options, extras)
                : DataMatrixCode.Render(_bytes!, format, _mode, _options, extras);
        }

        /// <summary>
        /// Saves the configured Data Matrix, selecting the output format from the file extension.
        /// </summary>
        public string Save(string path, RenderExtras? extras = null) {
            var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
            return OutputWriter.Write(path, Render(format, extras));
        }

        /// <summary>
        /// Writes the configured Data Matrix to a stream in the requested output format.
        /// </summary>
        public void Save(Stream stream, OutputFormat format, RenderExtras? extras = null) {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            OutputWriter.Write(stream, Render(format, extras));
        }
    }

}

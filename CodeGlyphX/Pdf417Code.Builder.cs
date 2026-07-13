using System;
using System.IO;
using System.Threading;
using System.Text;
using CodeGlyphX.Pdf417;
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

public static partial class Pdf417Code {
    /// <summary>
    /// Fluent PDF417 builder.
    /// </summary>
    public sealed class Pdf417Builder {
        private readonly string? _text;
        private readonly byte[]? _bytes;
        private readonly Pdf417EncodeOptions _encodeOptions;
        private readonly MatrixOptions _renderOptions;

        internal Pdf417Builder(string text, Pdf417EncodeOptions? encodeOptions, MatrixOptions? renderOptions) {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _encodeOptions = encodeOptions ?? new Pdf417EncodeOptions();
            _renderOptions = renderOptions ?? new MatrixOptions();
        }

        internal Pdf417Builder(byte[] data, Pdf417EncodeOptions? encodeOptions, MatrixOptions? renderOptions) {
            _bytes = data ?? throw new ArgumentNullException(nameof(data));
            _encodeOptions = encodeOptions ?? new Pdf417EncodeOptions();
            _renderOptions = renderOptions ?? new MatrixOptions();
        }

        /// <summary>
        /// Mutates PDF417 encoding options.
        /// </summary>
        public Pdf417Builder WithEncodeOptions(Action<Pdf417EncodeOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_encodeOptions);
            return this;
        }

        /// <summary>
        /// Mutates rendering options.
        /// </summary>
        public Pdf417Builder WithRenderOptions(Action<MatrixOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_renderOptions);
            return this;
        }

        /// <summary>
        /// Sets module size in pixels.
        /// </summary>
        public Pdf417Builder WithModuleSize(int moduleSize) {
            _renderOptions.ModuleSize = moduleSize;
            return this;
        }

        /// <summary>
        /// Sets quiet zone size in modules.
        /// </summary>
        public Pdf417Builder WithQuietZone(int quietZone) {
            _renderOptions.QuietZone = quietZone;
            return this;
        }

        /// <summary>
        /// Sets foreground/background colors.
        /// </summary>
        public Pdf417Builder WithColors(Rgba32 foreground, Rgba32 background) {
            _renderOptions.Foreground = foreground;
            _renderOptions.Background = background;
            return this;
        }

        /// <summary>
        /// Sets JPEG quality (1..100).
        /// </summary>
        public Pdf417Builder WithJpegQuality(int quality) {
            _renderOptions.JpegQuality = quality;
            return this;
        }

        /// <summary>
        /// Sets JPEG encoding options.
        /// </summary>
        public Pdf417Builder WithJpegOptions(JpegEncodeOptions options) {
            _renderOptions.JpegOptions = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        /// <summary>
        /// Enables HTML email-safe table rendering.
        /// </summary>
        public Pdf417Builder WithHtmlEmailSafeTable(bool enabled = true) {
            _renderOptions.HtmlEmailSafeTable = enabled;
            return this;
        }

        /// <summary>
        /// Sets ICO output sizes (in pixels).
        /// </summary>
        public Pdf417Builder WithIcoSizes(params int[] sizes) {
            _renderOptions.IcoSizes = sizes;
            return this;
        }

        /// <summary>
        /// Sets ICO aspect ratio preservation behavior.
        /// </summary>
        public Pdf417Builder WithIcoPreserveAspectRatio(bool enabled = true) {
            _renderOptions.IcoPreserveAspectRatio = enabled;
            return this;
        }

        /// <summary>
        /// Sets compaction mode.
        /// </summary>
        public Pdf417Builder WithCompaction(Pdf417Compaction compaction) {
            _encodeOptions.Compaction = compaction;
            return this;
        }

        /// <summary>
        /// Sets error correction level (0..8 or -1 for auto).
        /// </summary>
        public Pdf417Builder WithErrorCorrection(int level) {
            _encodeOptions.ErrorCorrectionLevel = level;
            return this;
        }

        /// <summary>
        /// Sets text encoding for byte compaction.
        /// </summary>
        public Pdf417Builder WithTextEncoding(Encoding encoding) {
            _encodeOptions.TextEncoding = encoding;
            return this;
        }

        /// <summary>
        /// Sets target aspect ratio (width/height).
        /// </summary>
        public Pdf417Builder WithAspectRatio(float ratio) {
            _encodeOptions.TargetAspectRatio = ratio;
            return this;
        }

        /// <summary>
        /// Sets column constraints.
        /// </summary>
        public Pdf417Builder WithColumns(int minColumns, int maxColumns) {
            _encodeOptions.MinColumns = minColumns;
            _encodeOptions.MaxColumns = maxColumns;
            return this;
        }

        /// <summary>
        /// Sets row constraints.
        /// </summary>
        public Pdf417Builder WithRows(int minRows, int maxRows) {
            _encodeOptions.MinRows = minRows;
            _encodeOptions.MaxRows = maxRows;
            return this;
        }

        /// <summary>
        /// Enables compact PDF417.
        /// </summary>
        public Pdf417Builder WithCompact(bool compact = true) {
            _encodeOptions.Compact = compact;
            return this;
        }

        /// <summary>
        /// Encodes the PDF417 as a module matrix.
        /// </summary>
        public BitMatrix Encode() {
            return _text is not null ? Pdf417Code.Encode(_text, _encodeOptions) : Pdf417Code.EncodeBytes(_bytes!, _encodeOptions);
        }

        /// <summary>
        /// Renders the configured PDF417 code to the requested output format.
        /// </summary>
        public RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return _text is not null
                ? Pdf417Code.Render(_text, format, _encodeOptions, _renderOptions, extras)
                : Pdf417Code.Render(_bytes!, format, _encodeOptions, _renderOptions, extras);
        }

        /// <summary>
        /// Saves the configured PDF417 code, selecting the output format from the file extension.
        /// </summary>
        public string Save(string path, RenderExtras? extras = null) {
            var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
            return OutputWriter.Write(path, Render(format, extras));
        }

        /// <summary>
        /// Writes the configured PDF417 code to a stream in the requested output format.
        /// </summary>
        public void Save(Stream stream, OutputFormat format, RenderExtras? extras = null) {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            OutputWriter.Write(stream, Render(format, extras));
        }
    }

}

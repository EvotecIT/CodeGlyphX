using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// A generated QR code (modules + metadata).
/// </summary>
public sealed class QrCode {
    /// <summary>
    /// Gets the QR version (1..40).
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the error correction level used for encoding.
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>
    /// Gets the selected mask pattern (0..7).
    /// </summary>
    public int Mask { get; }

    /// <summary>
    /// Gets the QR modules (dark = <c>true</c>, light = <c>false</c>), without quiet zone.
    /// </summary>
    public BitMatrix Modules { get; }

    /// <summary>
    /// Gets the module matrix size (width/height), i.e. <c>Version * 4 + 17</c>.
    /// </summary>
    public int Size => Modules.Width;

    /// <summary>
    /// Creates a new <see cref="QrCode"/>.
    /// </summary>
    /// <param name="version">QR version from 1 through 40.</param>
    /// <param name="errorCorrectionLevel">The error correction level represented by the modules.</param>
    /// <param name="mask">The selected mask pattern from 0 through 7.</param>
    /// <param name="modules">A square matrix whose width and height equal <c>version * 4 + 17</c>.</param>
    public QrCode(int version, QrErrorCorrectionLevel errorCorrectionLevel, int mask, BitMatrix modules) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (errorCorrectionLevel is < QrErrorCorrectionLevel.L or > QrErrorCorrectionLevel.H) throw new ArgumentOutOfRangeException(nameof(errorCorrectionLevel));
        if (mask is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(mask));
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        var size = version * 4 + 17;
        if (modules.Width != size || modules.Height != size) {
            throw new ArgumentException("Matrix width and height must equal version * 4 + 17.", nameof(modules));
        }

        Modules = modules.Clone().Freeze();
        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
    }

    /// <summary>Renders this encoded symbol without changing its version, mask or error correction.</summary>
    public RenderedOutput Render(OutputFormat format, QrRenderOptions? renderOptions = null, OutputOptions? outputOptions = null) =>
        QrRenderer.Render(this, format, renderOptions, outputOptions);

    /// <summary>Renders this encoded symbol to an RGBA buffer without an image container.</summary>
    public byte[] RenderPixels(out int widthPx, out int heightPx, out int stride, QrRenderOptions? renderOptions = null) =>
        QrRenderer.RenderPixels(this, out widthPx, out heightPx, out stride, renderOptions);

    /// <summary>Evaluates static appearance heuristics; the report does not decode the rendered artifact.</summary>
    public QrArtHeuristicReport EvaluateScanHeuristics(QrRenderOptions? renderOptions = null) =>
        QrRenderer.EvaluateScanHeuristics(this, renderOptions);

    /// <summary>Saves this encoded symbol, selecting the format from its extension (PNG by default).</summary>
    public string Save(string path, QrRenderOptions? renderOptions = null, OutputOptions? outputOptions = null) =>
        OutputWriter.Write(path, Render(OutputFormatInfo.Resolve(path, OutputFormat.Png), renderOptions, outputOptions));

    /// <summary>Writes this encoded symbol to a caller-owned stream, leaving it open.</summary>
    public void Save(System.IO.Stream stream, OutputFormat format, QrRenderOptions? renderOptions = null, OutputOptions? outputOptions = null) =>
        OutputWriter.Write(stream, Render(format, renderOptions, outputOptions));
}

using System;
using CodeGlyphX.Payloads;
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
    public QrCode(int version, QrErrorCorrectionLevel errorCorrectionLevel, int mask, BitMatrix modules) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (mask is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(mask));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));

        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
    }

    /// <summary>
    /// Encodes a payload into a <see cref="QrCode"/>.
    /// </summary>
    public static QrCode Encode(string payload, QrEasyOptions? options = null) {
        return QrEasy.Encode(payload, options);
    }

    /// <summary>
    /// Detects a payload type and encodes it into a <see cref="QrCode"/>.
    /// </summary>
    public static QrCode EncodeAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.EncodeAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Encodes a payload with embedded defaults into a <see cref="QrCode"/>.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEasyOptions? options = null) {
        return QrEasy.Encode(payload, options);
    }

    /// <summary>
    /// Renders this QR code to the requested output format.
    /// </summary>
    public RenderedOutput Render(OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrEasy.Render(this, format, options, extras);
    }

    /// <summary>
    /// Renders a payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(string payload, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var qr = Encode(payload, options);
        return QrEasy.Render(qr, format, options, extras);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code.
    /// </summary>
    public static RenderedOutput RenderAuto(string payload, OutputFormat format, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var qr = EncodeAuto(payload, detectOptions, options);
        return QrEasy.Render(qr, format, options, extras);
    }

    /// <summary>
    /// Renders a payload with embedded defaults to the requested output format.
    /// </summary>
    public static RenderedOutput Render(QrPayloadData payload, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var qr = Encode(payload, options);
        return QrEasy.Render(qr, format, options, extras);
    }

    /// <summary>
    /// Saves this QR code to a file, choosing the output format based on file extension.
    /// </summary>
    public string Save(string path, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(format, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves this QR code to a stream in the specified format.
    /// </summary>
    public void Save(OutputFormat format, System.IO.Stream stream, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var output = Render(format, options, extras);
        OutputWriter.Write(stream, output);
    }

    /// <summary>
    /// Saves a payload to a file, choosing the output format based on file extension.
    /// </summary>
    public static string Save(string payload, string path, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var output = Render(payload, OutputFormatInfo.Resolve(path, OutputFormat.Png), options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves a payload with embedded defaults to a file, choosing the output format based on file extension.
    /// </summary>
    public static string Save(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var output = Render(payload, OutputFormatInfo.Resolve(path, OutputFormat.Png), options, extras);
        return OutputWriter.Write(path, output);
    }
}

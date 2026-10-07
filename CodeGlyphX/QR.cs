using System;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX;

/// <summary>
/// Encodes, renders and saves QR symbols with separate encoding, appearance and output settings.
/// </summary>
/// <example><code>QR.Save("https://example.com", "qr.png");</code></example>
public static partial class QR {
    /// <summary>Starts a builder with its own copies of appearance and encoding options.</summary>
    public static QrBuilder Create(string payload, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        new QrBuilder(payload, renderOptions, encodingOptions);

    /// <summary>Starts a builder, using payload recommendations when encoding options are omitted.</summary>
    public static QrBuilder Create(QrPayloadData payload, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        new QrBuilder(payload, renderOptions, encodingOptions);

    /// <summary>Encodes text using the supplied settings, or stable M/UTF-8/automatic ECI defaults.</summary>
    public static QrCode Encode(string payload, QrEncodingOptions? encodingOptions = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        return QrCodeEncoder.EncodeText(payload, encodingOptions ?? new QrEncodingOptions());
    }

    /// <summary>
    /// Encodes a typed payload. Recommendations apply only when encoding options are omitted;
    /// an explicit options object controls the complete encoding operation.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEncodingOptions? encodingOptions = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        return Encode(payload.Text, ResolveEncodingOptions(payload, encodingOptions));
    }

    /// <summary>Detects a payload type and encodes it, using detected recommendations when options are omitted.</summary>
    public static QrCode EncodeAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEncodingOptions? encodingOptions = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        return Encode(QrPayloads.Detect(payload, detectOptions), encodingOptions);
    }

    internal static QrEncodingOptions ResolveEncodingOptions(QrPayloadData? payload, QrEncodingOptions? options) {
        if (options is not null) return options.Clone();
        return new QrEncodingOptions {
            ErrorCorrectionLevel = payload?.ErrorCorrectionLevel ?? QrErrorCorrectionLevel.M,
            TextEncoding = payload?.TextEncoding ?? QrTextEncoding.Utf8,
            MinVersion = payload?.MinVersion ?? 1,
            MaxVersion = payload?.MaxVersion ?? 40
        };
    }

    /// <summary>Encodes text and renders the resulting symbol. Appearance never changes encoding settings.</summary>
    public static RenderedOutput Render(string payload, OutputFormat format, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        Encode(payload, encodingOptions).Render(format, renderOptions, outputOptions);

    /// <summary>Encodes a typed payload and renders the resulting symbol.</summary>
    public static RenderedOutput Render(QrPayloadData payload, OutputFormat format, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        Encode(payload, encodingOptions).Render(format, renderOptions, outputOptions);

    /// <summary>Detects and encodes a payload, then renders the resulting symbol.</summary>
    public static RenderedOutput RenderAuto(string payload, OutputFormat format, QrPayloadDetectOptions? detectOptions = null, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        EncodeAuto(payload, detectOptions, encodingOptions).Render(format, renderOptions, outputOptions);

    /// <summary>Renders text as console-friendly ASCII with automatic sizing.</summary>
    public static string AsciiConsole(string payload, AsciiConsoleOptions? consoleOptions = null, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        Render(payload, OutputFormat.Ascii, renderOptions, encodingOptions, new OutputOptions { AsciiConsole = consoleOptions }).GetText();

    /// <summary>Renders a typed payload as console-friendly ASCII with automatic sizing.</summary>
    public static string AsciiConsole(QrPayloadData payload, AsciiConsoleOptions? consoleOptions = null, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        Render(payload, OutputFormat.Ascii, renderOptions, encodingOptions, new OutputOptions { AsciiConsole = consoleOptions }).GetText();

    /// <summary>Detects a payload and renders console-friendly ASCII with automatic sizing.</summary>
    public static string AsciiConsoleAuto(string payload, QrPayloadDetectOptions? detectOptions = null, AsciiConsoleOptions? consoleOptions = null, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        RenderAuto(payload, OutputFormat.Ascii, detectOptions, renderOptions, encodingOptions, new OutputOptions { AsciiConsole = consoleOptions }).GetText();
}

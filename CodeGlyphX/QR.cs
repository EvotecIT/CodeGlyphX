using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Simple QR helpers with fluent and static APIs.
/// </summary>
/// <remarks>
/// Use <see cref="Save(string,string,CodeGlyphX.QrEasyOptions,CodeGlyphX.Rendering.RenderExtras)"/> to pick the output format by file extension.
/// </remarks>
/// <example>
/// <code>
/// using CodeGlyphX;
/// QR.Save("https://example.com", "qr.png");
/// </code>
/// </example>
public static partial class QR {
    /// <summary>
    /// Starts a fluent QR builder for plain text.
    /// </summary>
    public static QrBuilder Create(string payload, QrEasyOptions? options = null) {
        return new QrBuilder(payload, options);
    }

    /// <summary>
    /// Starts a fluent QR builder for a payload with embedded defaults.
    /// </summary>
    public static QrBuilder Create(QrPayloadData payload, QrEasyOptions? options = null) {
        return new QrBuilder(payload, options);
    }

    /// <summary>
    /// Encodes a payload into a QR code.
    /// </summary>
    public static QrCode Encode(string payload, QrEasyOptions? options = null) => QrEasy.Encode(payload, options);

    /// <summary>
    /// Detects a payload type and encodes it into a QR code.
    /// </summary>
    public static QrCode EncodeAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        return QrEasy.EncodeAuto(payload, detectOptions, options);
    }

    /// <summary>
    /// Encodes a payload with embedded defaults into a QR code.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEasyOptions? options = null) => QrEasy.Encode(payload, options);

    private static RenderedOutput Render(string payload, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrCode.Render(payload, format, options, extras);
    }

    private static RenderedOutput RenderAuto(string payload, OutputFormat format, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrCode.RenderAuto(payload, format, detectOptions, options, extras);
    }

    private static RenderedOutput Render(QrPayloadData payload, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrCode.Render(payload, format, options, extras);
    }

    /// <summary>
    /// Renders a QR code as console-friendly ASCII text with auto-fit.
    /// </summary>
    public static string AsciiConsole(string payload, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        return Render(payload, OutputFormat.Ascii, options, new RenderExtras { AsciiConsole = consoleOptions }).GetText();
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as console-friendly ASCII text with auto-fit.
    /// </summary>
    public static string AsciiConsoleAuto(string payload, QrPayloadDetectOptions? detectOptions = null, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        return RenderAuto(payload, OutputFormat.Ascii, detectOptions, options, new RenderExtras { AsciiConsole = consoleOptions }).GetText();
    }

    /// <summary>
    /// Renders a QR code as console-friendly ASCII text with auto-fit for a payload with embedded defaults.
    /// </summary>
    public static string AsciiConsole(QrPayloadData payload, AsciiConsoleOptions? consoleOptions = null, QrEasyOptions? options = null) {
        return Render(payload, OutputFormat.Ascii, options, new RenderExtras { AsciiConsole = consoleOptions }).GetText();
    }
}

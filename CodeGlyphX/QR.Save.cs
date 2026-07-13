using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QR {

    /// <summary>
    /// Saves a QR code to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string payload, string path, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = QrCode.Render(payload, format, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Detects a payload type and saves a QR code to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveAuto(string payload, string path, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = QrCode.RenderAuto(payload, format, detectOptions, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves a QR code to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderExtras? extras = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = QrCode.Render(payload, format, options, extras);
        return OutputWriter.Write(path, output);
    }
}

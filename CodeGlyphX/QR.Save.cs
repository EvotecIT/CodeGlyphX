using System.IO;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class QR {
    /// <summary>Saves text as a QR symbol, selecting the output format from the extension (PNG by default).</summary>
    public static string Save(string payload, string path, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        Encode(payload, encodingOptions).Save(path, renderOptions, outputOptions);

    /// <summary>Saves a typed payload, selecting the output format from the extension (PNG by default).</summary>
    public static string Save(QrPayloadData payload, string path, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        Encode(payload, encodingOptions).Save(path, renderOptions, outputOptions);

    /// <summary>Detects and saves a payload, selecting the output format from the extension (PNG by default).</summary>
    public static string SaveAuto(string payload, string path, QrPayloadDetectOptions? detectOptions = null, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        EncodeAuto(payload, detectOptions, encodingOptions).Save(path, renderOptions, outputOptions);

    /// <summary>Writes a QR symbol to a caller-owned stream, leaving it open.</summary>
    public static void Save(string payload, Stream stream, OutputFormat format, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        Encode(payload, encodingOptions).Save(stream, format, renderOptions, outputOptions);

    /// <summary>Writes a typed payload to a caller-owned stream, leaving it open.</summary>
    public static void Save(QrPayloadData payload, Stream stream, OutputFormat format, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null, OutputOptions? outputOptions = null) =>
        Encode(payload, encodingOptions).Save(stream, format, renderOptions, outputOptions);
}

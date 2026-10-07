using CodeGlyphX.Payloads;

namespace CodeGlyphX;

public static partial class QR {
    /// <summary>Encodes text and renders an RGBA buffer without an image container.</summary>
    public static byte[] RenderPixels(string payload, out int widthPx, out int heightPx, out int stride, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        Encode(payload, encodingOptions).RenderPixels(out widthPx, out heightPx, out stride, renderOptions);

    /// <summary>Encodes a typed payload and renders an RGBA buffer without an image container.</summary>
    public static byte[] RenderPixels(QrPayloadData payload, out int widthPx, out int heightPx, out int stride, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        Encode(payload, encodingOptions).RenderPixels(out widthPx, out heightPx, out stride, renderOptions);

    /// <summary>Evaluates static appearance heuristics; the report does not guarantee scanner interoperability.</summary>
    public static QrArtHeuristicReport EvaluateScanHeuristics(string payload, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        Encode(payload, encodingOptions).EvaluateScanHeuristics(renderOptions);

    /// <summary>Evaluates static appearance heuristics for a typed payload.</summary>
    public static QrArtHeuristicReport EvaluateScanHeuristics(QrPayloadData payload, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) =>
        Encode(payload, encodingOptions).EvaluateScanHeuristics(renderOptions);
}

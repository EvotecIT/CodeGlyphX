using System;
using System.Text;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Represents rendered output (text or binary).
/// </summary>
public sealed class RenderedOutput {
    private string? _text;

    /// <summary>
    /// Output format.
    /// </summary>
    public OutputFormat Format { get; }

    /// <summary>
    /// Output kind.
    /// </summary>
    public OutputKind Kind { get; }

    /// <summary>
    /// MIME type when known.
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// Output bytes.
    /// </summary>
    public byte[] Data { get; }

    private RenderedOutput(OutputFormat format, OutputKind kind, byte[] data, string? text) {
        Format = format;
        Kind = kind;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        _text = text;
        MimeType = OutputFormatInfo.GetMimeType(format);
    }

    /// <summary>
    /// Creates a binary output.
    /// </summary>
    public static RenderedOutput FromBinary(OutputFormat format, byte[] data) {
        return new RenderedOutput(format, OutputKind.Binary, data, text: null);
    }

    /// <summary>
    /// Creates a text output (UTF-8 bytes).
    /// </summary>
    public static RenderedOutput FromText(OutputFormat format, string text, Encoding? encoding = null) {
        var enc = encoding ?? Encoding.UTF8;
        var bytes = enc.GetBytes(text ?? string.Empty);
        return new RenderedOutput(format, OutputKind.Text, bytes, text ?? string.Empty);
    }

    /// <summary>
    /// Returns the text representation when this is a text output.
    /// </summary>
    public string GetText(Encoding? encoding = null) {
        if (Kind != OutputKind.Text) {
            throw new InvalidOperationException("Output is binary.");
        }
        if (_text is not null) return _text;
        var enc = encoding ?? Encoding.UTF8;
        _text = enc.GetString(Data);
        return _text;
    }

    /// <summary>
    /// Returns true when this output is textual.
    /// </summary>
    public bool IsText => Kind == OutputKind.Text;
}

using System;
using System.Text;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Represents rendered output (text or binary).
/// </summary>
public sealed class RenderedOutput {
    private readonly string? _text;
    private readonly byte[] _data;

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
    /// Gets a read-only view of the output bytes owned by this result.
    /// Use <see cref="ToArray"/> when an independent mutable array is required.
    /// </summary>
    public ReadOnlyMemory<byte> Data => _data;

    internal byte[] OwnedBytes => _data;

    private RenderedOutput(OutputFormat format, OutputKind kind, byte[] data, string? text) {
        Format = format;
        Kind = kind;
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _text = text;
        MimeType = OutputFormatInfo.GetMimeType(format);
    }

    /// <summary>
    /// Creates a binary output.
    /// </summary>
    public static RenderedOutput FromBinary(OutputFormat format, byte[] data) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return new RenderedOutput(format, OutputKind.Binary, (byte[])data.Clone(), text: null);
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
    public string GetText() {
        if (Kind != OutputKind.Text) {
            throw new InvalidOperationException("Output is binary.");
        }
        return _text!;
    }

    /// <summary>
    /// Copies the output bytes to an independently owned mutable array.
    /// </summary>
    public byte[] ToArray() => (byte[])_data.Clone();

    /// <summary>
    /// Returns true when this output is textual.
    /// </summary>
    public bool IsText => Kind == OutputKind.Text;
}

using System;

namespace CodeGlyphX;

/// <summary>
/// Result of decoding a 1D barcode payload.
/// </summary>
public sealed class BarcodeDecoded {
    /// <summary>
    /// Gets the decoded barcode type.
    /// </summary>
    public BarcodeType Type { get; }

    /// <summary>
    /// Gets the decoded text (for EAN/UPC add-ons this includes a '+' suffix, e.g. "123456789012+12").
    /// </summary>
    public string Text { get; }

    internal BarcodeDecoded(BarcodeType type, string text) {
        Type = type;
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}

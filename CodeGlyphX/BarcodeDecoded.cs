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
    /// <remarks>When <see cref="ReaderInitialization"/> is true, this is reader-configuration content for inspection.</remarks>
    public string Text { get; }

    /// <summary>
    /// Gets whether Code 128 FNC3 marks this symbol as reader configuration.
    /// </summary>
    /// <remarks>The library exposes the content and flag; it does not apply reader settings.</remarks>
    public bool ReaderInitialization { get; }

    // Configuration and ordinary data can have identical visible contents.
    internal string DeduplicationKey => Type + "\u001f" + ReaderInitialization + "\u001f" + Text;

    internal BarcodeDecoded(BarcodeType type, string text, bool readerInitialization = false) {
        Type = type;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        ReaderInitialization = readerInitialization;
    }
}

using System;

namespace CodeGlyphX;

/// <summary>
/// Result of decoding a QR or barcode symbol.
/// </summary>
public sealed class CodeGlyphDecoded {
    /// <summary>
    /// Gets the decoded symbol kind.
    /// </summary>
    public CodeGlyphKind Kind { get; }

    /// <summary>
    /// Gets the decoded QR result when <see cref="Kind"/> is <see cref="CodeGlyphKind.Qr"/>.
    /// </summary>
    public QrDecoded? Qr { get; }

    /// <summary>
    /// Gets the decoded barcode result when <see cref="Kind"/> is <see cref="CodeGlyphKind.Barcode1D"/>.
    /// </summary>
    public BarcodeDecoded? Barcode { get; }

    /// <summary>
    /// Gets the decoded Data Matrix text when <see cref="Kind"/> is <see cref="CodeGlyphKind.DataMatrix"/>.
    /// </summary>
    public string? DataMatrixText { get; }

    /// <summary>
    /// Gets the decoded PDF417 text when <see cref="Kind"/> is <see cref="CodeGlyphKind.Pdf417"/>.
    /// </summary>
    public string? Pdf417Text { get; }

    /// <summary>
    /// Gets the decoded text (QR or barcode).
    /// </summary>
    public string Text => Qr?.Text ?? Barcode?.Text ?? DataMatrixText ?? Pdf417Text ?? string.Empty;

    /// <summary>
    /// Gets the decoded payload bytes for QR codes.
    /// </summary>
    public byte[]? Bytes => Qr?.Bytes;

    internal CodeGlyphDecoded(QrDecoded qr) {
        Qr = qr ?? throw new ArgumentNullException(nameof(qr));
        Kind = CodeGlyphKind.Qr;
    }

    internal CodeGlyphDecoded(BarcodeDecoded barcode) {
        Barcode = barcode ?? throw new ArgumentNullException(nameof(barcode));
        Kind = CodeGlyphKind.Barcode1D;
    }

    internal CodeGlyphDecoded(CodeGlyphKind kind, string text) {
        if (kind != CodeGlyphKind.DataMatrix && kind != CodeGlyphKind.Pdf417) {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only DataMatrix or Pdf417 allowed.");
        }
        if (text is null) throw new ArgumentNullException(nameof(text));
        Kind = kind;
        if (kind == CodeGlyphKind.DataMatrix) {
            DataMatrixText = text;
        } else {
            Pdf417Text = text;
        }
    }
}

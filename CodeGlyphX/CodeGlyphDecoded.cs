using System;
using CodeGlyphX.Pdf417;

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
    /// Gets the decoded PDF417 payload when <see cref="Kind"/> is <see cref="CodeGlyphKind.Pdf417"/>.
    /// </summary>
    public Pdf417Decoded? Pdf417 { get; }

    /// <summary>
    /// Gets the Macro PDF417 metadata when present.
    /// </summary>
    public Pdf417MacroMetadata? Pdf417Macro => Pdf417?.Macro;

    /// <summary>
    /// Gets the decoded Aztec text when <see cref="Kind"/> is <see cref="CodeGlyphKind.Aztec"/>.
    /// </summary>
    public string? AztecText { get; }

    /// <summary>
    /// Gets the decoded text (QR/Barcode/DataMatrix/PDF417/Aztec).
    /// </summary>
    public string Text => Qr?.Text ?? Barcode?.Text ?? DataMatrixText ?? Pdf417Text ?? AztecText ?? string.Empty;

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

    internal CodeGlyphDecoded(Pdf417Decoded pdf417) {
        Pdf417 = pdf417 ?? throw new ArgumentNullException(nameof(pdf417));
        Pdf417Text = pdf417.Text;
        Kind = CodeGlyphKind.Pdf417;
    }

    internal CodeGlyphDecoded(CodeGlyphKind kind, string text) {
        if (kind != CodeGlyphKind.DataMatrix && kind != CodeGlyphKind.Pdf417 && kind != CodeGlyphKind.Aztec) {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only DataMatrix, Pdf417, or Aztec allowed.");
        }
        if (text is null) throw new ArgumentNullException(nameof(text));
        Kind = kind;
        if (kind == CodeGlyphKind.DataMatrix) {
            DataMatrixText = text;
        } else if (kind == CodeGlyphKind.Pdf417) {
            Pdf417Text = text;
        } else {
            AztecText = text;
        }
    }
}

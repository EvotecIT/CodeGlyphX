using System;
using CodeGlyphX.Kix;

namespace CodeGlyphX;

/// <summary>
/// Encodes 2D barcode symbologies into a <see cref="BitMatrix"/>.
/// </summary>
public static class MatrixBarcodeEncoder {
    /// <summary>
    /// Encodes a 2D barcode using the specified <see cref="BarcodeType"/>.
    /// </summary>
    public static BitMatrix Encode(BarcodeType type, string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return type switch {
            BarcodeType.KixCode => KixEncoder.Encode(value),
            BarcodeType.DataMatrix => DataMatrix.DataMatrixEncoder.Encode(value),
            BarcodeType.PDF417 => Pdf417.Pdf417Encoder.Encode(value),
            _ => throw new NotSupportedException($"BarcodeType.{type} is not a 2D matrix barcode.")
        };
    }

    /// <summary>
    /// Encodes a KIX code into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeKix(string value) => KixEncoder.Encode(value);

    /// <summary>
    /// Encodes a Data Matrix symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeDataMatrix(string value) => DataMatrix.DataMatrixEncoder.Encode(value);

    /// <summary>
    /// Encodes a PDF417 symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodePdf417(string value) => Pdf417.Pdf417Encoder.Encode(value);
}

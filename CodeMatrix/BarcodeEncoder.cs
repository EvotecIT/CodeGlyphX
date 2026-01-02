using System;
using CodeMatrix.Code128;

namespace CodeMatrix;

/// <summary>
/// Encodes supported barcode symbologies into a <see cref="Barcode1D"/> model.
/// </summary>
public static class BarcodeEncoder {
    /// <summary>
    /// Encodes a barcode value using the specified <see cref="BarcodeType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when the requested type is not implemented yet.</exception>
    public static Barcode1D Encode(BarcodeType type, string value) {
        return type switch {
            BarcodeType.Code128 => Code128Encoder.Encode(value),
            _ => throw new NotSupportedException(
                $"BarcodeType.{type} is not supported yet. See ROADMAP.md: Phase 5 — Improvements."),
        };
    }
}

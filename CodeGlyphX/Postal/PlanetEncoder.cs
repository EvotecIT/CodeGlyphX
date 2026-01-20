using System;

namespace CodeGlyphX.Postal;

/// <summary>
/// Encodes PLANET barcodes.
/// </summary>
public static class PlanetEncoder {
    /// <summary>
    /// Encodes a PLANET barcode into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix Encode(string content) {
        return PostalEncoder.Encode(content, invert: true);
    }
}

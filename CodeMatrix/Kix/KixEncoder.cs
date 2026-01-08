using System;
using CodeMatrix.RoyalMail;

namespace CodeMatrix.Kix;

/// <summary>
/// Encodes KIX (Royal Mail 4-state, without headers) barcodes.
/// </summary>
public static class KixEncoder {
    /// <summary>
    /// Encodes a KIX barcode into a matrix.
    /// </summary>
    public static BitMatrix Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        return RoyalMailFourStateEncoder.Encode(content, includeHeaders: false);
    }
}

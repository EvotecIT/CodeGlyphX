namespace CodeGlyphX;

/// <summary>
/// Structured append metadata for multi-symbol QR payloads.
/// </summary>
public readonly struct QrStructuredAppend {
    /// <summary>
    /// Index of this symbol in the sequence (1..16).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Total number of symbols in the sequence (1..16).
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Parity for reassembly (0..255).
    /// </summary>
    public int Parity { get; }

    /// <summary>
    /// Returns true when the values are within the QR structured-append range.
    /// </summary>
    public bool IsValid => Index >= 1 && Index <= 16 && Total >= 1 && Total <= 16;

    /// <summary>
    /// Creates structured append metadata.
    /// </summary>
    /// <param name="index">Index of this symbol in the sequence (1..16).</param>
    /// <param name="total">Total number of symbols in the sequence (1..16).</param>
    /// <param name="parity">Parity for reassembly (0..255).</param>
    public QrStructuredAppend(int index, int total, int parity) {
        Index = index;
        Total = total;
        Parity = parity;
    }

    /// <inheritdoc />
    public override string ToString() {
        return IsValid ? $"{Index}/{Total} p{Parity}" : $"invalid ({Index}/{Total} p{Parity})";
    }
}

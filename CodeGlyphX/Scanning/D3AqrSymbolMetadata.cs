namespace CodeGlyphX;

/// <summary>The provisional D3 category and standard QR host metadata.</summary>
public sealed class D3AqrSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the fourteen-bit category.</summary>
    public int Category { get; }
    /// <summary>Gets the D3 to host module-pitch ratio.</summary>
    public double HostRatio { get; }
    /// <summary>Gets version, mask, error correction and control metadata of the QR host.</summary>
    public QrSymbolMetadata Host { get; }
    internal D3AqrSymbolMetadata(D3Aqr.D3AqrDecoded value) {
        Category = value.Category;
        HostRatio = value.HostRatio;
        Host = new QrSymbolMetadata(value.Host);
    }
}

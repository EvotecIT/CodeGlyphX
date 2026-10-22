using CodeGlyphX.AustraliaPost;

namespace CodeGlyphX;

/// <summary>An owned Australia Post symbol retaining the supplied FCC, DPID, customer field and readable parity.</summary>
public sealed class AustraliaPostSymbol : MatrixSymbol {
    /// <summary>Gets the exact source fields and parity values before padding ambiguity can affect a decoded interpretation.</summary>
    public AustraliaPostDecoded Data { get; }
    internal AustraliaPostSymbol(BitMatrix modules, AustraliaPostDecoded data) : base(SymbolFormat.AustraliaPost, modules) => Data = data;
}

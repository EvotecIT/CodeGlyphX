using System;

namespace CodeGlyphX;

/// <summary>
/// Encoded MaxiCode symbol and its fixed 30 by 33 sampled module grid.
/// </summary>
public sealed class MaxiCodeSymbol {
    /// <summary>Gets the encoded mode.</summary>
    public MaxiCodeMode Mode { get; }
    /// <summary>Gets the fixed sampled module grid. The central bullseye is renderer metadata rather than data modules.</summary>
    public BitMatrix Modules { get; }

    internal MaxiCodeSymbol(MaxiCodeMode mode, BitMatrix modules) {
        Mode = mode;
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
    }
}

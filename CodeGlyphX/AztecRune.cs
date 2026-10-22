using System;
using System.Globalization;
using CodeGlyphX.Aztec;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>Encodes and decodes the 256 numeric Aztec Runes defined by ISO/IEC 24778.</summary>
/// <remarks>A Rune is an 11 by 11 module mark, distinct from compact Aztec Code.</remarks>
public static class AztecRune {
    /// <summary>Encodes a value from zero through 255 without a quiet zone.</summary>
    public static MatrixSymbol Encode(int value) {
        if ((uint)value > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), "An Aztec Rune carries a value from zero through 255.");
        return new MatrixSymbol(SymbolFormat.AztecRune, AztecEncoder.EncodeRune(value));
    }

    /// <summary>Attempts to decode a sampled Rune in any right-angle rotation or reflected orientation.</summary>
    public static bool TryDecode(BitMatrix modules, out int value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        value = 0;
        if (modules.Width != 11 || modules.Height != 11) return false;
        for (var mirror = 0; mirror < 2; mirror++)
            for (var rotation = 0; rotation < 4; rotation++)
                if (AztecRuneCodec.TryDecode(new BitMatrixView(modules, rotation, mirror != 0), out value)) return true;
        return false;
    }

    internal static MatrixSymbol EncodeText(string value) {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || (uint)number > byte.MaxValue)
            throw new ArgumentException("An Aztec Rune requires a decimal value from zero through 255.", nameof(value));
        return Encode(number);
    }

    internal static DetectedSymbol Result(int value) => new(SymbolFormat.AztecRune,
        value.ToString(CultureInfo.InvariantCulture), new[] { (byte)value }, new AztecRuneSymbolMetadata(value));
}

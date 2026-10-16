using System;

namespace CodeGlyphX.Postal;

/// <summary>Renders and reads explicitly specified descender, ascender, full-height, and tracker bars.</summary>
/// <remarks>DAFT describes bar states rather than a self-identifying payload symbology. Decoding requires a format hint.</remarks>
public static class DaftCode {
    /// <summary>Encodes a nonempty sequence of uppercase D, A, F, and T into a logical three-row state grid.</summary>
    public static MatrixSymbol Encode(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) throw new ArgumentException("DAFT requires at least one bar.", nameof(value));
        var modules = new BitMatrix(checked(value.Length * 2 - 1), 3);
        for (var i = 0; i < value.Length; i++) {
            var state = value[i];
            if (state != 'D' && state != 'A' && state != 'F' && state != 'T')
                throw new ArgumentException("DAFT accepts uppercase D, A, F, and T only.", nameof(value));
            modules[i * 2, 0] = state == 'A' || state == 'F';
            modules[i * 2, 1] = true;
            modules[i * 2, 2] = state == 'D' || state == 'F';
        }
        return new MatrixSymbol(SymbolFormat.Daft, modules);
    }

    /// <summary>Reads exact logical states, rejecting missing trackers, nonempty gaps, and other grid sizes.</summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        text = string.Empty;
        if (modules is null || modules.Height != 3 || modules.Width == 0 || (modules.Width & 1) == 0) return false;
        var result = new char[(modules.Width + 1) / 2];
        for (var x = 0; x < modules.Width; x++) {
            if ((x & 1) != 0) {
                if (modules[x, 0] || modules[x, 1] || modules[x, 2]) return false;
            } else {
                if (!modules[x, 1]) return false;
                result[x / 2] = modules[x, 0] ? (modules[x, 2] ? 'F' : 'A') : (modules[x, 2] ? 'D' : 'T');
            }
        }
        text = new string(result);
        return true;
    }
}

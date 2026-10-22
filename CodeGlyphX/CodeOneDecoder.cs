using System;
using System.Globalization;
using System.Text;
using System.Threading;
using CodeGlyphX.CodeOne;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>Decodes sampled Code One module grids and their control metadata.</summary>
public static class CodeOneDecoder {
    /// <summary>Attempts all right-angle and reflected orientations of a sampled grid.</summary>
    public static bool TryDecode(BitMatrix modules, out CodeOneDecoded decoded) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        decoded = null!;
        if (modules.Width > 148 || modules.Height > 148) return false;
        for (var mirror = 0; mirror < 2; mirror++)
            for (var rotation = 0; rotation < 4; rotation++)
                if (TryDecodeCanonical(new BitMatrixView(modules, rotation, mirror != 0), out decoded)) return true;
        return false;
    }

    internal static bool TryDecodeCanonical(BitMatrixView grid, out CodeOneDecoded decoded, CancellationToken cancellationToken = default) {
        decoded = null!;
        foreach (var layout in CodeOneLayout.All) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (!layout.TryExtract(grid, out var words) || !CodeOneErrorCorrection.Process(words, layout, decode: true, cancellationToken)) continue;
            if (layout.BitsPerWord == 5) {
                ulong value = 0;
                for (var i = 0; i < layout.DataCount; i++) value = (value << 5) | (uint)words[i];
                var limit = layout.Version == CodeOneVersion.S10 ? 1000000UL
                    : layout.Version == CodeOneVersion.S20 ? 1000000000000UL : 1000000000000000000UL;
                if (value == 0 || value > limit) return false;
                var text = (value - 1).ToString(CultureInfo.InvariantCulture);
                decoded = new CodeOneDecoded(text, Encoding.ASCII.GetBytes(text), layout.Version, false, Array.Empty<int>(), value - 1);
                return true;
            }
            if (!CodeOnePayloadDecoder.TryDecode(words, layout.DataCount, out var payload)) return false;
            decoded = new CodeOneDecoded(payload.Text, payload.Bytes, layout.Version, payload.IsGs1, payload.Assignments);
            return true;
        }
        return false;
    }

    internal static DetectedSymbol Result(CodeOneDecoded value) => new(SymbolFormat.CodeOne, value.Text,
        value.NumericValue.HasValue ? null : value.Bytes, new CodeOneSymbolMetadata(value),
        value.IsGs1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None);
}

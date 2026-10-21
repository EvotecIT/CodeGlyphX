using CodeGlyphX.Internal.ReedSolomon;
using CodeGlyphX.Internal;
using System.Threading;

namespace CodeGlyphX.CodeOne;

internal static class CodeOneErrorCorrection {
    private static readonly GenericGf ByteField = new(0x12d, 256, 0);
    private static readonly GenericGf NumericField = new(0x25, 32, 0);
    internal static bool Process(int[] words, CodeOneLayout layout, bool decode, CancellationToken cancellationToken = default) {
        var field = layout.BitsPerWord == 5 ? NumericField : ByteField;
        var dataCount = layout.DataCount / layout.Blocks;
        var eccCount = layout.EccCount / layout.Blocks;
        var encoder = decode ? null : new ReedSolomonEncoder(field);
        var decoder = decode ? new ReedSolomonDecoder(field) : null;
        for (var block = 0; block < layout.Blocks; block++) {
            if (decode && DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var values = new int[dataCount + eccCount];
            for (var i = 0; i < dataCount; i++) values[i] = words[i * layout.Blocks + block];
            if (decode) {
                for (var i = 0; i < eccCount; i++) values[dataCount + i] = words[layout.DataCount + i * layout.Blocks + block];
                try { decoder!.Decode(values, eccCount); }
                catch (ReedSolomonException) { return false; }
            } else encoder!.Encode(values, eccCount);
            for (var i = 0; i < dataCount; i++) words[i * layout.Blocks + block] = values[i];
            for (var i = 0; i < eccCount; i++) words[layout.DataCount + i * layout.Blocks + block] = values[dataCount + i];
        }
        return true;
    }
}

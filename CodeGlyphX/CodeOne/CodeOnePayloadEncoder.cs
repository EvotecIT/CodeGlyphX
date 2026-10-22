using System.Collections.Generic;

namespace CodeGlyphX.CodeOne;

internal static partial class CodeOnePayloadEncoder {
    internal static int[]? Encode(byte[] bytes, bool gs1, int capacity) {
        return Optimize(bytes, gs1, capacity);
    }
}

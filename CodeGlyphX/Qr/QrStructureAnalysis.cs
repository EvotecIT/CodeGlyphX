using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal static class QrStructureAnalysis {
    internal static bool TryGetVersionFromSize(int size, out int version) {
        version = (size - 17) / 4;
        return version is >= 1 and <= 40 && version * 4 + 17 == size;
    }

    internal static BitMatrix BuildFunctionMask(int version, int size) {
        var isFunction = new BitMatrix(size, size);

        MarkFinder(0, 0, isFunction);
        MarkFinder(size - 7, 0, isFunction);
        MarkFinder(0, size - 7, isFunction);

        // Timing patterns
        for (var i = 0; i < size; i++) {
            isFunction[6, i] = true;
            isFunction[i, 6] = true;
        }

        // Alignment patterns
        var align = QrTables.GetAlignmentPatternPositions(version);
        for (var i = 0; i < align.Length; i++) {
            for (var j = 0; j < align.Length; j++) {
                if ((i == 0 && j == 0) || (i == 0 && j == align.Length - 1) || (i == align.Length - 1 && j == 0)) {
                    continue;
                }
                MarkAlignment(align[i], align[j], isFunction);
            }
        }

        // Dark module
        isFunction[8, size - 8] = true;

        // Format info
        for (var i = 0; i <= 5; i++) isFunction[8, i] = true;
        isFunction[8, 7] = true;
        isFunction[8, 8] = true;
        isFunction[7, 8] = true;
        for (var i = 9; i < 15; i++) isFunction[14 - i, 8] = true;
        for (var i = 0; i < 8; i++) isFunction[size - 1 - i, 8] = true;
        for (var i = 8; i < 15; i++) isFunction[8, size - 15 + i] = true;

        // Version info
        if (version >= 7) {
            for (var i = 0; i < 18; i++) {
                var a = size - 11 + (i % 3);
                var b = i / 3;
                isFunction[a, b] = true;
                isFunction[b, a] = true;
            }
        }

        return isFunction;
    }

    private static void MarkFinder(int x, int y, BitMatrix isFunction) {
        for (var dy = -1; dy <= 7; dy++) {
            for (var dx = -1; dx <= 7; dx++) {
                var xx = x + dx;
                var yy = y + dy;
                if ((uint)xx >= (uint)isFunction.Width || (uint)yy >= (uint)isFunction.Height) continue;
                isFunction[xx, yy] = true;
            }
        }
    }

    private static void MarkAlignment(int x, int y, BitMatrix isFunction) {
        for (var dy = -2; dy <= 2; dy++) {
            for (var dx = -2; dx <= 2; dx++) {
                isFunction[x + dx, y + dy] = true;
            }
        }
    }
}


using System;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    private const byte Data = 0, Finder = 1, Separator = 2, Alignment = 3, OtherFunction = 4;

    /// <summary>
    /// Maps photo-treatment modules to their function zones and alignment-pattern centres. Both photo painters use the
    /// same map so finder separators and overlapping alignment positions retain identical ownership.
    /// </summary>
    private static void ClassifyPhotoFunctions(QrCode qr, BitMatrix functions, out byte[] kinds, out int[] alignmentCenters) {
        var size = qr.Size;
        kinds = new byte[size * size];
        alignmentCenters = new int[size * size];
        var alignment = QrTables.GetAlignmentPatternPositions(qr.Version);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            if (!functions[x, y]) continue;
            var i = y * size + x;
            if (InPhotoFinderZone(x, y, size)) {
                var lx = x < 8 ? x : x - (size - 7);
                var ly = y < 8 ? y : y - (size - 7);
                kinds[i] = lx is >= 0 and < 7 && ly is >= 0 and < 7 ? Finder : Separator;
                continue;
            }
            kinds[i] = OtherFunction;
            foreach (var ay in alignment) foreach (var ax in alignment) {
                if (Math.Abs(x - ax) > 2 || Math.Abs(y - ay) > 2 || InPhotoFinderZone(ax, ay, size)) continue;
                kinds[i] = Alignment;
                alignmentCenters[i] = ay * size + ax;
            }
        }
    }

    private static bool InPhotoFinderZone(int x, int y, int size) =>
        (x < 8 && y < 8) || (x >= size - 8 && y < 8) || (x < 8 && y >= size - 8);
}

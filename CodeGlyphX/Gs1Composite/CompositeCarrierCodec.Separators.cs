// Portions adapted from Zint, Copyright (c) Robin Stuart and contributors, BSD-3-Clause.
// See THIRD-PARTY-NOTICES.md.
namespace CodeGlyphX.Gs1Composite;

internal static partial class CompositeCarrierCodec {
    private static void FillDataBarSeparator(BitMatrix matrix, Gs1CompositeCarrier carrier, int finderLimit = int.MaxValue) {
        var end = carrier == Gs1CompositeCarrier.DataBarLimited ? 70 : matrix.Width - 4;
        for (var x = 4; x < end; x++) matrix[x, 0] = !matrix[x, 1];
        switch (carrier) {
            case Gs1CompositeCarrier.DataBarOmnidirectional:
            case Gs1CompositeCarrier.DataBarTruncated:
                FinderSeparator(matrix, 18, 31);
                FinderSeparator(matrix, 63, 76);
                break;
            case Gs1CompositeCarrier.DataBarStacked:
            case Gs1CompositeCarrier.DataBarStackedOmnidirectional:
                FinderSeparator(matrix, 18, 31);
                break;
            case Gs1CompositeCarrier.DataBarExpanded:
            case Gs1CompositeCarrier.DataBarExpandedStacked:
                // The first row runs left-to-right. Alternate finder versions use the first/last 13 modules.
                for (var block = 0; block < finderLimit && block * 49 + 19 < end; block++) {
                    var start = block * 49 + 19 + ((block & 1) == 0 ? 0 : 2);
                    FinderSeparator(matrix, start, start + 13);
                }
                break;
        }
    }

    private static void FinderSeparator(BitMatrix matrix, int start, int end) {
        var latch = true;
        for (var x = start; x < end && x < matrix.Width; x++) {
            if (matrix[x, 1]) { matrix[x, 0] = false; latch = true; }
            else { matrix[x, 0] = latch; latch = !latch; }
        }
    }
}

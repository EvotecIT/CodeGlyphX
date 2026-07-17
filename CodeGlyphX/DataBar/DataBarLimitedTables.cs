// Portions adapted from the Zint backend and ZXing-C++.
// Licensed under BSD-3-Clause and Apache-2.0; see THIRD-PARTY-NOTICES.md.

namespace CodeGlyphX.DataBar;

internal static class DataBarLimitedTables {
    internal const int PairDivisor = 2013571;
    internal const int MaximumPairValue = PairDivisor - 1;
    internal const long MaximumSymbolValue = 1999999999999L;

    internal static readonly int[] GroupSum = {
        0, 183064, 820064, 1000776, 1491021, 1979845, 1996939
    };

    internal static readonly int[] EvenTable = {
        28, 728, 6454, 203, 2408, 1, 16632
    };

    internal static readonly int[] OddModules = {
        17, 13, 9, 15, 11, 19, 7
    };

    internal static readonly int[] OddWidest = {
        6, 5, 3, 5, 4, 8, 1
    };

    // ISO/IEC 24724 Annex C check-character patterns, represented as 18 modules.
    internal static readonly int[] CheckPatterns = {
        174818, 174706, 174650, 174450, 174394, 174266, 173426, 173370,
        173242, 172730, 169330, 169274, 169146, 168634, 166586, 152946,
        152890, 152762, 152250, 150202, 142010, 174946, 174898, 174874,
        174514, 174490, 174298, 173490, 173466, 173274, 172762, 169394,
        169370, 169178, 168666, 166618, 153010, 152986, 152794, 152282,
        150234, 142042, 175010, 174994, 174546, 169426, 153042, 175458,
        175410, 175386, 175282, 169650, 169626, 169562, 168794, 166746,
        153266, 153242, 150362, 177506, 177458, 177434, 177330, 177306,
        176818, 154290, 154266, 154202, 153946, 150874, 142682, 185698,
        185650, 185626, 185522, 185498, 185434, 185010, 184986, 182962,
        218418, 218394, 218290, 218266, 218202, 217754, 217690, 215706,
        218514
    };
}

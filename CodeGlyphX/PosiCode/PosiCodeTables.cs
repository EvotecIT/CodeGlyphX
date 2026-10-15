using System;
using System.Collections.Generic;

namespace CodeGlyphX.PosiCode;

/// <summary>Character and gap definitions from ANSI/AIM BC8, including its check-character composition.</summary>
internal static class PosiCodeTables {
    internal const string Set0 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
    internal const string Set1 = "^;<=>?@[\\]abcdefghijklmnopqrstuvwxyz_`\u007f{|}~";
    internal const string Set2 = "'\u001b\u001c\u001d\u001e\u001f!\"#&\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000a\u000b\u000c\u000d\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a()\0*,:";
    internal static readonly string[][] Patterns = {
        "141111 131211 121311 111411 131112 121212 111312 121113 111213 111114 181111 171211 161311 151411 141511 131611 121711 111811 171112 161212 151312 141412 131512 121612 111712 161113 151213 141313 131413 121513 111613 151114 141214 131314 121414 111514 141115 131215 121315 111415 131116 121216 111316 121117 111217 111118".Split(' '),
        "151212 141312 131412 121512 141213 131313 121413 131214 121314 121215 191212 181312 171412 161512 151612 141712 131812 121912 181213 171313 161413 151513 141613 131713 121813 171214 161314 151414 141514 131614 121714 161215 151315 141415 131515 121615 151216 141316 131416 121516 141217 131317 121417 131218 121318 121219".Split(' '),
        "111411 111312 111213 111114 121311 121212 121113 141111 131211 131112 171111 161211 151311 141411 131511 121611 111711 161112 151212 141312 131412 121512 111612 151113 141213 131313 121413 111513 141114 131214 121314 111414 131115 121215 111315 121116 111216 111117".Split(' '),
        "121512 121413 121314 121215 131412 131313 131214 151212 141312 141213 181212 171312 161412 151512 141612 131712 121812 171213 161313 151413 141513 131613 121713 161214 151314 141414 131514 121614 151215 141315 131415 121515 141216 131316 121416 131217 121317 121218".Split(' ')
    };
    internal static readonly string[] Starts = { "1;111112", "1<121312", "151111", "141212" };
    internal static readonly string[] Stops = { "111111111;1", "121212121<1", "1", "1" };

    internal static void Validate(PosiCodeVariant variant) {
        if ((int)variant < 0 || (int)variant > 3) throw new ArgumentOutOfRangeException(nameof(variant));
    }

    internal static int CheckValue(IReadOnlyList<int> words, int start, int count, PosiCodeVariant variant, int sequence) {
        var check = 0;
        for (var i = start; i < start + count; i++) {
            var word = words[i];
            for (var bit = 0; bit < 6; bit++) {
                if (((word ^ check) & 1) != 0) check ^= 7682;
                check >>= 1;
                word >>= 1;
            }
        }
        check = (check + sequence) & 1023;
        if ((int)variant < 2) return check + 45;
        return check > 824 && check < 853 ? check + 292 : check;
    }

    internal static int[] CheckWidths(int rank, PosiCodeVariant variant) {
        var extra = 8;
        var widths = new int[6];
        for (var cell = 0; cell < 5; cell++) {
            var taken = 0;
            while (taken < extra) {
                var count = Choose(extra - taken + 4 - cell, 4 - cell);
                if (rank < count) break;
                rank -= count;
                taken++;
            }
            widths[cell] = taken + 2;
            extra -= taken;
        }
        widths[5] = extra + 2;
        var runs = new int[12];
        for (var i = 0; i < 6; i++) {
            runs[i * 2] = 1;
            runs[i * 2 + 1] = widths[5 - i] - (((int)variant & 1) == 0 ? 1 : 0);
        }
        return runs;
    }

    private static int Choose(int n, int k) {
        var result = 1;
        for (var i = 1; i <= k; i++) result = result * (n - k + i) / i;
        return result;
    }

    internal static void Append(List<BarSegment> bars, string pattern) {
        foreach (var width in pattern) bars.Add(new BarSegment((bars.Count & 1) == 0, width - '0'));
    }
}

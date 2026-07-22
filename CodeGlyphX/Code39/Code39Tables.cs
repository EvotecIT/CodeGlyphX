using System;
using System.Collections.Generic;

namespace CodeGlyphX.Code39;

internal static class Code39Tables {
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
    private const int AsteriskWidePattern = 0x094;

    // The nine low bits describe the alternating bars and spaces. A set bit is
    // wide (two modules); an unset bit is narrow (one module).
    private static readonly int[] WidePatterns = {
        0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064,
        0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C,
        0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016,
        0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x0A8,
        0x0A2, 0x08A, 0x02A
    };

    public static readonly IReadOnlyDictionary<char, (int value, bool[] data)> EncodingTable = CreateEncodingTable();

    private static IReadOnlyDictionary<char, (int value, bool[] data)> CreateEncodingTable() {
        var table = new Dictionary<char, (int value, bool[] data)>(Alphabet.Length + 1);
        for (var i = 0; i < Alphabet.Length; i++) {
            table.Add(Alphabet[i], (i, ExpandWidePattern(WidePatterns[i])));
        }
        table.Add('*', (-1, ExpandWidePattern(AsteriskWidePattern)));
        return table;
    }

    private static bool[] ExpandWidePattern(int pattern) {
        var wideElements = 0;
        for (var mask = 1 << 8; mask != 0; mask >>= 1) {
            if ((pattern & mask) != 0) wideElements++;
        }
        if (wideElements != 3 || (pattern & ~0x1FF) != 0) {
            throw new InvalidOperationException("A Code 39 pattern must contain nine elements with exactly three wide elements.");
        }

        var modules = new bool[12];
        var offset = 0;
        var isBar = true;

        for (var mask = 1 << 8; mask != 0; mask >>= 1) {
            var width = (pattern & mask) != 0 ? 2 : 1;
            for (var i = 0; i < width; i++) {
                modules[offset++] = isBar;
            }
            isBar = !isBar;
        }

        return modules;
    }

    public static readonly IReadOnlyDictionary<char, string> ExtendedTable = new Dictionary<char, string> {
        { '\u0000', "%U" },
        { '\u0001', "$A" },
        { '\u0002', "$B" },
        { '\u0003', "$C" },
        { '\u0004', "$D" },
        { '\u0005', "$E" },
        { '\u0006', "$F" },
        { '\u0007', "$G" },
        { '\u0008', "$H" },
        { '\t', "$I" },
        { '\n', "$J" },
        { '\u000B', "$K" },
        { '\u000C', "$L" },
        { '\r', "$M" },
        { '\u000E', "$N" },
        { '\u000F', "$O" },
        { '\u0010', "$P" },
        { '\u0011', "$Q" },
        { '\u0012', "$R" },
        { '\u0013', "$S" },
        { '\u0014', "$T" },
        { '\u0015', "$U" },
        { '\u0016', "$V" },
        { '\u0017', "$W" },
        { '\u0018', "$X" },
        { '\u0019', "$Y" },
        { '\u001A', "$Z" },
        { '\u001B', "%A" },
        { '\u001C', "%B" },
        { '\u001D', "%C" },
        { '\u001E', "%D" },
        { '\u001F', "%E" },
        { '!', "/A" },
        { '\"', "/B" },
        { '#', "/C" },
        { '$', "/D" },
        { '%', "/E" },
        { '&', "/F" },
        { '\'', "/G" },
        { '(', "/H" },
        { ')', "/I" },
        { '*', "/J" },
        { '+', "/K" },
        { ',', "/L" },
        { '/', "/O" },
        { ':', "/Z" },
        { ';', "%F" },
        { '<', "%G" },
        { '=', "%H" },
        { '>', "%I" },
        { '?', "%J" },
        { '@', "%V" },
        { '[', "%K" },
        { '\\', "%L" },
        { ']', "%M" },
        { '^', "%N" },
        { '_', "%O" },
        { '`', "%W" },
        { 'a', "+A" },
        { 'b', "+B" },
        { 'c', "+C" },
        { 'd', "+D" },
        { 'e', "+E" },
        { 'f', "+F" },
        { 'g', "+G" },
        { 'h', "+H" },
        { 'i', "+I" },
        { 'j', "+J" },
        { 'k', "+K" },
        { 'l', "+L" },
        { 'm', "+M" },
        { 'n', "+N" },
        { 'o', "+O" },
        { 'p', "+P" },
        { 'q', "+Q" },
        { 'r', "+R" },
        { 's', "+S" },
        { 't', "+T" },
        { 'u', "+U" },
        { 'v', "+V" },
        { 'w', "+W" },
        { 'x', "+X" },
        { 'y', "+Y" },
        { 'z', "+Z" },
        { '{', "%P" },
        { '|', "%Q" },
        { '}', "%R" },
        { '~', "%S" },
        { '\u007F', "%T" }
    };
}

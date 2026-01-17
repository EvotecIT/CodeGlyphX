using System.Collections.Generic;

namespace CodeGlyphX.Plessey;

internal static class PlesseyTables {
    public const string StartBits = "1101";
    public const string StopBits = "0011";

    public static readonly IReadOnlyDictionary<char, int> HexValues = new Dictionary<char, int> {
        { '0', 0 }, { '1', 1 }, { '2', 2 }, { '3', 3 }, { '4', 4 }, { '5', 5 }, { '6', 6 }, { '7', 7 },
        { '8', 8 }, { '9', 9 }, { 'A', 10 }, { 'B', 11 }, { 'C', 12 }, { 'D', 13 }, { 'E', 14 }, { 'F', 15 }
    };
}

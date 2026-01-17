using System.Collections.Generic;

namespace CodeGlyphX.Code11;

internal static class Code11Tables {
    public const string StartStopPattern = "00110";

    public static readonly IReadOnlyDictionary<char, string> EncodingTable = new Dictionary<char, string> {
        { '0', "00001" },
        { '1', "10001" },
        { '2', "01001" },
        { '3', "11000" },
        { '4', "00101" },
        { '5', "10100" },
        { '6', "01100" },
        { '7', "00011" },
        { '8', "10010" },
        { '9', "10000" },
        { '-', "00100" }
    };

    public static readonly IReadOnlyDictionary<char, int> ValueTable = new Dictionary<char, int> {
        { '0', 0 },
        { '1', 1 },
        { '2', 2 },
        { '3', 3 },
        { '4', 4 },
        { '5', 5 },
        { '6', 6 },
        { '7', 7 },
        { '8', 8 },
        { '9', 9 },
        { '-', 10 }
    };
}

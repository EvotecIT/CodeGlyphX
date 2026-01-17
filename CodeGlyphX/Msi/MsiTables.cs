using System.Collections.Generic;

namespace CodeGlyphX.Msi;

internal static class MsiTables {
    public const string Start = "110";
    public const string Stop = "1001";

    public static readonly IReadOnlyDictionary<char, string> DigitPatterns = new Dictionary<char, string> {
        { '0', "100100100100" },
        { '1', "100100100110" },
        { '2', "100100110100" },
        { '3', "100100110110" },
        { '4', "100110100100" },
        { '5', "100110100110" },
        { '6', "100110110100" },
        { '7', "100110110110" },
        { '8', "110100100100" },
        { '9', "110100100110" }
    };
}

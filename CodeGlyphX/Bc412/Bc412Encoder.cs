using System;
using System.Collections.Generic;

namespace CodeGlyphX.Bc412;

/// <summary>Encodes SEMI and IBM BC412 identifiers.</summary>
public static class Bc412Encoder {
    /// <summary>Encodes uppercase letters and digits, excluding O, using the selected check and guard convention.</summary>
    /// <remarks>SEMI requires 7..18 data characters, its check character and guards. IBM accepts 1..500 characters.
    /// Unframed IBM modules require explicit bounds and checksum information when decoding.</remarks>
    public static Barcode1D Encode(string value, Bc412Variant variant = Bc412Variant.Semi,
        bool includeChecksum = true, bool includeStartStop = true) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (variant != Bc412Variant.Semi && variant != Bc412Variant.Ibm) throw new ArgumentOutOfRangeException(nameof(variant));
        if (variant == Bc412Variant.Ibm) return EncodeIbm(value, includeChecksum, includeStartStop);
        if (!includeChecksum || !includeStartStop) throw new ArgumentException("SEMI BC412 requires its check character and start/stop guards.");
        if (value.Length < 7 || value.Length > 18)
            throw new ArgumentException("SEMI BC412 requires 7 through 18 data characters.", nameof(value));
        var data = new char[value.Length + 1];
        data[0] = value[0];
        data[1] = '0';
        value.CopyTo(1, data, 2, value.Length - 1);
        var sum = 0;
        for (var i = 0; i < data.Length; i++) {
            var digit = Bc412Tables.Alphabet.IndexOf(data[i]);
            if (digit < 0) throw new ArgumentException("BC412 requires digits and uppercase letters other than O.", nameof(value));
            sum += digit * ((i & 1) == 0 ? 1 : 2);
        }
        data[1] = Bc412Tables.Alphabet[sum * 17 % 35];
        var runs = new List<BarSegment>(data.Length * 8 + 5);
        Append(runs, "12");
        foreach (var character in data) Append(runs, Bc412Tables.Patterns[Bc412Tables.Alphabet.IndexOf(character)]);
        Append(runs, "111");
        return new Barcode1D(runs, SymbolFormat.Bc412);
    }

    private static Barcode1D EncodeIbm(string value, bool checksum, bool guards) {
        if (value.Length == 0 || value.Length > 500) throw new ArgumentException("IBM BC412 requires 1 through 500 data characters.", nameof(value));
        var runs = new List<BarSegment>(value.Length * 8 + 13);
        if (guards) Append(runs, "12");
        var sum = 0;
        foreach (var character in value) {
            var digit = Bc412Tables.Alphabet.IndexOf(character);
            if (digit < 0) throw new ArgumentException("BC412 requires digits and uppercase letters other than O.", nameof(value));
            sum += digit;
            Append(runs, Bc412Tables.Patterns[digit]);
        }
        if (checksum) Append(runs, Bc412Tables.Patterns[sum % 35]);
        if (guards) Append(runs, "111");
        return new Barcode1D(runs, SymbolFormat.Bc412);
    }

    private static void Append(List<BarSegment> runs, string pattern) {
        foreach (var width in pattern) runs.Add(new BarSegment((runs.Count & 1) == 0, width - '0'));
    }
}

/// <summary>The BC412 character order and four-bar, twelve-module symbol patterns.</summary>
internal static class Bc412Tables {
    internal const string Alphabet = "0R9GLVHA8EZ4NTS1J2Q6C7DYKBUIX3FWP5M";
    internal static readonly string[] Patterns = {
        "11111115", "13111212", "11131113", "12111213", "12121311",
        "13131111", "12111312", "11131212", "11121411", "11151111",
        "15111111", "11111511", "12131211", "13121112", "13111311",
        "11111214", "12121113", "11111313", "13111113", "11121213",
        "11141112", "11121312", "11141211", "14121111", "12121212",
        "11131311", "13121211", "12111411", "14111211", "11111412",
        "12111114", "14111112", "12141111", "11121114", "12131112"
    };
}

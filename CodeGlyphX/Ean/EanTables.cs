using System.Collections.Generic;

namespace CodeGlyphX.Ean;

internal static class EanTables {
    public readonly struct EncodedNumber {
        public EncodedNumber(bool[] leftOdd, bool[] leftEven, bool[] right, bool[] checksum) {
            LeftOdd = leftOdd;
            LeftEven = leftEven;
            Right = right;
            Checksum = checksum;
        }

        public bool[] LeftOdd { get; }
        public bool[] LeftEven { get; }
        public bool[] Right { get; }
        public bool[] Checksum { get; }
    }

    public static readonly IReadOnlyDictionary<char, EncodedNumber> EncodingTable = new Dictionary<char, EncodedNumber> {
        { '0', new EncodedNumber(
            new[] { false, false, false, true, true, false, true },
            new[] { false, true, false, false, true, true, true },
            new[] { true, true, true, false, false, true, false },
            new bool[6]) },
        { '1', new EncodedNumber(
            new[] { false, false, true, true, false, false, true },
            new[] { false, true, true, false, false, true, true },
            new[] { true, true, false, false, true, true, false },
            new[] { false, false, true, false, true, true }) },
        { '2', new EncodedNumber(
            new[] { false, false, true, false, false, true, true },
            new[] { false, false, true, true, false, true, true },
            new[] { true, true, false, true, true, false, false },
            new[] { false, false, true, true, false, true }) },
        { '3', new EncodedNumber(
            new[] { false, true, true, true, true, false, true },
            new[] { false, true, false, false, false, false, true },
            new[] { true, false, false, false, false, true, false },
            new[] { false, false, true, true, true, false }) },
        { '4', new EncodedNumber(
            new[] { false, true, false, false, false, true, true },
            new[] { false, false, true, true, true, false, true },
            new[] { true, false, true, true, true, false, false },
            new[] { false, true, false, false, true, true }) },
        { '5', new EncodedNumber(
            new[] { false, true, true, false, false, false, true },
            new[] { false, true, true, true, false, false, true },
            new[] { true, false, false, true, true, true, false },
            new[] { false, true, true, false, false, true }) },
        { '6', new EncodedNumber(
            new[] { false, true, false, true, true, true, true },
            new[] { false, false, false, false, true, false, true },
            new[] { true, false, true, false, false, false, false },
            new[] { false, true, true, true, false, false }) },
        { '7', new EncodedNumber(
            new[] { false, true, true, true, false, true, true },
            new[] { false, false, true, false, false, false, true },
            new[] { true, false, false, false, true, false, false },
            new[] { false, true, false, true, false, true }) },
        { '8', new EncodedNumber(
            new[] { false, true, true, false, true, true, true },
            new[] { false, false, false, true, false, false, true },
            new[] { true, false, false, true, false, false, false },
            new[] { false, true, false, true, true, false }) },
        { '9', new EncodedNumber(
            new[] { false, false, false, true, false, true, true },
            new[] { false, false, true, false, true, true, true },
            new[] { true, true, true, false, true, false, false },
            new[] { false, true, true, false, true, false }) }
    };
}

using System.Collections.Generic;

namespace CodeMatrix.UpcA;

internal static class UpcATables {
    public readonly struct EncodedNumber {
        public EncodedNumber(bool[] left, bool[] right) {
            Left = left;
            Right = right;
        }

        public bool[] Left { get; }
        public bool[] Right { get; }
    }

    public static readonly IReadOnlyDictionary<char, EncodedNumber> EncodingTable = new Dictionary<char, EncodedNumber> {
        { '0', new EncodedNumber(
            new[] { false, false, false, true, true, false, true },
            new[] { true, true, true, false, false, true, false }) },
        { '1', new EncodedNumber(
            new[] { false, false, true, true, false, false, true },
            new[] { true, true, false, false, true, true, false }) },
        { '2', new EncodedNumber(
            new[] { false, false, true, false, false, true, true },
            new[] { true, true, false, true, true, false, false }) },
        { '3', new EncodedNumber(
            new[] { false, true, true, true, true, false, true },
            new[] { true, false, false, false, false, true, false }) },
        { '4', new EncodedNumber(
            new[] { false, true, false, false, false, true, true },
            new[] { true, false, true, true, true, false, false }) },
        { '5', new EncodedNumber(
            new[] { false, true, true, false, false, false, true },
            new[] { true, false, false, true, true, true, false }) },
        { '6', new EncodedNumber(
            new[] { false, true, false, true, true, true, true },
            new[] { true, false, true, false, false, false, false }) },
        { '7', new EncodedNumber(
            new[] { false, true, true, true, false, true, true },
            new[] { true, false, false, false, true, false, false }) },
        { '8', new EncodedNumber(
            new[] { false, true, true, false, true, true, true },
            new[] { true, false, false, true, false, false, false }) },
        { '9', new EncodedNumber(
            new[] { false, false, false, true, false, true, true },
            new[] { true, true, true, false, true, false, false }) }
    };
}

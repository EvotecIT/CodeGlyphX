using System.Collections.Generic;

namespace CodeMatrix.UpcE;

internal static class UpcETables {
    public readonly struct EncodedNumber {
        public EncodedNumber(bool[] odd, bool[] even) {
            Odd = odd;
            Even = even;
        }

        public bool[] Odd { get; }
        public bool[] Even { get; }
    }

    public readonly struct ParityPatterns {
        public ParityPatterns(Parity[] numberSystemZero, Parity[] numberSystemOne) {
            NumberSystemZero = numberSystemZero;
            NumberSystemOne = numberSystemOne;
        }

        public Parity[] NumberSystemZero { get; }
        public Parity[] NumberSystemOne { get; }
    }

    public enum Parity {
        Odd,
        Even
    }

    public static readonly IReadOnlyDictionary<char, EncodedNumber> EncodingTable = new Dictionary<char, EncodedNumber> {
        { '0', new EncodedNumber(
            new[] { false, false, false, true, true, false, true },
            new[] { false, true, false, false, true, true, true }) },
        { '1', new EncodedNumber(
            new[] { false, false, true, true, false, false, true },
            new[] { false, true, true, false, false, true, true }) },
        { '2', new EncodedNumber(
            new[] { false, false, true, false, false, true, true },
            new[] { false, false, true, true, false, true, true }) },
        { '3', new EncodedNumber(
            new[] { false, true, true, true, true, false, true },
            new[] { false, true, false, false, false, false, true }) },
        { '4', new EncodedNumber(
            new[] { false, true, false, false, false, true, true },
            new[] { false, false, true, true, true, false, true }) },
        { '5', new EncodedNumber(
            new[] { false, true, true, false, false, false, true },
            new[] { false, true, true, true, false, false, true }) },
        { '6', new EncodedNumber(
            new[] { false, true, false, true, true, true, true },
            new[] { false, false, false, false, true, false, true }) },
        { '7', new EncodedNumber(
            new[] { false, true, true, true, false, true, true },
            new[] { false, false, true, false, false, false, true }) },
        { '8', new EncodedNumber(
            new[] { false, true, true, false, true, true, true },
            new[] { false, false, false, true, false, false, true }) },
        { '9', new EncodedNumber(
            new[] { false, false, false, true, false, true, true },
            new[] { false, false, true, false, true, true, true }) }
    };

    public static readonly IReadOnlyDictionary<char, ParityPatterns> ParityPatternTable = new Dictionary<char, ParityPatterns> {
        { '0', new ParityPatterns(
            new[] { Parity.Even, Parity.Even, Parity.Even, Parity.Odd, Parity.Odd, Parity.Odd },
            new[] { Parity.Odd, Parity.Odd, Parity.Odd, Parity.Even, Parity.Even, Parity.Even }) },
        { '1', new ParityPatterns(
            new[] { Parity.Even, Parity.Even, Parity.Odd, Parity.Even, Parity.Odd, Parity.Odd },
            new[] { Parity.Odd, Parity.Odd, Parity.Even, Parity.Odd, Parity.Even, Parity.Even }) },
        { '2', new ParityPatterns(
            new[] { Parity.Even, Parity.Even, Parity.Odd, Parity.Odd, Parity.Even, Parity.Odd },
            new[] { Parity.Odd, Parity.Odd, Parity.Even, Parity.Even, Parity.Odd, Parity.Even }) },
        { '3', new ParityPatterns(
            new[] { Parity.Even, Parity.Even, Parity.Odd, Parity.Odd, Parity.Odd, Parity.Even },
            new[] { Parity.Odd, Parity.Odd, Parity.Even, Parity.Even, Parity.Even, Parity.Odd }) },
        { '4', new ParityPatterns(
            new[] { Parity.Even, Parity.Odd, Parity.Even, Parity.Even, Parity.Odd, Parity.Odd },
            new[] { Parity.Odd, Parity.Even, Parity.Odd, Parity.Odd, Parity.Even, Parity.Even }) },
        { '5', new ParityPatterns(
            new[] { Parity.Even, Parity.Odd, Parity.Odd, Parity.Even, Parity.Even, Parity.Odd },
            new[] { Parity.Odd, Parity.Even, Parity.Even, Parity.Odd, Parity.Odd, Parity.Even }) },
        { '6', new ParityPatterns(
            new[] { Parity.Even, Parity.Odd, Parity.Odd, Parity.Odd, Parity.Even, Parity.Even },
            new[] { Parity.Odd, Parity.Even, Parity.Even, Parity.Even, Parity.Odd, Parity.Odd }) },
        { '7', new ParityPatterns(
            new[] { Parity.Even, Parity.Odd, Parity.Even, Parity.Odd, Parity.Even, Parity.Odd },
            new[] { Parity.Odd, Parity.Even, Parity.Odd, Parity.Even, Parity.Odd, Parity.Even }) },
        { '8', new ParityPatterns(
            new[] { Parity.Even, Parity.Odd, Parity.Even, Parity.Odd, Parity.Odd, Parity.Even },
            new[] { Parity.Odd, Parity.Even, Parity.Odd, Parity.Even, Parity.Even, Parity.Odd }) },
        { '9', new ParityPatterns(
            new[] { Parity.Even, Parity.Odd, Parity.Odd, Parity.Even, Parity.Odd, Parity.Even },
            new[] { Parity.Odd, Parity.Even, Parity.Even, Parity.Odd, Parity.Even, Parity.Odd }) }
    };
}

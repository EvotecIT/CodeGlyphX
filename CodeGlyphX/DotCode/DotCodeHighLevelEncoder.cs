// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;

namespace CodeGlyphX.DotCode;

internal static class DotCodeHighLevelEncoder {
    internal sealed class Result {
        internal List<int> Codewords { get; }
        internal bool BinaryFinish { get; }
        internal char FinalMode { get; }
        internal Result(List<int> codewords, bool binaryFinish, char finalMode) { Codewords = codewords; BinaryFinish = binaryFinish; FinalMode = finalMode; }
    }

    private sealed class BinaryBuffer {
        private ulong _value;
        private int _size;
        internal bool HasValues => _size != 0;

        internal void Append(List<int> output, int value) {
            _value = _value * 259 + (uint)value;
            _size++;
            if (_size == 5) Empty(output);
        }

        internal void Empty(List<int> output) {
            if (_size == 0) return;
            var digits = new int[_size + 1];
            for (var i = 0; i < digits.Length; i++) { digits[i] = (int)(_value % 103); _value /= 103; }
            for (var i = digits.Length - 1; i >= 0; i--) output.Add(digits[i]);
            _value = 0;
            _size = 0;
        }
    }

    internal static Result Encode(byte[] source, DotCodeEncodingOptions options, int? eci) {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (source.Length == 0) throw new ArgumentException("DotCode requires a non-empty payload.", nameof(source));

        var output = new List<int>(source.Length * 2 + 8);
        var mode = 'C';
        var insideMacro = 0;
        var position = 0;
        var binary = new BinaryBuffer();

        if (options.ReaderInitialization) {
            output.Add(109);
        } else if (!options.IsGs1 && !eci.HasValue && IsTwoDigits(source, 0)) {
            output.Add(107);
        } else if (IsLeadSpecial(source[0])) {
            output.Add(101);
            output.Add(source[0] + 64);
            mode = 'A';
            position++;
        } else if (source.Length > 5 && source[0] == '[' && source[1] == ')' && source[2] == '>' && source[3] == 30 && source[source.Length - 1] == 4) {
            var special = source[4] == '0' && (source[5] == '5' || source[5] == '6') || source[4] == '1' && source[5] == '2';
            var rsEot = source.Length > 1 && source[source.Length - 2] == 30;
            if (source.Length > 6 && special && source[6] == 29 && rsEot) insideMacro = source[5] == '5' ? 97 : source[5] == '6' ? 98 : 99;
            else if (!special && IsTwoDigits(source, 4)) insideMacro = 100;
            if (insideMacro != 0) {
                output.Add(106);
                output.Add(insideMacro);
                mode = 'B';
                if (insideMacro == 100) { output.Add(source[4] - '0' + 16); output.Add(source[5] - '0' + 16); position = 6; }
                else position = 7;
            }
        }

        AppendEci(output, binary, mode, eci);
        while (position < source.Length) {
            if (position == source.Length - 2 && insideMacro is 97 or 98 or 99) { position += 2; continue; }
            if (position == source.Length - 1 && insideMacro == 100) { position++; continue; }

            if (mode == 'C') {
                if (SeventeenTen(source, position)) {
                    output.Add(100); output.Add(ToPair(source, position + 2)); output.Add(ToPair(source, position + 4)); output.Add(ToPair(source, position + 6));
                    position += 10; continue;
                }
                if (IsTwoDigits(source, position) || options.IsGs1 && source[position] == 29) {
                    if (source[position] == 29) { output.Add(107); position++; }
                    else { output.Add(ToPair(source, position)); position += 2; }
                    continue;
                }
                if (IsBinary(source, position)) {
                    if (position + 1 < source.Length && IsDigit(source[position + 1])) {
                        AddUpperShift(output, source[position]); position++;
                    } else { output.Add(112); mode = 'X'; }
                    continue;
                }

                var a = AheadA(source, position);
                var bChars = AheadB(source, position, out var bWords);
                if (a > bChars) { output.Add(101); mode = 'A'; }
                else if (bWords is >= 1 and <= 4) {
                    output.Add(101 + bWords);
                    AppendBCharacters(output, source, ref position, bWords);
                } else { output.Add(106); mode = 'B'; }
                continue;
            }

            if (mode == 'B') {
                var digitPairs = TryC(source, position);
                if (digitPairs >= 2) {
                    if (digitPairs <= 4) { output.Add(103 + digitPairs - 2); AppendPairs(output, source, ref position, digitPairs); }
                    else { output.Add(106); mode = 'C'; }
                    continue;
                }
                if (options.IsGs1 && source[position] == 29) { output.Add(107); position++; continue; }
                if (DatumB(source, position) != 0 && AppendBCharacter(output, source, ref position, position != 0)) continue;
                if (IsBinary(source, position)) {
                    if (DatumB(source, position + 1) != 0) { AddUpperShift(output, source[position]); position++; }
                    else { output.Add(112); mode = 'X'; }
                    continue;
                }
                if (AheadA(source, position) == 1) { output.Add(101); AppendACharacter(output, source[position]); position++; }
                else { output.Add(102); mode = 'A'; }
                continue;
            }

            if (mode == 'A') {
                var digitPairs = TryC(source, position);
                if (digitPairs >= 2) {
                    if (digitPairs <= 4) { output.Add(103 + digitPairs - 2); AppendPairs(output, source, ref position, digitPairs); }
                    else { output.Add(106); mode = 'C'; }
                    continue;
                }
                if (options.IsGs1 && source[position] == 29) { output.Add(107); position++; continue; }
                if (DatumA(source, position)) { AppendACharacter(output, source[position]); position++; continue; }
                if (IsBinary(source, position)) {
                    if (DatumA(source, position + 1)) { AddUpperShift(output, source[position]); position++; }
                    else { output.Add(112); mode = 'X'; }
                    continue;
                }
                AheadB(source, position, out var bWords);
                if (bWords is >= 1 and <= 6) { output.Add(95 + bWords); AppendBCharacters(output, source, ref position, bWords); }
                else { output.Add(102); mode = 'B'; }
                continue;
            }

            var pairs = TryC(source, position);
            if (pairs >= 2) {
                binary.Empty(output);
                if (pairs <= 7) { output.Add(101 + pairs); AppendPairs(output, source, ref position, pairs); }
                else { output.Add(111); mode = 'C'; }
                continue;
            }
            if (IsBinary(source, position) || IsBinary(source, position + 1) || IsBinary(source, position + 2) || IsBinary(source, position + 3)) {
                binary.Append(output, source[position++]);
                continue;
            }
            binary.Empty(output);
            if (AheadA(source, position) > AheadB(source, position, out _)) { output.Add(109); mode = 'A'; }
            else { output.Add(110); mode = 'B'; }
        }

        if (mode == 'X' && binary.HasValues) binary.Empty(output);
        return new Result(output, mode == 'X', mode);
    }

    private static void AppendEci(List<int> output, BinaryBuffer binary, char mode, int? eci) {
        if (!eci.HasValue) return;
        var assignment = eci.Value;
        if (mode == 'X') {
            if (assignment <= 0xFF) { binary.Append(output, 256); binary.Append(output, assignment); }
            else if (assignment <= 0xFFFF) { binary.Append(output, 257); binary.Append(output, assignment >> 8); binary.Append(output, assignment & 0xFF); }
            else { binary.Append(output, 258); binary.Append(output, assignment >> 16); binary.Append(output, (assignment >> 8) & 0xFF); binary.Append(output, assignment & 0xFF); }
            return;
        }
        output.Add(108);
        if (assignment <= 39) output.Add(assignment);
        else {
            var value = assignment - 40;
            output.Add(value / 12769 + 40);
            output.Add(value % 12769 / 113);
            output.Add(value % 113);
        }
    }

    private static void AddUpperShift(List<int> output, byte value) {
        var shifted = value - 128;
        if (shifted < 32) { output.Add(110); output.Add(shifted + 64); }
        else { output.Add(111); output.Add(shifted - 32); }
    }

    private static void AppendACharacter(List<int> output, byte value) => output.Add(value < 32 ? value + 64 : value - 32);

    private static void AppendBCharacters(List<int> output, byte[] source, ref int position, int count) {
        for (var i = 0; i < count; i++) AppendBCharacter(output, source, ref position, allowSpecial: true);
    }

    private static bool AppendBCharacter(List<int> output, byte[] source, ref int position, bool allowSpecial) {
        var value = source[position];
        if (value is >= 32 and <= 127) output.Add(value - 32);
        else if (value == 13 && position + 1 < source.Length && source[position + 1] == 10) { output.Add(96); position++; }
        else if (allowSpecial) {
            if (value == 9) output.Add(97);
            else if (value == 28) output.Add(98);
            else if (value == 29) output.Add(99);
            else if (value == 30) output.Add(100);
            else return false;
        } else return false;
        position++;
        return true;
    }

    private static void AppendPairs(List<int> output, byte[] source, ref int position, int count) {
        for (var i = 0; i < count; i++) { output.Add(ToPair(source, position)); position += 2; }
    }

    private static int AheadA(byte[] source, int position) {
        var count = 0;
        while (position < source.Length && DatumA(source, position) && TryC(source, position) < 2) { count++; position++; }
        return count;
    }

    private static int AheadB(byte[] source, int position, out int codewordCount) {
        var start = position;
        codewordCount = 0;
        while (position < source.Length) {
            var advance = DatumB(source, position);
            if (advance == 0 || TryC(source, position) >= 2) break;
            codewordCount++;
            position += advance;
        }
        return position - start;
    }

    private static int TryC(byte[] source, int position) {
        if (position >= source.Length || !IsDigit(source[position])) return 0;
        var current = AheadC(source, position);
        return current > AheadC(source, position + 1) ? current : 0;
    }

    private static int AheadC(byte[] source, int position) {
        var count = 0;
        while (IsTwoDigits(source, position)) { count++; position += 2; }
        return count;
    }

    private static bool SeventeenTen(byte[] source, int position) => position + 9 < source.Length &&
        source[position] == '1' && source[position + 1] == '7' && source[position + 8] == '1' && source[position + 9] == '0' &&
        IsDigit(source[position + 2]) && IsDigit(source[position + 3]) && IsDigit(source[position + 4]) &&
        IsDigit(source[position + 5]) && IsDigit(source[position + 6]) && IsDigit(source[position + 7]);

    private static bool DatumA(byte[] source, int position) => position < source.Length && source[position] <= 95;

    private static int DatumB(byte[] source, int position) {
        if (position >= source.Length) return 0;
        var value = source[position];
        if (value is >= 32 and <= 127 || value is 9 or 28 or 29 or 30) return 1;
        return value == 13 && position + 1 < source.Length && source[position + 1] == 10 ? 2 : 0;
    }

    private static bool IsBinary(byte[] source, int position) => position < source.Length && source[position] >= 128;
    private static bool IsTwoDigits(byte[] source, int position) => position + 1 < source.Length && IsDigit(source[position]) && IsDigit(source[position + 1]);
    private static bool IsDigit(byte value) => value is >= (byte)'0' and <= (byte)'9';
    private static int ToPair(byte[] source, int position) => (source[position] - '0') * 10 + source[position + 1] - '0';
    private static bool IsLeadSpecial(byte value) => value is 9 or 28 or 29 or 30;
}

// Portions adapted from the Zint backend and ZXing-C++.
// Licensed under BSD-3-Clause and Apache-2.0; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.MaxiCode;

internal static class MaxiCodeHighLevelDecoder {
    internal sealed class Result {
        internal string Text { get; }
        internal byte[] Bytes { get; }
        internal int[] EciAssignments { get; }
        internal int? StructuredAppendIndex { get; }
        internal int? StructuredAppendCount { get; }

        internal Result(string text, byte[] bytes, int[] eciAssignments, int? index, int? count) {
            Text = text;
            Bytes = bytes;
            EciAssignments = eciAssignments;
            StructuredAppendIndex = index;
            StructuredAppendCount = count;
        }
    }

    internal static bool TryDecode(byte[] symbols, out Result result) {
        result = null!;
        var bytes = new List<byte>(symbols.Length);
        var chunk = new List<byte>(symbols.Length);
        var text = new StringBuilder();
        var eciAssignments = new List<int>();
        var encoding = EncodingUtils.Latin1;
        int? structuredIndex = null;
        int? structuredCount = null;
        var state = 0;
        var previousState = 0;
        var shift = -1;
        var index = 0;

        if (symbols.Length >= 2 && symbols[0] == 33) {
            var append = symbols[1];
            var count = (append & 0x07) + 1;
            var item = ((append >> 3) & 0x07) + 1;
            if (count is >= 2 and <= 8 && item <= count) {
                structuredIndex = item;
                structuredCount = count;
                index = 2;
            }
        }

        while (index < symbols.Length) {
            var symbol = symbols[index];
            if ((state is 0 or 1 && symbol == 33) || (state == 2 && symbol == 28)) break;

            if (symbol == 27) {
                FlushChunk(chunk, encoding, text);
                if (!TryReadEci(symbols, ref index, out var assignment)) return false;
                eciAssignments.Add(assignment);
                if (TryGetEncoding(assignment, out var mapped)) encoding = mapped;
            } else if (symbol == 31) {
                if (index + 5 >= symbols.Length) return false;
                var value = (symbols[index + 1] << 24) | (symbols[index + 2] << 18) |
                            (symbols[index + 3] << 12) | (symbols[index + 4] << 6) | symbols[index + 5];
                var digits = value.ToString("D9");
                for (var i = 0; i < digits.Length; i++) AppendByte((byte)digits[i], bytes, chunk);
                index += 5;
            } else if (state == 1 && symbol is 56 or 57) {
                previousState = state;
                state = 0;
                shift = symbol == 56 ? 2 : 3;
            } else if ((state == 1 && symbol == 63) || (state >= 2 && symbol == 58)) {
                state = 0;
                shift = -1;
            } else if ((state == 0 || state >= 2) && symbol == 63) {
                state = 1;
                shift = -1;
            } else if (IsLock(state, symbol)) {
                previousState = state;
                shift = -1;
            } else {
                var shiftTarget = MaxiCodeTables.GetShiftTarget(state, symbol);
                if (shiftTarget >= 0) {
                    previousState = state;
                    state = shiftTarget;
                    shift = 1;
                } else {
                    if (!MaxiCodeTables.TryDecodeSymbol(state, symbol, out var value)) return false;
                    AppendByte(value, bytes, chunk);
                }
            }

            if (shift == 0) {
                state = previousState;
                shift = -1;
            } else if (shift > 0) {
                shift--;
            }
            index++;
        }

        FlushChunk(chunk, encoding, text);
        result = new Result(text.ToString(), bytes.ToArray(), eciAssignments.ToArray(), structuredIndex, structuredCount);
        return true;
    }

    private static void AppendByte(byte value, List<byte> bytes, List<byte> chunk) {
        bytes.Add(value);
        chunk.Add(value);
    }

    private static void FlushChunk(List<byte> chunk, Encoding encoding, StringBuilder text) {
        if (chunk.Count == 0) return;
        text.Append(encoding.GetString(chunk.ToArray()));
        chunk.Clear();
    }

    private static bool TryReadEci(byte[] symbols, ref int index, out int assignment) {
        assignment = 0;
        if (++index >= symbols.Length) return false;
        var first = symbols[index];
        if ((first & 0x20) == 0) { assignment = first; return true; }
        if (++index >= symbols.Length) return false;
        var second = symbols[index];
        if ((first & 0x10) == 0) { assignment = ((first & 0x0F) << 6) | second; return true; }
        if (++index >= symbols.Length) return false;
        var third = symbols[index];
        if ((first & 0x08) == 0) { assignment = ((first & 0x07) << 12) | (second << 6) | third; return true; }
        if (++index >= symbols.Length) return false;
        var fourth = symbols[index];
        assignment = ((first & 0x03) << 18) | (second << 12) | (third << 6) | fourth;
        return assignment <= 999999;
    }

    private static bool TryGetEncoding(int assignment, out Encoding encoding) {
        switch (assignment) {
            case 3: encoding = EncodingUtils.Latin1; return true;
            case 25: encoding = Encoding.BigEndianUnicode; return true;
            case 26: encoding = EncodingUtils.Utf8Strict; return true;
            case 27: encoding = Encoding.ASCII; return true;
            default: encoding = EncodingUtils.Latin1; return false;
        }
    }

    private static bool IsLock(int state, int symbol) =>
        (state == 2 && symbol == 62) || (state == 3 && symbol == 60) || (state == 4 && symbol == 61);
}

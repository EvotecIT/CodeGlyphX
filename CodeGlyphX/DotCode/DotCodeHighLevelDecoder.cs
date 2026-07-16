// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.DotCode;

internal static class DotCodeHighLevelDecoder {
    internal sealed class Result {
        internal string Text { get; }
        internal byte[] Bytes { get; }
        internal bool HasFnc1 { get; }
        internal bool ReaderInitialization { get; }
        internal int[] EciAssignments { get; }
        internal int? StructuredAppendIndex { get; }
        internal int? StructuredAppendCount { get; }

        internal Result(string text, byte[] bytes, bool hasFnc1, bool readerInitialization, int[] eciAssignments,
            int? structuredAppendIndex, int? structuredAppendCount) {
            Text = text; Bytes = bytes; HasFnc1 = hasFnc1; ReaderInitialization = readerInitialization;
            EciAssignments = eciAssignments; StructuredAppendIndex = structuredAppendIndex; StructuredAppendCount = structuredAppendCount;
        }
    }

    private sealed class PayloadBuilder {
        private readonly List<byte> _bytes = new();
        private readonly List<byte> _pending = new();
        private readonly StringBuilder _text = new();
        private Encoding _encoding = EncodingUtils.Latin1;

        internal byte[] Bytes => _bytes.ToArray();
        internal string Text { get { Flush(); return _text.ToString(); } }

        internal void Add(byte value) { _bytes.Add(value); _pending.Add(value); }
        internal void Add(params byte[] values) { foreach (var value in values) Add(value); }
        internal void ChangeEncoding(int eci) { Flush(); _encoding = MapEncoding(eci) ?? _encoding; }
        internal void RemoveLast(int count) {
            if (_pending.Count < count || _bytes.Count < count) throw new FormatException("Invalid DotCode structured-append trailer.");
            _pending.RemoveRange(_pending.Count - count, count);
            _bytes.RemoveRange(_bytes.Count - count, count);
        }

        private void Flush() {
            if (_pending.Count == 0) return;
            _text.Append(_encoding.GetString(_pending.ToArray()));
            _pending.Clear();
        }

        private static Encoding? MapEncoding(int eci) {
            return EncodingUtils.TryGetEncoding(eci, out var encoding) ? encoding : null;
        }
    }

    internal static bool TryDecode(int[] words, out Result result) {
        result = null!;
        try {
            var payload = new PayloadBuilder();
            var eciAssignments = new List<int>();
            var mode = 'C';
            var index = 0;
            var hasFnc1 = false;
            var readerInitialization = false;
            int? structuredIndex = null;
            int? structuredCount = null;
            var macro = 0;

            var parseLength = words.Length;
            if (parseLength >= 3 && words[parseLength - 1] == 108) {
                var candidateIndex = StructuredCodewordValue(words[parseLength - 3]);
                var candidateCount = StructuredCodewordValue(words[parseLength - 2]);
                if (candidateIndex >= 1 && candidateCount is >= 2 and <= 35 && candidateIndex <= candidateCount) {
                    structuredIndex = candidateIndex;
                    structuredCount = candidateCount;
                    parseLength -= 3;
                    if (parseLength > 0 && words[parseLength - 1] is 101 or 106 or 109) parseLength--;
                    var payloadWords = new int[parseLength];
                    Array.Copy(words, payloadWords, parseLength);
                    words = payloadWords;
                }
            }

            if (index < words.Length && words[index] == 109) { readerInitialization = true; index++; }
            if (index < words.Length && words[index] == 107) index++; // Numeric-start control.
            if (index + 1 < words.Length && words[index] == 106 && words[index + 1] is >= 97 and <= 100) {
                mode = 'B';
                macro = words[index + 1];
                index += 2;
                payload.Add((byte)'[', (byte)')', (byte)'>', 30);
                if (macro == 100) {
                    if (index + 1 >= words.Length) return false;
                    payload.Add((byte)('0' + words[index++] - 16), (byte)('0' + words[index++] - 16));
                } else {
                    if (macro == 97) payload.Add((byte)'0', (byte)'5', 29);
                    else if (macro == 98) payload.Add((byte)'0', (byte)'6', 29);
                    else payload.Add((byte)'1', (byte)'2', 29);
                }
            }

            while (index < words.Length) {
                var word = words[index++];
                if (mode == 'X') {
                    index--;
                    if (!DecodeBinary(words, ref index, payload, eciAssignments, ref mode)) return false;
                    continue;
                }

                var directLimit = mode == 'C' ? 99 : 95;
                if (word <= directLimit) {
                    if (mode == 'C') AddPair(payload, word);
                    else if (mode == 'A') payload.Add(DecodeA(word));
                    else payload.Add((byte)(word + 32));
                    continue;
                }

                if (mode == 'C' && word == 100) {
                    if (index + 2 >= words.Length || words[index] > 99 || words[index + 1] > 99 || words[index + 2] > 99) return false;
                    payload.Add((byte)'1', (byte)'7'); AddPair(payload, words[index++]); AddPair(payload, words[index++]); AddPair(payload, words[index++]); payload.Add((byte)'1', (byte)'0');
                    continue;
                }
                if (mode == 'B' && word is >= 96 and <= 100) {
                    if (word == 96) payload.Add(13, 10);
                    else payload.Add((byte)(word == 97 ? 9 : word == 98 ? 28 : word == 99 ? 29 : 30));
                    continue;
                }

                if (word == 107) { payload.Add(29); hasFnc1 = true; continue; }
                if (word == 108) {
                    if (!ReadEci(words, ref index, out var eci)) return false;
                    eciAssignments.Add(eci); payload.ChangeEncoding(eci);
                    continue;
                }
                if (word == 109) { readerInitialization = true; continue; }
                if (word is 110 or 111) {
                    if (index >= words.Length) return false;
                    var shifted = words[index++];
                    payload.Add((byte)((word == 110 ? DecodeA(shifted) : DecodeB(shifted)) + 128));
                    continue;
                }
                if (word == 112) { mode = 'X'; continue; }

                if (mode == 'C') {
                    if (word == 101) mode = 'A';
                    else if (word is >= 102 and <= 105) { if (!DecodeBShift(words, ref index, word - 101, payload)) return false; }
                    else if (word == 106) mode = 'B';
                    else return false;
                } else if (mode == 'A') {
                    if (word is >= 96 and <= 101) { if (!DecodeBShift(words, ref index, word - 95, payload)) return false; }
                    else if (word == 102) mode = 'B';
                    else if (word is >= 103 and <= 105) { if (!DecodeCShift(words, ref index, word - 101, payload)) return false; }
                    else if (word == 106) mode = 'C';
                    else return false;
                } else {
                    if (word == 101) { if (index >= words.Length) return false; payload.Add(DecodeA(words[index++])); }
                    else if (word == 102) mode = 'A';
                    else if (word is >= 103 and <= 105) { if (!DecodeCShift(words, ref index, word - 101, payload)) return false; }
                    else if (word == 106) mode = 'C';
                    else return false;
                }
            }

            if (macro is >= 97 and <= 99) payload.Add(30, 4);
            else if (macro == 100) payload.Add(4);
            result = new Result(payload.Text, payload.Bytes, hasFnc1, readerInitialization, eciAssignments.ToArray(), structuredIndex, structuredCount);
            return true;
        } catch (ArgumentException) { return false; }
          catch (FormatException) { return false; }
          catch (OverflowException) { return false; }
    }

    private static bool DecodeBinary(int[] words, ref int index, PayloadBuilder payload, List<int> eciAssignments, ref char mode) {
        var start = index;
        while (index < words.Length && words[index] <= 102) index++;
        var count = index - start;
        var fullGroups = count / 6;
        var remainder = count % 6;
        if (remainder == 1) return false;
        var values = new List<int>(fullGroups * 5 + Math.Max(0, remainder - 1));
        for (var group = 0; group < fullGroups; group++) DecodeRadix(words, start + group * 6, 6, 5, values);
        if (remainder > 0) DecodeRadix(words, start + fullGroups * 6, remainder, remainder - 1, values);
        for (var i = 0; i < values.Count; i++) {
            var value = values[i];
            if (value <= 255) payload.Add((byte)value);
            else {
                var bytes = value - 255;
                if (bytes is < 1 or > 3 || i + bytes >= values.Count) return false;
                var eci = 0;
                for (var j = 0; j < bytes; j++) { var part = values[++i]; if (part > 255) return false; eci = eci << 8 | part; }
                eciAssignments.Add(eci); payload.ChangeEncoding(eci);
            }
        }
        if (index >= words.Length) return true;
        var control = words[index++];
        if (control is >= 103 and <= 108) return DecodeCShift(words, ref index, control - 101, payload);
        if (control == 109) { mode = 'A'; return true; }
        if (control == 110) { mode = 'B'; return true; }
        if (control == 111) { mode = 'C'; return true; }
        return false;
    }

    private static void DecodeRadix(int[] words, int start, int digitCount, int valueCount, List<int> output) {
        ulong value = 0;
        for (var i = 0; i < digitCount; i++) value = value * 103 + (uint)words[start + i];
        var values = new int[valueCount];
        for (var i = valueCount - 1; i >= 0; i--) { values[i] = (int)(value % 259); value /= 259; }
        if (value != 0) throw new FormatException("Invalid DotCode binary radix value.");
        output.AddRange(values);
    }

    private static bool DecodeBShift(int[] words, ref int index, int count, PayloadBuilder payload) {
        for (var i = 0; i < count; i++) {
            if (index >= words.Length || words[index] > 100) return false;
            var word = words[index++];
            if (word <= 95) payload.Add((byte)(word + 32));
            else if (word == 96) payload.Add(13, 10);
            else payload.Add((byte)(word == 97 ? 9 : word == 98 ? 28 : word == 99 ? 29 : 30));
        }
        return true;
    }

    private static bool DecodeCShift(int[] words, ref int index, int count, PayloadBuilder payload) {
        for (var i = 0; i < count; i++) { if (index >= words.Length || words[index] > 99) return false; AddPair(payload, words[index++]); }
        return true;
    }

    private static bool ReadEci(int[] words, ref int index, out int eci) {
        eci = 0;
        if (index >= words.Length) return false;
        var first = words[index++];
        if (first <= 39) { eci = first; return true; }
        if (index + 1 >= words.Length || first > 103 || words[index] > 112 || words[index + 1] > 112) return false;
        eci = (first - 40) * 12769 + words[index++] * 113 + words[index++] + 40;
        return eci <= 811799;
    }

    private static void AddPair(PayloadBuilder payload, int word) { payload.Add((byte)('0' + word / 10), (byte)('0' + word % 10)); }
    private static byte DecodeA(int word) => word <= 63 ? (byte)(word + 32) : (byte)(word - 64);
    private static byte DecodeB(int word) => word <= 95 ? (byte)(word + 32) : throw new FormatException("Invalid DotCode Code Set B value.");
    private static int StructuredCodewordValue(int value) => value is >= 17 and <= 25 ? value - 16 : value is >= 33 and <= 58 ? value - 23 : -1;
}

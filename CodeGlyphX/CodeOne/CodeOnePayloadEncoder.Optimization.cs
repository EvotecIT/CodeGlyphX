using System;
using System.Collections.Generic;

namespace CodeGlyphX.CodeOne;

internal static partial class CodeOnePayloadEncoder {
    private enum Mode { Ascii, Pair, C40, Text, Edi, Byte, Decimal }
    private readonly struct Step {
        internal readonly int Start, End;
        internal readonly Mode Mode;
        internal readonly bool Terminal;
        internal readonly int Length, Tail;
        internal Step(int start, int end, Mode mode, int length, bool terminal = false, int tail = 0) {
            Start = start; End = end; Mode = mode; Length = length; Terminal = terminal; Tail = tail;
        }
    }

    // Each edge starts and ends at an ASCII boundary. Compact modes share their
    // normative latch/unlatch rules; terminal edges may fill the final codeword.
    private static int[]? Optimize(byte[] source, bool gs1, int capacity) {
        if (capacity < 1) return null;
        var costs = new int[source.Length + 1];
        var previous = new Step[source.Length + 1];
        for (var i = 1; i < costs.Length; i++) costs[i] = int.MaxValue;
        void Offer(int start, int end, Mode mode, int length, bool terminal = false, int tail = 0) {
            var total = costs[start] + length;
            if (total > capacity || total >= costs[end]) return;
            costs[end] = total; previous[end] = new Step(start, end, mode, length, terminal, tail);
        }
        for (var start = 0; start < source.Length; start++) {
            if (costs[start] == int.MaxValue || costs[start] >= capacity) continue;
            Offer(start, start + 1, Mode.Ascii, source[start] >= 128 ? 2 : 1);
            if (Digit(source[start]) && start + 1 < source.Length && Digit(source[start + 1])) Offer(start, start + 2, Mode.Pair, 1);
            var c40 = 0; var text = 0; var edi = true; var numeric = true; var binary = true;
            for (var end = start + 1; end <= source.Length; end++) {
                var value = source[end - 1];
                var length = end - start;
                c40 += TripletValueCount(value, textMode: false, gs1);
                text += TripletValueCount(value, textMode: true, gs1);
                edi &= EdiValue(value) >= 0;
                numeric &= Digit(value);
                binary &= !gs1 || value != Gs1.GroupSeparator;
                if (binary) {
                    var size = length + (length <= 249 ? 2 : 3);
                    Offer(start, end, Mode.Byte, size);
                    if (end == source.Length && costs[start] + length + 2 == capacity) Offer(start, end, Mode.Byte, length + 2, terminal: true);
                }
                void Triplet(Mode mode, int values) {
                    if (values % 3 != 0) {
                        // Two C40/Text values may share the final two data codewords
                        // with one trailing shift value, without an ASCII unlatch.
                        if (mode != Mode.Edi && values % 3 == 2 && end == source.Length
                            && costs[start] + 1 + (values + 1) / 3 * 2 == capacity)
                            Offer(start, end, mode, 1 + (values + 1) / 3 * 2, terminal: true);
                        return;
                    }
                    var size = 1 + values / 3 * 2;
                    if (end == source.Length && costs[start] + size == capacity) Offer(start, end, mode, size, terminal: true);
                    else Offer(start, end, mode, size + 1);
                    if (costs[start] + size + 1 != capacity) return;
                    var tail = source.Length - end;
                    var single = tail == 1 && source[end] < 128 && (mode == Mode.Edi
                        || TripletValueCount(source[end], mode == Mode.Text, gs1) == 1);
                    if (single || mode == Mode.Edi && tail == 2 && Digit(source[end]) && Digit(source[end + 1]))
                        Offer(start, source.Length, mode, size + 1, terminal: true, tail);
                }
                Triplet(Mode.C40, c40); Triplet(Mode.Text, text);
                if (edi) Triplet(Mode.Edi, length);
                if (!numeric || length < 3) continue;
                var bits = 4 + length / 3 * 10;
                var remainder = length % 3;
                var sizeClosed = (bits + 6 + 7) / 8;
                var pad = sizeClosed * 8 - bits - 6;
                if (remainder == 0 || remainder == 1 && pad >= 4) Offer(start, end, Mode.Decimal, sizeClosed);
                var decimalTail = source.Length - end;
                if (remainder == 0 && bits % 8 == 0 && costs[start] + bits / 8 + 1 == capacity
                    && (decimalTail == 1 && source[end] < 128
                        || decimalTail == 2 && Digit(source[end]) && Digit(source[end + 1])))
                    Offer(start, source.Length, Mode.Decimal, bits / 8 + 1, terminal: true, tail: decimalTail);
                if (end != source.Length) continue;
                var remaining = (capacity - costs[start]) * 8 - bits;
                if (remainder == 0 && remaining is 0 or 2 or 4 or 6
                    || remainder == 1 && remaining is 4 or 6 || remainder == 2 && remaining == 8)
                    Offer(start, end, Mode.Decimal, capacity - costs[start], terminal: true);
            }
        }
        if (costs[source.Length] == int.MaxValue) return null;
        var steps = new List<Step>();
        for (var end = source.Length; end > 0; end = previous[end].Start) steps.Add(previous[end]);
        steps.Reverse();
        var output = new List<int>(costs[source.Length]);
        foreach (var step in steps) Emit(source, gs1, step, output);
        return output.ToArray();
    }

    private static void Emit(byte[] source, bool gs1, Step step, List<int> output) {
        var length = step.End - step.Start;
        if (step.Mode == Mode.Ascii) {
            var value = (int)source[step.Start];
            if (value >= 128) { output.Add(235); output.Add(value - 127); }
            else output.Add(AsciiCodeword((byte)value, gs1));
        } else if (step.Mode == Mode.Pair) output.Add(130 + (source[step.Start] - '0') * 10 + source[step.Start + 1] - '0');
        else if (step.Mode == Mode.Byte) {
            output.Add(231);
            if (step.Terminal) output.Add(0);
            else if (length <= 249) output.Add(length);
            else { output.Add(249 + length / 250); output.Add(length % 250); }
            for (var i = step.Start; i < step.End; i++) output.Add(source[i]);
        } else if (step.Mode == Mode.Decimal) EmitDecimal(source, gs1, step, output);
        else {
            output.Add(step.Mode == Mode.C40 ? 230 : step.Mode == Mode.Text ? 239 : 238);
            var values = new List<int>();
            for (var i = step.Start; i < step.End - step.Tail; i++) {
                if (step.Mode == Mode.Edi) values.Add(EdiValue(source[i]));
                else AppendTripletValues(source[i], step.Mode == Mode.Text, gs1, values);
            }
            if (values.Count % 3 == 2 && step.Terminal) values.Add(0);
            for (var i = 0; i < values.Count; i += 3) {
                var packed = values[i] * 1600 + values[i + 1] * 40 + values[i + 2] + 1;
                output.Add(packed >> 8); output.Add(packed & 255);
            }
            if (!step.Terminal) output.Add(255);
            if (step.Tail == 1) output.Add(AsciiCodeword(source[step.End - 1], gs1));
            else if (step.Tail == 2) output.Add(130 + (source[step.End - 2] - '0') * 10 + source[step.End - 1] - '0');
        }
    }

    private static void EmitDecimal(byte[] source, bool gs1, Step step, List<int> output) {
        var bits = new List<bool>(step.Length * 8);
        void Append(int value, int count) { for (var shift = count - 1; shift >= 0; shift--) bits.Add((value & (1 << shift)) != 0); }
        Append(15, 4);
        var i = step.Start;
        var compactEnd = step.End - step.Tail;
        for (; i + 2 < compactEnd; i += 3) Append(1 + (source[i] - '0') * 100 + (source[i + 1] - '0') * 10 + source[i + 2] - '0', 10);
        if (!step.Terminal) Append(63, 6);
        if (step.Tail == 1) Append(AsciiCodeword(source[i], gs1), 8);
        else if (step.Tail == 2) Append(130 + (source[i] - '0') * 10 + source[i + 1] - '0', 8);
        else if (step.End - i == 1) Append(source[i] - '0' + 1, 4);
        else if (step.End - i == 2) Append(130 + (source[i] - '0') * 10 + source[i + 1] - '0', 8);
        var padding = step.Length * 8 - bits.Count;
        if (padding >= 4) { Append(15, 4); padding -= 4; }
        if (padding == 2) Append(1, 2);
        if (bits.Count != step.Length * 8) throw new InvalidOperationException("Invalid Code One decimal plan.");
        for (i = 0; i < bits.Count; i += 8) {
            var value = 0;
            for (var j = 0; j < 8; j++) value = value * 2 + (bits[i + j] ? 1 : 0);
            output.Add(value);
        }
    }

    private static bool Digit(byte value) => value >= '0' && value <= '9';
    private static int AsciiCodeword(byte value, bool gs1) => gs1 && value == Gs1.GroupSeparator ? 232 : value + 1;
    private static int EdiValue(byte value) => value == '\r' ? 0 : value == '*' ? 1 : value == '>' ? 2 : value == ' ' ? 3
        : Digit(value) ? value - '0' + 4 : value >= 'A' && value <= 'Z' ? value - 'A' + 14 : -1;
    private static int TripletValueCount(byte value, bool textMode, bool gs1) {
        if (gs1 && value == Gs1.GroupSeparator) return 2;
        var upper = value >= 128 ? 2 : 0;
        if (upper != 0) value -= 128;
        return upper + (value == ' ' || Digit(value) || (textMode ? value >= 'a' && value <= 'z' : value >= 'A' && value <= 'Z') ? 1 : 2);
    }
    private static void AppendTripletValues(byte value, bool textMode, bool gs1, List<int> values) {
        if (gs1 && value == Gs1.GroupSeparator) { values.Add(1); values.Add(27); return; }
        if (value >= 128) { values.Add(1); values.Add(30); value -= 128; }
        if (value == ' ') values.Add(3);
        else if (Digit(value)) values.Add(value - '0' + 4);
        else if (textMode ? value >= 'a' && value <= 'z' : value >= 'A' && value <= 'Z') values.Add(value - (textMode ? 'a' : 'A') + 14);
        else if (value < 32) { values.Add(0); values.Add(value); }
        else {
            const string punctuation = "!\"#$%&'()*+,-./:;<=>?@[\\]^_";
            var index = punctuation.IndexOf((char)value);
            if (index >= 0) { values.Add(1); values.Add(index); }
            else { values.Add(2); values.Add(textMode && value >= 'A' && value <= 'Z' ? value - 'A' + 1 : value - 96); }
        }
    }
}

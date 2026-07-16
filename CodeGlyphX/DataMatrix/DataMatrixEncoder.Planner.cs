using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixEncoder {
    private enum PlannedEncodation : byte {
        Ascii,
        C40,
        Text,
        X12,
        Edifact,
        Base256
    }

    private readonly struct PlannedSegment {
        public int Start { get; }
        public int Length { get; }
        public PlannedEncodation Mode { get; }

        public PlannedSegment(int start, int length, PlannedEncodation mode) {
            Start = start;
            Length = length;
            Mode = mode;
        }
    }

    private static List<byte> EncodeOptimized(string text, bool isGs1, int positionOffset) {
        if (text.Length == 0) return new List<byte>();
        if (ContainsOnly(text, '0', '9')) return EncodeAscii(EncodingUtils.Latin1.GetBytes(text), isGs1);

        var costs = new int[text.Length + 1];
        var previous = new int[text.Length + 1];
        var modes = new PlannedEncodation[text.Length + 1];
        for (var i = 1; i < costs.Length; i++) costs[i] = int.MaxValue;
        var c40ValueCounts = BuildC40ValueCounts(text, isText: false, isGs1);
        var textValueCounts = BuildC40ValueCounts(text, isText: true, isGs1);
        var x12Characters = BuildX12Characters(text);
        var edifactCharacters = BuildEdifactCharacters(text);
        BuildUtf8CharacterWidths(text, out var utf8ByteCounts, out var utf16Widths);

        for (var start = 0; start < text.Length; start++) {
            if (costs[start] == int.MaxValue) continue;
            AddAsciiCandidates(text, start, costs, previous, modes);
            AddC40TextCandidates(c40ValueCounts, start, isText: false, costs, previous, modes);
            AddC40TextCandidates(textValueCounts, start, isText: true, costs, previous, modes);
            AddX12Candidates(x12Characters, start, costs, previous, modes);
            AddEdifactCandidates(edifactCharacters, start, costs, previous, modes);
            if (!isGs1) AddBase256Candidates(utf8ByteCounts, utf16Widths, start, costs, previous, modes);
        }

        if (costs[text.Length] == int.MaxValue) {
            throw new ArgumentException("Text cannot be represented by the selected Data Matrix encoding options.", nameof(text));
        }

        var segments = BuildPlan(text.Length, previous, modes);
        var output = new List<byte>(costs[text.Length]);
        for (var i = 0; i < segments.Count; i++) {
            var segment = segments[i];
            var value = text.Substring(segment.Start, segment.Length);
            switch (segment.Mode) {
                case PlannedEncodation.Ascii:
                    output.AddRange(EncodeAscii(EncodingUtils.Latin1.GetBytes(value), isGs1));
                    break;
                case PlannedEncodation.C40:
                    output.AddRange(EncodeC40Text(value, isText: false, isGs1));
                    break;
                case PlannedEncodation.Text:
                    output.AddRange(EncodeC40Text(value, isText: true, isGs1));
                    break;
                case PlannedEncodation.X12:
                    output.AddRange(EncodeX12(value, isGs1));
                    break;
                case PlannedEncodation.Edifact:
                    output.AddRange(EncodeEdifact(value));
                    break;
                case PlannedEncodation.Base256:
                    output.AddRange(EncodeBase256(Encoding.UTF8.GetBytes(value), positionOffset + output.Count));
                    break;
                default:
                    throw new InvalidOperationException("Unsupported planned Data Matrix encodation.");
            }
        }
        return output;
    }

    private static bool ContainsOnly(string text, char minimum, char maximum) {
        for (var i = 0; i < text.Length; i++) {
            if (text[i] < minimum || text[i] > maximum) return false;
        }
        return true;
    }

    private static void AddAsciiCandidates(
        string text,
        int start,
        int[] costs,
        int[] previous,
        PlannedEncodation[] modes) {
        var value = text[start];
        if (value > 255) return;
        Relax(start, start + 1, value <= 127 ? 1 : 2, PlannedEncodation.Ascii, costs, previous, modes);
        if (start + 1 < text.Length && value is >= '0' and <= '9' && text[start + 1] is >= '0' and <= '9') {
            Relax(start, start + 2, 1, PlannedEncodation.Ascii, costs, previous, modes);
        }
    }

    private static void AddC40TextCandidates(
        int[] valueCounts,
        int start,
        bool isText,
        int[] costs,
        int[] previous,
        PlannedEncodation[] modes) {
        var valueCount = 0;
        for (var end = start; end < valueCounts.Length; end++) {
            if (valueCounts[end] < 0) break;
            valueCount += valueCounts[end];
            if (valueCount % 3 != 0) continue;
            var codewordCount = 2 + valueCount / 3 * 2; // latch + packed triplets + unlatch
            Relax(
                start,
                end + 1,
                codewordCount,
                isText ? PlannedEncodation.Text : PlannedEncodation.C40,
                costs,
                previous,
                modes);
        }
    }

    private static void AddX12Candidates(
        bool[] validCharacters,
        int start,
        int[] costs,
        int[] previous,
        PlannedEncodation[] modes) {
        for (var end = start; end < validCharacters.Length; end++) {
            if (!validCharacters[end]) break;
            var length = end - start + 1;
            if (length % 3 != 0) continue;
            Relax(start, end + 1, 2 + length / 3 * 2, PlannedEncodation.X12, costs, previous, modes);
        }
    }

    private static void AddEdifactCandidates(
        bool[] validCharacters,
        int start,
        int[] costs,
        int[] previous,
        PlannedEncodation[] modes) {
        for (var end = start; end < validCharacters.Length; end++) {
            if (!validCharacters[end]) break;
            var length = end - start + 1;
            var packedGroups = (length + 4) / 4; // data plus unlatch value, rounded to a group of four
            Relax(start, end + 1, 1 + packedGroups * 3, PlannedEncodation.Edifact, costs, previous, modes);
        }
    }

    private static void AddBase256Candidates(
        int[] utf8ByteCounts,
        int[] utf16Widths,
        int start,
        int[] costs,
        int[] previous,
        PlannedEncodation[] modes) {
        var byteCount = 0;
        var end = start;
        while (end < utf8ByteCounts.Length && utf16Widths[end] > 0) {
            byteCount += utf8ByteCounts[end];
            end += utf16Widths[end];
            var lengthCodewords = byteCount <= 249 ? 1 : 2;
            Relax(start, end, 1 + lengthCodewords + byteCount, PlannedEncodation.Base256, costs, previous, modes);
        }
    }

    private static int[] BuildC40ValueCounts(string text, bool isText, bool isGs1) {
        var counts = new int[text.Length];
        var values = new List<int>(4);
        for (var i = 0; i < text.Length; i++) {
            values.Clear();
            counts[i] = TryEncodeC40Char(text[i], isText, isGs1, values) ? values.Count : -1;
        }
        return counts;
    }

    private static bool[] BuildX12Characters(string text) {
        var characters = new bool[text.Length];
        for (var i = 0; i < text.Length; i++) characters[i] = TryEncodeX12Char(text[i], out _);
        return characters;
    }

    private static bool[] BuildEdifactCharacters(string text) {
        var characters = new bool[text.Length];
        for (var i = 0; i < text.Length; i++) characters[i] = text[i] is >= ' ' and <= '^';
        return characters;
    }

    private static void BuildUtf8CharacterWidths(string text, out int[] byteCounts, out int[] utf16Widths) {
        byteCounts = new int[text.Length];
        utf16Widths = new int[text.Length];
        for (var i = 0; i < text.Length; i++) {
            var value = text[i];
            if (char.IsHighSurrogate(value) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) {
                byteCounts[i] = 4;
                utf16Widths[i] = 2;
                i++;
                continue;
            }
            byteCounts[i] = value <= 0x7F ? 1 : value <= 0x7FF ? 2 : 3;
            utf16Widths[i] = 1;
        }
    }

    private static void Relax(
        int start,
        int end,
        int segmentCost,
        PlannedEncodation mode,
        int[] costs,
        int[] previous,
        PlannedEncodation[] modes) {
        if (costs[start] > int.MaxValue - segmentCost) return;
        var candidate = costs[start] + segmentCost;
        if (candidate >= costs[end]) return;
        costs[end] = candidate;
        previous[end] = start;
        modes[end] = mode;
    }

    private static List<PlannedSegment> BuildPlan(int length, int[] previous, PlannedEncodation[] modes) {
        var reverse = new List<PlannedSegment>();
        for (var end = length; end > 0;) {
            var start = previous[end];
            reverse.Add(new PlannedSegment(start, end - start, modes[end]));
            end = start;
        }
        reverse.Reverse();

        var result = new List<PlannedSegment>(reverse.Count);
        for (var i = 0; i < reverse.Count; i++) {
            var segment = reverse[i];
            if (segment.Mode == PlannedEncodation.Ascii && result.Count > 0) {
                var last = result[result.Count - 1];
                if (last.Mode == PlannedEncodation.Ascii && last.Start + last.Length == segment.Start) {
                    result[result.Count - 1] = new PlannedSegment(last.Start, last.Length + segment.Length, last.Mode);
                    continue;
                }
            }
            result.Add(segment);
        }
        return result;
    }
}

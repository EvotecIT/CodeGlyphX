using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal sealed class QrSegmentPlan {
    public QrSegment[] Segments { get; }
    public int TotalBitLength { get; }
    public bool HasByteSegment { get; }

    public QrSegmentPlan(QrSegment[] segments, int totalBitLength, bool hasByteSegment) {
        Segments = segments;
        TotalBitLength = totalBitLength;
        HasByteSegment = hasByteSegment;
    }
}

internal static class QrSegmentPlanner {
    private const int Infinite = int.MaxValue / 4;

    public static QrSegmentPlan Plan(string text, QrTextEncoding encoding, int version, QrFnc1Mode fnc1Mode, int? eciAssignmentNumber) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (!QrEncoding.CanEncode(text, encoding))
            throw new ArgumentException($"Text cannot be encoded as {encoding}.", nameof(text));

        if (text.Length == 0) {
            var empty = QrSegment.CreateByte(Array.Empty<byte>());
            var bits = empty.GetTotalBitLength(version);
            if (eciAssignmentNumber is not null) bits += 4 + GetEciAssignmentBitCount(eciAssignmentNumber.Value);
            return new QrSegmentPlan(new[] { empty }, bits, hasByteSegment: true);
        }

        if (!HasNonByteCandidate(text, fnc1Mode)) {
            return CreateBytePlan(QrEncoding.Encode(text, encoding), version, eciAssignmentNumber);
        }

        var boundaries = BuildTextBoundaries(text);
        var unitCount = boundaries.Length - 1;
        var bytePrefix = BuildBytePrefix(text, boundaries, encoding);
        var stateCount = (unitCount + 1) * 2;
        var costs = new int[stateCount];
        var segmentCounts = new int[stateCount];
        var previousPositions = new int[stateCount];
        var previousByteStates = new bool[stateCount];
        var previousModes = new QrSegmentMode[stateCount];

        for (var i = 0; i < stateCount; i++) {
            costs[i] = Infinite;
            segmentCounts[i] = Infinite;
            previousPositions[i] = -1;
        }
        costs[0] = 0;
        segmentCounts[0] = 0;

        for (var position = 0; position < unitCount; position++) {
            for (var byteState = 0; byteState <= 1; byteState++) {
                var sourceIndex = StateIndex(position, byteState != 0);
                if (costs[sourceIndex] >= Infinite) continue;

                AddNumericTransitions(text, boundaries, version, position, byteState != 0, sourceIndex, costs, segmentCounts, previousPositions, previousByteStates, previousModes);
                AddAlphanumericTransitions(text, boundaries, version, fnc1Mode, position, byteState != 0, sourceIndex, costs, segmentCounts, previousPositions, previousByteStates, previousModes);
                AddKanjiTransitions(text, boundaries, version, position, byteState != 0, sourceIndex, costs, segmentCounts, previousPositions, previousByteStates, previousModes);
                AddByteTransitions(bytePrefix, version, position, sourceIndex, costs, segmentCounts, previousPositions, previousByteStates, previousModes);
            }
        }

        var noByteIndex = StateIndex(unitCount, false);
        var byteIndex = StateIndex(unitCount, true);
        var eciBits = eciAssignmentNumber is null ? 0 : 4 + GetEciAssignmentBitCount(eciAssignmentNumber.Value);
        var noByteCost = costs[noByteIndex];
        var byteCost = costs[byteIndex] >= Infinite ? Infinite : costs[byteIndex] + eciBits;
        var useByte = byteCost < noByteCost
            || (byteCost == noByteCost && segmentCounts[byteIndex] < segmentCounts[noByteIndex]);
        var finalIndex = useByte ? byteIndex : noByteIndex;
        var totalCost = useByte ? byteCost : noByteCost;
        if (totalCost >= Infinite) throw new ArgumentException("Text cannot be represented by supported QR segments.", nameof(text));

        var segments = Reconstruct(text, boundaries, encoding, fnc1Mode, unitCount, useByte, previousPositions, previousByteStates, previousModes);
        return new QrSegmentPlan(segments, totalCost, useByte);
    }

    public static QrSegmentPlan CreateBytePlan(byte[] bytes, int version, int? eciAssignmentNumber) {
        var segment = QrSegment.CreateByte(bytes);
        var bits = segment.GetTotalBitLength(version);
        if (eciAssignmentNumber is not null) bits += 4 + GetEciAssignmentBitCount(eciAssignmentNumber.Value);
        return new QrSegmentPlan(new[] { segment }, bits, hasByteSegment: true);
    }

    public static int GetEciAssignmentBitCount(int assignmentNumber) {
        if (assignmentNumber <= 0x7F) return 8;
        if (assignmentNumber <= 0x3FFF) return 16;
        return 24;
    }

    private static void AddNumericTransitions(
        string text,
        int[] boundaries,
        int version,
        int start,
        bool byteUsed,
        int sourceIndex,
        int[] costs,
        int[] segmentCounts,
        int[] previousPositions,
        bool[] previousByteStates,
        QrSegmentMode[] previousModes) {
        var countBits = QrSegment.GetCharacterCountBitLength(QrSegmentMode.Numeric, version);
        var maxCount = (1 << countBits) - 1;
        var count = 0;
        for (var end = start + 1; end < boundaries.Length && count < maxCount; end++) {
            if (!IsSingleCharacter(text, boundaries, end - 1, out var c) || c is < '0' or > '9') break;
            count++;
            Relax(end, byteUsed, QrSegmentMode.Numeric, start, byteUsed,
                4 + countBits + QrSegment.GetDataBitLength(QrSegmentMode.Numeric, count), sourceIndex,
                costs, segmentCounts, previousPositions, previousByteStates, previousModes);
        }
    }

    private static void AddAlphanumericTransitions(
        string text,
        int[] boundaries,
        int version,
        QrFnc1Mode fnc1Mode,
        int start,
        bool byteUsed,
        int sourceIndex,
        int[] costs,
        int[] segmentCounts,
        int[] previousPositions,
        bool[] previousByteStates,
        QrSegmentMode[] previousModes) {
        var countBits = QrSegment.GetCharacterCountBitLength(QrSegmentMode.Alphanumeric, version);
        var maxCount = (1 << countBits) - 1;
        var encodedCount = 0;
        for (var end = start + 1; end < boundaries.Length; end++) {
            if (!IsSingleCharacter(text, boundaries, end - 1, out var c)) break;
            var added = GetAlphanumericEncodedLength(c, fnc1Mode);
            if (added == 0 || encodedCount + added > maxCount) break;
            encodedCount += added;
            Relax(end, byteUsed, QrSegmentMode.Alphanumeric, start, byteUsed,
                4 + countBits + QrSegment.GetDataBitLength(QrSegmentMode.Alphanumeric, encodedCount), sourceIndex,
                costs, segmentCounts, previousPositions, previousByteStates, previousModes);
        }
    }

    private static void AddKanjiTransitions(
        string text,
        int[] boundaries,
        int version,
        int start,
        bool byteUsed,
        int sourceIndex,
        int[] costs,
        int[] segmentCounts,
        int[] previousPositions,
        bool[] previousByteStates,
        QrSegmentMode[] previousModes) {
        var countBits = QrSegment.GetCharacterCountBitLength(QrSegmentMode.Kanji, version);
        var maxCount = (1 << countBits) - 1;
        var count = 0;
        for (var end = start + 1; end < boundaries.Length && count < maxCount; end++) {
            if (!IsSingleCharacter(text, boundaries, end - 1, out var c) || !CouldBeQrKanji(c) || !QrKanjiTable.TryGetValue(c, out _)) break;
            count++;
            Relax(end, byteUsed, QrSegmentMode.Kanji, start, byteUsed,
                4 + countBits + QrSegment.GetDataBitLength(QrSegmentMode.Kanji, count), sourceIndex,
                costs, segmentCounts, previousPositions, previousByteStates, previousModes);
        }
    }

    private static void AddByteTransitions(
        int[] bytePrefix,
        int version,
        int start,
        int sourceIndex,
        int[] costs,
        int[] segmentCounts,
        int[] previousPositions,
        bool[] previousByteStates,
        QrSegmentMode[] previousModes) {
        var countBits = QrSegment.GetCharacterCountBitLength(QrSegmentMode.Byte, version);
        var maxCount = (1 << countBits) - 1;
        for (var end = start + 1; end < bytePrefix.Length; end++) {
            var byteCount = bytePrefix[end] - bytePrefix[start];
            if (byteCount > maxCount) break;
            Relax(end, true, QrSegmentMode.Byte, start, (sourceIndex & 1) != 0,
                4 + countBits + QrSegment.GetDataBitLength(QrSegmentMode.Byte, byteCount), sourceIndex,
                costs, segmentCounts, previousPositions, previousByteStates, previousModes);
        }
    }

    private static void Relax(
        int targetPosition,
        bool targetByteState,
        QrSegmentMode mode,
        int sourcePosition,
        bool sourceByteState,
        int transitionCost,
        int sourceIndex,
        int[] costs,
        int[] segmentCounts,
        int[] previousPositions,
        bool[] previousByteStates,
        QrSegmentMode[] previousModes) {
        var targetIndex = StateIndex(targetPosition, targetByteState);
        var candidateCost = costs[sourceIndex] + transitionCost;
        var candidateSegments = segmentCounts[sourceIndex] + 1;
        if (candidateCost > costs[targetIndex]
            || (candidateCost == costs[targetIndex] && candidateSegments >= segmentCounts[targetIndex])) return;

        costs[targetIndex] = candidateCost;
        segmentCounts[targetIndex] = candidateSegments;
        previousPositions[targetIndex] = sourcePosition;
        previousByteStates[targetIndex] = sourceByteState;
        previousModes[targetIndex] = mode;
    }

    private static QrSegment[] Reconstruct(
        string text,
        int[] boundaries,
        QrTextEncoding encoding,
        QrFnc1Mode fnc1Mode,
        int position,
        bool byteState,
        int[] previousPositions,
        bool[] previousByteStates,
        QrSegmentMode[] previousModes) {
        var result = new List<QrSegment>();
        while (position > 0) {
            var stateIndex = StateIndex(position, byteState);
            var previousPosition = previousPositions[stateIndex];
            if (previousPosition < 0) throw new InvalidOperationException("QR segment plan is incomplete.");
            var start = boundaries[previousPosition];
            var length = boundaries[position] - start;
            var part = text.Substring(start, length);
            var mode = previousModes[stateIndex];
            result.Add(mode switch {
                QrSegmentMode.Numeric => QrSegment.CreateNumeric(part),
                QrSegmentMode.Alphanumeric => QrSegment.CreateAlphanumeric(TransformAlphanumeric(part, fnc1Mode), part),
                QrSegmentMode.Byte => QrSegment.CreateByte(QrEncoding.Encode(part, encoding)),
                QrSegmentMode.Kanji => QrSegment.CreateKanji(part),
                _ => throw new InvalidOperationException("Unsupported QR segment mode.")
            });
            position = previousPosition;
            byteState = previousByteStates[stateIndex];
        }
        result.Reverse();
        return result.ToArray();
    }

    private static int[] BuildTextBoundaries(string text) {
        var boundaries = new List<int>(text.Length + 1) { 0 };
        var index = 0;
        while (index < text.Length) {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])) index += 2;
            else index++;
            boundaries.Add(index);
        }
        return boundaries.ToArray();
    }

    private static int[] BuildBytePrefix(string text, int[] boundaries, QrTextEncoding encoding) {
        var prefix = new int[boundaries.Length];
        for (var i = 0; i + 1 < boundaries.Length; i++) {
            prefix[i + 1] = prefix[i] + QrEncoding.GetByteCount(text, boundaries[i], boundaries[i + 1] - boundaries[i], encoding);
        }
        return prefix;
    }

    private static bool IsSingleCharacter(string text, int[] boundaries, int unitIndex, out char value) {
        var start = boundaries[unitIndex];
        if (boundaries[unitIndex + 1] - start != 1) {
            value = '\0';
            return false;
        }
        value = text[start];
        return true;
    }

    private static int GetAlphanumericEncodedLength(char value, QrFnc1Mode fnc1Mode) {
        if (fnc1Mode != QrFnc1Mode.None) {
            if (value == '\u001D') return 1;
            if (value == '%') return 2;
        }
        return "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:".IndexOf(value) >= 0 ? 1 : 0;
    }

    private static bool HasNonByteCandidate(string text, QrFnc1Mode fnc1Mode) {
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (GetAlphanumericEncodedLength(c, fnc1Mode) > 0 || CouldBeQrKanji(c) && QrKanjiTable.TryGetValue(c, out _)) return true;
        }
        return false;
    }

    internal static bool CouldBeQrKanji(char value) {
        // JIS X 0208 occupies these broad Unicode ranges. The cheap filter avoids building the
        // reverse Kanji table for common Latin, emoji-surrogate, Arabic, and other byte-only text.
        return value is >= '\u00A0' and <= '\u00FF'
            or >= '\u0370' and <= '\u045F'
            or >= '\u2000' and <= '\u9FFF'
            or >= '\uFF00' and <= '\uFFEF';
    }

    private static string TransformAlphanumeric(string text, QrFnc1Mode fnc1Mode) {
        if (fnc1Mode == QrFnc1Mode.None) return text;
        var extra = 0;
        for (var i = 0; i < text.Length; i++) if (text[i] == '%') extra++;
        if (extra == 0 && text.IndexOf('\u001D') < 0) return text;

        var chars = new char[text.Length + extra];
        var position = 0;
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (c == '\u001D') {
                chars[position++] = '%';
            } else if (c == '%') {
                chars[position++] = '%';
                chars[position++] = '%';
            } else {
                chars[position++] = c;
            }
        }
        return new string(chars, 0, position);
    }

    private static int StateIndex(int position, bool byteUsed) => position * 2 + (byteUsed ? 1 : 0);
}

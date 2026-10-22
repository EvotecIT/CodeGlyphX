// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CodeGlyphX.Internal.Fonts;

/// <summary>One logical text run with an explicit resolved paint direction.</summary>
internal sealed class ManagedBidiTextRun {
    internal ManagedBidiTextRun(string text, ManagedTextDirection direction, int embeddingLevel, int logicalOrder) {
        Text = text;
        Direction = direction;
        EmbeddingLevel = embeddingLevel;
        LogicalOrder = logicalOrder;
    }

    /// <summary>Text with Unicode bidi formatting controls removed.</summary>
    public string Text { get; }

    /// <summary>Resolved direction for this run.</summary>
    public ManagedTextDirection Direction { get; }

    /// <summary>Zero-based run order in the logical source text.</summary>
    public int LogicalOrder { get; }

    internal int EmbeddingLevel { get; }
}

/// <summary>
/// Resolves bounded directional runs, including Unicode embeddings, overrides, and isolates, without external shaping dependencies.
/// </summary>
internal static partial class ManagedBidiTextResolver {
    private const int MaximumEmbeddingDepth = 125;

    /// <summary>Resolves logical directional runs and removes non-painting bidi controls.</summary>
    public static IReadOnlyList<ManagedBidiTextRun> ResolveRuns(
        string? text,
        ManagedTextDirection baseDirection = ManagedTextDirection.Auto) {
        return ResolveRuns(text, baseDirection, CancellationToken.None);
    }

    /// <summary>Resolves logical directional runs and removes non-painting bidi controls.</summary>
    public static IReadOnlyList<ManagedBidiTextRun> ResolveRuns(
        string? text,
        ManagedTextDirection baseDirection,
        CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(text)) return Array.Empty<ManagedBidiTextRun>();
        IReadOnlyList<string> elements = ManagedTextElements.Split(text);
        if (!elements.Any(static element => IsParagraphBoundary(element))) {
            return ResolveParagraphRuns(text!, baseDirection, cancellationToken);
        }

        var runs = new List<ManagedBidiTextRun>();
        var paragraph = new StringBuilder();
        foreach (string element in elements) {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsParagraphBoundary(element)) {
                paragraph.Append(element);
                continue;
            }

            AppendParagraphRuns(runs, paragraph, baseDirection, cancellationToken);
            runs.Add(new ManagedBidiTextRun(element, ManagedTextDirection.LeftToRight, 0, runs.Count));
        }
        AppendParagraphRuns(runs, paragraph, baseDirection, cancellationToken);
        return runs.AsReadOnly();
    }

    private static IReadOnlyList<ManagedBidiTextRun> ResolveParagraphRuns(
        string text,
        ManagedTextDirection baseDirection,
        CancellationToken cancellationToken) {
        ManagedTextDirection resolvedBase = ResolveBaseDirection(text, baseDirection);

        var runs = new List<ManagedBidiTextRun>();
        var value = new StringBuilder();
        var states = new Stack<DirectionalState>();
        states.Push(new DirectionalState(
            resolvedBase,
            false,
            false,
            resolvedBase == ManagedTextDirection.RightToLeft ? 1 : 0,
            resolvedBase));
        var overflow = new BidiOverflowState();
        ManagedTextDirection lastDirection = resolvedBase;
        int lastLevel = states.Peek().Level;
        IReadOnlyList<string> elements = ManagedTextElements.Split(text);
        ManagedTextDirection?[] firstStrongIsolateDirections = ResolveFirstStrongIsolateDirections(elements, cancellationToken);
        ManagedTextDirection?[] followingStrongDirections = ResolveFollowingStrongDirections(elements, cancellationToken);
        for (int index = 0; index < elements.Count; index++) {
            if ((index & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            string element = elements[index];
            if (element.Length == 1 && ManagedTextElements.ContainsBidiControl(element)) {
                Flush(runs, value, lastDirection, lastLevel);
                TryApplyControl(
                    element[0],
                    firstStrongIsolateDirections[index],
                    states,
                    ref overflow,
                    ref lastDirection);
                lastLevel = ResolveLevel(states.Peek().Level, lastDirection);
                continue;
            }

            DirectionalState state = states.Peek();
            ManagedTextDirection direction = state.Override
                ? state.Direction
                : ResolveElementDirection(element, state.Direction, lastDirection, followingStrongDirections[index]);
            int level = ResolveLevel(state.Level, direction);
            if (value.Length > 0 && (direction != lastDirection || level != lastLevel)) {
                Flush(runs, value, lastDirection, lastLevel);
            }
            value.Append(element);
            lastDirection = direction;
            lastLevel = level;
        }
        Flush(runs, value, lastDirection, lastLevel);
        return runs.AsReadOnly();
    }

    private static void AppendParagraphRuns(
        List<ManagedBidiTextRun> destination,
        StringBuilder paragraph,
        ManagedTextDirection baseDirection,
        CancellationToken cancellationToken) {
        if (paragraph.Length == 0) return;
        foreach (ManagedBidiTextRun run in ResolveParagraphRuns(paragraph.ToString(), baseDirection, cancellationToken)) {
            destination.Add(new ManagedBidiTextRun(
                run.Text,
                run.Direction,
                run.EmbeddingLevel,
                destination.Count));
        }
        paragraph.Clear();
    }

    private static bool IsParagraphBoundary(string element) =>
        element is "\r" or "\n" or "\r\n" or "\u2028" or "\u2029";

    /// <summary>
    /// Resolves directional runs in visual placement order while retaining logical text order inside each run.
    /// </summary>
    public static IReadOnlyList<ManagedBidiTextRun> ResolveVisualRuns(
        string? text,
        ManagedTextDirection baseDirection = ManagedTextDirection.Auto) {
        return ResolveVisualRuns(text, baseDirection, CancellationToken.None);
    }

    /// <summary>
    /// Resolves directional runs in visual placement order while retaining logical text order inside each run.
    /// </summary>
    public static IReadOnlyList<ManagedBidiTextRun> ResolveVisualRuns(
        string? text,
        ManagedTextDirection baseDirection,
        CancellationToken cancellationToken) {
        IReadOnlyList<ManagedBidiTextRun> runs = ResolveRuns(text, baseDirection, cancellationToken);
        var visualRuns = runs
            .Select(static run => new VisualElement<ManagedBidiTextRun>(run, run.EmbeddingLevel))
            .ToList();
        ReorderByEmbeddingLevel(visualRuns, cancellationToken);
        return visualRuns.Select(static run => run.Value).ToArray();
    }

    /// <summary>Returns paint-order text after applying bounded embeddings, overrides, and isolates.</summary>
    public static string ToVisualOrder(
        string? text,
        ManagedTextDirection baseDirection = ManagedTextDirection.Auto) {
        return ToVisualOrder(text, baseDirection, CancellationToken.None);
    }

    /// <summary>Returns paint-order text after applying bounded embeddings, overrides, and isolates.</summary>
    public static string ToVisualOrder(
        string? text,
        ManagedTextDirection baseDirection,
        CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        IReadOnlyList<ManagedBidiTextRun> runs = ResolveRuns(text, baseDirection, cancellationToken);
        var visualElements = new List<VisualElement<string>>();
        foreach (ManagedBidiTextRun run in runs) {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (string element in ManagedTextElements.Split(run.Text)) {
                visualElements.Add(new VisualElement<string>(element, run.EmbeddingLevel));
            }
        }
        ReorderByEmbeddingLevel(visualElements, cancellationToken);
        var visual = new StringBuilder(text!.Length);
        foreach (VisualElement<string> element in visualElements) {
            visual.Append((element.Level & 1) == 1 ? MirrorText(element.Value) : element.Value);
        }
        return visual.ToString();
    }

    internal static IReadOnlyList<T> ToVisualOrder<T>(
        string directionalText,
        IReadOnlyList<T> visibleElements,
        ManagedTextDirection baseDirection,
        CancellationToken cancellationToken,
        Func<T, T>? mirrorOddLevel = null) {
        IReadOnlyList<ManagedBidiTextRun> runs = ResolveRuns(directionalText, baseDirection, cancellationToken);
        var visualElements = new List<VisualElement<T>>(visibleElements.Count);
        int elementIndex = 0;
        foreach (ManagedBidiTextRun run in runs) {
            cancellationToken.ThrowIfCancellationRequested();
            int count = ManagedTextElements.Split(run.Text).Count;
            if (count > visibleElements.Count - elementIndex) return Array.Empty<T>();
            for (int index = 0; index < count; index++) {
                T value = visibleElements[elementIndex++];
                if ((run.EmbeddingLevel & 1) == 1 && mirrorOddLevel != null) value = mirrorOddLevel(value);
                visualElements.Add(new VisualElement<T>(value, run.EmbeddingLevel));
            }
        }
        if (elementIndex != visibleElements.Count) return Array.Empty<T>();
        ReorderByEmbeddingLevel(visualElements, cancellationToken);
        return visualElements.Select(static element => element.Value).ToArray();
    }

    internal static IReadOnlyList<IReadOnlyList<T>> ToVisualLineOrder<T>(
        string directionalText,
        IReadOnlyList<IReadOnlyList<T>> visibleLines,
        ManagedTextDirection baseDirection,
        CancellationToken cancellationToken,
        Func<T, T>? mirrorOddLevel = null) {
        IReadOnlyList<ManagedBidiTextRun> runs = ResolveRuns(directionalText, baseDirection, cancellationToken);
        int visibleElementCount = visibleLines.Sum(static line => line.Count);
        var resolvedElements = new List<VisualElement<T>>(visibleElementCount);
        int lineIndex = 0;
        int elementIndex = 0;
        foreach (ManagedBidiTextRun run in runs) {
            cancellationToken.ThrowIfCancellationRequested();
            int count = ManagedTextElements.Split(run.Text).Count;
            for (int index = 0; index < count; index++) {
                while (lineIndex < visibleLines.Count && elementIndex >= visibleLines[lineIndex].Count) {
                    lineIndex++;
                    elementIndex = 0;
                }
                if (lineIndex >= visibleLines.Count) return Array.Empty<IReadOnlyList<T>>();
                T value = visibleLines[lineIndex][elementIndex++];
                if ((run.EmbeddingLevel & 1) == 1 && mirrorOddLevel != null) value = mirrorOddLevel(value);
                resolvedElements.Add(new VisualElement<T>(value, run.EmbeddingLevel));
            }
        }
        if (resolvedElements.Count != visibleElementCount) return Array.Empty<IReadOnlyList<T>>();

        var result = new List<IReadOnlyList<T>>(visibleLines.Count);
        int offset = 0;
        foreach (IReadOnlyList<T> line in visibleLines) {
            cancellationToken.ThrowIfCancellationRequested();
            List<VisualElement<T>> visualLine = resolvedElements.GetRange(offset, line.Count);
            ReorderByEmbeddingLevel(visualLine, cancellationToken);
            result.Add(visualLine.Select(static element => element.Value).ToArray());
            offset += line.Count;
        }
        return result.AsReadOnly();
    }

    internal static string MirrorText(string value) {
        if (value.Length == 0) return value;
        var mirrored = new StringBuilder(value.Length);
        foreach (char codeUnit in value) {
            mirrored.Append(ManagedBidiMirroring.Mirror(codeUnit));
        }
        return mirrored.ToString();
    }

    private static bool TryApplyControl(
        char control,
        ManagedTextDirection? firstStrongIsolateDirection,
        Stack<DirectionalState> states,
        ref BidiOverflowState overflow,
        ref ManagedTextDirection lastDirection) {
        switch (control) {
            case '\u061C':
            case '\u200F':
                lastDirection = ManagedTextDirection.RightToLeft;
                return true;
            case '\u200E':
                lastDirection = ManagedTextDirection.LeftToRight;
                return true;
            case '\u202A': Push(states, ManagedTextDirection.LeftToRight, false, false, ref lastDirection, ref overflow); return true;
            case '\u202B': Push(states, ManagedTextDirection.RightToLeft, false, false, ref lastDirection, ref overflow); return true;
            case '\u202D': Push(states, ManagedTextDirection.LeftToRight, true, false, ref lastDirection, ref overflow); return true;
            case '\u202E': Push(states, ManagedTextDirection.RightToLeft, true, false, ref lastDirection, ref overflow); return true;
            case '\u202C':
                if (overflow.OverflowEmbeddingCount > 0) {
                    overflow.OverflowEmbeddingCount--;
                } else if (overflow.OverflowIsolateCount == 0 && states.Count > 1 && !states.Peek().Isolate) {
                    lastDirection = states.Pop().OuterStrongDirection;
                }
                return true;
            case '\u2066': Push(states, ManagedTextDirection.LeftToRight, false, true, ref lastDirection, ref overflow); return true;
            case '\u2067': Push(states, ManagedTextDirection.RightToLeft, false, true, ref lastDirection, ref overflow); return true;
            case '\u2068':
                Push(states, firstStrongIsolateDirection ?? states.Peek().Direction, false, true, ref lastDirection, ref overflow);
                return true;
            case '\u2069':
                if (overflow.OverflowIsolateCount > 0) {
                    overflow.OverflowIsolateCount--;
                } else if (overflow.ValidIsolateCount > 0) {
                    overflow.OverflowEmbeddingCount = 0;
                    ManagedTextDirection outerStrongDirection = lastDirection;
                    while (states.Count > 1) {
                        DirectionalState state = states.Pop();
                        if (state.Isolate) {
                            outerStrongDirection = state.OuterStrongDirection;
                            break;
                        }
                    }
                    overflow.ValidIsolateCount--;
                    lastDirection = outerStrongDirection;
                }
                return true;
            default:
                return false;
        }
    }

    private static void Push(
        Stack<DirectionalState> states,
        ManagedTextDirection direction,
        bool @override,
        bool isolate,
        ref ManagedTextDirection lastDirection,
        ref BidiOverflowState overflow) {
        int level = NextEmbeddingLevel(states.Peek().Level, direction);
        bool overflowed = level > MaximumEmbeddingDepth
            || overflow.OverflowIsolateCount > 0
            || overflow.OverflowEmbeddingCount > 0;
        if (overflowed) {
            if (isolate) overflow.OverflowIsolateCount++;
            else if (overflow.OverflowIsolateCount == 0) overflow.OverflowEmbeddingCount++;
            return;
        }

        states.Push(new DirectionalState(direction, @override, isolate, level, lastDirection));
        lastDirection = direction;
        if (isolate) overflow.ValidIsolateCount++;
    }

    private static int NextEmbeddingLevel(int currentLevel, ManagedTextDirection direction) {
        int next = currentLevel + 1;
        bool requiresOdd = direction == ManagedTextDirection.RightToLeft;
        if (((next & 1) == 1) != requiresOdd) next++;
        return next;
    }

    private static int ResolveLevel(int embeddingLevel, ManagedTextDirection direction) {
        bool requiresOdd = direction == ManagedTextDirection.RightToLeft;
        return ((embeddingLevel & 1) == 1) == requiresOdd ? embeddingLevel : embeddingLevel + 1;
    }

    private static void ReorderByEmbeddingLevel<T>(List<VisualElement<T>> elements, CancellationToken cancellationToken) {
        if (elements.Count == 0) return;
        int maximumLevel = elements.Max(static element => element.Level);
        int minimumOddLevel = elements
            .Where(static element => (element.Level & 1) == 1)
            .Select(static element => element.Level)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        if (minimumOddLevel == int.MaxValue) return;

        for (int level = maximumLevel; level >= minimumOddLevel; level--) {
            cancellationToken.ThrowIfCancellationRequested();
            int start = 0;
            while (start < elements.Count) {
                while (start < elements.Count && elements[start].Level < level) start++;
                int end = start;
                while (end < elements.Count && elements[end].Level >= level) end++;
                if (end > start) elements.Reverse(start, end - start);
                start = end;
            }
        }
    }

    private static ManagedTextDirection?[] ResolveFirstStrongIsolateDirections(
        IReadOnlyList<string> elements,
        CancellationToken cancellationToken) {
        var directions = new ManagedTextDirection?[elements.Count];
        var isolates = new Stack<FirstStrongIsolateFrame>();
        int overflowDepth = 0;
        for (int index = 0; index < elements.Count; index++) {
            if ((index & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            string element = elements[index];
            if (element.Length == 1) {
                char control = element[0];
                if (control is '\u2066' or '\u2067' or '\u2068') {
                    if (overflowDepth > 0 || isolates.Count >= MaximumEmbeddingDepth) {
                        overflowDepth++;
                    } else {
                        isolates.Push(new FirstStrongIsolateFrame(control == '\u2068' ? index : -1));
                    }
                    continue;
                }
                if (control == '\u2069') {
                    if (overflowDepth > 0) {
                        overflowDepth--;
                    } else if (isolates.Count > 0) {
                        StoreFirstStrongDirection(isolates.Pop(), directions);
                    }
                    continue;
                }
                if (control is '\u061C' or '\u200E' or '\u200F') {
                    if (overflowDepth == 0 && isolates.Count > 0 && isolates.Peek().SourceIndex >= 0 && !isolates.Peek().Direction.HasValue) {
                        isolates.Peek().Direction = control == '\u200E'
                            ? ManagedTextDirection.LeftToRight
                            : ManagedTextDirection.RightToLeft;
                    }
                    continue;
                }
                if (ManagedTextElements.ContainsBidiControl(element)) continue;
            }

            if (overflowDepth > 0 || isolates.Count == 0 || isolates.Peek().SourceIndex < 0 || isolates.Peek().Direction.HasValue) {
                continue;
            }

            ManagedTextDirection direction = ManagedTextElements.ResolveBaseDirection(element);
            if (direction != ManagedTextDirection.Auto) isolates.Peek().Direction = direction;
        }

        while (isolates.Count > 0) StoreFirstStrongDirection(isolates.Pop(), directions);
        return directions;
    }

    private static void StoreFirstStrongDirection(
        FirstStrongIsolateFrame isolate,
        ManagedTextDirection?[] directions) {
        if (isolate.SourceIndex >= 0) directions[isolate.SourceIndex] = isolate.Direction;
    }

    private static ManagedTextDirection ResolveElementDirection(
        string element,
        ManagedTextDirection embeddingDirection,
        ManagedTextDirection lastDirection,
        ManagedTextDirection? followingStrongDirection) {
        ManagedTextDirection direction = ManagedTextElements.ResolveBaseDirection(element);
        if (direction != ManagedTextDirection.Auto) return direction;
        for (int index = 0; index < element.Length; index++) {
            if (char.IsDigit(element[index])) return ManagedTextDirection.LeftToRight;
        }
        return followingStrongDirection.HasValue && followingStrongDirection.Value == lastDirection
            ? lastDirection
            : embeddingDirection;
    }

    private static ManagedTextDirection?[] ResolveFollowingStrongDirections(
        IReadOnlyList<string> elements,
        CancellationToken cancellationToken) {
        var directions = new ManagedTextDirection?[elements.Count];
        ManagedTextDirection? following = null;
        for (int index = elements.Count - 1; index >= 0; index--) {
            if ((index & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            string element = elements[index];
            if (element is "\u200E") {
                following = ManagedTextDirection.LeftToRight;
                continue;
            }
            if (element is "\u061C" or "\u200F") {
                following = ManagedTextDirection.RightToLeft;
                continue;
            }
            if (element.Length == 1 && ManagedTextElements.ContainsBidiControl(element)) {
                following = null;
                continue;
            }

            directions[index] = following;
            ManagedTextDirection direction = ManagedTextElements.ResolveBaseDirection(element);
            if (direction != ManagedTextDirection.Auto) {
                following = direction;
                continue;
            }
            for (int characterIndex = 0; characterIndex < element.Length; characterIndex++) {
                if (char.IsDigit(element[characterIndex])) {
                    following = ManagedTextDirection.LeftToRight;
                    break;
                }
            }
        }
        return directions;
    }

    private static ManagedTextDirection ResolveBaseDirectionWithoutFormattingControls(string text) {
        var value = new StringBuilder(text.Length);
        var isolateDepth = 0;
        foreach (string element in ManagedTextElements.Enumerate(text)) {
            if (element is "\u2066" or "\u2067" or "\u2068") { isolateDepth++; continue; }
            if (element is "\u2069") { if (isolateDepth > 0) isolateDepth--; continue; }
            if (isolateDepth > 0) continue;
            if (!ManagedTextElements.ContainsBidiControl(element) || element is "\u061C" or "\u200E" or "\u200F") value.Append(element);
        }
        return ManagedTextElements.ResolveBaseDirection(value.ToString());
    }

    internal static ManagedTextDirection ResolveBaseDirection(string text, ManagedTextDirection requestedDirection) {
        ManagedTextDirection direction = requestedDirection == ManagedTextDirection.Auto
            ? ResolveBaseDirectionWithoutFormattingControls(text)
            : requestedDirection;
        return direction == ManagedTextDirection.Auto ? ManagedTextDirection.LeftToRight : direction;
    }

    private static void Flush(List<ManagedBidiTextRun> runs, StringBuilder value, ManagedTextDirection direction, int level) {
        if (value.Length == 0) return;
        runs.Add(new ManagedBidiTextRun(value.ToString(), direction, level, runs.Count));
        value.Clear();
    }

    private readonly struct DirectionalState {
        internal DirectionalState(
            ManagedTextDirection direction,
            bool @override,
            bool isolate,
            int level,
            ManagedTextDirection outerStrongDirection) {
            Direction = direction;
            Override = @override;
            Isolate = isolate;
            Level = level;
            OuterStrongDirection = outerStrongDirection;
        }
        internal ManagedTextDirection Direction { get; }
        internal bool Override { get; }
        internal bool Isolate { get; }
        internal int Level { get; }
        internal ManagedTextDirection OuterStrongDirection { get; }
    }

    private struct BidiOverflowState {
        internal int OverflowEmbeddingCount;
        internal int OverflowIsolateCount;
        internal int ValidIsolateCount;
    }

    private sealed class FirstStrongIsolateFrame {
        internal FirstStrongIsolateFrame(int sourceIndex) {
            SourceIndex = sourceIndex;
        }
        internal int SourceIndex { get; }
        internal ManagedTextDirection? Direction { get; set; }
    }

    private readonly struct VisualElement<T> {
        internal VisualElement(T value, int level) {
            Value = value;
            Level = level;
        }

        internal T Value { get; }
        internal int Level { get; }
    }
}

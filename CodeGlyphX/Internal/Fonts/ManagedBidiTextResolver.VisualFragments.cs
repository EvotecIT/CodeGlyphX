// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal.Fonts;

internal static partial class ManagedBidiTextResolver {
    /// <summary>
    /// Restores flat directional fragment order from left-to-right positions. Fragment text is
    /// already logical and remains unchanged; explicit bidi controls and embedding provenance
    /// belong to the logical-text resolver instead of this geometry-based recovery path.
    /// </summary>
    internal static T[] RestoreLogicalFragmentOrder<T>(IReadOnlyList<T> visualFragments,
        Func<T, string> getText, ManagedTextDirection baseDirection, CancellationToken token) {
        int count = visualFragments.Count;
        var text = new string[count];
        for (int index = 0; index < count; index++) {
            if ((index & 255) == 0) token.ThrowIfCancellationRequested();
            text[index] = getText(visualFragments[index]);
        }
        ManagedTextDirection?[] following = ResolveFollowingStrongDirections(text, token);
        var ranges = new List<(int Start, int End, ManagedTextDirection Direction)>();
        ManagedTextDirection previous = baseDirection;
        int start = 0;
        for (int index = 0; index < count; index++) {
            if ((index & 255) == 0) token.ThrowIfCancellationRequested();
            ManagedTextDirection direction = ResolveElementDirection(text[index], baseDirection, previous, following[index]);
            if (index > start && direction != previous) {
                ranges.Add((start, index, previous));
                start = index;
            }
            previous = direction;
        }
        if (start < count) ranges.Add((start, count, previous));
        var result = new T[count];
        int destination = 0;
        for (int rangeIndex = 0; rangeIndex < ranges.Count; rangeIndex++) {
            var range = ranges[baseDirection == ManagedTextDirection.RightToLeft ? ranges.Count - rangeIndex - 1 : rangeIndex];
            for (int offset = 0; offset < range.End - range.Start; offset++) {
                if ((destination & 255) == 0) token.ThrowIfCancellationRequested();
                int source = range.Direction == ManagedTextDirection.RightToLeft ? range.End - offset - 1 : range.Start + offset;
                result[destination++] = visualFragments[source];
            }
        }
        return result;
    }
}

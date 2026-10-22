// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeGlyphX.Internal.Fonts;

internal static class ManagedTextShaper {
    internal static ManagedTextFallback Resolve(
        string text,
        IManagedFontProgram font,
        ManagedTextDirection direction = ManagedTextDirection.Auto,
        System.Threading.CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(text) || !RequiresComplexLayout(text)) {
            return new ManagedTextFallback(text ?? string.Empty, used: false, incomplete: false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        bool incomplete =
            ManagedTextElements.ContainsCombiningMarkOrJoiner(text) ||
            ManagedTextElements.ContainsShapingRequiredScript(text) ||
            ManagedTextElements.ContainsVariationSelector(text) ||
            ManagedTextElements.ContainsZeroWidthJoinerSequence(text) ||
            (ManagedTextElements.ContainsJoiningScript(text) &&
             !ManagedArabicTextShaper.CanShapeAllJoiningCharacters(text));
        string contextual = ManagedArabicTextShaper.Shape(text);
        cancellationToken.ThrowIfCancellationRequested();
        string visual = ToVisualOrder(contextual, direction, cancellationToken);
        if (font.HasGlyphs(visual)) {
            return new ManagedTextFallback(visual, used: true, incomplete);
        }

        cancellationToken.ThrowIfCancellationRequested();
        string reordered = ToVisualOrder(
            ManagedArabicTextShaper.ToLogicalText(text),
            direction,
            cancellationToken);
        if (font.HasGlyphs(reordered)) {
            return new ManagedTextFallback(reordered, used: true, incomplete: true);
        }

        return new ManagedTextFallback(text, used: false, incomplete: true);
    }

    internal static bool RequiresComplexLayout(string? text) =>
        ManagedTextElements.ContainsCombiningMarkOrJoiner(text) ||
        ManagedTextElements.ContainsRightToLeft(text) ||
        ManagedTextElements.ContainsJoiningScript(text) ||
        ManagedTextElements.ContainsShapingRequiredScript(text) ||
        ManagedTextElements.ContainsBidiControl(text) ||
        ManagedTextElements.ContainsVariationSelector(text) ||
        ManagedTextElements.ContainsZeroWidthJoinerSequence(text);

    internal static string ToVisualOrder(
        string? value,
        System.Threading.CancellationToken cancellationToken = default) {
        return ToVisualOrder(value, ManagedTextDirection.Auto, cancellationToken);
    }

    internal static string ToVisualOrder(
        string? value,
        ManagedTextDirection requestedDirection,
        System.Threading.CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

        return ManagedBidiTextResolver.ToVisualOrder(value, requestedDirection, cancellationToken);
    }

    internal static IReadOnlyList<T> ToVisualOrder<T>(
        IReadOnlyList<T> elements,
        Func<T, string> textSelector,
        System.Threading.CancellationToken cancellationToken = default) {
        return ToVisualOrder(elements, textSelector, ManagedTextDirection.Auto, cancellationToken);
    }

    internal static IReadOnlyList<T> ToVisualOrder<T>(
        IReadOnlyList<T> elements,
        Func<T, string> textSelector,
        ManagedTextDirection requestedDirection,
        System.Threading.CancellationToken cancellationToken = default) {
        if (elements.Count == 0) return Array.Empty<T>();

        var completeText = new StringBuilder();
        foreach (T element in elements) completeText.Append(textSelector(element));
        var groups = new List<DirectionalElementGroup<T>>();
        var current = new List<T>();
        ManagedTextDirection baseDirection = requestedDirection == ManagedTextDirection.Auto
            ? ManagedTextElements.ResolveBaseDirection(completeText.ToString())
            : requestedDirection;
        TextElementDirection neutralDefault =
            baseDirection == ManagedTextDirection.RightToLeft
                ? TextElementDirection.RightToLeft
                : TextElementDirection.LeftToRight;
        TextElementDirection direction = neutralDefault;
        int elementIndex = 0;
        foreach (T element in elements) {
            if ((elementIndex++ & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            TextElementDirection resolved = ResolveDirection(textSelector(element));
            if (resolved == TextElementDirection.Neutral) {
                resolved = direction;
            }
            if (current.Count > 0 && direction != resolved) {
                groups.Add(new DirectionalElementGroup<T>(current.ToArray(), direction));
                current.Clear();
            }
            direction = resolved;
            current.Add(element);
        }
        if (current.Count > 0) {
            groups.Add(new DirectionalElementGroup<T>(current.ToArray(), direction));
        }

        if (baseDirection == ManagedTextDirection.RightToLeft) groups.Reverse();
        var visual = new List<T>(elements.Count);
        foreach (DirectionalElementGroup<T> group in groups) {
            cancellationToken.ThrowIfCancellationRequested();
            if (group.Direction == TextElementDirection.RightToLeft) {
                for (int index = group.Elements.Count - 1; index >= 0; index--) visual.Add(group.Elements[index]);
            } else {
                visual.AddRange(group.Elements);
            }
        }
        return visual;
    }

    private static TextElementDirection ResolveDirection(string element) {
        bool hasDigit = false;
        for (int index = 0; index < element.Length;) {
            int scalarIndex = index;
            int scalar = ReadScalar(element, ref index);
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(element, scalarIndex);
            if (category == UnicodeCategory.DecimalDigitNumber) {
                hasDigit = true;
                continue;
            }
            if (ManagedTextElements.IsRightToLeftScalar(scalar)) return TextElementDirection.RightToLeft;
            if (category is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter) {
                return TextElementDirection.LeftToRight;
            }
        }
        return hasDigit ? TextElementDirection.LeftToRight : TextElementDirection.Neutral;
    }

    private static int ReadScalar(string text, ref int index) {
        char first = text[index++];
        return char.IsHighSurrogate(first) &&
               index < text.Length &&
               char.IsLowSurrogate(text[index])
            ? char.ConvertToUtf32(first, text[index++])
            : first;
    }

    private enum TextElementDirection {
        Neutral,
        LeftToRight,
        RightToLeft
    }

    private readonly struct DirectionalElementGroup<T> {
        internal DirectionalElementGroup(IReadOnlyList<T> elements, TextElementDirection direction) {
            Elements = elements;
            Direction = direction;
        }

        internal IReadOnlyList<T> Elements { get; }
        internal TextElementDirection Direction { get; }
    }
}

internal readonly struct ManagedTextFallback {
    internal ManagedTextFallback(string text, bool used, bool incomplete) {
        Text = text;
        Used = used;
        Incomplete = incomplete;
    }

    internal string Text { get; }
    internal bool Used { get; }
    internal bool Incomplete { get; }
}

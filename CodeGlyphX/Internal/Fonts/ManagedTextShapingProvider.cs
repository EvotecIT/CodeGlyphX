// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGlyphX.Internal.Fonts;

/// <summary>
/// Dependency-light shaping provider for bounded Arabic joining, bidirectional text, and common
/// OpenType substitutions that can be represented by a TrueType-outline font.
/// </summary>
/// <remarks>
/// The provider deliberately declines scripts and lookup types that require shaping beyond the
/// bounded managed core. Callers then retain their normal scalar fallback and diagnostics. This
/// keeps <see cref="IManagedTextShapingProvider"/> as the single shaping contract used by Drawing and PDF.
/// </remarks>
internal sealed partial class ManagedTextShapingProvider : IManagedTextShapingProvider, IManagedTextShapingProviderMetadata {
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IManagedFontProgram, LatinFont> LatinFonts = new();
    /// <summary>Shared stateless provider instance.</summary>
    public static ManagedTextShapingProvider Instance { get; } = new ManagedTextShapingProvider();

    private ManagedTextShapingProvider() {
    }

    /// <inheritdoc />
    public ManagedTextShapingBackend Backend => ManagedTextShapingBackend.Managed;

    /// <inheritdoc />
    public ManagedTextShapingResult? ShapeText(ManagedTextShapingRequest request) {
        if (!TryShapeTokens(request, out IManagedFontProgram font, out List<ManagedOpenTypeSubstitution.GlyphToken> tokens,
            out ManagedTextDirection resolvedDirection)) return null;
        bool kerningEnabled = request.FeatureSettings.TryGetValue("kern", out int kerningValue)
            ? kerningValue != 0 : !request.ApplyDefaultLatinLigatures;
        var glyphIds = new int[tokens.Count];
        var scalars = new int[tokens.Count];
        for (int index = 0; index < tokens.Count; index++) {
            glyphIds[index] = tokens[index].GlyphId;
            scalars[index] = tokens[index].Scalar;
        }
        ManagedOpenTypeGlyphPositioning[] positioning = kerningEnabled
            ? PositionGlyphRun(font, glyphIds, scalars, request.CancellationToken)
            : new ManagedOpenTypeGlyphPositioning[tokens.Count];
        var glyphs = new List<ManagedShapedGlyph>(tokens.Count);
        var advanceAdjustments = new List<int>(tokens.Count);
        for (int index = 0; index < tokens.Count; index++) {
            ManagedOpenTypeSubstitution.GlyphToken token = tokens[index];
            request.CancellationToken.ThrowIfCancellationRequested();
            glyphs.Add(token.IsUnicodeContinuation
                ? ManagedShapedGlyph.CreateUnicodeContinuation(
                    token.GlyphId,
                    token.TextIndex,
                    positioning[index].XPlacement,
                    offsetY: 0)
                : ManagedShapedGlyph.CreatePositionedUsingNominalAdvance(
                    token.GlyphId,
                    token.UnicodeText,
                    token.TextIndex,
                    positioning[index].XPlacement,
                    offsetY: 0));
            advanceAdjustments.Add(positioning[index].XAdvance);
        }

        int[]? clusterStarts = tokens.Any(token => token.ClusterStart != token.TextIndex)
            ? tokens.Select(token => token.ClusterStart).ToArray() : null;
        return glyphs.Count == 0 ? null : new ManagedTextShapingResult(glyphs, advanceAdjustments, resolvedDirection, clusterStarts);
    }

    private static IReadOnlyList<VisualTextElement> MapVisualElements(
        string logical,
        string contextual,
        ManagedTextDirection direction,
        System.Threading.CancellationToken cancellationToken,
        bool reorder = true) {
        var logicalElements = new List<VisualTextElement>();
        int logicalIndex = 0;
        foreach (string contextualElement in ManagedTextElements.Enumerate(contextual)) {
            cancellationToken.ThrowIfCancellationRequested();
            int length = contextualElement.Length;
            string logicalElement = logical.Substring(logicalIndex, Math.Min(length, logical.Length - logicalIndex));
            if (!IsBidiControlElement(logicalElement)) {
                logicalElements.Add(new VisualTextElement(contextualElement, logicalElement, logicalIndex));
            }
            logicalIndex += length;
        }

        if (!reorder) return logicalElements;
        return ManagedBidiTextResolver.ToVisualOrder(
            contextual,
            logicalElements,
            direction,
            cancellationToken,
            static element => element.WithVisualText(ManagedBidiTextResolver.MirrorText(element.VisualText)));
    }

    private static bool TryAddElementGlyphs(
        IManagedFontProgram font,
        VisualTextElement element,
        List<ManagedOpenTypeSubstitution.GlyphToken> glyphs) {
        int visualIndex = 0;
        int logicalOffset = 0;
        while (visualIndex < element.VisualText.Length) {
            int visualScalar = ReadScalar(element.VisualText, ref visualIndex);
            int logicalStart = logicalOffset;
            int logicalScalar = ReadScalar(element.LogicalText, ref logicalOffset);
            if (!font.TryGetGlyphMetrics(visualScalar, out int glyphId, out _)) {
                return false;
            }

            string unicodeText = char.ConvertFromUtf32(logicalScalar);
            glyphs.Add(new ManagedOpenTypeSubstitution.GlyphToken(
                glyphId,
                unicodeText,
                element.LogicalIndex + logicalStart,
                logicalScalar));
        }

        return true;
    }

    private static ManagedOpenTypeGlyphPositioning[] PositionGlyphRun(
        IManagedFontProgram font,
        IReadOnlyList<int> glyphIds,
        IReadOnlyList<int> scalars,
        System.Threading.CancellationToken cancellationToken) {
        if (font is ManagedTrueTypeFont trueType) return trueType.PositionGlyphRun(glyphIds, scalars, cancellationToken);
        if (font is ManagedOpenTypeCffFont cff) return cff.PositionGlyphRun(glyphIds, scalars, cancellationToken);
        return new ManagedOpenTypeGlyphPositioning[glyphIds.Count];
    }

    private static bool IsBidiControlElement(string value) =>
        value.Length > 0 && ManagedTextElements.ContainsBidiControl(value);

    private static int ReadScalar(string text, ref int index) {
        char first = text[index++];
        return char.IsHighSurrogate(first) &&
               index < text.Length &&
               char.IsLowSurrogate(text[index])
            ? char.ConvertToUtf32(first, text[index++])
            : first;
    }

    private sealed class LatinFont {
        internal LatinFont(ManagedTextShapingRequest request, IManagedFontProgram? suppliedProgram) {
            var data = request.FontDataForShaping;
            var face = suppliedProgram?.CollectionIndex ?? request.FontCollectionIndex;
            Substitution = ManagedOpenTypeSubstitution.TryCreate(data, face);
            Font = suppliedProgram ?? (request.IsOpenTypeCff
                ? ManagedOpenTypeCffFont.TryLoad(data, face, request.VariationCoordinatesForShaping, out _)
                : ManagedTrueTypeFont.TryLoad(data, face));
        }
        internal IManagedFontProgram? Font { get; }
        internal ManagedOpenTypeSubstitution? Substitution { get; }
    }

    private readonly struct VisualTextElement {
        internal VisualTextElement(string visualText, string logicalText, int logicalIndex) {
            VisualText = visualText;
            LogicalText = logicalText;
            LogicalIndex = logicalIndex;
        }

        internal string VisualText { get; }
        internal string LogicalText { get; }
        internal int LogicalIndex { get; }

        internal VisualTextElement WithVisualText(string visualText) =>
            new VisualTextElement(visualText, LogicalText, LogicalIndex);
    }
}

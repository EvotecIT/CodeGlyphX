// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal.Fonts;

/// <summary>
/// Decoded font program used by ManagedIMO measurement and outline rendering.
/// Implementations may be supplied by specialized packages to override the first-party engine.
/// </summary>
internal interface IManagedFontProgram {
    /// <summary>
    /// Stable provider-defined identity for the decoded program and selected instance. The value
    /// must change when rendering-relevant configuration, such as variable-font axes, changes.
    /// </summary>
    string Fingerprint { get; }

    /// <summary>Best available human-readable face name.</summary>
    string? DisplayName { get; }

    /// <summary>Selected collection face index, when the source is a font collection.</summary>
    int? CollectionIndex { get; }

    /// <summary>Font design units per em.</summary>
    int UnitsPerEm { get; }

    /// <summary>Whether the source uses CFF or CFF2 outlines.</summary>
    bool IsOpenTypeCff { get; }

    /// <summary>
    /// Whether measurement and contours already apply complete script shaping and bidirectional layout.
    /// </summary>
    bool ProvidesComplexTextLayout { get; }

    /// <summary>Returns an independent font-data snapshot suitable for an external shaping provider.</summary>
    byte[] GetFontDataForShaping();

    /// <summary>Tests whether the selected program contains glyphs for every text element.</summary>
    bool HasGlyphs(string text);

    /// <summary>Measures text at the requested font size.</summary>
    double Measure(string text, double fontSize);

    /// <summary>Measures the supplied grapheme-safe text elements.</summary>
    IReadOnlyList<double> MeasureTextElements(IReadOnlyList<string> elements, double fontSize);

    /// <summary>Returns the line height at the requested font size.</summary>
    double LineHeight(double fontSize);

    /// <summary>Returns the face line-spacing ratio.</summary>
    double LineSpacingRatio { get; }

    /// <summary>Returns filled contours for the supplied text.</summary>
    List<List<ManagedPoint>> GetTextContours(string text, double x, double y, double fontSize);

    /// <summary>Maps one Unicode scalar to a glyph and design-unit advance.</summary>
    bool TryGetGlyphMetrics(int scalar, out int glyphId, out int advanceWidth);

    /// <summary>Measures a validated externally shaped run.</summary>
    double MeasureShapedText(string text, ManagedTextShapingResult result, double fontSize);

    /// <summary>Returns filled contours for a validated externally shaped run.</summary>
    List<List<ManagedPoint>> GetShapedTextContours(
        string text,
        ManagedTextShapingResult result,
        double x,
        double y,
        double fontSize);
}

/// <summary>
/// Optional bounded outline contract for font programs that can stop contour expansion promptly.
/// Renderers use this seam for cancellation and untrusted-input output budgets while retaining
/// compatibility with existing <see cref="IManagedFontProgram"/> implementations.
/// </summary>
internal interface IManagedBoundedFontProgram : IManagedFontProgram {
    /// <summary>Returns contours while enforcing the maximum total point count.</summary>
    List<List<ManagedPoint>> GetTextContoursBounded(
        string text,
        double x,
        double y,
        double fontSize,
        int maximumPointCount,
        CancellationToken cancellationToken);

    /// <summary>Returns externally shaped contours while enforcing the maximum total point count.</summary>
    List<List<ManagedPoint>> GetShapedTextContoursBounded(
        string text,
        ManagedTextShapingResult result,
        double x,
        double y,
        double fontSize,
        int maximumPointCount,
        CancellationToken cancellationToken);
}

/// <summary>Internal seam for sharing one CFF interpreter budget across renderer-owned runs.</summary>
internal interface IManagedCffBoundedFontProgram : IManagedBoundedFontProgram {
    List<List<ManagedPoint>> GetTextContoursBounded(
        string text,
        double x,
        double y,
        double fontSize,
        int maximumPointCount,
        CancellationToken cancellationToken,
        ManagedCffOperationBudget operationBudget);

    List<List<ManagedPoint>> GetShapedTextContoursBounded(
        string text,
        ManagedTextShapingResult result,
        double x,
        double y,
        double fontSize,
        int maximumPointCount,
        CancellationToken cancellationToken,
        ManagedCffOperationBudget operationBudget);
}

/// <summary>
/// Optional vertical-metrics contract for aligning fallback runs from different font faces on one baseline.
/// </summary>
internal interface IManagedFontBaselineMetrics {
    /// <summary>Returns the distance from the top of the face line box to its baseline.</summary>
    double BaselineOffset(double fontSize);
}

/// <summary>
/// Internal seam used to keep an external shaping engine on the same selected
/// variable-font instance as the first-party metrics and outline engine.
/// </summary>
internal interface IManagedVariableFontProgram {
    IReadOnlyDictionary<string, float> VariationCoordinatesForShaping { get; }
}

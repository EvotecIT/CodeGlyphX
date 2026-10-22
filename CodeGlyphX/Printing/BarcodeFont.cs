using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal.Fonts;

namespace CodeGlyphX.Printing;

/// <summary>A portable font loaded from bundled or caller-supplied TrueType or OpenType bytes.</summary>
/// <remarks>
/// The font owns its data and never searches installed fonts. TrueType outlines, OpenType CFF/CFF2,
/// and TrueType collection faces are supported. Managed shaping covers common Latin substitutions,
/// bidirectional text and bounded Arabic joining; strict mode rejects scripts requiring unsupported shaping.
/// Font redistribution and outline use remain subject to the selected font's license.
/// </remarks>
public sealed partial class BarcodeFont : IBarcodeFont {
    private const int MaximumFontBytes = 16 * 1024 * 1024;
    private const int MaximumTextLength = 32768;
    private const int MaximumOutlinePoints = 1048576;
    private readonly IManagedBoundedFontProgram _program;
    private readonly byte[] _data;

    /// <inheritdoc />
    public string Name { get; }
    /// <summary>Gets the selected zero-based TrueType collection face, or null for a standalone font.</summary>
    public int? CollectionIndex => _program.CollectionIndex;

    private BarcodeFont(IManagedBoundedFontProgram program, byte[] data) {
        _program = program;
        _data = data;
        Name = program.DisplayName ?? "Supplied barcode font";
    }

    /// <summary>Loads a font from an independent snapshot of at most 16 MiB.</summary>
    /// <param name="data">A complete, uncompressed TTF, OTF or TrueType collection.</param>
    /// <param name="collectionIndex">A zero-based TrueType collection face, or null to select the first supported face.</param>
    /// <returns>A scoped font that uses no operating-system font services.</returns>
    /// <exception cref="ArgumentException">The bytes are malformed, unsupported, or exceed the input limit.</exception>
    public static BarcodeFont FromBytes(byte[] data, int? collectionIndex = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (collectionIndex < 0) throw new ArgumentOutOfRangeException(nameof(collectionIndex));
        if (data.Length < 12 || data.Length > MaximumFontBytes) throw new ArgumentException("A font must contain 12 bytes to 16 MiB of uncompressed font data.", nameof(data));
        var snapshot = (byte[])data.Clone();
        try {
            BarcodeFontValidation.Validate(snapshot, collectionIndex);
            IManagedBoundedFontProgram? program = collectionIndex is null or 0
                ? ManagedOpenTypeCffFont.TryLoad(snapshot, null, out _) : null;
            program ??= ManagedTrueTypeFont.TryLoad(snapshot, collectionIndex);
            if (program is null || program.UnitsPerEm <= 0 || !BarcodeBounds.Finite(program.LineHeight(1)) || program.LineHeight(1) <= 0)
                throw new ArgumentException("The supplied font has no supported outline face or valid metrics.", nameof(data));
            return new BarcodeFont(program, snapshot);
        } catch (Exception exception) when (exception is InvalidDataException or NotSupportedException or OverflowException or IndexOutOfRangeException or ArgumentOutOfRangeException) {
            throw new ArgumentException("The supplied font is malformed or uses unsupported outline data.", nameof(data), exception);
        }
    }

    /// <inheritdoc />
    public BarcodeTextShape Shape(string text, double emSize, bool strict = true) {
        ValidateText(text, emSize);
        var missing = new List<int>();
        var replaced = PrepareText(text, missing);
        if (strict && missing.Count > 0) throw new ArgumentException("The supplied font does not contain every requested character.", nameof(text));
        var complex = ManagedTextShaper.RequiresComplexLayout(replaced);
        var request = new ManagedTextShapingRequest(replaced, Name, _data, _program.IsOpenTypeCff,
            _program.UnitsPerEm, ManagedTextDirection.Auto, null, CancellationToken.None,
            _program.CollectionIndex, null, cloneFontData: false, fontProgramCacheKey: _program,
            applyDefaultLatinLigatures: !complex);
        ManagedTextShapingResult? shaped;
        try { shaped = ManagedTextShapingProvider.Instance.ShapeText(request); }
        catch (ManagedFontResourceLimitException exception) { throw new InvalidDataException(exception.Message, exception); }
        var incomplete = false;
        var renderText = replaced;
        if (shaped is null && complex) {
            var fallback = ManagedTextShaper.Resolve(replaced, _program);
            incomplete = fallback.Incomplete;
            renderText = fallback.Text;
            if (strict && incomplete) throw new ArgumentException("The text requires shaping beyond the managed font engine's support.", nameof(text));
        }
        var advance = shaped is null ? _program.Measure(renderText, emSize) : _program.MeasureShapedText(replaced, shaped, emSize);
        var outlines = shaped is null
            ? _program.GetTextContoursBounded(renderText, 0, 0, emSize, MaximumOutlinePoints, CancellationToken.None)
            : _program.GetShapedTextContoursBounded(replaced, shaped, 0, 0, emSize, MaximumOutlinePoints, CancellationToken.None);
        var contours = new List<IEnumerable<BarcodePoint>>(outlines.Count);
        foreach (var outline in outlines) {
            var points = new List<BarcodePoint>(outline.Count);
            foreach (var point in outline) points.Add(new BarcodePoint(point.X, point.Y));
            contours.Add(points);
        }
        var baseline = (_program as IManagedFontBaselineMetrics)?.BaselineOffset(emSize) ?? emSize;
        return new BarcodeTextShape(text, advance, _program.LineHeight(emSize), baseline, contours, missing, incomplete);
    }

    private string PrepareText(string text, List<int> missing) {
        var result = new StringBuilder(text.Length);
        var seen = new HashSet<int>();
        foreach (var element in ManagedTextElements.Enumerate(text)) {
            if (_program.HasGlyphs(element)) { result.Append(element); continue; }
            for (var index = 0; index < element.Length; index++) {
                var scalar = char.IsHighSurrogate(element[index]) ? char.ConvertToUtf32(element[index], element[++index]) : element[index];
                var value = char.ConvertFromUtf32(scalar);
                if (_program.HasGlyphs(value)) { result.Append(value); continue; }
                if (seen.Add(scalar)) missing.Add(scalar);
                if (_program.HasGlyphs("?")) result.Append('?');
                else if (_program.HasGlyphs("\ufffd")) result.Append('\ufffd');
                else throw new ArgumentException("The font has neither the requested character nor a supported replacement glyph.", nameof(text));
            }
        }
        return result.ToString();
    }

    private static void ValidateText(string text, double emSize) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (text.Length > MaximumTextLength) throw new ArgumentException("A barcode text run exceeds 32768 UTF-16 characters.", nameof(text));
        if (!BarcodeBounds.Finite(emSize) || emSize <= 0 || emSize > 1000000) throw new ArgumentOutOfRangeException(nameof(emSize));
        for (var index = 0; index < text.Length; index++) {
            var value = text[index];
            if (char.IsControl(value) || value is '\u2028' or '\u2029') throw new ArgumentException("Shape accepts one line without control characters.", nameof(text));
            if (char.IsHighSurrogate(value)) {
                if (++index >= text.Length || !char.IsLowSurrogate(text[index])) throw new ArgumentException("Text contains an unpaired surrogate.", nameof(text));
            } else if (char.IsLowSurrogate(value)) throw new ArgumentException("Text contains an unpaired surrogate.", nameof(text));
        }
    }
}

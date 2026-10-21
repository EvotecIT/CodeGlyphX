// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CodeGlyphX.Internal.Fonts;

/// <summary>
/// Small managed reader for sfnt fonts that use TrueType <c>glyf</c> outlines, used for dependency-free text rasterization.
/// </summary>
/// <remarks>
/// This type reads font files directly and does not call operating-system graphics or font APIs.
/// It reads only the supplied font bytes and never searches the host for installed fonts.
/// </remarks>
internal sealed partial class ManagedTrueTypeFont : IManagedBoundedFontProgram, IManagedFontBaselineMetrics, IManagedVariableFontProgram {
    private const uint MaxTrueTypeCollectionFonts = 256;
    private const int MaxFontTableRecords = 512;
    private readonly byte[] _data;
    private readonly int _cmap;
    private readonly int _cmapLength;
    private readonly int _glyf;
    private readonly int _head;
    private readonly int _hhea;
    private readonly int _hmtx;
    private readonly ManagedOpenTypeKerning _kerning;
    private readonly ManagedOpenTypeTracking? _tracking;
    private readonly int _loca;
    private readonly int _maxp;
    private readonly int _name;
    private readonly HashSet<int> _validFormat4Subtables;
    private readonly HashSet<int> _validFormat12Subtables;
    private readonly HashSet<int> _paintedNotdefScalars;
    private readonly ManagedTrueTypeVariations? _variations;
    private readonly ManagedFontVariationModel _variationModel;
    private readonly int _unitsPerEm;
    private readonly int _ascender;
    private readonly int _descender;
    private readonly int _lineGap;
    private readonly ushort _numGlyphs;
    private readonly ushort _numHMetrics;
    private readonly short _indexToLocFormat;
    private readonly int? _collectionIndex;
    private readonly string _fingerprint;
    private ManagedTrueTypeFont(
        byte[] data,
        Dictionary<string, int> tables,
        IReadOnlyDictionary<string, int> tableLengths,
        int? collectionIndex,
        ManagedFontVariationModel? variationModel = null) {
        _data = data;
        _fingerprint = ComputeFingerprint(data, collectionIndex)
            + (variationModel != null && variationModel.IsVariable ? ":axes=" + variationModel.Identity : string.Empty);
        _collectionIndex = collectionIndex;
        _variationModel = variationModel ?? ManagedFontVariationModel.None;
        _cmap = tables["cmap"];
        _cmapLength = tableLengths["cmap"];
        _glyf = tables["glyf"];
        _head = tables["head"];
        _hhea = tables["hhea"];
        _hmtx = tables["hmtx"];
        int gpos = tables.TryGetValue("GPOS", out int gposOffset) ? gposOffset : -1;
        int kern = tables.TryGetValue("kern", out int kernOffset) ? kernOffset : -1;
        ManagedOpenTypeReader? reader = ManagedOpenTypeReader.TryCreateForFace(data, collectionIndex);
        if (_variationModel.IsVariable && reader == null) {
            reader = ManagedOpenTypeReader.TryCreate(data)
                ?? throw new InvalidDataException("The variable TrueType font table directory is invalid.");
        }
        _kerning = reader != null
            ? ManagedOpenTypeKerning.FromReader(reader, _variationModel)
            : new ManagedOpenTypeKerning(_data, kern, gpos, includeExtendedGpos: true);
        _loca = tables["loca"];
        _maxp = tables["maxp"];
        _name = tables.TryGetValue("name", out var name) ? name : -1;
        _validFormat4Subtables = ManagedOpenTypeCmap.CollectValidFormat4Subtables(
            _data,
            _cmap,
            _cmapLength,
            ManagedOpenTypeCmap.MaximumSubtables);
        _validFormat12Subtables = ManagedOpenTypeCmap.CollectValidFormat12Subtables(
            _data,
            _cmap,
            _cmapLength,
            ManagedOpenTypeCmap.MaximumSubtables,
            ManagedOpenTypeCmap.MaximumFormat12Groups);
        _paintedNotdefScalars = new HashSet<int>();
        if (tables.TryGetValue("pG00", out int provenance) && tableLengths.TryGetValue("pG00", out int provenanceLength) &&
            provenanceLength >= 4) {
            uint count = ReadUInt32(_data, provenance);
            if (count <= ManagedOpenTypeCmap.MaximumFormat12Groups && count <= (uint)((provenanceLength - 4) / 4)) {
                for (int index = 0; index < count; index++) {
                    uint scalar = ReadUInt32(_data, provenance + 4 + index * 4);
                    if (scalar <= 0x10FFFF && !(scalar >= 0xD800 && scalar <= 0xDFFF)) _paintedNotdefScalars.Add((int)scalar);
                }
            }
        }
        _tracking = tables.TryGetValue("trak", out int trackingOffset)
            ? ManagedOpenTypeTracking.Parse(data, trackingOffset, tableLengths["trak"]) : null;
        _unitsPerEm = ReadUInt16(_data, _head + 18);
        _indexToLocFormat = ReadInt16(_data, _head + 50);
        ManagedOpenTypeMvarMetrics? mvar = reader != null && _variationModel.IsVariable
            ? ManagedOpenTypeMvarMetrics.TryParse(reader, _variationModel)
            : null;
        _ascender = checked(ReadInt16(_data, _hhea + 4) + (mvar?.HorizontalAscenderDelta ?? 0));
        _descender = checked(ReadInt16(_data, _hhea + 6) + (mvar?.HorizontalDescenderDelta ?? 0));
        _lineGap = checked(ReadInt16(_data, _hhea + 8) + (mvar?.HorizontalLineGapDelta ?? 0));
        _numHMetrics = ReadUInt16(_data, _hhea + 34);
        _numGlyphs = ReadUInt16(_data, _maxp + 4);
        _variations = variationModel != null && variationModel.IsVariable
            ? ManagedTrueTypeVariations.Parse(data, tables, variationModel, _numGlyphs)
            : null;
    }

    /// <inheritdoc />
    public string Fingerprint => _trackingEvaluationScale == 1D ? _fingerprint
        : _fingerprint + ":tracking-scale=" + _trackingEvaluationScale.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    IReadOnlyDictionary<string, float> IManagedVariableFontProgram.VariationCoordinatesForShaping =>
        _variationModel?.DesignCoordinates ?? ManagedFontVariationModel.None.DesignCoordinates;

    private static string ComputeFingerprint(byte[] data, int? collectionIndex) {
        using HashAlgorithm hash = SHA256.Create();
        byte[] digest = hash.ComputeHash(data);
        return "sha256:" + ToLowerHex(digest) + ":face="
            + (collectionIndex.HasValue
                ? collectionIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "auto");
    }

    private static string ToLowerHex(byte[] bytes) {
        const string alphabet = "0123456789abcdef";
        var result = new char[bytes.Length * 2];
        for (int index = 0; index < bytes.Length; index++) {
            result[index * 2] = alphabet[bytes[index] >> 4];
            result[index * 2 + 1] = alphabet[bytes[index] & 0x0F];
        }
        return new string(result);
    }

    /// <summary>Attempts to decode a standalone font or the first suitable face in a font collection.</summary>
    /// <param name="data">Complete TrueType font data, including OpenType data that uses TrueType <c>glyf</c> outlines.</param>
    /// <returns>The decoded font, or <see langword="null"/> when the data is unsupported or malformed.</returns>
    public static ManagedTrueTypeFont? TryLoad(byte[] data) => TryLoad(data, null, null);

    /// <summary>Attempts to decode a specific face from in-memory font data.</summary>
    /// <param name="data">Complete TrueType font or collection data, including OpenType fonts that use TrueType <c>glyf</c> outlines.</param>
    /// <param name="collectionIndex">Zero-based collection face index, or <see langword="null"/> to select the first suitable face.</param>
    /// <returns>The decoded font, or <see langword="null"/> when the data or requested index is invalid.</returns>
    public static ManagedTrueTypeFont? TryLoad(byte[] data, int? collectionIndex) => TryLoad(data, collectionIndex, null);

    /// <summary>Attempts to decode a font face matching an optional collection index and face name.</summary>
    /// <param name="data">Complete TrueType font or collection data, including OpenType fonts that use TrueType <c>glyf</c> outlines.</param>
    /// <param name="collectionIndex">Zero-based collection face index, or <see langword="null"/> to search the collection.</param>
    /// <param name="faceName">Preferred family, full, or PostScript face name; <see langword="null"/> accepts any suitable face.</param>
    /// <returns>The matching font, or <see langword="null"/> when the data is malformed, unsupported, or contains no matching face.</returns>
    public static ManagedTrueTypeFont? TryLoad(byte[] data, int? collectionIndex, string? faceName) {
        if (collectionIndex.HasValue && collectionIndex.Value < 0) return null;
        try {
            if (data.Length < 12) return null;
            var scaler = ReadUInt32(data, 0);
            if (scaler == 0x74746366) {
                var fontCount = ReadUInt32(data, 8);
                if (fontCount == 0 || fontCount > MaxTrueTypeCollectionFonts) return null;
                if (data.Length < 12 + fontCount * 4) return null;
                if (collectionIndex.HasValue) {
                    if (collectionIndex.Value >= fontCount) return null;
                    var indexedFont = TryLoad(data, CheckedOffset(data, ReadUInt32(data, 12 + collectionIndex.Value * 4)), collectionIndex.Value);
                    return indexedFont != null && indexedFont.MatchesName(faceName) ? indexedFont : null;
                }

                for (var i = 0; i < fontCount; i++) {
                    var directoryOffset = CheckedOffset(data, ReadUInt32(data, 12 + i * 4));
                    var font = TryLoad(data, directoryOffset, (int)i);
                    if (font != null && font.MatchesName(faceName)) return font;
                }

                return null;
            }

            if (collectionIndex.HasValue && collectionIndex.Value > 0) return null;
            var standalone = TryLoad(data, 0, null);
            return standalone != null && standalone.MatchesName(faceName) ? standalone : null;
        } catch (InvalidDataException) {
            return null;
        } catch (ArgumentOutOfRangeException) {
            return null;
        } catch (IndexOutOfRangeException) {
            return null;
        }
    }

    internal static ManagedTrueTypeFont? TryLoad(
        byte[] data,
        ManagedFontVariationModel variationModel,
        out string? error) {
        if (variationModel == null) throw new ArgumentNullException(nameof(variationModel));
        error = null;
        try {
            return TryLoad(data, 0, null, variationModel);
        } catch (Exception exception) when (exception is InvalidDataException
                                            || exception is NotSupportedException
                                            || exception is OverflowException
                                            || exception is ArgumentOutOfRangeException
                                            || exception is IndexOutOfRangeException) {
            error = exception.Message;
            return null;
        }
    }

    private static ManagedTrueTypeFont? TryLoad(byte[] data, int directoryOffset, int? collectionIndex) {
        return TryLoad(data, directoryOffset, collectionIndex, null);
    }

    private static ManagedTrueTypeFont? TryLoad(
        byte[] data,
        int directoryOffset,
        int? collectionIndex,
        ManagedFontVariationModel? variationModel) {
        if (directoryOffset < 0 || directoryOffset + 12 > data.Length) return null;
        var scaler = ReadUInt32(data, directoryOffset);
        if (scaler != 0x00010000 && scaler != 0x74727565) return null;
        var count = ReadUInt16(data, directoryOffset + 4);
        if (count == 0 || count > MaxFontTableRecords) return null;
        if (data.Length < directoryOffset + 12 + count * 16) return null;
        var tables = new Dictionary<string, int>(StringComparer.Ordinal);
        var tableLengths = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++) {
            var record = directoryOffset + 12 + i * 16;
            if (record + 16 > data.Length) return null;
            var tag = ((char)data[record]).ToString() + (char)data[record + 1] + (char)data[record + 2] + (char)data[record + 3];
            uint offsetValue = ReadUInt32(data, record + 8);
            uint lengthValue = ReadUInt32(data, record + 12);
            if (offsetValue > int.MaxValue || lengthValue > int.MaxValue || tables.ContainsKey(tag)) return null;
            // Subsetters can leave an empty optional table recorded at the end of the file.
            // It carries no data, so treat it as absent instead of rejecting the whole face.
            if (lengthValue == 0 && offsetValue <= (uint)data.Length) continue;
            var offset = CheckedOffset(data, offsetValue);
            int length = checked((int)lengthValue);
            if (offset > data.Length - length) return null;
            tables[tag] = offset;
            tableLengths[tag] = length;
        }

        foreach (var required in new[] { "cmap", "glyf", "head", "hhea", "hmtx", "loca", "maxp" }) if (!tables.ContainsKey(required)) return null;
        return new ManagedTrueTypeFont(data, tables, tableLengths, collectionIndex, variationModel);
    }

    /// <inheritdoc />
    public double Measure(string text, double fontSize) {
        var scale = ScaleFor(fontSize);
        var width = 0.0;
        double tracking = HorizontalTracking(fontSize);
        ManagedTrueTypeVariations.WorkBudget? variationWorkBudget = _variations?.CreateWorkBudget();
        var glyphs = new List<int>();
        var scalars = new List<int>();
        var textIndexes = new List<int>();
        for (int index = 0; index < text.Length;) {
            int textIndex = index;
            int glyph = ReadMappedGlyph(text, ref index, out int scalar);
            if (glyph < 0) continue;
            textIndexes.Add(textIndex);
            glyphs.Add(glyph);
            scalars.Add(scalar);
        }
        bool[]? boundaries = TrackingBoundaries(text, textIndexes);
        ManagedOpenTypeGlyphPositioning[] positioning = _kerning.PositionRun(glyphs, scalars);
        for (int index = 0; index < glyphs.Count; index++) {
            width += checked(AdvanceWidth((ushort)glyphs[index], variationWorkBudget, CancellationToken.None) +
                             positioning[index].XAdvance) * scale + (boundaries == null || boundaries[index] ? tracking : 0D);
        }
        return width;
    }

    internal IReadOnlyList<double> MeasureTextElements(IReadOnlyList<string> elements, double fontSize) {
        var widths = new double[elements.Count];
        double scale = ScaleFor(fontSize);
        double tracking = HorizontalTracking(fontSize);
        ManagedTrueTypeVariations.WorkBudget? variationWorkBudget = _variations?.CreateWorkBudget();
        var glyphs = new List<int>();
        var scalars = new List<int>();
        var elementIndexes = new List<int>();
        var textIndexes = new List<int>();
        int sourceOffset = 0;
        for (int elementIndex = 0; elementIndex < elements.Count; elementIndex++) {
            string text = elements[elementIndex];
            for (int textIndex = 0; textIndex < text.Length;) {
                int sourceIndex = sourceOffset + textIndex;
                int glyph = ReadMappedGlyph(text, ref textIndex, out int scalar);
                if (glyph < 0) continue;
                textIndexes.Add(sourceIndex);
                glyphs.Add(glyph);
                scalars.Add(scalar);
                elementIndexes.Add(elementIndex);
            }
            sourceOffset += text.Length;
        }
        bool[]? boundaries = TrackingBoundaries(string.Concat(elements), textIndexes);
        ManagedOpenTypeGlyphPositioning[] positioning = _kerning.PositionRun(glyphs, scalars);
        for (int index = 0; index < glyphs.Count; index++) {
            widths[elementIndexes[index]] += checked(
                AdvanceWidth((ushort)glyphs[index], variationWorkBudget, CancellationToken.None) +
                positioning[index].XAdvance) * scale + (boundaries == null || boundaries[index] ? tracking : 0D);
        }
        return widths;
    }

    /// <inheritdoc />
    public double LineHeight(double fontSize) {
        return Math.Max(1, _ascender - _descender) * ScaleFor(fontSize);
    }

    /// <inheritdoc />
    public double BaselineOffset(double fontSize) => _ascender * ScaleFor(fontSize);

    internal double LineSpacingRatio =>
        Math.Max(1, _ascender - _descender + _lineGap) / (double)_unitsPerEm;

    /// <summary>
    /// Reads flattened fill contours for the supplied text at a top-left baseline box.
    /// </summary>
    public List<List<ManagedPoint>> GetTextContours(string text, double x, double y, double fontSize) {
        return GetTextContoursBounded(text, x, y, fontSize, int.MaxValue, CancellationToken.None);
    }

    /// <inheritdoc />
    public List<List<ManagedPoint>> GetTextContoursBounded(
        string text,
        double x,
        double y,
        double fontSize,
        int maximumPointCount,
        CancellationToken cancellationToken) {
        if (maximumPointCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumPointCount));
        var contours = new List<List<ManagedPoint>>();
        if (string.IsNullOrEmpty(text)) {
            return contours;
        }

        var scale = ScaleFor(fontSize);
        var cursor = x;
        double tracking = HorizontalTracking(fontSize);
        var baseline = y + _ascender * scale;
        int pointCount = 0;
        var readBudget = new GlyphReadBudget();
        ManagedTrueTypeVariations.WorkBudget? variationWorkBudget = _variations?.CreateWorkBudget();
        var glyphs = new List<(ushort Glyph, int Scalar)>();
        var textIndexes = new List<int>();
        for (int index = 0; index < text.Length;) {
            cancellationToken.ThrowIfCancellationRequested();
            int textIndex = index;
            int glyph = ReadMappedGlyph(text, ref index, out int scalar);
            if (glyph < 0) continue;
            textIndexes.Add(textIndex);
            glyphs.Add((checked((ushort)glyph), scalar));
        }

        var glyphIds = new List<int>(glyphs.Count);
        var scalars = new List<int>(glyphs.Count);
        foreach ((ushort glyph, int scalar) in glyphs) {
            glyphIds.Add(glyph);
            scalars.Add(scalar);
        }
        bool[]? boundaries = TrackingBoundaries(text, textIndexes);
        ManagedOpenTypeGlyphPositioning[] positioning = _kerning.PositionRun(glyphIds, scalars);

        for (int index = 0; index < glyphs.Count; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            ushort glyph = glyphs[index].Glyph;
            double glyphX = cursor + positioning[index].XPlacement * scale;
            List<List<ManagedPoint>> glyphContours = glyph == 0 && !HasPaintedNotdefMapping(glyphs[index].Scalar)
                ? new List<List<ManagedPoint>>() : ReadGlyphContours(
                glyph,
                new FontTransform(scale, 0, 0, -scale, glyphX, baseline),
                0,
                variationWorkBudget,
                maximumPointCount,
                ref pointCount,
                cancellationToken,
                readBudget,
                attachmentPoints: null);
            contours.AddRange(glyphContours);
            int positionedAdvance = checked(
                AdvanceWidth(glyph, variationWorkBudget, cancellationToken) +
                positioning[index].XAdvance);
            cursor += positionedAdvance * scale + (boundaries == null || boundaries[index] ? tracking : 0D);
        }

        return contours;
    }

    /// <summary>Best-effort display name read from the font name table.</summary>
    public string? DisplayName => FirstName(4) ?? FirstName(1) ?? FirstName(6) ?? FirstName(2);

    /// <summary>Collection index when the font was loaded from a TrueType collection.</summary>
    public int? CollectionIndex => _collectionIndex;

    internal bool HasGlyphs(string value) {
        return ManagedOpenTypeCmap.HasGlyphs(
            value,
            scalar => MapGlyph(scalar),
            MapVariationGlyph,
            HasPaintedNotdefMapping);
    }

    private int ReadMappedGlyph(string text, ref int index, out int scalar) =>
        ManagedOpenTypeCmap.ReadMappedGlyph(text, ref index, scalarValue => MapGlyph(scalarValue), MapVariationGlyph, out scalar);

    private int MapVariationGlyph(int scalar, int selector) => ManagedOpenTypeCmap.MapVariationSequence(
        _data,
        _cmap,
        _cmapLength,
        _numGlyphs,
        scalar,
        selector,
        mappedScalar => MapGlyph(mappedScalar));

    private bool MatchesName(string? faceName) {
        if (string.IsNullOrWhiteSpace(faceName)) return true;
        var requested = faceName!;
        foreach (var name in ReadNames()) {
            if (name.Equals(requested, StringComparison.OrdinalIgnoreCase)) return true;
            if (name.IndexOf(requested, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        return false;
    }

    private IEnumerable<string> ReadNames() {
        if (_name < 0 || _name + 6 > _data.Length) yield break;
        var count = ReadUInt16(_data, _name + 2);
        var stringOffset = _name + ReadUInt16(_data, _name + 4);
        for (var i = 0; i < count; i++) {
            var record = _name + 6 + i * 12;
            if (record + 12 > _data.Length) yield break;
            var nameId = ReadUInt16(_data, record + 6);
            if (nameId != 1 && nameId != 2 && nameId != 4 && nameId != 6) continue;
            var platform = ReadUInt16(_data, record);
            var length = ReadUInt16(_data, record + 8);
            var offset = stringOffset + ReadUInt16(_data, record + 10);
            if (offset < 0 || length == 0 || offset + length > _data.Length) continue;
            var value = DecodeName(platform, offset, length).Trim();
            if (value.Length > 0) yield return value;
        }
    }

    private string DecodeName(ushort platform, int offset, int length) {
        if (platform == 0 || platform == 3) return Encoding.BigEndianUnicode.GetString(_data, offset, length);
        return Encoding.ASCII.GetString(_data, offset, length);
    }

    private string? FirstName(ushort requestedNameId) {
        if (_name < 0 || _name + 6 > _data.Length) return null;
        var count = ReadUInt16(_data, _name + 2);
        var stringOffset = _name + ReadUInt16(_data, _name + 4);
        for (var i = 0; i < count; i++) {
            var record = _name + 6 + i * 12;
            if (record + 12 > _data.Length) return null;
            if (ReadUInt16(_data, record + 6) != requestedNameId) continue;
            var platform = ReadUInt16(_data, record);
            var length = ReadUInt16(_data, record + 8);
            var offset = stringOffset + ReadUInt16(_data, record + 10);
            if (offset < 0 || length == 0 || offset + length > _data.Length) continue;
            var value = DecodeName(platform, offset, length).Trim();
            if (value.Length > 0) return value;
        }

        return null;
    }

    private double ScaleFor(double fontSize) {
        return fontSize / Math.Max(1, _unitsPerEm);
    }

    private static int ReadScalar(string value, ref int index) {
        char first = value[index++];
        if (char.IsHighSurrogate(first) && index < value.Length && char.IsLowSurrogate(value[index])) {
            return char.ConvertToUtf32(first, value[index++]);
        }

        return first;
    }

    private static bool IsWhitespaceScalar(int scalar) => scalar <= char.MaxValue && char.IsWhiteSpace((char)scalar);

    // A PDF may explicitly map a character to an inked .notdef glyph. A cmap lookup returns
    // zero for both that mapping and an absent character, so coverage must inspect the cmap
    // entry before treating the painted placeholder as missing.
    private int BaseAdvanceWidth(ushort glyph) => glyph < _numHMetrics
            ? ReadUInt16(_data, _hmtx + glyph * 4)
            : ReadUInt16(_data, _hmtx + (_numHMetrics - 1) * 4);

    private int AdvanceWidth(ushort glyph) => AdvanceWidth(glyph, null, CancellationToken.None);

    private int Kerning(ushort left, ushort right, int leftScalar, int rightScalar) =>
        _kerning.Adjustment(left, right, leftScalar, rightScalar);

    private int AdvanceWidth(
        ushort glyph,
        ManagedTrueTypeVariations.WorkBudget? workBudget,
        CancellationToken cancellationToken) {
        int advance = BaseAdvanceWidth(glyph);
        return checked(advance + VariationAdvanceWidthDelta(glyph, advance, workBudget, cancellationToken));
    }

    private static ushort ReadUInt16(byte[] data, int offset) => (ushort)((data[offset] << 8) | data[offset + 1]);
    private static short ReadInt16(byte[] data, int offset) => (short)ReadUInt16(data, offset);
    private static double ReadF2Dot14(byte[] data, int offset) => ReadInt16(data, offset) / 16384.0;
    private static uint ReadUInt32(byte[] data, int offset) => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    private bool InBounds(int offset, int length) => offset >= 0 && length >= 0 && offset <= _data.Length - length;
    private static int CheckedOffset(byte[] data, uint offset) {
        if (offset > int.MaxValue || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        return (int)offset;
    }

    private static bool TryCheckedOffset(byte[] data, uint offset, out int checkedOffset) {
        if (offset > int.MaxValue || offset >= data.Length) {
            checkedOffset = 0;
            return false;
        }

        checkedOffset = (int)offset;
        return true;
    }

}

// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CodeGlyphX.Internal.Fonts;

internal sealed partial class ManagedTrueTypeFont {
    private bool HasPaintedNotdefMapping(int scalar) {
        if (!_paintedNotdefScalars.Contains(scalar) || _numGlyphs == 0 ||
            GlyphOffset(0) == GlyphOffset(1) || _cmapLength < 4)
            return false;
        int end = checked(_cmap + _cmapLength);
        int count = ReadUInt16(_data, _cmap + 2);
        if (count == 0 || count > ManagedOpenTypeCmap.MaximumSubtables || _cmap + 4 > end - count * 8)
            return false;
        for (int index = 0; index < count; index++) {
            int record = _cmap + 4 + index * 8;
            if (!ManagedOpenTypeCmap.IsUnicodeEncoding(ReadUInt16(_data, record), ReadUInt16(_data, record + 2))) continue;
            uint relative = ReadUInt32(_data, record + 4);
            if (relative > (uint)(_cmapLength - 2)) continue;
            int table = _cmap + checked((int)relative);
            int format = ReadUInt16(_data, table);
            if (format == 4 && scalar <= char.MaxValue && _validFormat4Subtables.Contains(table)) {
                int segments = ReadUInt16(_data, table + 6) / 2;
                int endCodes = table + 14;
                int starts = endCodes + segments * 2 + 2;
                int low = 0;
                int high = segments - 1;
                while (low <= high) {
                    int segment = low + (high - low) / 2;
                    int first = ReadUInt16(_data, starts + segment * 2);
                    int last = ReadUInt16(_data, endCodes + segment * 2);
                    if (scalar < first) high = segment - 1;
                    else if (scalar > last) low = segment + 1;
                    else {
                        if (scalar != 0xffff && MapFormat4(table, end, scalar) == 0) return true;
                        break;
                    }
                }
            } else if (format == 12 && _validFormat12Subtables.Contains(table)) {
                uint groups = ReadUInt32(_data, table + 12);
                int firstGroup = table + 16;
                uint low = 0;
                uint high = groups;
                while (low < high) {
                    uint group = low + (high - low) / 2;
                    int entry = firstGroup + checked((int)group * 12);
                    uint first = ReadUInt32(_data, entry);
                    if (first < scalar) low = group + 1;
                    else if (first > scalar) high = group;
                    else return ReadUInt32(_data, entry + 8) == 0;
                }
            }
        }
        return false;
    }

    private ushort MapGlyph(int scalar) {
        if (scalar < 0 || scalar > 0x10FFFF) return 0;
        var cmapOffset = _cmap;
        int cmapEnd = checked(cmapOffset + _cmapLength);
        if (_cmapLength < 4) return 0;
        var subtableCount = ReadUInt16(_data, cmapOffset + 2);
        if (subtableCount == 0 || subtableCount > ManagedOpenTypeCmap.MaximumSubtables) return 0;
        if (cmapOffset + 4 > cmapEnd - subtableCount * 8) return 0;
        var best = 0;
        var bestScore = 0;
        for (var i = 0; i < subtableCount; i++) {
            var record = cmapOffset + 4 + i * 8;
            var platform = ReadUInt16(_data, record);
            var encoding = ReadUInt16(_data, record + 2);
            uint offsetValue = ReadUInt32(_data, record + 4);
            if (offsetValue > (uint)(_cmapLength - 2)) continue;
            int offset = checked((int)offsetValue);
            var absolute = cmapOffset + offset;
            if (absolute < cmapOffset || absolute > cmapEnd - 2) continue;
            var format = ReadUInt16(_data, absolute);
            if (!ManagedOpenTypeCmap.IsUnicodeEncoding(platform, encoding)) continue;
            if (format == 4 && !_validFormat4Subtables.Contains(absolute)) continue;
            if (format == 12 && !_validFormat12Subtables.Contains(absolute)) continue;
            var score = ManagedOpenTypeCmap.ScoreSubtable(
                format,
                platform,
                encoding,
                preferFormat12: scalar > 0xFFFF);
            if ((format == 4 || format == 12) && score > bestScore) {
                best = absolute;
                bestScore = score;
            }
        }

        if (best == 0) return 0;
        ushort glyph = MapCmapSubtable(best, cmapEnd, scalar);
        return glyph != 0 ? glyph : MapFallbackCmapSubtable(best, cmapEnd, scalar, subtableCount);
    }

    private ushort MapCmapSubtable(int table, int cmapEnd, int scalar) =>
        ReadUInt16(_data, table) == 12
            ? MapFormat12(table, cmapEnd, scalar)
            : MapFormat4(table, cmapEnd, scalar);

    private ushort MapFallbackCmapSubtable(int selectedTable, int cmapEnd, int scalar, int subtableCount) {
        int bestScore = 0;
        ushort bestGlyph = 0;
        for (int index = 0; index < subtableCount; index++) {
            int record = _cmap + 4 + index * 8;
            int platform = ReadUInt16(_data, record);
            int encoding = ReadUInt16(_data, record + 2);
            uint offsetValue = ReadUInt32(_data, record + 4);
            if (offsetValue > (uint)(_cmapLength - 2)) continue;
            int table = _cmap + checked((int)offsetValue);
            if (table == selectedTable || table < _cmap || table > cmapEnd - 2 ||
                !ManagedOpenTypeCmap.IsUnicodeEncoding(platform, encoding)) continue;
            int format = ReadUInt16(_data, table);
            if (format == 4 && (scalar > 0xFFFF || !_validFormat4Subtables.Contains(table))) continue;
            if (format == 12 && !_validFormat12Subtables.Contains(table)) continue;
            if (format != 4 && format != 12) continue;
            int score = ManagedOpenTypeCmap.ScoreSubtable(
                format,
                platform,
                encoding,
                preferFormat12: scalar > 0xFFFF);
            if (score <= bestScore) continue;
            ushort glyph = MapCmapSubtable(table, cmapEnd, scalar);
            if (glyph == 0) continue;
            bestGlyph = glyph;
            bestScore = score;
        }
        return bestGlyph;
    }

    private ushort MapFormat4(int table, int cmapEnd, int scalar) {
        if (!_validFormat4Subtables.Contains(table)) return 0;
        if (scalar > char.MaxValue) return 0;
        if (table < _cmap || table > cmapEnd - 16) return 0;
        var length = ReadUInt16(_data, table + 2);
        if (length < 16 || table > cmapEnd - length) return 0;
        var code = scalar;
        var segCount = ReadUInt16(_data, table + 6) / 2;
        if (segCount == 0 || segCount > ManagedOpenTypeCmap.MaximumSubtables * 16) return 0;
        var endCodes = table + 14;
        var startCodes = endCodes + segCount * 2 + 2;
        var idDeltas = startCodes + segCount * 2;
        var idRangeOffsets = idDeltas + segCount * 2;
        if (idRangeOffsets < table || idRangeOffsets + segCount * 2 > table + length) return 0;

        int low = 0;
        int high = segCount - 1;
        while (low <= high) {
            int i = low + (high - low) / 2;
            int end = ReadUInt16(_data, endCodes + i * 2);
            int start = ReadUInt16(_data, startCodes + i * 2);
            if (code < start) {
                high = i - 1;
                continue;
            }
            if (code > end) {
                low = i + 1;
                continue;
            }
            int delta = ReadInt16(_data, idDeltas + i * 2);
            int rangeOffset = ReadUInt16(_data, idRangeOffsets + i * 2);
            if (rangeOffset == 0) return ValidateMappedGlyph((ushort)((code + delta) & 0xffff));
            int glyphOffset = idRangeOffsets + i * 2 + rangeOffset + (code - start) * 2;
            if (glyphOffset < table || glyphOffset > table + length - 2) return 0;
            ushort glyph = ReadUInt16(_data, glyphOffset);
            return glyph == 0 ? (ushort)0 : ValidateMappedGlyph((ushort)((glyph + delta) & 0xffff));
        }

        return 0;
    }

    private ushort MapFormat12(int table, int cmapEnd, int scalar) {
        if (!_validFormat12Subtables.Contains(table)) return 0;
        if (table < _cmap || table > cmapEnd - 16) return 0;
        var length = ReadUInt32(_data, table + 4);
        if (length < 16 || length > int.MaxValue || table > cmapEnd - (int)length) return 0;
        uint code = (uint)scalar;
        var groups = ReadUInt32(_data, table + 12);
        if (groups > ManagedOpenTypeCmap.MaximumFormat12Groups || groups > (length - 16U) / 12U) return 0;
        var groupOffset = table + 16;
        uint low = 0;
        uint high = groups;
        while (low < high) {
            uint middle = low + (high - low) / 2;
            int current = checked(groupOffset + (int)middle * 12);
            uint start = ReadUInt32(_data, current);
            uint end = ReadUInt32(_data, current + 4);
            if (code < start) {
                high = middle;
                continue;
            }
            if (code > end) {
                low = middle + 1;
                continue;
            }
            ulong glyph = (ulong)ReadUInt32(_data, current + 8) + code - start;
            return glyph < _numGlyphs ? checked((ushort)glyph) : (ushort)0;
        }

        return 0;
    }

    private ushort ValidateMappedGlyph(ushort glyph) => glyph > 0 && glyph < _numGlyphs ? glyph : (ushort)0;

}

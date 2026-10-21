using System;
using System.Collections.Generic;
using System.IO;

namespace CodeGlyphX.Printing;

/// <summary>Checks sfnt table extents and TrueType metrics before the managed reader follows glyph references.</summary>
internal static class BarcodeFontValidation {
    internal static void Validate(byte[] data, int? collectionIndex) {
        if (Read32(data, 0) != 0x74746366) { ValidateFace(data, 0); return; }
        var count = Read32(data, 8);
        if (count == 0 || count > 256 || 12L + count * 4L > data.Length || collectionIndex >= count) throw new InvalidDataException("Invalid TrueType collection directory.");
        for (var index = 0; index < count; index++) {
            if (collectionIndex.HasValue && collectionIndex.Value != index) continue;
            var offset = Read32(data, 12 + index * 4);
            if (offset > int.MaxValue) throw new InvalidDataException("Invalid face offset.");
            ValidateFace(data, (int)offset);
        }
    }

    private static void ValidateFace(byte[] data, int offset) {
        Range(data, offset, 12);
        var signature = Read32(data, offset);
        if (signature != 0x00010000 && signature != 0x74727565 && signature != 0x4f54544f) throw new InvalidDataException("Unsupported font container.");
        var count = Read16(data, offset + 4);
        if (count == 0 || count > 512) throw new InvalidDataException("Invalid table count.");
        Range(data, offset + 12, count * 16);
        var tables = new Dictionary<uint, (int Offset, int Length)>();
        for (var index = 0; index < count; index++) {
            var record = offset + 12 + index * 16;
            var tag = Read32(data, record);
            var start = Read32(data, record + 8);
            var length = Read32(data, record + 12);
            if (start > int.MaxValue || length > int.MaxValue || tables.ContainsKey(tag)) throw new InvalidDataException("Invalid font table record.");
            Range(data, (int)start, (int)length);
            tables.Add(tag, ((int)start, (int)length));
        }
        var head = Required(tables, 0x68656164, 54);
        var hhea = Required(tables, 0x68686561, 36);
        var maxp = Required(tables, 0x6d617870, 6);
        Required(tables, 0x636d6170, 4);
        var units = Read16(data, head.Offset + 18);
        var glyphCount = Read16(data, maxp.Offset + 4);
        var metricCount = Read16(data, hhea.Offset + 34);
        if (units < 16 || units > 16384 || glyphCount == 0 || metricCount == 0 || metricCount > glyphCount) throw new InvalidDataException("Invalid font design metrics.");
        Required(tables, 0x686d7478, metricCount * 4 + (glyphCount - metricCount) * 2);
        if (signature == 0x4f54544f) return;
        var strideValue = Read16(data, head.Offset + 50);
        if (strideValue > 1) throw new InvalidDataException("Invalid glyph location format.");
        var stride = strideValue == 0 ? 2 : 4;
        var locations = Required(tables, 0x6c6f6361, (glyphCount + 1) * stride);
        var glyphs = Required(tables, 0x676c7966, 0);
        uint previous = 0;
        for (var index = 0; index <= glyphCount; index++) {
            var current = stride == 2 ? (uint)Read16(data, locations.Offset + index * 2) * 2 : Read32(data, locations.Offset + index * 4);
            if (current < previous || current > glyphs.Length) throw new InvalidDataException("Invalid glyph extent.");
            if (current != previous && current - previous < 10) throw new InvalidDataException("Truncated glyph header.");
            previous = current;
        }
    }

    private static (int Offset, int Length) Required(Dictionary<uint, (int Offset, int Length)> tables, uint tag, int minimum) {
        if (!tables.TryGetValue(tag, out var value) || value.Length < minimum) throw new InvalidDataException("A required font table is missing or truncated.");
        return value;
    }
    private static void Range(byte[] data, int offset, int length) {
        if (offset < 0 || length < 0 || offset > data.Length - length) throw new InvalidDataException("A font table extends outside the supplied bytes.");
    }
    private static int Read16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];
    private static uint Read32(byte[] data, int offset) => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
}

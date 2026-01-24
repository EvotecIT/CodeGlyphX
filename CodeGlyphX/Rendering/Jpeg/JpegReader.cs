using System;

namespace CodeGlyphX.Rendering.Jpeg;

/// <summary>
/// Decodes baseline and progressive JPEG images to RGBA buffers (SOF0/SOF2, 8-bit, Huffman).
/// </summary>
public static partial class JpegReader {
    private static readonly byte[] ZigZag = {
        0, 1, 5, 6, 14, 15, 27, 28,
        2, 4, 7, 13, 16, 26, 29, 42,
        3, 8, 12, 17, 25, 30, 41, 43,
        9, 11, 18, 24, 31, 40, 44, 53,
        10, 19, 23, 32, 39, 45, 52, 54,
        20, 22, 33, 38, 46, 51, 55, 60,
        21, 34, 37, 47, 50, 56, 59, 61,
        35, 36, 48, 49, 57, 58, 62, 63,
    };

    private static readonly double[,] IdctCos = BuildCosTable();

    /// <summary>
    /// Returns true when the buffer looks like a JPEG.
    /// </summary>
    public static bool IsJpeg(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8;
    }

    /// <summary>
    /// Decodes a JPEG image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (!IsJpeg(data)) throw new FormatException("Invalid JPEG signature.");

        var quantTables = new int[4][];
        var dcTables = new HuffmanTable[4];
        var acTables = new HuffmanTable[4];
        var restartInterval = 0;
        var hasFrame = false;
        var progressive = false;
        var orientation = 1;
        int? adobeTransform = null;
        var frame = default(JpegFrame);
        ProgressiveState? progressiveState = null;

        var offset = 2;
        while (offset < data.Length) {
            if (data[offset] != 0xFF) {
                offset++;
                continue;
            }

            while (offset < data.Length && data[offset] == 0xFF) offset++;
            if (offset >= data.Length) break;
            var marker = data[offset++];

            if (marker == 0xD9) break;

            if (marker == 0xDA) {
                if (!hasFrame) throw new FormatException("Missing JPEG frame segment.");
                var segLen = ReadUInt16BE(data, offset);
                offset += 2;
                if (segLen < 2 || offset + segLen - 2 > data.Length) throw new FormatException("Invalid JPEG scan header.");
                var scan = ParseScanHeader(data.Slice(offset, segLen - 2), ref frame);
                offset += segLen - 2;

                var scanEnd = FindScanEnd(data, offset);
                var scanData = data.Slice(offset, scanEnd - offset);

                if (!progressive) {
                    width = frame.Width;
                    height = frame.Height;
                    var rgba = DecodeBaselineScan(scanData, scan, frame, quantTables, dcTables, acTables, restartInterval, adobeTransform);
                    return ApplyOrientation(rgba, ref width, ref height, orientation);
                }

                progressiveState ??= ProgressiveState.Create(frame, quantTables);
                DecodeProgressiveScan(scanData, scan, frame, progressiveState, quantTables, dcTables, acTables, restartInterval);
                offset = scanEnd;
                continue;
            }

            if (marker == 0xDB) {
                var segLen = ReadUInt16BE(data, offset);
                offset += 2;
                var end = offset + segLen - 2;
                if (segLen < 2 || end > data.Length) throw new FormatException("Invalid JPEG DQT segment.");
                while (offset < end) {
                    var info = data[offset++];
                    var precision = info >> 4;
                    var tableId = info & 0x0F;
                    if (precision != 0) throw new FormatException("Unsupported JPEG quantization precision.");
                    if (tableId >= quantTables.Length) throw new FormatException("Unsupported JPEG quantization table.");
                    if (offset + 64 > end) throw new FormatException("Invalid JPEG quantization table.");
                    var table = new int[64];
                    for (var i = 0; i < 64; i++) {
                        table[ZigZag[i]] = data[offset++];
                    }
                    quantTables[tableId] = table;
                }
                continue;
            }

            if (marker == 0xC4) {
                var segLen = ReadUInt16BE(data, offset);
                offset += 2;
                var end = offset + segLen - 2;
                if (segLen < 2 || end > data.Length) throw new FormatException("Invalid JPEG DHT segment.");
                while (offset < end) {
                    var info = data[offset++];
                    var tableClass = info >> 4;
                    var tableId = info & 0x0F;
                    if (tableId >= 4) throw new FormatException("Unsupported JPEG Huffman table.");
                    if (offset + 16 > end) throw new FormatException("Invalid JPEG Huffman table.");
                    var counts = new byte[16];
                    for (var i = 0; i < 16; i++) counts[i] = data[offset++];
                    var total = 0;
                    for (var i = 0; i < 16; i++) total += counts[i];
                    if (offset + total > end) throw new FormatException("Invalid JPEG Huffman values.");
                    var values = data.Slice(offset, total).ToArray();
                    offset += total;
                    var table = HuffmanTable.Build(counts, values);
                    if (tableClass == 0) dcTables[tableId] = table;
                    else if (tableClass == 1) acTables[tableId] = table;
                    else throw new FormatException("Unsupported JPEG Huffman table class.");
                }
                continue;
            }

            if (marker == 0xC0 || marker == 0xC2) {
                var segLen = ReadUInt16BE(data, offset);
                offset += 2;
                if (segLen < 8 || offset + segLen - 2 > data.Length) throw new FormatException("Invalid JPEG SOF segment.");
                frame = ParseFrameHeader(data.Slice(offset, segLen - 2));
                hasFrame = true;
                progressive = marker == 0xC2;
                offset += segLen - 2;
                continue;
            }

            if (marker == 0xDD) {
                var segLen = ReadUInt16BE(data, offset);
                offset += 2;
                if (segLen != 4 || offset + 2 > data.Length) throw new FormatException("Invalid JPEG DRI segment.");
                restartInterval = ReadUInt16BE(data, offset);
                offset += 2;
                continue;
            }

            if (marker == 0xE1) {
                var segLen = ReadUInt16BE(data, offset);
                offset += 2;
                if (segLen < 2 || offset + segLen - 2 > data.Length) throw new FormatException("Invalid JPEG APP1 segment.");
                var app1 = data.Slice(offset, segLen - 2);
                if (TryReadExifOrientation(app1, out var exifOrientation)) orientation = exifOrientation;
                offset += segLen - 2;
                continue;
            }

            if (marker == 0xEE) {
                var segLen = ReadUInt16BE(data, offset);
                offset += 2;
                if (segLen < 2 || offset + segLen - 2 > data.Length) throw new FormatException("Invalid JPEG APP14 segment.");
                var app14 = data.Slice(offset, segLen - 2);
                if (TryReadAdobeTransform(app14, out var transform)) adobeTransform = transform;
                offset += segLen - 2;
                continue;
            }

            if (marker >= 0xD0 && marker <= 0xD7) {
                continue;
            }

            var length = ReadUInt16BE(data, offset);
            offset += 2;
            if (length < 2 || offset + length - 2 > data.Length) throw new FormatException("Invalid JPEG segment.");
            offset += length - 2;
        }

        if (progressive && hasFrame && progressiveState is not null) {
            width = frame.Width;
            height = frame.Height;
            var rgba = progressiveState.RenderRgba(frame, adobeTransform);
            return ApplyOrientation(rgba, ref width, ref height, orientation);
        }

        throw new FormatException("JPEG scan not found.");
    }
}

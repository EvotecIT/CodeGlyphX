using System;

namespace CodeGlyphX.Rendering.Gif;

/// <summary>
/// Decodes GIF images to RGBA buffers (first frame only).
/// </summary>
public static class GifReader {
    /// <summary>
    /// Decodes a GIF image to an RGBA buffer.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> gif, out int width, out int height) {
        if (gif.Length < 13) throw new FormatException("Invalid GIF header.");
        if (!IsGif(gif)) throw new FormatException("Invalid GIF signature.");

        width = ReadUInt16LE(gif, 6);
        height = ReadUInt16LE(gif, 8);
        if (width <= 0 || height <= 0) throw new FormatException("Invalid GIF dimensions.");

        var packed = gif[10];
        var gctFlag = (packed & 0x80) != 0;
        var gctSize = 1 << ((packed & 0x07) + 1);
        var bgIndex = gif[11];

        var offset = 13;
        ReadOnlySpan<byte> globalTable = ReadOnlySpan<byte>.Empty;
        if (gctFlag) {
            var gctBytes = gctSize * 3;
            if (offset + gctBytes > gif.Length) throw new FormatException("Truncated GIF color table.");
            globalTable = gif.Slice(offset, gctBytes);
            offset += gctBytes;
        }

        var transparentIndex = -1;
        var hasTransparency = false;

        // Prepare background.
        var rgba = new byte[width * height * 4];
        if (!globalTable.IsEmpty && bgIndex < gctSize) {
            var p = bgIndex * 3;
            var br = globalTable[p + 0];
            var bg = globalTable[p + 1];
            var bb = globalTable[p + 2];
            for (var i = 0; i < width * height; i++) {
                var dst = i * 4;
                rgba[dst + 0] = br;
                rgba[dst + 1] = bg;
                rgba[dst + 2] = bb;
                rgba[dst + 3] = 255;
            }
        }

        while (offset < gif.Length) {
            var block = gif[offset++];
            if (block == 0x3B) {
                break; // trailer
            }

            if (block == 0x21) {
                // Extension
                if (offset >= gif.Length) break;
                var label = gif[offset++];
                if (label == 0xF9) {
                    // Graphics Control Extension
                    if (offset + 5 > gif.Length) throw new FormatException("Invalid GIF GCE.");
                    var blockSize = gif[offset++];
                    if (blockSize != 4) {
                        offset += blockSize;
                    } else {
                        var packedGce = gif[offset++];
                        var transparentFlag = (packedGce & 0x01) != 0;
                        offset += 2; // delay
                        transparentIndex = gif[offset++];
                        hasTransparency = transparentFlag;
                    }
                    if (offset < gif.Length && gif[offset] == 0) offset++;
                    continue;
                }

                SkipSubBlocks(gif, ref offset);
                continue;
            }

            if (block != 0x2C) {
                throw new FormatException("Unsupported GIF block.");
            }

            // Image descriptor
            if (offset + 9 > gif.Length) throw new FormatException("Invalid GIF image descriptor.");
            var left = ReadUInt16LE(gif, offset);
            var top = ReadUInt16LE(gif, offset + 2);
            var imgW = ReadUInt16LE(gif, offset + 4);
            var imgH = ReadUInt16LE(gif, offset + 6);
            var imgPacked = gif[offset + 8];
            offset += 9;

            var lctFlag = (imgPacked & 0x80) != 0;
            var interlaced = (imgPacked & 0x40) != 0;
            var lctSize = 1 << ((imgPacked & 0x07) + 1);

            ReadOnlySpan<byte> colorTable = globalTable;
            if (lctFlag) {
                var lctBytes = lctSize * 3;
                if (offset + lctBytes > gif.Length) throw new FormatException("Truncated GIF local color table.");
                colorTable = gif.Slice(offset, lctBytes);
                offset += lctBytes;
            }

            if (colorTable.IsEmpty) throw new FormatException("Missing GIF color table.");

            if (offset >= gif.Length) throw new FormatException("Missing GIF image data.");
            var minCodeSize = gif[offset++];

            var imageData = ReadSubBlocks(gif, ref offset);
            var pixels = new byte[imgW * imgH];
            var written = LzwDecode(imageData, minCodeSize, pixels);
            if (written < imgW * imgH) {
                // Leave remaining pixels as 0.
            }

            // Write into output buffer.
            if (!interlaced) {
                var idx = 0;
                for (var y = 0; y < imgH; y++) {
                    var dstRow = (top + y) * width * 4;
                    var xBase = left * 4;
                    for (var x = 0; x < imgW; x++) {
                        if ((uint)(top + y) >= (uint)height || (uint)(left + x) >= (uint)width) {
                            idx++;
                            continue;
                        }
                        var colorIndex = pixels[idx++];
                        WriteColor(colorTable, colorIndex, hasTransparency, transparentIndex, rgba, dstRow + xBase + x * 4);
                    }
                }
            } else {
                var idx = 0;
                WriteInterlaced(pixels, ref idx, imgW, imgH, left, top, width, height, colorTable, hasTransparency, transparentIndex, rgba);
            }

            // First frame only.
            return rgba;
        }

        return rgba;
    }

    internal static bool IsGif(ReadOnlySpan<byte> data) {
        if (data.Length < 6) return false;
        return data[0] == (byte)'G' && data[1] == (byte)'I' && data[2] == (byte)'F'
               && data[3] == (byte)'8' && (data[4] == (byte)'7' || data[4] == (byte)'9') && data[5] == (byte)'a';
    }

    private static void WriteInterlaced(
        byte[] pixels,
        ref int idx,
        int imgW,
        int imgH,
        int left,
        int top,
        int canvasW,
        int canvasH,
        ReadOnlySpan<byte> colorTable,
        bool hasTransparency,
        int transparentIndex,
        byte[] rgba) {
        var starts = new[] { 0, 4, 2, 1 };
        var steps = new[] { 8, 8, 4, 2 };
        for (var pass = 0; pass < 4; pass++) {
            for (var y = starts[pass]; y < imgH; y += steps[pass]) {
                var dstRow = (top + y) * canvasW * 4;
                var xBase = left * 4;
                for (var x = 0; x < imgW; x++) {
                    if (idx >= pixels.Length) return;
                    if ((uint)(top + y) >= (uint)canvasH || (uint)(left + x) >= (uint)canvasW) {
                        idx++;
                        continue;
                    }
                    var colorIndex = pixels[idx++];
                    WriteColor(colorTable, colorIndex, hasTransparency, transparentIndex, rgba, dstRow + xBase + x * 4);
                }
            }
        }
    }

    private static void WriteColor(ReadOnlySpan<byte> table, int index, bool hasTransparency, int transparentIndex, byte[] rgba, int dst) {
        if (hasTransparency && index == transparentIndex) {
            rgba[dst + 3] = 0;
            return;
        }
        var p = index * 3;
        if (p + 2 >= table.Length) return;
        rgba[dst + 0] = table[p + 0];
        rgba[dst + 1] = table[p + 1];
        rgba[dst + 2] = table[p + 2];
        rgba[dst + 3] = 255;
    }

    private static void SkipSubBlocks(ReadOnlySpan<byte> data, ref int offset) {
        while (offset < data.Length) {
            var size = data[offset++];
            if (size == 0) break;
            offset += size;
            if (offset > data.Length) throw new FormatException("Truncated GIF sub-block.");
        }
    }

    private static byte[] ReadSubBlocks(ReadOnlySpan<byte> data, ref int offset) {
        var total = 0;
        var start = offset;
        while (offset < data.Length) {
            var size = data[offset++];
            if (size == 0) break;
            offset += size;
            if (offset > data.Length) throw new FormatException("Truncated GIF sub-block.");
            total += size;
        }

        var output = new byte[total];
        var dst = 0;
        var pos = start;
        while (pos < data.Length) {
            var size = data[pos++];
            if (size == 0) break;
            data.Slice(pos, size).CopyTo(output.AsSpan(dst, size));
            dst += size;
            pos += size;
        }
        return output;
    }

    private static int LzwDecode(ReadOnlySpan<byte> data, int minCodeSize, byte[] output) {
        if (minCodeSize < 2 || minCodeSize > 11) throw new FormatException("Invalid GIF LZW code size.");
        var clearCode = 1 << minCodeSize;
        var endCode = clearCode + 1;
        var codeSize = minCodeSize + 1;
        var nextCode = endCode + 1;

        var prefix = new int[4096];
        var suffix = new byte[4096];
        var stack = new byte[4096];
        for (var i = 0; i < clearCode; i++) {
            prefix[i] = -1;
            suffix[i] = (byte)i;
        }

        var bitPos = 0;
        var outPos = 0;
        var oldCode = -1;
        var firstChar = 0;

        while (true) {
            var code = ReadCode(data, ref bitPos, codeSize);
            if (code < 0) break;

            if (code == clearCode) {
                codeSize = minCodeSize + 1;
                nextCode = endCode + 1;
                oldCode = -1;
                continue;
            }
            if (code == endCode) break;

            var stackSize = 0;
            if (oldCode == -1) {
                output[outPos++] = (byte)code;
                firstChar = code;
                oldCode = code;
                if (outPos >= output.Length) break;
                continue;
            }

            var inCode = code;
            if (code == nextCode) {
                // Special case.
                stack[stackSize++] = (byte)firstChar;
                code = oldCode;
            }

            while (code >= clearCode) {
                stack[stackSize++] = suffix[code];
                code = prefix[code];
            }
            firstChar = code;
            stack[stackSize++] = (byte)code;

            for (var i = stackSize - 1; i >= 0; i--) {
                output[outPos++] = stack[i];
                if (outPos >= output.Length) break;
            }

            if (outPos >= output.Length) break;

            prefix[nextCode] = oldCode;
            suffix[nextCode] = (byte)firstChar;
            nextCode++;

            if (nextCode == (1 << codeSize) && codeSize < 12) {
                codeSize++;
            }

            oldCode = inCode;
        }

        return outPos;
    }

    private static int ReadCode(ReadOnlySpan<byte> data, ref int bitPos, int codeSize) {
        var code = 0;
        for (var i = 0; i < codeSize; i++) {
            var byteIndex = bitPos >> 3;
            if (byteIndex >= data.Length) return -1;
            var bit = (data[byteIndex] >> (bitPos & 7)) & 1;
            code |= bit << i;
            bitPos++;
        }
        return code;
    }

    private static ushort ReadUInt16LE(ReadOnlySpan<byte> data, int offset) {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }
}

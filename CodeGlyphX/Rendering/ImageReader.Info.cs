using System;
using System.IO;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Webp;

namespace CodeGlyphX.Rendering;

public static partial class ImageReader {
    private const int MaxNetpbmDimension = 16384;

    /// <summary>
    /// Attempts to read image format and dimensions without decoding pixels.
    /// </summary>
    public static bool TryReadInfo(byte[] data, out ImageInfo info) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryReadInfo((ReadOnlySpan<byte>)data, out info);
    }

    /// <summary>
    /// Attempts to read image format and dimensions for a given page without decoding pixels.
    /// </summary>
    public static bool TryReadInfo(byte[] data, int pageIndex, out ImageInfo info) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryReadInfo((ReadOnlySpan<byte>)data, pageIndex, out info);
    }

    /// <summary>
    /// Attempts to read image format and dimensions without decoding pixels.
    /// </summary>
    public static bool TryReadInfo(ReadOnlySpan<byte> data, out ImageInfo info) {
        info = default;
        if (!TryDetectFormat(data, out var format)) return false;
        if (!TryReadDimensions(data, format, out var width, out var height)) return false;
        info = new ImageInfo(format, width, height);
        return true;
    }

    /// <summary>
    /// Attempts to read image format and dimensions for a given page without decoding pixels.
    /// </summary>
    public static bool TryReadInfo(ReadOnlySpan<byte> data, int pageIndex, out ImageInfo info) {
        info = default;
        if (pageIndex < 0) return false;
        if (!TryDetectFormat(data, out var format)) return false;
        if (pageIndex == 0) return TryReadInfo(data, out info);
        if (format != ImageFormat.Tiff) return false;
        if (!TryReadTiffSize(data, pageIndex, out var width, out var height)) return false;
        info = new ImageInfo(format, width, height);
        return true;
    }

    /// <summary>
    /// Attempts to read image format and dimensions from a stream without decoding pixels.
    /// </summary>
    public static bool TryReadInfo(Stream stream, out ImageInfo info) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryReadInfo(buffer.AsSpan(), out info);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryReadInfo(data, out info);
    }

    /// <summary>
    /// Attempts to read animation format info without decoding pixels.
    /// </summary>
    public static bool TryReadAnimationInfo(byte[] data, out ImageAnimationInfo info) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryReadAnimationInfo((ReadOnlySpan<byte>)data, out info);
    }

    /// <summary>
    /// Attempts to read animation format info without decoding pixels.
    /// </summary>
    public static bool TryReadAnimationInfo(ReadOnlySpan<byte> data, out ImageAnimationInfo info) {
        info = default;
        if (!TryDetectFormat(data, out var format)) return false;
        switch (format) {
            case ImageFormat.Gif:
                return TryReadGifAnimationInfo(data, out info);
            case ImageFormat.Webp:
                return TryReadWebpAnimationInfo(data, out info);
            default:
                return false;
        }
    }

    /// <summary>
    /// Attempts to read animation format info from a stream without decoding pixels.
    /// </summary>
    public static bool TryReadAnimationInfo(Stream stream, out ImageAnimationInfo info) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryReadAnimationInfo(buffer.AsSpan(), out info);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryReadAnimationInfo(data, out info);
    }

    /// <summary>
    /// Attempts to read image format and dimensions for a given page from a stream without decoding pixels.
    /// </summary>
    public static bool TryReadInfo(Stream stream, int pageIndex, out ImageInfo info) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryReadInfo(buffer.AsSpan(), pageIndex, out info);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryReadInfo(data, pageIndex, out info);
    }

    /// <summary>
    /// Attempts to read page count for multipage images (TIFF).
    /// </summary>
    public static bool TryReadPageCount(ReadOnlySpan<byte> data, out int pageCount) {
        pageCount = 0;
        if (!TiffReader.IsTiff(data)) return false;
        return TryReadTiffPageCount(data, out pageCount);
    }

    /// <summary>
    /// Attempts to read page count for multipage images (TIFF) from a byte buffer.
    /// </summary>
    public static bool TryReadPageCount(byte[] data, out int pageCount) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return TryReadPageCount((ReadOnlySpan<byte>)data, out pageCount);
    }

    /// <summary>
    /// Attempts to read page count for multipage images (TIFF) from a stream.
    /// </summary>
    public static bool TryReadPageCount(Stream stream, out int pageCount) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return TryReadPageCount(buffer.AsSpan(), out pageCount);
        }
        var data = RenderIO.ReadBinary(stream);
        return TryReadPageCount(data, out pageCount);
    }

    private static bool TryReadDimensions(ReadOnlySpan<byte> data, ImageFormat format, out int width, out int height) {
        width = 0;
        height = 0;
        switch (format) {
            case ImageFormat.Webp:
                return WebpReader.TryReadDimensions(data, out width, out height);
            case ImageFormat.Png:
                return TryReadPngSize(data, out width, out height);
            case ImageFormat.Jpeg:
                return TryReadJpegSize(data, out width, out height);
            case ImageFormat.Gif:
                return TryReadGifSize(data, out width, out height);
            case ImageFormat.Bmp:
                return TryReadBmpSize(data, out width, out height);
            case ImageFormat.Pbm:
                return TryReadPbmSize(data, out width, out height);
            case ImageFormat.Pgm:
                return TryReadPgmSize(data, out width, out height);
            case ImageFormat.Ppm:
                return TryReadPpmSize(data, out width, out height);
            case ImageFormat.Pam:
                return TryReadPamSize(data, out width, out height);
            case ImageFormat.Tga:
                return TryReadTgaSize(data, out width, out height);
            case ImageFormat.Tiff:
                return TryReadTiffSize(data, out width, out height);
            case ImageFormat.Xpm:
                return TryReadXpmSize(data, out width, out height);
            case ImageFormat.Xbm:
                return TryReadXbmSize(data, out width, out height);
            case ImageFormat.Ico:
                return TryReadIcoSize(data, out width, out height);
            default:
                return false;
        }
    }

    private static bool TryReadPngSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (data.Length < 24) return false;
        if (!IsPng(data)) return false;
        if (data[12] != (byte)'I' || data[13] != (byte)'H' || data[14] != (byte)'D' || data[15] != (byte)'R') return false;
        width = (int)ReadUInt32BE(data, 16);
        height = (int)ReadUInt32BE(data, 20);
        return width > 0 && height > 0;
    }

    private static bool TryReadJpegSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!Rendering.Jpeg.JpegReader.IsJpeg(data)) return false;
        return TryFindJpegFrameSize(data, out width, out height);
    }

    private static bool TryFindJpegFrameSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        var offset = 2;
        while (TryReadNextJpegMarker(data, ref offset, out var marker)) {
            if (marker == 0xD9 || marker == 0xDA) return false;
            if (IsRestartMarker(marker)) continue;
            if (!TryReadJpegSegmentLength(data, offset, out var length)) return false;
            if (IsStartOfFrame(marker)) return TryReadJpegFrameDimensions(data, offset, length, out width, out height);
            offset += length;
        }
        return false;
    }

    private static bool TryReadNextJpegMarker(ReadOnlySpan<byte> data, ref int offset, out byte marker) {
        marker = 0;
        while (offset + 1 < data.Length) {
            if (data[offset] != 0xFF) {
                offset++;
                continue;
            }
            while (offset < data.Length && data[offset] == 0xFF) offset++;
            if (offset >= data.Length) return false;
            marker = data[offset++];
            return true;
        }
        return false;
    }

    private static bool TryReadJpegSegmentLength(ReadOnlySpan<byte> data, int offset, out ushort length) {
        length = 0;
        if (offset + 1 >= data.Length) return false;
        length = ReadUInt16BE(data, offset);
        if (length < 2) return false;
        if (offset + length > data.Length) return false;
        return true;
    }

    private static bool TryReadJpegFrameDimensions(ReadOnlySpan<byte> data, int offset, ushort length, out int width, out int height) {
        width = 0;
        height = 0;
        if (length < 7) return false;
        height = ReadUInt16BE(data, offset + 3);
        width = ReadUInt16BE(data, offset + 5);
        return width > 0 && height > 0;
    }

    private static bool IsStartOfFrame(byte marker) {
        return marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
    }

    private static bool IsRestartMarker(byte marker) {
        return marker >= 0xD0 && marker <= 0xD7;
    }

    private static bool TryReadGifSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!GifReader.IsGif(data)) return false;
        if (data.Length < 10) return false;
        width = ReadUInt16LE(data, 6);
        height = ReadUInt16LE(data, 8);
        return width > 0 && height > 0;
    }

    private static bool TryReadBmpSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (data.Length < 26) return false;
        if (data[0] != (byte)'B' || data[1] != (byte)'M') return false;
        if (data.Length < 18) return false;
        var headerSize = (int)ReadUInt32LE(data, 14);
        if (headerSize < 12) return false;
        if (headerSize == 12) {
            if (data.Length < 22) return false;
            width = ReadUInt16LE(data, 18);
            height = ReadUInt16LE(data, 20);
        } else {
            if (data.Length < 26) return false;
            width = ReadInt32LE(data, 18);
            height = ReadInt32LE(data, 22);
            if (height < 0) height = -height;
        }
        return width > 0 && height > 0;
    }

    private static bool TryReadTgaSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!TgaReader.LooksLikeTga(data)) return false;
        width = ReadUInt16LE(data, 12);
        height = ReadUInt16LE(data, 14);
        return width > 0 && height > 0;
    }

    private static bool TryReadTiffSize(ReadOnlySpan<byte> data, out int width, out int height) {
        return TryReadTiffSize(data, pageIndex: 0, out width, out height);
    }

    private static bool TryReadTiffSize(ReadOnlySpan<byte> data, int pageIndex, out int width, out int height) {
        width = 0;
        height = 0;
        if (!TiffReader.IsTiff(data)) return false;
        if (pageIndex < 0) return false;

        var little = data[0] == (byte)'I';
        var ifdOffset = ReadUInt32LE(data, 4, little);
        if (ifdOffset > data.Length - 2) return false;

        var currentOffset = ifdOffset;
        for (var page = 0; page <= pageIndex; page++) {
            if (!TryReadTiffIfd(data, little, currentOffset, out var nextOffset, out var pageWidth, out var pageHeight)) return false;
            if (page == pageIndex) {
                width = pageWidth;
                height = pageHeight;
                return width > 0 && height > 0;
            }
            if (nextOffset == 0) return false;
            currentOffset = nextOffset;
        }

        return false;
    }

    private static bool TryReadTiffPageCount(ReadOnlySpan<byte> data, out int pageCount) {
        pageCount = 0;
        if (!TiffReader.IsTiff(data)) return false;
        var little = data[0] == (byte)'I';
        var ifdOffset = ReadUInt32LE(data, 4, little);
        if (ifdOffset > data.Length - 2) return false;

        var maxPages = Math.Max(1, data.Length / 12);
        var currentOffset = ifdOffset;
        var lastOffset = 0u;
        while (currentOffset != 0 && pageCount < maxPages) {
            if (currentOffset == lastOffset) return false;
            if (!TryReadTiffIfd(data, little, currentOffset, out var nextOffset, out _, out _)) return false;
            pageCount++;
            lastOffset = currentOffset;
            currentOffset = nextOffset;
        }
        return pageCount > 0;
    }

    private static bool TryReadTiffIfd(ReadOnlySpan<byte> data, bool little, uint ifdOffset, out uint nextOffset, out int width, out int height) {
        width = 0;
        height = 0;
        nextOffset = 0;
        if (ifdOffset == 0 || ifdOffset > data.Length - 2) return false;

        var entryCount = ReadUInt16LE(data, (int)ifdOffset, little);
        var entriesOffset = (int)ifdOffset + 2;
        var maxEntries = Math.Min(entryCount, (ushort)((data.Length - entriesOffset) / 12));

        for (var i = 0; i < maxEntries; i++) {
            var entryOffset = entriesOffset + i * 12;
            var tag = ReadUInt16LE(data, entryOffset, little);
            var type = ReadUInt16LE(data, entryOffset + 2, little);
            var count = ReadUInt32LE(data, entryOffset + 4, little);
            if (!TryGetTiffValueSpan(data, entryOffset, little, type, count, out var valueSpan)) continue;

            if (tag == 256) {
                width = (int)ReadTiffValue(valueSpan, type, little, 0);
            } else if (tag == 257) {
                height = (int)ReadTiffValue(valueSpan, type, little, 0);
            }
        }

        var nextOffsetPos = entriesOffset + entryCount * 12;
        if (nextOffsetPos + 4 <= data.Length) {
            nextOffset = ReadUInt32LE(data, nextOffsetPos, little);
        }

        return true;
    }

    private static bool TryReadPbmSize(ReadOnlySpan<byte> data, out int width, out int height) {
        return TryReadNetpbmSize(data, (byte)'1', (byte)'4', includeMaxVal: false, out width, out height);
    }

    private static bool TryReadPgmSize(ReadOnlySpan<byte> data, out int width, out int height) {
        return TryReadNetpbmSize(data, (byte)'2', (byte)'5', includeMaxVal: true, out width, out height);
    }

    private static bool TryReadPpmSize(ReadOnlySpan<byte> data, out int width, out int height) {
        return TryReadNetpbmSize(data, (byte)'3', (byte)'6', includeMaxVal: true, out width, out height);
    }

    private static bool TryReadNetpbmSize(ReadOnlySpan<byte> data, byte asciiMagic, byte rawMagic, bool includeMaxVal, out int width, out int height) {
        width = 0;
        height = 0;
        if (data.Length < 2) return false;
        if (data[0] != (byte)'P') return false;
        if (data[1] != asciiMagic && data[1] != rawMagic) return false;

        var pos = 2;
        if (!TryReadIntToken(data, ref pos, out width)) return false;
        if (!TryReadIntToken(data, ref pos, out height)) return false;
        if (!IsValidNetpbmDimensions(width, height)) return false;
        if (includeMaxVal) {
            if (!TryReadIntToken(data, ref pos, out var maxVal)) return false;
            if (maxVal <= 0) return false;
        }
        return true;
    }

    private static bool TryReadGifAnimationInfo(ReadOnlySpan<byte> data, out ImageAnimationInfo info) {
        info = default;
        if (data.Length < 13) return false;
        if (!IsGifHeader(data)) return false;

        var width = ReadUInt16LE(data, 6);
        var height = ReadUInt16LE(data, 8);
        if (width <= 0 || height <= 0) return false;

        var packed = data[10];
        var hasGlobalColorTable = (packed & 0x80) != 0;
        var backgroundIndex = data[11];
        var globalColorTableSize = hasGlobalColorTable ? 3 * (1 << ((packed & 0x07) + 1)) : 0;
        var offset = 13 + globalColorTableSize;
        if (offset > data.Length) return false;

        uint backgroundRgba = 0;
        if (hasGlobalColorTable) {
            var tableEntries = globalColorTableSize / 3;
            if (backgroundIndex < tableEntries) {
                var colorOffset = 13 + backgroundIndex * 3;
                if (colorOffset + 2 < data.Length) {
                    var r = data[colorOffset];
                    var g = data[colorOffset + 1];
                    var b = data[colorOffset + 2];
                    backgroundRgba = (uint)(r | (g << 8) | (b << 16) | (255u << 24));
                }
            }
        }

        var loopCount = 0;
        var frameCount = 0;

        while (offset < data.Length) {
            var introducer = data[offset++];
            if (introducer == 0x3B) break;

            if (introducer == 0x2C) {
                if (offset + 9 > data.Length) return false;
                var localPacked = data[offset + 8];
                offset += 9;
                if ((localPacked & 0x80) != 0) {
                    var localSize = 3 * (1 << ((localPacked & 0x07) + 1));
                    offset += localSize;
                    if (offset > data.Length) return false;
                }
                if (offset >= data.Length) return false;
                offset++; // LZW minimum code size
                if (!SkipGifSubBlocks(data, ref offset)) return false;
                frameCount++;
                continue;
            }

            if (introducer == 0x21) {
                if (offset >= data.Length) return false;
                var label = data[offset++];
                if (label == 0xFF) {
                    if (offset >= data.Length) return false;
                    var blockSize = data[offset++];
                    if (offset + blockSize > data.Length) return false;
                    var isNetscape = blockSize >= 11 && IsGifAppId(data.Slice(offset, blockSize));
                    offset += blockSize;
                    if (!ReadGifApplicationBlocks(data, ref offset, isNetscape, ref loopCount)) return false;
                } else {
                    if (!SkipGifSubBlocks(data, ref offset)) return false;
                }
                continue;
            }

            return false;
        }

        if (frameCount <= 0) return false;
        info = new ImageAnimationInfo(ImageFormat.Gif, width, height, frameCount, new ImageAnimationOptions(loopCount, backgroundRgba));
        return true;
    }

    private static bool IsGifHeader(ReadOnlySpan<byte> data) {
        if (data.Length < 6) return false;
        return (data[0] == (byte)'G' && data[1] == (byte)'I' && data[2] == (byte)'F'
            && data[3] == (byte)'8' && (data[4] == (byte)'7' || data[4] == (byte)'9') && data[5] == (byte)'a');
    }

    private static bool SkipGifSubBlocks(ReadOnlySpan<byte> data, ref int offset) {
        while (offset < data.Length) {
            var size = data[offset++];
            if (size == 0) return true;
            offset += size;
            if (offset > data.Length) return false;
        }
        return false;
    }

    private static bool IsGifAppId(ReadOnlySpan<byte> block) {
        if (block.Length < 11) return false;
        return block[0] == (byte)'N' && block[1] == (byte)'E' && block[2] == (byte)'T'
            && block[3] == (byte)'S' && block[4] == (byte)'C' && block[5] == (byte)'A'
            && block[6] == (byte)'P' && block[7] == (byte)'E' && block[8] == (byte)'2'
            && block[9] == (byte)'.' && block[10] == (byte)'0';
    }

    private static bool ReadGifApplicationBlocks(ReadOnlySpan<byte> data, ref int offset, bool isNetscape, ref int loopCount) {
        while (offset < data.Length) {
            var size = data[offset++];
            if (size == 0) return true;
            if (offset + size > data.Length) return false;
            if (isNetscape && size >= 3 && data[offset] == 1) {
                loopCount = ReadUInt16LE(data, offset + 1, little: true);
            }
            offset += size;
        }
        return false;
    }

    private static bool TryReadWebpAnimationInfo(ReadOnlySpan<byte> data, out ImageAnimationInfo info) {
        info = default;
        if (!WebpReader.IsWebp(data)) return false;
        if (!WebpReader.TryReadDimensions(data, out var width, out var height)) return false;
        if (!TryReadWebpAnimationChunks(data, out var frameCount, out var loopCount, out var backgroundRgba)) return false;
        if (frameCount <= 0) return false;
        info = new ImageAnimationInfo(ImageFormat.Webp, width, height, frameCount, new ImageAnimationOptions(loopCount, backgroundRgba));
        return true;
    }

    private static bool TryReadWebpAnimationChunks(ReadOnlySpan<byte> data, out int frameCount, out int loopCount, out uint backgroundRgba) {
        frameCount = 0;
        loopCount = 0;
        backgroundRgba = 0;

        if (data.Length < 12) return false;
        if (!IsFourCc(data, 0, "RIFF") || !IsFourCc(data, 8, "WEBP")) return false;

        var offset = 12;
        while (offset + 8 <= data.Length) {
            var chunkSize = ReadUInt32LE(data, offset + 4, little: true);
            var payloadOffset = offset + 8;
            if (payloadOffset + chunkSize > data.Length) return false;

            if (IsFourCc(data, offset, "ANMF")) {
                frameCount++;
            } else if (IsFourCc(data, offset, "ANIM")) {
                if (chunkSize >= 6) {
                    var bgra = ReadUInt32LE(data, payloadOffset, little: true);
                    backgroundRgba = ConvertBgraToRgba(bgra);
                    loopCount = ReadUInt16LE(data, payloadOffset + 4, little: true);
                }
            }

            var padded = chunkSize + (chunkSize & 1);
            var next = payloadOffset + (int)padded;
            if (next < 0 || next > data.Length) return false;
            offset = next;
        }

        return true;
    }

    private static bool IsFourCc(ReadOnlySpan<byte> data, int offset, string fourCc) {
        if (offset < 0 || offset + 4 > data.Length) return false;
        return data[offset] == (byte)fourCc[0]
            && data[offset + 1] == (byte)fourCc[1]
            && data[offset + 2] == (byte)fourCc[2]
            && data[offset + 3] == (byte)fourCc[3];
    }

    private static uint ConvertBgraToRgba(uint bgra) {
        var b = (byte)(bgra & 0xFF);
        var g = (byte)((bgra >> 8) & 0xFF);
        var r = (byte)((bgra >> 16) & 0xFF);
        var a = (byte)((bgra >> 24) & 0xFF);
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }

    private static bool TryReadPamSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (data.Length < 2 || data[0] != (byte)'P' || data[1] != (byte)'7') return false;
        var pos = 2;
        if (!TryReadPamHeader(data, ref pos, out width, out height)) return false;
        return IsValidNetpbmDimensions(width, height);
    }

    private static bool TryReadXbmSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        var text = System.Text.Encoding.ASCII.GetString(data.ToArray());
        width = ExtractDefineValue(text, "_width");
        height = ExtractDefineValue(text, "_height");
        return width > 0 && height > 0;
    }

    private static bool TryReadXpmSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        var text = System.Text.Encoding.ASCII.GetString(data.ToArray());
        var strings = ExtractQuotedStrings(text);
        if (strings.Count == 0) return false;
        var headerParts = strings[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (headerParts.Length < 2) return false;
        if (!int.TryParse(headerParts[0], out width)) return false;
        if (!int.TryParse(headerParts[1], out height)) return false;
        if (width <= 0 || height <= 0) return false;
        return width <= MaxNetpbmDimension && height <= MaxNetpbmDimension;
    }

    private static bool TryReadIcoSize(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!IcoReader.IsIco(data)) return false;
        if (data.Length < 6) return false;
        var count = ReadUInt16LE(data, 4);
        if (count <= 0) return false;
        if (data.Length < 6 + count * 16) return false;

        var bestArea = 0;
        for (var i = 0; i < count; i++) {
            var entry = 6 + i * 16;
            var w = data[entry + 0];
            var h = data[entry + 1];
            var widthPx = w == 0 ? 256 : w;
            var heightPx = h == 0 ? 256 : h;
            var area = widthPx * heightPx;
            if (area > bestArea) {
                bestArea = area;
                width = widthPx;
                height = heightPx;
            }
        }

        return width > 0 && height > 0;
    }

    private static ushort ReadUInt16BE(ReadOnlySpan<byte> data, int offset) {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static ushort ReadUInt16LE(ReadOnlySpan<byte> data, int offset) {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static ushort ReadUInt16LE(ReadOnlySpan<byte> data, int offset, bool little) {
        return little ? ReadUInt16LE(data, offset) : ReadUInt16BE(data, offset);
    }

    private static uint ReadUInt32BE(ReadOnlySpan<byte> data, int offset) {
        return (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
    }

    private static uint ReadUInt32LE(ReadOnlySpan<byte> data, int offset) {
        return (uint)(data[offset]
                      | (data[offset + 1] << 8)
                      | (data[offset + 2] << 16)
                      | (data[offset + 3] << 24));
    }

    private static uint ReadUInt32LE(ReadOnlySpan<byte> data, int offset, bool little) {
        return little ? ReadUInt32LE(data, offset) : ReadUInt32BE(data, offset);
    }

    private static int ReadInt32LE(ReadOnlySpan<byte> data, int offset) {
        return unchecked((int)ReadUInt32LE(data, offset));
    }

    private static bool TryReadIntToken(ReadOnlySpan<byte> data, ref int pos, out int value) {
        value = 0;
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) return false;

        var sawDigit = false;
        while (pos < data.Length) {
            var c = data[pos];
            if (c < (byte)'0' || c > (byte)'9') break;
            sawDigit = true;
            if (value > (int.MaxValue - 9) / 10) return false;
            value = value * 10 + (c - (byte)'0');
            pos++;
        }
        return sawDigit;
    }

    private static void SkipWhitespaceAndComments(ReadOnlySpan<byte> data, ref int pos) {
        while (pos < data.Length) {
            var c = data[pos];
            if (c == (byte)'#') {
                pos++;
                while (pos < data.Length && data[pos] != (byte)'\n' && data[pos] != (byte)'\r') pos++;
                continue;
            }
            if (c <= 32) {
                pos++;
                continue;
            }
            break;
        }
    }

    private static bool IsToken(ReadOnlySpan<byte> data, int pos, string token) {
        if (pos + token.Length > data.Length) return false;
        for (var i = 0; i < token.Length; i++) {
            var c = data[pos + i];
            if (c == token[i] || c == token[i] - 32) continue;
            return false;
        }
        return true;
    }

    private static string ReadToken(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) return string.Empty;
        var start = pos;
        while (pos < data.Length) {
            var c = data[pos];
            if (c <= 32) break;
            pos++;
        }
        if (pos == start) return string.Empty;
        return System.Text.Encoding.ASCII.GetString(data.Slice(start, pos - start).ToArray());
    }

    private static int ExtractDefineValue(string text, string suffix) {
        var idx = text.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
        while (idx >= 0) {
            var lineStart = text.LastIndexOf('\n', idx);
            if (lineStart < 0) {
                lineStart = 0;
            } else {
                lineStart += 1;
            }
            var lineEnd = text.IndexOf('\n', idx);
            if (lineEnd < 0) lineEnd = text.Length;
            var line = text.Substring(lineStart, lineEnd - lineStart);
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3
                && parts[0].Equals("#define", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[2], out var value)) {
                return value;
            }
            idx = text.IndexOf(suffix, idx + suffix.Length, StringComparison.OrdinalIgnoreCase);
        }
        return 0;
    }

    private static System.Collections.Generic.List<string> ExtractQuotedStrings(string text) {
        var list = new System.Collections.Generic.List<string>();
        var sb = new System.Text.StringBuilder();
        var inString = false;
        var escape = false;
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (!inString) {
                if (c == '"') {
                    inString = true;
                    sb.Clear();
                }
                continue;
            }

            if (escape) {
                sb.Append(c);
                escape = false;
                continue;
            }

            if (c == '\\' && i + 1 < text.Length) {
                escape = true;
                continue;
            }
            if (c == '"') {
                inString = false;
                list.Add(sb.ToString());
                continue;
            }
            sb.Append(c);
        }
        return list;
    }

    private static bool IsValidNetpbmDimensions(int width, int height) {
        if (width <= 0 || height <= 0) return false;
        return width <= MaxNetpbmDimension && height <= MaxNetpbmDimension;
    }

    private static bool TryReadPamHeader(ReadOnlySpan<byte> data, ref int pos, out int width, out int height) {
        width = 0;
        height = 0;
        while (true) {
            SkipWhitespaceAndComments(data, ref pos);
            if (pos >= data.Length) return false;
            if (IsToken(data, pos, "ENDHDR")) return true;
            var key = ReadToken(data, ref pos);
            if (key.Length == 0) return false;
            if (key.Equals("WIDTH", StringComparison.OrdinalIgnoreCase)) {
                if (!TryReadIntToken(data, ref pos, out width)) return false;
                continue;
            }
            if (key.Equals("HEIGHT", StringComparison.OrdinalIgnoreCase)) {
                if (!TryReadIntToken(data, ref pos, out height)) return false;
                continue;
            }
            if (!SkipToken(data, ref pos)) return false;
        }
    }

    private static bool SkipToken(ReadOnlySpan<byte> data, ref int pos) {
        SkipWhitespaceAndComments(data, ref pos);
        if (pos >= data.Length) return false;
        var start = pos;
        while (pos < data.Length) {
            var c = data[pos];
            if (c <= 32) break;
            pos++;
        }
        return pos > start;
    }

    private static bool TryGetTiffValueSpan(ReadOnlySpan<byte> data, int entryOffset, bool little, ushort type, uint count, out ReadOnlySpan<byte> valueSpan) {
        valueSpan = default;
        var typeSize = GetTiffTypeSize(type);
        if (typeSize == 0) return false;
        var total = checked((int)(count * (uint)typeSize));
        var valueOffset = entryOffset + 8;
        if (total <= 4) {
            if (valueOffset + total > data.Length) return false;
            valueSpan = data.Slice(valueOffset, total);
            return true;
        }
        var offset = (int)ReadUInt32LE(data, valueOffset, little);
        if (offset < 0 || offset + total > data.Length) return false;
        valueSpan = data.Slice(offset, total);
        return true;
    }

    private static int GetTiffTypeSize(ushort type) {
        return type switch {
            1 => 1,
            3 => 2,
            4 => 4,
            _ => 0
        };
    }

    private static uint ReadTiffValue(ReadOnlySpan<byte> span, ushort type, bool little, int index) {
        return type switch {
            1 => span[index],
            3 => ReadUInt16LE(span, index * 2, little),
            4 => ReadUInt32LE(span, index * 4, little),
            _ => 0
        };
    }
}

using System;
using System.IO;
using System.IO.Compression;
using CodeGlyphX.Rendering.Jpeg;

namespace CodeGlyphX.Rendering.Pdf;

/// <summary>
/// Minimal PDF image decoder (image-only PDFs with embedded JPEG/Flate XObjects).
/// </summary>
public static class PdfReader {
    private static readonly byte[] PdfSignature = { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-' };
    private static readonly byte[] SubtypeToken = { (byte)'/', (byte)'S', (byte)'u', (byte)'b', (byte)'t', (byte)'y', (byte)'p', (byte)'e' };
    private static readonly byte[] ImageToken = { (byte)'/', (byte)'I', (byte)'m', (byte)'a', (byte)'g', (byte)'e' };
    private static readonly byte[] StreamToken = { (byte)'s', (byte)'t', (byte)'r', (byte)'e', (byte)'a', (byte)'m' };
    private static readonly byte[] EndStreamToken = { (byte)'e', (byte)'n', (byte)'d', (byte)'s', (byte)'t', (byte)'r', (byte)'e', (byte)'a', (byte)'m' };

    /// <summary>
    /// Returns true when the buffer looks like a PDF file.
    /// </summary>
    public static bool IsPdf(ReadOnlySpan<byte> data) {
        if (data.Length < PdfSignature.Length) return false;
        for (var i = 0; i < PdfSignature.Length; i++) {
            if (data[i] != PdfSignature[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Attempts to read PDF image dimensions without decoding pixels.
    /// </summary>
    public static bool TryReadDimensions(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!IsPdf(data)) return false;
        var offset = 0;
        while (TryFindImage(data, ref offset, out var info, out _)) {
            if (info.Width > 0 && info.Height > 0) {
                width = info.Width;
                height = info.Height;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Attempts to decode an image-only PDF to RGBA (embedded JPEG or Flate image XObject).
    /// </summary>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;
        if (!IsPdf(data)) return false;

        var offset = 0;
        while (TryFindImage(data, ref offset, out var info, out var stream)) {
            if (info.Filter is null) {
                if (TryDecodeFlate(info, stream, out rgba, out width, out height)) {
                    return true;
                }
                try {
                    rgba = JpegReader.DecodeRgba32(stream, out width, out height);
                    return true;
                } catch (FormatException) {
                    continue;
                }
            }
            if (info.Filter.Equals("DCTDecode", StringComparison.OrdinalIgnoreCase) ||
                info.Filter.Equals("DCT", StringComparison.OrdinalIgnoreCase)) {
                try {
                    rgba = JpegReader.DecodeRgba32(stream, out width, out height);
                    return true;
                } catch (FormatException) {
                    continue;
                }
            }

            if (info.Filter.Equals("FlateDecode", StringComparison.OrdinalIgnoreCase) ||
                info.Filter.Equals("Fl", StringComparison.OrdinalIgnoreCase)) {
                if (!TryDecodeFlate(info, stream, out rgba, out width, out height)) {
                    continue;
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Decodes an image-only PDF to RGBA (embedded JPEG or Flate image XObject).
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (!TryDecodeRgba32(data, out var rgba, out width, out height)) {
            throw new FormatException("Unsupported or invalid PDF/PS image.");
        }
        return rgba;
    }

    private static bool TryFindImage(ReadOnlySpan<byte> data, ref int offset, out PdfImageInfo info, out ReadOnlySpan<byte> stream) {
        info = default;
        stream = ReadOnlySpan<byte>.Empty;
        while (offset < data.Length) {
            var imageIdx = IndexOfToken(data, ImageToken, offset);
            if (imageIdx < 0) return false;

            var dictStart = LastIndexOfToken(data, (byte)'<', (byte)'<', imageIdx);
            var dictEnd = IndexOfToken(data, (byte)'>', (byte)'>', imageIdx);
            if (dictStart < 0 || dictEnd < 0 || dictEnd <= dictStart) {
                offset = imageIdx + ImageToken.Length;
                continue;
            }

            var dict = data.Slice(dictStart, dictEnd + 2 - dictStart);
            if (IndexOfToken(dict, SubtypeToken, 0) < 0) {
                offset = imageIdx + ImageToken.Length;
                continue;
            }

            if (!TryParseImageInfo(dict, out info)) {
                offset = dictEnd + 2;
                continue;
            }

            if (!TryReadStream(data, dictEnd + 2, info.StreamLength, out stream, out var endOffset)) {
                offset = dictEnd + 2;
                continue;
            }

            offset = endOffset;
            return true;
        }
        return false;
    }

    private static bool TryParseImageInfo(ReadOnlySpan<byte> dict, out PdfImageInfo info) {
        info = default;
        if (!TryReadNumberAfterKey(dict, "/Width", out var width)) return false;
        if (!TryReadNumberAfterKey(dict, "/Height", out var height)) return false;

        var bits = 8;
        if (TryReadNumberAfterKey(dict, "/BitsPerComponent", out var bpc)) {
            bits = bpc;
        }

        string? filter = null;
        if (TryReadNameAfterKey(dict, "/Filter", out var filterName)) {
            filter = filterName;
        }

        var length = 0;
        TryReadNumberAfterKey(dict, "/Length", out length);

        string? colorSpace = null;
        if (TryReadNameAfterKey(dict, "/ColorSpace", out var csName)) {
            colorSpace = csName;
        }

        var predictor = 1;
        if (TryReadNumberAfterKey(dict, "/Predictor", out var predictorValue)) {
            predictor = predictorValue;
        }

        var colors = 0;
        if (TryReadNumberAfterKey(dict, "/Colors", out var colorsValue)) {
            colors = colorsValue;
        }

        info = new PdfImageInfo(width, height, bits, colors, colorSpace, filter, predictor, length);
        return true;
    }

    private static bool TryDecodeFlate(PdfImageInfo info, ReadOnlySpan<byte> stream, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (info.Width <= 0 || info.Height <= 0) return false;
        if (info.BitsPerComponent != 8) return false;

        var colors = info.Colors;
        if (colors <= 0) {
            if (string.Equals(info.ColorSpace, "DeviceRGB", StringComparison.OrdinalIgnoreCase)) colors = 3;
            else if (string.Equals(info.ColorSpace, "DeviceGray", StringComparison.OrdinalIgnoreCase)) colors = 1;
        }
        if (colors != 0 && colors != 1 && colors != 3) return false;

        var predictor = info.Predictor <= 0 ? 1 : info.Predictor;
        byte[] inflated;
        try {
            inflated = DecompressFlateAll(stream);
        } catch (FormatException) {
            return false;
        }

        if (colors == 0) {
            if (!TryInferColorsFromLength(inflated.Length, info.Width, info.Height, predictor, out colors)) {
                return false;
            }
        } else {
            var expectedRowSize = checked(info.Width * colors);
            var expected = predictor >= 10 ? checked((expectedRowSize + 1) * info.Height) : checked(expectedRowSize * info.Height);
            if (inflated.Length != expected) {
                if (!TryInferColorsFromLength(inflated.Length, info.Width, info.Height, predictor, out colors)) {
                    return false;
                }
            }
        }

        var rowSize = checked(info.Width * colors);
        if (predictor == 2) {
            ApplyTiffPredictor(inflated, rowSize, info.Height, colors);
        } else if (predictor >= 10) {
            if (!TryApplyPngPredictor(inflated, info.Width, info.Height, colors, out var decoded)) return false;
            inflated = decoded;
        }

        var pixelCount = info.Width * info.Height;
        rgba = new byte[pixelCount * 4];
        if (colors == 3) {
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 3;
                var dst = i * 4;
                rgba[dst + 0] = inflated[src + 0];
                rgba[dst + 1] = inflated[src + 1];
                rgba[dst + 2] = inflated[src + 2];
                rgba[dst + 3] = 255;
            }
        } else {
            for (var i = 0; i < pixelCount; i++) {
                var v = inflated[i];
                var dst = i * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
        }

        width = info.Width;
        height = info.Height;
        return true;
    }

    private static bool TryReadStream(ReadOnlySpan<byte> data, int start, int lengthHint, out ReadOnlySpan<byte> stream, out int endOffset) {
        stream = ReadOnlySpan<byte>.Empty;
        endOffset = start;
        var streamIndex = IndexOfToken(data, StreamToken, start);
        if (streamIndex < 0) return false;

        var dataStart = streamIndex + StreamToken.Length;
        if (dataStart < data.Length) {
            if (data[dataStart] == '\r' && dataStart + 1 < data.Length && data[dataStart + 1] == '\n') {
                dataStart += 2;
            } else if (data[dataStart] == '\n' || data[dataStart] == '\r') {
                dataStart += 1;
            }
        }

        if (lengthHint > 0) {
            var dataEndHint = dataStart + lengthHint;
            if (dataEndHint > data.Length) return false;
            stream = data.Slice(dataStart, lengthHint);
            var endStreamIndex = IndexOfToken(data, EndStreamToken, dataEndHint);
            endOffset = endStreamIndex >= 0 ? endStreamIndex + EndStreamToken.Length : dataEndHint;
            return true;
        }

        var endStreamIndexLegacy = IndexOfToken(data, EndStreamToken, dataStart);
        if (endStreamIndexLegacy < 0) return false;

        var dataEnd = endStreamIndexLegacy;
        while (dataEnd > dataStart && (data[dataEnd - 1] == (byte)'\n' || data[dataEnd - 1] == (byte)'\r')) {
            dataEnd--;
        }
        stream = data.Slice(dataStart, dataEnd - dataStart);
        endOffset = endStreamIndexLegacy + EndStreamToken.Length;
        return true;
    }

    private static int IndexOfToken(ReadOnlySpan<byte> data, ReadOnlySpan<byte> token, int start) {
        var idx = data.Slice(start).IndexOf(token);
        return idx < 0 ? -1 : start + idx;
    }

    private static int IndexOfToken(ReadOnlySpan<byte> data, byte a, byte b, int start) {
        for (var i = start; i + 1 < data.Length; i++) {
            if (data[i] == a && data[i + 1] == b) return i;
        }
        return -1;
    }

    private static int LastIndexOfToken(ReadOnlySpan<byte> data, byte a, byte b, int before) {
        var start = Math.Min(before, data.Length - 2);
        for (var i = start; i >= 0; i--) {
            if (data[i] == a && data[i + 1] == b) return i;
        }
        return -1;
    }

    private static bool TryReadNumberAfterKey(ReadOnlySpan<byte> data, string key, out int value) {
        value = 0;
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        while (i < data.Length && IsDelimiter(data[i])) i++;
        var sign = 1;
        if (i < data.Length && data[i] == '-') { sign = -1; i++; }
        var found = false;
        var result = 0;
        while (i < data.Length && data[i] >= '0' && data[i] <= '9') {
            found = true;
            result = result * 10 + (data[i] - '0');
            i++;
        }
        if (!found) return false;
        value = result * sign;
        return true;
    }

    private static bool TryReadNameAfterKey(ReadOnlySpan<byte> data, string key, out string name) {
        name = string.Empty;
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        while (i < data.Length && IsDelimiter(data[i])) i++;
        while (i < data.Length && data[i] != '/') {
            if (!IsDelimiter(data[i])) return false;
            i++;
        }
        if (i >= data.Length || data[i] != '/') return false;
        i++;
        var start = i;
        while (i < data.Length && !IsDelimiter(data[i])) i++;
        if (i <= start) return false;
        name = GetAsciiString(data, start, i - start);
        return true;
    }

    private static string GetAsciiString(ReadOnlySpan<byte> data, int start, int length) {
#if NET8_0_OR_GREATER
        return System.Text.Encoding.ASCII.GetString(data.Slice(start, length));
#else
        if (length <= 0) return string.Empty;
        var buffer = new byte[length];
        data.Slice(start, length).CopyTo(buffer);
        return System.Text.Encoding.ASCII.GetString(buffer);
#endif
    }

    private static bool IsDelimiter(byte b) {
        return b == 0 || b == (byte)' ' || b == (byte)'\t' || b == (byte)'\r' || b == (byte)'\n'
               || b == (byte)'/' || b == (byte)'<' || b == (byte)'>' || b == (byte)'[' || b == (byte)']'
               || b == (byte)'(' || b == (byte)')';
    }

    private static bool TryInferColorsFromLength(int length, int width, int height, int predictor, out int colors) {
        colors = 0;
        if (width <= 0 || height <= 0) return false;
        if (predictor >= 10) {
            var rgb = checked((width * 3 + 1) * height);
            if (length == rgb) {
                colors = 3;
                return true;
            }
            var gray = checked((width + 1) * height);
            if (length == gray) {
                colors = 1;
                return true;
            }
            return false;
        }

        var rgbRaw = checked(width * height * 3);
        if (length == rgbRaw) {
            colors = 3;
            return true;
        }
        var grayRaw = checked(width * height);
        if (length == grayRaw) {
            colors = 1;
            return true;
        }
        return false;
    }

    private static byte[] DecompressFlate(ReadOnlySpan<byte> src, int expected) {
        using var input = new MemoryStream(src.ToArray(), writable: false);
#if NET8_0_OR_GREATER
        Stream stream = LooksLikeZlib(src)
            ? new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true)
            : new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
#else
        Stream stream;
        if (LooksLikeZlib(src)) {
            if (src.Length < 6) throw new FormatException("Invalid PDF deflate stream.");
            stream = new DeflateStream(new MemoryStream(src.Slice(2, src.Length - 6).ToArray(), writable: false), CompressionMode.Decompress, leaveOpen: true);
        } else {
            stream = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
        }
#endif
        using (stream) {
            var buffer = new byte[expected];
            ReadExact(stream, buffer);
            return buffer;
        }
    }

    private static byte[] DecompressFlateAll(ReadOnlySpan<byte> src) {
        using var input = new MemoryStream(src.ToArray(), writable: false);
#if NET8_0_OR_GREATER
        Stream stream = LooksLikeZlib(src)
            ? new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true)
            : new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
#else
        Stream stream;
        if (LooksLikeZlib(src)) {
            if (src.Length < 6) throw new FormatException("Invalid PDF deflate stream.");
            stream = new DeflateStream(new MemoryStream(src.Slice(2, src.Length - 6).ToArray(), writable: false), CompressionMode.Decompress, leaveOpen: true);
        } else {
            stream = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true);
        }
#endif
        using (stream) {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }

    private static bool LooksLikeZlib(ReadOnlySpan<byte> data) {
        if (data.Length < 2) return false;
        var cmf = data[0];
        var flg = data[1];
        if ((cmf & 0x0F) != 8) return false;
        return ((cmf << 8) + flg) % 31 == 0;
    }

    private static void ReadExact(Stream stream, byte[] buffer) {
        var offset = 0;
        while (offset < buffer.Length) {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read <= 0) throw new FormatException("Truncated PDF image data.");
            offset += read;
        }
    }

    private static void ApplyTiffPredictor(byte[] data, int rowSize, int rows, int bytesPerPixel) {
        if (bytesPerPixel <= 0) return;
        for (var y = 0; y < rows; y++) {
            var rowStart = y * rowSize;
            for (var i = bytesPerPixel; i < rowSize; i++) {
                data[rowStart + i] = unchecked((byte)(data[rowStart + i] + data[rowStart + i - bytesPerPixel]));
            }
        }
    }

    private static bool TryApplyPngPredictor(byte[] data, int width, int height, int colors, out byte[] output) {
        output = new byte[width * height * colors];
        var rowSize = width * colors;
        var srcOffset = 0;
        for (var y = 0; y < height; y++) {
            if (srcOffset >= data.Length) return false;
            var filter = data[srcOffset++];
            if (srcOffset + rowSize > data.Length) return false;
            var dstRow = y * rowSize;
            var prevRow = y == 0 ? -1 : (y - 1) * rowSize;
            for (var x = 0; x < rowSize; x++) {
                var raw = data[srcOffset++];
                var left = x >= colors ? output[dstRow + x - colors] : (byte)0;
                var up = prevRow >= 0 ? output[prevRow + x] : (byte)0;
                var upLeft = prevRow >= 0 && x >= colors ? output[prevRow + x - colors] : (byte)0;
                byte value;
                switch (filter) {
                    case 0:
                        value = raw;
                        break;
                    case 1:
                        value = unchecked((byte)(raw + left));
                        break;
                    case 2:
                        value = unchecked((byte)(raw + up));
                        break;
                    case 3:
                        value = unchecked((byte)(raw + ((left + up) >> 1)));
                        break;
                    case 4:
                        value = unchecked((byte)(raw + Paeth(left, up, upLeft)));
                        break;
                    default:
                        return false;
                }
                output[dstRow + x] = value;
            }
        }
        return true;
    }

    private static byte Paeth(byte a, byte b, byte c) {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }

    private readonly struct PdfImageInfo {
        public PdfImageInfo(int width, int height, int bitsPerComponent, int colors, string? colorSpace, string? filter, int predictor, int streamLength) {
            Width = width;
            Height = height;
            BitsPerComponent = bitsPerComponent;
            Colors = colors;
            ColorSpace = colorSpace;
            Filter = filter;
            Predictor = predictor;
            StreamLength = streamLength;
        }

        public int Width { get; }
        public int Height { get; }
        public int BitsPerComponent { get; }
        public int Colors { get; }
        public string? ColorSpace { get; }
        public string? Filter { get; }
        public int Predictor { get; }
        public int StreamLength { get; }
    }
}

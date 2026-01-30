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
    private static readonly byte[] InlineImageToken = { (byte)'B', (byte)'I' };
    private static readonly byte[] InlineImageDataToken = { (byte)'I', (byte)'D' };
    private static readonly byte[] InlineImageEndToken = { (byte)'E', (byte)'I' };

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
            if (TryDecodeWithFilters(info, stream, out rgba, out width, out height)) {
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
            var inlineIdx = IndexOfInlineToken(data, InlineImageToken, offset);
            var nextIdx = SelectNextIndex(imageIdx, inlineIdx);
            if (nextIdx < 0) return false;

            if (nextIdx == inlineIdx) {
                if (TryReadInlineImage(data, inlineIdx, out info, out stream, out var endOffset)) {
                    offset = endOffset;
                    return true;
                }
                offset = inlineIdx + InlineImageToken.Length;
                continue;
            }

            if (!TryReadXObjectImage(data, imageIdx, out info, out stream, out var xObjectEnd)) {
                offset = imageIdx + ImageToken.Length;
                continue;
            }

            offset = xObjectEnd;
            return true;
        }
        return false;
    }

    private static bool TryReadXObjectImage(ReadOnlySpan<byte> data, int imageIdx, out PdfImageInfo info, out ReadOnlySpan<byte> stream, out int endOffset) {
        info = default;
        stream = ReadOnlySpan<byte>.Empty;
        endOffset = imageIdx + ImageToken.Length;

        var dictStart = LastIndexOfToken(data, (byte)'<', (byte)'<', imageIdx);
        var dictEnd = IndexOfToken(data, (byte)'>', (byte)'>', imageIdx);
        if (dictStart < 0 || dictEnd < 0 || dictEnd <= dictStart) {
            return false;
        }

        var dict = data.Slice(dictStart, dictEnd + 2 - dictStart);
        if (IndexOfToken(dict, SubtypeToken, 0) < 0) {
            endOffset = dictEnd + 2;
            return false;
        }

        if (!TryParseImageInfo(dict, out info)) {
            endOffset = dictEnd + 2;
            return false;
        }

        if (!TryReadStream(data, dictEnd + 2, info.StreamLength, out stream, out endOffset)) {
            endOffset = dictEnd + 2;
            return false;
        }

        return true;
    }

    private static bool TryReadInlineImage(ReadOnlySpan<byte> data, int inlineIdx, out PdfImageInfo info, out ReadOnlySpan<byte> stream, out int endOffset) {
        info = default;
        stream = ReadOnlySpan<byte>.Empty;
        endOffset = inlineIdx + InlineImageToken.Length;

        var idIndex = IndexOfInlineToken(data, InlineImageDataToken, inlineIdx + InlineImageToken.Length);
        if (idIndex < 0) return false;

        var dictStart = inlineIdx + InlineImageToken.Length;
        var dictLength = idIndex - dictStart;
        if (dictLength <= 0) return false;
        var dict = data.Slice(dictStart, dictLength);

        if (!TryParseInlineImageInfo(dict, out info)) return false;

        var dataStart = idIndex + InlineImageDataToken.Length;
        while (dataStart < data.Length && IsDelimiter(data[dataStart])) {
            dataStart++;
        }
        if (dataStart >= data.Length) return false;

        if (!TryReadInlineImageData(data, dataStart, out stream, out var eiIndex)) return false;
        endOffset = eiIndex + InlineImageEndToken.Length;
        return true;
    }

    private static bool TryParseImageInfo(ReadOnlySpan<byte> dict, out PdfImageInfo info) {
        info = default;
        if (!TryReadNumberAfterKey(dict, "/Width", out var width)) return false;
        if (!TryReadNumberAfterKey(dict, "/Height", out var height)) return false;

        var bits = 8;
        if (TryReadNumberAfterKey(dict, "/BitsPerComponent", out var bpc)) {
            bits = bpc;
        }

        string[]? filters = null;
        if (TryReadNameArrayAfterKey(dict, "/Filter", out var filterNames)) {
            filters = filterNames;
        } else if (TryReadNameAfterKey(dict, "/Filter", out var filterName)) {
            filters = new[] { filterName };
        }

        var length = 0;
        TryReadNumberAfterKey(dict, "/Length", out length);

        string? colorSpace = null;
        if (TryReadNameAfterKey(dict, "/ColorSpace", out var csName)) {
            colorSpace = csName;
        } else if (TryReadFirstNameInArrayAfterKey(dict, "/ColorSpace", out var csArrayName)) {
            colorSpace = csArrayName;
        }

        var predictor = 1;
        if (TryReadNumberAfterKey(dict, "/Predictor", out var predictorValue)) {
            predictor = predictorValue;
        }

        var colors = 0;
        if (TryReadNumberAfterKey(dict, "/Colors", out var colorsValue)) {
            colors = colorsValue;
        }

        float[]? decode = null;
        if (TryReadNumberArrayAfterKey(dict, "/Decode", out var decodeValues)) {
            decode = decodeValues;
        }

        info = new PdfImageInfo(width, height, bits, colors, colorSpace, filters, predictor, length, decode);
        return true;
    }

    private static bool TryParseInlineImageInfo(ReadOnlySpan<byte> dict, out PdfImageInfo info) {
        info = default;
        if (!TryReadNumberAfterAnyKey(dict, new[] { "/W", "/Width" }, out var width)) return false;
        if (!TryReadNumberAfterAnyKey(dict, new[] { "/H", "/Height" }, out var height)) return false;

        var bits = 8;
        if (TryReadNumberAfterAnyKey(dict, new[] { "/BPC", "/BitsPerComponent" }, out var bpc)) {
            bits = bpc;
        }

        string[]? filters = null;
        if (TryReadNameArrayAfterAnyKey(dict, new[] { "/F", "/Filter" }, out var filterNames)) {
            filters = filterNames;
        } else if (TryReadNameAfterAnyKey(dict, new[] { "/F", "/Filter" }, out var filterName)) {
            filters = new[] { filterName };
        }

        string? colorSpace = null;
        if (TryReadNameAfterAnyKey(dict, new[] { "/CS", "/ColorSpace" }, out var csName)) {
            colorSpace = csName;
        } else if (TryReadFirstNameInArrayAfterAnyKey(dict, new[] { "/CS", "/ColorSpace" }, out var csArrayName)) {
            colorSpace = csArrayName;
        }

        var predictor = 1;
        if (TryReadNumberAfterAnyKey(dict, new[] { "/Predictor" }, out var predictorValue)) {
            predictor = predictorValue;
        }

        var colors = 0;
        if (TryReadNumberAfterAnyKey(dict, new[] { "/Colors" }, out var colorsValue)) {
            colors = colorsValue;
        }

        float[]? decode = null;
        if (TryReadNumberArrayAfterAnyKey(dict, new[] { "/D", "/Decode" }, out var decodeValues)) {
            decode = decodeValues;
        }

        info = new PdfImageInfo(width, height, bits, colors, colorSpace, filters, predictor, streamLength: 0, decode);
        return true;
    }

    private static bool TryDecodeWithFilters(PdfImageInfo info, ReadOnlySpan<byte> stream, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (info.Filters is null || info.Filters.Length == 0) {
            if (TryDecodeRaster(info, stream, applyPredictor: info.Predictor > 1, out rgba, out width, out height)) {
                return true;
            }
            try {
                rgba = JpegReader.DecodeRgba32(stream, out width, out height);
                return true;
            } catch (FormatException) {
                return false;
            }
        }

        var data = stream.ToArray();
        for (var i = 0; i < info.Filters.Length; i++) {
            var filter = info.Filters[i];
            if (IsFilter(filter, "ASCII85Decode", "A85")) {
                if (!TryDecodeAscii85(data, out var decoded)) return false;
                data = decoded;
                continue;
            }
            if (IsFilter(filter, "RunLengthDecode", "RL")) {
                if (!TryDecodeRunLength(data, out var decoded)) return false;
                data = decoded;
                continue;
            }
            if (IsFilter(filter, "FlateDecode", "Fl")) {
                try {
                    data = DecompressFlateAll(data);
                } catch (FormatException) {
                    return false;
                }
                continue;
            }
            if (IsFilter(filter, "DCTDecode", "DCT")) {
                try {
                    rgba = JpegReader.DecodeRgba32(data, out width, out height);
                    return true;
                } catch (FormatException) {
                    return false;
                }
            }
            return false;
        }

        return TryDecodeRaster(info, data, applyPredictor: info.Predictor > 1, out rgba, out width, out height);
    }

    private static bool TryDecodeRaster(PdfImageInfo info, ReadOnlySpan<byte> data, bool applyPredictor, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (info.Width <= 0 || info.Height <= 0) return false;
        if (info.BitsPerComponent != 8) return false;

        var colors = info.Colors;
        if (colors <= 0) {
            if (string.Equals(info.ColorSpace, "DeviceRGB", StringComparison.OrdinalIgnoreCase)) colors = 3;
            else if (string.Equals(info.ColorSpace, "DeviceGray", StringComparison.OrdinalIgnoreCase)) colors = 1;
            else if (string.Equals(info.ColorSpace, "DeviceCMYK", StringComparison.OrdinalIgnoreCase)) colors = 4;
        }
        if (colors != 0 && colors != 1 && colors != 3 && colors != 4) return false;

        if (colors == 0) {
            if (!TryInferColorsFromLength(data.Length, info.Width, info.Height, info.Predictor, out colors)) {
                return false;
            }
        } else {
            var expectedRowSize = checked(info.Width * colors);
            var expected = info.Predictor >= 10 ? checked((expectedRowSize + 1) * info.Height) : checked(expectedRowSize * info.Height);
            if (data.Length != expected) {
                if (!TryInferColorsFromLength(data.Length, info.Width, info.Height, info.Predictor, out colors)) {
                    return false;
                }
            }
        }

        var rowSize = checked(info.Width * colors);
        byte[] expanded;
        if (applyPredictor && info.Predictor == 2) {
            expanded = data.ToArray();
            ApplyTiffPredictor(expanded, rowSize, info.Height, colors);
        } else if (applyPredictor && info.Predictor >= 10) {
            if (!TryApplyPngPredictor(data.ToArray(), info.Width, info.Height, colors, out var decoded)) return false;
            expanded = decoded;
        } else {
            expanded = data.ToArray();
        }

        if (info.Decode is not null && info.Decode.Length >= colors * 2) {
            ApplyDecodeArray(expanded, colors, info.Decode);
        }

        var pixelCount = info.Width * info.Height;
        rgba = new byte[pixelCount * 4];
        if (colors == 3) {
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 3;
                var dst = i * 4;
                rgba[dst + 0] = expanded[src + 0];
                rgba[dst + 1] = expanded[src + 1];
                rgba[dst + 2] = expanded[src + 2];
                rgba[dst + 3] = 255;
            }
        } else if (colors == 4) {
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 4;
                var dst = i * 4;
                var c = expanded[src + 0];
                var m = expanded[src + 1];
                var y = expanded[src + 2];
                var k = expanded[src + 3];
                rgba[dst + 0] = (byte)(255 - Math.Min(255, c + k));
                rgba[dst + 1] = (byte)(255 - Math.Min(255, m + k));
                rgba[dst + 2] = (byte)(255 - Math.Min(255, y + k));
                rgba[dst + 3] = 255;
            }
        } else {
            for (var i = 0; i < pixelCount; i++) {
                var v = expanded[i];
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

    private static bool TryReadInlineImageData(ReadOnlySpan<byte> data, int dataStart, out ReadOnlySpan<byte> stream, out int eiIndex) {
        stream = ReadOnlySpan<byte>.Empty;
        eiIndex = -1;
        for (var i = dataStart; i + 1 < data.Length; i++) {
            if (data[i] != InlineImageEndToken[0] || data[i + 1] != InlineImageEndToken[1]) continue;
            var beforeOk = i == 0 || IsDelimiter(data[i - 1]);
            var afterIndex = i + InlineImageEndToken.Length;
            var afterOk = afterIndex >= data.Length || IsDelimiter(data[afterIndex]);
            if (!beforeOk || !afterOk) continue;
            stream = data.Slice(dataStart, i - dataStart);
            eiIndex = i;
            return true;
        }
        return false;
    }

    private static int IndexOfToken(ReadOnlySpan<byte> data, ReadOnlySpan<byte> token, int start) {
        var idx = data.Slice(start).IndexOf(token);
        return idx < 0 ? -1 : start + idx;
    }

    private static int IndexOfInlineToken(ReadOnlySpan<byte> data, ReadOnlySpan<byte> token, int start) {
        for (var i = start; i + token.Length <= data.Length; i++) {
            if (data[i] != token[0] || data[i + 1] != token[1]) continue;
            var beforeOk = i == 0 || IsDelimiter(data[i - 1]);
            var afterIndex = i + token.Length;
            var afterOk = afterIndex >= data.Length || IsDelimiter(data[afterIndex]);
            if (beforeOk && afterOk) return i;
        }
        return -1;
    }

    private static int IndexOfToken(ReadOnlySpan<byte> data, byte a, byte b, int start) {
        for (var i = start; i + 1 < data.Length; i++) {
            if (data[i] == a && data[i + 1] == b) return i;
        }
        return -1;
    }

    private static int SelectNextIndex(int a, int b) {
        if (a < 0) return b;
        if (b < 0) return a;
        return a < b ? a : b;
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

    private static bool TryReadNumberAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out int value) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadNumberAfterKey(data, keys[i], out value)) return true;
        }
        value = 0;
        return false;
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

    private static bool TryReadNameAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out string name) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadNameAfterKey(data, keys[i], out name)) return true;
        }
        name = string.Empty;
        return false;
    }

    private static bool TryReadNameArrayAfterKey(ReadOnlySpan<byte> data, string key, out string[] names) {
        names = Array.Empty<string>();
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        while (i < data.Length && IsDelimiter(data[i])) i++;
        if (i >= data.Length || data[i] != (byte)'[') return false;
        i++;
        var values = new System.Collections.Generic.List<string>();
        while (i < data.Length) {
            while (i < data.Length && IsDelimiter(data[i])) i++;
            if (i >= data.Length) break;
            if (data[i] == (byte)']') {
                i++;
                break;
            }
            if (data[i] != (byte)'/') return false;
            i++;
            var start = i;
            while (i < data.Length && !IsDelimiter(data[i])) i++;
            if (i <= start) return false;
            values.Add(GetAsciiString(data, start, i - start));
        }
        if (values.Count == 0) return false;
        names = values.ToArray();
        return true;
    }

    private static bool TryReadNameArrayAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out string[] names) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadNameArrayAfterKey(data, keys[i], out names)) return true;
        }
        names = Array.Empty<string>();
        return false;
    }

    private static bool TryReadFirstNameInArrayAfterKey(ReadOnlySpan<byte> data, string key, out string name) {
        name = string.Empty;
        if (!TryReadNameArrayAfterKey(data, key, out var names)) return false;
        if (names.Length == 0) return false;
        name = names[0];
        return true;
    }

    private static bool TryReadFirstNameInArrayAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out string name) {
        name = string.Empty;
        if (!TryReadNameArrayAfterAnyKey(data, keys, out var names)) return false;
        if (names.Length == 0) return false;
        name = names[0];
        return true;
    }

    private static bool TryReadNumberArrayAfterKey(ReadOnlySpan<byte> data, string key, out float[] values) {
        values = Array.Empty<float>();
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        while (i < data.Length && IsDelimiter(data[i])) i++;
        if (i >= data.Length || data[i] != (byte)'[') return false;
        i++;
        var list = new System.Collections.Generic.List<float>();
        while (i < data.Length) {
            while (i < data.Length && IsDelimiter(data[i])) i++;
            if (i >= data.Length) break;
            if (data[i] == (byte)']') {
                i++;
                break;
            }
            if (!TryReadFloatToken(data, ref i, out var value)) return false;
            list.Add(value);
        }
        if (list.Count == 0) return false;
        values = list.ToArray();
        return true;
    }

    private static bool TryReadNumberArrayAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out float[] values) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadNumberArrayAfterKey(data, keys[i], out values)) return true;
        }
        values = Array.Empty<float>();
        return false;
    }

    private static bool TryReadFloatToken(ReadOnlySpan<byte> data, ref int index, out float value) {
        value = 0;
        if (index >= data.Length) return false;
        var sign = 1f;
        if (data[index] == (byte)'-') {
            sign = -1f;
            index++;
        } else if (data[index] == (byte)'+') {
            index++;
        }

        var hasDigits = false;
        var integer = 0;
        while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9') {
            hasDigits = true;
            integer = integer * 10 + (data[index] - (byte)'0');
            index++;
        }

        var frac = 0;
        var fracDiv = 1;
        if (index < data.Length && data[index] == (byte)'.') {
            index++;
            while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9') {
                hasDigits = true;
                frac = frac * 10 + (data[index] - (byte)'0');
                fracDiv *= 10;
                index++;
            }
        }

        if (!hasDigits) return false;
        var number = integer + (fracDiv > 1 ? (float)frac / fracDiv : 0f);
        value = number * sign;
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
            var cmyk = checked((width * 4 + 1) * height);
            if (length == cmyk) {
                colors = 4;
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
        var cmykRaw = checked(width * height * 4);
        if (length == cmykRaw) {
            colors = 4;
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

    private static bool IsFilter(string value, string fullName, string shortName) {
        return value.Equals(fullName, StringComparison.OrdinalIgnoreCase)
            || value.Equals(shortName, StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyDecodeArray(byte[] data, int colors, float[] decode) {
        var entries = colors * 2;
        if (decode.Length < entries) return;
        var length = data.Length;
        var pixelStride = colors;
        for (var i = 0; i < length; i += pixelStride) {
            for (var c = 0; c < colors; c++) {
                var dmin = decode[c * 2];
                var dmax = decode[c * 2 + 1];
                var sample = data[i + c];
                var value = dmin + (sample / 255f) * (dmax - dmin);
                var scaled = (int)Math.Round(value * 255f);
                if (scaled < 0) scaled = 0;
                if (scaled > 255) scaled = 255;
                data[i + c] = (byte)scaled;
            }
        }
    }

    private static bool TryDecodeAscii85(ReadOnlySpan<byte> src, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        using var ms = new MemoryStream();
        uint tuple = 0;
        var count = 0;
        for (var i = 0; i < src.Length; i++) {
            var b = src[i];
            if (b == (byte)'~') {
                if (i + 1 < src.Length && src[i + 1] == (byte)'>') {
                    i++;
                    break;
                }
            }
            if (b == (byte)'z') {
                if (count != 0) return false;
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                continue;
            }
            if (b <= 32) continue;
            if (b < (byte)'!' || b > (byte)'u') return false;
            tuple = tuple * 85 + (uint)(b - (byte)'!');
            count++;
            if (count == 5) {
                WriteTuple(ms, tuple);
                tuple = 0;
                count = 0;
            }
        }

        if (count > 0) {
            for (var i = count; i < 5; i++) {
                tuple = tuple * 85 + 84;
            }
            var buffer = new byte[4];
            buffer[0] = (byte)(tuple >> 24);
            buffer[1] = (byte)(tuple >> 16);
            buffer[2] = (byte)(tuple >> 8);
            buffer[3] = (byte)tuple;
            ms.Write(buffer, 0, count - 1);
        }

        decoded = ms.ToArray();
        return true;
    }

    private static void WriteTuple(Stream stream, uint tuple) {
        stream.WriteByte((byte)(tuple >> 24));
        stream.WriteByte((byte)(tuple >> 16));
        stream.WriteByte((byte)(tuple >> 8));
        stream.WriteByte((byte)tuple);
    }

    private static bool TryDecodeRunLength(ReadOnlySpan<byte> src, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        using var ms = new MemoryStream();
        var i = 0;
        while (i < src.Length) {
            var b = src[i++];
            if (b == 128) break;
            if (b <= 127) {
                var count = b + 1;
                if (i + count > src.Length) return false;
                var chunk = new byte[count];
                src.Slice(i, count).CopyTo(chunk);
                ms.Write(chunk, 0, chunk.Length);
                i += count;
            } else {
                var count = 257 - b;
                if (i >= src.Length) return false;
                var value = src[i++];
                for (var j = 0; j < count; j++) {
                    ms.WriteByte(value);
                }
            }
        }
        decoded = ms.ToArray();
        return true;
    }

    private readonly struct PdfImageInfo {
        public PdfImageInfo(int width, int height, int bitsPerComponent, int colors, string? colorSpace, string[]? filters, int predictor, int streamLength, float[]? decode) {
            Width = width;
            Height = height;
            BitsPerComponent = bitsPerComponent;
            Colors = colors;
            ColorSpace = colorSpace;
            Filters = filters;
            Predictor = predictor;
            StreamLength = streamLength;
            Decode = decode;
        }

        public int Width { get; }
        public int Height { get; }
        public int BitsPerComponent { get; }
        public int Colors { get; }
        public string? ColorSpace { get; }
        public string[]? Filters { get; }
        public int Predictor { get; }
        public int StreamLength { get; }
        public float[]? Decode { get; }
    }
}

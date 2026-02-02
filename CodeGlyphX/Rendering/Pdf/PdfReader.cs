using System;
using System.IO;
using System.IO.Compression;
using CodeGlyphX.Rendering;
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
    private const string PdfImageLimitMessage = "PDF image exceeds size limits.";

    private enum PdfColorSpaceKind {
        Unknown = 0,
        DeviceGray,
        DeviceRGB,
        DeviceCMYK,
        Indexed,
        Separation,
        DeviceN
    }

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
                if (info.SoftMaskObj > 0) {
                    TryApplySoftMask(data, info, rgba, width, height);
                }
                if (info.MaskObj > 0) {
                    TryApplyMaskImage(data, info, rgba, width, height);
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
        if (dictStart < 0) {
            return false;
        }

        if (!TryReadDictionarySliceAt(data, dictStart, out var dict, out var dictEnd)) {
            return false;
        }
        if (IndexOfToken(dict, SubtypeToken, 0) < 0) {
            endOffset = dictEnd;
            return false;
        }

        if (!TryParseImageInfo(data, dict, out info)) {
            endOffset = dictEnd;
            return false;
        }

        if (!TryReadStream(data, dictEnd, info.StreamLength, out stream, out endOffset)) {
            endOffset = dictEnd;
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

        if (!TryParseInlineImageInfo(data, dict, out info)) return false;

        var dataStart = idIndex + InlineImageDataToken.Length;
        if (dataStart < data.Length && IsWhitespaceByte(data[dataStart])) {
            if (data[dataStart] == (byte)'\r' && dataStart + 1 < data.Length && data[dataStart + 1] == (byte)'\n') {
                dataStart += 2;
            } else {
                dataStart++;
            }
        }
        if (dataStart >= data.Length) return false;

        if (!TryReadInlineImageData(data, dataStart, out stream, out var eiIndex)) return false;
        endOffset = eiIndex + InlineImageEndToken.Length;
        return true;
    }

    private static bool TryParseImageInfo(ReadOnlySpan<byte> data, ReadOnlySpan<byte> dict, out PdfImageInfo info) {
        info = default;
        if (!TryReadNumberAfterKey(dict, "/Width", out var width)) return false;
        if (!TryReadNumberAfterKey(dict, "/Height", out var height)) return false;

        var bits = 8;
        var bitsSpecified = false;
        if (TryReadNumberAfterKey(dict, "/BitsPerComponent", out var bpc)) {
            bits = bpc;
            bitsSpecified = true;
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

        var isImageMask = false;
        if (TryReadBoolAfterKey(dict, "/ImageMask", out var imageMask)) {
            isImageMask = imageMask;
        }
        if (isImageMask && !bitsSpecified) {
            bits = 1;
        }

        var softMaskObj = 0;
        var softMaskGen = 0;
        if (TryReadIndirectRefAfterKey(dict, "/SMask", out var smObj, out var smGen)) {
            softMaskObj = smObj;
            softMaskGen = smGen;
        }

        var maskObj = 0;
        var maskGen = 0;
        if (TryReadIndirectRefAfterKey(dict, "/Mask", out var maskRefObj, out var maskRefGen)) {
            maskObj = maskRefObj;
            maskGen = maskRefGen;
        }

        var predictor = 1;
        if (TryReadNumberAfterKey(dict, "/Predictor", out var predictorValue)) {
            predictor = predictorValue;
        }

        var colors = 0;
        if (TryReadNumberAfterKey(dict, "/Colors", out var colorsValue)) {
            colors = colorsValue;
        }

        float[]? mask = null;
        if (TryReadNumberArrayAfterKey(dict, "/Mask", out var maskValues)) {
            mask = maskValues;
        }

        var lzwEarlyChange = -1;
        var lzwEarlyChangeSet = false;
        if (filters != null && TryReadDecodeParmsArrayAfterKey(dict, "/DecodeParms", out var dpArray)) {
            ApplyDecodeParmsForFilters(filters, dpArray, ref predictor, ref colors, ref lzwEarlyChange, ref lzwEarlyChangeSet);
        } else if (TryReadDecodeParmsAfterKey(dict, "/DecodeParms", out var dpPredictor, out var dpColors, out _, out var dpEarlyChange, out var dpHasEarlyChange)) {
            if (predictor == 1 && dpPredictor > 0) predictor = dpPredictor;
            if (colors == 0 && dpColors > 0) colors = dpColors;
            if (dpHasEarlyChange) {
                lzwEarlyChange = dpEarlyChange;
                lzwEarlyChangeSet = true;
            }
        }

        float[]? decode = null;
        if (TryReadNumberArrayAfterKey(dict, "/Decode", out var decodeValues)) {
            decode = decodeValues;
        }

        var colorSpaceKind = ParseColorSpaceName(colorSpace);
        if (TryReadIccBasedColorSpaceAfterKey(dict, data, "/ColorSpace", out var iccBase)) {
            colorSpaceKind = iccBase;
        }
        if (TryReadAlternateColorSpaceAfterKey(dict, data, "/ColorSpace", out var altKind)) {
            colorSpaceKind = altKind;
        }
        if (TryReadIndexedColorSpaceAfterKey(dict, "/ColorSpace", out var indexedBase, out var indexedHigh, out var indexedLookup)) {
            colorSpaceKind = PdfColorSpaceKind.Indexed;
            info = new PdfImageInfo(width, height, bits, colors, colorSpaceKind, indexedBase, indexedHigh, indexedLookup, filters, predictor, lzwEarlyChange, softMaskObj, softMaskGen, maskObj, maskGen, isImageMask, length, decode, mask);
            return true;
        }

        info = new PdfImageInfo(width, height, bits, colors, colorSpaceKind, PdfColorSpaceKind.Unknown, 0, null, filters, predictor, lzwEarlyChange, softMaskObj, softMaskGen, maskObj, maskGen, isImageMask, length, decode, mask);
        return true;
    }

    private static bool TryParseInlineImageInfo(ReadOnlySpan<byte> data, ReadOnlySpan<byte> dict, out PdfImageInfo info) {
        info = default;
        if (!TryReadNumberAfterAnyKey(dict, new[] { "/W", "/Width" }, out var width)) return false;
        if (!TryReadNumberAfterAnyKey(dict, new[] { "/H", "/Height" }, out var height)) return false;

        var bits = 8;
        var bitsSpecified = false;
        if (TryReadNumberAfterAnyKey(dict, new[] { "/BPC", "/BitsPerComponent" }, out var bpc)) {
            bits = bpc;
            bitsSpecified = true;
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

        var isImageMask = false;
        if (TryReadBoolAfterAnyKey(dict, new[] { "/IM", "/ImageMask" }, out var imageMask)) {
            isImageMask = imageMask;
        }
        if (isImageMask && !bitsSpecified) {
            bits = 1;
        }

        var softMaskObj = 0;
        var softMaskGen = 0;
        if (TryReadIndirectRefAfterAnyKey(dict, new[] { "/SMask" }, out var smObj, out var smGen)) {
            softMaskObj = smObj;
            softMaskGen = smGen;
        }

        var maskObj = 0;
        var maskGen = 0;
        if (TryReadIndirectRefAfterAnyKey(dict, new[] { "/Mask" }, out var maskRefObj, out var maskRefGen)) {
            maskObj = maskRefObj;
            maskGen = maskRefGen;
        }

        var predictor = 1;
        if (TryReadNumberAfterAnyKey(dict, new[] { "/Predictor" }, out var predictorValue)) {
            predictor = predictorValue;
        }

        var colors = 0;
        if (TryReadNumberAfterAnyKey(dict, new[] { "/Colors" }, out var colorsValue)) {
            colors = colorsValue;
        }

        float[]? mask = null;
        if (TryReadNumberArrayAfterAnyKey(dict, new[] { "/Mask" }, out var maskValues)) {
            mask = maskValues;
        }

        var lzwEarlyChange = -1;
        var lzwEarlyChangeSet = false;
        if (filters != null && TryReadDecodeParmsArrayAfterAnyKey(dict, new[] { "/DecodeParms", "/DP" }, out var dpArray)) {
            ApplyDecodeParmsForFilters(filters, dpArray, ref predictor, ref colors, ref lzwEarlyChange, ref lzwEarlyChangeSet);
        } else if (TryReadDecodeParmsAfterAnyKey(dict, new[] { "/DecodeParms", "/DP" }, out var dpPredictor, out var dpColors, out _, out var dpEarlyChange, out var dpHasEarlyChange)) {
            if (predictor == 1 && dpPredictor > 0) predictor = dpPredictor;
            if (colors == 0 && dpColors > 0) colors = dpColors;
            if (dpHasEarlyChange) {
                lzwEarlyChange = dpEarlyChange;
                lzwEarlyChangeSet = true;
            }
        }

        float[]? decode = null;
        if (TryReadNumberArrayAfterAnyKey(dict, new[] { "/D", "/Decode" }, out var decodeValues)) {
            decode = decodeValues;
        }

        var colorSpaceKind = ParseColorSpaceName(colorSpace);
        if (TryReadIccBasedColorSpaceAfterAnyKey(dict, data, new[] { "/CS", "/ColorSpace" }, out var iccBase)) {
            colorSpaceKind = iccBase;
        }
        if (TryReadAlternateColorSpaceAfterAnyKey(dict, data, new[] { "/CS", "/ColorSpace" }, out var altKind)) {
            colorSpaceKind = altKind;
        }
        if (TryReadIndexedColorSpaceAfterAnyKey(dict, new[] { "/CS", "/ColorSpace" }, out var indexedBase, out var indexedHigh, out var indexedLookup)) {
            colorSpaceKind = PdfColorSpaceKind.Indexed;
            info = new PdfImageInfo(width, height, bits, colors, colorSpaceKind, indexedBase, indexedHigh, indexedLookup, filters, predictor, lzwEarlyChange, softMaskObj, softMaskGen, maskObj, maskGen, isImageMask, streamLength: 0, decode, mask);
            return true;
        }

        info = new PdfImageInfo(width, height, bits, colors, colorSpaceKind, PdfColorSpaceKind.Unknown, 0, null, filters, predictor, lzwEarlyChange, softMaskObj, softMaskGen, maskObj, maskGen, isImageMask, streamLength: 0, decode, mask);
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
            if (IsFilter(filter, "ASCIIHexDecode", "AHx")) {
                if (!TryDecodeAsciiHex(data, out var decoded)) return false;
                data = decoded;
                continue;
            }
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
            if (IsFilter(filter, "LZWDecode", "LZW")) {
                if (!TryDecodeLzw(info, data, out var decoded)) return false;
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

    private static void TryApplySoftMask(ReadOnlySpan<byte> data, PdfImageInfo info, byte[] rgba, int width, int height) {
        if (info.SoftMaskObj <= 0) return;
        if (!TryResolveIndirectStream(data, info.SoftMaskObj, info.SoftMaskGen, out var dict, out var stream)) return;
        if (!TryParseImageInfo(data, dict, out var maskInfo)) return;
        if (!TryDecodeWithFilters(maskInfo, stream, out var maskRgba, out var maskWidth, out var maskHeight)) return;
        if (maskWidth != width || maskHeight != height) return;

        if (!DecodeGuards.TryEnsurePixelCount(width, height, out var pixelCount)) return;
        for (var i = 0; i < pixelCount; i++) {
            var maskAlpha = maskRgba[i * 4];
            var dst = i * 4 + 3;
            var baseAlpha = rgba[dst];
            rgba[dst] = (byte)((baseAlpha * maskAlpha + 127) / 255);
        }
    }

    private static void TryApplyMaskImage(ReadOnlySpan<byte> data, PdfImageInfo info, byte[] rgba, int width, int height) {
        if (info.MaskObj <= 0) return;
        if (!TryResolveIndirectStream(data, info.MaskObj, info.MaskGen, out var dict, out var stream)) return;
        if (!TryParseImageInfo(data, dict, out var maskInfo)) return;
        if (!TryDecodeWithFilters(maskInfo, stream, out var maskRgba, out var maskWidth, out var maskHeight)) return;
        if (maskWidth != width || maskHeight != height) return;

        if (!DecodeGuards.TryEnsurePixelCount(width, height, out var pixelCount)) return;
        for (var i = 0; i < pixelCount; i++) {
            var maskAlpha = maskInfo.IsImageMask ? maskRgba[i * 4 + 3] : maskRgba[i * 4];
            var dst = i * 4 + 3;
            var baseAlpha = rgba[dst];
            rgba[dst] = (byte)((baseAlpha * maskAlpha + 127) / 255);
        }
    }

    private static bool TryDecodeRaster(PdfImageInfo info, ReadOnlySpan<byte> data, bool applyPredictor, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (info.Width <= 0 || info.Height <= 0) return false;
        if (!DecodeGuards.TryEnsurePixelCount(info.Width, info.Height, out _)) return false;

        var bits = info.BitsPerComponent;
        var colors = info.Colors;
        if (info.IsImageMask) {
            colors = 1;
        }
        if (info.ColorSpaceKind == PdfColorSpaceKind.Indexed) {
            colors = 1;
        } else if (colors <= 0) {
            colors = info.ColorSpaceKind switch {
                PdfColorSpaceKind.DeviceGray => 1,
                PdfColorSpaceKind.DeviceRGB => 3,
                PdfColorSpaceKind.DeviceCMYK => 4,
                _ => colors
            };
        }
        if (colors != 0 && colors != 1 && colors != 3 && colors != 4) return false;

        if (bits != 8) {
            if (colors <= 0) return false;
            if (applyPredictor && info.Predictor > 1 && bits != 16) return false;
            if (!TryValidatePackedLengthForPredictor(info.Width, info.Height, colors, bits, info.Predictor, data.Length)) return false;
        } else {
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
        }

        byte[] expanded;
        if (bits == 8) {
            var rowSize = checked(info.Width * colors);
            if (applyPredictor && info.Predictor == 2) {
                expanded = data.ToArray();
                ApplyTiffPredictor(expanded, rowSize, info.Height, colors);
            } else if (applyPredictor && info.Predictor >= 10) {
                if (!TryApplyPngPredictor(data.ToArray(), info.Width, info.Height, colors, out var decoded)) return false;
                expanded = decoded;
            } else {
                expanded = data.ToArray();
            }
        } else {
            var scale = info.IsImageMask || info.ColorSpaceKind != PdfColorSpaceKind.Indexed;
            if (bits == 16) {
                if (applyPredictor && info.Predictor == 2) {
                    var decoded16 = data.ToArray();
                    var rowSize = checked(info.Width * colors * 2);
                    ApplyTiffPredictor16(decoded16, rowSize, info.Height, colors);
                    if (!TryExpandSamples(decoded16, info.Width, info.Height, colors, bits, scale, out expanded)) return false;
                } else if (applyPredictor && info.Predictor >= 10) {
                    if (!TryApplyPngPredictor16(data.ToArray(), info.Width, info.Height, colors, out var decoded16)) return false;
                    if (!TryExpandSamples(decoded16, info.Width, info.Height, colors, bits, scale, out expanded)) return false;
                } else {
                    if (!TryExpandSamples(data, info.Width, info.Height, colors, bits, scale, out expanded)) return false;
                }
            } else {
                if (!TryExpandSamples(data, info.Width, info.Height, colors, bits, scale, out expanded)) return false;
            }
        }

        if (info.IsImageMask) {
            if (info.Decode is not null && info.Decode.Length >= 2) {
                ApplyDecodeArray(expanded, 1, info.Decode);
            }
            var maskPixelCount = info.Width * info.Height;
            rgba = new byte[maskPixelCount * 4];
            for (var i = 0; i < maskPixelCount; i++) {
                var dst = i * 4;
                var alpha = expanded[i];
                rgba[dst + 0] = 0;
                rgba[dst + 1] = 0;
                rgba[dst + 2] = 0;
                rgba[dst + 3] = alpha;
            }
            width = info.Width;
            height = info.Height;
            return true;
        }

        if (info.ColorSpaceKind == PdfColorSpaceKind.Indexed && info.Decode is not null && info.Decode.Length >= 2) {
            if (!TryApplyIndexedDecode(expanded, info.BitsPerComponent, info.IndexedHighVal, info.Decode)) return false;
        }

        var expandedScaled = !info.IsImageMask && info.ColorSpaceKind != PdfColorSpaceKind.Indexed;
        byte[]? maskAlpha = null;
        if (info.Mask is not null && info.Mask.Length >= colors * 2) {
            if (TryBuildMaskAlpha(expanded, info.Width, info.Height, colors, info.BitsPerComponent, info.Mask, expandedScaled, out var alpha)) {
                maskAlpha = alpha;
            }
        }

        if (info.ColorSpaceKind == PdfColorSpaceKind.Indexed) {
            if (!TryExpandIndexed(info, expanded, out rgba)) return false;
            if (maskAlpha is not null) {
                var maskPixelCount = info.Width * info.Height;
                for (var i = 0; i < maskPixelCount; i++) {
                    rgba[i * 4 + 3] = maskAlpha[i];
                }
            }
            width = info.Width;
            height = info.Height;
            return true;
        }

        if (info.Decode is not null && info.Decode.Length >= colors * 2) {
            ApplyDecodeArray(expanded, colors, info.Decode);
        }

        var totalPixels = info.Width * info.Height;
        rgba = new byte[totalPixels * 4];
        if (colors == 3) {
            for (var i = 0; i < totalPixels; i++) {
                var src = i * 3;
                var dst = i * 4;
                rgba[dst + 0] = expanded[src + 0];
                rgba[dst + 1] = expanded[src + 1];
                rgba[dst + 2] = expanded[src + 2];
                rgba[dst + 3] = 255;
            }
        } else if (colors == 4) {
            for (var i = 0; i < totalPixels; i++) {
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
            for (var i = 0; i < totalPixels; i++) {
                var v = expanded[i];
                var dst = i * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = 255;
            }
        }

        if (maskAlpha is not null) {
            for (var i = 0; i < totalPixels; i++) {
                rgba[i * 4 + 3] = maskAlpha[i];
            }
        }

        width = info.Width;
        height = info.Height;
        return true;
    }

    private static bool TryBuildMaskAlpha(byte[] expanded, int width, int height, int colors, int bitsPerComponent, float[] mask, bool expandedScaled, out byte[] alpha) {
        alpha = Array.Empty<byte>();
        if (width <= 0 || height <= 0 || colors <= 0) return false;
        if (mask.Length < colors * 2) return false;
        var maxValue = bitsPerComponent == 16 ? 65535 : (1 << bitsPerComponent) - 1;
        if (maxValue <= 0) return false;
        if (expandedScaled) {
            var ranges = new byte[colors * 2];
            for (var c = 0; c < colors; c++) {
                var min = ClampMaskValue(mask[c * 2], maxValue);
                var max = ClampMaskValue(mask[c * 2 + 1], maxValue);
                if (min > max) {
                    var tmp = min;
                    min = max;
                    max = tmp;
                }
                ranges[c * 2] = ScaleMaskValue(min, maxValue);
                ranges[c * 2 + 1] = ScaleMaskValue(max, maxValue);
            }

            var pixelCount = DecodeGuards.EnsurePixelCount(width, height, PdfImageLimitMessage);
            alpha = new byte[pixelCount];
            for (var i = 0; i < pixelCount; i++) {
                var baseIndex = i * colors;
                var match = true;
                for (var c = 0; c < colors; c++) {
                    var v = expanded[baseIndex + c];
                    var min = ranges[c * 2];
                    var max = ranges[c * 2 + 1];
                    if (v < min || v > max) {
                        match = false;
                        break;
                    }
                }
                alpha[i] = match ? (byte)0 : (byte)255;
            }
            return true;
        }

        var rawRanges = new int[colors * 2];
        for (var c = 0; c < colors; c++) {
            var min = ClampMaskValue(mask[c * 2], maxValue);
            var max = ClampMaskValue(mask[c * 2 + 1], maxValue);
            if (min > max) {
                var tmp = min;
                min = max;
                max = tmp;
            }
            rawRanges[c * 2] = min;
            rawRanges[c * 2 + 1] = max;
        }

        var count = DecodeGuards.EnsurePixelCount(width, height, PdfImageLimitMessage);
        alpha = new byte[count];
        for (var i = 0; i < count; i++) {
            var baseIndex = i * colors;
            var match = true;
            for (var c = 0; c < colors; c++) {
                var v = expanded[baseIndex + c];
                var min = rawRanges[c * 2];
                var max = rawRanges[c * 2 + 1];
                if (v < min || v > max) {
                    match = false;
                    break;
                }
            }
            alpha[i] = match ? (byte)0 : (byte)255;
        }
        return true;
    }

    private static bool TryApplyIndexedDecode(byte[] indices, int bitsPerComponent, int highVal, float[] decode) {
        if (decode.Length < 2) return false;
        if (highVal < 0) return false;
        var maxSample = bitsPerComponent == 16 ? 65535 : (1 << bitsPerComponent) - 1;
        if (maxSample <= 0) return false;
        var dmin = decode[0];
        var dmax = decode[1];
        for (var i = 0; i < indices.Length; i++) {
            var raw = indices[i];
            var mapped = dmin + raw * (dmax - dmin) / maxSample;
            var value = (int)Math.Round(mapped);
            if (value < 0) value = 0;
            if (value > highVal) value = highVal;
            indices[i] = (byte)value;
        }
        return true;
    }

    private static int ClampMaskValue(float value, int maxValue) {
        var v = (int)Math.Round(value);
        if (v < 0) return 0;
        if (v > maxValue) return maxValue;
        return v;
    }

    private static byte ScaleMaskValue(int value, int maxValue) {
        if (maxValue <= 0) return 0;
        if (maxValue == 255) return (byte)value;
        return (byte)((value * 255 + (maxValue / 2)) / maxValue);
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
            var dataEnd = i;
            if (dataEnd > dataStart && IsWhitespaceByte(data[dataEnd - 1])) {
                if (data[dataEnd - 1] == (byte)'\n' && dataEnd - 2 >= dataStart && data[dataEnd - 2] == (byte)'\r') {
                    dataEnd -= 2;
                } else {
                    dataEnd--;
                }
            }
            stream = data.Slice(dataStart, dataEnd - dataStart);
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

    private static bool TryReadBoolAfterKey(ReadOnlySpan<byte> data, string key, out bool value) {
        value = false;
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        SkipWhitespace(data, ref i);
        if (StartsWithKeyword(data, i, "true")) {
            value = true;
            return true;
        }
        if (StartsWithKeyword(data, i, "false")) {
            value = false;
            return true;
        }
        return false;
    }

    private static bool TryReadBoolAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out bool value) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadBoolAfterKey(data, keys[i], out value)) return true;
        }
        value = false;
        return false;
    }

    private static bool TryReadNameAfterKey(ReadOnlySpan<byte> data, string key, out string name) {
        name = string.Empty;
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        SkipWhitespace(data, ref i);
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
        SkipWhitespace(data, ref i);
        if (i >= data.Length || data[i] != (byte)'[') return false;
        i++;
        var values = new System.Collections.Generic.List<string>();
        while (i < data.Length) {
            SkipWhitespace(data, ref i);
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
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        SkipWhitespace(data, ref i);
        if (i >= data.Length || data[i] != (byte)'[') return false;
        i++;
        SkipWhitespace(data, ref i);
        if (i >= data.Length || data[i] != (byte)'/') return false;
        i++;
        var start = i;
        while (i < data.Length && !IsDelimiter(data[i])) i++;
        if (i <= start) return false;
        name = GetAsciiString(data, start, i - start);
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
        SkipWhitespace(data, ref i);
        if (i >= data.Length || data[i] != (byte)'[') return false;
        i++;
        var list = new System.Collections.Generic.List<float>();
        while (i < data.Length) {
            SkipWhitespace(data, ref i);
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

    private readonly struct PdfDecodeParms {
        public PdfDecodeParms(int predictor, int colors, int columns, int earlyChange, bool hasEarlyChange) {
            Predictor = predictor;
            Colors = colors;
            Columns = columns;
            EarlyChange = earlyChange;
            HasEarlyChange = hasEarlyChange;
        }

        public int Predictor { get; }
        public int Colors { get; }
        public int Columns { get; }
        public int EarlyChange { get; }
        public bool HasEarlyChange { get; }
        public bool HasAny => Predictor > 0 || Colors > 0 || Columns > 0 || HasEarlyChange;
    }

    private static bool TryReadDecodeParmsAfterKey(ReadOnlySpan<byte> data, string key, out int predictor, out int colors, out int columns, out int earlyChange, out bool hasEarlyChange) {
        predictor = 0;
        colors = 0;
        columns = 0;
        earlyChange = 0;
        hasEarlyChange = false;
        if (!TryReadDictionarySliceAfterKey(data, key, out var dict)) return false;
        if (!TryReadDecodeParmsFromDict(dict, out var parms)) return false;
        predictor = parms.Predictor;
        colors = parms.Colors;
        columns = parms.Columns;
        earlyChange = parms.EarlyChange;
        hasEarlyChange = parms.HasEarlyChange;
        return parms.HasAny;
    }

    private static bool TryReadDecodeParmsAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out int predictor, out int colors, out int columns, out int earlyChange, out bool hasEarlyChange) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadDecodeParmsAfterKey(data, keys[i], out predictor, out colors, out columns, out earlyChange, out hasEarlyChange)) return true;
        }
        predictor = 0;
        colors = 0;
        columns = 0;
        earlyChange = 0;
        hasEarlyChange = false;
        return false;
    }

    private static bool TryReadDecodeParmsArrayAfterKey(ReadOnlySpan<byte> data, string key, out PdfDecodeParms[] parms) {
        parms = Array.Empty<PdfDecodeParms>();
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        while (i < data.Length && data[i] <= 32) i++;
        if (i >= data.Length || data[i] != (byte)'[') return false;
        i++;
        var start = i;
        var depth = 1;
        while (i < data.Length) {
            if (data[i] == (byte)'[') depth++;
            else if (data[i] == (byte)']') {
                depth--;
                if (depth == 0) {
                    var array = data.Slice(start, i - start);
                    return TryReadDecodeParmsArray(array, out parms);
                }
            }
            i++;
        }
        return false;
    }

    private static bool TryReadDecodeParmsArrayAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out PdfDecodeParms[] parms) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadDecodeParmsArrayAfterKey(data, keys[i], out parms)) return true;
        }
        parms = Array.Empty<PdfDecodeParms>();
        return false;
    }

    private static bool TryReadDecodeParmsArray(ReadOnlySpan<byte> data, out PdfDecodeParms[] parms) {
        parms = Array.Empty<PdfDecodeParms>();
        var list = new System.Collections.Generic.List<PdfDecodeParms>();
        var i = 0;
        while (i < data.Length) {
            SkipWhitespace(data, ref i);
            if (i >= data.Length) break;
            if (data[i] == (byte)'<' && i + 1 < data.Length && data[i + 1] == (byte)'<') {
                if (!TryReadDictionarySliceAt(data, i, out var dict, out var endIndex)) return false;
                if (!TryReadDecodeParmsFromDict(dict, out var parmsEntry)) {
                    parmsEntry = new PdfDecodeParms(0, 0, 0, 0, false);
                }
                list.Add(parmsEntry);
                i = endIndex;
                continue;
            }
            if (StartsWithKeyword(data, i, "null")) {
                list.Add(new PdfDecodeParms(0, 0, 0, 0, false));
                i += 4;
                continue;
            }
            if (IsNumberStart(data[i])) {
                if (!TrySkipNumberToken(data, ref i)) return false;
                SkipWhitespace(data, ref i);
                var saved = i;
                if (i < data.Length && IsNumberStart(data[i])) {
                    if (!TrySkipNumberToken(data, ref i)) return false;
                    SkipWhitespace(data, ref i);
                    if (i < data.Length && data[i] == (byte)'R') {
                        i++;
                    } else {
                        i = saved;
                    }
                }
                list.Add(new PdfDecodeParms(0, 0, 0, 0, false));
                continue;
            }
            if (data[i] == (byte)'/') {
                i++;
                while (i < data.Length && data[i] > 32 && !IsDelimiter(data[i])) i++;
                list.Add(new PdfDecodeParms(0, 0, 0, 0, false));
                continue;
            }
            list.Add(new PdfDecodeParms(0, 0, 0, 0, false));
            i++;
        }
        if (list.Count == 0) return false;
        parms = list.ToArray();
        return true;
    }

    private static bool TryReadDecodeParmsFromDict(ReadOnlySpan<byte> dict, out PdfDecodeParms parms) {
        var predictor = 0;
        var colors = 0;
        var columns = 0;
        var earlyChange = 0;
        var hasEarlyChange = false;
        TryReadNumberAfterKey(dict, "/Predictor", out predictor);
        TryReadNumberAfterKey(dict, "/Colors", out colors);
        TryReadNumberAfterKey(dict, "/Columns", out columns);
        if (TryReadNumberAfterKey(dict, "/EarlyChange", out var earlyValue)) {
            earlyChange = earlyValue;
            hasEarlyChange = true;
        }
        parms = new PdfDecodeParms(predictor, colors, columns, earlyChange, hasEarlyChange);
        return parms.HasAny;
    }

    private static void ApplyDecodeParmsForFilters(string[] filters, PdfDecodeParms[] parms, ref int predictor, ref int colors, ref int lzwEarlyChange, ref bool lzwEarlyChangeSet) {
        var count = Math.Min(filters.Length, parms.Length);
        var predictorSet = predictor != 1;
        var colorsSet = colors != 0;
        for (var i = count - 1; i >= 0; i--) {
            var filter = filters[i];
            var isFlate = IsFilter(filter, "FlateDecode", "Fl");
            var isLzw = IsFilter(filter, "LZWDecode", "LZW");
            if (!isFlate && !isLzw) continue;
            var dp = parms[i];
            if (!predictorSet && dp.Predictor > 0) {
                predictor = dp.Predictor;
                predictorSet = true;
            }
            if (!colorsSet && dp.Colors > 0) {
                colors = dp.Colors;
                colorsSet = true;
            }
            if (!lzwEarlyChangeSet && isLzw && dp.HasEarlyChange) {
                lzwEarlyChange = dp.EarlyChange;
                lzwEarlyChangeSet = true;
            }
            if (predictorSet && colorsSet && lzwEarlyChangeSet) break;
        }
    }

    private static bool TryReadIccBasedColorSpaceAfterKey(ReadOnlySpan<byte> data, ReadOnlySpan<byte> fullData, string key, out PdfColorSpaceKind baseKind) {
        baseKind = PdfColorSpaceKind.Unknown;
        if (!TryReadArraySliceAfterKey(data, key, out var array)) return false;
        return TryReadIccBasedColorSpace(array, fullData, out baseKind);
    }

    private static bool TryReadIccBasedColorSpaceAfterAnyKey(ReadOnlySpan<byte> data, ReadOnlySpan<byte> fullData, string[] keys, out PdfColorSpaceKind baseKind) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadIccBasedColorSpaceAfterKey(data, fullData, keys[i], out baseKind)) return true;
        }
        baseKind = PdfColorSpaceKind.Unknown;
        return false;
    }

    private static bool TryReadIccBasedColorSpace(ReadOnlySpan<byte> array, ReadOnlySpan<byte> fullData, out PdfColorSpaceKind baseKind) {
        baseKind = PdfColorSpaceKind.Unknown;
        var index = 0;
        if (!TryReadNameToken(array, ref index, out var first) || !first.Equals("ICCBased", StringComparison.OrdinalIgnoreCase)) return false;
        SkipWhitespace(array, ref index);
        if (index >= array.Length) return false;
        if (array[index] == (byte)'<' && index + 1 < array.Length && array[index + 1] == (byte)'<') {
            if (!TryReadDictionarySliceAt(array, index, out var dict, out _)) return false;
            return TryResolveIccDictKind(dict, out baseKind);
        }
        if (TryReadIndirectReference(array, ref index, out var obj, out var gen)) {
            if (!TryResolveIndirectDictionary(fullData, obj, gen, out var dict)) return false;
            return TryResolveIccDictKind(dict, out baseKind);
        }
        return false;
    }

    private static bool TryResolveIccDictKind(ReadOnlySpan<byte> dict, out PdfColorSpaceKind baseKind) {
        baseKind = PdfColorSpaceKind.Unknown;
        string? alternate = null;
        if (TryReadNameAfterKey(dict, "/Alternate", out var altName)) {
            alternate = altName;
        } else if (TryReadFirstNameInArrayAfterKey(dict, "/Alternate", out var altArrayName)) {
            alternate = altArrayName;
        }
        if (!string.IsNullOrEmpty(alternate)) {
            var altKind = ParseColorSpaceName(alternate);
            if (altKind != PdfColorSpaceKind.Unknown) {
                baseKind = altKind;
                return true;
            }
        }
        if (TryReadNumberAfterKey(dict, "/N", out var n)) {
            baseKind = n switch {
                1 => PdfColorSpaceKind.DeviceGray,
                3 => PdfColorSpaceKind.DeviceRGB,
                4 => PdfColorSpaceKind.DeviceCMYK,
                _ => PdfColorSpaceKind.Unknown
            };
            return baseKind != PdfColorSpaceKind.Unknown;
        }
        return false;
    }

    private static bool TryReadAlternateColorSpaceAfterKey(ReadOnlySpan<byte> data, ReadOnlySpan<byte> fullData, string key, out PdfColorSpaceKind altKind) {
        altKind = PdfColorSpaceKind.Unknown;
        if (!TryReadArraySliceAfterKey(data, key, out var array)) return false;
        return TryReadAlternateColorSpace(array, fullData, out altKind);
    }

    private static bool TryReadAlternateColorSpaceAfterAnyKey(ReadOnlySpan<byte> data, ReadOnlySpan<byte> fullData, string[] keys, out PdfColorSpaceKind altKind) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadAlternateColorSpaceAfterKey(data, fullData, keys[i], out altKind)) return true;
        }
        altKind = PdfColorSpaceKind.Unknown;
        return false;
    }

    private static bool TryReadAlternateColorSpace(ReadOnlySpan<byte> array, ReadOnlySpan<byte> fullData, out PdfColorSpaceKind altKind) {
        altKind = PdfColorSpaceKind.Unknown;
        var index = 0;
        if (!TryReadNameToken(array, ref index, out var first)) return false;
        if (!first.Equals("Separation", StringComparison.OrdinalIgnoreCase) && !first.Equals("DeviceN", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }
        SkipWhitespace(array, ref index);
        if (!TrySkipObjectToken(array, ref index)) return false;
        SkipWhitespace(array, ref index);
        if (index >= array.Length) return false;
        if (array[index] == (byte)'[') {
            if (!TryReadArraySliceAt(array, index, out var altArray, out _)) return false;
            var altIndex = 0;
            if (!TryReadNameToken(altArray, ref altIndex, out var altFirst)) return false;
            if (altFirst.Equals("ICCBased", StringComparison.OrdinalIgnoreCase)) {
                return TryReadIccBasedColorSpace(altArray, fullData, out altKind);
            }
            if (altFirst.Equals("Indexed", StringComparison.OrdinalIgnoreCase)) {
                if (!TryReadNameToken(altArray, ref altIndex, out var baseName)) return false;
                altKind = ParseColorSpaceName(baseName);
                return altKind != PdfColorSpaceKind.Unknown;
            }
            altKind = ParseColorSpaceName(altFirst);
            return altKind != PdfColorSpaceKind.Unknown;
        }
        if (!TryReadNameToken(array, ref index, out var altName)) return false;
        altKind = ParseColorSpaceName(altName);
        return altKind != PdfColorSpaceKind.Unknown;
    }

    private static bool TryReadIndexedColorSpaceAfterKey(ReadOnlySpan<byte> data, string key, out PdfColorSpaceKind baseKind, out int highVal, out byte[] lookup) {
        baseKind = PdfColorSpaceKind.Unknown;
        highVal = 0;
        lookup = Array.Empty<byte>();
        if (!TryReadArraySliceAfterKey(data, key, out var array)) return false;
        var index = 0;
        if (!TryReadNameToken(array, ref index, out var first) || !first.Equals("Indexed", StringComparison.OrdinalIgnoreCase)) return false;
        if (!TryReadNameToken(array, ref index, out var baseName)) return false;
        baseKind = ParseColorSpaceName(baseName);
        if (baseKind == PdfColorSpaceKind.Unknown) return false;
        if (!TryReadIntToken(array, ref index, out highVal)) return false;
        if (!TryReadLookupToken(array, ref index, out lookup)) return false;
        return lookup.Length > 0;
    }

    private static bool TryReadIndexedColorSpaceAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out PdfColorSpaceKind baseKind, out int highVal, out byte[] lookup) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadIndexedColorSpaceAfterKey(data, keys[i], out baseKind, out highVal, out lookup)) return true;
        }
        baseKind = PdfColorSpaceKind.Unknown;
        highVal = 0;
        lookup = Array.Empty<byte>();
        return false;
    }

    private static bool TryReadDictionarySliceAfterKey(ReadOnlySpan<byte> data, string key, out ReadOnlySpan<byte> dict) {
        dict = ReadOnlySpan<byte>.Empty;
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        while (i < data.Length && data[i] <= 32) i++;
        if (i >= data.Length) return false;
        if (data[i] == (byte)'[') {
            if (!TryReadArraySliceAfterKey(data, key, out var array)) return false;
            return TryReadDictionarySlice(array, out dict);
        }
        if (data[i] == (byte)'<' && i + 1 < data.Length && data[i + 1] == (byte)'<') {
            return TryReadDictionarySlice(data.Slice(i), out dict);
        }
        return false;
    }

    private static bool TryReadDictionarySlice(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> dict) {
        dict = ReadOnlySpan<byte>.Empty;
        var start = IndexOfToken(data, (byte)'<', (byte)'<', 0);
        if (start < 0) return false;
        var i = start + 2;
        var depth = 1;
        while (i + 1 < data.Length) {
            if (data[i] == (byte)'<' && data[i + 1] == (byte)'<') {
                depth++;
                i += 2;
                continue;
            }
            if (data[i] == (byte)'>' && data[i + 1] == (byte)'>') {
                depth--;
                if (depth == 0) {
                    dict = data.Slice(start + 2, i - (start + 2));
                    return true;
                }
                i += 2;
                continue;
            }
            i++;
        }
        return false;
    }

    private static bool TryReadDictionarySliceAt(ReadOnlySpan<byte> data, int start, out ReadOnlySpan<byte> dict, out int endIndex) {
        dict = ReadOnlySpan<byte>.Empty;
        endIndex = start;
        if (start < 0 || start + 1 >= data.Length) return false;
        if (data[start] != (byte)'<' || data[start + 1] != (byte)'<') return false;
        var i = start + 2;
        var depth = 1;
        while (i + 1 < data.Length) {
            if (data[i] == (byte)'<' && data[i + 1] == (byte)'<') {
                depth++;
                i += 2;
                continue;
            }
            if (data[i] == (byte)'>' && data[i + 1] == (byte)'>') {
                depth--;
                if (depth == 0) {
                    dict = data.Slice(start + 2, i - (start + 2));
                    endIndex = i + 2;
                    return true;
                }
                i += 2;
                continue;
            }
            i++;
        }
        return false;
    }

    private static bool TryReadArraySliceAfterKey(ReadOnlySpan<byte> data, string key, out ReadOnlySpan<byte> array) {
        array = ReadOnlySpan<byte>.Empty;
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        SkipWhitespace(data, ref i);
        if (i >= data.Length || data[i] != (byte)'[') return false;
        i++;
        var start = i;
        var depth = 1;
        while (i < data.Length) {
            if (data[i] == (byte)'[') depth++;
            else if (data[i] == (byte)']') {
                depth--;
                if (depth == 0) {
                    array = data.Slice(start, i - start);
                    return true;
                }
            }
            i++;
        }
        return false;
    }

    private static bool TryReadArraySliceAt(ReadOnlySpan<byte> data, int start, out ReadOnlySpan<byte> array, out int endIndex) {
        array = ReadOnlySpan<byte>.Empty;
        endIndex = start;
        if (start < 0 || start >= data.Length || data[start] != (byte)'[') return false;
        var i = start + 1;
        var depth = 1;
        while (i < data.Length) {
            if (data[i] == (byte)'[') depth++;
            else if (data[i] == (byte)']') {
                depth--;
                if (depth == 0) {
                    array = data.Slice(start + 1, i - (start + 1));
                    endIndex = i + 1;
                    return true;
                }
            }
            i++;
        }
        return false;
    }

    private static bool TryReadNameToken(ReadOnlySpan<byte> data, ref int index, out string name) {
        name = string.Empty;
        SkipDelimiters(data, ref index);
        if (index >= data.Length || data[index] != (byte)'/') return false;
        index++;
        var start = index;
        while (index < data.Length && !IsDelimiter(data[index])) index++;
        if (index <= start) return false;
        name = GetAsciiString(data, start, index - start);
        return true;
    }

    private static bool TryReadIntToken(ReadOnlySpan<byte> data, ref int index, out int value) {
        value = 0;
        SkipDelimiters(data, ref index);
        var sign = 1;
        if (index < data.Length && data[index] == (byte)'-') {
            sign = -1;
            index++;
        }
        var found = false;
        var result = 0;
        while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9') {
            found = true;
            result = result * 10 + (data[index] - (byte)'0');
            index++;
        }
        if (!found) return false;
        value = result * sign;
        return true;
    }

    private static bool TryReadLookupToken(ReadOnlySpan<byte> data, ref int index, out byte[] lookup) {
        lookup = Array.Empty<byte>();
        SkipWhitespace(data, ref index);
        if (index >= data.Length) return false;
        if (data[index] == (byte)'<') {
            return TryReadHexString(data, ref index, out lookup);
        }
        if (data[index] == (byte)'(') {
            return TryReadLiteralString(data, ref index, out lookup);
        }
        return false;
    }

    private static void SkipDelimiters(ReadOnlySpan<byte> data, ref int index) {
        while (index < data.Length && IsDelimiter(data[index]) && data[index] != (byte)'/') index++;
    }

    private static void SkipWhitespace(ReadOnlySpan<byte> data, ref int index) {
        while (index < data.Length && data[index] <= 32) index++;
    }

    private static bool IsWhitespaceByte(byte b) {
        return b == 0 || b == (byte)' ' || b == (byte)'\t' || b == (byte)'\r' || b == (byte)'\n' || b == (byte)'\f';
    }

    private static bool IsNumberStart(byte b) {
        return (b >= (byte)'0' && b <= (byte)'9') || b == (byte)'-' || b == (byte)'+' || b == (byte)'.';
    }

    private static bool TrySkipNumberToken(ReadOnlySpan<byte> data, ref int index) {
        if (index >= data.Length) return false;
        if (data[index] == (byte)'+' || data[index] == (byte)'-') index++;
        var hasDigits = false;
        while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9') {
            hasDigits = true;
            index++;
        }
        if (index < data.Length && data[index] == (byte)'.') {
            index++;
            while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9') {
                hasDigits = true;
                index++;
            }
        }
        return hasDigits;
    }

    private static bool StartsWithKeyword(ReadOnlySpan<byte> data, int index, string keyword) {
        if (index < 0) return false;
        if (index + keyword.Length > data.Length) return false;
        for (var i = 0; i < keyword.Length; i++) {
            if (data[index + i] != (byte)keyword[i]) return false;
        }
        return true;
    }

    private static bool TrySkipObjectToken(ReadOnlySpan<byte> data, ref int index) {
        if (index >= data.Length) return false;
        if (data[index] == (byte)'(') {
            return TryReadLiteralString(data, ref index, out _);
        }
        if (data[index] == (byte)'<' && index + 1 < data.Length && data[index + 1] == (byte)'<') {
            return TryReadDictionarySliceAt(data, index, out _, out index);
        }
        if (data[index] == (byte)'<') {
            return TryReadHexString(data, ref index, out _);
        }
        if (data[index] == (byte)'[') {
            return TrySkipArrayToken(data, ref index);
        }
        if (data[index] == (byte)'/') {
            index++;
            while (index < data.Length && data[index] > 32 && !IsDelimiter(data[index])) index++;
            return true;
        }
        if (StartsWithKeyword(data, index, "null")) { index += 4; return true; }
        if (StartsWithKeyword(data, index, "true")) { index += 4; return true; }
        if (StartsWithKeyword(data, index, "false")) { index += 5; return true; }
        if (IsNumberStart(data[index])) {
            if (!TrySkipNumberToken(data, ref index)) return false;
            var saved = index;
            SkipWhitespace(data, ref index);
            if (index < data.Length && IsNumberStart(data[index])) {
                if (!TrySkipNumberToken(data, ref index)) return false;
                SkipWhitespace(data, ref index);
                if (index < data.Length && data[index] == (byte)'R') {
                    index++;
                    return true;
                }
                index = saved;
            }
            return true;
        }
        return false;
    }

    private static bool TrySkipArrayToken(ReadOnlySpan<byte> data, ref int index) {
        if (index >= data.Length || data[index] != (byte)'[') return false;
        index++;
        var depth = 1;
        while (index < data.Length) {
            SkipWhitespace(data, ref index);
            if (index >= data.Length) break;
            if (data[index] == (byte)'[') {
                depth++;
                index++;
                continue;
            }
            if (data[index] == (byte)']') {
                depth--;
                index++;
                if (depth == 0) return true;
                continue;
            }
            if (!TrySkipObjectToken(data, ref index)) return false;
        }
        return false;
    }

    private static bool TryReadIndirectRefAfterKey(ReadOnlySpan<byte> data, string key, out int obj, out int gen) {
        obj = 0;
        gen = 0;
        var idx = data.IndexOf(System.Text.Encoding.ASCII.GetBytes(key));
        if (idx < 0) return false;
        var i = idx + key.Length;
        SkipWhitespace(data, ref i);
        return TryReadIndirectReference(data, ref i, out obj, out gen);
    }

    private static bool TryReadIndirectRefAfterAnyKey(ReadOnlySpan<byte> data, string[] keys, out int obj, out int gen) {
        for (var i = 0; i < keys.Length; i++) {
            if (TryReadIndirectRefAfterKey(data, keys[i], out obj, out gen)) return true;
        }
        obj = 0;
        gen = 0;
        return false;
    }

    private static bool TryReadIndirectReference(ReadOnlySpan<byte> data, ref int index, out int obj, out int gen) {
        obj = 0;
        gen = 0;
        var i = index;
        if (!TryReadIntTokenSimple(data, ref i, out obj)) return false;
        if (!TryReadIntTokenSimple(data, ref i, out gen)) return false;
        SkipWhitespace(data, ref i);
        if (i >= data.Length || data[i] != (byte)'R') return false;
        index = i + 1;
        return true;
    }

    private static bool TryReadIntTokenSimple(ReadOnlySpan<byte> data, ref int index, out int value) {
        value = 0;
        SkipWhitespace(data, ref index);
        var sign = 1;
        if (index < data.Length && data[index] == (byte)'-') {
            sign = -1;
            index++;
        } else if (index < data.Length && data[index] == (byte)'+') {
            index++;
        }
        var found = false;
        var result = 0;
        while (index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'9') {
            found = true;
            result = result * 10 + (data[index] - (byte)'0');
            index++;
        }
        if (!found) return false;
        value = result * sign;
        return true;
    }

    private static bool TryResolveIndirectDictionary(ReadOnlySpan<byte> data, int obj, int gen, out ReadOnlySpan<byte> dict) {
        dict = ReadOnlySpan<byte>.Empty;
        if (obj < 0 || gen < 0) return false;
        var token = System.Text.Encoding.ASCII.GetBytes("obj");
        var start = 0;
        while (start < data.Length) {
            var idx = data.Slice(start).IndexOf(token);
            if (idx < 0) break;
            idx += start;
            var beforeOk = idx == 0 || IsDelimiter(data[idx - 1]);
            var afterOk = idx + token.Length >= data.Length || IsDelimiter(data[idx + token.Length]);
            if (beforeOk && afterOk && TryParseIndirectHeader(data, idx, out var foundObj, out var foundGen)) {
                if (foundObj == obj && foundGen == gen) {
                    var dictStart = IndexOfToken(data, (byte)'<', (byte)'<', idx + token.Length);
                    if (dictStart < 0) return false;
                    return TryReadDictionarySliceAt(data, dictStart, out dict, out _);
                }
            }
            start = idx + token.Length;
        }
        return false;
    }

    private static bool TryResolveIndirectStream(ReadOnlySpan<byte> data, int obj, int gen, out ReadOnlySpan<byte> dict, out ReadOnlySpan<byte> stream) {
        dict = ReadOnlySpan<byte>.Empty;
        stream = ReadOnlySpan<byte>.Empty;
        if (obj < 0 || gen < 0) return false;
        var token = System.Text.Encoding.ASCII.GetBytes("obj");
        var start = 0;
        while (start < data.Length) {
            var idx = data.Slice(start).IndexOf(token);
            if (idx < 0) break;
            idx += start;
            var beforeOk = idx == 0 || IsDelimiter(data[idx - 1]);
            var afterOk = idx + token.Length >= data.Length || IsDelimiter(data[idx + token.Length]);
            if (beforeOk && afterOk && TryParseIndirectHeader(data, idx, out var foundObj, out var foundGen)) {
                if (foundObj == obj && foundGen == gen) {
                    var dictStart = IndexOfToken(data, (byte)'<', (byte)'<', idx + token.Length);
                    if (dictStart < 0) return false;
                    if (!TryReadDictionarySliceAt(data, dictStart, out dict, out var dictEnd)) return false;
                    var lengthHint = 0;
                    TryReadNumberAfterKey(dict, "/Length", out lengthHint);
                    if (!TryReadStream(data, dictEnd, lengthHint, out stream, out _)) return false;
                    return true;
                }
            }
            start = idx + token.Length;
        }
        return false;
    }

    private static bool TryParseIndirectHeader(ReadOnlySpan<byte> data, int objTokenIndex, out int obj, out int gen) {
        obj = 0;
        gen = 0;
        var i = objTokenIndex - 1;
        if (!TryReadIntBackward(data, ref i, out gen)) return false;
        if (!TryReadIntBackward(data, ref i, out obj)) return false;
        return true;
    }

    private static bool TryReadIntBackward(ReadOnlySpan<byte> data, ref int index, out int value) {
        value = 0;
        while (index >= 0 && data[index] <= 32) index--;
        if (index < 0 || data[index] < (byte)'0' || data[index] > (byte)'9') return false;
        var end = index;
        while (index >= 0 && data[index] >= (byte)'0' && data[index] <= (byte)'9') index--;
        var start = index + 1;
        var result = 0;
        for (var i = start; i <= end; i++) {
            result = result * 10 + (data[i] - (byte)'0');
        }
        value = result;
        return true;
    }

    private static bool TryReadHexString(ReadOnlySpan<byte> data, ref int index, out byte[] bytes) {
        bytes = Array.Empty<byte>();
        if (index >= data.Length || data[index] != (byte)'<') return false;
        index++;
        var buffer = new System.Collections.Generic.List<byte>();
        var highNibble = -1;
        while (index < data.Length) {
            var b = data[index++];
            if (b == (byte)'>') {
                if (highNibble >= 0) {
                    buffer.Add((byte)(highNibble << 4));
                }
                bytes = buffer.ToArray();
                return true;
            }
            if (IsDelimiter(b)) continue;
            var nibble = HexToNibble(b);
            if (nibble < 0) return false;
            if (highNibble < 0) {
                highNibble = nibble;
            } else {
                buffer.Add((byte)((highNibble << 4) | nibble));
                highNibble = -1;
            }
        }
        return false;
    }

    private static int HexToNibble(byte b) {
        if (b >= (byte)'0' && b <= (byte)'9') return b - (byte)'0';
        if (b >= (byte)'a' && b <= (byte)'f') return b - (byte)'a' + 10;
        if (b >= (byte)'A' && b <= (byte)'F') return b - (byte)'A' + 10;
        return -1;
    }

    private static bool TryReadLiteralString(ReadOnlySpan<byte> data, ref int index, out byte[] bytes) {
        bytes = Array.Empty<byte>();
        if (index >= data.Length || data[index] != (byte)'(') return false;
        index++;
        var buffer = new System.Collections.Generic.List<byte>();
        var depth = 1;
        while (index < data.Length) {
            var b = data[index++];
            if (b == (byte)'\\') {
                if (index >= data.Length) return false;
                var esc = data[index++];
                switch (esc) {
                    case (byte)'n': buffer.Add((byte)'\n'); break;
                    case (byte)'r': buffer.Add((byte)'\r'); break;
                    case (byte)'t': buffer.Add((byte)'\t'); break;
                    case (byte)'b': buffer.Add((byte)'\b'); break;
                    case (byte)'f': buffer.Add((byte)'\f'); break;
                    case (byte)'\\': buffer.Add((byte)'\\'); break;
                    case (byte)'(' : buffer.Add((byte)'('); break;
                    case (byte)')' : buffer.Add((byte)')'); break;
                    default:
                        if (esc >= (byte)'0' && esc <= (byte)'7') {
                            var octal = esc - (byte)'0';
                            var count = 1;
                            while (count < 3 && index < data.Length && data[index] >= (byte)'0' && data[index] <= (byte)'7') {
                                octal = (octal * 8) + (data[index] - (byte)'0');
                                index++;
                                count++;
                            }
                            buffer.Add((byte)octal);
                        } else {
                            buffer.Add(esc);
                        }
                        break;
                }
                continue;
            }
            if (b == (byte)'(') {
                depth++;
                buffer.Add(b);
                continue;
            }
            if (b == (byte)')') {
                depth--;
                if (depth == 0) {
                    bytes = buffer.ToArray();
                    return true;
                }
                buffer.Add(b);
                continue;
            }
            buffer.Add(b);
        }
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

        var components = new[] { 3, 4, 1 };
        if (predictor >= 10) {
            for (var i = 0; i < components.Length; i++) {
                var componentCount = components[i];
                var rowBytes = (long)width * (long)componentCount + 1L;
                var expected = rowBytes * (long)height;
                if (TryMatchLength(length, expected, componentCount, out colors)) return true;
            }
            return false;
        }

        var pixelCount = (long)width * height;
        for (var i = 0; i < components.Length; i++) {
            var componentCount = components[i];
            var expected = pixelCount * (long)componentCount;
            if (TryMatchLength(length, expected, componentCount, out colors)) return true;
        }
        return false;
    }

    private static bool TryMatchLength(int length, long expected, int componentCount, out int colors) {
        colors = 0;
        if (expected > int.MaxValue) return false;
        if (length != expected) return false;
        colors = componentCount;
        return true;
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

    private static void ApplyTiffPredictor16(byte[] data, int rowSize, int rows, int colors) {
        var bytesPerPixel = colors * 2;
        if (bytesPerPixel <= 0) return;
        for (var y = 0; y < rows; y++) {
            var rowStart = y * rowSize;
            for (var i = bytesPerPixel; i + 1 < rowSize; i += 2) {
                var prevIndex = rowStart + i - bytesPerPixel;
                var prev = (data[prevIndex] << 8) | data[prevIndex + 1];
                var curIndex = rowStart + i;
                var cur = (data[curIndex] << 8) | data[curIndex + 1];
                var value = (cur + prev) & 0xFFFF;
                data[curIndex] = (byte)(value >> 8);
                data[curIndex + 1] = (byte)value;
            }
        }
    }

    private static bool TryApplyPngPredictor(byte[] data, int width, int height, int colors, out byte[] output) {
        var outputLength = DecodeGuards.EnsureByteCount((long)width * height * colors, PdfImageLimitMessage);
        output = new byte[outputLength];
        var rowSize = DecodeGuards.EnsureByteCount((long)width * colors, PdfImageLimitMessage);
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

    private static bool TryApplyPngPredictor16(byte[] data, int width, int height, int colors, out byte[] output) {
        output = Array.Empty<byte>();
        if (width <= 0 || height <= 0 || colors <= 0) return false;
        var rowSize = checked(width * colors * 2);
        var expected = checked((rowSize + 1) * height);
        if (data.Length != expected) return false;

        var decoded = new byte[rowSize * height];
        var bytesPerPixel = colors * 2;
        for (var y = 0; y < height; y++) {
            var srcRow = y * (rowSize + 1);
            var dstRow = y * rowSize;
            var filter = data[srcRow++];
            switch (filter) {
                case 0:
                    Buffer.BlockCopy(data, srcRow, decoded, dstRow, rowSize);
                    break;
                case 1:
                    for (var i = 0; i < rowSize; i++) {
                        var left = i >= bytesPerPixel ? decoded[dstRow + i - bytesPerPixel] : (byte)0;
                        decoded[dstRow + i] = (byte)(data[srcRow + i] + left);
                    }
                    break;
                case 2:
                    for (var i = 0; i < rowSize; i++) {
                        var up = y > 0 ? decoded[dstRow - rowSize + i] : (byte)0;
                        decoded[dstRow + i] = (byte)(data[srcRow + i] + up);
                    }
                    break;
                case 3:
                    for (var i = 0; i < rowSize; i++) {
                        var left = i >= bytesPerPixel ? decoded[dstRow + i - bytesPerPixel] : (byte)0;
                        var up = y > 0 ? decoded[dstRow - rowSize + i] : (byte)0;
                        decoded[dstRow + i] = (byte)(data[srcRow + i] + ((left + up) >> 1));
                    }
                    break;
                case 4:
                    for (var i = 0; i < rowSize; i++) {
                        var a = i >= bytesPerPixel ? decoded[dstRow + i - bytesPerPixel] : (byte)0;
                        var b = y > 0 ? decoded[dstRow - rowSize + i] : (byte)0;
                        var c = (y > 0 && i >= bytesPerPixel) ? decoded[dstRow - rowSize + i - bytesPerPixel] : (byte)0;
                        decoded[dstRow + i] = (byte)(data[srcRow + i] + Paeth(a, b, c));
                    }
                    break;
                default:
                    return false;
            }
        }

        output = decoded;
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

    private static PdfColorSpaceKind ParseColorSpaceName(string? name) {
        if (string.IsNullOrWhiteSpace(name)) return PdfColorSpaceKind.Unknown;
        if (string.Equals(name, "DeviceGray", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "G", StringComparison.OrdinalIgnoreCase)) {
            return PdfColorSpaceKind.DeviceGray;
        }
        if (string.Equals(name, "CalGray", StringComparison.OrdinalIgnoreCase)) {
            return PdfColorSpaceKind.DeviceGray;
        }
        if (string.Equals(name, "DeviceRGB", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "RGB", StringComparison.OrdinalIgnoreCase)) {
            return PdfColorSpaceKind.DeviceRGB;
        }
        if (string.Equals(name, "CalRGB", StringComparison.OrdinalIgnoreCase)) {
            return PdfColorSpaceKind.DeviceRGB;
        }
        if (string.Equals(name, "DeviceCMYK", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "CMYK", StringComparison.OrdinalIgnoreCase)) {
            return PdfColorSpaceKind.DeviceCMYK;
        }
        return PdfColorSpaceKind.Unknown;
    }

    private static bool TryValidatePackedLength(int width, int height, int colors, int bitsPerComponent, int length) {
        if (width <= 0 || height <= 0 || colors <= 0) return false;
        if (bitsPerComponent != 1 && bitsPerComponent != 2 && bitsPerComponent != 4 && bitsPerComponent != 16) return false;
        var rowBits = (long)width * colors * bitsPerComponent;
        var rowBytes = (int)((rowBits + 7) / 8);
        var expected = (long)rowBytes * height;
        return expected == length;
    }

    private static bool TryValidatePackedLengthForPredictor(int width, int height, int colors, int bitsPerComponent, int predictor, int length) {
        if (TryValidatePackedLength(width, height, colors, bitsPerComponent, length)) return true;
        if (predictor < 10) return false;
        if (width <= 0 || height <= 0 || colors <= 0) return false;
        if (bitsPerComponent != 1 && bitsPerComponent != 2 && bitsPerComponent != 4 && bitsPerComponent != 16) return false;
        var rowBits = (long)width * colors * bitsPerComponent;
        var rowBytes = (int)((rowBits + 7) / 8);
        var expected = (long)(rowBytes + 1) * height;
        return expected == length;
    }

    private static bool TryExpandSamples(ReadOnlySpan<byte> data, int width, int height, int colors, int bitsPerComponent, bool scale, out byte[] expanded) {
        expanded = Array.Empty<byte>();
        if (bitsPerComponent == 16) {
            if (!scale) return false;
            var samplesPerRow16 = checked(width * colors);
            var rowBytes16 = checked(samplesPerRow16 * 2);
            if ((long)rowBytes16 * height > data.Length) return false;
            expanded = new byte[samplesPerRow16 * height];
            var srcIndex = 0;
            for (var i = 0; i < expanded.Length; i++) {
                var value = (data[srcIndex] << 8) | data[srcIndex + 1];
                srcIndex += 2;
                expanded[i] = (byte)((value + 128) / 257);
            }
            return true;
        }
        if (bitsPerComponent != 1 && bitsPerComponent != 2 && bitsPerComponent != 4) return false;
        var samplesPerRow = checked(width * colors);
        var rowBits = (long)samplesPerRow * bitsPerComponent;
        var rowBytes = (int)((rowBits + 7) / 8);
        if ((long)rowBytes * height > data.Length) return false;

        expanded = new byte[samplesPerRow * height];
        var mask = (1 << bitsPerComponent) - 1;
        for (var y = 0; y < height; y++) {
            var rowStart = y * samplesPerRow;
            var baseOffset = y * rowBytes;
            var bitOffset = 0;
            for (var s = 0; s < samplesPerRow; s++) {
                var byteIndex = baseOffset + (bitOffset >> 3);
                var shift = 8 - bitsPerComponent - (bitOffset & 7);
                var value = (data[byteIndex] >> shift) & mask;
                if (scale) {
                    value = (value * 255) / mask;
                }
                expanded[rowStart + s] = (byte)value;
                bitOffset += bitsPerComponent;
            }
        }
        return true;
    }

    private static bool TryExpandIndexed(PdfImageInfo info, byte[] indices, out byte[] rgba) {
        rgba = Array.Empty<byte>();
        if (!TryGetIndexedExpandContext(info, indices, out var context)) return false;

        rgba = DecodeGuards.AllocateRgba32(info.Width, info.Height, PdfImageLimitMessage);
        switch (context.BaseComponents) {
            case 1:
                ExpandIndexedGray(info, indices, rgba, context.PixelCount);
                break;
            case 3:
                ExpandIndexedRgb(info, indices, rgba, context.PixelCount);
                break;
            default:
                ExpandIndexedCmyk(info, indices, rgba, context.PixelCount);
                break;
        }
        return true;
    }

    private readonly struct IndexedExpandContext {
        public IndexedExpandContext(int pixelCount, int baseComponents) {
            PixelCount = pixelCount;
            BaseComponents = baseComponents;
        }

        public int PixelCount { get; }
        public int BaseComponents { get; }
    }

    private static bool TryGetIndexedExpandContext(PdfImageInfo info, byte[] indices, out IndexedExpandContext context) {
        context = default;
        if (info.IndexedLookup is null) return false;
        if (info.IndexedHighVal < 0) return false;
        if (info.Width <= 0 || info.Height <= 0) return false;
        if (!DecodeGuards.TryEnsurePixelCount(info.Width, info.Height, out var pixelCount)) return false;
        if (indices.Length < pixelCount) return false;
        if (!TryGetIndexedBaseComponents(info.IndexedBase, out var baseComponents)) return false;

        var entryCount = info.IndexedHighVal + 1;
        var lookupBytes = DecodeGuards.EnsureByteCount((long)entryCount * baseComponents, "PDF indexed lookup exceeds size limits.");
        if (info.IndexedLookup.Length < lookupBytes) return false;

        context = new IndexedExpandContext(pixelCount, baseComponents);
        return true;
    }

    private static bool TryGetIndexedBaseComponents(PdfColorSpaceKind kind, out int components) {
        components = kind switch {
            PdfColorSpaceKind.DeviceGray => 1,
            PdfColorSpaceKind.DeviceRGB => 3,
            PdfColorSpaceKind.DeviceCMYK => 4,
            _ => 0
        };
        return components != 0;
    }

    private static void ExpandIndexedGray(PdfImageInfo info, byte[] indices, byte[] rgba, int pixelCount) {
        for (var i = 0; i < pixelCount; i++) {
            var index = ClampIndex(indices[i], info.IndexedHighVal);
            var v = info.IndexedLookup![index];
            var dst = i * 4;
            rgba[dst + 0] = v;
            rgba[dst + 1] = v;
            rgba[dst + 2] = v;
            rgba[dst + 3] = 255;
        }
    }

    private static void ExpandIndexedRgb(PdfImageInfo info, byte[] indices, byte[] rgba, int pixelCount) {
        for (var i = 0; i < pixelCount; i++) {
            var index = ClampIndex(indices[i], info.IndexedHighVal);
            var lookupOffset = index * 3;
            var dst = i * 4;
            rgba[dst + 0] = info.IndexedLookup![lookupOffset + 0];
            rgba[dst + 1] = info.IndexedLookup![lookupOffset + 1];
            rgba[dst + 2] = info.IndexedLookup![lookupOffset + 2];
            rgba[dst + 3] = 255;
        }
    }

    private static void ExpandIndexedCmyk(PdfImageInfo info, byte[] indices, byte[] rgba, int pixelCount) {
        for (var i = 0; i < pixelCount; i++) {
            var index = ClampIndex(indices[i], info.IndexedHighVal);
            var lookupOffset = index * 4;
            var c = info.IndexedLookup![lookupOffset + 0];
            var m = info.IndexedLookup![lookupOffset + 1];
            var y = info.IndexedLookup![lookupOffset + 2];
            var k = info.IndexedLookup![lookupOffset + 3];
            var dst = i * 4;
            rgba[dst + 0] = (byte)(255 - Math.Min(255, c + k));
            rgba[dst + 1] = (byte)(255 - Math.Min(255, m + k));
            rgba[dst + 2] = (byte)(255 - Math.Min(255, y + k));
            rgba[dst + 3] = 255;
        }
    }

    private static int ClampIndex(byte index, int max) {
        return index > max ? max : index;
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

    private static bool TryDecodeAsciiHex(ReadOnlySpan<byte> src, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        var buffer = new System.Collections.Generic.List<byte>();
        var highNibble = -1;
        for (var i = 0; i < src.Length; i++) {
            var b = src[i];
            if (b == (byte)'>') {
                if (highNibble >= 0) {
                    buffer.Add((byte)(highNibble << 4));
                }
                decoded = buffer.ToArray();
                return true;
            }
            if (b <= 32) continue;
            var nibble = HexToNibble(b);
            if (nibble < 0) return false;
            if (highNibble < 0) {
                highNibble = nibble;
            } else {
                buffer.Add((byte)((highNibble << 4) | nibble));
                highNibble = -1;
            }
        }
        if (highNibble >= 0) {
            buffer.Add((byte)(highNibble << 4));
        }
        decoded = buffer.ToArray();
        return true;
    }

    private static bool TryDecodeLzw(PdfImageInfo info, ReadOnlySpan<byte> src, out byte[] decoded) {
        decoded = Array.Empty<byte>();
        if (!TryGetExpectedDecodedLength(info, out var expected)) return false;
        try {
            if (info.LzwEarlyChange == 0 || info.LzwEarlyChange == 1) {
                decoded = DecompressLzw(src, expected, info.LzwEarlyChange);
            } else {
                decoded = DecompressLzwCompat(src, expected);
            }
            return true;
        } catch (FormatException) {
            if (info.LzwEarlyChange == 0 || info.LzwEarlyChange == 1) {
                try {
                    decoded = DecompressLzwCompat(src, expected);
                    return true;
                } catch (FormatException) {
                    return false;
                }
            }
            return false;
        }
    }

    private static bool TryGetExpectedDecodedLength(PdfImageInfo info, out int expected) {
        expected = 0;
        if (info.Width <= 0 || info.Height <= 0) return false;

        var colors = info.Colors;
        if (info.ColorSpaceKind == PdfColorSpaceKind.Indexed) {
            colors = 1;
        } else if (colors <= 0) {
            colors = info.ColorSpaceKind switch {
                PdfColorSpaceKind.DeviceGray => 1,
                PdfColorSpaceKind.DeviceRGB => 3,
                PdfColorSpaceKind.DeviceCMYK => 4,
                _ => 0
            };
        }
        if (colors <= 0) return false;

        var bits = info.BitsPerComponent;
        if (bits <= 0) return false;

        try {
            if (bits == 8) {
                var rowSize = checked(info.Width * colors);
                expected = info.Predictor >= 10
                    ? checked((rowSize + 1) * info.Height)
                    : checked(rowSize * info.Height);
                return true;
            }
            var rowBits = checked(info.Width * colors * bits);
            var rowBytes = (rowBits + 7) / 8;
            expected = info.Predictor >= 10
                ? checked((rowBytes + 1) * info.Height)
                : checked(rowBytes * info.Height);
            return true;
        } catch (OverflowException) {
            expected = 0;
            return false;
        }
    }

    private static byte[] DecompressLzwCompat(ReadOnlySpan<byte> src, int expected) {
        FormatException? last = null;
        var attempts = new[] { 1, 0 };
        foreach (var earlyChange in attempts) {
            try {
                return DecompressLzw(src, expected, earlyChange);
            } catch (FormatException ex) {
                last = ex;
            }
        }
        throw last ?? new FormatException("Invalid PDF LZW data.");
    }

    private static byte[] DecompressLzw(ReadOnlySpan<byte> src, int expected, int earlyChange) {
        if (expected <= 0) throw new FormatException("Invalid PDF LZW output size.");
        var prefix = new short[4096];
        var suffix = new byte[4096];
        var stack = new byte[4096];
        var output = new byte[expected];

        for (var i = 0; i < 256; i++) {
            prefix[i] = -1;
            suffix[i] = (byte)i;
        }

        var bitPos = 0;
        var codeSize = 9;
        const int clear = 256;
        const int eoi = 257;
        var nextCode = 258;
        var oldCode = -1;
        var outIndex = 0;
        byte firstChar = 0;

        while (true) {
            var code = ReadBitsMsb(src, ref bitPos, codeSize);
            if (code < 0) break;
            if (code == clear) {
                codeSize = 9;
                nextCode = 258;
                oldCode = -1;
                continue;
            }
            if (code == eoi) break;

            var inCode = code;
            var stackTop = 0;
            if (code >= nextCode) {
                if (oldCode < 0) throw new FormatException("Invalid PDF LZW stream.");
                if (stackTop >= stack.Length) throw new FormatException("Invalid PDF LZW stack overflow.");
                stack[stackTop++] = firstChar;
                code = oldCode;
            }

            while (code >= 256) {
                if ((uint)code >= 4096) throw new FormatException("Invalid PDF LZW code.");
                if (stackTop >= stack.Length) throw new FormatException("Invalid PDF LZW stack overflow.");
                stack[stackTop++] = suffix[code];
                code = prefix[code];
            }

            firstChar = (byte)code;
            if (stackTop >= stack.Length) throw new FormatException("Invalid PDF LZW stack overflow.");
            stack[stackTop++] = firstChar;

            while (stackTop > 0) {
                if (outIndex >= output.Length) throw new FormatException("PDF LZW output too large.");
                output[outIndex++] = stack[--stackTop];
            }

            if (oldCode >= 0) {
                if (nextCode < 4096) {
                    prefix[nextCode] = (short)oldCode;
                    suffix[nextCode] = firstChar;
                    nextCode++;
                    if (nextCode == (1 << codeSize) - earlyChange && codeSize < 12) {
                        codeSize++;
                    }
                }
            }
            oldCode = inCode;
        }

        if (outIndex != output.Length) throw new FormatException("PDF LZW output truncated.");
        return output;
    }

    private static int ReadBitsMsb(ReadOnlySpan<byte> data, ref int bitPos, int bitCount) {
        var totalBits = data.Length * 8;
        if (bitPos + bitCount > totalBits) return -1;
        var value = 0;
        for (var i = 0; i < bitCount; i++) {
            var bitIndex = bitPos + i;
            var byteIndex = bitIndex >> 3;
            var shift = 7 - (bitIndex & 7);
            var bit = (data[byteIndex] >> shift) & 1;
            value = (value << 1) | bit;
        }
        bitPos += bitCount;
        return value;
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
        public PdfImageInfo(
            int width,
            int height,
            int bitsPerComponent,
            int colors,
            PdfColorSpaceKind colorSpaceKind,
            PdfColorSpaceKind indexedBase,
            int indexedHighVal,
            byte[]? indexedLookup,
            string[]? filters,
            int predictor,
            int lzwEarlyChange,
            int softMaskObj,
            int softMaskGen,
            int maskObj,
            int maskGen,
            bool isImageMask,
            int streamLength,
            float[]? decode,
            float[]? mask) {
            Width = width;
            Height = height;
            BitsPerComponent = bitsPerComponent;
            Colors = colors;
            ColorSpaceKind = colorSpaceKind;
            IndexedBase = indexedBase;
            IndexedHighVal = indexedHighVal;
            IndexedLookup = indexedLookup;
            Filters = filters;
            Predictor = predictor;
            LzwEarlyChange = lzwEarlyChange;
            SoftMaskObj = softMaskObj;
            SoftMaskGen = softMaskGen;
            MaskObj = maskObj;
            MaskGen = maskGen;
            IsImageMask = isImageMask;
            StreamLength = streamLength;
            Decode = decode;
            Mask = mask;
        }

        public int Width { get; }
        public int Height { get; }
        public int BitsPerComponent { get; }
        public int Colors { get; }
        public PdfColorSpaceKind ColorSpaceKind { get; }
        public PdfColorSpaceKind IndexedBase { get; }
        public int IndexedHighVal { get; }
        public byte[]? IndexedLookup { get; }
        public string[]? Filters { get; }
        public int Predictor { get; }
        public int LzwEarlyChange { get; }
        public int SoftMaskObj { get; }
        public int SoftMaskGen { get; }
        public int MaskObj { get; }
        public int MaskGen { get; }
        public bool IsImageMask { get; }
        public int StreamLength { get; }
        public float[]? Decode { get; }
        public float[]? Mask { get; }
    }
}

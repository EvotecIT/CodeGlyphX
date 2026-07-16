using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.DotCode;
using CodeGlyphX.Internal;
using CodeGlyphX.Internal.ReedSolomon;

namespace CodeGlyphX;

/// <summary>Encodes AIM DotCode symbols with GS1, ECI, structured append, and selectable masks.</summary>
public static class DotCodeEncoder {
    /// <summary>Encodes text using automatic dimensions and masking.</summary>
    public static DotCodeSymbol EncodeText(string text) => EncodeText(text, new DotCodeEncodingOptions());

    /// <summary>Encodes text with explicit DotCode options.</summary>
    public static DotCodeSymbol EncodeText(string text, DotCodeEncodingOptions options) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (options is null) throw new ArgumentNullException(nameof(options));
        options = options.Clone();
        if (options.IsGs1) text = Gs1.ElementString(text);

        var encoding = options.TextEncoding;
        var eci = options.EciAssignmentNumber;
        if (encoding is null) {
            var latin1 = true;
            for (var i = 0; i < text.Length; i++) if (text[i] > 255) { latin1 = false; break; }
            encoding = latin1 ? EncodingUtils.Latin1 : Encoding.UTF8;
            if (!latin1 && !eci.HasValue) eci = 26;
        } else if (!eci.HasValue) {
            eci = EncodingToEci(encoding);
        }
        return EncodeBytesCore(encoding.GetBytes(text), options, eci ?? 0);
    }

    /// <summary>Encodes an arbitrary byte payload.</summary>
    public static DotCodeSymbol EncodeBytes(byte[] data, DotCodeEncodingOptions? options = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return EncodeBytesCore((byte[])data.Clone(), (options ?? new DotCodeEncodingOptions()).Clone(), options?.EciAssignmentNumber ?? 0);
    }

    /// <summary>Encodes and validates a GS1 AI string.</summary>
    public static DotCodeSymbol EncodeGs1(string aiText, DotCodeEncodingOptions? options = null) {
        if (aiText is null) throw new ArgumentNullException(nameof(aiText));
        var effective = (options ?? new DotCodeEncodingOptions()).Clone();
        effective.IsGs1 = true;
        return EncodeText(aiText, effective);
    }

    private static DotCodeSymbol EncodeBytesCore(byte[] data, DotCodeEncodingOptions options, int eci) {
        Validate(options, eci);
        var highLevel = DotCodeHighLevelEncoder.Encode(data, options, eci);
        var structuredAppend = BuildStructuredAppend(options, highLevel.FinalMode);
        var codewords = highLevel.Codewords;
        var dataLength = codewords.Count + structuredAppend.Count;
        var initialEccLength = 3 + dataLength / 2;
        var minimumDots = 9 * (dataLength + initialEccLength) + 2;
        var minimumArea = minimumDots * 2;
        ResolveDimensions(minimumArea, options.Width, out var width, out var height);
        if (width > 200 || height > 200) throw new ArgumentException("The DotCode payload requires dimensions larger than 200 by 200 modules.", nameof(data));

        var dotCapacity = width * height / 2;
        var paddingDots = dotCapacity - minimumDots;
        if (paddingDots >= 9) {
            var first = true;
            while (paddingDots >= 9) {
                if (paddingDots < 18 && (dataLength & 1) == 0) paddingDots -= 9;
                else if (paddingDots >= 18) paddingDots -= (dataLength & 1) == 0 ? 9 : 18;
                else break;
                codewords.Add(first && highLevel.BinaryFinish ? 109 : 106);
                dataLength++;
                first = false;
            }
            if (structuredAppend.Count > 0 && structuredAppend[0] == 109) structuredAppend[0] = 106;
        }
        codewords.AddRange(structuredAppend);
        if (codewords.Count != dataLength) throw new InvalidOperationException("DotCode padding produced an inconsistent data length.");

        var eccLength = 3 + dataLength / 2;
        var selectedMask = options.Mask ?? SelectMask(codewords, dataLength, eccLength, width, height, dotCapacity);
        var finalWords = ApplyMask(codewords, dataLength, eccLength, selectedMask % 4);
        var stream = BuildStream(finalWords, dotCapacity);
        var modules = DotCodeMatrix.Fold(stream, width, height);
        if (selectedMask >= 4) DotCodeMatrix.ForceCorners(modules);
        return new DotCodeSymbol(modules, selectedMask, dataLength, eccLength);
    }

    private static int SelectMask(List<int> codewords, int dataLength, int eccLength, int width, int height, int dotCapacity) {
        var bestMask = 0;
        var highScore = int.MinValue;
        for (var mask = 0; mask < 4; mask++) {
            var matrix = DotCodeMatrix.Fold(BuildStream(ApplyMask(codewords, dataLength, eccLength, mask), dotCapacity), width, height);
            var score = DotCodeMatrix.Score(matrix);
            if (score >= highScore) { highScore = score; bestMask = mask; }
        }
        if (highScore > width * height / 2) return bestMask;
        for (var mask = 0; mask < 4; mask++) {
            var matrix = DotCodeMatrix.Fold(BuildStream(ApplyMask(codewords, dataLength, eccLength, mask), dotCapacity), width, height);
            DotCodeMatrix.ForceCorners(matrix);
            var score = DotCodeMatrix.Score(matrix);
            if (score >= highScore) { highScore = score; bestMask = mask + 4; }
        }
        return bestMask;
    }

    private static int[] ApplyMask(List<int> codewords, int dataLength, int eccLength, int mask) {
        var result = new int[dataLength + 1 + eccLength];
        result[0] = mask;
        var increment = mask == 1 ? 3 : mask == 2 ? 7 : mask == 3 ? 17 : 0;
        var weight = 0;
        for (var i = 0; i < dataLength; i++) { result[i + 1] = (codewords[i] + weight) % DotCodeTables.Prime; weight += increment; }
        PrimeFieldReedSolomon.EncodeInterleaved(result, dataLength + 1, eccLength, DotCodeTables.Prime, DotCodeTables.Primitive);
        return result;
    }

    private static bool[] BuildStream(int[] words, int dotCapacity) {
        var stream = new bool[dotCapacity];
        var position = 0;
        AppendBits(stream, ref position, words[0], 2);
        for (var i = 1; i < words.Length; i++) AppendBits(stream, ref position, DotCodeTables.DotPatterns[words[i]], 9);
        while (position < stream.Length) stream[position++] = true;
        return stream;
    }

    private static void AppendBits(bool[] stream, ref int position, int value, int count) {
        for (var bit = count - 1; bit >= 0; bit--) stream[position++] = (value & 1 << bit) != 0;
    }

    private static List<int> BuildStructuredAppend(DotCodeEncodingOptions options, char finalMode) {
        var result = new List<int>(4);
        if (!options.StructuredAppendCount.HasValue) return result;
        var index = options.StructuredAppendIndex!.Value;
        var count = options.StructuredAppendCount.Value;
        if (finalMode == 'C') result.Add(101);
        else if (finalMode == 'X') result.Add(109);
        result.Add(index < 10 ? 16 + index : 33 + index - 10);
        result.Add(count < 10 ? 16 + count : 33 + count - 10);
        result.Add(108);
        return result;
    }

    private static void ResolveDimensions(int minimumArea, int? requestedWidth, out int width, out int height) {
        if (requestedWidth.HasValue) {
            width = requestedWidth.Value;
            height = Math.Max(5, (minimumArea + width - 1) / width);
            if (((width + height) & 1) == 0) height++;
            return;
        }

        var h = (float)Math.Sqrt(minimumArea * 0.666);
        var w = (float)Math.Sqrt(minimumArea * 1.5);
        height = (int)h;
        width = (int)w;
        if (((width + height) & 1) != 0) {
            if (width * height < minimumArea) { width++; height++; }
        } else if (h * width < w * height) {
            width++;
            if (width * height < minimumArea) { width--; height++; if (width * height < minimumArea) width += 2; }
        } else {
            height++;
            if (width * height < minimumArea) { width++; height--; if (width * height < minimumArea) height += 2; }
        }
    }

    private static void Validate(DotCodeEncodingOptions options, int eci) {
        if (options.Width is < 5 or > 200) throw new ArgumentOutOfRangeException(nameof(options), options.Width, "DotCode width must be between 5 and 200 modules.");
        if (options.Mask is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(options), options.Mask, "DotCode mask must be between 0 and 7.");
        if (eci is < 0 or > 811799) throw new ArgumentOutOfRangeException(nameof(options), eci, "DotCode ECI assignments must be between 0 and 811799.");
        if (options.ReaderInitialization && options.IsGs1) throw new ArgumentException("Reader initialization cannot be combined with GS1 DotCode.", nameof(options));
        if (options.StructuredAppendCount.HasValue != options.StructuredAppendIndex.HasValue) throw new ArgumentException("Structured append requires both index and count.", nameof(options));
        if (options.StructuredAppendCount is < 2 or > 35) throw new ArgumentOutOfRangeException(nameof(options), options.StructuredAppendCount, "DotCode structured-append count must be between 2 and 35.");
        if (options.StructuredAppendIndex.HasValue && (options.StructuredAppendIndex < 1 || options.StructuredAppendIndex > options.StructuredAppendCount)) {
            throw new ArgumentOutOfRangeException(nameof(options), options.StructuredAppendIndex, "DotCode structured-append index must be between 1 and count.");
        }
    }

    private static int? EncodingToEci(Encoding encoding) {
        if (encoding.CodePage == Encoding.UTF8.CodePage) return 26;
        if (encoding.CodePage == 28591) return 3;
        if (encoding.CodePage == 932) return 20;
        if (encoding.CodePage == 1250) return 21;
        if (encoding.CodePage == 1251) return 22;
        if (encoding.CodePage == 1252) return 23;
        return null;
    }
}

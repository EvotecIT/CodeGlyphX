using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixEncoder {
    /// <summary>
    /// Encodes a GS1 element string with FNC1 in first position.
    /// </summary>
    /// <remarks>
    /// Supply the machine-readable element string without human-readable parentheses and use
    /// ASCII group separator (<c>\u001D</c>) after variable-length fields when another field follows.
    /// </remarks>
    public static BitMatrix EncodeGs1(string elementString, DataMatrixEncodingOptions? options = null) {
        if (elementString is null) throw new ArgumentNullException(nameof(elementString));
        var effective = options?.Clone() ?? new DataMatrixEncodingOptions();
        effective.IsGs1 = true;
        return Encode(elementString, effective);
    }

    /// <summary>
    /// Encodes a Macro 05 payload body. The decoder reconstructs the standard envelope.
    /// </summary>
    public static BitMatrix EncodeMacro05(string body, DataMatrixEncodingOptions? options = null) {
        return EncodeMacro(body, DataMatrixMacro.Macro05, options);
    }

    /// <summary>
    /// Encodes a Macro 06 payload body. The decoder reconstructs the standard envelope.
    /// </summary>
    public static BitMatrix EncodeMacro06(string body, DataMatrixEncodingOptions? options = null) {
        return EncodeMacro(body, DataMatrixMacro.Macro06, options);
    }

    /// <summary>
    /// Encodes two through sixteen pre-split text parts as one Data Matrix structured-append sequence.
    /// </summary>
    public static BitMatrix[] EncodeStructuredAppend(
        IReadOnlyList<string> parts,
        int fileId1 = 1,
        int fileId2 = 1,
        DataMatrixEncodingOptions? options = null) {
        if (parts is null) throw new ArgumentNullException(nameof(parts));
        ValidateStructuredAppendParts(parts.Count, fileId1, fileId2);
        var baseline = options?.Clone() ?? new DataMatrixEncodingOptions();
        var result = new BitMatrix[parts.Count];
        for (var i = 0; i < parts.Count; i++) {
            if (parts[i] is null) throw new ArgumentException("Structured-append parts cannot contain null values.", nameof(parts));
            var effective = baseline.Clone();
            effective.StructuredAppend = new DataMatrixStructuredAppend(i + 1, parts.Count, fileId1, fileId2);
            result[i] = Encode(parts[i], effective);
        }
        return result;
    }

    /// <summary>
    /// Encodes two through sixteen pre-split binary parts as one Data Matrix structured-append sequence.
    /// </summary>
    public static BitMatrix[] EncodeStructuredAppend(
        IReadOnlyList<byte[]> parts,
        int fileId1 = 1,
        int fileId2 = 1,
        DataMatrixEncodingOptions? options = null) {
        if (parts is null) throw new ArgumentNullException(nameof(parts));
        ValidateStructuredAppendParts(parts.Count, fileId1, fileId2);
        var baseline = options?.Clone() ?? new DataMatrixEncodingOptions();
        var result = new BitMatrix[parts.Count];
        for (var i = 0; i < parts.Count; i++) {
            if (parts[i] is null) throw new ArgumentException("Structured-append parts cannot contain null values.", nameof(parts));
            var effective = baseline.Clone();
            effective.StructuredAppend = new DataMatrixStructuredAppend(i + 1, parts.Count, fileId1, fileId2);
            result[i] = EncodeBytes(parts[i], effective);
        }
        return result;
    }

    private static BitMatrix EncodeMacro(string body, DataMatrixMacro macro, DataMatrixEncodingOptions? options) {
        if (body is null) throw new ArgumentNullException(nameof(body));
        var effective = options?.Clone() ?? new DataMatrixEncodingOptions();
        effective.Macro = macro;
        return Encode(body, effective);
    }

    private static bool TryExtractMacroEnvelope(string text, out string body, out DataMatrixMacro macro) {
        body = string.Empty;
        macro = DataMatrixMacro.None;
        if (text.Length < 9
            || text[0] != '['
            || text[1] != ')'
            || text[2] != '>'
            || text[3] != '\u001E'
            || text[4] != '0'
            || (text[5] != '5' && text[5] != '6')
            || text[6] != '\u001D'
            || text[text.Length - 2] != '\u001E'
            || text[text.Length - 1] != '\u0004') {
            return false;
        }
        macro = text[5] == '5' ? DataMatrixMacro.Macro05 : DataMatrixMacro.Macro06;
        body = text.Substring(7, text.Length - 9);
        return true;
    }

    private static void ValidateStructuredAppendParts(int count, int fileId1, int fileId2) {
        if (count is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(count), "Structured append requires two through sixteen parts.");
        if (fileId1 is < 1 or > 254) throw new ArgumentOutOfRangeException(nameof(fileId1));
        if (fileId2 is < 1 or > 254) throw new ArgumentOutOfRangeException(nameof(fileId2));
    }
}

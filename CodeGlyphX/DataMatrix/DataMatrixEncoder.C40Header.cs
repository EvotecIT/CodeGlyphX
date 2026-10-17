using System;

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixEncoder {
    private static BitMatrix EncodeC40BasicHeader(string text, DataMatrixEncodingOptions options) {
        var headerLength = options.C40BasicHeaderLength;
        if (headerLength > text.Length) throw new ArgumentException("The C40 header exceeds the string length.", nameof(text));
        for (var i = 0; i < headerLength; i++)
            if (!IsBasicC40(text[i])) throw new ArgumentException("The C40 header requires uppercase letters, digits and spaces.", nameof(text));
        // Continue C40 over complete basic triples in customer content rather
        // than wasting an unlatch/latch pair at the application header boundary.
        var end = headerLength;
        while (end < text.Length && IsBasicC40(text[end])) end++;
        end -= (end - headerLength) % 3;
        var words = EncodeC40Text(text.Substring(0, end), isText: false);
        if (end < text.Length) words.AddRange(EncodeOptimized(text.Substring(end), false, words.Count));
        return EncodeCodewords(words, options);
    }

    private static bool IsBasicC40(char value) => value == ' ' || (value >= '0' && value <= '9') || (value >= 'A' && value <= 'Z');
}

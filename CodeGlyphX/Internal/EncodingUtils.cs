using System.Text;

namespace CodeGlyphX.Internal;

internal static class EncodingUtils {
    internal static Encoding Utf8Strict { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    internal static Encoding Latin1 {
        get {
#if NET7_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return Encoding.Latin1;
#else
            return Encoding.GetEncoding("iso-8859-1");
#endif
        }
    }
}

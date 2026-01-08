using System.Text;

namespace CodeGlyphX.Internal;

internal static class EncodingUtils {
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

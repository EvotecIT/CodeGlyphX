using System;
using System.Text;

namespace CodeGlyphX.Internal;

internal static class EncodingUtils {
    internal static Encoding Utf8Strict { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Encodes text without permitting replacement fallbacks that would change the caller's payload.</summary>
    internal static byte[] GetBytesStrict(Encoding encoding, string text, string parameterName) {
        var strict = (Encoding)encoding.Clone();
        strict.EncoderFallback = EncoderFallback.ExceptionFallback;
        try {
            return strict.GetBytes(text);
        } catch (EncoderFallbackException exception) {
            throw new ArgumentException($"Text cannot be represented by {encoding.WebName}.", parameterName, exception);
        }
    }

    internal static Encoding Latin1 {
        get {
#if NET7_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return Encoding.Latin1;
#else
            return Encoding.GetEncoding("iso-8859-1");
#endif
        }
    }

    internal static bool TryGetEciAssignment(Encoding encoding, out int assignment) {
        switch (encoding.CodePage) {
            case 28591: assignment = 3; return true;
            case 932: assignment = 20; return true;
            case 1250: assignment = 21; return true;
            case 1251: assignment = 22; return true;
            case 1252: assignment = 23; return true;
            case 1201: assignment = 25; return true;
            case 65001: assignment = 26; return true;
            case 20127: assignment = 27; return true;
            case 936: assignment = 29; return true;
            case 54936: assignment = 32; return true;
            default: assignment = 0; return false;
        }
    }

    internal static bool TryGetEncoding(int assignment, out Encoding encoding) {
        try {
            switch (assignment) {
                case 3: encoding = Latin1; return true;
                case 20: encoding = Encoding.GetEncoding(932); return true;
                case 21: encoding = Encoding.GetEncoding(1250); return true;
                case 22: encoding = Encoding.GetEncoding(1251); return true;
                case 23: encoding = Encoding.GetEncoding(1252); return true;
                case 25: encoding = Encoding.BigEndianUnicode; return true;
                case 26: encoding = Utf8Strict; return true;
                case 27: encoding = Encoding.ASCII; return true;
                case 29: encoding = Encoding.GetEncoding("GB2312"); return true;
                case 32: encoding = Encoding.GetEncoding(54936); return true;
                default: encoding = Latin1; return false;
            }
        } catch (ArgumentException) {
            encoding = Latin1;
            return false;
        } catch (NotSupportedException) {
            encoding = Latin1;
            return false;
        }
    }
}

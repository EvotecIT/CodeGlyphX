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

    /// <summary>
    /// Resolves the byte encoding and ECI declaration as one contract so an explicit ECI can never describe
    /// different bytes from the ones emitted by a text encoder.
    /// </summary>
    internal static Encoding ResolveTextEncoding(
        string text,
        Encoding? requestedEncoding,
        int? requestedEci,
        string formatName,
        out int? eci) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        eci = requestedEci;

        if (requestedEncoding is not null) {
            if (TryGetEciAssignment(requestedEncoding, out var inferredEci)) {
                if (eci.HasValue && eci.Value != inferredEci) {
                    throw new InvalidOperationException(
                        $"The selected {formatName} text encoding uses ECI {inferredEci}, but ECI {eci.Value} was requested.");
                }
                eci ??= inferredEci;
            } else if (!eci.HasValue) {
                throw new InvalidOperationException(
                    $"The selected {formatName} text encoding has no known ECI assignment. Set EciAssignmentNumber explicitly.");
            } else if (TryGetEncoding(eci.Value, out var mappedEncoding) && mappedEncoding.CodePage != requestedEncoding.CodePage) {
                throw new InvalidOperationException(
                    $"ECI {eci.Value} identifies {mappedEncoding.WebName}, not the selected {requestedEncoding.WebName} encoding.");
            }
            return requestedEncoding;
        }

        if (eci.HasValue) {
            if (TryGetEncoding(eci.Value, out var mappedEncoding)) return mappedEncoding;
            throw new InvalidOperationException(
                $"ECI {eci.Value} has no known {formatName} text encoding. Set TextEncoding explicitly or encode bytes directly.");
        }

        for (var i = 0; i < text.Length; i++) {
            if (text[i] <= 0xFF) continue;
            eci = 26;
            return Utf8Strict;
        }
        return Latin1;
    }

    internal static bool TryGetEciAssignment(Encoding encoding, out int assignment) {
        switch (encoding.CodePage) {
            case 437: assignment = 0; return true;
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
                case 0: return TryGetCodePageEncoding(437, out encoding);
                case 3: encoding = Latin1; return true;
                case 20: return TryGetCodePageEncoding(932, out encoding);
                case 21: return TryGetCodePageEncoding(1250, out encoding);
                case 22: return TryGetCodePageEncoding(1251, out encoding);
                case 23: return TryGetCodePageEncoding(1252, out encoding);
                case 25: encoding = Encoding.BigEndianUnicode; return true;
                case 26: encoding = Utf8Strict; return true;
                case 27: encoding = Encoding.ASCII; return true;
                case 29: return TryGetCodePageEncoding(936, out encoding);
                case 32: return TryGetCodePageEncoding(54936, out encoding);
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

    private static bool TryGetCodePageEncoding(int codePage, out Encoding encoding) {
        var mappedEncoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage);
        if (mappedEncoding is not null) {
            encoding = mappedEncoding;
            return true;
        }
        encoding = Latin1;
        return false;
    }
}

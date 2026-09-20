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
                if (eci.HasValue && eci.Value != inferredEci
                    && (!TryGetEncoding(eci.Value, out var aliasEncoding) || aliasEncoding.CodePage != requestedEncoding.CodePage)) {
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
            case 28592: assignment = 4; return true;
            case 28593: assignment = 5; return true;
            case 28594: assignment = 6; return true;
            case 28595: assignment = 7; return true;
            case 28596: assignment = 8; return true;
            case 28597: assignment = 9; return true;
            case 28598: assignment = 10; return true;
            case 28599: assignment = 11; return true;
            case 28600: assignment = 12; return true;
            case 28601: assignment = 13; return true;
            case 28603: assignment = 15; return true;
            case 28604: assignment = 16; return true;
            case 28605: assignment = 17; return true;
            case 28606: assignment = 18; return true;
            case 932: assignment = 20; return true;
            case 1250: assignment = 21; return true;
            case 1251: assignment = 22; return true;
            case 1252: assignment = 23; return true;
            case 1256: assignment = 24; return true;
            case 1201: assignment = 25; return true;
            case 65001: assignment = 26; return true;
            case 20127: assignment = 27; return true;
            case 950: assignment = 28; return true;
            case 936: assignment = 29; return true;
            case 51949: assignment = 30; return true;
            case 54936: assignment = 32; return true;
            default: assignment = 0; return false;
        }
    }

    internal static bool TryGetEncoding(int assignment, out Encoding encoding) {
        try {
            switch (assignment) {
                case 0:
                case 2: return TryGetCodePageEncoding(437, out encoding);
                case 1:
                case 3: encoding = Latin1; return true;
                case 4: return TryGetCodePageEncoding(28592, out encoding);
                case 5: return TryGetCodePageEncoding(28593, out encoding);
                case 6: return TryGetCodePageEncoding(28594, out encoding);
                case 7: return TryGetCodePageEncoding(28595, out encoding);
                case 8: return TryGetCodePageEncoding(28596, out encoding);
                case 9: return TryGetCodePageEncoding(28597, out encoding);
                case 10: return TryGetCodePageEncoding(28598, out encoding);
                case 11: return TryGetCodePageEncoding(28599, out encoding);
                case 12: return TryGetCodePageEncoding(28600, out encoding);
                case 13: return TryGetCodePageEncoding(28601, out encoding);
                case 15: return TryGetCodePageEncoding(28603, out encoding);
                case 16: return TryGetCodePageEncoding(28604, out encoding);
                case 17: return TryGetCodePageEncoding(28605, out encoding);
                case 18: return TryGetCodePageEncoding(28606, out encoding);
                case 20: return TryGetCodePageEncoding(932, out encoding);
                case 21: return TryGetCodePageEncoding(1250, out encoding);
                case 22: return TryGetCodePageEncoding(1251, out encoding);
                case 23: return TryGetCodePageEncoding(1252, out encoding);
                case 24: return TryGetCodePageEncoding(1256, out encoding);
                case 25: encoding = Encoding.BigEndianUnicode; return true;
                case 26: encoding = Utf8Strict; return true;
                case 170:
                case 27: encoding = Encoding.ASCII; return true;
                case 28: return TryGetCodePageEncoding(950, out encoding);
                case 29: return TryGetCodePageEncoding(936, out encoding);
                case 30: return TryGetCodePageEncoding(51949, out encoding);
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

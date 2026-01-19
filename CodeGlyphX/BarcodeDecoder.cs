using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Code11;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.Code93;
using CodeGlyphX.Codabar;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
using CodeGlyphX.Internal;
using CodeGlyphX.Msi;
using CodeGlyphX.Plessey;
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;

namespace CodeGlyphX;

/// <summary>
/// Best-effort 1D barcode decoder (scanline-based).
/// </summary>
public static class BarcodeDecoder {
    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, null, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, expectedType, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with custom decoding options.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, null, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with an optional type hint and custom decoding options.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with an optional type hint, custom options, and cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates)) return false;
        for (var i = 0; i < candidates.Length; i++) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (TryDecodeWithTransforms(candidates[i], expectedType, options, cancellationToken, out decoded)) return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded, out BarcodeDecodeDiagnostics diagnostics) {
        decoded = null!;
        diagnostics = new BarcodeDecodeDiagnostics();
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates)) {
            diagnostics.Failure = "No scanline candidates.";
            return false;
        }
        diagnostics.CandidateCount = candidates.Length;
        for (var i = 0; i < candidates.Length; i++) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (TryDecodeWithTransforms(candidates[i], expectedType, options, cancellationToken, diagnostics, out decoded)) {
                diagnostics.Success = true;
                return true;
            }
        }
        diagnostics.Failure ??= "No supported barcode decoded.";
        return false;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, null, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, expectedType, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with custom decoding options.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, null, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with an optional type hint and custom decoding options.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecode(pixels, width, height, stride, format, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with an optional type hint, custom options, and cancellation.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates)) return false;
        for (var i = 0; i < candidates.Length; i++) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (TryDecodeWithTransforms(candidates[i], expectedType, options, cancellationToken, out decoded)) return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a raw pixel buffer with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded, out BarcodeDecodeDiagnostics diagnostics) {
        decoded = null!;
        diagnostics = new BarcodeDecodeDiagnostics();
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
        if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates)) {
            diagnostics.Failure = "No scanline candidates.";
            return false;
        }
        diagnostics.CandidateCount = candidates.Length;
        for (var i = 0; i < candidates.Length; i++) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            if (TryDecodeWithTransforms(candidates[i], expectedType, options, cancellationToken, diagnostics, out decoded)) {
                diagnostics.Success = true;
                return true;
            }
        }
        diagnostics.Failure ??= "No supported barcode decoded.";
        return false;
    }
#endif

    /// <summary>
    /// Attempts to decode a 1D barcode from a module sequence.
    /// </summary>
    public static bool TryDecode(bool[] modules, out BarcodeDecoded decoded) {
        return TryDecode(modules, null, null, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a module sequence.
    /// </summary>
    public static bool TryDecode(bool[] modules, BarcodeType? expectedType, out BarcodeDecoded decoded) {
        return TryDecode(modules, expectedType, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a module sequence with custom decoding options.
    /// </summary>
    public static bool TryDecode(bool[] modules, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecode(modules, null, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a module sequence with an optional type hint and custom decoding options.
    /// </summary>
    public static bool TryDecode(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecode(modules, expectedType, options, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a 1D barcode from a module sequence with an optional type hint, custom decoding options, and cancellation.
    /// </summary>
    public static bool TryDecode(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        if (modules is null || modules.Length == 0) return false;
        return TryDecodeWithTransforms(modules, expectedType, options, cancellationToken, out decoded);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodeWithTransforms(modules, expectedType, options, CancellationToken.None, null, out decoded);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeWithTransforms(modules, expectedType, options, cancellationToken, null, out decoded);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, BarcodeDecodeDiagnostics? diagnostics, out BarcodeDecoded decoded) {
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (TryDecodeCoreTrimmed(modules, expectedType, options, cancellationToken, diagnostics, out decoded)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var inverted = InvertModules(modules);
        if (diagnostics is not null) diagnostics.InvertedTried = true;
        if (TryDecodeCoreTrimmed(inverted, expectedType, options, cancellationToken, diagnostics, out decoded)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var reversed = ReverseModules(modules);
        if (diagnostics is not null) diagnostics.ReversedTried = true;
        if (TryDecodeCoreTrimmed(reversed, expectedType, options, cancellationToken, diagnostics, out decoded)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var invertedReversed = InvertModules(reversed);
        if (diagnostics is not null) diagnostics.InvertedTried = true;
        return TryDecodeCoreTrimmed(invertedReversed, expectedType, options, cancellationToken, diagnostics, out decoded);
    }

    private static bool TryDecodeCoreTrimmed(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, BarcodeDecodeDiagnostics? diagnostics, out BarcodeDecoded decoded) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        var trimmed = TrimModules(modules);
        if (trimmed.Length == 0) return false;
        if (diagnostics is not null) diagnostics.AttemptCount++;
        return TryDecodeCore(trimmed, expectedType, options, cancellationToken, out decoded);
    }

    private static bool TryDecodeCore(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        if (expectedType.HasValue) {
            return TryDecodeType(expectedType.Value, modules, options, out decoded);
        }

        // Fixed-length symbologies first.
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeEan8(modules, out var ean8)) {
            decoded = new BarcodeDecoded(BarcodeType.EAN, ean8);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeUpcA(modules, out var upca)) {
            decoded = new BarcodeDecoded(BarcodeType.UPCA, upca);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeEan13(modules, out var ean13)) {
            decoded = new BarcodeDecoded(BarcodeType.EAN, ean13);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeUpcE(modules, out var upce)) {
            decoded = new BarcodeDecoded(BarcodeType.UPCE, upce);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeItf14(modules, out var itf14)) {
            decoded = new BarcodeDecoded(BarcodeType.ITF14, itf14);
            return true;
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode128(modules, out var code128, out var isGs1)) {
            decoded = new BarcodeDecoded(isGs1 ? BarcodeType.GS1_128 : BarcodeType.Code128, code128);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode39(modules, options, out var code39)) {
            decoded = new BarcodeDecoded(BarcodeType.Code39, code39);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode93(modules, out var code93)) {
            decoded = new BarcodeDecoded(BarcodeType.Code93, code93);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCodabar(modules, out var codabar)) {
            decoded = new BarcodeDecoded(BarcodeType.Codabar, codabar);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeMsi(modules, options, out var msi)) {
            decoded = new BarcodeDecoded(BarcodeType.MSI, msi);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode11(modules, options, out var code11)) {
            decoded = new BarcodeDecoded(BarcodeType.Code11, code11);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodePlessey(modules, options, out var plessey)) {
            decoded = new BarcodeDecoded(BarcodeType.Plessey, plessey);
            return true;
        }

        return false;
    }

    private static bool TryDecodeType(BarcodeType type, bool[] modules, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        decoded = null!;
        switch (type) {
            case BarcodeType.EAN:
                if (TryDecodeEan8(modules, out var ean8)) {
                    decoded = new BarcodeDecoded(BarcodeType.EAN, ean8);
                    return true;
                }
                if (TryDecodeEan13(modules, out var ean13)) {
                    decoded = new BarcodeDecoded(BarcodeType.EAN, ean13);
                    return true;
                }
                return false;
            case BarcodeType.UPCA:
                if (TryDecodeUpcA(modules, out var upca)) {
                    decoded = new BarcodeDecoded(BarcodeType.UPCA, upca);
                    return true;
                }
                return false;
            case BarcodeType.UPCE:
                if (TryDecodeUpcE(modules, out var upce)) {
                    decoded = new BarcodeDecoded(BarcodeType.UPCE, upce);
                    return true;
                }
                return false;
            case BarcodeType.ITF14:
                if (TryDecodeItf14(modules, out var itf14)) {
                    decoded = new BarcodeDecoded(BarcodeType.ITF14, itf14);
                    return true;
                }
                return false;
            case BarcodeType.Code128:
                if (TryDecodeCode128(modules, out var code128, out var isGs1)) {
                    decoded = new BarcodeDecoded(isGs1 ? BarcodeType.GS1_128 : BarcodeType.Code128, code128);
                    return true;
                }
                return false;
            case BarcodeType.GS1_128:
                if (TryDecodeCode128(modules, out var gs1, out var isGs1Only) && isGs1Only) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1_128, gs1);
                    return true;
                }
                return false;
            case BarcodeType.Code39:
                if (TryDecodeCode39(modules, options, out var code39)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code39, code39);
                    return true;
                }
                return false;
            case BarcodeType.Code93:
                if (TryDecodeCode93(modules, out var code93)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code93, code93);
                    return true;
                }
                return false;
            case BarcodeType.Codabar:
                if (TryDecodeCodabar(modules, out var codabar)) {
                    decoded = new BarcodeDecoded(BarcodeType.Codabar, codabar);
                    return true;
                }
                return false;
            case BarcodeType.MSI:
                if (TryDecodeMsi(modules, options, out var msi)) {
                    decoded = new BarcodeDecoded(BarcodeType.MSI, msi);
                    return true;
                }
                return false;
            case BarcodeType.Code11:
                if (TryDecodeCode11(modules, options, out var code11)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code11, code11);
                    return true;
                }
                return false;
            case BarcodeType.Plessey:
                if (TryDecodePlessey(modules, options, out var plessey)) {
                    decoded = new BarcodeDecoded(BarcodeType.Plessey, plessey);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static bool[] TrimModules(bool[] modules) {
        var start = 0;
        while (start < modules.Length && !modules[start]) start++;
        var end = modules.Length - 1;
        while (end >= start && !modules[end]) end--;
        if (start > end) return Array.Empty<bool>();
        var trimmed = new bool[end - start + 1];
        Array.Copy(modules, start, trimmed, 0, trimmed.Length);
        return trimmed;
    }

    private static bool[] InvertModules(bool[] modules) {
        var output = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) output[i] = !modules[i];
        return output;
    }

    private static bool[] ReverseModules(bool[] modules) {
        var output = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) output[i] = modules[modules.Length - 1 - i];
        return output;
    }

    private static int[] GetRuns(bool[] modules) {
        var runs = new List<int>(modules.Length / 2);
        var current = modules[0];
        var len = 1;
        for (var i = 1; i < modules.Length; i++) {
            if (modules[i] == current) {
                len++;
            } else {
                runs.Add(len);
                current = modules[i];
                len = 1;
            }
        }
        runs.Add(len);
        return runs.ToArray();
    }

    private static bool TryDecodeCode39(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        var patternToChar = Code39PatternMap.Value;
        var maxSymbols = (modules.Length + 1) / 13;
        if (maxSymbols <= 0) return false;

        var rented = ArrayPool<char>.Shared.Rent(maxSymbols);
        var count = 0;

        try {
            var index = 0;
            while (index + 12 <= modules.Length) {
                var key = PatternBits(modules, index, 12);
                if (!patternToChar.TryGetValue(key, out var ch)) return false;
                if (count >= rented.Length) return false;
                rented[count++] = ch;
                index += 12;
                if (index < modules.Length && !modules[index]) index++; // inter-character space
            }

            if (count < 2) return false;
            if (rented[0] != '*' || rented[count - 1] != '*') return false;

            var rawLen = count - 2;
            var raw = rawLen > 0 ? new string(rented, 1, rawLen) : string.Empty;
            var policy = options?.Code39Checksum ?? Code39ChecksumPolicy.None;
            if (policy != Code39ChecksumPolicy.None && raw.Length >= 2) {
                // Minimum length is one data symbol plus optional checksum.
                var expected = GetCode39ChecksumChar(raw.AsSpan(0, raw.Length - 1));
                if (expected != '#' && raw[raw.Length - 1] == expected) {
                    raw = raw.Substring(0, raw.Length - 1);
                } else if (policy == Code39ChecksumPolicy.RequireValid) {
                    return false;
                }
            }
            text = DecodeCode39Extended(raw);
            return true;
        } finally {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool TryDecodeCode93(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 10) return false;
        if (!modules[modules.Length - 1]) return false;

        var dataLen = modules.Length - 1;
        if (dataLen % 9 != 0) return false;
        var charCount = dataLen / 9;
        if (charCount < 2) return false;

        var chars = new char[charCount];
        for (var i = 0; i < charCount; i++) {
            var key = PatternBits(modules, i * 9, 9);
            if (!Code93PatternMap.Value.TryGetValue(key, out var ch)) return false;
            chars[i] = ch;
        }

        if (chars[0] != '*' || chars[chars.Length - 1] != '*') return false;
        var raw = new string(chars, 1, chars.Length - 2);
        if (raw.Length >= 2) {
            var c = GetCode93Checksum(raw.Substring(0, raw.Length - 2), 20);
            var k = GetCode93Checksum(raw.Substring(0, raw.Length - 1), 15);
            if (raw[raw.Length - 2] == c && raw[raw.Length - 1] == k) {
                raw = raw.Substring(0, raw.Length - 2);
            }
        }

        text = DecodeCode93Extended(raw);
        return true;
    }

    private static bool TryDecodeCodabar(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 7) return false;

        var maxSymbols = (runs.Length + 1) / 8;
        if (maxSymbols <= 0) return false;
        var rented = ArrayPool<char>.Shared.Rent(maxSymbols);
        var count = 0;
        var pos = 0;
        try {
            while (pos + 7 <= runs.Length) {
                var min = int.MaxValue;
                var max = 0;
                for (var i = 0; i < 7; i++) {
                    var len = runs[pos + i];
                    if (len < min) min = len;
                    if (len > max) max = len;
                }
                if (min <= 0) return false;
                var threshold = (min + max) / 2.0;

                var key = 0;
                for (var i = 0; i < 7; i++) {
                    key = (key << 1) | (runs[pos + i] > threshold ? 1 : 0);
                }
                if (!CodabarPatternMap.Value.TryGetValue(key, out var ch)) return false;
                if (count >= rented.Length) return false;
                rented[count++] = ch;
                pos += 7;

                if (pos < runs.Length) {
                    if ((pos & 1) == 0) return false;
                    pos++;
                }
            }

            if (pos != runs.Length) return false;
            if (count < 2) return false;
            if (!CodabarTables.StartStopChars.Contains(rented[0]) || !CodabarTables.StartStopChars.Contains(rented[count - 1])) return false;

            var rawLen = count - 2;
            text = rawLen > 0 ? new string(rented, 1, rawLen) : string.Empty;
            return true;
        } finally {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool TryDecodeMsi(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        if (modules.Length < MsiStartPattern.Length + MsiStopPattern.Length + 12) return false;
        if (!MatchPattern(modules, 0, MsiStartPattern)) return false;
        if (!MatchPattern(modules, modules.Length - MsiStopPattern.Length, MsiStopPattern)) return false;

        var dataLen = modules.Length - MsiStartPattern.Length - MsiStopPattern.Length;
        if (dataLen <= 0 || dataLen % 12 != 0) return false;
        var count = dataLen / 12;

        var digits = new char[count];
        var offset = MsiStartPattern.Length;
        for (var i = 0; i < count; i++) {
            var key = PatternBits(modules, offset, 12);
            if (!MsiPatternMap.Value.TryGetValue(key, out var digit)) return false;
            digits[i] = digit;
            offset += 12;
        }

        var raw = new string(digits);
        var policy = options?.MsiChecksum ?? MsiChecksumPolicy.None;
        if (policy != MsiChecksumPolicy.None) {
            if (TryStripMsiChecksum(raw, out var stripped)) {
                text = stripped;
                return true;
            }
            if (policy == MsiChecksumPolicy.RequireValid) return false;
        }

        text = raw;
        return true;
    }

    private static bool TryDecodeCode11(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        if (modules.Length < 7) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 5) return false;

        var maxSymbols = (runs.Length + 1) / 6;
        if (maxSymbols <= 0) return false;
        var rented = ArrayPool<char>.Shared.Rent(maxSymbols);
        var count = 0;
        var pos = 0;
        try {
            while (pos + 5 <= runs.Length) {
                var min = int.MaxValue;
                var max = 0;
                for (var i = 0; i < 5; i++) {
                    var len = runs[pos + i];
                    if (len < min) min = len;
                    if (len > max) max = len;
                }
                if (min <= 0) return false;
                var threshold = (min + max) / 2.0;

                var key = 0;
                for (var i = 0; i < 5; i++) {
                    key = (key << 1) | (runs[pos + i] > threshold ? 1 : 0);
                }
                if (!Code11PatternMap.Value.TryGetValue(key, out var ch)) return false;
                if (count >= rented.Length) return false;
                rented[count++] = ch;
                pos += 5;

                if (pos < runs.Length) {
                    if ((pos & 1) == 0) return false;
                    pos++;
                }
            }

            if (pos != runs.Length) return false;
            if (count < 2) return false;
            if (rented[0] != '*' || rented[count - 1] != '*') return false;

            var rawLen = count - 2;
            var raw = rawLen > 0 ? new string(rented, 1, rawLen) : string.Empty;
            var policy = options?.Code11Checksum ?? Code11ChecksumPolicy.None;
            if (policy != Code11ChecksumPolicy.None) {
                if (TryStripCode11Checksum(raw, out var stripped)) {
                    text = stripped;
                    return true;
                }
                if (policy == Code11ChecksumPolicy.RequireValid) return false;
            }

            text = raw;
            return true;
        } finally {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static bool TryDecodePlessey(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        if (TryDecodePlesseyInternal(modules, options, out text)) return true;
        var reversed = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) reversed[i] = modules[modules.Length - 1 - i];
        return TryDecodePlesseyInternal(reversed, options, out text);
    }

    private static bool TryDecodePlesseyInternal(bool[] modules, BarcodeDecodeOptions? options, out string text) {
        text = string.Empty;
        if (modules.Length < 24) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 13) return false;
        if ((runs.Length & 1) == 0) return false;

        var startRuns = PlesseyTables.StartBits.Length * 2;
        if (runs.Length < startRuns + 9) return false;

        var pos = 0;
        for (var i = 0; i < PlesseyTables.StartBits.Length; i++) {
            if (pos + 1 >= runs.Length) return false;
            if (!TryDecodePlesseyPair(runs[pos], runs[pos + 1], out var bit)) return false;
            if (bit != (PlesseyTables.StartBits[i] == '1')) return false;
            pos += 2;
        }

        var stopStart = runs.Length - PlesseyTables.StopBits.Length * 2;
        if ((stopStart & 1) == 0) return false;
        var terminationIndex = stopStart - 1;
        if (terminationIndex < pos) return false;

        for (var i = 0; i < PlesseyTables.StopBits.Length; i++) {
            var spaceIndex = stopStart + i * 2;
            if (spaceIndex + 1 >= runs.Length) return false;
            if (!TryDecodePlesseyPair(runs[spaceIndex + 1], runs[spaceIndex], out var bit)) return false;
            if (bit != (PlesseyTables.StopBits[i] == '1')) return false;
        }

        var bitCount = terminationIndex - pos;
        if ((bitCount & 1) != 0) return false;
        var dataBits = bitCount / 2;
        if (dataBits <= 8) return false;
        if ((dataBits - 8) % 4 != 0) return false;

        var bits = new bool[dataBits];
        var bitPos = 0;
        for (var i = pos; i < terminationIndex; i += 2) {
            if (!TryDecodePlesseyPair(runs[i], runs[i + 1], out var bit)) return false;
            bits[bitPos++] = bit;
        }

        var payloadBits = dataBits - 8;
        var crcBits = new bool[8];
        Array.Copy(bits, payloadBits, crcBits, 0, 8);

        var payload = new bool[payloadBits];
        Array.Copy(bits, 0, payload, 0, payloadBits);

        var policy = options?.PlesseyChecksum ?? PlesseyChecksumPolicy.RequireValid;
        if (policy != PlesseyChecksumPolicy.None) {
            var expected = CalcPlesseyCrc(payload);
            var actual = BitsToByte(crcBits);
            if (expected != actual && policy == PlesseyChecksumPolicy.RequireValid) return false;
        }

        var chars = new char[payloadBits / 4];
        for (var i = 0; i < chars.Length; i++) {
            var value = 0;
            for (var b = 0; b < 4; b++) {
                if (payload[i * 4 + b]) value |= 1 << b;
            }
            chars[i] = value < 10 ? (char)('0' + value) : (char)('A' + (value - 10));
        }

        text = new string(chars);
        return true;
    }

    private static bool TryDecodeCode128(bool[] modules, out string text, out bool isGs1) {
        text = string.Empty;
        isGs1 = false;
        if (modules.Length < 24) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 6) return false;
        if (runs[0] == 0) return false;

        var codes = new List<int>();
        var pos = 0;
        while (pos < runs.Length) {
            if (runs.Length - pos >= 7) {
                var stopKey = PatternKey(runs, pos, 7);
                if (stopKey == Code128StopKey.Value) {
                    pos += 7;
                    break;
                }
            }
            if (runs.Length - pos < 6) return false;
            var key = PatternKey(runs, pos, 6);
            if (!Code128PatternMap.Value.TryGetValue(key, out var code)) return false;
            codes.Add(code);
            pos += 6;
        }

        if (codes.Count < 3) return false;

        var checksum = codes[codes.Count - 1];
        var sum = codes[0];
        for (var i = 1; i < codes.Count - 1; i++) sum = (sum + codes[i] * i) % 103;
        if (sum != checksum) return false;

        var start = codes[0];
        var set = start == Code128Tables.StartC ? 'C' : start == Code128Tables.StartB ? 'B' : start == Code128Tables.StartA ? 'A' : '?';
        if (set == '?') return false;

        var sb = new System.Text.StringBuilder();
        var gs1StartConsumed = false;
        for (var i = 1; i < codes.Count - 1; i++) {
            var code = codes[i];
            if (code == Code128Tables.Fnc1) {
                isGs1 = true;
                if (!gs1StartConsumed) {
                    gs1StartConsumed = true;
                    continue;
                }
                sb.Append(Gs1.GroupSeparator);
                continue;
            }
            if (set == 'A') {
                if (code == Code128Tables.CodeC) {
                    set = 'C';
                    continue;
                }
                if (code == Code128Tables.CodeB) {
                    set = 'B';
                    continue;
                }
                if (code == Code128Tables.CodeA) continue;
                if (code >= 0 && code <= 95) {
                    sb.Append((char)code);
                    continue;
                }
                return false;
            }

            if (set == 'B') {
                if (code == Code128Tables.CodeC) {
                    set = 'C';
                    continue;
                }
                if (code == Code128Tables.CodeA) {
                    set = 'A';
                    continue;
                }
                if (code == Code128Tables.CodeB) continue;
                if (code >= 0 && code <= 95) {
                    sb.Append((char)(code + 32));
                    continue;
                }
                return false;
            }

            if (set == 'C') {
                if (code == Code128Tables.CodeB) {
                    set = 'B';
                    continue;
                }
                if (code == Code128Tables.CodeA) {
                    set = 'A';
                    continue;
                }
                if (code == Code128Tables.CodeC) continue;
                if (code >= 0 && code <= 99) {
                    sb.Append(code.ToString("00"));
                    continue;
                }
                return false;
            }
        }

        text = sb.ToString();
        return true;
    }

    private static bool TryDecodeItf14(bool[] modules, out string text) {
        text = string.Empty;
        if (modules.Length < 15) return false;
        if (!modules[0]) return false;

        var runs = GetRuns(modules);
        if (runs.Length < 7) return false;

        var minRun = int.MaxValue;
        var maxRun = 0;
        for (var i = 0; i < runs.Length; i++) {
            if (runs[i] < minRun) minRun = runs[i];
            if (runs[i] > maxRun) maxRun = runs[i];
        }

        if (minRun <= 0 || maxRun < minRun * 2) return false;
        var threshold = (minRun + maxRun) / 2.0;

        // Start pattern: narrow bar/space/bar/space.
        if (runs.Length < 4) return false;
        for (var i = 0; i < 4; i++) {
            if (runs[i] > threshold) return false;
        }

        var pos = 4;
        var remaining = runs.Length - pos;
        if (remaining < 3) return false;
        if ((remaining - 3) % 10 != 0) return false;

        var pairs = (remaining - 3) / 10;
        if (pairs * 2 != 14) return false;

        var digits = new char[pairs * 2];
        for (var pair = 0; pair < pairs; pair++) {
            var barKey = PatternKey(runs, pos, threshold);
            var spaceKey = PatternKey(runs, pos + 1, threshold);
            if (!Itf14PatternMap.Value.TryGetValue(barKey, out var leftDigit)) return false;
            if (!Itf14PatternMap.Value.TryGetValue(spaceKey, out var rightDigit)) return false;
            digits[pair * 2] = (char)('0' + leftDigit);
            digits[pair * 2 + 1] = (char)('0' + rightDigit);
            pos += 10;
        }

        // Stop pattern: wide bar, narrow space, narrow bar.
        if (pos + 2 >= runs.Length) return false;
        if (runs[pos] <= threshold) return false;
        if (runs[pos + 1] > threshold) return false;
        if (runs[pos + 2] > threshold) return false;

        var raw = new string(digits);
        var expected = CalcItf14Checksum(raw.AsSpan(0, 13));
        if (raw[13] != expected) return false;

        text = raw;
        return true;
    }

    private static int PatternKey(int[] runs, int start, double threshold) {
        var key = 0;
        var idx = start;
        for (var i = 0; i < 5; i++) {
            if (idx >= runs.Length) return -1;
            if (runs[idx] > threshold) key |= 1 << (4 - i);
            idx += 2;
        }
        return key;
    }

    private static char CalcItf14Checksum(ReadOnlySpan<char> content) {
        var sum = 0;
        for (var i = content.Length - 1; i >= 0; i--) {
            var digit = content[i] - '0';
            var weight = ((content.Length - 1 - i) & 1) == 0 ? 3 : 1;
            sum += digit * weight;
        }
        var check = (10 - (sum % 10)) % 10;
        return (char)('0' + check);
    }

    private static bool TryDecodeEan8(bool[] modules, out string text) {
        text = string.Empty;
        if (!TryNormalizeModules(modules, 67, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart) || !MatchPattern(modules, 64, GuardStart)) return false;
        if (!MatchPattern(modules, 31, GuardCenter)) return false;

        var digits = new char[8];
        var offset = 3;
        for (var i = 0; i < 4; i++) {
            if (!TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.LeftOdd, out var digit)) return false;
            digits[i] = digit;
        }
        offset = 3 + 4 * 7 + 5;
        for (var i = 0; i < 4; i++) {
            if (!TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.Right, out var digit)) return false;
            digits[i + 4] = digit;
        }

        var raw = new string(digits);
        if (!IsValidEanChecksum(raw)) return false;
        text = raw;
        return true;
    }

    private static bool TryDecodeEan13(bool[] modules, out string text) {
        text = string.Empty;
        if (!TryNormalizeModules(modules, 95, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart) || !MatchPattern(modules, 92, GuardStart)) return false;
        if (!MatchPattern(modules, 45, GuardCenter)) return false;

        var leftDigits = new char[6];
        var parity = new bool[6];
        var offset = 3;
        for (var i = 0; i < 6; i++) {
            if (TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.LeftOdd, out var digit)) {
                leftDigits[i] = digit;
                parity[i] = false;
            } else if (TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.LeftEven, out digit)) {
                leftDigits[i] = digit;
                parity[i] = true;
            } else {
                return false;
            }
        }

        var rightDigits = new char[6];
        offset = 3 + 6 * 7 + 5;
        for (var i = 0; i < 6; i++) {
            if (!TryMatchEanDigit(modules, offset + i * 7, EanDigitKind.Right, out var digit)) return false;
            rightDigits[i] = digit;
        }

        var firstDigit = EanParityMap.Value[ParityKey(parity)];
        var raw = firstDigit + new string(leftDigits) + new string(rightDigits);
        if (!IsValidEanChecksum(raw)) return false;
        text = raw;
        return true;
    }

    private static bool TryDecodeUpcA(bool[] modules, out string text) {
        text = string.Empty;
        if (!TryNormalizeModules(modules, 95, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart) || !MatchPattern(modules, 92, GuardStart)) return false;
        if (!MatchPattern(modules, 45, GuardCenter)) return false;

        var digits = new char[12];
        var offset = 3;
        for (var i = 0; i < 6; i++) {
            if (!TryMatchUpcDigit(modules, offset + i * 7, true, out var digit)) return false;
            digits[i] = digit;
        }
        offset = 3 + 6 * 7 + 5;
        for (var i = 0; i < 6; i++) {
            if (!TryMatchUpcDigit(modules, offset + i * 7, false, out var digit)) return false;
            digits[i + 6] = digit;
        }
        var raw = new string(digits);
        if (!IsValidUpcAChecksum(raw)) return false;
        text = raw;
        return true;
    }

    private static bool TryDecodeUpcE(bool[] modules, out string text) {
        text = string.Empty;
        if (!TryNormalizeModules(modules, 51, out var normalized)) return false;
        modules = normalized;
        if (!MatchPattern(modules, 0, GuardStart)) return false;
        if (!MatchPattern(modules, 45, GuardUpcEEnd)) return false;

        var digits = new char[6];
        var parity = new UpcETables.Parity[6];
        var offset = 3;
        for (var i = 0; i < 6; i++) {
            if (TryMatchUpcEDigit(modules, offset + i * 7, UpcETables.Parity.Odd, out var digit)) {
                digits[i] = digit;
                parity[i] = UpcETables.Parity.Odd;
            } else if (TryMatchUpcEDigit(modules, offset + i * 7, UpcETables.Parity.Even, out digit)) {
                digits[i] = digit;
                parity[i] = UpcETables.Parity.Even;
            } else {
                return false;
            }
        }

        var parityKey = ParityKey(parity);
        foreach (var kvp in UpcETables.ParityPatternTable) {
            var pattern = kvp.Value;
            if (ParityKey(pattern.NumberSystemZero) == parityKey) {
                var candidate = "0" + new string(digits) + kvp.Key;
                if (IsValidUpcE(candidate)) {
                    text = candidate;
                    return true;
                }
            }
            if (ParityKey(pattern.NumberSystemOne) == parityKey) {
                var candidate = "1" + new string(digits) + kvp.Key;
                if (IsValidUpcE(candidate)) {
                    text = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchPattern(bool[] modules, int offset, bool[] pattern) {
        if (offset < 0 || offset + pattern.Length > modules.Length) return false;
        for (var i = 0; i < pattern.Length; i++) {
            if (modules[offset + i] != pattern[i]) return false;
        }
        return true;
    }

    private enum EanDigitKind {
        LeftOdd,
        LeftEven,
        Right
    }

    private static bool TryMatchEanDigit(bool[] modules, int offset, EanDigitKind kind, out char digit) {
        digit = '\0';
        for (var d = 0; d <= 9; d++) {
            var enc = EanTables.EncodingTable[(char)('0' + d)];
            var pattern = kind switch {
                EanDigitKind.LeftOdd => enc.LeftOdd,
                EanDigitKind.LeftEven => enc.LeftEven,
                _ => enc.Right
            };
            if (MatchPattern(modules, offset, pattern)) {
                digit = (char)('0' + d);
                return true;
            }
        }
        return false;
    }

    private static bool TryMatchUpcDigit(bool[] modules, int offset, bool left, out char digit) {
        digit = '\0';
        for (var d = 0; d <= 9; d++) {
            var enc = UpcATables.EncodingTable[(char)('0' + d)];
            var pattern = left ? enc.Left : enc.Right;
            if (MatchPattern(modules, offset, pattern)) {
                digit = (char)('0' + d);
                return true;
            }
        }
        return false;
    }

    private static bool TryMatchUpcEDigit(bool[] modules, int offset, UpcETables.Parity parity, out char digit) {
        digit = '\0';
        for (var d = 0; d <= 9; d++) {
            var enc = UpcETables.EncodingTable[(char)('0' + d)];
            var pattern = parity == UpcETables.Parity.Odd ? enc.Odd : enc.Even;
            if (MatchPattern(modules, offset, pattern)) {
                digit = (char)('0' + d);
                return true;
            }
        }
        return false;
    }

    private static bool IsValidEanChecksum(string value) {
        if (value.Length != 8 && value.Length != 13) return false;
        var expected = value[value.Length - 1];
        var actual = CalcEanChecksum(value.Substring(0, value.Length - 1));
        return expected == actual;
    }

    private static char CalcEanChecksum(string content) {
        var triple = content.Length == 7;
        var sum = 0;
        for (var i = 0; i < content.Length; i++) {
            var val = content[i] - '0';
            if (triple) val *= 3;
            triple = !triple;
            sum += val;
        }
        return (char)((10 - sum % 10) % 10 + '0');
    }

    private static bool IsValidUpcAChecksum(string value) {
        if (value.Length != 12) return false;
        var expected = value[value.Length - 1];
        var actual = CalcUpcAChecksum(value.Substring(0, 11));
        return expected == actual;
    }

    private static bool IsValidUpcE(string value) {
        if (value.Length != 8) return false;
        var upcA = ExpandUpcEToUpcA(value);
        return IsValidUpcAChecksum(upcA);
    }

    private static string ExpandUpcEToUpcA(string value) {
        var numberSystem = value[0];
        var digits = value.Substring(1, 6);
        var check = value[7];

        string upcA;
        switch (digits[5]) {
            case '0':
            case '1':
            case '2':
                upcA = $"{numberSystem}{digits.Substring(0, 2)}{digits[5]}0000{digits.Substring(2, 3)}";
                break;
            case '3':
                upcA = $"{numberSystem}{digits.Substring(0, 3)}00000{digits.Substring(3, 2)}";
                break;
            case '4':
                upcA = $"{numberSystem}{digits.Substring(0, 4)}00000{digits[4]}";
                break;
            default:
                upcA = $"{numberSystem}{digits.Substring(0, 5)}0000{digits[5]}";
                break;
        }

        return upcA + check;
    }

    private static char CalcUpcAChecksum(string content) {
        var digits = content.Select(c => c - '0').ToArray();
        var sum = 3 * (digits[0] + digits[2] + digits[4] + digits[6] + digits[8] + digits[10]);
        sum += digits[1] + digits[3] + digits[5] + digits[7] + digits[9];
        sum %= 10;
        sum = sum != 0 ? 10 - sum : 0;
        return (char)(sum + '0');
    }

    private static bool TryNormalizeModules(bool[] modules, int expectedLength, out bool[] normalized) {
        normalized = modules;
        if (modules.Length == expectedLength) return true;
        if (modules.Length == 0) return false;

        var len = modules.Length;
        var ratio = len / (double)expectedLength;
        if (ratio < 0.5 || ratio > 3.0) return false;

        var output = new bool[expectedLength];
        for (var i = 0; i < expectedLength; i++) {
            var start = (int)Math.Floor(i * len / (double)expectedLength);
            var end = (int)Math.Floor((i + 1) * len / (double)expectedLength);
            if (end <= start) {
                output[i] = modules[Math.Min(start, len - 1)];
                continue;
            }
            var count = 0;
            for (var j = start; j < end && j < len; j++) {
                if (modules[j]) count++;
            }
            var total = end - start;
            output[i] = count * 2 >= total;
        }
        normalized = output;
        return true;
    }

    private static string DecodeCode39Extended(string raw) {
        if (raw.IndexOfAny(new[] { '$', '%', '/', '+' }) < 0) return raw;
        var reverse = Code39ExtendedMap.Value;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < raw.Length; i++) {
            if (i + 1 < raw.Length) {
                var key = raw.Substring(i, 2);
                if (reverse.TryGetValue(key, out var mapped)) {
                    sb.Append(mapped);
                    i++;
                    continue;
                }
            }
            sb.Append(raw[i]);
        }
        return sb.ToString();
    }

    private static string DecodeCode93Extended(string raw) {
        if (!raw.Any(c => c == Code93Tables.Fnc1 || c == Code93Tables.Fnc2 || c == Code93Tables.Fnc3 || c == Code93Tables.Fnc4)) {
            return raw;
        }
        var reverse = Code93ExtendedMap.Value;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < raw.Length; i++) {
            var ch = raw[i];
            if ((ch == Code93Tables.Fnc1 || ch == Code93Tables.Fnc2 || ch == Code93Tables.Fnc3 || ch == Code93Tables.Fnc4) && i + 1 < raw.Length) {
                var key = raw.Substring(i, 2);
                if (reverse.TryGetValue(key, out var mapped)) {
                    sb.Append(mapped);
                    i++;
                    continue;
                }
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static uint PatternBits(bool[] modules, int offset, int length) {
        uint key = 0;
        for (var i = 0; i < length; i++) {
            key = (key << 1) | (modules[offset + i] ? 1u : 0u);
        }
        return key;
    }

    private static int PatternBitsFromString(string pattern) {
        var key = 0;
        for (var i = 0; i < pattern.Length; i++) {
            key = (key << 1) | (pattern[i] == '1' ? 1 : 0);
        }
        return key;
    }

    private static int PatternKey(int[] runs, int offset, int count) {
        var key = 0;
        for (var i = 0; i < count; i++) {
            key = key * 10 + runs[offset + i];
        }
        return key;
    }

    private static string ParityKey(bool[] parity) {
        var key = 0;
        for (var i = 0; i < parity.Length; i++) {
            key = (key << 1) | (parity[i] ? 1 : 0);
        }
        return Convert.ToString(key, 2).PadLeft(parity.Length, '0');
    }

    private static string ParityKey(UpcETables.Parity[] parity) {
        var key = 0;
        for (var i = 0; i < parity.Length; i++) {
            key = (key << 1) | (parity[i] == UpcETables.Parity.Even ? 1 : 0);
        }
        return Convert.ToString(key, 2).PadLeft(parity.Length, '0');
    }

    private static char GetCode39ChecksumChar(ReadOnlySpan<char> content) {
        var sum = 0;
        foreach (var ch in content) {
            if (!Code39Tables.EncodingTable.TryGetValue(ch, out var entry) || entry.value < 0) return '#';
            sum += entry.value;
        }
        sum %= 43;
        foreach (var kvp in Code39Tables.EncodingTable) {
            if (kvp.Value.value == sum) return kvp.Key;
        }
        return '#';
    }

    private static char GetCode93Checksum(string content, int maxWeight) {
        var weight = 1;
        var sum = 0;
        for (var i = content.Length - 1; i >= 0; i--) {
            var ch = content[i];
            if (!Code93Tables.EncodingTable.TryGetValue(ch, out var entry)) return ' ';
            sum += entry.value * weight;
            if (++weight > maxWeight) weight = 1;
        }
        sum %= 47;
        foreach (var kvp in Code93Tables.EncodingTable) {
            if (kvp.Value.value == sum) return kvp.Key;
        }
        return ' ';
    }

    private static bool TryStripMsiChecksum(string raw, out string stripped) {
        stripped = raw;
        if (raw.Length >= 2) {
            var data = raw.Substring(0, raw.Length - 2);
            var check1 = raw[raw.Length - 2];
            var check2 = raw[raw.Length - 1];
            if (CalcMsiMod10(data) == check1 && CalcMsiMod10(data + check1) == check2) {
                stripped = data;
                return true;
            }
        }
        if (raw.Length >= 1) {
            var data = raw.Substring(0, raw.Length - 1);
            var check = raw[raw.Length - 1];
            if (CalcMsiMod10(data) == check) {
                stripped = data;
                return true;
            }
        }
        return false;
    }

    private static char CalcMsiMod10(string content) {
        var sum = 0;
        var doubleIt = true;
        for (var i = content.Length - 1; i >= 0; i--) {
            var digit = content[i] - '0';
            if (doubleIt) {
                digit *= 2;
                if (digit > 9) digit = (digit / 10) + (digit % 10);
            }
            sum += digit;
            doubleIt = !doubleIt;
        }
        var mod = sum % 10;
        var check = mod == 0 ? 0 : 10 - mod;
        return (char)('0' + check);
    }

    private static bool TryStripCode11Checksum(string raw, out string stripped) {
        stripped = raw;
        if (raw.Length >= 2) {
            var data = raw.Substring(0, raw.Length - 2);
            var c = raw[raw.Length - 2];
            var k = raw[raw.Length - 1];
            if (CalcCode11Checksum(data, 10) == c && CalcCode11Checksum(data + c, 9) == k) {
                stripped = data;
                return true;
            }
        }
        if (raw.Length >= 1) {
            var data = raw.Substring(0, raw.Length - 1);
            var c = raw[raw.Length - 1];
            if (CalcCode11Checksum(data, 10) == c) {
                stripped = data;
                return true;
            }
        }
        return false;
    }

    private static char CalcCode11Checksum(string content, int maxWeight) {
        var sum = 0;
        var weight = 1;
        for (var i = content.Length - 1; i >= 0; i--) {
            var ch = content[i];
            if (!Code11Tables.ValueTable.TryGetValue(ch, out var value)) return '-';
            sum += value * weight;
            weight++;
            if (weight > maxWeight) weight = 1;
        }
        var check = sum % 11;
        return check == 10 ? '-' : (char)('0' + check);
    }

    private static bool TryDecodePlesseyPair(int barRun, int spaceRun, out bool bit) {
        bit = false;
        if (barRun == spaceRun) return false;
        bit = barRun > spaceRun;
        return true;
    }

    private static byte CalcPlesseyCrc(bool[] bits) {
        const int poly = 0x1E9;
        var crc = 0;
        for (var i = 0; i < bits.Length; i++) {
            crc = (crc << 1) | (bits[i] ? 1 : 0);
            if ((crc & 0x100) != 0) crc ^= poly;
        }
        for (var i = 0; i < 8; i++) {
            crc <<= 1;
            if ((crc & 0x100) != 0) crc ^= poly;
        }
        return (byte)(crc & 0xFF);
    }

    private static byte BitsToByte(bool[] bits) {
        var value = 0;
        for (var i = 0; i < bits.Length; i++) {
            if (bits[i]) value |= 1 << i;
        }
        return (byte)value;
    }

    private static readonly bool[] GuardStart = { true, false, true };
    private static readonly bool[] GuardCenter = { false, true, false, true, false };
    private static readonly bool[] GuardUpcEEnd = { false, true, false, true, false, true };
    private static readonly bool[] MsiStartPattern = { true, true, false };
    private static readonly bool[] MsiStopPattern = { true, false, false, true };

    private static readonly Lazy<Dictionary<uint, char>> Code39PatternMap = new(() => {
        var dict = new Dictionary<uint, char>();
        foreach (var kvp in Code39Tables.EncodingTable) {
            uint key = 0;
            var data = kvp.Value.data;
            for (var i = 0; i < data.Length; i++) {
                key = (key << 1) | (data[i] ? 1u : 0u);
            }
            dict[key] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<int, char>> CodabarPatternMap = new(() => {
        var dict = new Dictionary<int, char>();
        foreach (var kvp in CodabarTables.EncodingTable) {
            dict[PatternBitsFromString(kvp.Value)] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<int, char>> Code11PatternMap = new(() => {
        var dict = new Dictionary<int, char> {
            { PatternBitsFromString(Code11Tables.StartStopPattern), '*' }
        };
        foreach (var kvp in Code11Tables.EncodingTable) {
            dict[PatternBitsFromString(kvp.Value)] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<uint, char>> MsiPatternMap = new(() => {
        var dict = new Dictionary<uint, char>();
        foreach (var kvp in MsiTables.DigitPatterns) {
            dict[(uint)PatternBitsFromString(kvp.Value)] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<string, char>> Code39ExtendedMap = new(() => {
        var dict = new Dictionary<string, char>();
        foreach (var kvp in Code39Tables.ExtendedTable) {
            dict[kvp.Value] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<uint, char>> Code93PatternMap = new(() => {
        var dict = new Dictionary<uint, char>();
        foreach (var kvp in Code93Tables.EncodingTable) {
            dict[kvp.Value.data] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<string, char>> Code93ExtendedMap = new(() => {
        var dict = new Dictionary<string, char>();
        for (var i = 0; i < Code93Tables.ExtendedTable.Length; i++) {
            var key = Code93Tables.ExtendedTable[i];
            if (!dict.ContainsKey(key)) dict[key] = (char)i;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<string, char>> EanParityMap = new(() => {
        var dict = new Dictionary<string, char>();
        foreach (var kvp in EanTables.EncodingTable) {
            var key = ParityKey(kvp.Value.Checksum);
            dict[key] = kvp.Key;
        }
        return dict;
    });

    private static readonly Lazy<Dictionary<int, int>> Code128PatternMap = new(() => {
        var dict = new Dictionary<int, int>();
        for (var code = 0; code <= 105; code++) {
            var pattern = Code128Tables.GetPattern(code);
            var key = PatternKeyFromCode(pattern, 6);
            dict[key] = code;
        }
        return dict;
    });

    private static readonly Lazy<int> Code128StopKey = new(() => {
        var pattern = Code128Tables.GetPattern(Code128Tables.Stop);
        return PatternKeyFromCode(pattern, 7);
    });

    private static readonly Lazy<Dictionary<int, int>> Itf14PatternMap = new(() => {
        var dict = new Dictionary<int, int>();
        for (var digit = 0; digit <= 9; digit++) {
            var pattern = Itf14Tables.DigitPatterns[digit];
            var key = 0;
            for (var i = 0; i < 5; i++) {
                if (pattern[i] == 3) key |= 1 << (4 - i);
            }
            dict[key] = digit;
        }
        return dict;
    });

    private static int PatternKeyFromCode(uint pattern, int nibbles) {
        var key = 0;
        for (var i = nibbles - 1; i >= 0; i--) {
            var width = (int)((pattern >> (i * 4)) & 0xFu);
            key = key * 10 + width;
        }
        return key;
    }
}

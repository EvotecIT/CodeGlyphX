using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Pdf417.Ec;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

/// <summary>
/// Decodes PDF417 barcodes.
/// </summary>
public static partial class Pdf417Decoder {
    private const int StartPatternWidth = 17;
    private const int StopPatternWidth = 18;
    private const int StartPattern = 0x1fea8; // 17 bits
    private const int StopPattern = 0x3fa29;  // 18 bits
    private const int DefaultThreshold = 128;

    private static readonly Dictionary<int, int>[] PatternToCodeword = BuildPatternMaps();

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        return TryDecode(modules, CancellationToken.None, out value);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, including Macro metadata when present.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out Pdf417Decoded decoded) {
        return TryDecode(modules, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, with cancellation.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }

        if (TryDecodeCore(modules, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeWithStartPattern(modules, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        var mirror = MirrorX(modules);
        if (TryDecodeCore(mirror, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeWithStartPattern(mirror, cancellationToken, out value)) return true;

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, with cancellation and Macro metadata when present.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }

        if (TryDecodeCore(modules, cancellationToken, out var value, out var macro)) {
            decoded = new Pdf417Decoded(value, macro);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (TryDecodeWithStartPattern(modules, cancellationToken, out value, out macro)) {
            decoded = new Pdf417Decoded(value, macro);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        var mirror = MirrorX(modules);
        if (TryDecodeCore(mirror, cancellationToken, out value, out macro)) {
            decoded = new Pdf417Decoded(value, macro);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (TryDecodeWithStartPattern(mirror, cancellationToken, out value, out macro)) {
            decoded = new Pdf417Decoded(value, macro);
            return true;
        }

        decoded = null!;
        return false;
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, with diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        return TryDecode(modules, CancellationToken.None, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        diagnostics = new Pdf417DecodeDiagnostics();
        return TryDecodeInternal(modules, cancellationToken, diagnostics, out value);
    }
}

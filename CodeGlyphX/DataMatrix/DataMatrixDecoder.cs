using System;
using System.Buffers;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Decodes Data Matrix (ECC200) symbols.
/// </summary>
public static partial class DataMatrixDecoder {
    private enum DataMatrixEncodation {
        Ascii,
        C40,
        Text,
        X12,
        Edifact,
        Base256
    }

    private static readonly char[] C40_SHIFT2_SET_CHARS = {
        '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', ':',
        ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_'
    };

    private static readonly char[] TEXT_SHIFT2_SET_CHARS = C40_SHIFT2_SET_CHARS;

    private static readonly char[] C40_SHIFT3_SET_CHARS = "`abcdefghijklmnopqrstuvwxyz{|}~\u007f".ToCharArray();

    private static readonly char[] TEXT_SHIFT3_SET_CHARS = "`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~\u007f".ToCharArray();
    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        return TryDecode(modules, CancellationToken.None, out value);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a module matrix, with cancellation.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }

        if (TryDecodeCore(modules, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        var mirror = MirrorX(modules);
        return TryDecodeCore(mirror, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a module matrix, with diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecode(modules, CancellationToken.None, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a module matrix, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value, out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }

        if (TryDecodeCore(modules, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        diagnostics.MirroredTried = true;
        var mirror = MirrorX(modules);
        if (TryDecodeCore(mirror, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }

        value = string.Empty;
        diagnostics.Failure ??= "No Data Matrix decoded.";
        return false;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return TryDecodePixels(pixels, width, height, stride, format, CancellationToken.None, out value);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value, diagnostics);
    }
#endif

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value, out DataMatrixDecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value, diagnostics);
    }
}

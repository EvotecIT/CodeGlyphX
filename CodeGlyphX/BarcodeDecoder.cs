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
public static partial class BarcodeDecoder {
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
}

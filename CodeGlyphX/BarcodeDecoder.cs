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

    /// <summary>
    /// Attempts to decode all 1D barcodes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out BarcodeDecoded[] decoded, BarcodeType? expectedType = null, BarcodeDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        decoded = Array.Empty<BarcodeDecoded>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates)) {
            candidates = Array.Empty<bool[]>();
        }

        var list = new List<BarcodeDecoded>(4);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < candidates.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (TryDecodeWithTransforms(candidates[i], expectedType, options, cancellationToken, out var hit)) {
                AddUnique(list, seen, hit);
            }
        }

        if (options?.EnableTileScan == true && !DecodeBudget.ShouldAbort(cancellationToken)) {
            ScanTiles(pixels, width, height, stride, cancellationToken, options, (tile, tw, th, tstride) => {
                if (TryDecode(tile, tw, th, tstride, format, expectedType, options, cancellationToken, out var hit)) {
                    AddUnique(list, seen, hit);
                }
                return false;
            });
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
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

    /// <summary>
    /// Attempts to decode all 1D barcodes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out BarcodeDecoded[] decoded, BarcodeType? expectedType = null, BarcodeDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        decoded = Array.Empty<BarcodeDecoded>();
        if (pixels.IsEmpty) return false;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;

        if (options?.EnableTileScan == true) {
            var buffer = pixels.ToArray();
            return TryDecodeAll(buffer, width, height, stride, format, out decoded, expectedType, options, cancellationToken);
        }

        if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates)) return false;
        var list = new List<BarcodeDecoded>(4);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < candidates.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (TryDecodeWithTransforms(candidates[i], expectedType, options, cancellationToken, out var hit)) {
                AddUnique(list, seen, hit);
            }
        }
        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
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

    private static void AddUnique(List<BarcodeDecoded> list, HashSet<string> seen, BarcodeDecoded decoded) {
        if (decoded is null) return;
        var key = decoded.Type.ToString() + ":" + decoded.Text;
        if (seen.Add(key)) list.Add(decoded);
    }

    private static void ScanTiles(byte[] rgba, int width, int height, int stride, CancellationToken token, BarcodeDecodeOptions? options, Func<byte[], int, int, int, bool> onTile) {
        if (width <= 0 || height <= 0 || stride < width * 4) return;
        var grid = options?.TileGrid > 1 ? options.TileGrid : (Math.Max(width, height) >= 720 ? 3 : 2);
        var pad = Math.Max(8, Math.Min(width, height) / 40);
        var tileW = width / grid;
        var tileH = height / grid;

        for (var ty = 0; ty < grid; ty++) {
            for (var tx = 0; tx < grid; tx++) {
                if (DecodeBudget.ShouldAbort(token)) return;
                var x0 = tx * tileW;
                var y0 = ty * tileH;
                var x1 = (tx == grid - 1) ? width : (tx + 1) * tileW;
                var y1 = (ty == grid - 1) ? height : (ty + 1) * tileH;

                x0 = Math.Max(0, x0 - pad);
                y0 = Math.Max(0, y0 - pad);
                x1 = Math.Min(width, x1 + pad);
                y1 = Math.Min(height, y1 + pad);

                var tw = x1 - x0;
                var th = y1 - y0;
                if (tw < 48 || th < 48) continue;

                var tileStride = tw * 4;
                var tile = new byte[tileStride * th];
                for (var y = 0; y < th; y++) {
                    if (DecodeBudget.ShouldAbort(token)) return;
                    Buffer.BlockCopy(rgba, (y0 + y) * stride + x0 * 4, tile, y * tileStride, tileStride);
                }

                if (onTile(tile, tw, th, tileStride)) return;
            }
        }
    }
}

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
        for (var pass = 0; pass < PixelPassCount(expectedType); pass++) {
            if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates, retailRecovery: IsRetailRecoveryPass(expectedType, pass), retailHint: IsRetailBarcodeType(expectedType), dataBarRecovery: IsDataBarRecoveryPass(expectedType, pass))) continue;
            for (var i = 0; i < candidates.Length;) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, null, out decoded, out var selected, retailOnly: IsRetailRecoveryPass(expectedType, pass))) {
                    if (IsCorroboratedRetailRecovery(pixels, width, height, stride, format, expectedType, options, cancellationToken, decoded, selected)) return true;
                    decoded = null!;
                }
            }
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
        for (var pass = 0; pass < PixelPassCount(expectedType); pass++) {
            if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates, retailRecovery: IsRetailRecoveryPass(expectedType, pass), retailHint: IsRetailBarcodeType(expectedType), dataBarRecovery: IsDataBarRecoveryPass(expectedType, pass))) continue;
            diagnostics.CandidateCount += candidates.Length;
            for (var i = 0; i < candidates.Length;) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { diagnostics.Failure = "Cancelled."; return false; }
                if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, diagnostics, out decoded, out var selected, retailOnly: IsRetailRecoveryPass(expectedType, pass))) {
                    if (IsCorroboratedRetailRecovery(pixels, width, height, stride, format, expectedType, options, cancellationToken, decoded, selected)) {
                        diagnostics.Success = true;
                        return true;
                    }
                    decoded = null!;
                }
            }
        }
        diagnostics.Failure ??= diagnostics.CandidateCount == 0 ? "No scanline candidates." : "No supported barcode decoded.";
        return false;
    }

    /// <summary>
    /// Attempts to decode all 1D barcodes from a raw pixel buffer.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out BarcodeDecoded[] decoded, BarcodeType? expectedType = null, BarcodeDecodeOptions? options = null, CancellationToken cancellationToken = default) {
        return TryDecodeAll(pixels, width, height, stride, format, out decoded, expectedType, options, cancellationToken, deduplicate: true);
    }

    internal static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out BarcodeDecoded[] decoded, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, bool deduplicate, bool recoverAfterHit = true) {
        decoded = Array.Empty<BarcodeDecoded>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        var list = new List<BarcodeDecoded>(4);
        var ordinaryHits = new List<BarcodeDecoded>(4);
        var seen = deduplicate ? new HashSet<string>(StringComparer.Ordinal) : null;

        // Unchecked formats need the same physical tile-boundary validation
        // through the pixel API and SymbolScanner. Keep that policy here.
        if (expectedType == BarcodeType.Pharmacode || expectedType == BarcodeType.ITF || expectedType == BarcodeType.Industrial2of5) {
            if (!TryDecodeAllLocated(pixels, width, height, stride, format, out var located,
                    expectedType, options, cancellationToken, recoverAfterHit)) return false;
            foreach (var candidate in located) AddUnique(list, seen, candidate.Decoded);
            decoded = list.ToArray();
            return decoded.Length > 0;
        }

        for (var pass = 0; pass < PixelPassCount(expectedType) && (pass == 0 || recoverAfterHit || list.Count == 0); pass++) {
            if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates, retailRecovery: IsRetailRecoveryPass(expectedType, pass), retailHint: IsRetailBarcodeType(expectedType), dataBarRecovery: IsDataBarRecoveryPass(expectedType, pass))) continue;
            for (var i = 0; i < candidates.Length;) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) break;
                if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, null, out var hit, out var selected, retailOnly: IsRetailRecoveryPass(expectedType, pass), ordinaryHits: IsRetailRecoveryPass(expectedType, pass) ? ordinaryHits : null)) {
                    if (IsCorroboratedRetailRecovery(pixels, width, height, stride, format, expectedType, options, cancellationToken, hit, selected)) {
                        AddUnique(list, seen, hit);
                        if (pass == 0) ordinaryHits.Add(hit);
                    }
                }
            }
        }

        if (options?.EnableTileScan == true && !DecodeBudget.ShouldAbort(cancellationToken)) {
            ScanTiles(pixels, width, height, stride, format, cancellationToken, options, (tile, tw, th, tstride, _, _) => {
                if (TryDecode(tile, tw, th, tstride, format, expectedType, options, cancellationToken, out var hit)) {
                    AddUnique(list, seen, hit);
                }
                return false;
            }, findPharmacodeRegions: expectedType == BarcodeType.Pharmacode);
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
    }

    internal static bool TryDecodeAllLocated(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        out BarcodeImageCandidate[] decoded,
        BarcodeType? expectedType = null,
        BarcodeDecodeOptions? options = null,
        CancellationToken cancellationToken = default,
        bool recoverAfterHit = true) {
        decoded = Array.Empty<BarcodeImageCandidate>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;

        var list = new List<BarcodeImageCandidate>(8);
        DecodeLocatedRegion(
            pixels,
            width,
            height,
            stride,
            format,
            expectedType,
            options,
            cancellationToken,
            originX: 0,
            originY: 0,
            list,
            recoverAfterHit);

        if (options?.EnableTileScan == true && !DecodeBudget.ShouldAbort(cancellationToken)) {
            ScanTiles(pixels, width, height, stride, format, cancellationToken, options, (tile, tw, th, tstride, x, y) => {
                DecodeLocatedRegion(tile, tw, th, tstride, format, expectedType, options, cancellationToken, x, y, list, recoverAfterHit);
                return false;
            }, findPharmacodeRegions: expectedType == BarcodeType.Pharmacode);
        }

        // Cancellation ends the remaining search; candidates decoded before it remain valid.
        for (var i = list.Count - 1; i >= 0; i--) {
            if (IsBoundaryClippedUncheckedBarcode(list[i], pixels, width, height, stride, format, cancellationToken)) list.RemoveAt(i);
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
        for (var pass = 0; pass < PixelPassCount(expectedType); pass++) {
            if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates, retailRecovery: IsRetailRecoveryPass(expectedType, pass), retailHint: IsRetailBarcodeType(expectedType), dataBarRecovery: IsDataBarRecoveryPass(expectedType, pass))) continue;
            for (var i = 0; i < candidates.Length;) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, null, out decoded, out var selected, retailOnly: IsRetailRecoveryPass(expectedType, pass))) {
                    if (IsCorroboratedRetailRecovery(pixels, width, height, stride, format, expectedType, options, cancellationToken, decoded, selected)) return true;
                    decoded = null!;
                }
            }
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
        for (var pass = 0; pass < PixelPassCount(expectedType); pass++) {
            if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates, retailRecovery: IsRetailRecoveryPass(expectedType, pass), retailHint: IsRetailBarcodeType(expectedType), dataBarRecovery: IsDataBarRecoveryPass(expectedType, pass))) continue;
            diagnostics.CandidateCount += candidates.Length;
            for (var i = 0; i < candidates.Length;) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { diagnostics.Failure = "Cancelled."; return false; }
                if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, diagnostics, out decoded, out var selected, retailOnly: IsRetailRecoveryPass(expectedType, pass))) {
                    if (IsCorroboratedRetailRecovery(pixels, width, height, stride, format, expectedType, options, cancellationToken, decoded, selected)) {
                        diagnostics.Success = true;
                        return true;
                    }
                    decoded = null!;
                }
            }
        }
        diagnostics.Failure ??= diagnostics.CandidateCount == 0 ? "No scanline candidates." : "No supported barcode decoded.";
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

        var list = new List<BarcodeDecoded>(4);
        var ordinaryHits = new List<BarcodeDecoded>(4);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var pass = 0; pass < PixelPassCount(expectedType); pass++) {
            if (!BarcodeScanline.TryGetModuleCandidates(pixels, width, height, stride, format, cancellationToken, out var candidates, retailRecovery: IsRetailRecoveryPass(expectedType, pass), retailHint: IsRetailBarcodeType(expectedType), dataBarRecovery: IsDataBarRecoveryPass(expectedType, pass))) continue;
            for (var i = 0; i < candidates.Length;) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) break;
                if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, null, out var hit, out var selected, retailOnly: IsRetailRecoveryPass(expectedType, pass), ordinaryHits: IsRetailRecoveryPass(expectedType, pass) ? ordinaryHits : null)) {
                    if (IsCorroboratedRetailRecovery(pixels, width, height, stride, format, expectedType, options, cancellationToken, hit, selected)) {
                        AddUnique(list, seen, hit);
                        if (pass == 0) ordinaryHits.Add(hit);
                    }
                }
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

    private static void AddUnique(List<BarcodeDecoded> list, HashSet<string>? seen, BarcodeDecoded decoded) {
        if (decoded is null) return;
        if (seen is null || seen.Add(decoded.DeduplicationKey)) list.Add(decoded);
    }

    private static void DecodeLocatedRegion(
        byte[] pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        BarcodeType? expectedType,
        BarcodeDecodeOptions? options,
        CancellationToken cancellationToken,
        int originX,
        int originY,
        List<BarcodeImageCandidate> decoded,
        bool recoverAfterHit) {
        var region = new ImageRegion(originX, originY, width, height);
        var originalCount = decoded.Count;
        var ordinaryHits = new List<BarcodeDecoded>(4);
        for (var pass = 0; pass < PixelPassCount(expectedType) && (pass == 0 || recoverAfterHit || decoded.Count == originalCount); pass++) {
            if (!BarcodeScanline.TryGetLocatedModuleCandidates(
                    pixels, width, height, stride, format, cancellationToken,
                    out var candidates, retailRecovery: IsRetailRecoveryPass(expectedType, pass), retailHint: IsRetailBarcodeType(expectedType), dataBarRecovery: IsDataBarRecoveryPass(expectedType, pass))) continue;
            for (var i = 0; i < candidates.Length;) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return;
                if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, null, out var hit, out var selected, retailOnly: IsRetailRecoveryPass(expectedType, pass), ordinaryHits: IsRetailRecoveryPass(expectedType, pass) ? ordinaryHits : null)) {
                    if (IsCorroboratedRetailRecovery(pixels, width, height, stride, format, expectedType, options, cancellationToken, hit, selected)) {
                        decoded.Add(new BarcodeImageCandidate(hit, selected, region));
                        if (pass == 0) ordinaryHits.Add(hit);
                    }
                }
            }
        }
    }

    private static void ScanTiles(byte[] rgba, int width, int height, int stride, PixelFormat format, CancellationToken token, BarcodeDecodeOptions? options, Func<byte[], int, int, int, int, int, bool> onTile, bool findPharmacodeRegions = false) {
        if (width <= 0 || height <= 0 || stride < width * 4) return;
        if (findPharmacodeRegions) {
            foreach (var region in FindPharmacodeRegions(rgba, width, height, stride, format, token)) {
                if (ScanBarcodeRegion(region)) return;
            }
        }
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

                if (ScanBarcodeRegion(new ImageRegion(x0, y0, tw, th))) return;
            }
        }

        bool ScanBarcodeRegion(ImageRegion region) {
            var tileStride = checked(region.Width * 4);
            var tile = new byte[checked(tileStride * region.Height)];
            for (var y = 0; y < region.Height; y++) {
                if (DecodeBudget.ShouldAbort(token)) return true;
                Buffer.BlockCopy(rgba, (region.Y + y) * stride + region.X * 4, tile, y * tileStride, tileStride);
            }
            return onTile(tile, region.Width, region.Height, tileStride, region.X, region.Y);
        }
    }
}

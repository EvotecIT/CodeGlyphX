using System.Threading;
using CodeGlyphX.Internal;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    /// <summary>
    /// Confirms a retail image hit against at most four nearby physical rows.
    /// A single checksum-valid texture match on an additional row is insufficient.
    /// Two-pixel spacing tolerates threshold flicker between neighboring image rows.
    /// </summary>
    private static bool IsCorroboratedRetailRecovery(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken,
        BarcodeDecoded decoded, BarcodeScanlineCandidate selected) {
        if (decoded.Type != BarcodeType.EAN && decoded.Type != BarcodeType.UPCA && decoded.Type != BarcodeType.UPCE) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        var extent = selected.IsVertical ? width : height;
        // Preserve raw one-pixel scanline inputs. Images with more than two
        // physical rows must repeat the same payload in a neighboring row.
        if (extent <= 2)
            return true;
        for (var distance = 1; distance <= 2; distance++) {
            for (var direction = -1; direction <= 1; direction += 2) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var position = selected.Position + direction * distance;
                if ((uint)position >= (uint)(selected.IsVertical ? width : height)) continue;
                if (!BarcodeScanline.TryGetRetailRowCandidates(pixels, width, height, stride, format, position,
                        selected.IsVertical, cancellationToken, out var candidates)) continue;
                for (var i = 0; i < candidates.Length;) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    if (TryDecodeScanline(candidates, ref i, expectedType, options, cancellationToken, null,
                            out var neighbor, out _, retailOnly: true) && neighbor.Type == decoded.Type && neighbor.Text == decoded.Text)
                        return true;
                }
            }
        }
        return false;
    }
}

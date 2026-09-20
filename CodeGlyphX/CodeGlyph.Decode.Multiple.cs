using System;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX;

public static partial class CodeGlyph {
    private static bool TryDecodeAllCore(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (cancellationToken.IsCancellationRequested) return false;
        var squareish = IsSquareish(width, height);
        var qrOptionsLocal = ResolveMultiQrOptions(qrOptions);
        var preferQr = squareish && LooksLikeQr(pixels, width, height, stride, format);
        var foundQr = false;

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (squareish) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults, qrOptionsLocal, cancellationToken)) {
                for (var i = 0; i < qrResults.Length; i++) {
                    list.Add(new CodeGlyphDecoded(qrResults[i]));
                }
                foundQr = qrResults.Length > 0;
            }

            if (!preferQr || !foundQr) {
                if (cancellationToken.IsCancellationRequested) return false;
                if (AztecDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out var aztec)) {
                    list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
                }
            }
        }

        if (!preferQr || !foundQr) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, cancellationToken, out var dataMatrix)) {
                list.Add(new CodeGlyphDecoded(dataMatrix));
            }

            if (cancellationToken.IsCancellationRequested) return false;
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out Pdf417Decoded pdf417)) {
                list.Add(new CodeGlyphDecoded(pdf417));
            }
        }

        if (includeBarcode && !preferBarcode) {
            if (cancellationToken.IsCancellationRequested) return false;
            if (BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, format, out var barcodes, expectedBarcode, barcodeOptions, cancellationToken)) {
                for (var i = 0; i < barcodes.Length; i++) {
                    list.Add(new CodeGlyphDecoded(barcodes[i]));
                }
            }
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
    }
}

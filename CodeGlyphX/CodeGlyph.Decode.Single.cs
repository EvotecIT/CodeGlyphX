using System.Threading;
using CodeGlyphX.Rendering;
#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX;

public static partial class CodeGlyph {
    private static bool TryDecodeCore(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        return TryDecodeCore(pixels, width, height, stride, format, out decoded, out _, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions);
    }
}

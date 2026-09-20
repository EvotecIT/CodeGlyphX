using System;
using System.Threading;
using CodeGlyphX.Internal;
#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixDecoder {
    /// <summary>Preserves payload metadata and diagnostics in the unified decoding facade.</summary>
    internal static bool TryDecodeDetailed(PixelSpan pixels, int width, int height, int stride,
        PixelFormat format, CancellationToken cancellationToken, out DataMatrixDecoded decoded,
        out DataMatrixDecodeDiagnostics diagnostics) {
        diagnostics = new DataMatrixDecodeDiagnostics();
        var success = TryDecodePixelsDetailed(pixels, width, height, stride, format, cancellationToken, out decoded, diagnostics);
        diagnostics.Success = success;
        if (!success) diagnostics.Failure = DecodeBudget.ShouldAbort(cancellationToken) ? "Cancelled." : "No Data Matrix decoded.";
        return success;
    }
}

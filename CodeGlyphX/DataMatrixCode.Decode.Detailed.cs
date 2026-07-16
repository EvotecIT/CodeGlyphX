using System;
using System.Threading;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class DataMatrixCode {
    /// <summary>
    /// Attempts to decode Data Matrix PNG bytes and preserve GS1, structured-append, Macro, Reader Programming, and ECI metadata.
    /// </summary>
    public static bool TryDecodePngDetailed(byte[] png, out DataMatrixDecoded decoded) {
        return TryDecodePngDetailed(png, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode Data Matrix PNG bytes with image options and cancellation while preserving control metadata.
    /// </summary>
    public static bool TryDecodePngDetailed(
        byte[] png,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        out DataMatrixDecoded decoded) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = cancellationToken;
        if (token.IsCancellationRequested) { decoded = null!; return false; }
        if (!ImageDecodeHelper.TryDecodePngRgba32(png, options, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out token);
        return DataMatrixDecoder.TryDecodeDetailed(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode Data Matrix PNG bytes from a span and preserve control metadata.
    /// </summary>
    public static bool TryDecodePngDetailed(ReadOnlySpan<byte> png, out DataMatrixDecoded decoded) {
        return TryDecodePngDetailed(png, null, CancellationToken.None, out decoded);
    }

    /// <summary>
    /// Attempts to decode Data Matrix PNG bytes from a span with image options and cancellation while preserving control metadata.
    /// </summary>
    public static bool TryDecodePngDetailed(
        ReadOnlySpan<byte> png,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        out DataMatrixDecoded decoded) {
        var token = cancellationToken;
        if (token.IsCancellationRequested) { decoded = null!; return false; }
        if (!ImageDecodeHelper.TryDecodePngRgba32(png, options, out var rgba, out var width, out var height)) {
            decoded = null!;
            return false;
        }
        using var budget = ImageDecodeHelper.BeginRecognitionBudget(cancellationToken, options, out token);
        return DataMatrixDecoder.TryDecodeDetailed(rgba, width, height, width * 4, PixelFormat.Rgba32, token, out decoded);
    }
#endif
}

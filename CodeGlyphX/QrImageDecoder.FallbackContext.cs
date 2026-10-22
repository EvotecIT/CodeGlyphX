using System;
using System.Threading;

namespace CodeGlyphX;

public static partial class QrImageDecoder {
    private static bool FallbackShouldAbort(CancellationToken cancellationToken) =>
        Internal.DecodeBudget.ShouldAbort(cancellationToken);

    private static void ThrowIfFallbackAborted(CancellationToken cancellationToken) {
        if (FallbackShouldAbort(cancellationToken)) throw new OperationCanceledException(cancellationToken);
    }

    private readonly struct QrFallbackFrame {
        internal QrFallbackFrame(byte[] pixels, int width, int height, int stride, PixelFormat format) {
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
            Width = width;
            Height = height;
            Stride = stride;
            Format = format;
        }

        internal byte[] Pixels { get; }

        internal int Width { get; }

        internal int Height { get; }

        internal int Stride { get; }

        internal PixelFormat Format { get; }
    }
}

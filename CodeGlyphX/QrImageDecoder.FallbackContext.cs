using System;

namespace CodeGlyphX;

public static partial class QrImageDecoder {
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

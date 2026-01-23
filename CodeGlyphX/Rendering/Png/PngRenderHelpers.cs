using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace CodeGlyphX.Rendering.Png;

internal static class PngRenderHelpers {
    internal static void FillBackground(byte[] scanlines, int widthPx, int heightPx, int stride, Rgba32 color) {
        if (scanlines is null) throw new ArgumentNullException(nameof(scanlines));
        var rowLength = stride + 1;
        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowLength);
        try {
            rowBuffer[0] = 0;
            FillRowPixels(rowBuffer, 1, widthPx, color);
            for (var y = 0; y < heightPx; y++) {
                Buffer.BlockCopy(rowBuffer, 0, scanlines, y * rowLength, rowLength);
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }
    }

    internal static void FillBackgroundPixels(byte[] pixels, int widthPx, int heightPx, int stride, Rgba32 color) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        var rowBuffer = ArrayPool<byte>.Shared.Rent(stride);
        try {
            FillRowPixels(rowBuffer, 0, widthPx, color);
            for (var y = 0; y < heightPx; y++) {
                Buffer.BlockCopy(rowBuffer, 0, pixels, y * stride, stride);
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }
    }

    internal static void FillRowPixels(byte[] buffer, int offset, int pixelCount, Rgba32 color) {
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (pixelCount <= 0) return;
        var length = pixelCount * 4;
        var bytes = buffer.AsSpan(offset, length);
        if (BitConverter.IsLittleEndian) {
            uint packed = color.R | ((uint)color.G << 8) | ((uint)color.B << 16) | ((uint)color.A << 24);
            MemoryMarshal.Cast<byte, uint>(bytes).Fill(packed);
            return;
        }
        var p = 0;
        for (var i = 0; i < pixelCount; i++) {
            bytes[p++] = color.R;
            bytes[p++] = color.G;
            bytes[p++] = color.B;
            bytes[p++] = color.A;
        }
    }
}

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

    internal static void ApplyAdaptiveFilterHeuristic(byte[] scanlines, int heightPx, int stride, int bytesPerPixel = 4) {
        if (scanlines is null) throw new ArgumentNullException(nameof(scanlines));
        if (heightPx <= 0) return;
        if (stride <= 0) throw new ArgumentOutOfRangeException(nameof(stride));
        if (bytesPerPixel <= 0) throw new ArgumentOutOfRangeException(nameof(bytesPerPixel));
        var rowLength = stride + 1;
        if (scanlines.Length < heightPx * rowLength) throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlines));

        static int AbsSignedByte(byte value) {
            var signed = (sbyte)value;
            return signed < 0 ? -signed : signed;
        }

        static byte PaethPredictor(int a, int b, int c) {
            var p = a + b - c;
            var pa = Math.Abs(p - a);
            var pb = Math.Abs(p - b);
            var pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return (byte)a;
            return pb <= pc ? (byte)b : (byte)c;
        }

        var filtered = ArrayPool<byte>.Shared.Rent(stride);
        var rawRow = ArrayPool<byte>.Shared.Rent(stride);
        var prevRaw = ArrayPool<byte>.Shared.Rent(stride);
        Array.Clear(prevRaw, 0, stride);
        try {
            var rowStart = 0;
            for (var y = 0; y < heightPx; y++) {
                var rawStart = rowStart + 1;
                Buffer.BlockCopy(scanlines, rawStart, rawRow, 0, stride);

                var sumNone = 0;
                var sumSub = 0;
                var sumUp = 0;
                var sumPaeth = 0;

                for (var i = 0; i < stride; i++) {
                    var raw = rawRow[i];
                    var left = i >= bytesPerPixel ? rawRow[i - bytesPerPixel] : (byte)0;
                    var up = prevRaw[i];
                    var upLeft = i >= bytesPerPixel ? prevRaw[i - bytesPerPixel] : (byte)0;

                    sumNone += AbsSignedByte(raw);
                    sumSub += AbsSignedByte(unchecked((byte)(raw - left)));
                    sumUp += AbsSignedByte(unchecked((byte)(raw - up)));
                    var paeth = PaethPredictor(left, up, upLeft);
                    sumPaeth += AbsSignedByte(unchecked((byte)(raw - paeth)));
                }

                var bestType = 0;
                var bestSum = sumNone;
                if (sumSub < bestSum) {
                    bestType = 1;
                    bestSum = sumSub;
                }
                if (sumUp < bestSum) {
                    bestType = 2;
                    bestSum = sumUp;
                }
                if (sumPaeth < bestSum) {
                    bestType = 4;
                }

                if (bestType == 0) {
                    scanlines[rowStart] = 0;
                } else {
                    scanlines[rowStart] = (byte)bestType;
                    for (var i = 0; i < stride; i++) {
                        var raw = rawRow[i];
                        var left = i >= bytesPerPixel ? rawRow[i - bytesPerPixel] : (byte)0;
                        var up = prevRaw[i];
                        var upLeft = i >= bytesPerPixel ? prevRaw[i - bytesPerPixel] : (byte)0;
                        filtered[i] = bestType switch {
                            1 => unchecked((byte)(raw - left)),
                            2 => unchecked((byte)(raw - up)),
                            4 => unchecked((byte)(raw - PaethPredictor(left, up, upLeft))),
                            _ => raw
                        };
                    }
                    Buffer.BlockCopy(filtered, 0, scanlines, rawStart, stride);
                }

                Buffer.BlockCopy(rawRow, 0, prevRaw, 0, stride);

                rowStart += rowLength;
            }
        } finally {
            ArrayPool<byte>.Shared.Return(prevRaw);
            ArrayPool<byte>.Shared.Return(rawRow);
            ArrayPool<byte>.Shared.Return(filtered);
        }
    }

    internal static void ApplySubFilterRow(byte[] rowBuffer, int offset, int rowLength) {
        if (rowBuffer is null) throw new ArgumentNullException(nameof(rowBuffer));
        if (offset < 0 || offset >= rowBuffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (rowLength <= 1 || offset + rowLength > rowBuffer.Length) throw new ArgumentOutOfRangeException(nameof(rowLength));

        rowBuffer[offset] = 1;
        var prev = 0;
        var end = offset + rowLength;
        for (var i = offset + 1; i < end; i++) {
            var raw = rowBuffer[i];
            rowBuffer[i] = unchecked((byte)(raw - prev));
            prev = raw;
        }
    }

    internal static void ApplySubFilterRow(byte[] rowBuffer, int rowLength) {
        if (rowBuffer is null) throw new ArgumentNullException(nameof(rowBuffer));
        if (rowLength <= 1 || rowLength > rowBuffer.Length) throw new ArgumentOutOfRangeException(nameof(rowLength));

        ApplySubFilterRow(rowBuffer, 0, rowLength);
    }
}

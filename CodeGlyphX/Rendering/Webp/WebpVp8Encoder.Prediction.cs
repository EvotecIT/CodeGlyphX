using System;

namespace CodeGlyphX.Rendering.Webp;

internal static partial class WebpVp8Encoder {
    private static byte[] PadPlane(byte[] input, int width, int height, int paddedWidth, int paddedHeight) {
        var output = new byte[checked(paddedWidth * paddedHeight)];
        for (int y = 0; y < paddedHeight; y++) for (int x = 0; x < paddedWidth; x++)
            output[y * paddedWidth + x] = input[Math.Min(y, height - 1) * width + Math.Min(x, width - 1)];
        return output;
    }
    private static void PrefillPrediction(byte[] plane, int width, int height, int x, int y, int size, int mode) {
        Span<byte> predicted = stackalloc byte[256];
        WebpVp8Prediction.PredictBlock(plane, width, height, x, y, size, mode, predicted);
        for (int row = 0; row < size; row++) for (int col = 0; col < size; col++)
            plane[(y + row) * width + x + col] = predicted[row * size + col];
    }
    private static void CopyPredictionSubblock(ReadOnlySpan<byte> prediction, int stride, int x, int y, Span<byte> block) {
        for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++)
            block[row * 4 + col] = prediction[(y + row) * stride + x + col];
    }
    private static bool PlaneBlockMatches(byte[] source, byte[] prediction, int width, int x, int y, int size) {
        for (int row = 0; row < size; row++) for (int col = 0; col < size; col++) {
            int index = (y + row) * width + x + col;
            if (source[index] != prediction[index]) return false;
        }
        return true;
    }
}

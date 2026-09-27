using System;

namespace CodeGlyphX.Rendering.Webp;

internal static partial class WebpVp8Decoder {
    private static void ApplyDecodedResidual(byte[] plane, int width, int height, int x, int y,
        int[] coefficients, bool is4x4, int mode, bool overrideDc, int dcValue) {
        if (is4x4) PredictDecodedSubblock(plane, width, height, x, y, mode);
        if (overrideDc) {
            coefficients = (int[])coefficients.Clone();
            coefficients[0] = dcValue;
        }
        int[] residual = WebpVp8Transform.InverseTransform4x4(coefficients);
        for (int row = 0; row < 4 && y + row < height; row++) {
            for (int col = 0; col < 4 && x + col < width; col++) {
                int index = (y + row) * width + x + col;
                plane[index] = ClampToByte(plane[index] + residual[row * 4 + col]);
            }
        }
    }

    private static void PredictDecodedBlock(byte[] plane, int width, int height, int x, int y, int size, int mode) {
        Span<byte> predicted = stackalloc byte[256];
        WebpVp8Prediction.PredictBlock(plane, width, height, x, y, size, mode, predicted);
        CopyDecodedPrediction(plane, width, height, x, y, size, predicted);
    }
    private static void PredictDecodedSubblock(byte[] plane, int width, int height, int x, int y, int mode) {
        Span<byte> predicted = stackalloc byte[16];
        WebpVp8Prediction.PredictSubblock(plane, width, height, x, y, mode, predicted);
        CopyDecodedPrediction(plane, width, height, x, y, 4, predicted);
    }
    private static void CopyDecodedPrediction(byte[] plane, int width, int height, int x, int y, int size, ReadOnlySpan<byte> predicted) {
        for (int row = 0; row < size && y + row < height; row++) {
            for (int col = 0; col < size && x + col < width; col++) {
                plane[(y + row) * width + x + col] = predicted[row * size + col];
            }
        }
    }
}

using System;

namespace CodeGlyphX.Rendering.Webp;

internal static partial class WebpVp8Encoder {
    // Forward transform adapted from WebM libvpx (BSD 3-Clause); see THIRD-PARTY-NOTICES.md.
    // VP8 forward integer transform. Keep the specified rounding at each pass;
    // inverting a rounded unit-basis IDCT produces a singular matrix.
    private static void ComputeCoefficients(ReadOnlySpan<int> residual, Span<double> coefficients) {
        Span<int> temporary = stackalloc int[16];
        for (int row = 0; row < 4; row++) {
            int at = row * 4;
            int a = (residual[at] + residual[at + 3]) * 8;
            int b = (residual[at + 1] + residual[at + 2]) * 8;
            int c = (residual[at + 1] - residual[at + 2]) * 8;
            int d = (residual[at] - residual[at + 3]) * 8;
            temporary[at] = a + b;
            temporary[at + 2] = a - b;
            temporary[at + 1] = (c * 2217 + d * 5352 + 14500) >> 12;
            temporary[at + 3] = (d * 2217 - c * 5352 + 7500) >> 12;
        }
        for (int col = 0; col < 4; col++) {
            int a = temporary[col] + temporary[col + 12];
            int b = temporary[col + 4] + temporary[col + 8];
            int c = temporary[col + 4] - temporary[col + 8];
            int d = temporary[col] - temporary[col + 12];
            coefficients[col] = (a + b + 7) >> 4;
            coefficients[col + 8] = (a - b + 7) >> 4;
            coefficients[col + 4] = ((c * 2217 + d * 5352 + 12000) >> 16) + (d == 0 ? 0 : 1);
            coefficients[col + 12] = (d * 2217 - c * 5352 + 51000) >> 16;
        }
    }
    private static void ComputeWalshCoefficients(ReadOnlySpan<double> dcValues, Span<double> coefficients) {
        Span<double> temporary = stackalloc double[16];
        for (int row = 0; row < 4; row++) {
            int at = row * 4;
            double a = dcValues[at] + dcValues[at + 3], b = dcValues[at + 1] + dcValues[at + 2];
            double c = dcValues[at + 1] - dcValues[at + 2], d = dcValues[at] - dcValues[at + 3];
            temporary[at] = a + b; temporary[at + 1] = d + c;
            temporary[at + 2] = a - b; temporary[at + 3] = d - c;
        }
        for (int col = 0; col < 4; col++) {
            double a = temporary[col] + temporary[col + 12], b = temporary[col + 4] + temporary[col + 8];
            double c = temporary[col + 4] - temporary[col + 8], d = temporary[col] - temporary[col + 12];
            coefficients[col] = (a + b) / 2; coefficients[col + 4] = (d + c) / 2;
            coefficients[col + 8] = (a - b) / 2; coefficients[col + 12] = (d - c) / 2;
        }
    }
    private static int[] InverseTransform4x4(int[] input) {
        var output = new int[CoefficientsPerBlock];
        var temp = new int[CoefficientsPerBlock];

        for (var i = 0; i < BlockSize; i++) {
            var ip0 = input[i];
            var ip4 = input[i + 4];
            var ip8 = input[i + 8];
            var ip12 = input[i + 12];

            var a1 = ip0 + ip8;
            var b1 = ip0 - ip8;
            var temp1 = (ip4 * IdctSinpi8Sqrt2) >> 16;
            var temp2 = ip12 + ((ip12 * IdctCospi8Sqrt2Minus1) >> 16);
            var c1 = temp1 - temp2;
            temp1 = ip4 + ((ip4 * IdctCospi8Sqrt2Minus1) >> 16);
            temp2 = (ip12 * IdctSinpi8Sqrt2) >> 16;
            var d1 = temp1 + temp2;

            temp[i] = a1 + d1;
            temp[i + 12] = a1 - d1;
            temp[i + 4] = b1 + c1;
            temp[i + 8] = b1 - c1;
        }

        for (var i = 0; i < BlockSize; i++) {
            var baseIndex = i * BlockSize;
            var t0 = temp[baseIndex];
            var t1 = temp[baseIndex + 1];
            var t2 = temp[baseIndex + 2];
            var t3 = temp[baseIndex + 3];

            var a1 = t0 + t2;
            var b1 = t0 - t2;
            var temp1 = (t1 * IdctSinpi8Sqrt2) >> 16;
            var temp2 = t3 + ((t3 * IdctCospi8Sqrt2Minus1) >> 16);
            var c1 = temp1 - temp2;
            temp1 = t1 + ((t1 * IdctCospi8Sqrt2Minus1) >> 16);
            temp2 = (t3 * IdctSinpi8Sqrt2) >> 16;
            var d1 = temp1 + temp2;

            output[baseIndex] = (a1 + d1 + 4) >> 3;
            output[baseIndex + 3] = (a1 - d1 + 4) >> 3;
            output[baseIndex + 1] = (b1 + c1 + 4) >> 3;
            output[baseIndex + 2] = (b1 - c1 + 4) >> 3;
        }

        return output;
    }

    private static int[] InverseWalshTransform4x4(int[] input) {
        var temp = new int[CoefficientsPerBlock];
        var output = new int[CoefficientsPerBlock];

        for (var i = 0; i < BlockSize; i++) {
            var ip0 = input[i];
            var ip4 = input[i + 4];
            var ip8 = input[i + 8];
            var ip12 = input[i + 12];

            var a1 = ip0 + ip12;
            var b1 = ip4 + ip8;
            var c1 = ip4 - ip8;
            var d1 = ip0 - ip12;

            temp[i] = a1 + b1;
            temp[i + 4] = c1 + d1;
            temp[i + 8] = a1 - b1;
            temp[i + 12] = d1 - c1;
        }

        for (var i = 0; i < BlockSize; i++) {
            var baseIndex = i * BlockSize;
            var t0 = temp[baseIndex];
            var t1 = temp[baseIndex + 1];
            var t2 = temp[baseIndex + 2];
            var t3 = temp[baseIndex + 3];

            var a1 = t0 + t3;
            var b1 = t1 + t2;
            var c1 = t1 - t2;
            var d1 = t0 - t3;

            var a2 = a1 + b1;
            var b2 = c1 + d1;
            var c2 = a1 - b1;
            var d2 = d1 - c1;

            output[baseIndex] = (a2 + 3) >> 3;
            output[baseIndex + 1] = (b2 + 3) >> 3;
            output[baseIndex + 2] = (c2 + 3) >> 3;
            output[baseIndex + 3] = (d2 + 3) >> 3;
        }

        return output;
    }

}

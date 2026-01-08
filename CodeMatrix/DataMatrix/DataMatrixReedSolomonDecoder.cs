using System;

namespace CodeGlyphX.DataMatrix;

internal static class DataMatrixReedSolomonDecoder {
    public static bool TryCorrectInPlace(byte[] codewords, int eccLen) {
        if (codewords is null) throw new ArgumentNullException(nameof(codewords));
        if (eccLen <= 0) throw new ArgumentOutOfRangeException(nameof(eccLen));
        if (eccLen >= codewords.Length) throw new ArgumentOutOfRangeException(nameof(eccLen));

        var syndromes = new byte[eccLen];
        var hasError = false;
        for (var i = 0; i < eccLen; i++) {
            var eval = (byte)0;
            var x = DataMatrixReedSolomon.ExpOf(i);
            for (var j = 0; j < codewords.Length; j++) eval = (byte)(DataMatrixReedSolomon.Multiply(eval, x) ^ codewords[j]);
            syndromes[i] = eval;
            if (eval != 0) hasError = true;
        }

        if (!hasError) return true;

        // Berlekamp–Massey
        var sigma = new byte[eccLen + 1];
        var sigma2 = new byte[eccLen + 1];
        sigma[0] = 1;
        sigma2[0] = 1;

        var L = 0;
        var m = 1;
        var b = (byte)1;

        for (var n = 0; n < eccLen; n++) {
            var d = syndromes[n];
            for (var i = 1; i <= L; i++) d ^= DataMatrixReedSolomon.Multiply(sigma[i], syndromes[n - i]);

            if (d == 0) {
                m++;
                continue;
            }

            var t = (byte[])sigma.Clone();
            var coef = DataMatrixReedSolomon.Multiply(d, DataMatrixReedSolomon.Inverse(b));
            for (var i = 0; i < sigma2.Length; i++) {
                if (sigma2[i] == 0) continue;
                var idx = i + m;
                if (idx >= sigma.Length) break;
                sigma[idx] ^= DataMatrixReedSolomon.Multiply(coef, sigma2[i]);
            }

            if (2 * L <= n) {
                L = n + 1 - L;
                sigma2 = t;
                b = d;
                m = 1;
            } else {
                m++;
            }
        }

        if (L == 0 || L > eccLen / 2) return false;

        // Find error locations (Chien search)
        var errorLocations = new byte[L];
        var e = 0;
        for (var i = 1; i < 256 && e < L; i++) {
            if (EvaluatePoly(sigma, L + 1, (byte)i) == 0) {
                errorLocations[e] = DataMatrixReedSolomon.Inverse((byte)i);
                e++;
            }
        }
        if (e != L) return false;

        // Omega = (S * sigma) mod x^eccLen
        var omega = new byte[eccLen];
        for (var i = 0; i <= L; i++) {
            if (sigma[i] == 0) continue;
            for (var j = 0; j < eccLen; j++) {
                var idx = i + j;
                if (idx >= eccLen) break;
                omega[idx] ^= DataMatrixReedSolomon.Multiply(sigma[i], syndromes[j]);
            }
        }

        // sigma' (formal derivative)
        var sigmaDeriv = new byte[L];
        for (var i = 1; i <= L; i += 2) sigmaDeriv[i - 1] = sigma[i];

        // Forney: correct
        for (var i = 0; i < errorLocations.Length; i++) {
            var xi = errorLocations[i];
            var xiInv = DataMatrixReedSolomon.Inverse(xi);

            var numerator = EvaluatePoly(omega, omega.Length, xiInv);
            var denominator = EvaluatePoly(sigmaDeriv, sigmaDeriv.Length, xiInv);
            if (denominator == 0) return false;

            var magnitude = DataMatrixReedSolomon.Multiply(numerator, DataMatrixReedSolomon.Inverse(denominator));
            var position = codewords.Length - 1 - DataMatrixReedSolomon.LogOf(xi);
            if ((uint)position >= (uint)codewords.Length) return false;
            codewords[position] ^= magnitude;
        }

        // Verify
        for (var i = 0; i < eccLen; i++) {
            var eval = (byte)0;
            var x = DataMatrixReedSolomon.ExpOf(i);
            for (var j = 0; j < codewords.Length; j++) eval = (byte)(DataMatrixReedSolomon.Multiply(eval, x) ^ codewords[j]);
            if (eval != 0) return false;
        }

        return true;
    }

    private static byte EvaluatePoly(byte[] poly, int length, byte x) {
        var y = (byte)0;
        for (var i = length - 1; i >= 0; i--) y = (byte)(DataMatrixReedSolomon.Multiply(y, x) ^ poly[i]);
        return y;
    }
}

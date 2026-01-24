using System;
using System.Buffers;

namespace CodeGlyphX.Qr;

internal static class QrReedSolomonDecoder {
    public static bool TryCorrectInPlace(byte[] codewords, int eccLen, Func<bool>? shouldStop = null) {
        if (codewords is null) throw new ArgumentNullException(nameof(codewords));
        return TryCorrectInPlace(codewords, codewords.Length, eccLen, shouldStop);
    }

    public static bool TryCorrectInPlace(byte[] codewords, int codewordCount, int eccLen, Func<bool>? shouldStop = null) {
        if (codewords is null) throw new ArgumentNullException(nameof(codewords));
        if (codewordCount < 0 || codewordCount > codewords.Length) throw new ArgumentOutOfRangeException(nameof(codewordCount));
        if (eccLen <= 0) throw new ArgumentOutOfRangeException(nameof(eccLen));
        if (eccLen >= codewordCount) throw new ArgumentOutOfRangeException(nameof(eccLen));
        if (shouldStop is null) return TryCorrectInPlaceNoStop(codewords, codewordCount, eccLen);

        const int StackallocThreshold = 128;
        byte[]? syndromesArray = null;
        byte[]? sigmaArray = null;
        byte[]? sigma2Array = null;
        byte[]? sigmaTempArray = null;
        byte[]? omegaArray = null;
        byte[]? errorLocationsArray = null;
        byte[]? sigmaDerivArray = null;

        try {
            Span<byte> syndromes = eccLen <= StackallocThreshold
                ? stackalloc byte[eccLen]
                : (syndromesArray = ArrayPool<byte>.Shared.Rent(eccLen)).AsSpan(0, eccLen);
            syndromes.Clear();

            var hasError = false;
            for (var i = 0; i < eccLen; i++) {
                if (shouldStop?.Invoke() == true) return false;
                var eval = (byte)0;
                var x = QrReedSolomon.ExpOf(i);
                for (var j = 0; j < codewordCount; j++) {
                    if ((j & 15) == 0 && shouldStop?.Invoke() == true) return false;
                    eval = (byte)(QrReedSolomon.Multiply(eval, x) ^ codewords[j]);
                }
                syndromes[i] = eval;
                if (eval != 0) hasError = true;
            }

            if (!hasError) return true;

            // Berlekamp–Massey
            var sigmaLen = eccLen + 1;
            Span<byte> sigma = sigmaLen <= StackallocThreshold
                ? stackalloc byte[sigmaLen]
                : (sigmaArray = ArrayPool<byte>.Shared.Rent(sigmaLen)).AsSpan(0, sigmaLen);
            Span<byte> sigma2 = sigmaLen <= StackallocThreshold
                ? stackalloc byte[sigmaLen]
                : (sigma2Array = ArrayPool<byte>.Shared.Rent(sigmaLen)).AsSpan(0, sigmaLen);
            Span<byte> sigmaTemp = sigmaLen <= StackallocThreshold
                ? stackalloc byte[sigmaLen]
                : (sigmaTempArray = ArrayPool<byte>.Shared.Rent(sigmaLen)).AsSpan(0, sigmaLen);

            sigma.Clear();
            sigma2.Clear();
            sigmaTemp.Clear();
            sigma[0] = 1;
            sigma2[0] = 1;

            var L = 0;
            var m = 1;
            var b = (byte)1;

            for (var n = 0; n < eccLen; n++) {
                if (shouldStop?.Invoke() == true) return false;
                var d = syndromes[n];
                for (var i = 1; i <= L; i++) d ^= QrReedSolomon.Multiply(sigma[i], syndromes[n - i]);

                if (d == 0) {
                    m++;
                    continue;
                }

                sigma.CopyTo(sigmaTemp);
                var coef = QrReedSolomon.Multiply(d, QrReedSolomon.Inverse(b));
                for (var i = 0; i < sigma2.Length; i++) {
                    if (sigma2[i] == 0) continue;
                    var idx = i + m;
                    if (idx >= sigma.Length) break;
                    sigma[idx] ^= QrReedSolomon.Multiply(coef, sigma2[i]);
                }

                if (2 * L <= n) {
                    L = n + 1 - L;
                    var tmp = sigma2;
                    sigma2 = sigmaTemp;
                    sigmaTemp = tmp;
                    b = d;
                    m = 1;
                } else {
                    m++;
                }
            }

            if (L == 0 || L > eccLen / 2) return false;

            // Find error locations (Chien search)
            Span<byte> errorLocations = L <= StackallocThreshold
                ? stackalloc byte[L]
                : (errorLocationsArray = ArrayPool<byte>.Shared.Rent(L)).AsSpan(0, L);
            var e = 0;
            for (var i = 1; i < 256 && e < L; i++) {
                if ((i & 7) == 0 && shouldStop?.Invoke() == true) return false;
                if (EvaluatePoly(sigma, L + 1, (byte)i) == 0) {
                    errorLocations[e] = QrReedSolomon.Inverse((byte)i);
                    e++;
                }
            }
            if (e != L) return false;

            // Omega = (S * sigma) mod x^eccLen
            Span<byte> omega = eccLen <= StackallocThreshold
                ? stackalloc byte[eccLen]
                : (omegaArray = ArrayPool<byte>.Shared.Rent(eccLen)).AsSpan(0, eccLen);
            omega.Clear();
            for (var i = 0; i <= L; i++) {
                if (sigma[i] == 0) continue;
                for (var j = 0; j < eccLen; j++) {
                    if ((j & 15) == 0 && shouldStop?.Invoke() == true) return false;
                    var idx = i + j;
                    if (idx >= eccLen) break;
                    omega[idx] ^= QrReedSolomon.Multiply(sigma[i], syndromes[j]);
                }
            }

            // sigma' (formal derivative)
            Span<byte> sigmaDeriv = L <= StackallocThreshold
                ? stackalloc byte[L]
                : (sigmaDerivArray = ArrayPool<byte>.Shared.Rent(L)).AsSpan(0, L);
            sigmaDeriv.Clear();
            for (var i = 1; i <= L; i += 2) sigmaDeriv[i - 1] = sigma[i];

            // Forney: correct
            for (var i = 0; i < errorLocations.Length; i++) {
                if (shouldStop?.Invoke() == true) return false;
                var xi = errorLocations[i];
                var xiInv = QrReedSolomon.Inverse(xi);

                var numerator = EvaluatePoly(omega, omega.Length, xiInv);
                var denominator = EvaluatePoly(sigmaDeriv, sigmaDeriv.Length, xiInv);
                if (denominator == 0) return false;

                var magnitude = QrReedSolomon.Multiply(numerator, QrReedSolomon.Inverse(denominator));
                var position = codewordCount - 1 - QrReedSolomon.LogOf(xi);
                if ((uint)position >= (uint)codewordCount) return false;
                codewords[position] ^= magnitude;
            }

            // Verify
            for (var i = 0; i < eccLen; i++) {
                if (shouldStop?.Invoke() == true) return false;
                var eval = (byte)0;
                var x = QrReedSolomon.ExpOf(i);
                for (var j = 0; j < codewordCount; j++) {
                    if ((j & 15) == 0 && shouldStop?.Invoke() == true) return false;
                    eval = (byte)(QrReedSolomon.Multiply(eval, x) ^ codewords[j]);
                }
                if (eval != 0) return false;
            }

            return true;
        } finally {
            if (syndromesArray is not null) ArrayPool<byte>.Shared.Return(syndromesArray, clearArray: false);
            if (sigmaArray is not null) ArrayPool<byte>.Shared.Return(sigmaArray, clearArray: false);
            if (sigma2Array is not null) ArrayPool<byte>.Shared.Return(sigma2Array, clearArray: false);
            if (sigmaTempArray is not null) ArrayPool<byte>.Shared.Return(sigmaTempArray, clearArray: false);
            if (omegaArray is not null) ArrayPool<byte>.Shared.Return(omegaArray, clearArray: false);
            if (errorLocationsArray is not null) ArrayPool<byte>.Shared.Return(errorLocationsArray, clearArray: false);
            if (sigmaDerivArray is not null) ArrayPool<byte>.Shared.Return(sigmaDerivArray, clearArray: false);
        }
    }

    private static bool TryCorrectInPlaceNoStop(byte[] codewords, int codewordCount, int eccLen) {
        const int StackallocThreshold = 128;
        byte[]? syndromesArray = null;
        byte[]? sigmaArray = null;
        byte[]? sigma2Array = null;
        byte[]? sigmaTempArray = null;
        byte[]? omegaArray = null;
        byte[]? errorLocationsArray = null;
        byte[]? sigmaDerivArray = null;

        try {
            Span<byte> syndromes = eccLen <= StackallocThreshold
                ? stackalloc byte[eccLen]
                : (syndromesArray = ArrayPool<byte>.Shared.Rent(eccLen)).AsSpan(0, eccLen);
            syndromes.Clear();

            var hasError = false;
            for (var i = 0; i < eccLen; i++) {
                var eval = (byte)0;
                var x = QrReedSolomon.ExpOf(i);
                for (var j = 0; j < codewordCount; j++) {
                    eval = (byte)(QrReedSolomon.Multiply(eval, x) ^ codewords[j]);
                }
                syndromes[i] = eval;
                if (eval != 0) hasError = true;
            }

            if (!hasError) return true;

            // Berlekamp–Massey
            var sigmaLen = eccLen + 1;
            Span<byte> sigma = sigmaLen <= StackallocThreshold
                ? stackalloc byte[sigmaLen]
                : (sigmaArray = ArrayPool<byte>.Shared.Rent(sigmaLen)).AsSpan(0, sigmaLen);
            Span<byte> sigma2 = sigmaLen <= StackallocThreshold
                ? stackalloc byte[sigmaLen]
                : (sigma2Array = ArrayPool<byte>.Shared.Rent(sigmaLen)).AsSpan(0, sigmaLen);
            Span<byte> sigmaTemp = sigmaLen <= StackallocThreshold
                ? stackalloc byte[sigmaLen]
                : (sigmaTempArray = ArrayPool<byte>.Shared.Rent(sigmaLen)).AsSpan(0, sigmaLen);

            sigma.Clear();
            sigma2.Clear();
            sigmaTemp.Clear();
            sigma[0] = 1;
            sigma2[0] = 1;

            var L = 0;
            var m = 1;
            var b = (byte)1;

            for (var n = 0; n < eccLen; n++) {
                var d = syndromes[n];
                for (var i = 1; i <= L; i++) d ^= QrReedSolomon.Multiply(sigma[i], syndromes[n - i]);

                if (d == 0) {
                    m++;
                    continue;
                }

                sigma.CopyTo(sigmaTemp);
                var coef = QrReedSolomon.Multiply(d, QrReedSolomon.Inverse(b));
                for (var i = 0; i < sigma2.Length; i++) {
                    if (sigma2[i] == 0) continue;
                    var idx = i + m;
                    if (idx >= sigma.Length) break;
                    sigma[idx] ^= QrReedSolomon.Multiply(coef, sigma2[i]);
                }

                if (2 * L <= n) {
                    L = n + 1 - L;
                    var tmp = sigma2;
                    sigma2 = sigmaTemp;
                    sigmaTemp = tmp;
                    b = d;
                    m = 1;
                } else {
                    m++;
                }
            }

            if (L == 0 || L > eccLen / 2) return false;

            // Find error locations (Chien search)
            Span<byte> errorLocations = L <= StackallocThreshold
                ? stackalloc byte[L]
                : (errorLocationsArray = ArrayPool<byte>.Shared.Rent(L)).AsSpan(0, L);
            var e = 0;
            for (var i = 1; i < 256 && e < L; i++) {
                if (EvaluatePoly(sigma, L + 1, (byte)i) == 0) {
                    errorLocations[e] = QrReedSolomon.Inverse((byte)i);
                    e++;
                }
            }
            if (e != L) return false;

            // Omega = (S * sigma) mod x^eccLen
            Span<byte> omega = eccLen <= StackallocThreshold
                ? stackalloc byte[eccLen]
                : (omegaArray = ArrayPool<byte>.Shared.Rent(eccLen)).AsSpan(0, eccLen);
            omega.Clear();
            for (var i = 0; i <= L; i++) {
                if (sigma[i] == 0) continue;
                for (var j = 0; j < eccLen; j++) {
                    var idx = i + j;
                    if (idx >= eccLen) break;
                    omega[idx] ^= QrReedSolomon.Multiply(sigma[i], syndromes[j]);
                }
            }

            // sigma' (formal derivative)
            Span<byte> sigmaDeriv = L <= StackallocThreshold
                ? stackalloc byte[L]
                : (sigmaDerivArray = ArrayPool<byte>.Shared.Rent(L)).AsSpan(0, L);
            sigmaDeriv.Clear();
            for (var i = 1; i <= L; i += 2) sigmaDeriv[i - 1] = sigma[i];

            // Forney: correct
            for (var i = 0; i < errorLocations.Length; i++) {
                var xi = errorLocations[i];
                var xiInv = QrReedSolomon.Inverse(xi);

                var numerator = EvaluatePoly(omega, omega.Length, xiInv);
                var denominator = EvaluatePoly(sigmaDeriv, sigmaDeriv.Length, xiInv);
                if (denominator == 0) return false;

                var magnitude = QrReedSolomon.Multiply(numerator, QrReedSolomon.Inverse(denominator));
                var position = codewordCount - 1 - QrReedSolomon.LogOf(xi);
                if ((uint)position >= (uint)codewordCount) return false;
                codewords[position] ^= magnitude;
            }

            // Verify
            for (var i = 0; i < eccLen; i++) {
                var eval = (byte)0;
                var x = QrReedSolomon.ExpOf(i);
                for (var j = 0; j < codewordCount; j++) {
                    eval = (byte)(QrReedSolomon.Multiply(eval, x) ^ codewords[j]);
                }
                if (eval != 0) return false;
            }

            return true;
        } finally {
            if (syndromesArray is not null) ArrayPool<byte>.Shared.Return(syndromesArray, clearArray: false);
            if (sigmaArray is not null) ArrayPool<byte>.Shared.Return(sigmaArray, clearArray: false);
            if (sigma2Array is not null) ArrayPool<byte>.Shared.Return(sigma2Array, clearArray: false);
            if (sigmaTempArray is not null) ArrayPool<byte>.Shared.Return(sigmaTempArray, clearArray: false);
            if (omegaArray is not null) ArrayPool<byte>.Shared.Return(omegaArray, clearArray: false);
            if (errorLocationsArray is not null) ArrayPool<byte>.Shared.Return(errorLocationsArray, clearArray: false);
            if (sigmaDerivArray is not null) ArrayPool<byte>.Shared.Return(sigmaDerivArray, clearArray: false);
        }
    }

    private static byte EvaluatePoly(ReadOnlySpan<byte> poly, int length, byte x) {
        var y = (byte)0;
        for (var i = length - 1; i >= 0; i--) y = (byte)(QrReedSolomon.Multiply(y, x) ^ poly[i]);
        return y;
    }
}

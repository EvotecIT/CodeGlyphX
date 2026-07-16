using System;
using System.Collections.Generic;

namespace CodeGlyphX.Internal.ReedSolomon;

/// <summary>Systematic Reed-Solomon encoding and correction over a prime field.</summary>
internal static class PrimeFieldReedSolomon {
    internal static void EncodeInterleaved(int[] words, int dataLength, int eccLength, int prime, int primitive) {
        if (words is null) throw new ArgumentNullException(nameof(words));
        if (dataLength < 1 || eccLength < 1 || words.Length != dataLength + eccLength) {
            throw new ArgumentException("The word buffer must contain data followed by error-correction space.", nameof(words));
        }

        var step = (words.Length + prime - 2) / (prime - 1);
        for (var start = 0; start < step; start++) {
            var blockData = (dataLength - start + step - 1) / step;
            var blockLength = (words.Length - start + step - 1) / step;
            var blockEcc = blockLength - blockData;
            var block = new int[blockLength];
            for (var i = 0; i < blockData; i++) block[i] = words[start + i * step];
            EncodeBlock(block, blockData, blockEcc, prime, primitive);
            for (var i = 0; i < blockEcc; i++) words[start + (blockData + i) * step] = block[blockData + i];
        }
    }

    internal static void DecodeInterleaved(int[] words, int dataLength, int eccLength, int prime, int primitive) {
        if (words is null) throw new ArgumentNullException(nameof(words));
        if (dataLength < 1 || eccLength < 1 || words.Length != dataLength + eccLength) {
            throw new ArgumentException("The word buffer length is inconsistent with the data and ECC lengths.", nameof(words));
        }

        var step = (words.Length + prime - 2) / (prime - 1);
        for (var start = 0; start < step; start++) {
            var blockData = (dataLength - start + step - 1) / step;
            var blockLength = (words.Length - start + step - 1) / step;
            var block = new int[blockLength];
            for (var i = 0; i < blockLength; i++) block[i] = words[start + i * step];
            DecodeBlock(block, blockLength - blockData, prime, primitive);
            for (var i = 0; i < blockLength; i++) words[start + i * step] = block[i];
        }
    }

    private static void EncodeBlock(int[] words, int dataLength, int eccLength, int prime, int primitive) {
        var generator = BuildGenerator(eccLength, prime, primitive);
        var remainder = new int[eccLength];
        for (var i = 0; i < dataLength; i++) {
            var k = Mod(words[i] + remainder[0], prime);
            for (var j = 0; j < eccLength - 1; j++) {
                remainder[j] = Mod(remainder[j + 1] - generator[j + 1] * k, prime);
            }
            remainder[eccLength - 1] = Mod(-generator[eccLength] * k, prime);
        }
        for (var i = 0; i < eccLength; i++) words[dataLength + i] = remainder[i] == 0 ? 0 : prime - remainder[i];
    }

    private static void DecodeBlock(int[] received, int eccLength, int prime, int primitive) {
        var syndromes = new int[eccLength];
        var noError = true;
        for (var i = 0; i < eccLength; i++) {
            syndromes[i] = Evaluate(received, Pow(primitive, i + 1, prime), prime);
            if (syndromes[i] != 0) noError = false;
        }
        if (noError) return;

        var locator = BerlekampMassey(syndromes, prime);
        var errorCount = locator.Length - 1;
        if (errorCount < 1 || errorCount * 2 > eccLength) throw new ReedSolomonException("Too many prime-field Reed-Solomon errors.");

        var positions = new List<int>(errorCount);
        for (var position = 0; position < received.Length; position++) {
            var x = Pow(primitive, received.Length - 1 - position, prime);
            if (EvaluateAscending(locator, Inverse(x, prime), prime) == 0) positions.Add(position);
        }
        if (positions.Count != errorCount) throw new ReedSolomonException("Prime-field error locator roots do not match its degree.");

        var augmented = new int[errorCount, errorCount + 1];
        for (var row = 0; row < errorCount; row++) {
            for (var column = 0; column < errorCount; column++) {
                var x = Pow(primitive, received.Length - 1 - positions[column], prime);
                augmented[row, column] = Pow(x, row + 1, prime);
            }
            augmented[row, errorCount] = syndromes[row];
        }
        var magnitudes = Solve(augmented, prime);
        for (var i = 0; i < positions.Count; i++) received[positions[i]] = Mod(received[positions[i]] - magnitudes[i], prime);

        for (var i = 0; i < eccLength; i++) {
            if (Evaluate(received, Pow(primitive, i + 1, prime), prime) != 0) {
                throw new ReedSolomonException("Prime-field Reed-Solomon correction did not converge.");
            }
        }
    }

    private static int[] BerlekampMassey(int[] syndromes, int prime) {
        var current = new int[syndromes.Length + 1];
        var previous = new int[syndromes.Length + 1];
        current[0] = previous[0] = 1;
        var degree = 0;
        var shift = 1;
        var previousDiscrepancy = 1;

        for (var n = 0; n < syndromes.Length; n++) {
            var discrepancy = syndromes[n];
            for (var i = 1; i <= degree; i++) discrepancy = Mod(discrepancy + current[i] * syndromes[n - i], prime);
            if (discrepancy == 0) { shift++; continue; }

            var snapshot = (int[])current.Clone();
            var scale = Mod(discrepancy * Inverse(previousDiscrepancy, prime), prime);
            for (var i = 0; i + shift < current.Length; i++) {
                current[i + shift] = Mod(current[i + shift] - scale * previous[i], prime);
            }
            if (2 * degree <= n) {
                degree = n + 1 - degree;
                previous = snapshot;
                previousDiscrepancy = discrepancy;
                shift = 1;
            } else {
                shift++;
            }
        }

        var result = new int[degree + 1];
        Array.Copy(current, result, result.Length);
        return result;
    }

    private static int[] Solve(int[,] matrix, int prime) {
        var size = matrix.GetLength(0);
        for (var column = 0; column < size; column++) {
            var pivot = column;
            while (pivot < size && matrix[pivot, column] == 0) pivot++;
            if (pivot == size) throw new ReedSolomonException("Prime-field error magnitudes are singular.");
            if (pivot != column) {
                for (var j = column; j <= size; j++) (matrix[column, j], matrix[pivot, j]) = (matrix[pivot, j], matrix[column, j]);
            }
            var inverse = Inverse(matrix[column, column], prime);
            for (var j = column; j <= size; j++) matrix[column, j] = Mod(matrix[column, j] * inverse, prime);
            for (var row = 0; row < size; row++) {
                if (row == column) continue;
                var factor = matrix[row, column];
                for (var j = column; j <= size; j++) matrix[row, j] = Mod(matrix[row, j] - factor * matrix[column, j], prime);
            }
        }
        var result = new int[size];
        for (var i = 0; i < size; i++) result[i] = matrix[i, size];
        return result;
    }

    private static int[] BuildGenerator(int degree, int prime, int primitive) {
        var result = new[] { 1 };
        for (var i = 1; i <= degree; i++) {
            var root = Pow(primitive, i, prime);
            var next = new int[result.Length + 1];
            for (var j = 0; j < result.Length; j++) {
                next[j] = Mod(next[j] + result[j], prime);
                next[j + 1] = Mod(next[j + 1] - result[j] * root, prime);
            }
            result = next;
        }
        return result;
    }

    private static int Evaluate(int[] coefficients, int value, int prime) {
        var result = coefficients[0];
        for (var i = 1; i < coefficients.Length; i++) result = Mod(result * value + coefficients[i], prime);
        return result;
    }

    private static int EvaluateAscending(int[] coefficients, int value, int prime) {
        var result = 0;
        for (var i = coefficients.Length - 1; i >= 0; i--) result = Mod(result * value + coefficients[i], prime);
        return result;
    }

    private static int Pow(int value, int exponent, int prime) {
        var result = 1;
        var factor = Mod(value, prime);
        while (exponent > 0) {
            if ((exponent & 1) != 0) result = Mod(result * factor, prime);
            factor = Mod(factor * factor, prime);
            exponent >>= 1;
        }
        return result;
    }

    private static int Inverse(int value, int prime) {
        if (value == 0) throw new ReedSolomonException("Cannot invert zero in a prime field.");
        return Pow(value, prime - 2, prime);
    }

    private static int Mod(int value, int prime) {
        var result = value % prime;
        return result < 0 ? result + prime : result;
    }
}

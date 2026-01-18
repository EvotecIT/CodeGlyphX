using System;
using System.Collections.Generic;

namespace CodeGlyphX.Aztec.Internal;

internal sealed class ReedSolomonEncoder {
    private readonly GenericGf _field;
    private readonly List<GenericGfPoly> _cachedGenerators = new();

    public ReedSolomonEncoder(GenericGf field) {
        _field = field ?? throw new ArgumentNullException(nameof(field));
        _cachedGenerators.Add(field.One);
    }

    private GenericGfPoly BuildGenerator(int degree) {
        if (degree >= _cachedGenerators.Count) {
            var lastGenerator = _cachedGenerators[_cachedGenerators.Count - 1];
            for (var d = _cachedGenerators.Count; d <= degree; d++) {
                var next = lastGenerator.Multiply(new GenericGfPoly(_field, new[] { 1, _field.Exp(d - 1 + _field.GeneratorBase) }));
                _cachedGenerators.Add(next);
                lastGenerator = next;
            }
        }
        return _cachedGenerators[degree];
    }

    public void Encode(int[] toEncode, int ecBytes) {
        if (toEncode is null) throw new ArgumentNullException(nameof(toEncode));
        if (ecBytes == 0) throw new ArgumentException("No error correction bytes.", nameof(ecBytes));
        var dataBytes = toEncode.Length - ecBytes;
        if (dataBytes <= 0) throw new ArgumentException("No data bytes provided.", nameof(toEncode));

        var generator = BuildGenerator(ecBytes);
        var infoCoefficients = new int[dataBytes];
        Array.Copy(toEncode, 0, infoCoefficients, 0, dataBytes);
        var info = new GenericGfPoly(_field, infoCoefficients);
        var infoWithZeroes = info.MultiplyByMonomial(ecBytes, 1);
        var remainder = infoWithZeroes.Divide(generator)[1];
        var coefficients = remainder.IsZero ? Array.Empty<int>() : GetCoefficients(remainder);

        var numZeroCoefficients = ecBytes - coefficients.Length;
        for (var i = 0; i < numZeroCoefficients; i++) {
            toEncode[dataBytes + i] = 0;
        }
        Array.Copy(coefficients, 0, toEncode, dataBytes + numZeroCoefficients, coefficients.Length);
    }

    private static int[] GetCoefficients(GenericGfPoly poly) {
        var degree = poly.Degree;
        var result = new int[degree + 1];
        for (var i = 0; i <= degree; i++) {
            result[i] = poly.GetCoefficient(degree - i);
        }
        return result;
    }
}

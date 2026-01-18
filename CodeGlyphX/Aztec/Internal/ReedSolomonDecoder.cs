using System;

namespace CodeGlyphX.Aztec.Internal;

internal sealed class ReedSolomonDecoder {
    private readonly GenericGf _field;

    public ReedSolomonDecoder(GenericGf field) {
        _field = field ?? throw new ArgumentNullException(nameof(field));
    }

    public void Decode(int[] received, int ecBytes) {
        if (received is null) throw new ArgumentNullException(nameof(received));

        var poly = new GenericGfPoly(_field, received);
        var syndromeCoefficients = new int[ecBytes];
        var noError = true;

        for (var i = 0; i < ecBytes; i++) {
            var eval = poly.EvaluateAt(_field.Exp(i + _field.GeneratorBase));
            syndromeCoefficients[ecBytes - 1 - i] = eval;
            if (eval != 0) noError = false;
        }

        if (noError) return;

        var syndrome = new GenericGfPoly(_field, syndromeCoefficients);
        var sigmaOmega = RunEuclideanAlgorithm(_field.BuildMonomial(ecBytes, 1), syndrome, ecBytes);
        var sigma = sigmaOmega[0];
        var omega = sigmaOmega[1];
        var errorLocations = FindErrorLocations(sigma);
        var errorMagnitudes = FindErrorMagnitudes(omega, errorLocations);

        for (var i = 0; i < errorLocations.Length; i++) {
            var position = received.Length - 1 - _field.Log(errorLocations[i]);
            if (position < 0) throw new ReedSolomonException("Bad error location.");
            received[position] = GenericGf.AddOrSubtract(received[position], errorMagnitudes[i]);
        }
    }

    private GenericGfPoly[] RunEuclideanAlgorithm(GenericGfPoly a, GenericGfPoly b, int R) {
        if (a.Degree < b.Degree) {
            (a, b) = (b, a);
        }

        var rLast = a;
        var r = b;
        var tLast = _field.Zero;
        var t = _field.One;

        while (2 * r.Degree >= R) {
            var rLastLast = rLast;
            var tLastLast = tLast;
            rLast = r;
            tLast = t;

            if (rLast.IsZero) throw new ReedSolomonException("r_{i-1} was zero");

            r = rLastLast;
            var q = _field.Zero;
            var denominatorLeadingTerm = rLast.GetCoefficient(rLast.Degree);
            var dltInverse = _field.Inverse(denominatorLeadingTerm);

            while (r.Degree >= rLast.Degree && !r.IsZero) {
                var degreeDiff = r.Degree - rLast.Degree;
                var scale = _field.Multiply(r.GetCoefficient(r.Degree), dltInverse);
                q = q.AddOrSubtract(_field.BuildMonomial(degreeDiff, scale));
                r = r.AddOrSubtract(rLast.MultiplyByMonomial(degreeDiff, scale));
            }

            t = q.Multiply(tLast).AddOrSubtract(tLastLast);
            if (r.Degree >= rLast.Degree) {
                throw new InvalidOperationException("Division algorithm failed to reduce polynomial.");
            }
        }

        var sigmaTildeAtZero = t.GetCoefficient(0);
        if (sigmaTildeAtZero == 0) throw new ReedSolomonException("sigmaTilde(0) was zero");

        var inverse = _field.Inverse(sigmaTildeAtZero);
        var sigma = t.Multiply(inverse);
        var omega = r.Multiply(inverse);
        return new[] { sigma, omega };
    }

    private int[] FindErrorLocations(GenericGfPoly errorLocator) {
        var numErrors = errorLocator.Degree;
        if (numErrors == 1) return new[] { errorLocator.GetCoefficient(1) };

        var result = new int[numErrors];
        var e = 0;
        for (var i = 1; i < _field.Size && e < numErrors; i++) {
            if (errorLocator.EvaluateAt(i) == 0) {
                result[e] = _field.Inverse(i);
                e++;
            }
        }

        if (e != numErrors) throw new ReedSolomonException("Error locator degree does not match number of roots.");
        return result;
    }

    private int[] FindErrorMagnitudes(GenericGfPoly errorEvaluator, int[] errorLocations) {
        var s = errorLocations.Length;
        var result = new int[s];

        for (var i = 0; i < s; i++) {
            var xiInverse = _field.Inverse(errorLocations[i]);
            var denominator = 1;
            for (var j = 0; j < s; j++) {
                if (i == j) continue;
                var term = _field.Multiply(errorLocations[j], xiInverse);
                // In GF(2^m), subtraction equals addition; (1 - term) is term ^ 1.
                var termPlus1 = (term & 0x1) == 0 ? term | 1 : term & ~1;
                denominator = _field.Multiply(denominator, termPlus1);
            }

            result[i] = _field.Multiply(errorEvaluator.EvaluateAt(xiInverse), _field.Inverse(denominator));
            if (_field.GeneratorBase != 0) {
                result[i] = _field.Multiply(result[i], xiInverse);
            }
        }

        return result;
    }
}

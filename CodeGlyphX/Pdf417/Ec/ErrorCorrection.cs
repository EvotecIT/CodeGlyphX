using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pdf417.Ec;

internal sealed partial class ErrorCorrection {
    private readonly ModulusGF _field;
    private readonly int _maximumParity;

    public ErrorCorrection() : this(ModulusGF.Pdf417, 512) { }

    internal ErrorCorrection(ModulusGF field, int maximumParity) {
        _field = field;
        _maximumParity = maximumParity;
    }

    /// <summary>Corrects prime-field codewords, using known unreadable positions when available.
    /// Every successful correction has zero syndromes; errors cost two parity words and erasures one.</summary>
    public bool Decode(int[] received, int numECCodewords, int[]? erasures = null, CancellationToken cancellationToken = default) {
        if (DecodeBudget.ShouldAbort(cancellationToken) || received.Length == 0 || received.Length >= _field.Size ||
            numECCodewords < 1 || numECCodewords > _maximumParity || numECCodewords >= received.Length) return false;
        foreach (var word in received) if ((uint)word >= _field.Size) return false;
        var erasureCount = erasures?.Length ?? 0;
        if (erasureCount > numECCodewords) return false;
        if (erasureCount > 0) {
            var seen = new bool[received.Length];
            foreach (var position in erasures!) {
                if ((uint)position >= received.Length || seen[position]) return false;
                seen[position] = true;
            }
        }
        var poly = new ModulusPoly(_field, received);
        var syndromes = new int[numECCodewords];
        var error = false;
        for (var i = numECCodewords; i > 0; i--) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var eval = poly.EvaluateAt(_field.Exp(i));
            syndromes[numECCodewords - i] = eval;
            if (eval != 0) error = true;
        }

        if (!error) return true;

        var syndrome = new ModulusPoly(_field, syndromes);
        ModulusPoly sigma, omega;
        if (erasureCount > 0) {
            if (!TryFindErasurePolynomials(received.Length, syndromes, erasures!, cancellationToken, out sigma, out omega)) return false;
        } else {
            var sigmaOmega = RunEuclideanAlgorithm(_field.BuildMonomial(numECCodewords, 1), syndrome, numECCodewords, cancellationToken);
            if (sigmaOmega is null) return false;
            sigma = sigmaOmega[0];
            omega = sigmaOmega[1];
        }
        if ((sigma.Degree - erasureCount) * 2 + erasureCount > numECCodewords) return false;

        var errorLocations = FindErrorLocations(sigma, cancellationToken);
        if (errorLocations is null) return false;

        var errorMagnitudes = FindErrorMagnitudes(omega, sigma, errorLocations, cancellationToken);
        if (errorMagnitudes is null) return false;
        for (var i = 0; i < errorLocations.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var position = received.Length - 1 - _field.Log(errorLocations[i]);
            if ((uint)position >= received.Length) return false;
            received[position] = _field.Subtract(received[position], errorMagnitudes[i]);
        }

        var corrected = new ModulusPoly(_field, received);
        for (var i = 1; i <= numECCodewords; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken) || corrected.EvaluateAt(_field.Exp(i)) != 0) return false;
        }
        return true;
    }

    private ModulusPoly[]? RunEuclideanAlgorithm(ModulusPoly a, ModulusPoly b, int r, CancellationToken cancellationToken) {
        if (a.Degree < b.Degree) {
            var temp = a;
            a = b;
            b = temp;
        }

        var rLast = a;
        var rCurr = b;
        var tLast = _field.Zero;
        var tCurr = _field.One;

        while (rCurr.Degree >= r / 2) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return null;
            var rLastLast = rLast;
            var tLastLast = tLast;
            rLast = rCurr;
            tLast = tCurr;

            if (rLast.IsZero) return null;

            rCurr = rLastLast;
            var q = _field.Zero;
            var denominatorLeadingTerm = rLast.GetCoefficient(rLast.Degree);
            var dltInverse = _field.Inverse(denominatorLeadingTerm);

            while (rCurr.Degree >= rLast.Degree && !rCurr.IsZero) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return null;
                var degreeDiff = rCurr.Degree - rLast.Degree;
                var scale = _field.Multiply(rCurr.GetCoefficient(rCurr.Degree), dltInverse);
                q = q.Add(_field.BuildMonomial(degreeDiff, scale));
                rCurr = rCurr.Subtract(rLast.MultiplyByMonomial(degreeDiff, scale));
            }

            tCurr = q.Multiply(tLast).Subtract(tLastLast).GetNegative();
        }

        var sigmaTildeAtZero = tCurr.GetCoefficient(0);
        if (sigmaTildeAtZero == 0) return null;

        var inverse = _field.Inverse(sigmaTildeAtZero);
        var sigma = tCurr.Multiply(inverse);
        var omega = rCurr.Multiply(inverse);
        return new[] { sigma, omega };
    }

    private int[]? FindErrorLocations(ModulusPoly errorLocator, CancellationToken cancellationToken) {
        var numErrors = errorLocator.Degree;
        var result = new int[numErrors];
        var e = 0;
        for (var i = 1; i < _field.Size && e < numErrors; i++) {
            if ((i & 31) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return null;
            if (errorLocator.EvaluateAt(i) == 0) {
                result[e] = _field.Inverse(i);
                e++;
            }
        }
        if (e != numErrors) return null;
        return result;
    }

    private int[]? FindErrorMagnitudes(ModulusPoly errorEvaluator, ModulusPoly errorLocator, int[] errorLocations, CancellationToken cancellationToken) {
        var errorLocatorDegree = errorLocator.Degree;
        if (errorLocatorDegree < 1) return new int[0];
        var formalDerivativeCoefficients = new int[errorLocatorDegree];
        for (var i = 1; i <= errorLocatorDegree; i++) {
            formalDerivativeCoefficients[errorLocatorDegree - i] = _field.Multiply(i, errorLocator.GetCoefficient(i));
        }
        var formalDerivative = new ModulusPoly(_field, formalDerivativeCoefficients);

        var s = errorLocations.Length;
        var result = new int[s];
        for (var i = 0; i < s; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return null;
            var xiInverse = _field.Inverse(errorLocations[i]);
            var numerator = _field.Subtract(0, errorEvaluator.EvaluateAt(xiInverse));
            var derivative = formalDerivative.EvaluateAt(xiInverse);
            if (derivative == 0) return null;
            var denominator = _field.Inverse(derivative);
            result[i] = _field.Multiply(numerator, denominator);
        }
        return result;
    }
}

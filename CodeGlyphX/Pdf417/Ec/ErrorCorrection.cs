namespace CodeGlyphX.Pdf417.Ec;

internal sealed class ErrorCorrection {
    private readonly ModulusGF _field;

    public ErrorCorrection() {
        _field = ModulusGF.Pdf417;
    }

    public bool Decode(int[] received, int numECCodewords) {
        var poly = new ModulusPoly(_field, received);
        var syndromes = new int[numECCodewords];
        var error = false;
        for (var i = numECCodewords; i > 0; i--) {
            var eval = poly.EvaluateAt(_field.Exp(i));
            syndromes[numECCodewords - i] = eval;
            if (eval != 0) error = true;
        }

        if (!error) return true;

        var syndrome = new ModulusPoly(_field, syndromes);
        var sigmaOmega = RunEuclideanAlgorithm(_field.BuildMonomial(numECCodewords, 1), syndrome, numECCodewords);
        if (sigmaOmega is null) return false;

        var sigma = sigmaOmega[0];
        var omega = sigmaOmega[1];
        if (sigma is null || omega is null) return false;

        var errorLocations = FindErrorLocations(sigma);
        if (errorLocations is null) return false;

        var errorMagnitudes = FindErrorMagnitudes(omega, sigma, errorLocations);
        for (var i = 0; i < errorLocations.Length; i++) {
            var position = received.Length - 1 - _field.Log(errorLocations[i]);
            if (position < 0) return false;
            received[position] = _field.Subtract(received[position], errorMagnitudes[i]);
        }

        return true;
    }

    private ModulusPoly[]? RunEuclideanAlgorithm(ModulusPoly a, ModulusPoly b, int r) {
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

    private int[]? FindErrorLocations(ModulusPoly errorLocator) {
        var numErrors = errorLocator.Degree;
        var result = new int[numErrors];
        var e = 0;
        for (var i = 1; i < _field.Size && e < numErrors; i++) {
            if (errorLocator.EvaluateAt(i) == 0) {
                result[e] = _field.Inverse(i);
                e++;
            }
        }
        if (e != numErrors) return null;
        return result;
    }

    private int[] FindErrorMagnitudes(ModulusPoly errorEvaluator, ModulusPoly errorLocator, int[] errorLocations) {
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
            var xiInverse = _field.Inverse(errorLocations[i]);
            var numerator = _field.Subtract(0, errorEvaluator.EvaluateAt(xiInverse));
            var denominator = _field.Inverse(formalDerivative.EvaluateAt(xiInverse));
            result[i] = _field.Multiply(numerator, denominator);
        }
        return result;
    }
}

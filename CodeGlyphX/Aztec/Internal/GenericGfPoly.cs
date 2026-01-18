using System;

namespace CodeGlyphX.Aztec.Internal;

internal sealed class GenericGfPoly {
    private readonly GenericGf _field;
    private readonly int[] _coefficients;

    public int Degree => _coefficients.Length - 1;

    public bool IsZero => _coefficients[0] == 0;

    public GenericGfPoly(GenericGf field, int[] coefficients) {
        _field = field ?? throw new ArgumentNullException(nameof(field));
        if (coefficients is null || coefficients.Length == 0) throw new ArgumentException("Coefficients cannot be empty.", nameof(coefficients));

        var offset = 0;
        while (offset < coefficients.Length - 1 && coefficients[offset] == 0) offset++;

        _coefficients = new int[coefficients.Length - offset];
        Array.Copy(coefficients, offset, _coefficients, 0, _coefficients.Length);
    }

    public int GetCoefficient(int degree) {
        return _coefficients[_coefficients.Length - 1 - degree];
    }

    public int EvaluateAt(int a) {
        if (a == 0) return GetCoefficient(0);
        if (a == 1) {
            var result = 0;
            for (var i = 0; i < _coefficients.Length; i++) result = GenericGf.AddOrSubtract(result, _coefficients[i]);
            return result;
        }

        var resultValue = _coefficients[0];
        for (var i = 1; i < _coefficients.Length; i++) {
            resultValue = GenericGf.AddOrSubtract(_field.Multiply(a, resultValue), _coefficients[i]);
        }
        return resultValue;
    }

    public GenericGfPoly AddOrSubtract(GenericGfPoly other) {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (_field != other._field) throw new ArgumentException("Fields do not match.", nameof(other));
        if (IsZero) return other;
        if (other.IsZero) return this;

        var smaller = _coefficients;
        var larger = other._coefficients;
        if (smaller.Length > larger.Length) {
            (smaller, larger) = (larger, smaller);
        }

        var sum = new int[larger.Length];
        var lengthDiff = larger.Length - smaller.Length;
        Array.Copy(larger, 0, sum, 0, lengthDiff);
        for (var i = lengthDiff; i < larger.Length; i++) {
            sum[i] = GenericGf.AddOrSubtract(smaller[i - lengthDiff], larger[i]);
        }

        return new GenericGfPoly(_field, sum);
    }

    public GenericGfPoly Multiply(GenericGfPoly other) {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (_field != other._field) throw new ArgumentException("Fields do not match.", nameof(other));
        if (IsZero || other.IsZero) return _field.Zero;

        var a = _coefficients;
        var b = other._coefficients;
        var product = new int[a.Length + b.Length - 1];

        for (var i = 0; i < a.Length; i++) {
            var aCoeff = a[i];
            for (var j = 0; j < b.Length; j++) {
                product[i + j] = GenericGf.AddOrSubtract(product[i + j], _field.Multiply(aCoeff, b[j]));
            }
        }

        return new GenericGfPoly(_field, product);
    }

    public GenericGfPoly Multiply(int scalar) {
        if (scalar == 0) return _field.Zero;
        if (scalar == 1) return this;

        var size = _coefficients.Length;
        var product = new int[size];
        for (var i = 0; i < size; i++) product[i] = _field.Multiply(_coefficients[i], scalar);
        return new GenericGfPoly(_field, product);
    }

    public GenericGfPoly MultiplyByMonomial(int degree, int coefficient) {
        if (degree < 0) throw new ArgumentOutOfRangeException(nameof(degree));
        if (coefficient == 0) return _field.Zero;

        var size = _coefficients.Length;
        var product = new int[size + degree];
        for (var i = 0; i < size; i++) {
            product[i] = _field.Multiply(_coefficients[i], coefficient);
        }

        return new GenericGfPoly(_field, product);
    }

    public GenericGfPoly[] Divide(GenericGfPoly other) {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (_field != other._field) throw new ArgumentException("Fields do not match.", nameof(other));
        if (other.IsZero) throw new ArgumentException("Divide by 0 polynomial.", nameof(other));

        var quotient = _field.Zero;
        var remainder = this;

        var denominatorLeadingTerm = other.GetCoefficient(other.Degree);
        var inverseDenominatorLeadingTerm = _field.Inverse(denominatorLeadingTerm);

        while (remainder.Degree >= other.Degree && !remainder.IsZero) {
            var degreeDifference = remainder.Degree - other.Degree;
            var scale = _field.Multiply(remainder.GetCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
            var term = other.MultiplyByMonomial(degreeDifference, scale);
            var iterationQuotient = _field.BuildMonomial(degreeDifference, scale);
            quotient = quotient.AddOrSubtract(iterationQuotient);
            remainder = remainder.AddOrSubtract(term);
        }

        return new[] { quotient, remainder };
    }
}

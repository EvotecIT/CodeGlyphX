using System;
using System.Text;

namespace CodeGlyphX.Pdf417.Ec;

internal sealed class ModulusPoly {
    private readonly ModulusGF _field;
    private readonly int[] _coefficients;

    public ModulusPoly(ModulusGF field, int[] coefficients) {
        if (coefficients.Length == 0) throw new ArgumentException("Coefficients must be non-empty.", nameof(coefficients));
        _field = field;
        var coefficientsLength = coefficients.Length;
        if (coefficientsLength > 1 && coefficients[0] == 0) {
            var firstNonZero = 1;
            while (firstNonZero < coefficientsLength && coefficients[firstNonZero] == 0) {
                firstNonZero++;
            }
            if (firstNonZero == coefficientsLength) {
                _coefficients = new[] { 0 };
            } else {
                _coefficients = new int[coefficientsLength - firstNonZero];
                Array.Copy(coefficients, firstNonZero, _coefficients, 0, _coefficients.Length);
            }
        } else {
            _coefficients = coefficients;
        }
    }

    internal int[] Coefficients => _coefficients;

    internal int Degree => _coefficients.Length - 1;

    internal bool IsZero => _coefficients[0] == 0;

    internal int GetCoefficient(int degree) => _coefficients[_coefficients.Length - 1 - degree];

    internal int EvaluateAt(int a) {
        if (a == 0) return GetCoefficient(0);
        if (a == 1) {
            var result = 0;
            foreach (var coefficient in _coefficients) result = _field.Add(result, coefficient);
            return result;
        }
        var res = _coefficients[0];
        for (var i = 1; i < _coefficients.Length; i++) {
            res = _field.Add(_field.Multiply(a, res), _coefficients[i]);
        }
        return res;
    }

    internal ModulusPoly Add(ModulusPoly other) {
        if (!_field.Equals(other._field)) throw new ArgumentException("ModulusPolys do not have same ModulusGF field");
        if (IsZero) return other;
        if (other.IsZero) return this;

        var smaller = _coefficients;
        var larger = other._coefficients;
        if (smaller.Length > larger.Length) {
            var temp = smaller;
            smaller = larger;
            larger = temp;
        }

        var sum = new int[larger.Length];
        var lengthDiff = larger.Length - smaller.Length;
        Array.Copy(larger, 0, sum, 0, lengthDiff);
        for (var i = lengthDiff; i < larger.Length; i++) {
            sum[i] = _field.Add(smaller[i - lengthDiff], larger[i]);
        }
        return new ModulusPoly(_field, sum);
    }

    internal ModulusPoly Subtract(ModulusPoly other) {
        if (!_field.Equals(other._field)) throw new ArgumentException("ModulusPolys do not have same ModulusGF field");
        if (other.IsZero) return this;
        return Add(other.GetNegative());
    }

    internal ModulusPoly Multiply(ModulusPoly other) {
        if (!_field.Equals(other._field)) throw new ArgumentException("ModulusPolys do not have same ModulusGF field");
        if (IsZero || other.IsZero) return _field.Zero;

        var a = _coefficients;
        var b = other._coefficients;
        var product = new int[a.Length + b.Length - 1];
        for (var i = 0; i < a.Length; i++) {
            var aCoeff = a[i];
            for (var j = 0; j < b.Length; j++) {
                product[i + j] = _field.Add(product[i + j], _field.Multiply(aCoeff, b[j]));
            }
        }
        return new ModulusPoly(_field, product);
    }

    internal ModulusPoly GetNegative() {
        var size = _coefficients.Length;
        var neg = new int[size];
        for (var i = 0; i < size; i++) {
            neg[i] = _field.Subtract(0, _coefficients[i]);
        }
        return new ModulusPoly(_field, neg);
    }

    internal ModulusPoly Multiply(int scalar) {
        if (scalar == 0) return _field.Zero;
        if (scalar == 1) return this;
        var product = new int[_coefficients.Length];
        for (var i = 0; i < _coefficients.Length; i++) {
            product[i] = _field.Multiply(_coefficients[i], scalar);
        }
        return new ModulusPoly(_field, product);
    }

    internal ModulusPoly MultiplyByMonomial(int degree, int coefficient) {
        if (degree < 0) throw new ArgumentException("Degree must be non-negative.", nameof(degree));
        if (coefficient == 0) return _field.Zero;
        var product = new int[_coefficients.Length + degree];
        for (var i = 0; i < _coefficients.Length; i++) {
            product[i] = _field.Multiply(_coefficients[i], coefficient);
        }
        return new ModulusPoly(_field, product);
    }

    public override string ToString() {
        var result = new StringBuilder(8 * Degree);
        for (var degree = Degree; degree >= 0; degree--) {
            var coefficient = GetCoefficient(degree);
            if (coefficient == 0) continue;
            if (coefficient < 0) {
                result.Append(" - ");
                coefficient = -coefficient;
            } else if (result.Length > 0) {
                result.Append(" + ");
            }
            if (degree == 0 || coefficient != 1) result.Append(coefficient);
            if (degree != 0) {
                if (degree == 1) result.Append('x');
                else {
                    result.Append("x^");
                    result.Append(degree);
                }
            }
        }
        return result.ToString();
    }
}

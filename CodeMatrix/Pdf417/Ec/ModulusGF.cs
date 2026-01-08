using System;

namespace CodeGlyphX.Pdf417.Ec;

internal sealed class ModulusGF {
    public static ModulusGF Pdf417 = new ModulusGF(929, 3);

    private readonly int[] _expTable;
    private readonly int[] _logTable;
    public ModulusPoly Zero { get; }
    public ModulusPoly One { get; }
    private readonly int _modulus;

    public ModulusGF(int modulus, int generator) {
        _modulus = modulus;
        _expTable = new int[modulus];
        _logTable = new int[modulus];
        var x = 1;
        for (var i = 0; i < modulus; i++) {
            _expTable[i] = x;
            x = (x * generator) % modulus;
        }
        for (var i = 0; i < modulus - 1; i++) {
            _logTable[_expTable[i]] = i;
        }
        Zero = new ModulusPoly(this, new[] { 0 });
        One = new ModulusPoly(this, new[] { 1 });
    }

    internal ModulusPoly BuildMonomial(int degree, int coefficient) {
        if (degree < 0) throw new ArgumentException("Degree must be non-negative.", nameof(degree));
        if (coefficient == 0) return Zero;
        var coefficients = new int[degree + 1];
        coefficients[0] = coefficient;
        return new ModulusPoly(this, coefficients);
    }

    internal int Add(int a, int b) => (a + b) % _modulus;

    internal int Subtract(int a, int b) => (_modulus + a - b) % _modulus;

    internal int Exp(int a) => _expTable[a];

    internal int Log(int a) {
        if (a == 0) throw new ArgumentException("Log(0) is undefined.", nameof(a));
        return _logTable[a];
    }

    internal int Inverse(int a) {
        if (a == 0) throw new ArithmeticException("Cannot invert 0.");
        return _expTable[_modulus - _logTable[a] - 1];
    }

    internal int Multiply(int a, int b) {
        if (a == 0 || b == 0) return 0;
        return _expTable[(_logTable[a] + _logTable[b]) % (_modulus - 1)];
    }

    internal int Size => _modulus;
}

using System;

namespace CodeGlyphX.Aztec.Internal;

internal sealed class GenericGf {
    public static readonly GenericGf AztecParam = new(0x13, 16, 1);
    public static readonly GenericGf AztecData6 = new(0x43, 64, 1);
    public static readonly GenericGf AztecData8 = new(0x12d, 256, 1);
    public static readonly GenericGf AztecData10 = new(0x409, 1024, 1);
    public static readonly GenericGf AztecData12 = new(0x1069, 4096, 1);

    private readonly int[] _expTable;
    private readonly int[] _logTable;

    public int Size { get; }
    public int GeneratorBase { get; }

    public GenericGfPoly Zero { get; }
    public GenericGfPoly One { get; }

    public GenericGf(int primitive, int size, int generatorBase) {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

        Size = size;
        GeneratorBase = generatorBase;
        _expTable = new int[size];
        _logTable = new int[size];

        var x = 1;
        for (var i = 0; i < size; i++) {
            _expTable[i] = x;
            x <<= 1;
            if (x >= size) {
                x ^= primitive;
                x &= size - 1;
            }
        }

        for (var i = 0; i < size - 1; i++) {
            _logTable[_expTable[i]] = i;
        }

        Zero = new GenericGfPoly(this, new[] { 0 });
        One = new GenericGfPoly(this, new[] { 1 });
    }

    public static int AddOrSubtract(int a, int b) => a ^ b;

    public int Exp(int a) => _expTable[a];

    public int Log(int a) {
        if (a == 0) throw new ArgumentException("Cannot take log(0).", nameof(a));
        return _logTable[a];
    }

    public int Inverse(int a) {
        if (a == 0) throw new ArgumentException("Cannot invert 0.", nameof(a));
        return _expTable[Size - 1 - _logTable[a]];
    }

    public int Multiply(int a, int b) {
        if (a == 0 || b == 0) return 0;
        return _expTable[(_logTable[a] + _logTable[b]) % (Size - 1)];
    }

    public GenericGfPoly BuildMonomial(int degree, int coefficient) {
        if (degree < 0) throw new ArgumentOutOfRangeException(nameof(degree));
        if (coefficient == 0) return Zero;

        var coefficients = new int[degree + 1];
        coefficients[0] = coefficient;
        return new GenericGfPoly(this, coefficients);
    }
}

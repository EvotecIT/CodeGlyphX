using System;

namespace CodeMatrix.DataMatrix;

internal static class DataMatrixReedSolomon {
    private const int Primitive = 0x12D;
    private static readonly byte[] Exp = new byte[512];
    private static readonly byte[] Log = new byte[256];

    static DataMatrixReedSolomon() {
        var x = 1;
        for (var i = 0; i < 255; i++) {
            Exp[i] = (byte)x;
            Log[x] = (byte)i;
            x <<= 1;
            if (x >= 256) x ^= Primitive;
        }
        for (var i = 255; i < Exp.Length; i++) Exp[i] = Exp[i - 255];
        Log[0] = 0; // Not used.
    }

    public static byte[] ComputeDivisor(int degree) {
        if (degree is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(degree));

        var result = new byte[degree];
        result[degree - 1] = 1;

        byte root = 1;
        for (var i = 0; i < degree; i++) {
            for (var j = 0; j < result.Length; j++) {
                result[j] = Multiply(result[j], root);
                if (j + 1 < result.Length) result[j] ^= result[j + 1];
            }
            root = Multiply(root, 0x02);
        }

        return result;
    }

    public static byte[] ComputeRemainder(byte[] data, byte[] divisor) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (divisor is null) throw new ArgumentNullException(nameof(divisor));
        if (divisor.Length is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(divisor));

        var result = new byte[divisor.Length];
        for (var i = 0; i < data.Length; i++) {
            var factor = (byte)(data[i] ^ result[0]);

            for (var j = 0; j < result.Length - 1; j++) result[j] = result[j + 1];
            result[result.Length - 1] = 0;

            for (var j = 0; j < result.Length; j++) result[j] ^= Multiply(divisor[j], factor);
        }
        return result;
    }

    internal static byte Multiply(byte x, byte y) {
        if (x == 0 || y == 0) return 0;
        return Exp[Log[x] + Log[y]];
    }

    internal static byte Inverse(byte x) {
        if (x == 0) throw new DivideByZeroException("Cannot invert 0 in GF(256).");
        return Exp[255 - Log[x]];
    }

    internal static int LogOf(byte x) {
        if (x == 0) throw new ArgumentOutOfRangeException(nameof(x), "Log(0) is undefined.");
        return Log[x];
    }

    internal static byte ExpOf(int exponent) => Exp[exponent];
}

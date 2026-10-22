using System;
using System.Threading;
using CodeGlyphX.Pdf417.Ec;

namespace CodeGlyphX.Ultracode;

internal static class UltracodeErrorCorrection {
    internal static readonly ModulusGF Field = new(283, 3);

    // Systematic Reed-Solomon over the existing prime-field arithmetic, roots 3^1 through 3^Q.
    internal static int[] Encode(int[] data, int parityCount) {
        var generator = new[] { 1 };
        for (var root = 1; root <= parityCount; root++) {
            var next = new int[generator.Length + 1];
            for (var i = 0; i < generator.Length; i++) {
                next[i] = Field.Add(next[i], generator[i]);
                next[i + 1] = Field.Subtract(next[i + 1], Field.Multiply(generator[i], Field.Exp(root)));
            }
            generator = next;
        }
        var work = new int[data.Length + parityCount];
        Array.Copy(data, work, data.Length);
        for (var index = 0; index < data.Length; index++) {
            var factor = work[index];
            for (var i = 1; i < generator.Length; i++)
                work[index + i] = Field.Subtract(work[index + i], Field.Multiply(factor, generator[i]));
        }
        var parity = new int[parityCount];
        for (var i = 0; i < parity.Length; i++) parity[i] = Field.Subtract(0, work[data.Length + i]);
        return parity;
    }

    internal static bool Correct(int[] words, int parityCount, int[] erasures, CancellationToken cancellationToken) =>
        new ErrorCorrection(Field, 101).Decode(words, parityCount, erasures, cancellationToken);
}

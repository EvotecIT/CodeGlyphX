using System;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pdf417.Ec;

internal sealed partial class ErrorCorrection {
    // Remove each known erasure from the syndrome recurrence, locate the
    // remaining unknown errors, then evaluate the combined locator against
    // the original syndromes. Coefficients in the recurrence are ascending.
    private bool TryFindErasurePolynomials(int length, int[] syndromes, int[] erasures,
        CancellationToken cancellationToken, out ModulusPoly sigma, out ModulusPoly omega) {
        sigma = omega = _field.Zero;
        var reduced = new int[syndromes.Length];
        for (var i = 0; i < reduced.Length; i++) reduced[i] = syndromes[syndromes.Length - 1 - i];
        var count = reduced.Length;
        var known = _field.One;
        foreach (var position in erasures) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var location = _field.Exp(length - 1 - position);
            known = known.Multiply(new ModulusPoly(_field, new[] { _field.Subtract(0, location), 1 }));
            for (var i = 0; i < count - 1; i++)
                reduced[i] = _field.Subtract(reduced[i + 1], _field.Multiply(location, reduced[i]));
            count--;
        }
        var unknown = FindRecurrence(reduced, count, cancellationToken);
        if (unknown is null || unknown.Degree * 2 + erasures.Length > syndromes.Length) return false;
        sigma = known.Multiply(unknown);
        var product = sigma.Multiply(new ModulusPoly(_field, syndromes));
        var coefficients = product.Coefficients;
        if (coefficients.Length > syndromes.Length) {
            var truncated = new int[syndromes.Length];
            Array.Copy(coefficients, coefficients.Length - truncated.Length, truncated, 0, truncated.Length);
            coefficients = truncated;
        }
        omega = new ModulusPoly(_field, coefficients);
        return !DecodeBudget.ShouldAbort(cancellationToken);
    }

    // Berlekamp-Massey over the existing prime field. Only the erasure-aware
    // path uses this recurrence; ordinary decoding retains its Euclidean path.
    private ModulusPoly? FindRecurrence(int[] sequence, int count, CancellationToken cancellationToken) {
        var current = new int[count + 1];
        var prior = new int[count + 1];
        current[0] = prior[0] = 1;
        var degree = 0;
        var priorDegree = 0;
        var shift = 1;
        var priorDiscrepancy = 1;
        for (var n = 0; n < count; n++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return null;
            var discrepancy = sequence[n];
            for (var i = 1; i <= degree; i++)
                discrepancy = _field.Add(discrepancy, _field.Multiply(current[i], sequence[n - i]));
            if (discrepancy == 0) { shift++; continue; }
            var saved = degree * 2 <= n ? (int[])current.Clone() : null;
            var scale = _field.Multiply(discrepancy, _field.Inverse(priorDiscrepancy));
            for (var i = 0; i <= priorDegree && i + shift < current.Length; i++)
                current[i + shift] = _field.Subtract(current[i + shift], _field.Multiply(scale, prior[i]));
            if (saved is not null) {
                prior = saved;
                priorDegree = degree;
                degree = n + 1 - degree;
                priorDiscrepancy = discrepancy;
                shift = 1;
            } else shift++;
        }
        var coefficients = new int[degree + 1];
        for (var i = 0; i <= degree; i++) coefficients[i] = current[degree - i];
        return new ModulusPoly(_field, coefficients);
    }
}

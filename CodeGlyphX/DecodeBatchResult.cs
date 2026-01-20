using System;

namespace CodeGlyphX;

/// <summary>
/// Result set for a batch decode operation.
/// </summary>
public readonly struct DecodeBatchResult<T> {
    /// <summary>
    /// Gets the per-item decode results.
    /// </summary>
    public DecodeResult<T>[] Results { get; }

    /// <summary>
    /// Gets aggregated diagnostics for the batch.
    /// </summary>
    public DecodeBatchDiagnostics Diagnostics { get; }

    /// <summary>
    /// Gets the number of results returned.
    /// </summary>
    public int Count => Results?.Length ?? 0;

    internal DecodeBatchResult(DecodeResult<T>[] results, DecodeBatchDiagnostics diagnostics) {
        Results = results ?? Array.Empty<DecodeResult<T>>();
        Diagnostics = diagnostics;
    }
}

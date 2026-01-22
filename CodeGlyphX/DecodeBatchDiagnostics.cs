using System;

namespace CodeGlyphX;

/// <summary>
/// Aggregated diagnostics for a batch decode run.
/// </summary>
public readonly struct DecodeBatchDiagnostics {
    /// <summary>
    /// Gets total number of inputs processed.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Gets the number of successful decodes.
    /// </summary>
    public int Success { get; }

    /// <summary>
    /// Gets the number of invalid input failures.
    /// </summary>
    public int InvalidInput { get; }

    /// <summary>
    /// Gets the number of unsupported format failures.
    /// </summary>
    public int UnsupportedFormat { get; }

    /// <summary>
    /// Gets the number of cancelled decodes.
    /// </summary>
    public int Cancelled { get; }

    /// <summary>
    /// Gets the number of no-result decodes.
    /// </summary>
    public int NoResult { get; }

    /// <summary>
    /// Gets the number of unexpected errors.
    /// </summary>
    public int Error { get; }

    /// <summary>
    /// Gets the total elapsed time across all items.
    /// </summary>
    public TimeSpan TotalElapsed { get; }

    /// <summary>
    /// Gets the number of failed decodes.
    /// </summary>
    public int FailureCount => Total - Success;

    /// <summary>
    /// Gets the average elapsed time per item.
    /// </summary>
    public TimeSpan AverageElapsed => Total > 0 ? TimeSpan.FromTicks(TotalElapsed.Ticks / Total) : TimeSpan.Zero;

    internal DecodeBatchDiagnostics(DecodeBatchCounts counts, TimeSpan totalElapsed) {
        Total = counts.Total;
        Success = counts.Success;
        InvalidInput = counts.InvalidInput;
        UnsupportedFormat = counts.UnsupportedFormat;
        Cancelled = counts.Cancelled;
        NoResult = counts.NoResult;
        Error = counts.Error;
        TotalElapsed = totalElapsed;
    }
}

internal readonly struct DecodeBatchCounts {
    public int Total { get; init; }
    public int Success { get; init; }
    public int InvalidInput { get; init; }
    public int UnsupportedFormat { get; init; }
    public int Cancelled { get; init; }
    public int NoResult { get; init; }
    public int Error { get; init; }
}

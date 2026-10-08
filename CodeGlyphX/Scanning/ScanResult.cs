using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CodeGlyphX;

/// <summary>
/// Result of a unified symbol scan.
/// </summary>
public sealed class ScanResult {
    /// <summary>Gets the scan outcome.</summary>
    public ScanStatus Status { get; }
    /// <summary>Gets decoded symbols.</summary>
    public IReadOnlyList<DetectedSymbol> Symbols { get; }
    /// <summary>Gets requested formats that do not currently support image scanning.</summary>
    public IReadOnlyList<SymbolFormat> UnsupportedFormats { get; }
    /// <summary>Gets total elapsed wall-clock time, including compressed-image decoding.</summary>
    public TimeSpan Elapsed { get; }
    /// <summary>Gets an optional failure description.</summary>
    public string? Failure { get; }
    /// <summary>Gets whether the scan returned at least one symbol.</summary>
    public bool IsSuccess => Status == ScanStatus.Success;
    /// <summary>Gets why recognition stopped, including after successful partial results.</summary>
    public ScanCompletionReason CompletionReason { get; }
    /// <summary>Gets whether result capacity, cancellation, or a total or family recognition deadline prevented full recognition.</summary>
    public bool IsPartial => CompletionReason != ScanCompletionReason.Completed;

    internal ScanResult(
        ScanStatus status,
        IList<DetectedSymbol> symbols,
        IList<SymbolFormat> unsupportedFormats,
        TimeSpan elapsed,
        string? failure = null,
        ScanCompletionReason completionReason = ScanCompletionReason.Completed) {
        Status = status;
        Symbols = new ReadOnlyCollection<DetectedSymbol>(new List<DetectedSymbol>(symbols ?? throw new ArgumentNullException(nameof(symbols))));
        UnsupportedFormats = new ReadOnlyCollection<SymbolFormat>(new List<SymbolFormat>(unsupportedFormats ?? throw new ArgumentNullException(nameof(unsupportedFormats))));
        Elapsed = elapsed;
        Failure = failure;
        CompletionReason = completionReason;
    }
}

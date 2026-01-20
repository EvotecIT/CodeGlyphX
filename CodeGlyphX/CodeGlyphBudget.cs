using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>
/// Provides a temporary decode time budget for operations that honor it.
/// </summary>
public static class CodeGlyphBudget {
    /// <summary>
    /// Starts a decode time budget scope. Dispose the returned handle to restore the previous budget.
    /// </summary>
    public static IDisposable? Begin(int maxMilliseconds) {
        return DecodeBudget.Begin(maxMilliseconds);
    }
}

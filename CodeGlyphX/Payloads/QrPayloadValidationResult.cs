using System.Collections.Generic;

namespace CodeGlyphX.Payloads;

/// <summary>
/// Validation result for parsed QR payloads.
/// </summary>
public sealed class QrPayloadValidationResult {
    private readonly List<string> _errors = new();

    /// <summary>
    /// Gets validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// Returns true when there are no validation errors.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    internal void Add(string message) {
        _errors.Add(message);
    }
}

using System;

namespace CodeGlyphX.Gs1Data;

/// <summary>Describes one actionable problem in a GS1 message.</summary>
public sealed class Gs1ValidationIssue {
    /// <summary>Gets the issue classification.</summary>
    public Gs1ValidationIssueCode Code { get; }

    /// <summary>Gets the affected Application Identifier, when known.</summary>
    public string? Ai { get; }

    /// <summary>Gets the zero-based position within the original input, or -1 when not applicable.</summary>
    public int Position { get; }

    /// <summary>Gets the human-readable diagnostic.</summary>
    public string Message { get; }

    internal Gs1ValidationIssue(Gs1ValidationIssueCode code, string? ai, int position, string message) {
        Code = code;
        Ai = ai;
        Position = position;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <inheritdoc />
    public override string ToString() => Ai is null ? Message : $"AI ({Ai}): {Message}";
}

using System;

namespace CodeMatrix;

/// <summary>
/// Parsed <c>otpauth://</c> payload with warnings.
/// </summary>
public sealed class OtpAuthParseResult {
    /// <summary>
    /// Gets the parsed payload.
    /// </summary>
    public OtpAuthPayload Payload { get; }
    /// <summary>
    /// Gets the issuer extracted from the label (may be empty).
    /// </summary>
    public string LabelIssuer { get; }
    /// <summary>
    /// Gets the issuer from the query parameter (may be empty).
    /// </summary>
    public string ParamIssuer { get; }
    /// <summary>
    /// Gets any warnings or normalization notes.
    /// </summary>
    public string[] Warnings { get; }

    internal OtpAuthParseResult(OtpAuthPayload payload, string labelIssuer, string paramIssuer, string[] warnings) {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        LabelIssuer = labelIssuer ?? string.Empty;
        ParamIssuer = paramIssuer ?? string.Empty;
        Warnings = warnings ?? Array.Empty<string>();
    }
}

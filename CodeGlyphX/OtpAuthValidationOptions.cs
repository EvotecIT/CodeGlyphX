namespace CodeGlyphX;

/// <summary>
/// Validation options for <c>otpauth://</c> payloads.
/// </summary>
public sealed class OtpAuthValidationOptions {
    /// <summary>
    /// Requires issuer to be present.
    /// </summary>
    public bool RequireIssuer { get; set; } = true;
    /// <summary>
    /// Requires issuer to match between label and query when both are present.
    /// </summary>
    public bool RequireIssuerMatch { get; set; } = true;
    /// <summary>
    /// Requires digits to be 6 or 8.
    /// </summary>
    public bool RequireStandardDigits { get; set; } = true;
    /// <summary>
    /// Requires TOTP period to be 30 seconds.
    /// </summary>
    public bool RequireDefaultPeriod { get; set; } = true;
    /// <summary>
    /// Requires SHA1 algorithm.
    /// </summary>
    public bool RequireSha1 { get; set; } = true;
    /// <summary>
    /// Requires a minimum secret length in bytes.
    /// </summary>
    public int MinSecretBytes { get; set; } = 10;
    /// <summary>
    /// Allows unknown query parameters.
    /// </summary>
    public bool AllowUnknownParameters { get; set; } = true;

    /// <summary>
    /// Strict defaults for maximum compatibility.
    /// </summary>
    public static OtpAuthValidationOptions Strict => new();
}

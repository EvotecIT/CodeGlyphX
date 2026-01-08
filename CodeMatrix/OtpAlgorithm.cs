namespace CodeGlyphX;

/// <summary>
/// Hash algorithm identifiers used in <c>otpauth://</c> URIs.
/// </summary>
public enum OtpAlgorithm {
    /// <summary>
    /// HMAC-SHA1 (default for TOTP).
    /// </summary>
    Sha1,
    /// <summary>
    /// HMAC-SHA256.
    /// </summary>
    Sha256,
    /// <summary>
    /// HMAC-SHA512.
    /// </summary>
    Sha512,
}

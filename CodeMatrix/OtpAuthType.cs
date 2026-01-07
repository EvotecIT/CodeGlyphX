namespace CodeMatrix;

/// <summary>
/// OTP token type for <c>otpauth://</c> URIs.
/// </summary>
public enum OtpAuthType {
    /// <summary>
    /// Time-based one-time password.
    /// </summary>
    Totp,
    /// <summary>
    /// Counter-based one-time password.
    /// </summary>
    Hotp,
}

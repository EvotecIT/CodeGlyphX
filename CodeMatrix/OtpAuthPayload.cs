using System;

namespace CodeMatrix;

/// <summary>
/// Parsed contents of an <c>otpauth://</c> URI.
/// </summary>
public sealed class OtpAuthPayload {
    /// <summary>
    /// Gets the OTP type.
    /// </summary>
    public OtpAuthType Type { get; }
    /// <summary>
    /// Gets the issuer (may be empty).
    /// </summary>
    public string Issuer { get; }
    /// <summary>
    /// Gets the account label.
    /// </summary>
    public string Account { get; }
    /// <summary>
    /// Gets the decoded secret bytes.
    /// </summary>
    public byte[] Secret { get; }
    /// <summary>
    /// Gets the HMAC algorithm.
    /// </summary>
    public OtpAlgorithm Algorithm { get; }
    /// <summary>
    /// Gets the number of digits.
    /// </summary>
    public int Digits { get; }
    /// <summary>
    /// Gets the TOTP period in seconds (null for HOTP).
    /// </summary>
    public int? Period { get; }
    /// <summary>
    /// Gets the HOTP counter (null for TOTP).
    /// </summary>
    public long? Counter { get; }

    /// <summary>
    /// Gets whether this payload is TOTP.
    /// </summary>
    public bool IsTotp => Type == OtpAuthType.Totp;
    /// <summary>
    /// Gets whether this payload is HOTP.
    /// </summary>
    public bool IsHotp => Type == OtpAuthType.Hotp;

    internal OtpAuthPayload(
        OtpAuthType type,
        string issuer,
        string account,
        byte[] secret,
        OtpAlgorithm algorithm,
        int digits,
        int? period,
        long? counter) {
        Type = type;
        Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        Account = account ?? throw new ArgumentNullException(nameof(account));
        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        Algorithm = algorithm;
        Digits = digits;
        Period = period;
        Counter = counter;
    }
}

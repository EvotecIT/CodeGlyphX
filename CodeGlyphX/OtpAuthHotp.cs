using System;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>
/// Builder for deterministic <c>otpauth://hotp/</c> URIs.
/// </summary>
/// <remarks>
/// Output is stable (parameter ordering is fixed) and uses percent-encoding for label/query parts.
/// </remarks>
public static class OtpAuthHotp {
#if NET8_0_OR_GREATER
    /// <summary>
    /// Builds an <c>otpauth://hotp/</c> URI for a HOTP token.
    /// </summary>
    public static string Create(
        string issuer,
        string account,
        ReadOnlySpan<byte> secret,
        long counter,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6) {
        return CreateInternal(issuer, account, secret.ToArray(), counter, alg, digits);
    }
#endif

    /// <summary>
    /// Builds an <c>otpauth://hotp/</c> URI for a HOTP token.
    /// </summary>
    public static string Create(
        string issuer,
        string account,
        byte[] secret,
        long counter,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6) {
        if (secret is null) throw new ArgumentNullException(nameof(secret));
        return CreateInternal(issuer, account, secret, counter, alg, digits);
    }

    private static string CreateInternal(
        string issuer,
        string account,
        byte[] secret,
        long counter,
        OtpAlgorithm alg,
        int digits) {
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));
        if (account is null) throw new ArgumentNullException(nameof(account));
        if (secret.Length == 0) throw new ArgumentException("Secret cannot be empty.", nameof(secret));
        if (counter < 0) throw new ArgumentOutOfRangeException(nameof(counter));
        if (digits is <= 0 or > 10) throw new ArgumentOutOfRangeException(nameof(digits));

        issuer = issuer.Trim();
        account = account.Trim();
        if (account.Length == 0) throw new ArgumentException("Account cannot be empty.", nameof(account));

        var secretB32 = OtpAuthSecret.ToBase32(secret);

        var sb = new StringBuilder();
        sb.Append("otpauth://hotp/");

        if (issuer.Length != 0) {
            PercentEncoding.AppendEscaped(sb, issuer);
            sb.Append(':');
        }
        PercentEncoding.AppendEscaped(sb, account);

        sb.Append("?secret=");
        PercentEncoding.AppendEscaped(sb, secretB32);

        if (issuer.Length != 0) {
            sb.Append("&issuer=");
            PercentEncoding.AppendEscaped(sb, issuer);
        }

        if (alg != OtpAlgorithm.Sha1) {
            sb.Append("&algorithm=");
            sb.Append(alg switch {
                OtpAlgorithm.Sha256 => "SHA256",
                OtpAlgorithm.Sha512 => "SHA512",
                _ => "SHA1",
            });
        }

        if (digits != 6) {
            sb.Append("&digits=");
            sb.Append(digits);
        }

        sb.Append("&counter=");
        sb.Append(counter);

        return sb.ToString();
    }
}

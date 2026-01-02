using System;
using System.Text;
using CodeMatrix.Internal;

namespace CodeMatrix;

/// <summary>
/// Builder for deterministic <c>otpauth://totp/</c> URIs.
/// </summary>
/// <remarks>
/// Output is stable (parameter ordering is fixed) and uses percent-encoding for label/query parts.
/// </remarks>
public static class OtpAuthTotp {
#if NET8_0_OR_GREATER
    /// <summary>
    /// Builds an <c>otpauth://totp/</c> URI for a TOTP token.
    /// </summary>
    public static string Create(
        string issuer,
        string account,
        ReadOnlySpan<byte> secret,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6,
        int period = 30) {
        return CreateInternal(issuer, account, secret.ToArray(), alg, digits, period);
    }
#endif

    /// <summary>
    /// Builds an <c>otpauth://totp/</c> URI for a TOTP token.
    /// </summary>
    public static string Create(
        string issuer,
        string account,
        byte[] secret,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6,
        int period = 30) {
        if (secret is null) throw new ArgumentNullException(nameof(secret));
        return CreateInternal(issuer, account, secret, alg, digits, period);
    }

    private static string CreateInternal(
        string issuer,
        string account,
        byte[] secret,
        OtpAlgorithm alg,
        int digits,
        int period) {
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));
        if (account is null) throw new ArgumentNullException(nameof(account));
        if (secret.Length == 0) throw new ArgumentException("Secret cannot be empty.", nameof(secret));
        if (digits is <= 0 or > 10) throw new ArgumentOutOfRangeException(nameof(digits));
        if (period is <= 0 or > 3600) throw new ArgumentOutOfRangeException(nameof(period));

        issuer = issuer.Trim();
        account = account.Trim();
        if (account.Length == 0) throw new ArgumentException("Account cannot be empty.", nameof(account));

        var secretB32 = OtpAuthSecret.ToBase32(secret);

        var sb = new StringBuilder();
        sb.Append("otpauth://totp/");

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

        if (period != 30) {
            sb.Append("&period=");
            sb.Append(period);
        }

        return sb.ToString();
    }
}

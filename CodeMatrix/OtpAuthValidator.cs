using System;

namespace CodeMatrix;

/// <summary>
/// Validation helpers for <c>otpauth://</c> payloads.
/// </summary>
public static class OtpAuthValidator {
    /// <summary>
    /// Validates a detailed OTP parse result.
    /// </summary>
    public static bool TryValidate(OtpAuthParseResult result, OtpAuthValidationOptions options, out string error) {
        if (!TryValidate(result, options, out string[] errors)) {
            error = errors[0];
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates a detailed OTP parse result, returning all errors.
    /// </summary>
    public static bool TryValidate(OtpAuthParseResult result, OtpAuthValidationOptions options, out string[] errors) {
        if (result is null) throw new ArgumentNullException(nameof(result));
        if (options is null) throw new ArgumentNullException(nameof(options));

        var list = new System.Collections.Generic.List<string>(6);
        var payload = result.Payload;

        if (options.RequireIssuer && string.IsNullOrEmpty(payload.Issuer)) {
            list.Add("Issuer is required.");
        }

        if (options.RequireIssuerMatch &&
            !string.IsNullOrEmpty(result.LabelIssuer) &&
            !string.IsNullOrEmpty(result.ParamIssuer) &&
            !string.Equals(result.LabelIssuer, result.ParamIssuer, StringComparison.Ordinal)) {
            list.Add("Issuer mismatch between label and query.");
        }

        if (options.RequireStandardDigits && payload.Digits is not (6 or 8)) {
            list.Add("Digits must be 6 or 8.");
        }

        if (options.RequireDefaultPeriod && payload.IsTotp && payload.Period != 30) {
            list.Add("TOTP period must be 30 seconds.");
        }

        if (options.RequireSha1 && payload.Algorithm != OtpAlgorithm.Sha1) {
            list.Add("Algorithm must be SHA1.");
        }

        if (payload.Secret.Length < options.MinSecretBytes) {
            list.Add($"Secret must be at least {options.MinSecretBytes} bytes.");
        }

        if (!options.AllowUnknownParameters) {
            for (var i = 0; i < result.Warnings.Length; i++) {
                if (result.Warnings[i].StartsWith("Unknown parameter", StringComparison.Ordinal)) {
                    list.Add("Unknown query parameters are not allowed.");
                    break;
                }
            }
        }

        errors = list.ToArray();
        return errors.Length == 0;
    }
}

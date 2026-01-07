using System;
using System.Globalization;
using CodeMatrix.Internal;

namespace CodeMatrix;

/// <summary>
/// Parser for <c>otpauth://</c> URIs.
/// </summary>
public static class OtpAuthParser {
    private const string SchemePrefix = "otpauth://";

    /// <summary>
    /// Parses an <c>otpauth://</c> URI or throws <see cref="FormatException"/>.
    /// </summary>
    public static OtpAuthPayload Parse(string uri) {
        if (!TryParse(uri, out var payload, out var error)) {
            throw new FormatException(error);
        }
        return payload;
    }

    /// <summary>
    /// Attempts to parse an <c>otpauth://</c> URI.
    /// </summary>
    public static bool TryParse(string uri, out OtpAuthPayload payload) {
        return TryParse(uri, out payload, out _);
    }

    /// <summary>
    /// Attempts to parse an <c>otpauth://</c> URI, returning a failure reason on error.
    /// </summary>
    public static bool TryParse(string uri, out OtpAuthPayload payload, out string error) {
        return TryParseCore(uri, includeWarnings: false, out payload, out _, out error);
    }

    /// <summary>
    /// Parses an <c>otpauth://</c> URI and returns warnings.
    /// </summary>
    public static OtpAuthParseResult ParseDetailed(string uri) {
        if (!TryParseDetailed(uri, out var result, out var error)) {
            throw new FormatException(error);
        }
        return result;
    }

    /// <summary>
    /// Attempts to parse an <c>otpauth://</c> URI and returns warnings.
    /// </summary>
    public static bool TryParseDetailed(string uri, out OtpAuthParseResult result, out string error) {
        if (TryParseCore(uri, includeWarnings: true, out _, out var detailed, out error) && detailed is not null) {
            result = detailed;
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseCore(
        string uri,
        bool includeWarnings,
        out OtpAuthPayload payload,
        out OtpAuthParseResult? detailed,
        out string error) {
        payload = null!;
        detailed = null;
        error = string.Empty;

        if (uri is null) {
            error = "URI is null.";
            return false;
        }

        if (!uri.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase)) {
            error = "URI does not start with otpauth://.";
            return false;
        }

        var rest = uri.Substring(SchemePrefix.Length);
        var slash = rest.IndexOf('/');
        if (slash <= 0 || slash == rest.Length - 1) {
            error = "URI is missing the type or label.";
            return false;
        }

        var typeText = rest.Substring(0, slash);
        if (!TryParseType(typeText, out var type)) {
            error = "Unsupported OTP type.";
            return false;
        }

        var pathAndQuery = rest.Substring(slash + 1);
        var qIndex = pathAndQuery.IndexOf('?');
        var labelPart = qIndex >= 0 ? pathAndQuery.Substring(0, qIndex) : pathAndQuery;
        var queryPart = qIndex >= 0 ? pathAndQuery.Substring(qIndex + 1) : string.Empty;

        if (labelPart.Length == 0) {
            error = "URI label is empty.";
            return false;
        }

        if (!PercentEncoding.TryDecode(labelPart, out var label)) {
            error = "Invalid percent-encoding in label.";
            return false;
        }

        var issuerFromLabel = string.Empty;
        var account = label;
        var colon = label.IndexOf(':');
        if (colon >= 0) {
            issuerFromLabel = label.Substring(0, colon);
            account = label.Substring(colon + 1);
        }

        if (account.Length == 0) {
            error = "Account is empty.";
            return false;
        }

        string secretText = string.Empty;
        string issuerParam = string.Empty;
        string algorithmParam = string.Empty;
        string digitsParam = string.Empty;
        string periodParam = string.Empty;
        string counterParam = string.Empty;
        var warnings = includeWarnings ? new System.Collections.Generic.List<string>(6) : null;
        System.Collections.Generic.HashSet<string>? unknownParams = includeWarnings ? new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) : null;

        if (queryPart.Length != 0) {
            var parts = queryPart.Split('&');
            for (var i = 0; i < parts.Length; i++) {
                var part = parts[i];
                if (part.Length == 0) continue;

                var eq = part.IndexOf('=');
                var name = eq >= 0 ? part.Substring(0, eq) : part;
                var value = eq >= 0 ? part.Substring(eq + 1) : string.Empty;

                if (!PercentEncoding.TryDecode(name, out var decodedName) ||
                    !PercentEncoding.TryDecode(value, out var decodedValue)) {
                    error = "Invalid percent-encoding in query.";
                    return false;
                }

                if (decodedName.Equals("secret", StringComparison.OrdinalIgnoreCase)) {
                    secretText = decodedValue;
                } else if (decodedName.Equals("issuer", StringComparison.OrdinalIgnoreCase)) {
                    issuerParam = decodedValue;
                } else if (decodedName.Equals("algorithm", StringComparison.OrdinalIgnoreCase)) {
                    algorithmParam = decodedValue;
                } else if (decodedName.Equals("digits", StringComparison.OrdinalIgnoreCase)) {
                    digitsParam = decodedValue;
                } else if (decodedName.Equals("period", StringComparison.OrdinalIgnoreCase)) {
                    periodParam = decodedValue;
                } else if (decodedName.Equals("counter", StringComparison.OrdinalIgnoreCase)) {
                    counterParam = decodedValue;
                } else if (includeWarnings) {
                    unknownParams!.Add(decodedName);
                }
            }
        }

        if (secretText.Length == 0) {
            error = "Secret is missing.";
            return false;
        }

        byte[] secret;
        try {
            secret = OtpAuthSecret.FromBase32(secretText);
        } catch (FormatException ex) {
            error = ex.Message;
            return false;
        }

        var issuer = issuerParam.Length != 0 ? issuerParam : issuerFromLabel;

        var algorithm = OtpAlgorithm.Sha1;
        if (algorithmParam.Length != 0) {
            if (algorithmParam.Equals("SHA1", StringComparison.OrdinalIgnoreCase)) algorithm = OtpAlgorithm.Sha1;
            else if (algorithmParam.Equals("SHA256", StringComparison.OrdinalIgnoreCase)) algorithm = OtpAlgorithm.Sha256;
            else if (algorithmParam.Equals("SHA512", StringComparison.OrdinalIgnoreCase)) algorithm = OtpAlgorithm.Sha512;
            else {
                error = "Unsupported algorithm.";
                return false;
            }
        }

        var digits = 6;
        if (digitsParam.Length != 0) {
            if (!int.TryParse(digitsParam, NumberStyles.None, CultureInfo.InvariantCulture, out digits) ||
                digits is <= 0 or > 10) {
                error = "Invalid digits value.";
                return false;
            }
        }

        if (type == OtpAuthType.Totp) {
            var period = 30;
            if (periodParam.Length != 0) {
                if (!int.TryParse(periodParam, NumberStyles.None, CultureInfo.InvariantCulture, out period) ||
                    period is <= 0 or > 3600) {
                    error = "Invalid period value.";
                    return false;
                }
            }

            payload = new OtpAuthPayload(type, issuer, account, secret, algorithm, digits, period, null);
            detailed = includeWarnings ? BuildDetailed(payload, issuerFromLabel, issuerParam, warnings, unknownParams, period, counter: null, algorithm, digits, secret.Length, account, issuer) : null;
            return true;
        }

        if (counterParam.Length == 0) {
            error = "Counter is missing.";
            return false;
        }

        if (!long.TryParse(counterParam, NumberStyles.None, CultureInfo.InvariantCulture, out var counter) || counter < 0) {
            error = "Invalid counter value.";
            return false;
        }

        payload = new OtpAuthPayload(type, issuer, account, secret, algorithm, digits, null, counter);
        detailed = includeWarnings ? BuildDetailed(payload, issuerFromLabel, issuerParam, warnings, unknownParams, period: null, counter, algorithm, digits, secret.Length, account, issuer) : null;
        return true;
    }

    private static bool TryParseType(string typeText, out OtpAuthType type) {
        if (typeText.Equals("totp", StringComparison.OrdinalIgnoreCase)) {
            type = OtpAuthType.Totp;
            return true;
        }
        if (typeText.Equals("hotp", StringComparison.OrdinalIgnoreCase)) {
            type = OtpAuthType.Hotp;
            return true;
        }

        type = default;
        return false;
    }

    private static OtpAuthParseResult BuildDetailed(
        OtpAuthPayload payload,
        string issuerFromLabel,
        string issuerParam,
        System.Collections.Generic.List<string>? warnings,
        System.Collections.Generic.HashSet<string>? unknownParams,
        int? period,
        long? counter,
        OtpAlgorithm algorithm,
        int digits,
        int secretLength,
        string account,
        string issuer) {
        var notes = warnings ?? new System.Collections.Generic.List<string>(6);

        if (issuerFromLabel.Length == 0 && issuerParam.Length == 0) {
            notes.Add("Issuer is missing (label and query).");
        } else if (issuerFromLabel.Length != 0 && issuerParam.Length == 0) {
            notes.Add("Issuer missing in query; derived from label.");
        } else if (issuerFromLabel.Length == 0 && issuerParam.Length != 0) {
            notes.Add("Issuer missing in label; only query issuer is set.");
        } else if (!issuerFromLabel.Equals(issuerParam, StringComparison.Ordinal)) {
            notes.Add("Issuer mismatch between label and query.");
        }

        if (account.Length != account.Trim().Length) {
            notes.Add("Account contains leading/trailing whitespace.");
        }
        if (issuer.Length != issuer.Trim().Length) {
            notes.Add("Issuer contains leading/trailing whitespace.");
        }
        if (account.IndexOf(':') >= 0) {
            notes.Add("Account contains ':'; only the first ':' separates issuer.");
        }

        if (secretLength < 10) {
            notes.Add("Secret is short (<10 bytes); consider 80+ bits.");
        }

        if (digits is not (6 or 8)) {
            notes.Add("Digits is non-standard; 6 or 8 is most compatible.");
        }

        if (algorithm != OtpAlgorithm.Sha1) {
            notes.Add("Algorithm is non-default; some apps expect SHA1.");
        }

        if (period.HasValue && period.Value != 30) {
            notes.Add("Period is non-default; 30s is most compatible.");
        }

        if (payload.IsHotp && counter == 0) {
            notes.Add("Counter is 0; ensure this is intended.");
        }

        if (unknownParams is not null && unknownParams.Count != 0) {
            foreach (var name in unknownParams) {
                notes.Add($"Unknown parameter: {name}");
            }
        }

        return new OtpAuthParseResult(payload, issuerFromLabel, issuerParam, notes.ToArray());
    }
}

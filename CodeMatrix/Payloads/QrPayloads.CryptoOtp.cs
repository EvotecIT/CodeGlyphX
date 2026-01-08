using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CodeGlyphX.Payloads;

public static partial class QrPayloads {
    /// <summary>
    /// Builds a Bitcoin-like URI payload.
    /// </summary>
    public static QrPayloadData BitcoinLike(QrBitcoinLikeType type, string address, double? amount = null, string? label = null, string? message = null) {
        if (string.IsNullOrEmpty(address)) throw new ArgumentException("Address must not be empty.", nameof(address));
        string? encLabel = string.IsNullOrEmpty(label) ? null : Uri.EscapeDataString(label);
        string? encMessage = string.IsNullOrEmpty(message) ? null : Uri.EscapeDataString(message);
        string? encAmount = amount.HasValue ? amount.Value.ToString("#.########", CultureInfo.InvariantCulture) : null;

        var parts = new List<string>(3);
        if (!string.IsNullOrEmpty(encLabel)) parts.Add("label=" + encLabel);
        if (!string.IsNullOrEmpty(encMessage)) parts.Add("message=" + encMessage);
        if (!string.IsNullOrEmpty(encAmount)) parts.Add("amount=" + encAmount);

        var query = parts.Count > 0 ? "?" + string.Join("&", parts.ToArray()) : string.Empty;
        var payload = type.ToString().ToLowerInvariant() + ":" + address + query;
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a Monero URI payload.
    /// </summary>
    public static QrPayloadData Monero(string address, float? amount = null, string? paymentId = null, string? recipientName = null, string? description = null) {
        if (string.IsNullOrEmpty(address)) throw new ArgumentException("Address must not be empty.", nameof(address));
        if (amount.HasValue && amount <= 0f) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than 0.");

        var sb = new StringBuilder();
        sb.Append("monero://").Append(address);
        var hasQuery = !string.IsNullOrEmpty(paymentId) || !string.IsNullOrEmpty(recipientName) || !string.IsNullOrEmpty(description) || amount.HasValue;
        if (hasQuery) sb.Append('?');
        if (!string.IsNullOrEmpty(paymentId)) sb.Append("tx_payment_id=").Append(Uri.EscapeDataString(paymentId)).Append('&');
        if (!string.IsNullOrEmpty(recipientName)) sb.Append("recipient_name=").Append(Uri.EscapeDataString(recipientName)).Append('&');
        if (amount.HasValue) sb.Append("tx_amount=").Append(amount.Value.ToString(CultureInfo.InvariantCulture)).Append('&');
        if (!string.IsNullOrEmpty(description)) sb.Append("tx_description=").Append(Uri.EscapeDataString(description));
        var payload = sb.ToString().TrimEnd('&');
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a Shadowsocks URI payload.
    /// </summary>
    public static QrPayloadData ShadowSocks(string hostname, int port, string password, QrShadowSocksMethod method, string? tag = null) {
        return ShadowSocks(hostname, port, password, method, null, tag);
    }

    /// <summary>
    /// Builds a Shadowsocks URI payload with parameters.
    /// </summary>
    public static QrPayloadData ShadowSocks(string hostname, int port, string password, QrShadowSocksMethod method, Dictionary<string, string>? parameters, string? tag = null) {
        if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        var host = Uri.CheckHostName(hostname) == UriHostNameType.IPv6 ? "[" + hostname + "]" : hostname;
        var methodStr = ShadowSocksMethodToString(method);

        string payload;
        if (parameters is null || parameters.Count == 0) {
            var core = $"{methodStr}:{password}@{host}:{port}";
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(core));
            payload = "ss://" + b64 + (string.IsNullOrEmpty(tag) ? string.Empty : ("#" + tag));
        } else {
            var core = methodStr + ":" + password;
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(core)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            var paramText = string.Join("&", parameters.Select(kv => UrlEncode(kv.Key) + "=" + UrlEncode(kv.Value)).ToArray());
            payload = $"ss://{b64}@{host}:{port}/?{paramText}" + (string.IsNullOrEmpty(tag) ? string.Empty : ("#" + tag));
        }

        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds an otpauth URI payload for HOTP/TOTP.
    /// </summary>
    public static QrPayloadData OneTimePassword(
        OtpAuthType type,
        string secretBase32,
        string? label = null,
        string? issuer = null,
        OtpAlgorithm algorithm = OtpAlgorithm.Sha1,
        int digits = 6,
        int? period = 30,
        int? counter = null) {
        if (string.IsNullOrWhiteSpace(secretBase32)) throw new InvalidOperationException("Secret must be a filled out base32 encoded string");
        var secret = secretBase32.Replace(" ", "");
        var issuerEscaped = string.IsNullOrEmpty(issuer) ? null : Uri.EscapeDataString(issuer);
        var labelEscaped = string.IsNullOrEmpty(label) ? null : Uri.EscapeDataString(label);
        if (!string.IsNullOrEmpty(issuer) && issuer!.IndexOf(':') >= 0) throw new InvalidOperationException("Issuer must not have a ':'");
        if (!string.IsNullOrEmpty(label) && label!.IndexOf(':') >= 0) throw new InvalidOperationException("Label must not have a ':'");

        var sb = new StringBuilder();
        sb.Append(type == OtpAuthType.Totp ? "otpauth://totp/" : "otpauth://hotp/");
        if (!string.IsNullOrEmpty(issuerEscaped) && !string.IsNullOrEmpty(labelEscaped)) {
            sb.Append(issuerEscaped).Append(':').Append(labelEscaped);
        } else if (!string.IsNullOrEmpty(issuerEscaped)) {
            sb.Append(issuerEscaped);
        } else if (!string.IsNullOrEmpty(labelEscaped)) {
            sb.Append(labelEscaped);
        }
        sb.Append("?secret=").Append(secret);
        if (!string.IsNullOrEmpty(issuerEscaped)) sb.Append("&issuer=").Append(issuerEscaped);
        if (algorithm != OtpAlgorithm.Sha1) sb.Append("&algorithm=").Append(algorithm == OtpAlgorithm.Sha256 ? "SHA256" : "SHA512");
        if (digits != 6) sb.Append("&digits=").Append(digits);

        if (type == OtpAuthType.Hotp) {
            sb.Append("&counter=").Append(counter ?? 1);
        } else {
            if (!period.HasValue) throw new InvalidOperationException("Period must be set when using TOTP.");
            if (period.Value != 30) sb.Append("&period=").Append(period.Value);
        }

        return new QrPayloadData(sb.ToString());
    }

    private static string ShadowSocksMethodToString(QrShadowSocksMethod method) {
        return method switch {
            QrShadowSocksMethod.Chacha20IetfPoly1305 => "chacha20-ietf-poly1305",
            QrShadowSocksMethod.Aes128Gcm => "aes-128-gcm",
            QrShadowSocksMethod.Aes192Gcm => "aes-192-gcm",
            QrShadowSocksMethod.Aes256Gcm => "aes-256-gcm",
            QrShadowSocksMethod.XChacha20IetfPoly1305 => "xchacha20-ietf-poly1305",
            QrShadowSocksMethod.Aes128Cfb => "aes-128-cfb",
            QrShadowSocksMethod.Aes192Cfb => "aes-192-cfb",
            QrShadowSocksMethod.Aes256Cfb => "aes-256-cfb",
            QrShadowSocksMethod.Aes128Ctr => "aes-128-ctr",
            QrShadowSocksMethod.Aes192Ctr => "aes-192-ctr",
            QrShadowSocksMethod.Aes256Ctr => "aes-256-ctr",
            QrShadowSocksMethod.Camellia128Cfb => "camellia-128-cfb",
            QrShadowSocksMethod.Camellia192Cfb => "camellia-192-cfb",
            QrShadowSocksMethod.Camellia256Cfb => "camellia-256-cfb",
            QrShadowSocksMethod.Chacha20Ietf => "chacha20-ietf",
            QrShadowSocksMethod.Aes256Cb => "aes-256-cfb",
            QrShadowSocksMethod.Aes128Ofb => "aes-128-ofb",
            QrShadowSocksMethod.Aes192Ofb => "aes-192-ofb",
            QrShadowSocksMethod.Aes256Ofb => "aes-256-ofb",
            QrShadowSocksMethod.Aes128Cfb1 => "aes-128-cfb1",
            QrShadowSocksMethod.Aes192Cfb1 => "aes-192-cfb1",
            QrShadowSocksMethod.Aes256Cfb1 => "aes-256-cfb1",
            QrShadowSocksMethod.Aes128Cfb8 => "aes-128-cfb8",
            QrShadowSocksMethod.Aes192Cfb8 => "aes-192-cfb8",
            QrShadowSocksMethod.Aes256Cfb8 => "aes-256-cfb8",
            QrShadowSocksMethod.Chacha20 => "chacha20",
            QrShadowSocksMethod.BfCfb => "bf-cfb",
            QrShadowSocksMethod.Rc4Md5 => "rc4-md5",
            QrShadowSocksMethod.Salsa20 => "salsa20",
            QrShadowSocksMethod.DesCfb => "des-cfb",
            QrShadowSocksMethod.IdeaCfb => "idea-cfb",
            QrShadowSocksMethod.Rc2Cfb => "rc2-cfb",
            QrShadowSocksMethod.Cast5Cfb => "cast5-cfb",
            QrShadowSocksMethod.Salsa20Ctr => "salsa20-ctr",
            QrShadowSocksMethod.Rc4 => "rc4",
            QrShadowSocksMethod.SeedCfb => "seed-cfb",
            QrShadowSocksMethod.Table => "table",
            _ => "aes-256-gcm"
        };
    }

    private static string UrlEncode(string value) {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var c in value) {
            sb.Append(c switch {
                ' ' => "+",
                '\0' => "%00",
                '\t' => "%09",
                '\n' => "%0a",
                '\r' => "%0d",
                '\"' => "%22",
                '#' => "%23",
                '$' => "%24",
                '%' => "%25",
                '&' => "%26",
                '\'' => "%27",
                '+' => "%2b",
                ',' => "%2c",
                '/' => "%2f",
                ':' => "%3a",
                ';' => "%3b",
                '<' => "%3c",
                '=' => "%3d",
                '>' => "%3e",
                '?' => "%3f",
                '@' => "%40",
                '[' => "%5b",
                '\\' => "%5c",
                ']' => "%5d",
                '^' => "%5e",
                '`' => "%60",
                '{' => "%7b",
                '|' => "%7c",
                '}' => "%7d",
                '~' => "%7e",
                _ => c.ToString()
            });
        }
        return sb.ToString();
    }
}

using System;
using System.Collections.Generic;

namespace CodeGlyphX.Payloads;

/// <summary>
/// Parsed payload data records.
/// </summary>
public static class QrParsedData {
    /// <summary>WiFi payload.</summary>
    public sealed record Wifi(string Ssid, string? Password, string AuthType, bool Hidden);

    /// <summary>Email payload.</summary>
    public sealed record Email(string Address, string? Subject, string? Body);

    /// <summary>Phone payload.</summary>
    public sealed record Phone(string Number);

    /// <summary>SMS payload.</summary>
    public sealed record Sms(string Number, string? Body);

    /// <summary>UPI payment payload.</summary>
    public sealed record Upi(string Vpa, string? Name, string? MerchantCode, string? TransactionRef, string? TransactionNote, decimal? Amount, string? Currency);

    /// <summary>Geo payload.</summary>
    public sealed record Geo(double Latitude, double Longitude, double? Altitude);

    /// <summary>MeCard payload.</summary>
    public sealed record MeCard(string FirstName, string LastName, string? Phone, string? Email, string? Url, string? Address, string? Note, string? Organization);

    /// <summary>vCard payload.</summary>
    public sealed record VCard(string Raw, string? FullName, string? Organization, string? Email, string? Phone);

    /// <summary>Calendar payload (VEVENT/VCALENDAR).</summary>
    public sealed record Calendar(string Raw, string? Summary, DateTime? Start, DateTime? End);

    /// <summary>OTP payload (otpauth://).</summary>
    public sealed record Otp(OtpAuthPayload Payload);

    /// <summary>App Store payload.</summary>
    public sealed record AppStore(string Raw, string? Platform, string? IdOrPackage);

    /// <summary>Social profile payload.</summary>
    public sealed record Social(string Raw, string? Network, string? HandleOrUrl);

    /// <summary>Bookmark payload.</summary>
    public sealed record Bookmark(string Url, string? Title);

    /// <summary>PayPal payment payload (PayPal.Me).</summary>
    public sealed record PayPal(string Handle, decimal? Amount, string? Currency, string Url);

    /// <summary>Crypto/payment URI payload (bitcoin:, monero:, etc.).</summary>
    public sealed record Crypto(string Scheme, string Address, decimal? Amount, string? Label, string? Message, IReadOnlyDictionary<string, string> Parameters);

    /// <summary>EMVCo merchant-presented payment payload.</summary>
    public sealed record EmvCoMerchant(
        string PayloadFormatIndicator,
        string? MerchantCategoryCode,
        string? TransactionCurrency,
        decimal? TransactionAmount,
        string? CountryCode,
        string? MerchantName,
        string? MerchantCity,
        IReadOnlyDictionary<string, string> Fields,
        bool CrcValid);

    /// <summary>Lightning payment invoice (BOLT11/12).</summary>
    public sealed record Lightning(string Invoice, string? NetworkPrefix);

    /// <summary>EIP-681 Ethereum payment request.</summary>
    public sealed record Eip681(
        string Address,
        long? ChainId,
        decimal? AmountEther,
        decimal? ValueWei,
        string? Function,
        IReadOnlyDictionary<string, string> Parameters);
}

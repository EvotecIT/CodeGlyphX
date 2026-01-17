using System;

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
}

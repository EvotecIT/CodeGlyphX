namespace CodeGlyphX.Payloads;

/// <summary>
/// Known QR payload kinds.
/// </summary>
public enum QrPayloadType {
    /// <summary>Unknown or plain text.</summary>
    Text,
    /// <summary>URL payload.</summary>
    Url,
    /// <summary>Email payload.</summary>
    Email,
    /// <summary>Phone payload.</summary>
    Phone,
    /// <summary>SMS payload.</summary>
    Sms,
    /// <summary>WiFi payload.</summary>
    Wifi,
    /// <summary>UPI payload.</summary>
    Upi,
    /// <summary>Geo payload.</summary>
    Geo,
    /// <summary>MeCard payload.</summary>
    MeCard,
    /// <summary>vCard payload.</summary>
    VCard,
    /// <summary>Calendar payload.</summary>
    Calendar,
    /// <summary>OTP payload.</summary>
    Otp,
    /// <summary>App Store payload.</summary>
    AppStore,
    /// <summary>Social profile payload.</summary>
    Social
}

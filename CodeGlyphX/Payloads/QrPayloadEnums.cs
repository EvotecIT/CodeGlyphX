namespace CodeGlyphX.Payloads;

/// <summary>
/// Calendar payload output formats.
/// </summary>
public enum QrCalendarEncoding {
    /// <summary>
    /// Simple VEVENT block.
    /// </summary>
    Universal,
    /// <summary>
    /// Full VCALENDAR wrapper with VEVENT.
    /// </summary>
    ICalComplete
}

/// <summary>
/// Contact payload output formats.
/// </summary>
public enum QrContactOutputType {
    /// <summary>
    /// MECARD format.
    /// </summary>
    MeCard,
    /// <summary>
    /// vCard 2.1 format.
    /// </summary>
    VCard21,
    /// <summary>
    /// vCard 3.0 format.
    /// </summary>
    VCard3,
    /// <summary>
    /// vCard 4.0 format.
    /// </summary>
    VCard4
}

/// <summary>
/// Address field ordering in contact payloads.
/// </summary>
public enum QrContactAddressOrder {
    /// <summary>
    /// Default order (street then house number).
    /// </summary>
    Default,
    /// <summary>
    /// Reversed order (house number then street).
    /// </summary>
    Reversed
}

/// <summary>
/// Contact address type.
/// </summary>
public enum QrContactAddressType {
    /// <summary>
    /// Home address.
    /// </summary>
    Home,
    /// <summary>
    /// Work address.
    /// </summary>
    Work,
    /// <summary>
    /// Home address, preferred.
    /// </summary>
    HomePreferred,
    /// <summary>
    /// Work address, preferred.
    /// </summary>
    WorkPreferred
}

/// <summary>
/// Geolocation payload formats.
/// </summary>
public enum QrGeolocationEncoding {
    /// <summary>
    /// geo: URI.
    /// </summary>
    Geo,
    /// <summary>
    /// Google Maps URL.
    /// </summary>
    GoogleMaps
}

/// <summary>
/// Email payload formats.
/// </summary>
public enum QrMailEncoding {
    /// <summary>
    /// mailto: URI.
    /// </summary>
    Mailto,
    /// <summary>
    /// MATMSG format.
    /// </summary>
    Matmsg,
    /// <summary>
    /// SMTP format.
    /// </summary>
    Smtp
}

/// <summary>
/// MMS payload formats.
/// </summary>
public enum QrMmsEncoding {
    /// <summary>
    /// mms: URI.
    /// </summary>
    Mms,
    /// <summary>
    /// mmsto: URI.
    /// </summary>
    Mmsto
}

/// <summary>
/// SMS payload formats.
/// </summary>
public enum QrSmsEncoding {
    /// <summary>
    /// sms: URI with body query.
    /// </summary>
    Sms,
    /// <summary>
    /// SMSTO format.
    /// </summary>
    Smsto,
    /// <summary>
    /// iOS sms: with body parameter.
    /// </summary>
    SmsIos
}

/// <summary>
/// Bitcoin-like URI schemes.
/// </summary>
public enum QrBitcoinLikeType {
    /// <summary>
    /// bitcoin: URI.
    /// </summary>
    Bitcoin,
    /// <summary>
    /// bitcoincash: URI.
    /// </summary>
    BitcoinCash,
    /// <summary>
    /// litecoin: URI.
    /// </summary>
    Litecoin
}

/// <summary>
/// Girocode versions.
/// </summary>
public enum QrGirocodeVersion {
    /// <summary>
    /// Version 1 (legacy).
    /// </summary>
    Version1,
    /// <summary>
    /// Version 2.
    /// </summary>
    Version2
}

/// <summary>
/// Girocode remittance types.
/// </summary>
public enum QrGirocodeRemittanceType {
    /// <summary>
    /// Structured reference.
    /// </summary>
    Structured,
    /// <summary>
    /// Unstructured reference.
    /// </summary>
    Unstructured
}

/// <summary>
/// Girocode payload encodings.
/// </summary>
public enum QrGirocodeEncoding {
    /// <summary>
    /// UTF-8.
    /// </summary>
    Utf8,
    /// <summary>
    /// ISO-8859-1.
    /// </summary>
    Iso8859_1,
    /// <summary>
    /// ISO-8859-2.
    /// </summary>
    Iso8859_2,
    /// <summary>
    /// ISO-8859-4.
    /// </summary>
    Iso8859_4,
    /// <summary>
    /// ISO-8859-5.
    /// </summary>
    Iso8859_5,
    /// <summary>
    /// ISO-8859-7.
    /// </summary>
    Iso8859_7,
    /// <summary>
    /// ISO-8859-10.
    /// </summary>
    Iso8859_10,
    /// <summary>
    /// ISO-8859-15.
    /// </summary>
    Iso8859_15
}

/// <summary>
/// BezahlCode authority types.
/// </summary>
public enum QrBezahlAuthorityType {
    /// <summary>
    /// Single payment.
    /// </summary>
    SinglePayment,
    /// <summary>
    /// Single SEPA payment.
    /// </summary>
    SinglePaymentSepa,
    /// <summary>
    /// Single direct debit.
    /// </summary>
    SingleDirectDebit,
    /// <summary>
    /// Single SEPA direct debit.
    /// </summary>
    SingleDirectDebitSepa,
    /// <summary>
    /// Periodic single payment.
    /// </summary>
    PeriodicSinglePayment,
    /// <summary>
    /// Periodic SEPA single payment.
    /// </summary>
    PeriodicSinglePaymentSepa,
    /// <summary>
    /// Contact payload.
    /// </summary>
    Contact,
    /// <summary>
    /// Contact payload v2.
    /// </summary>
    ContactV2
}

/// <summary>
/// Swiss QR currency codes.
/// </summary>
public enum QrSwissCurrency {
    /// <summary>
    /// CHF.
    /// </summary>
    CHF,
    /// <summary>
    /// EUR.
    /// </summary>
    EUR
}

/// <summary>
/// Shadowsocks cipher methods.
/// </summary>
public enum QrShadowSocksMethod {
    /// <summary>
    /// chacha20-ietf-poly1305.
    /// </summary>
    Chacha20IetfPoly1305,
    /// <summary>
    /// aes-128-gcm.
    /// </summary>
    Aes128Gcm,
    /// <summary>
    /// aes-192-gcm.
    /// </summary>
    Aes192Gcm,
    /// <summary>
    /// aes-256-gcm.
    /// </summary>
    Aes256Gcm,
    /// <summary>
    /// xchacha20-ietf-poly1305.
    /// </summary>
    XChacha20IetfPoly1305,
    /// <summary>
    /// aes-128-cfb.
    /// </summary>
    Aes128Cfb,
    /// <summary>
    /// aes-192-cfb.
    /// </summary>
    Aes192Cfb,
    /// <summary>
    /// aes-256-cfb.
    /// </summary>
    Aes256Cfb,
    /// <summary>
    /// aes-128-ctr.
    /// </summary>
    Aes128Ctr,
    /// <summary>
    /// aes-192-ctr.
    /// </summary>
    Aes192Ctr,
    /// <summary>
    /// aes-256-ctr.
    /// </summary>
    Aes256Ctr,
    /// <summary>
    /// camellia-128-cfb.
    /// </summary>
    Camellia128Cfb,
    /// <summary>
    /// camellia-192-cfb.
    /// </summary>
    Camellia192Cfb,
    /// <summary>
    /// camellia-256-cfb.
    /// </summary>
    Camellia256Cfb,
    /// <summary>
    /// chacha20-ietf.
    /// </summary>
    Chacha20Ietf,
    /// <summary>
    /// aes-256-cfb (alias).
    /// </summary>
    Aes256Cb,
    /// <summary>
    /// aes-128-ofb.
    /// </summary>
    Aes128Ofb,
    /// <summary>
    /// aes-192-ofb.
    /// </summary>
    Aes192Ofb,
    /// <summary>
    /// aes-256-ofb.
    /// </summary>
    Aes256Ofb,
    /// <summary>
    /// aes-128-cfb1.
    /// </summary>
    Aes128Cfb1,
    /// <summary>
    /// aes-192-cfb1.
    /// </summary>
    Aes192Cfb1,
    /// <summary>
    /// aes-256-cfb1.
    /// </summary>
    Aes256Cfb1,
    /// <summary>
    /// aes-128-cfb8.
    /// </summary>
    Aes128Cfb8,
    /// <summary>
    /// aes-192-cfb8.
    /// </summary>
    Aes192Cfb8,
    /// <summary>
    /// aes-256-cfb8.
    /// </summary>
    Aes256Cfb8,
    /// <summary>
    /// chacha20.
    /// </summary>
    Chacha20,
    /// <summary>
    /// bf-cfb.
    /// </summary>
    BfCfb,
    /// <summary>
    /// rc4-md5.
    /// </summary>
    Rc4Md5,
    /// <summary>
    /// salsa20.
    /// </summary>
    Salsa20,
    /// <summary>
    /// des-cfb.
    /// </summary>
    DesCfb,
    /// <summary>
    /// idea-cfb.
    /// </summary>
    IdeaCfb,
    /// <summary>
    /// rc2-cfb.
    /// </summary>
    Rc2Cfb,
    /// <summary>
    /// cast5-cfb.
    /// </summary>
    Cast5Cfb,
    /// <summary>
    /// salsa20-ctr.
    /// </summary>
    Salsa20Ctr,
    /// <summary>
    /// rc4.
    /// </summary>
    Rc4,
    /// <summary>
    /// seed-cfb.
    /// </summary>
    SeedCfb,
    /// <summary>
    /// table (legacy).
    /// </summary>
    Table
}

/// <summary>
/// App store target platform.
/// </summary>
public enum QrAppStorePlatform {
    /// <summary>
    /// Apple App Store.
    /// </summary>
    Apple,
    /// <summary>
    /// Google Play.
    /// </summary>
    GooglePlay
}

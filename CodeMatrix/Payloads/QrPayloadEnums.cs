#pragma warning disable CS1591
namespace CodeMatrix.Payloads;

public enum QrCalendarEncoding {
    Universal,
    ICalComplete
}

public enum QrContactOutputType {
    MeCard,
    VCard21,
    VCard3,
    VCard4
}

public enum QrContactAddressOrder {
    Default,
    Reversed
}

public enum QrContactAddressType {
    Home,
    Work,
    HomePreferred,
    WorkPreferred
}

public enum QrGeolocationEncoding {
    Geo,
    GoogleMaps
}

public enum QrMailEncoding {
    Mailto,
    Matmsg,
    Smtp
}

public enum QrMmsEncoding {
    Mms,
    Mmsto
}

public enum QrSmsEncoding {
    Sms,
    Smsto,
    SmsIos
}

public enum QrBitcoinLikeType {
    Bitcoin,
    BitcoinCash,
    Litecoin
}

public enum QrGirocodeVersion {
    Version1,
    Version2
}

public enum QrGirocodeRemittanceType {
    Structured,
    Unstructured
}

public enum QrGirocodeEncoding {
    Utf8,
    Iso8859_1,
    Iso8859_2,
    Iso8859_4,
    Iso8859_5,
    Iso8859_7,
    Iso8859_10,
    Iso8859_15
}

public enum QrBezahlAuthorityType {
    SinglePayment,
    SinglePaymentSepa,
    SingleDirectDebit,
    SingleDirectDebitSepa,
    PeriodicSinglePayment,
    PeriodicSinglePaymentSepa,
    Contact,
    ContactV2
}

public enum QrSwissCurrency {
    CHF,
    EUR
}

public enum QrShadowSocksMethod {
    Chacha20IetfPoly1305,
    Aes128Gcm,
    Aes192Gcm,
    Aes256Gcm,
    XChacha20IetfPoly1305,
    Aes128Cfb,
    Aes192Cfb,
    Aes256Cfb,
    Aes128Ctr,
    Aes192Ctr,
    Aes256Ctr,
    Camellia128Cfb,
    Camellia192Cfb,
    Camellia256Cfb,
    Chacha20Ietf,
    Aes256Cb,
    Aes128Ofb,
    Aes192Ofb,
    Aes256Ofb,
    Aes128Cfb1,
    Aes192Cfb1,
    Aes256Cfb1,
    Aes128Cfb8,
    Aes192Cfb8,
    Aes256Cfb8,
    Chacha20,
    BfCfb,
    Rc4Md5,
    Salsa20,
    DesCfb,
    IdeaCfb,
    Rc2Cfb,
    Cast5Cfb,
    Salsa20Ctr,
    Rc4,
    SeedCfb,
    Table
}

#pragma warning restore CS1591
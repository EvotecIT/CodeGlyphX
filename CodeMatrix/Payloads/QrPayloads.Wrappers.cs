namespace CodeGlyphX.Payloads;

public static partial class QrPayloads {
    /// <summary>
    /// Builds a Slovenian UPN QR payload.
    /// </summary>
    public static QrPayloadData SlovenianUpn(SlovenianUpnQrPayload payload) {
        if (payload is null) throw new System.ArgumentNullException(nameof(payload));
        return payload.ToPayloadData();
    }

    /// <summary>
    /// Builds a Swiss QR Code payload.
    /// </summary>
    public static QrPayloadData SwissQrCode(SwissQrCodePayload payload) {
        if (payload is null) throw new System.ArgumentNullException(nameof(payload));
        return payload.ToPayloadData();
    }

    /// <summary>
    /// Builds a BezahlCode payload (contact).
    /// </summary>
    public static QrPayloadData BezahlCode(
        QrBezahlAuthorityType authority,
        string name,
        string account = "",
        string bnc = "",
        string iban = "",
        string bic = "",
        string reason = "") {
        return BezahlCodeContact(authority, name, account, bnc, iban, bic, reason);
    }
}

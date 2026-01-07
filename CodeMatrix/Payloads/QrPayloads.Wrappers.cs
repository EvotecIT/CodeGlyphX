#pragma warning disable CS1591
namespace CodeMatrix.Payloads;

public static partial class QrPayloads {
    public static QrPayloadData SlovenianUpn(SlovenianUpnQrPayload payload) {
        if (payload is null) throw new System.ArgumentNullException(nameof(payload));
        return payload.ToPayloadData();
    }

    public static QrPayloadData SwissQrCode(SwissQrCodePayload payload) {
        if (payload is null) throw new System.ArgumentNullException(nameof(payload));
        return payload.ToPayloadData();
    }

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

#pragma warning restore CS1591
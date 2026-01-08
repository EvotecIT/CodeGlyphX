using System;
using System.Globalization;

namespace CodeMatrix.Payloads;

public static partial class QrPayloads {
    /// <summary>
    /// Builds a SEPA Girocode payload.
    /// </summary>
    public static QrPayloadData Girocode(
        string iban,
        string? bic,
        string name,
        decimal amount,
        string remittanceInformation = "",
        QrGirocodeRemittanceType remittanceType = QrGirocodeRemittanceType.Unstructured,
        string purposeOfCreditTransfer = "",
        string messageToGirocodeUser = "",
        QrGirocodeVersion version = QrGirocodeVersion.Version1,
        QrGirocodeEncoding encoding = QrGirocodeEncoding.Iso8859_1) {
        if (!QrPayloadValidation.IsValidIban(iban)) {
            throw new ArgumentException("The IBAN entered isn't valid.", nameof(iban));
        }
        if (!QrPayloadValidation.IsValidBic(bic, version == QrGirocodeVersion.Version1)) {
            throw new ArgumentException("The BIC entered isn't valid.", nameof(bic));
        }
        if (string.IsNullOrEmpty(name) || name.Length > 70) {
            throw new ArgumentException("Name must be shorter than 71 chars.", nameof(name));
        }

        var amountText = amount.ToString(CultureInfo.InvariantCulture);
        if (amountText.Contains('.') && amountText.Split('.')[1].TrimEnd('0').Length > 2) {
            throw new ArgumentException("Amount must have less than 3 digits after decimal point.", nameof(amount));
        }
        if (amount < 0.01m || amount > 999999999.99m) {
            throw new ArgumentException("Amount has to be at least 0.01 and must be smaller or equal to 999999999.99.", nameof(amount));
        }
        if (purposeOfCreditTransfer.Length > 4) {
            throw new ArgumentException("Purpose of credit transfer can only have 4 chars at maximum.", nameof(purposeOfCreditTransfer));
        }
        if (remittanceType == QrGirocodeRemittanceType.Unstructured && remittanceInformation.Length > 140) {
            throw new ArgumentException("Unstructured reference texts have to be shorter than 141 chars.", nameof(remittanceInformation));
        }
        if (remittanceType == QrGirocodeRemittanceType.Structured && remittanceInformation.Length > 35) {
            throw new ArgumentException("Structured reference texts have to be shorter than 36 chars.", nameof(remittanceInformation));
        }
        if (messageToGirocodeUser.Length > 70) {
            throw new ArgumentException("Message to the Girocode-User reader texts have to be shorter than 71 chars.", nameof(messageToGirocodeUser));
        }

        var br = "\n";
        var versionCode = version == QrGirocodeVersion.Version1 ? "001" : "002";
        var encodingCode = ((int)encoding + 1).ToString(CultureInfo.InvariantCulture);
        var payload = "BCD" + br +
            versionCode + br +
            encodingCode + br +
            "SCT" + br +
            (bic ?? string.Empty).Replace(" ", "").ToUpperInvariant() + br +
            name + br +
            iban.Replace(" ", "").ToUpperInvariant() + br +
            string.Format(CultureInfo.InvariantCulture, "EUR{0:0.00}", amount) + br +
            purposeOfCreditTransfer + br +
            (remittanceType == QrGirocodeRemittanceType.Structured ? remittanceInformation : string.Empty) + br +
            (remittanceType == QrGirocodeRemittanceType.Unstructured ? remittanceInformation : string.Empty) + br +
            messageToGirocodeUser;

        return new QrPayloadData(payload, QrErrorCorrectionLevel.M, textEncoding: MapGirocodeEncoding(encoding));
    }

    /// <summary>
    /// Builds a BezahlCode contact payload.
    /// </summary>
    public static QrPayloadData BezahlCodeContact(
        QrBezahlAuthorityType authority,
        string name,
        string account = "",
        string bnc = "",
        string iban = "",
        string bic = "",
        string reason = "") {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name must not be empty.", nameof(name));

        var authorityText = MapBezahlAuthority(authority);
        if (authority != QrBezahlAuthorityType.Contact && authority != QrBezahlAuthorityType.ContactV2) {
            throw new ArgumentException("Only contact/contact_v2 authorities are supported by this overload.", nameof(authority));
        }

        var payload = "bank://" + authorityText + "?" +
            "name=" + Uri.EscapeDataString(name) + "&";

        if (authority == QrBezahlAuthorityType.Contact) {
            payload += "account=" + (account ?? string.Empty) + "&";
            payload += "bnc=" + (bnc ?? string.Empty) + "&";
        } else if (!string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(bnc)) {
            payload += "account=" + account + "&";
            payload += "bnc=" + bnc + "&";
        } else {
            payload += "iban=" + (iban ?? string.Empty) + "&";
            payload += "bic=" + (bic ?? string.Empty) + "&";
        }

        if (!string.IsNullOrEmpty(reason)) {
            payload += "reason=" + Uri.EscapeDataString(reason) + "&";
        }

        payload = payload.TrimEnd('&');
        return new QrPayloadData(payload);
    }

    private static string MapBezahlAuthority(QrBezahlAuthorityType authority) {
        return authority switch {
            QrBezahlAuthorityType.SinglePayment => "singlepayment",
            QrBezahlAuthorityType.SinglePaymentSepa => "singlepaymentsepa",
            QrBezahlAuthorityType.SingleDirectDebit => "singledirectdebit",
            QrBezahlAuthorityType.SingleDirectDebitSepa => "singledirectdebitsepa",
            QrBezahlAuthorityType.PeriodicSinglePayment => "periodicsinglepayment",
            QrBezahlAuthorityType.PeriodicSinglePaymentSepa => "periodicsinglepaymentsepa",
            QrBezahlAuthorityType.Contact => "contact",
            QrBezahlAuthorityType.ContactV2 => "contact_v2",
            _ => "contact"
        };
    }

    private static QrTextEncoding MapGirocodeEncoding(QrGirocodeEncoding encoding) {
        return encoding switch {
            QrGirocodeEncoding.Utf8 => QrTextEncoding.Utf8,
            QrGirocodeEncoding.Iso8859_1 => QrTextEncoding.Latin1,
            QrGirocodeEncoding.Iso8859_2 => QrTextEncoding.Iso8859_2,
            QrGirocodeEncoding.Iso8859_4 => QrTextEncoding.Iso8859_4,
            QrGirocodeEncoding.Iso8859_5 => QrTextEncoding.Iso8859_5,
            QrGirocodeEncoding.Iso8859_7 => QrTextEncoding.Iso8859_7,
            QrGirocodeEncoding.Iso8859_10 => QrTextEncoding.Iso8859_10,
            QrGirocodeEncoding.Iso8859_15 => QrTextEncoding.Iso8859_15,
            _ => QrTextEncoding.Latin1
        };
    }
}

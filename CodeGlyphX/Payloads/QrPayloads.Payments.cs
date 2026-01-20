using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeGlyphX.Payloads;

public static partial class QrPayloads {
    /// <summary>
    /// Builds a PayPal.Me payment payload.
    /// </summary>
    public static QrPayloadData PayPalMe(string handleOrUrl, decimal? amount = null, string? currency = null, bool useHttps = true) {
        var handle = NormalizePayPalHandle(handleOrUrl);
        if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("PayPal handle must not be empty.", nameof(handleOrUrl));

        if (amount.HasValue && amount.Value <= 0m) {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        var baseUrl = (useHttps ? "https://" : "http://") + "paypal.me/" + handle;
        if (!amount.HasValue) return new QrPayloadData(baseUrl);

        var amountText = amount.Value.ToString("0.##", CultureInfo.InvariantCulture);
        var currencyText = currency?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(currencyText) && !QrPayloadValidation.IsValidCurrency(currencyText)) {
            throw new ArgumentException("Currency is invalid.", nameof(currency));
        }
        var payload = baseUrl + "/" + amountText + currencyText;
        return new QrPayloadData(payload);
    }

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

    private static string NormalizePayPalHandle(string handleOrUrl) {
        if (string.IsNullOrWhiteSpace(handleOrUrl)) return string.Empty;
        var trimmed = handleOrUrl.Trim();
        if (trimmed.StartsWith("@", StringComparison.Ordinal)) trimmed = trimmed.Substring(1);

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) {
                if (IsPayPalMeHost(uri.Host)) {
                    return ExtractFirstSegment(uri.AbsolutePath);
                }
            }
            return trimmed;
        }

        if (trimmed.StartsWith("paypal.me/", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("www.paypal.me/", StringComparison.OrdinalIgnoreCase)) {
            if (Uri.TryCreate("https://" + trimmed, UriKind.Absolute, out var uri)) {
                return ExtractFirstSegment(uri.AbsolutePath);
            }
        }

        return trimmed;
    }

    private static bool IsPayPalMeHost(string host) {
        if (string.IsNullOrEmpty(host)) return false;
        if (host.Equals("paypal.me", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("www.paypal.me", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static string ExtractFirstSegment(string path) {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        var trimmed = path.Trim('/');
        if (trimmed.Length == 0) return string.Empty;
        var slash = trimmed.IndexOf('/');
        return slash < 0 ? trimmed : trimmed.Substring(0, slash);
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

    /// <summary>
    /// Builds a BezahlCode single payment payload (non-SEPA).
    /// </summary>
    public static QrPayloadData BezahlCodeSinglePayment(
        string name,
        string account,
        string bnc,
        decimal amount,
        string reason = "",
        string currency = "EUR",
        string postingKey = "",
        DateTime? executionDate = null) {
        ValidateBezahlName(name);
        if (string.IsNullOrWhiteSpace(account)) throw new ArgumentException("Account must not be empty.", nameof(account));
        if (string.IsNullOrWhiteSpace(bnc)) throw new ArgumentException("BNC must not be empty.", nameof(bnc));
        var parameters = BuildBezahlCommon(name, amount, reason, currency, postingKey, executionDate);
        parameters.Add(("account", account, true));
        parameters.Add(("bnc", bnc, true));
        return BuildBezahlPayload(QrBezahlAuthorityType.SinglePayment, parameters);
    }

    /// <summary>
    /// Builds a BezahlCode single payment payload (SEPA).
    /// </summary>
    public static QrPayloadData BezahlCodeSinglePaymentSepa(
        string name,
        string iban,
        string bic,
        decimal amount,
        string reason = "",
        string currency = "EUR",
        string sepaReference = "",
        DateTime? executionDate = null) {
        ValidateBezahlName(name);
        ValidateBezahlIbanBic(iban, bic);
        var parameters = BuildBezahlCommon(name, amount, reason, currency, string.Empty, executionDate);
        parameters.Add(("iban", iban, true));
        parameters.Add(("bic", bic, true));
        if (!string.IsNullOrEmpty(sepaReference)) parameters.Add(("separeference", sepaReference, true));
        return BuildBezahlPayload(QrBezahlAuthorityType.SinglePaymentSepa, parameters);
    }

    /// <summary>
    /// Builds a BezahlCode single direct debit payload (non-SEPA).
    /// </summary>
    public static QrPayloadData BezahlCodeSingleDirectDebit(
        string name,
        string account,
        string bnc,
        decimal amount,
        string creditorId,
        string mandateId,
        DateTime dateOfSignature,
        string reason = "",
        string currency = "EUR",
        string postingKey = "",
        DateTime? executionDate = null) {
        ValidateBezahlName(name);
        if (string.IsNullOrWhiteSpace(account)) throw new ArgumentException("Account must not be empty.", nameof(account));
        if (string.IsNullOrWhiteSpace(bnc)) throw new ArgumentException("BNC must not be empty.", nameof(bnc));
        ValidateBezahlDirectDebit(creditorId, mandateId);
        var parameters = BuildBezahlCommon(name, amount, reason, currency, postingKey, executionDate);
        parameters.Add(("account", account, true));
        parameters.Add(("bnc", bnc, true));
        parameters.Add(("creditorid", creditorId, true));
        parameters.Add(("mandateid", mandateId, true));
        parameters.Add(("dateofsignature", FormatBezahlDate(dateOfSignature), false));
        return BuildBezahlPayload(QrBezahlAuthorityType.SingleDirectDebit, parameters);
    }

    /// <summary>
    /// Builds a BezahlCode single direct debit payload (SEPA).
    /// </summary>
    public static QrPayloadData BezahlCodeSingleDirectDebitSepa(
        string name,
        string iban,
        string bic,
        decimal amount,
        string creditorId,
        string mandateId,
        DateTime dateOfSignature,
        string reason = "",
        string currency = "EUR",
        string sepaReference = "",
        DateTime? executionDate = null) {
        ValidateBezahlName(name);
        ValidateBezahlIbanBic(iban, bic);
        ValidateBezahlDirectDebit(creditorId, mandateId);
        var parameters = BuildBezahlCommon(name, amount, reason, currency, string.Empty, executionDate);
        parameters.Add(("iban", iban, true));
        parameters.Add(("bic", bic, true));
        if (!string.IsNullOrEmpty(sepaReference)) parameters.Add(("separeference", sepaReference, true));
        parameters.Add(("creditorid", creditorId, true));
        parameters.Add(("mandateid", mandateId, true));
        parameters.Add(("dateofsignature", FormatBezahlDate(dateOfSignature), false));
        return BuildBezahlPayload(QrBezahlAuthorityType.SingleDirectDebitSepa, parameters);
    }

    /// <summary>
    /// Builds a BezahlCode periodic single payment payload (non-SEPA).
    /// </summary>
    public static QrPayloadData BezahlCodePeriodicSinglePayment(
        string name,
        string account,
        string bnc,
        decimal amount,
        QrBezahlPeriodicUnit periodicUnit,
        int periodicUnitRotation,
        DateTime periodicFirstExecutionDate,
        DateTime periodicLastExecutionDate,
        string reason = "",
        string currency = "EUR",
        string postingKey = "") {
        ValidateBezahlName(name);
        if (string.IsNullOrWhiteSpace(account)) throw new ArgumentException("Account must not be empty.", nameof(account));
        if (string.IsNullOrWhiteSpace(bnc)) throw new ArgumentException("BNC must not be empty.", nameof(bnc));
        var parameters = BuildBezahlCommon(name, amount, reason, currency, postingKey, null);
        parameters.Add(("account", account, true));
        parameters.Add(("bnc", bnc, true));
        AppendBezahlPeriodic(parameters, periodicUnit, periodicUnitRotation, periodicFirstExecutionDate, periodicLastExecutionDate);
        return BuildBezahlPayload(QrBezahlAuthorityType.PeriodicSinglePayment, parameters);
    }

    /// <summary>
    /// Builds a BezahlCode periodic single payment payload (SEPA).
    /// </summary>
    public static QrPayloadData BezahlCodePeriodicSinglePaymentSepa(
        string name,
        string iban,
        string bic,
        decimal amount,
        QrBezahlPeriodicUnit periodicUnit,
        int periodicUnitRotation,
        DateTime periodicFirstExecutionDate,
        DateTime periodicLastExecutionDate,
        string reason = "",
        string currency = "EUR",
        string sepaReference = "") {
        ValidateBezahlName(name);
        ValidateBezahlIbanBic(iban, bic);
        var parameters = BuildBezahlCommon(name, amount, reason, currency, string.Empty, null);
        parameters.Add(("iban", iban, true));
        parameters.Add(("bic", bic, true));
        if (!string.IsNullOrEmpty(sepaReference)) parameters.Add(("separeference", sepaReference, true));
        AppendBezahlPeriodic(parameters, periodicUnit, periodicUnitRotation, periodicFirstExecutionDate, periodicLastExecutionDate);
        return BuildBezahlPayload(QrBezahlAuthorityType.PeriodicSinglePaymentSepa, parameters);
    }

    /// <summary>
    /// Builds a Russia payment order payload (ST00012).
    /// </summary>
    public static QrPayloadData RussiaPaymentOrder(
        string name,
        string personalAcc,
        string bankName,
        string bic,
        string correspAcc,
        string payeeInn,
        string kpp,
        decimal sum,
        string purpose) {
        var payload = new RussiaPaymentOrderPayload(
            name,
            personalAcc,
            bankName,
            bic,
            correspAcc,
            payeeInn,
            kpp,
            sum,
            purpose);
        return payload.ToPayloadData();
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

    private static List<(string key, string value, bool escape)> BuildBezahlCommon(
        string name,
        decimal amount,
        string reason,
        string currency,
        string postingKey,
        DateTime? executionDate) {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        var parameters = new List<(string key, string value, bool escape)> {
            ("name", name, true),
            ("amount", FormatBezahlAmount(amount), false)
        };
        if (!string.IsNullOrEmpty(reason)) parameters.Add(("reason", reason, true));
        if (!string.IsNullOrEmpty(currency)) parameters.Add(("currency", currency, false));
        if (!string.IsNullOrEmpty(postingKey)) parameters.Add(("postingkey", postingKey, true));
        if (executionDate.HasValue) parameters.Add(("executiondate", FormatBezahlDate(executionDate.Value), false));
        return parameters;
    }

    private static void AppendBezahlPeriodic(
        List<(string key, string value, bool escape)> parameters,
        QrBezahlPeriodicUnit periodicUnit,
        int periodicUnitRotation,
        DateTime periodicFirstExecutionDate,
        DateTime periodicLastExecutionDate) {
        if (periodicUnitRotation <= 0) throw new ArgumentOutOfRangeException(nameof(periodicUnitRotation));
        parameters.Add(("periodictimeunit", periodicUnit == QrBezahlPeriodicUnit.Monthly ? "M" : "W", false));
        parameters.Add(("periodictimeunitrotation", periodicUnitRotation.ToString(CultureInfo.InvariantCulture), false));
        parameters.Add(("periodicfirstexecutiondate", FormatBezahlDate(periodicFirstExecutionDate), false));
        parameters.Add(("periodiclastexecutiondate", FormatBezahlDate(periodicLastExecutionDate), false));
    }

    private static QrPayloadData BuildBezahlPayload(QrBezahlAuthorityType authority, List<(string key, string value, bool escape)> parameters) {
        var authorityText = MapBezahlAuthority(authority);
        var payload = "bank://" + authorityText + "?";
        for (var i = 0; i < parameters.Count; i++) {
            var entry = parameters[i];
            if (string.IsNullOrEmpty(entry.value)) continue;
            payload += entry.key + "=" + (entry.escape ? Uri.EscapeDataString(entry.value) : entry.value) + "&";
        }
        payload = payload.TrimEnd('&');
        return new QrPayloadData(payload);
    }

    private static string FormatBezahlAmount(decimal amount) {
        var text = amount.ToString("0.00", CultureInfo.InvariantCulture);
        return text.Replace('.', ',');
    }

    private static string FormatBezahlDate(DateTime date) {
        return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static void ValidateBezahlName(string name) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be empty.", nameof(name));
    }

    private static void ValidateBezahlIbanBic(string iban, string bic) {
        if (!QrPayloadValidation.IsValidIban(iban)) {
            throw new ArgumentException("The IBAN entered isn't valid.", nameof(iban));
        }
        if (!QrPayloadValidation.IsValidBic(bic)) {
            throw new ArgumentException("The BIC entered isn't valid.", nameof(bic));
        }
    }

    private static void ValidateBezahlDirectDebit(string creditorId, string mandateId) {
        if (string.IsNullOrWhiteSpace(creditorId)) throw new ArgumentException("CreditorId must not be empty.", nameof(creditorId));
        if (string.IsNullOrWhiteSpace(mandateId)) throw new ArgumentException("MandateId must not be empty.", nameof(mandateId));
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

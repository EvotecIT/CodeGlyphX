using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeGlyphX.Payloads;

/// <summary>
/// Russia payment order payload (ST00012, UTF-8).
/// </summary>
public sealed class RussiaPaymentOrderPayload {
    private readonly Dictionary<string, string> _additionalFields = new();
    private readonly QrRussiaPaymentEncoding _encoding;
    private readonly string _name;
    private readonly string _personalAcc;
    private readonly string _bankName;
    private readonly string _bic;
    private readonly string _correspAcc;
    private readonly string _payeeInn;
    private readonly string _kpp;
    private readonly long _sumKopeks;
    private readonly string _purpose;

    /// <summary>
    /// Optional payer last name.
    /// </summary>
    public string? LastName { get; set; }
    /// <summary>
    /// Optional payer first name.
    /// </summary>
    public string? FirstName { get; set; }
    /// <summary>
    /// Optional payer middle name.
    /// </summary>
    public string? MiddleName { get; set; }
    /// <summary>
    /// Optional payer address.
    /// </summary>
    public string? PayerAddress { get; set; }
    /// <summary>
    /// Optional KPP (tax registration reason code).
    /// </summary>
    public string? Kpp => _kpp;
    /// <summary>
    /// Optional OKTMO code.
    /// </summary>
    public string? Oktmo { get; set; }
    /// <summary>
    /// Optional CBC (budget classification code).
    /// </summary>
    public string? Cbc { get; set; }
    /// <summary>
    /// Optional UIN (unique accrual identifier).
    /// </summary>
    public string? Uin { get; set; }
    /// <summary>
    /// Optional AIP identifier.
    /// </summary>
    public string? Aip { get; set; }

    /// <summary>
    /// Additional custom fields appended to the payload.
    /// </summary>
    public IDictionary<string, string> AdditionalFields => _additionalFields;

    /// <summary>
    /// Creates a Russia payment order payload.
    /// </summary>
    public RussiaPaymentOrderPayload(
        string name,
        string personalAcc,
        string bankName,
        string bic,
        string correspAcc,
        string payeeInn,
        string kpp,
        decimal sum,
        string purpose,
        QrRussiaPaymentEncoding encoding = QrRussiaPaymentEncoding.Utf8) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must not be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(personalAcc)) throw new ArgumentException("PersonalAcc must not be empty.", nameof(personalAcc));
        if (string.IsNullOrWhiteSpace(bankName)) throw new ArgumentException("BankName must not be empty.", nameof(bankName));
        if (string.IsNullOrWhiteSpace(bic)) throw new ArgumentException("BIC must not be empty.", nameof(bic));
        if (string.IsNullOrWhiteSpace(correspAcc)) throw new ArgumentException("CorrespAcc must not be empty.", nameof(correspAcc));
        if (string.IsNullOrWhiteSpace(payeeInn)) throw new ArgumentException("PayeeINN must not be empty.", nameof(payeeInn));
        if (string.IsNullOrWhiteSpace(kpp)) throw new ArgumentException("KPP must not be empty.", nameof(kpp));
        if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException("Purpose must not be empty.", nameof(purpose));

        _name = name;
        _personalAcc = personalAcc;
        _bankName = bankName;
        _bic = bic;
        _correspAcc = correspAcc;
        _payeeInn = payeeInn;
        _kpp = kpp;
        _purpose = purpose;
        _encoding = encoding;
        _sumKopeks = ToKopeks(sum);
    }

    /// <summary>
    /// Returns the Russia payment order payload string.
    /// </summary>
    public string ToPayloadString() {
        var sb = new StringBuilder();
        sb.Append("ST0001").Append(MapEncoding(_encoding));

        AppendField(sb, "Name", _name);
        AppendField(sb, "PersonalAcc", _personalAcc);
        AppendField(sb, "BankName", _bankName);
        AppendField(sb, "BIC", _bic);
        AppendField(sb, "CorrespAcc", _correspAcc);
        AppendField(sb, "PayeeINN", _payeeInn);
        AppendField(sb, "KPP", _kpp);
        AppendField(sb, "CBC", Cbc);
        AppendField(sb, "OKTMO", Oktmo);
        AppendField(sb, "Sum", _sumKopeks.ToString(CultureInfo.InvariantCulture));
        AppendField(sb, "LastName", LastName);
        AppendField(sb, "FirstName", FirstName);
        AppendField(sb, "MiddleName", MiddleName);
        AppendField(sb, "PayerAddress", PayerAddress);
        AppendField(sb, "AIP", Aip);
        AppendField(sb, "UIN", Uin);
        AppendField(sb, "Purpose", _purpose);

        foreach (var kvp in _additionalFields) {
            AppendField(sb, kvp.Key, kvp.Value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts the payload to a QR payload wrapper.
    /// </summary>
    public QrPayloadData ToPayloadData() {
        return new QrPayloadData(ToPayloadString(), QrErrorCorrectionLevel.M, textEncoding: QrTextEncoding.Utf8);
    }

    private static void AppendField(StringBuilder sb, string key, string? value) {
        if (value is null || value.Length == 0) return;
        if (value.IndexOf('|') >= 0 || value.IndexOf('=') >= 0) {
            throw new ArgumentException("Field values cannot contain '|' or '='.");
        }
        sb.Append('|').Append(key).Append('=').Append(value);
    }

    private static string MapEncoding(QrRussiaPaymentEncoding encoding) {
        _ = encoding;
        return "2";
    }

    private static long ToKopeks(decimal amount) {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        var kopeks = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        return (long)kopeks;
    }
}

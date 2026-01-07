using System;
using System.Globalization;
using System.Text;
#pragma warning disable CS1591

namespace CodeMatrix.Payloads;

/// <summary>
/// Slovenian UPN QR payment payload.
/// </summary>
public sealed class SlovenianUpnQrPayload {
    private readonly string _payerName;
    private readonly string _payerAddress;
    private readonly string _payerPlace;
    private readonly string _amount;
    private readonly string _code;
    private readonly string _purpose;
    private readonly string _deadline;
    private readonly string _recipientIban;
    private readonly string _recipientName;
    private readonly string _recipientAddress;
    private readonly string _recipientPlace;
    private readonly string _recipientSiModel;
    private readonly string _recipientSiReference;

    public SlovenianUpnQrPayload(
        string payerName,
        string payerAddress,
        string payerPlace,
        string recipientName,
        string recipientAddress,
        string recipientPlace,
        string recipientIban,
        string description,
        double amount,
        DateTime? deadline = null,
        string recipientSiModel = "SI00",
        string recipientSiReference = "",
        string code = "OTHR") {
        _payerName = LimitLength((payerName ?? string.Empty).Trim(), 33);
        _payerAddress = LimitLength((payerAddress ?? string.Empty).Trim(), 33);
        _payerPlace = LimitLength((payerPlace ?? string.Empty).Trim(), 33);
        _amount = FormatAmount(amount);
        _code = LimitLength((code ?? string.Empty).Trim().ToUpperInvariant(), 4);
        _purpose = LimitLength((description ?? string.Empty).Trim(), 42);
        _deadline = deadline.HasValue ? deadline.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;
        _recipientIban = LimitLength((recipientIban ?? string.Empty).Trim(), 34);
        _recipientName = LimitLength((recipientName ?? string.Empty).Trim(), 33);
        _recipientAddress = LimitLength((recipientAddress ?? string.Empty).Trim(), 33);
        _recipientPlace = LimitLength((recipientPlace ?? string.Empty).Trim(), 33);
        _recipientSiModel = LimitLength((recipientSiModel ?? string.Empty).Trim().ToUpperInvariant(), 4);
        _recipientSiReference = LimitLength((recipientSiReference ?? string.Empty).Trim(), 22);
    }

    public QrPayloadData ToPayloadData() {
        return new QrPayloadData(ToString(), QrErrorCorrectionLevel.M, minVersion: 15, maxVersion: 15, textEncoding: QrTextEncoding.Iso8859_2);
    }

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("UPNQR");
        sb.Append('\n').Append('\n').Append('\n').Append('\n').Append('\n');
        sb.Append(_payerName).Append('\n');
        sb.Append(_payerAddress).Append('\n');
        sb.Append(_payerPlace).Append('\n');
        sb.Append(_amount).Append('\n').Append('\n').Append('\n');
        sb.Append(_code.ToUpperInvariant()).Append('\n');
        sb.Append(_purpose).Append('\n');
        sb.Append(_deadline).Append('\n');
        sb.Append(_recipientIban.ToUpperInvariant()).Append('\n');
        sb.Append(_recipientSiModel).Append(_recipientSiReference).Append('\n');
        sb.Append(_recipientName).Append('\n');
        sb.Append(_recipientAddress).Append('\n');
        sb.Append(_recipientPlace).Append('\n');
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:000}", CalculateChecksum()).Append('\n');
        return sb.ToString();
    }

    private static string LimitLength(string value, int maxLength) {
        if (value.Length > maxLength) return value.Substring(0, maxLength);
        return value;
    }

    private static string FormatAmount(double amount) {
        var num = (int)Math.Round(amount * 100.0);
        return string.Format(CultureInfo.InvariantCulture, "{0:00000000000}", num);
    }

    private int CalculateChecksum() {
        return 5 + _payerName.Length + _payerAddress.Length + _payerPlace.Length + _amount.Length + _code.Length +
               _purpose.Length + _deadline.Length + _recipientIban.Length + _recipientName.Length + _recipientAddress.Length +
               _recipientPlace.Length + _recipientSiModel.Length + _recipientSiReference.Length + 19;
    }
}

#pragma warning restore CS1591
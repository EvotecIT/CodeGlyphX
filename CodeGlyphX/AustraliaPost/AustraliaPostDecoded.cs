using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CodeGlyphX.AustraliaPost;

/// <summary>
/// Decoded Australia Post customer barcode information.
/// </summary>
public sealed class AustraliaPostDecoded {
    /// <summary>
    /// Gets the decoded barcode format.
    /// </summary>
    public AustraliaPostFormat Format { get; }

    /// <summary>
    /// Gets the format control code (FCC).
    /// </summary>
    public int FormatControlCode { get; }

    /// <summary>
    /// Gets the Delivery Point Identifier (DPID).
    /// </summary>
    public string Dpid { get; }

    /// <summary>
    /// Gets the customer information field (may be empty for Standard format).
    /// </summary>
    public string CustomerInfo { get; }

    /// <summary>
    /// Gets the customer information encoding table, if present.
    /// </summary>
    public AustraliaPostCustomerEncodingTable? CustomerInfoEncoding { get; }

    /// <summary>
    /// Gets whether the customer information table or character count remains ambiguous after application hints.
    /// </summary>
    public bool IsCustomerInfoAmbiguous { get; }

    /// <summary>
    /// Gets the concatenated payload (DPID + customer info).
    /// </summary>
    public string Value => Dpid + CustomerInfo;

    /// <summary>Gets the selected FCC, DPID and customer data, without parity or filler bars.</summary>
    public string EncodedValue => FormatControlCode.ToString("00", CultureInfo.InvariantCulture) + Value;
    /// <summary>Gets the four Reed-Solomon symbols as decimal values from zero through 63.</summary>
    public IReadOnlyList<int> ErrorCorrectionSymbols { get; }
    /// <summary>Gets valid interpretations before application table/length hints. A trailing z can also be padding.</summary>
    public IReadOnlyList<AustraliaPostCustomerInfo> CustomerInfoAlternatives { get; }
    /// <summary>Gets the postal readable line, including FCC, DPID, customer data and decimal parity values.</summary>
    public string HumanReadable => FormatControlCode.ToString("00", CultureInfo.InvariantCulture) + " " + Dpid
        + (CustomerInfo.Length == 0 ? string.Empty : " " + CustomerInfo)
        + (ErrorCorrectionSymbols.Count == 0 ? string.Empty : " " + string.Join(" ", ErrorCorrectionSymbols.Select(value => value.ToString(CultureInfo.InvariantCulture))));

    /// <summary>Applies known customer-field table/length without discarding alternative physical interpretations.</summary>
    public bool TrySelectCustomerInfo(AustraliaPostCustomerEncodingTable? table, int? length, out AustraliaPostDecoded selected) {
        selected = null!;
        if (table.HasValue && !Enum.IsDefined(typeof(AustraliaPostCustomerEncodingTable), table.Value)) return false;
        if (length.HasValue && (length < 0 || length > 15)) return false;
        if (Format == AustraliaPostFormat.Standard) {
            if (length.HasValue && length != 0) return false;
            selected = this; return true;
        }
        var choices = CustomerInfoAlternatives.Where(item => (!table.HasValue || item.EncodingTable == table)
            && (!length.HasValue || item.Text.Length == length)).ToArray();
        if (choices.Length == 0) return false;
        // Preserve the previous default table where it remains a valid application choice.
        var chosen = choices.OrderByDescending(item => item.EncodingTable == CustomerInfoEncoding).ThenByDescending(item => item.Text.Length).First();
        selected = new AustraliaPostDecoded(Format, FormatControlCode, Dpid, chosen.Text, chosen.EncodingTable,
            choices.Length > 1, ErrorCorrectionSymbols, CustomerInfoAlternatives);
        return true;
    }

    internal AustraliaPostDecoded(
        AustraliaPostFormat format,
        int formatControlCode,
        string dpid,
        string customerInfo,
        AustraliaPostCustomerEncodingTable? customerInfoEncoding,
        bool isCustomerInfoAmbiguous, IEnumerable<int>? parity = null, IEnumerable<AustraliaPostCustomerInfo>? alternatives = null) {
        Format = format;
        FormatControlCode = formatControlCode;
        Dpid = dpid;
        CustomerInfo = customerInfo;
        CustomerInfoEncoding = customerInfoEncoding;
        IsCustomerInfoAmbiguous = isCustomerInfoAmbiguous;
        ErrorCorrectionSymbols = Array.AsReadOnly(parity?.ToArray() ?? Array.Empty<int>());
        CustomerInfoAlternatives = Array.AsReadOnly(alternatives?.ToArray() ?? Array.Empty<AustraliaPostCustomerInfo>());
    }
}

/// <summary>One valid interpretation of an Australia Post customer-information field.</summary>
public sealed class AustraliaPostCustomerInfo {
    /// <summary>Gets the customer text before filler bars.</summary>
    public string Text { get; }
    /// <summary>Gets the character or numeric table used for this interpretation.</summary>
    public AustraliaPostCustomerEncodingTable EncodingTable { get; }
    internal AustraliaPostCustomerInfo(string text, AustraliaPostCustomerEncodingTable table) { Text = text; EncodingTable = table; }
}

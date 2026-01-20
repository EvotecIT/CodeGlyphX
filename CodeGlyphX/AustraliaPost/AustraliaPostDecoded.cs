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
    /// Gets whether the customer information encoding was ambiguous.
    /// </summary>
    public bool IsCustomerInfoAmbiguous { get; }

    /// <summary>
    /// Gets the concatenated payload (DPID + customer info).
    /// </summary>
    public string Value => Dpid + CustomerInfo;

    internal AustraliaPostDecoded(
        AustraliaPostFormat format,
        int formatControlCode,
        string dpid,
        string customerInfo,
        AustraliaPostCustomerEncodingTable? customerInfoEncoding,
        bool isCustomerInfoAmbiguous) {
        Format = format;
        FormatControlCode = formatControlCode;
        Dpid = dpid;
        CustomerInfo = customerInfo;
        CustomerInfoEncoding = customerInfoEncoding;
        IsCustomerInfoAmbiguous = isCustomerInfoAmbiguous;
    }
}

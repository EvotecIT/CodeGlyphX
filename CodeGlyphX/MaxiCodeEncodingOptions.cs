using System.Text;

namespace CodeGlyphX;

/// <summary>
/// Controls MaxiCode mode, primary carrier data, ECI, and structured append.
/// </summary>
public sealed class MaxiCodeEncodingOptions {
    /// <summary>Gets or sets the MaxiCode mode. The default is <see cref="MaxiCodeMode.Auto"/>.</summary>
    public MaxiCodeMode Mode { get; set; } = MaxiCodeMode.Auto;
    /// <summary>Gets or sets the postal code used by Modes 2 and 3.</summary>
    public string? PostalCode { get; set; }
    /// <summary>Gets or sets the three-digit ISO country code used by Modes 2 and 3.</summary>
    public int CountryCode { get; set; }
    /// <summary>Gets or sets the three-digit service class used by Modes 2 and 3.</summary>
    public int ServiceClass { get; set; }
    /// <summary>Gets or sets an optional ECI assignment number from 0 through 999999.</summary>
    public int? EciAssignmentNumber { get; set; }
    /// <summary>Gets or sets the payload encoding. A matching known ECI is inferred; unknown encodings require <see cref="EciAssignmentNumber"/>. Text that cannot be represented is rejected.</summary>
    public Encoding? TextEncoding { get; set; }
    /// <summary>Gets or sets the one-based structured-append symbol index.</summary>
    public int? StructuredAppendIndex { get; set; }
    /// <summary>Gets or sets the structured-append symbol count (2 through 8).</summary>
    public int? StructuredAppendCount { get; set; }
    /// <summary>Gets or sets the two-digit Structured Carrier Message prefix version (0 through 99).</summary>
    public int? StructuredCarrierMessageVersion { get; set; }

    internal MaxiCodeEncodingOptions Clone() => new() {
        Mode = Mode,
        PostalCode = PostalCode,
        CountryCode = CountryCode,
        ServiceClass = ServiceClass,
        EciAssignmentNumber = EciAssignmentNumber,
        TextEncoding = TextEncoding,
        StructuredAppendIndex = StructuredAppendIndex,
        StructuredAppendCount = StructuredAppendCount,
        StructuredCarrierMessageVersion = StructuredCarrierMessageVersion
    };
}

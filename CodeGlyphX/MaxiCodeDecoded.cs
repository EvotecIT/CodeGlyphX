using System;
using System.Collections.Generic;

namespace CodeGlyphX;

/// <summary>
/// Detailed MaxiCode payload and carrier metadata.
/// </summary>
public sealed class MaxiCodeDecoded {
    private readonly byte[] _bytes;

    /// <summary>Gets the decoded mode.</summary>
    public MaxiCodeMode Mode { get; }
    /// <summary>Gets the decoded secondary message.</summary>
    public string Text { get; }
    /// <summary>Gets a copy of decoded secondary-message bytes.</summary>
    public byte[] Bytes => (byte[])_bytes.Clone();
    /// <summary>Gets the postal code for Modes 2 and 3.</summary>
    public string? PostalCode { get; }
    /// <summary>Gets the country code for Modes 2 and 3.</summary>
    public int? CountryCode { get; }
    /// <summary>Gets the service class for Modes 2 and 3.</summary>
    public int? ServiceClass { get; }
    /// <summary>Gets ECI assignments encountered in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    /// <summary>Gets the one-based structured-append index.</summary>
    public int? StructuredAppendIndex { get; }
    /// <summary>Gets the structured-append count.</summary>
    public int? StructuredAppendCount { get; }
    /// <summary>Gets whether this is a Mode 6 reader-programming symbol.</summary>
    public bool IsReaderProgramming => Mode == MaxiCodeMode.ReaderProgramming;
    /// <summary>Gets the AIM symbology identifier.</summary>
    public string SymbologyIdentifier => Mode is MaxiCodeMode.StructuredCarrierNumeric or MaxiCodeMode.StructuredCarrierAlphanumeric ? "]U1" : "]U0";

    internal MaxiCodeDecoded(
        MaxiCodeMode mode,
        string text,
        byte[] bytes,
        string? postalCode,
        int? countryCode,
        int? serviceClass,
        int[] eciAssignments,
        int? structuredAppendIndex,
        int? structuredAppendCount) {
        Mode = mode;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _bytes = (byte[])(bytes ?? throw new ArgumentNullException(nameof(bytes))).Clone();
        PostalCode = postalCode;
        CountryCode = countryCode;
        ServiceClass = serviceClass;
        EciAssignments = Array.AsReadOnly(eciAssignments ?? throw new ArgumentNullException(nameof(eciAssignments)));
        StructuredAppendIndex = structuredAppendIndex;
        StructuredAppendCount = structuredAppendCount;
    }
}

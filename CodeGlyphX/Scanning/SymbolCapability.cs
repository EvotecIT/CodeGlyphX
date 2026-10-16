using System;

namespace CodeGlyphX;

/// <summary>
/// Describes the implemented operations for one symbol format.
/// </summary>
public sealed class SymbolCapability {
    /// <summary>Gets the physical symbol format.</summary>
    public SymbolFormat Format { get; }
    /// <summary>Gets the human-readable format name.</summary>
    public string DisplayName { get; }
    /// <summary>Gets the broad symbol family.</summary>
    public SymbolFamily Family { get; }
    /// <summary>Gets the implemented operations.</summary>
    public SymbolCapabilityFlags Operations { get; }
    /// <summary>Gets the legacy barcode type when this format is represented by <see cref="BarcodeType"/>.</summary>
    public BarcodeType? LegacyBarcodeType { get; }

    /// <summary>Gets whether module generation is implemented.</summary>
    public bool CanEncode => Has(SymbolCapabilityFlags.Encode);
    /// <summary>Gets whether module-level decoding is implemented.</summary>
    public bool CanDecodeModules => Has(SymbolCapabilityFlags.DecodeModules);
    /// <summary>Gets whether image recognition is implemented.</summary>
    public bool CanScanImages => Has(SymbolCapabilityFlags.ScanImage);
    /// <summary>Gets whether this format participates when no explicit scan formats are supplied.</summary>
    /// <remarks>
    /// Pharmacode and Patch Code patterns provide insufficient evidence for reliable unrestricted
    /// recognition. Request them explicitly when the application knows they may be present.
    /// DAFT, Flattermarken, FIM and KIX also require explicit selection because raw or unchecked patterns
    /// do not provide sufficient evidence for unrestricted recognition. Raw marks can require a calibrated grid.
    /// Telepen Numeric requires explicit selection because its bars also have a valid Telepen ASCII interpretation.
    /// </remarks>
    public bool IsDefaultScanFormat => CanScanImages && Format != SymbolFormat.Pharmacode &&
        Format != SymbolFormat.PharmacodeTwoTrack && Format != SymbolFormat.PatchCode && Format != SymbolFormat.TelepenNumeric &&
        Format != SymbolFormat.Daft && Format != SymbolFormat.Flattermarken && Format != SymbolFormat.FacingIdentificationMark &&
        Format != SymbolFormat.KixCode;
    /// <summary>Gets whether one image scan can return multiple instances.</summary>
    public bool CanScanMultiple => Has(SymbolCapabilityFlags.ScanMultiple);
    /// <summary>Gets whether image recognition reports symbol geometry.</summary>
    public bool ReportsGeometry => Has(SymbolCapabilityFlags.ReportsGeometry);

    internal SymbolCapability(
        SymbolFormat format,
        string displayName,
        SymbolFamily family,
        SymbolCapabilityFlags operations,
        BarcodeType? legacyBarcodeType = null) {
        Format = format;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Family = family;
        Operations = operations;
        LegacyBarcodeType = legacyBarcodeType;
    }

    /// <summary>Checks whether an operation is implemented.</summary>
    public bool Has(SymbolCapabilityFlags operation) => (Operations & operation) == operation;
}

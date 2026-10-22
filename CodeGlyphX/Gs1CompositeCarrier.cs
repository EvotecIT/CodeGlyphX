namespace CodeGlyphX;

/// <summary>The linear carrier linked to a GS1 Composite component.</summary>
public enum Gs1CompositeCarrier {
    /// <summary>GS1-128, supporting CC-A, CC-B and CC-C.</summary>
    Gs1_128,
    /// <summary>EAN-13.</summary>
    Ean13,
    /// <summary>EAN-8.</summary>
    Ean8,
    /// <summary>UPC-A.</summary>
    UpcA,
    /// <summary>UPC-E.</summary>
    UpcE,
    /// <summary>GS1 DataBar Omnidirectional.</summary>
    DataBarOmnidirectional,
    /// <summary>GS1 DataBar Truncated.</summary>
    DataBarTruncated,
    /// <summary>GS1 DataBar Stacked.</summary>
    DataBarStacked,
    /// <summary>GS1 DataBar Stacked Omnidirectional.</summary>
    DataBarStackedOmnidirectional,
    /// <summary>GS1 DataBar Limited.</summary>
    DataBarLimited,
    /// <summary>GS1 DataBar Expanded.</summary>
    DataBarExpanded,
    /// <summary>GS1 DataBar Expanded Stacked.</summary>
    DataBarExpandedStacked
}

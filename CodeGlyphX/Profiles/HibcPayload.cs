using System;

namespace CodeGlyphX;

/// <summary>A checked HIBC supplier payload with validated primary and production data structure.</summary>
/// <remarks>Structural validity does not assign a labeler code or establish regulatory acceptance.</remarks>
public sealed class HibcPayload {
    /// <summary>Gets the complete payload, including its leading plus and check character.</summary>
    public string Text { get; }
    /// <summary>Gets the unframed data.</summary>
    public string Data { get; }
    /// <summary>Gets the primary identifier, or null for a separate secondary symbol.</summary>
    public HibcPrimaryData? Primary { get; }
    /// <summary>Gets the production data, or null for a primary-only symbol.</summary>
    public HibcProductionData? Production { get; }
    internal HibcPayload(string text, string data, HibcPrimaryData? primary, HibcProductionData? production) {
        Text = text; Data = data; Primary = primary; Production = production;
    }
}

/// <summary>Fields of a syntactically valid HIBC primary identifier.</summary>
public sealed class HibcPrimaryData {
    /// <summary>Gets the primary data without flag or check character.</summary>
    public string Data { get; }
    /// <summary>Gets the four-character labeler identification code.</summary>
    public string LabelerCode => Data.Substring(0, 4);
    /// <summary>Gets the one through eighteen character product or catalog number.</summary>
    public string ProductNumber => Data.Substring(4, Data.Length - 5);
    /// <summary>Gets the packaging unit identifier, zero through nine.</summary>
    public int UnitOfMeasure => Data[Data.Length - 1] - '0';
    internal HibcPrimaryData(string data) { Data = data; }
}

/// <summary>The encoded HIBC date notation. Two-digit years remain two-digit values without an inferred century.</summary>
public enum HibcDateFormat {
    /// <summary>No date is present.</summary>
    None,
    /// <summary>Month and two-digit year.</summary>
    MonthYear,
    /// <summary>Month, day and two-digit year.</summary>
    MonthDayYear,
    /// <summary>Two-digit year, month and day.</summary>
    YearMonthDay,
    /// <summary>Two-digit year, month, day and hour.</summary>
    YearMonthDayHour,
    /// <summary>Two-digit year and ordinal day of year.</summary>
    YearJulianDay,
    /// <summary>Two-digit year, ordinal day of year and hour.</summary>
    YearJulianDayHour
}

/// <summary>Validated secondary and supplemental HIBC supplier fields.</summary>
public sealed class HibcProductionData {
    /// <summary>Gets the date digits exactly as encoded, without the format flag.</summary>
    public string? Date { get; internal set; }
    /// <summary>Gets how to interpret the encoded date digits.</summary>
    public HibcDateFormat DateFormat { get; internal set; }
    /// <summary>Gets the lot/batch identifier when present.</summary>
    public string? LotNumber { get; internal set; }
    /// <summary>Gets the secondary or supplemental serial identifier when present.</summary>
    public string? SerialNumber { get; internal set; }
    /// <summary>Gets a separate secondary symbol's primary-link character.</summary>
    /// <remarks>Compare it with the expected primary check character before treating two symbols as linked.</remarks>
    public char? LinkCharacter { get; internal set; }
    /// <summary>Gets whether a decoded bare Julian-date prefix used the permitted historical lot field.</summary>
    public bool HasLegacyLot { get; internal set; }
    /// <summary>Gets the supplemental manufacture date.</summary>
    public DateTime? ManufactureDate { get; internal set; }
    /// <summary>Gets the supplemental expiry date expressed with a four-digit year.</summary>
    public DateTime? ExpiryDate { get; internal set; }
    /// <summary>Gets the supplemental quantity, when present.</summary>
    public int? Quantity { get; internal set; }
    internal HibcProductionData() { }
}

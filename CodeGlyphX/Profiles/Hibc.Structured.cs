using System;

namespace CodeGlyphX;

public static partial class Hibc {
    /// <summary>Checks supplier data fields and adds the HIBC flag and Mod-43 check character.</summary>
    /// <remarks>The existing Prepare method remains available for low-level transport framing.</remarks>
    public static string PrepareStructured(string data) {
        var payload = Prepare(data);
        if (!TryParseStructured(payload, out var parsed) || parsed.Production?.HasLegacyLot == true)
            throw new ArgumentException("The data is not a current HIBC supplier primary, secondary, or concatenated structure.", nameof(data));
        return payload;
    }

    /// <summary>Validates framed supplier data, retaining primary, secondary and supplemental fields.</summary>
    /// <remarks>Historical bare-Julian-date lot fields are accepted for decoding and marked on the result.</remarks>
    public static bool TryParseStructured(string payload, out HibcPayload value) {
        value = null!;
        if (!TryParse(payload, out var data)) return false;
        return HibcStructureParser.TryParse(payload, data, out value);
    }

    /// <summary>Checks separate supplier structures, their primary-link character, and the quantity/unit-of-measure constraint.</summary>
    public static bool IsLinked(string primaryPayload, string secondaryPayload) =>
        TryParseStructured(primaryPayload, out var primary) && primary.Primary is not null && primary.Production is null
        && TryParseStructured(secondaryPayload, out var secondary) && secondary.Primary is null && secondary.Production is not null
        && secondary.Production.LinkCharacter == primaryPayload[primaryPayload.Length - 1]
        && (!secondary.Production.Quantity.HasValue || primary.Primary.UnitOfMeasure == 9);

    /// <summary>Formats checked HIBC data for human reading, including delimiters and primary/secondary line separation.</summary>
    /// <remarks>Printed underscores represent spaces; slashed zero represents encoded zero. The barcode payload is unchanged.</remarks>
    public static string HumanReadable(string payload) {
        if (!TryParse(payload, out var data)) throw new ArgumentException("The HIBC flag or check character is invalid.", nameof(payload));
        var text = payload.Replace(' ', '_').Replace('0', 'Ø');
        var split = data.Length > 0 && data[0] >= 'A' && data[0] <= 'Z' ? data.IndexOf('/') : -1;
        if (split >= 0) text = text.Insert(split + 2, "\n");
        return "*" + text + "*";
    }
}

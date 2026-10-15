using System;
using CodeGlyphX.DataMatrix;

namespace CodeGlyphX.Mailmark;

/// <summary>The three Data Matrix sizes permitted for 2D Mailmark.</summary>
public enum Mailmark2DType {
    /// <summary>A 24 by 24 square symbol.</summary>
    Type7 = 7,
    /// <summary>A 32 by 32 square symbol.</summary>
    Type9 = 9,
    /// <summary>A 16 by 48 rectangular symbol.</summary>
    Type29 = 29
}

/// <summary>Builds and reads the 45-character Royal Mail header and its optional customer content.</summary>
/// <remarks>Validates field structure and encoding. Postal address assignment and mail service eligibility require Royal Mail's separate data and processes.</remarks>
public static class Mailmark2D {
    /// <summary>Builds a padded Mailmark string. Supply chain and item identifiers require seven and eight digits.</summary>
    public static string Prepare(string supplyChainId, string itemId, string destination, char itemClass = '2',
        char serviceType = '0', string returnPostcode = "", string customerContent = "", char informationType = '0') {
        if (supplyChainId is null) throw new ArgumentNullException(nameof(supplyChainId));
        if (itemId is null) throw new ArgumentNullException(nameof(itemId));
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        if (returnPostcode is null) throw new ArgumentNullException(nameof(returnPostcode));
        if (customerContent is null) throw new ArgumentNullException(nameof(customerContent));
        if (destination.Length > 9 || returnPostcode.Length > 7) throw new ArgumentException("Postcode fields exceed their Mailmark field lengths.");
        var value = "JGB " + informationType + "1" + itemClass + supplyChainId + itemId
            + destination.PadRight(9) + serviceType + returnPostcode.PadRight(7) + "      " + customerContent;
        if (supplyChainId.Length != 7 || itemId.Length != 8 || !TryParse(value, out _))
            throw new ArgumentException("Invalid Mailmark fields.");
        return value;
    }

    /// <summary>Encodes a complete Mailmark string with the mandatory basic C40 header and selected Data Matrix size.</summary>
    public static DataMatrixSymbol Encode(string value, Mailmark2DType type = Mailmark2DType.Type29) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (!TryParse(value, out _)) throw new ArgumentException("Invalid Mailmark 2D header or customer content.", nameof(value));
        var rows = type == Mailmark2DType.Type7 ? 24 : type == Mailmark2DType.Type9 ? 32 : type == Mailmark2DType.Type29 ? 16 : 0;
        if (rows == 0) throw new ArgumentOutOfRangeException(nameof(type));
        return DataMatrixEncoder.EncodeSymbol(value, new DataMatrixEncodingOptions {
            Rows = rows, Columns = type == Mailmark2DType.Type29 ? 48 : rows,
            Shape = type == Mailmark2DType.Type29 ? DataMatrixShape.OriginalRectangular : DataMatrixShape.Square,
            C40BasicHeaderLength = 45
        });
    }

    /// <summary>Decodes a Mailmark module grid, validating its size, header contents and basic C40 encodation.</summary>
    public static bool TryDecode(BitMatrix modules, out Mailmark2DData data, out Mailmark2DType type) {
        data = null!;
        type = default;
        if (modules is null || !DataMatrixDecoder.TryDecodeDetailed(modules, out var decoded)) return false;
        if (decoded.Rows == 24 && decoded.Columns == 24) type = Mailmark2DType.Type7;
        else if (decoded.Rows == 32 && decoded.Columns == 32) type = Mailmark2DType.Type9;
        else if (decoded.Rows == 16 && decoded.Columns == 48) type = Mailmark2DType.Type29;
        else return false;
        return decoded.C40BasicHeaderLength >= 45 && TryParse(decoded.Text, out data);
    }

    /// <summary>Parses fixed fields without removing significant spaces from the original payload.</summary>
    public static bool TryParse(string value, out Mailmark2DData data) {
        data = null!;
        if (value is null || value.Length < 45 || !value.StartsWith("JGB ", StringComparison.Ordinal)) return false;
        for (var i = 4; i < 45; i++)
            if (value[i] != ' ' && (value[i] < '0' || value[i] > '9') && (value[i] < 'A' || value[i] > 'Z')) return false;
        if ("012ABC".IndexOf(value[4]) < 0 || value[5] != '1' || value[6] == ' ' || value[31] < '0' || value[31] > '4') return false;
        for (var i = 7; i < 22; i++) if (value[i] < '0' || value[i] > '9') return false;
        if (!MailmarkDestination.IsValid2DDestination(value.AsSpan(22, 9))
            || !MailmarkDestination.IsValid2DReturn(value.AsSpan(32, 7))) return false;
        for (var i = 39; i < 45; i++) if (value[i] != ' ') return false;
        for (var i = 45; i < value.Length; i++) if (value[i] > 255) return false;
        data = new Mailmark2DData(value);
        return true;
    }
}

/// <summary>Parsed fixed Mailmark fields and customer content.</summary>
public sealed class Mailmark2DData {
    /// <summary>Gets the complete string, preserving field padding.</summary>
    public string Text { get; }
    /// <summary>Gets the product information type.</summary>
    public char InformationType => Text[4];
    /// <summary>Gets the version identifier.</summary>
    public char Version => Text[5];
    /// <summary>Gets the mail class.</summary>
    public char Class => Text[6];
    /// <summary>Gets the seven-digit supply chain identifier.</summary>
    public string SupplyChainId => Text.Substring(7, 7);
    /// <summary>Gets the eight-digit item identifier.</summary>
    public string ItemId => Text.Substring(14, 8);
    /// <summary>Gets the destination postcode and delivery point suffix without right padding.</summary>
    public string Destination => Text.Substring(22, 9).TrimEnd(' ');
    /// <summary>Gets the service type.</summary>
    public char ServiceType => Text[31];
    /// <summary>Gets the return postcode without right padding.</summary>
    public string ReturnPostcode => Text.Substring(32, 7).TrimEnd(' ');
    /// <summary>Gets the optional customer content.</summary>
    public string CustomerContent => Text.Substring(45);
    internal Mailmark2DData(string value) => Text = value;
}

using System;
using CodeGlyphX.ChannelCode;
using CodeGlyphX.Code11;
using CodeGlyphX.Code25;
using CodeGlyphX.Code39;
using CodeGlyphX.Codabar;
using CodeGlyphX.Itf;
using CodeGlyphX.Msi;
using CodeGlyphX.Plessey;
using CodeGlyphX.Bc412;

namespace CodeGlyphX;

public static partial class BarcodeEncoder {
    /// <summary>Encodes with explicit optional-check, finder and readable-text controls.</summary>
    public static Barcode1D Encode(BarcodeType type, string value, BarcodeEncodingOptions options) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (value.Length > 32768) throw new ArgumentException("Explicit linear encoding accepts at most 32768 input characters.", nameof(value));
        var o = options.Snapshot();
        ValidateEncodingControls(type, o);
        var mode = o.CheckMode ?? (type is BarcodeType.Code39 or BarcodeType.Code11 or BarcodeType.MSI or BarcodeType.Plessey or BarcodeType.Bc412
            ? BarcodeCheckMode.Compute : BarcodeCheckMode.None);
        switch (type) {
            case BarcodeType.Bc412:
                if (mode == BarcodeCheckMode.None) throw new ArgumentException("SEMI BC412 requires its check character.", nameof(options));
                if (mode == BarcodeCheckMode.Validate && value.Length < 8) throw new ArgumentException("BC412 requires at least seven data characters and its supplied check.", nameof(value));
                var semiData = mode == BarcodeCheckMode.Validate ? value.Remove(1, 1) : value;
                var semiChecked = Bc412Encoder.AddSemiCheck(semiData);
                if (mode == BarcodeCheckMode.Validate && semiChecked != value) throw new ArgumentException("The supplied second-position BC412 check character is incorrect.", nameof(value));
                return Readable(Bc412Encoder.Encode(semiData), o.PrintCheckCharacters ? semiChecked : semiData);
            case BarcodeType.Code39: return OptionalCode39(value, mode, o);
            case BarcodeType.Code11: return OptionalCode11(value, mode, o.PrintCheckCharacters);
            case BarcodeType.Codabar: return OptionalCodabar(value, mode, o);
            case BarcodeType.MSI: return OptionalMsi(value, mode, o);
            case BarcodeType.Plessey: return CheckedPlessey(value, mode, o.PrintCheckCharacters);
            case BarcodeType.ITF:
            case BarcodeType.Industrial2of5:
            case BarcodeType.Matrix2of5:
            case BarcodeType.IATA2of5:
            case BarcodeType.Coop2of5:
            case BarcodeType.Datalogic2of5: return OptionalTwoOfFive(type, value, mode, o.PrintCheckCharacters);
            case BarcodeType.ChannelCode:
                var channel = ChannelCodeEncoder.Encode(value, o.ChannelCount, o.ChannelShortFinder);
                return Readable(channel, value.PadLeft((o.ChannelCount ?? value.Length + 1) - 1, '0'));
            case BarcodeType.ITF14:
                var digits = value.Replace(" ", string.Empty);
                var itf14 = Itf14Encoder.Encode(digits);
                return Readable(itf14, digits.Length == 13 ? value + IdentifierChecks.Gs1(digits) : value);
            default:
                var encoded = Encode(type, value);
                // Retail layout derives checked digits and add-on groups from the actual bars.
                if (type is BarcodeType.EAN or BarcodeType.UPCA or BarcodeType.UPCE or BarcodeType.EAN2 or BarcodeType.EAN5) return encoded;
                var readable = PrintableAscii(encoded.HumanReadableText ?? value);
                if (encoded.Format is SymbolFormat.Gs1Code128 or SymbolFormat.Gs1DataBarExpanded)
                    readable = Gs1.HumanReadable(value);
                if (encoded.Format is SymbolFormat.Gs1DataBarLimited or SymbolFormat.Gs1DataBarOmnidirectional or SymbolFormat.Gs1DataBarTruncated)
                    readable = "(01)" + BarcodeProfiles.Prepare(LinearBarcodeProfile.Ean14, value);
                return Readable(encoded, readable);
        }
    }

    private static Barcode1D Readable(Barcode1D encoded, string text) => new(encoded.Segments, encoded.Format, text);

    private static Barcode1D OptionalCode39(string value, BarcodeCheckMode mode, BarcodeEncodingOptions o) {
        var data = mode == BarcodeCheckMode.Validate ? RemoveSuppliedChecks(value, 1) : value;
        var check = mode == BarcodeCheckMode.None ? string.Empty : Code39Encoder.GetChecksum(o.Code39FullAscii ? Code39Encoder.Prepare(data) : data).ToString();
        RequireSuppliedChecks(value, data, check, mode);
        var encoded = Code39Encoder.Encode(data, mode != BarcodeCheckMode.None, o.Code39FullAscii);
        return Readable(encoded, "*" + PrintableAscii(data) + (o.PrintCheckCharacters ? check : string.Empty) + "*");
    }

    private static string PrintableAscii(string value) {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++) if (chars[i] < ' ' || chars[i] == '\u007f') chars[i] = ' ';
        return new string(chars);
    }

    private static Barcode1D OptionalCode11(string value, BarcodeCheckMode mode, bool printChecks) {
        if (mode == BarcodeCheckMode.Validate && value.Length == 11)
            throw new ArgumentException("Code 11 takes one check after fewer than ten data characters and two after ten or more.", nameof(value));
        var count = value.Length <= 10 ? 1 : 2;
        var data = mode == BarcodeCheckMode.Validate ? RemoveSuppliedChecks(value, count) : value;
        var checks = string.Empty;
        if (mode != BarcodeCheckMode.None) {
            checks = Code11Encoder.CalcChecksum(data, 10).ToString();
            if (data.Length >= 10) checks += Code11Encoder.CalcChecksum(data + checks, 9);
        }
        RequireSuppliedChecks(value, data, checks, mode);
        return Readable(Code11Encoder.Encode(data, mode != BarcodeCheckMode.None), data + (printChecks ? checks : string.Empty));
    }

    private static Barcode1D OptionalMsi(string value, BarcodeCheckMode mode, BarcodeEncodingOptions o) {
        var algorithm = mode == BarcodeCheckMode.None ? MsiChecksumType.None : o.MsiChecksum;
        var data = value;
        if (mode == BarcodeCheckMode.Validate && !MsiChecks.TryStrip(value, algorithm, o.MsiExpectedDataLength, out data))
            throw new ArgumentException("The supplied MSI check is invalid or ambiguous. Select its algorithm and supply a known data length when needed.", nameof(value));
        var encoded = MsiEncoder.Encode(data, algorithm);
        return Readable(encoded, o.PrintCheckCharacters ? MsiChecks.Append(data, algorithm) : data);
    }

    private static Barcode1D OptionalTwoOfFive(BarcodeType type, string value, BarcodeCheckMode mode, bool printChecks) {
        var data = mode == BarcodeCheckMode.Validate ? RemoveSuppliedChecks(value, 1) : value;
        IdentifierChecks.RequireDigits(data);
        var check = mode == BarcodeCheckMode.None ? string.Empty : IdentifierChecks.Gs1(data).ToString();
        RequireSuppliedChecks(value, data, check, mode);
        var carried = data + check;
        // USS-I 2/5 requires pairs; its optional check is calculated before adding a leading zero.
        if (type == BarcodeType.ITF && (carried.Length & 1) != 0) { carried = "0" + carried; data = "0" + data; }
        var encoded = type switch {
            BarcodeType.ITF => ItfEncoder.Encode(carried),
            BarcodeType.Industrial2of5 => Industrial2Of5Encoder.Encode(carried),
            BarcodeType.Matrix2of5 => Matrix2Of5Encoder.Encode(carried),
            BarcodeType.IATA2of5 => Iata2Of5Encoder.Encode(carried),
            BarcodeType.Coop2of5 => Coop2Of5Encoder.Encode(carried),
            _ => Datalogic2Of5Encoder.Encode(carried)
        };
        return Readable(encoded, data + (printChecks ? check : string.Empty));
    }

    private static Barcode1D OptionalCodabar(string value, BarcodeCheckMode mode, BarcodeEncodingOptions o) {
        value = value.Trim();
        var start = o.CodabarStart; var stop = o.CodabarStop;
        if (value.Length >= 2 && CodabarEncoder.IsStartStop(value[0]) && CodabarEncoder.IsStartStop(value[value.Length - 1])) {
            start = value[0]; stop = value[value.Length - 1]; value = value.Substring(1, value.Length - 2);
        }
        start = CodabarEncoder.NormalizeStartStop(start); stop = CodabarEncoder.NormalizeStartStop(stop);
        var data = mode == BarcodeCheckMode.Validate ? RemoveSuppliedChecks(value, 1) : value;
        const string alphabet = "0123456789-$:/.+ABCD";
        var sum = alphabet.IndexOf(start) + alphabet.IndexOf(stop);
        foreach (var character in data) {
            var digit = alphabet.IndexOf(character);
            if (digit < 0 || digit >= 16) throw new ArgumentException("Codabar data requires digits or -$:/.+.", nameof(value));
            sum = (sum + digit) % 16;
        }
        var check = mode == BarcodeCheckMode.None ? string.Empty : alphabet[(16 - sum % 16) % 16].ToString();
        RequireSuppliedChecks(value, data, check, mode);
        return Readable(CodabarEncoder.Encode(data + check, start, stop), start + data + (o.PrintCheckCharacters ? check : string.Empty) + stop);
    }

    private static Barcode1D CheckedPlessey(string value, BarcodeCheckMode mode, bool printChecks) {
        if (mode == BarcodeCheckMode.None) throw new ArgumentException("Plessey requires its CRC check characters.", nameof(mode));
        value = value.ToUpperInvariant();
        var data = mode == BarcodeCheckMode.Validate ? RemoveSuppliedChecks(value, 2) : value;
        var check = PlesseyEncoder.CheckCharacters(data);
        RequireSuppliedChecks(value, data, check, mode);
        return Readable(PlesseyEncoder.Encode(data), data + (printChecks ? check : string.Empty));
    }

    private static string RemoveSuppliedChecks(string value, int count) {
        if (value.Length <= count) throw new ArgumentException("A supplied check follows at least one data character.", nameof(value));
        return value.Substring(0, value.Length - count);
    }
    private static void RequireSuppliedChecks(string value, string data, string check, BarcodeCheckMode mode) {
        if (mode == BarcodeCheckMode.Validate && value != data + check) throw new ArgumentException("The supplied check character is incorrect.", nameof(value));
    }

    private static void ValidateEncodingControls(BarcodeType type, BarcodeEncodingOptions o) {
        var supportsCheck = type is BarcodeType.Code39 or BarcodeType.Code11 or BarcodeType.Codabar or BarcodeType.MSI or BarcodeType.Plessey or BarcodeType.Bc412
            or BarcodeType.ITF or BarcodeType.Industrial2of5 or BarcodeType.Matrix2of5 or BarcodeType.IATA2of5 or BarcodeType.Coop2of5 or BarcodeType.Datalogic2of5;
        if (o.CheckMode.HasValue && o.CheckMode != BarcodeCheckMode.None && !supportsCheck)
            throw new ArgumentException("This format has no selectable optional check character.", nameof(o));
        if ((o.Code39FullAscii && type != BarcodeType.Code39) || ((o.ChannelShortFinder || o.ChannelCount.HasValue) && type != BarcodeType.ChannelCode)
            || ((o.MsiChecksum != MsiChecksumType.Mod10 || o.MsiExpectedDataLength.HasValue) && type != BarcodeType.MSI)
            || ((o.CodabarStart != 'A' || o.CodabarStop != 'B') && type != BarcodeType.Codabar))
            throw new ArgumentException("An encoding control does not apply to the selected format.", nameof(o));
    }
}

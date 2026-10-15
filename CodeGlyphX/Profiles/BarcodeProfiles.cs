using System;
using CodeGlyphX.Code39;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>Prepares, validates, encodes, and decodes application identifiers using existing barcode carriers.</summary>
public static class BarcodeProfiles {
    /// <summary>Returns the canonical identifier, computing an omitted check digit and rejecting an incorrect supplied one.</summary>
    /// <remarks>Hyphens and spaces are accepted in identifiers. ISBN and ISMN return their current thirteen-digit form.</remarks>
    public static string Prepare(LinearBarcodeProfile profile, string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var normalized = value.Replace("-", string.Empty).Replace(" ", string.Empty);
        return profile switch {
            LinearBarcodeProfile.Ean14 => IdentifierChecks.WithCheck(normalized, 14, IdentifierChecks.Gs1),
            LinearBarcodeProfile.Sscc18 => IdentifierChecks.WithCheck(normalized, 18, IdentifierChecks.Gs1),
            LinearBarcodeProfile.Isbn => BookIdentifiers.Isbn(normalized),
            LinearBarcodeProfile.Ismn => BookIdentifiers.Ismn(normalized),
            LinearBarcodeProfile.Issn => BookIdentifiers.Issn(normalized),
            LinearBarcodeProfile.Pzn7 => IdentifierChecks.WithCheck(normalized, 7, body => IdentifierChecks.Pzn(body, 2)),
            LinearBarcodeProfile.Pzn8 => IdentifierChecks.WithCheck(normalized, 8, body => IdentifierChecks.Pzn(body, 1)),
            LinearBarcodeProfile.Identcode => IdentifierChecks.WithCheck(normalized, 12, IdentifierChecks.DeutschePost),
            LinearBarcodeProfile.Leitcode => IdentifierChecks.WithCheck(normalized, 14, IdentifierChecks.DeutschePost),
            LinearBarcodeProfile.MarksAndSpencer => PrepareMarksAndSpencer(normalized),
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };
    }

    /// <summary>Encodes a validated identifier using the application's physical carrier.</summary>
    public static Barcode1D Encode(LinearBarcodeProfile profile, string value) {
        var identifier = Prepare(profile, value);
        return profile switch {
            LinearBarcodeProfile.Ean14 => Gs1.Encode128("(01)" + identifier),
            LinearBarcodeProfile.Sscc18 => Gs1.Encode128("(00)" + identifier),
            LinearBarcodeProfile.Isbn or LinearBarcodeProfile.Ismn or LinearBarcodeProfile.MarksAndSpencer => EanEncoder.Encode(identifier),
            LinearBarcodeProfile.Issn => EanEncoder.Encode("977" + identifier.Substring(0, 7) + "00"),
            LinearBarcodeProfile.Pzn7 or LinearBarcodeProfile.Pzn8 => Code39Encoder.Encode("-" + identifier, includeChecksum: false),
            LinearBarcodeProfile.Identcode or LinearBarcodeProfile.Leitcode => ItfEncoder.Encode(identifier),
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };
    }

    /// <summary>Returns a readable caption for a checked application identifier.</summary>
    public static string HumanReadable(LinearBarcodeProfile profile, string value) {
        var identifier = Prepare(profile, value);
        return profile switch {
            LinearBarcodeProfile.Ean14 => "(01)" + identifier,
            LinearBarcodeProfile.Sscc18 => "(00)" + identifier,
            LinearBarcodeProfile.Isbn => "ISBN " + identifier,
            LinearBarcodeProfile.Ismn => "ISMN " + identifier,
            LinearBarcodeProfile.Issn => "ISSN " + identifier.Substring(0, 4) + "-" + identifier.Substring(4),
            LinearBarcodeProfile.Pzn7 or LinearBarcodeProfile.Pzn8 => "PZN " + identifier,
            LinearBarcodeProfile.MarksAndSpencer => "M " + identifier.Substring(1) + " S",
            _ => identifier
        };
    }

    /// <summary>Renders a checked identifier with its application caption through the standard barcode renderer.</summary>
    /// <remarks>A caller-supplied label replaces the default caption. This is a symbol and caption, not a complete commercial label template.</remarks>
    public static RenderedOutput Render(LinearBarcodeProfile profile, string value, OutputFormat format,
        BarcodeOptions? options = null, OutputOptions? outputOptions = null) {
        var render = options?.Clone() ?? new BarcodeOptions();
        render.LabelText ??= HumanReadable(profile, value);
        return Encode(profile, value).Render(format, render, outputOptions);
    }

    /// <summary>Decodes a known carrier and validates its application identifier before returning a canonical value.</summary>
    /// <remarks>The profile is explicit because a physical carrier does not uniquely identify its application.</remarks>
    public static bool TryDecode(LinearBarcodeProfile profile, ReadOnlySpan<bool> modules, out string identifier) {
        identifier = string.Empty;
        var carrier = profile switch {
            LinearBarcodeProfile.Ean14 or LinearBarcodeProfile.Sscc18 => BarcodeType.GS1_128,
            LinearBarcodeProfile.Pzn7 or LinearBarcodeProfile.Pzn8 => BarcodeType.Code39,
            LinearBarcodeProfile.Identcode or LinearBarcodeProfile.Leitcode => BarcodeType.ITF,
            LinearBarcodeProfile.Isbn or LinearBarcodeProfile.Ismn or LinearBarcodeProfile.Issn or LinearBarcodeProfile.MarksAndSpencer => BarcodeType.EAN,
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };
        if (!BarcodeDecoder.TryDecode(modules.ToArray(), carrier, out var decoded)) return false;
        return TryParse(profile, decoded.Text, out identifier);
    }

    /// <summary>Validates an already-decoded carrier payload and extracts its application identifier.</summary>
    public static bool TryParse(LinearBarcodeProfile profile, string encodedText, out string identifier) {
        identifier = string.Empty;
        if (encodedText is null) return false;
        var value = encodedText;
        switch (profile) {
            case LinearBarcodeProfile.Ean14:
                if (value.Length != 16 || !value.StartsWith("01", StringComparison.Ordinal)) return false;
                value = value.Substring(2); break;
            case LinearBarcodeProfile.Sscc18:
                if (value.Length != 20 || !value.StartsWith("00", StringComparison.Ordinal)) return false;
                value = value.Substring(2); break;
            case LinearBarcodeProfile.Pzn7:
            case LinearBarcodeProfile.Pzn8:
                if (!value.StartsWith("-", StringComparison.Ordinal) || value.Length != (profile == LinearBarcodeProfile.Pzn7 ? 8 : 9)) return false;
                value = value.Substring(1); break;
            case LinearBarcodeProfile.Issn:
                if (value.Length != 13 || !value.StartsWith("977", StringComparison.Ordinal)) return false;
                try { IdentifierChecks.WithCheck(value, 13, IdentifierChecks.Gs1); }
                catch (ArgumentException) { return false; }
                value = value.Substring(3, 7); break;
            case LinearBarcodeProfile.Identcode:
                if (value.Length != 12) return false; break;
            case LinearBarcodeProfile.Leitcode:
                if (value.Length != 14) return false; break;
            case LinearBarcodeProfile.Isbn:
            case LinearBarcodeProfile.Ismn:
                if (value.Length != 13) return false; break;
            case LinearBarcodeProfile.MarksAndSpencer:
                if (value.Length != 8 || value[0] != '0') return false; break;
            default: throw new ArgumentOutOfRangeException(nameof(profile));
        }
        try { identifier = Prepare(profile, value); return true; }
        catch (ArgumentException) { return false; }
    }

    private static string PrepareMarksAndSpencer(string value) {
        if (value.Length == 7) value = "0" + value;
        if (value.Length != 8 || value[0] != '0') throw new ArgumentException("M&S requires seven digits or an eight-digit EAN identifier beginning with zero.", nameof(value));
        return IdentifierChecks.WithCheck(value, 8, IdentifierChecks.Gs1);
    }
}

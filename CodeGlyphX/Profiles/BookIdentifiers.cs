using System;

namespace CodeGlyphX;

/// <summary>Owns conversion between publishing identifiers and their EAN representation.</summary>
internal static class BookIdentifiers {
    internal static string Isbn(string value) {
        if (value.Length == 9 || value.Length == 10) {
            var legacy = IdentifierChecks.WithCheck(value, 10, body => IdentifierChecks.Mod11(body, 10));
            var body = "978" + legacy.Substring(0, 9);
            return body + IdentifierChecks.Gs1(body);
        }
        if ((value.Length != 12 && value.Length != 13)
            || (!value.StartsWith("978", StringComparison.Ordinal) && !value.StartsWith("979", StringComparison.Ordinal)))
            throw new ArgumentException("ISBN requires an ISBN-10 or an ISBN-13 beginning with 978 or 979.", nameof(value));
        return IdentifierChecks.WithCheck(value, 13, IdentifierChecks.Gs1);
    }

    internal static string Ismn(string value) {
        if ((value.Length == 9 || value.Length == 10) && value[0] == 'M') {
            var body = "9790" + value.Substring(1, 8);
            var converted = body + IdentifierChecks.Gs1(body);
            IdentifierChecks.RequireDigits(body);
            if (value.Length == 10 && value[9] != converted[12]) throw new ArgumentException("The legacy ISMN check digit is incorrect.", nameof(value));
            return converted;
        }
        if (!value.StartsWith("9790", StringComparison.Ordinal)) throw new ArgumentException("ISMN requires a legacy M prefix or a 9790 prefix.", nameof(value));
        return IdentifierChecks.WithCheck(value, 13, IdentifierChecks.Gs1);
    }

    internal static string Issn(string value) => IdentifierChecks.WithCheck(value, 8, body => IdentifierChecks.Mod11(body, 8));
}

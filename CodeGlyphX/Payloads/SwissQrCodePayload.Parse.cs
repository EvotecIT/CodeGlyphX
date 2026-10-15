using System;
using System.Globalization;
using System.Text;

namespace CodeGlyphX.Payloads;

public sealed partial class SwissQrCodePayload {
    internal static bool TryParseBytes(byte[] bytes, out SwissQrCodePayload payload, out string text, bool legacy = false) {
        payload = null!;
        text = string.Empty;
        if (bytes.Length < 4 || bytes[0] != 'S' || bytes[1] != 'P' || bytes[2] != 'C' || (bytes[3] != 10 && bytes[3] != 13)) return false;
        try {
            text = new UTF8Encoding(false, true).GetString(bytes);
            return TryParse(text, out payload, legacy);
        } catch (DecoderFallbackException) { text = string.Empty; return false; }
    }
    /// <summary>Gets the validated creditor account.</summary>
    public Iban Account => _iban;
    /// <summary>Gets the creditor address.</summary>
    public Contact Creditor => _creditor;
    /// <summary>Gets the ultimate debtor, when supplied.</summary>
    public Contact? Debtor => _debitor;
    /// <summary>Gets the amount, or null when the payer supplies it.</summary>
    public decimal? Amount => _amount;
    /// <summary>Gets the payment currency.</summary>
    public SwissQrCurrency Currency => _currency;
    /// <summary>Gets the payment reference.</summary>
    public Reference PaymentReference => _reference;
    /// <summary>Gets the additional payment information.</summary>
    public AdditionalInformation Information => _additionalInformation;
    /// <summary>Gets the first alternative-procedure field.</summary>
    public string? AlternativeProcedure1 => _alternativeProcedure1;
    /// <summary>Gets the second alternative-procedure field.</summary>
    public string? AlternativeProcedure2 => _alternativeProcedure2;

    /// <summary>Parses and validates an SPC/0200/1 payload with LF or CRLF separators.</summary>
    /// <param name="value">Complete Swiss QR payload, without a byte-order mark or trailing newline.</param>
    /// <param name="payload">The typed payment fields when valid.</param>
    /// <param name="allowLegacyCombinedAddresses">Allows the combined address format used before the 2025 structured-address requirement.</param>
    public static bool TryParse(string value, out SwissQrCodePayload payload, bool allowLegacyCombinedAddresses = false) {
        payload = null!;
        if (value is null || value.Length > 997 || value.EndsWith("\n", StringComparison.Ordinal)) return false;
        var crlf = value.IndexOf('\r') >= 0;
        var normalized = crlf ? value.Replace("\r\n", "\n") : value;
        if (normalized.IndexOf('\r') >= 0 || (crlf && value.Replace("\r\n", "").IndexOf('\n') >= 0)) return false;
        var fields = normalized.Split('\n');
        if (fields.Length < 31 || fields.Length > 34 || fields[0] != "SPC" || fields[1] != "0200" || fields[2] != "1" || fields[30] != "EPD") return false;
        for (var i = 11; i < 18; i++) if (fields[i].Length != 0) return false;
        foreach (var field in fields) foreach (var c in field) if (!IsAllowedCharacter(c)) return false;
        if (fields[3].Length != 21 || fields[3].IndexOf(' ') >= 0 || fields[3].IndexOf('-') >= 0) return false;
        try {
            var iban = new Iban(fields[3], QrPayloadValidation.IsValidQrIban(fields[3]) ? SwissQrIbanType.QrIban : SwissQrIbanType.Iban);
            if (!TryReadContact(fields, 4, false, allowLegacyCombinedAddresses, out var creditor)
                || !TryReadContact(fields, 20, true, allowLegacyCombinedAddresses, out var debtor)) return false;
            decimal? amount = null;
            if (fields[18].Length > 0) {
                if (!decimal.TryParse(fields[18], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
                    || number.ToString("0.00", CultureInfo.InvariantCulture) != fields[18]) return false;
                amount = number;
            }
            if (fields[19] != "CHF" && fields[19] != "EUR") return false;
            var referenceType = fields[27] == "QRR" ? SwissQrReferenceType.QRR : fields[27] == "SCOR" ? SwissQrReferenceType.SCOR : SwissQrReferenceType.NON;
            if (fields[27] != "QRR" && fields[27] != "SCOR" && fields[27] != "NON") return false;
            var reference = new Reference(referenceType, fields[28].Length == 0 ? null : fields[28]);
            payload = new SwissQrCodePayload(iban, fields[19] == "CHF" ? SwissQrCurrency.CHF : SwissQrCurrency.EUR, creditor!, reference,
                new AdditionalInformation(fields[29], fields.Length > 31 ? fields[31] : null), debtor, amount,
                alternativeProcedure1: fields.Length > 32 ? fields[32] : null,
                alternativeProcedure2: fields.Length > 33 ? fields[33] : null);
            return true;
        } catch (ArgumentException) { return false; }
    }

    private static bool TryReadContact(string[] fields, int offset, bool optional, bool allowCombined, out Contact? contact) {
        contact = null;
        if (fields[offset].Length == 0) {
            if (!optional) return false;
            for (var i = 1; i < 7; i++) if (fields[offset + i].Length != 0) return false;
            return true;
        }
        if (fields[offset] == "S") {
            contact = Contact.CreateStructured(fields[offset + 1], fields[offset + 2], fields[offset + 3], fields[offset + 4], fields[offset + 5], fields[offset + 6]);
            return true;
        }
        if (allowCombined && fields[offset] == "K" && fields[offset + 4].Length == 0 && fields[offset + 5].Length == 0) {
            contact = Contact.CreateCombined(fields[offset + 1], fields[offset + 2], fields[offset + 3], fields[offset + 6]);
            return true;
        }
        return false;
    }

    private static string? CleanField(string? value, int maximumLength, string name) {
        if (value is null) return null;
        value = value.Replace("\r", "").Replace("\n", "");
        if (value.Length > maximumLength) throw new ArgumentException($"The field exceeds {maximumLength} characters.", name);
        foreach (var c in value) if (!IsAllowedCharacter(c)) throw new ArgumentException("The field contains a character outside the Swiss QR character set.", name);
        return value;
    }

    private static bool IsAllowedCharacter(char c) => (c >= '\u0020' && c <= '\u007e') || (c >= '\u00a0' && c <= '\u017f')
        || (c >= '\u0218' && c <= '\u021b') || c == '\u20ac';

    public sealed partial class Contact {
        /// <summary>Gets the structured or legacy combined address kind.</summary>
        public SwissQrAddressType AddressType => _addressType;
        /// <summary>Gets the party name.</summary>
        public string Name => _name;
        /// <summary>Gets the street or first legacy address line.</summary>
        public string? StreetOrAddressLine1 => _streetOrAddressLine1;
        /// <summary>Gets the building number or second legacy address line.</summary>
        public string? HouseNumberOrAddressLine2 => _houseNumberOrAddressLine2;
        /// <summary>Gets the postal code, empty for legacy combined addresses.</summary>
        public string ZipCode => _zipCode;
        /// <summary>Gets the town, empty for legacy combined addresses.</summary>
        public string City => _city;
        /// <summary>Gets the uppercase country code.</summary>
        public string Country => _country;
    }
}

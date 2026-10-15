using System;
using System.Globalization;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Payloads;

/// <summary>
/// Swiss QR bill payload.
/// </summary>
public sealed partial class SwissQrCodePayload {
    private const string Br = "\n";
    private readonly Iban _iban;
    private readonly Contact _creditor;
    private readonly AdditionalInformation _additionalInformation;
    private readonly decimal? _amount;
    private readonly SwissQrCurrency _currency;
    private readonly Contact? _debitor;
    private readonly Reference _reference;
    private readonly string? _alternativeProcedure1;
    private readonly string? _alternativeProcedure2;

    /// <summary>
    /// Creates a Swiss QR bill payload.
    /// </summary>
    public SwissQrCodePayload(
        Iban iban,
        SwissQrCurrency currency,
        Contact creditor,
        Reference reference,
        AdditionalInformation? additionalInformation = null,
        Contact? debitor = null,
        decimal? amount = null,
        Contact? ultimateCreditor = null,
        string? alternativeProcedure1 = null,
        string? alternativeProcedure2 = null) {
        _iban = iban ?? throw new ArgumentNullException(nameof(iban));
        _creditor = creditor ?? throw new ArgumentNullException(nameof(creditor));
        if (ultimateCreditor is not null) throw new ArgumentException("The ultimate creditor fields are reserved and must be empty.", nameof(ultimateCreditor));
        _additionalInformation = additionalInformation ?? new AdditionalInformation();
        if (amount.HasValue && (amount.Value < 0.01m || amount.Value > 999999999.99m || decimal.Round(amount.Value, 2) != amount.Value)) {
            throw new ArgumentException("The amount must be 0.01 through 999999999.99 with at most two decimal places.", nameof(amount));
        }
        if (currency != SwissQrCurrency.CHF && currency != SwissQrCurrency.EUR) throw new ArgumentOutOfRangeException(nameof(currency));
        _amount = amount;
        _currency = currency;
        _debitor = debitor;
        _reference = reference ?? throw new ArgumentNullException(nameof(reference));
        if (_iban.IsQrIban && _reference.RefType != SwissQrReferenceType.QRR) {
            throw new ArgumentException("If QR-IBAN is used, you have to choose \"QRR\" as reference type.", nameof(reference));
        }
        if (!_iban.IsQrIban && _reference.RefType == SwissQrReferenceType.QRR) {
            throw new ArgumentException("If non QR-IBAN is used, you have to choose either \"SCOR\" or \"NON\" as reference type.", nameof(reference));
        }
        if (alternativeProcedure1 is not null && alternativeProcedure1.Length > 100) {
            throw new ArgumentException("Alternative procedure information block 1 must be shorter than 101 chars.", nameof(alternativeProcedure1));
        }
        if (alternativeProcedure2 is not null && alternativeProcedure2.Length > 100) {
            throw new ArgumentException("Alternative procedure information block 2 must be shorter than 101 chars.", nameof(alternativeProcedure2));
        }
        _alternativeProcedure1 = CleanField(alternativeProcedure1, 100, nameof(alternativeProcedure1));
        _alternativeProcedure2 = CleanField(alternativeProcedure2, 100, nameof(alternativeProcedure2));
    }

    /// <summary>
    /// Converts this payload to a QR payload with recommended settings.
    /// </summary>
    public QrPayloadData ToPayloadData() {
        return new QrPayloadData(ToString(), QrErrorCorrectionLevel.M, textEncoding: QrTextEncoding.Utf8);
    }

    /// <summary>
    /// Returns the Swiss QR bill payload string.
    /// </summary>
    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("SPC").Append(Br);
        sb.Append("0200").Append(Br);
        sb.Append("1").Append(Br);
        sb.Append(_iban).Append(Br);
        sb.Append(_creditor.ToString());
        sb.Append(RepeatBr(7));
        sb.Append(_amount.HasValue ? _amount.Value.ToString("0.00", CultureInfo.InvariantCulture) : string.Empty).Append(Br);
        sb.Append(_currency).Append(Br);
        sb.Append(_debitor is null ? RepeatBr(7) : _debitor.ToString());
        sb.Append(_reference.RefType).Append(Br);
        var referenceText = _reference.ReferenceText;
        sb.Append(!string.IsNullOrEmpty(referenceText) ? referenceText : string.Empty).Append(Br);
        var unstructured = _additionalInformation.UnstructuredMessage;
        sb.Append(!string.IsNullOrEmpty(unstructured) ? unstructured : string.Empty).Append(Br);
        sb.Append(_additionalInformation.Trailer).Append(Br);
        var billInfo = _additionalInformation.BillInformation;
        if (!string.IsNullOrEmpty(billInfo) || !string.IsNullOrEmpty(_alternativeProcedure1) || !string.IsNullOrEmpty(_alternativeProcedure2)) {
            sb.Append(billInfo).Append(Br);
        }
        var alt1 = _alternativeProcedure1;
        if (!string.IsNullOrEmpty(alt1) || !string.IsNullOrEmpty(_alternativeProcedure2)) {
            sb.Append(alt1).Append(Br);
        }
        var alt2 = _alternativeProcedure2;
        if (!string.IsNullOrEmpty(alt2)) {
            sb.Append(alt2).Append(Br);
        }

        if (sb.Length > 0 && sb[sb.Length - 1] == '\n') {
            sb.Length -= 1;
        }
        return sb.ToString();
    }

    private static string RepeatBr(int count) {
        if (count <= 0) return string.Empty;
        return new string('\n', count);
    }

    /// <summary>
    /// Additional information section.
    /// </summary>
    public sealed class AdditionalInformation {
        private readonly string? _unstructuredMessage;
        private readonly string? _billInformation;

        /// <summary>
        /// Unstructured message (single line).
        /// </summary>
        public string? UnstructuredMessage => string.IsNullOrEmpty(_unstructuredMessage) ? null : _unstructuredMessage!.Replace("\n", "");

        /// <summary>
        /// Bill information (single line).
        /// </summary>
        public string? BillInformation => string.IsNullOrEmpty(_billInformation) ? null : _billInformation!.Replace("\n", "");

        /// <summary>
        /// Trailer marker.
        /// </summary>
        public string Trailer { get; } = "EPD";

        /// <summary>
        /// Creates an additional information block.
        /// </summary>
        public AdditionalInformation(string? unstructuredMessage = null, string? billInformation = null) {
            if ((unstructuredMessage?.Length ?? 0) + (billInformation?.Length ?? 0) > 140) {
                throw new ArgumentException("Unstructured message and bill information must be shorter than 141 chars in total.");
            }
            _unstructuredMessage = CleanField(unstructuredMessage, 140, nameof(unstructuredMessage));
            _billInformation = CleanField(billInformation, 140, nameof(billInformation));
        }
    }

    /// <summary>
    /// Reference section.
    /// </summary>
    public sealed class Reference {
        private readonly string? _reference;

        /// <summary>
        /// Reference type.
        /// </summary>
        public SwissQrReferenceType RefType { get; }

        /// <summary>
        /// Reference text (single line).
        /// </summary>
        public string? ReferenceText => string.IsNullOrEmpty(_reference) ? null : _reference!.Replace("\n", "");

        /// <summary>
        /// Creates a reference section.
        /// </summary>
        public Reference(SwissQrReferenceType referenceType, string? reference = null) {
            if (referenceType < SwissQrReferenceType.QRR || referenceType > SwissQrReferenceType.NON) throw new ArgumentOutOfRangeException(nameof(referenceType));
            RefType = referenceType;
            if (referenceType == SwissQrReferenceType.NON && reference != null) {
                throw new ArgumentException("Reference is only allowed when referenceType not equals \"NON\".");
            }
            if (referenceType != SwissQrReferenceType.NON && string.IsNullOrWhiteSpace(reference)) {
                throw new ArgumentException("Reference is required when referenceType is \"QRR\" or \"SCOR\".", nameof(reference));
            }
            if (referenceType == SwissQrReferenceType.QRR && reference != null) {
                if (!RegexCache.DigitsRequired().IsMatch(reference)) throw new ArgumentException("QR-reference must exist out of digits only.");
                if (reference.Length != 27) throw new ArgumentException("QR-references require exactly 27 digits.", nameof(reference));
                if (!QrPayloadValidation.ChecksumMod10(reference)) throw new ArgumentException("QR-reference is invalid. Checksum error.");
            }
            if (referenceType == SwissQrReferenceType.SCOR && reference != null && reference.Length > 25) {
                throw new ArgumentException("Creditor references (ISO 11649) have to be shorter than 26 chars.");
            }
            if (referenceType == SwissQrReferenceType.SCOR && !QrPayloadValidation.IsValidCreditorReference(reference))
                throw new ArgumentException("Invalid ISO 11649 creditor reference or Mod 97 check digits.", nameof(reference));
            _reference = reference?.ToUpperInvariant();
        }
    }

    /// <summary>
    /// IBAN section.
    /// </summary>
    public sealed class Iban {
        private readonly string _iban;
        private readonly SwissQrIbanType _ibanType;

        /// <summary>
        /// True when the IBAN is a QR-IBAN.
        /// </summary>
        public bool IsQrIban => _ibanType == SwissQrIbanType.QrIban;

        /// <summary>
        /// Creates an IBAN instance.
        /// </summary>
        public Iban(string iban, SwissQrIbanType ibanType) {
            if (iban is null) throw new ArgumentNullException(nameof(iban));
            if (ibanType != SwissQrIbanType.Iban && ibanType != SwissQrIbanType.QrIban) throw new ArgumentOutOfRangeException(nameof(ibanType));
            iban = iban.ToUpperInvariant().Replace("-", "").Replace(" ", "");
            if (iban.Length != 21) throw new ArgumentException("A Swiss QR account requires a 21-character CH or LI IBAN.", nameof(iban));
            if (ibanType == SwissQrIbanType.Iban && !QrPayloadValidation.IsValidIban(iban)) {
                throw new ArgumentException("The IBAN entered isn't valid.", nameof(iban));
            }
            if (ibanType == SwissQrIbanType.QrIban && !QrPayloadValidation.IsValidQrIban(iban)) {
                throw new ArgumentException("The QR-IBAN entered isn't valid.", nameof(iban));
            }
            if (!iban.StartsWith("CH", StringComparison.Ordinal) && !iban.StartsWith("LI", StringComparison.Ordinal)) {
                throw new ArgumentException("The IBAN must start with \"CH\" or \"LI\".", nameof(iban));
            }
            if ((ibanType == SwissQrIbanType.QrIban) != QrPayloadValidation.IsValidQrIban(iban))
                throw new ArgumentException("The declared account kind does not match its QR-IID range.", nameof(ibanType));
            _iban = iban;
            _ibanType = ibanType;
        }

        /// <summary>
        /// Returns the normalized IBAN.
        /// </summary>
        public override string ToString() {
            return _iban.Replace("-", "").Replace("\n", "").Replace(" ", "");
        }
    }

    /// <summary>
    /// Contact section.
    /// </summary>
    public sealed partial class Contact {
        private readonly string _name;
        private readonly string _zipCode;
        private readonly string _city;
        private readonly string _country;
        private readonly string? _streetOrAddressLine1;
        private readonly string? _houseNumberOrAddressLine2;
        private readonly SwissQrAddressType _addressType;

        /// <summary>
        /// Creates a contact with a structured address.
        /// </summary>
        public static Contact CreateStructured(string name, string street, string houseNumber, string zipCode, string city, string country) {
            return new Contact(name, zipCode, city, country, street, houseNumber, SwissQrAddressType.StructuredAddress);
        }

        /// <summary>
        /// Creates a contact with a combined address.
        /// </summary>
        public static Contact CreateCombined(string name, string addressLine1, string addressLine2, string country) {
            return new Contact(name, null, null, country, addressLine1, addressLine2, SwissQrAddressType.CombinedAddress);
        }

        private Contact(string name, string? zipCode, string? city, string country, string? streetOrAddressLine1, string? houseNumberOrAddressLine2, SwissQrAddressType addressType) {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name must not be empty.", nameof(name));
            if (name.Length > 70) throw new ArgumentException("Name must be shorter than 71 chars.", nameof(name));
            if (country is null || !IsoCountryCodes.ContainsAlpha2(country, ignoreCase: true))
                throw new ArgumentException("Country must be an assigned ISO 3166-1 two-letter code.", nameof(country));

            _name = CleanField(name, 70, nameof(name))!;
            if (_name.Length == 0) throw new ArgumentException("Name must not be empty.", nameof(name));
            _country = country.ToUpperInvariant();
            _addressType = addressType;
            _streetOrAddressLine1 = CleanField(streetOrAddressLine1, 70, nameof(streetOrAddressLine1));
            _houseNumberOrAddressLine2 = CleanField(houseNumberOrAddressLine2, addressType == SwissQrAddressType.StructuredAddress ? 16 : 70, nameof(houseNumberOrAddressLine2));

            if (_addressType == SwissQrAddressType.StructuredAddress) {
                if (string.IsNullOrEmpty(zipCode)) throw new ArgumentException("Zip code must not be empty.", nameof(zipCode));
                if (string.IsNullOrEmpty(city)) throw new ArgumentException("City must not be empty.", nameof(city));
                _zipCode = CleanField(zipCode, 16, nameof(zipCode))!;
                _city = CleanField(city, 35, nameof(city))!;
                if (_zipCode.Length == 0 || _city.Length == 0) throw new ArgumentException("Structured addresses require a postal code and town.");
            } else {
                if (string.IsNullOrEmpty(houseNumberOrAddressLine2)) throw new ArgumentException("Address line 2 must be provided for combined addresses.", nameof(houseNumberOrAddressLine2));
                _zipCode = string.Empty;
                _city = string.Empty;
            }
        }

        /// <summary>
        /// Returns the contact block string.
        /// </summary>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(_addressType == SwissQrAddressType.StructuredAddress ? "S" : "K").Append(Br);
            sb.Append(_name.Replace("\n", "")).Append(Br);
            var line1 = _streetOrAddressLine1;
            var line2 = _houseNumberOrAddressLine2;
            sb.Append(!string.IsNullOrEmpty(line1) ? line1!.Replace("\n", "") : string.Empty).Append(Br);
            sb.Append(!string.IsNullOrEmpty(line2) ? line2!.Replace("\n", "") : string.Empty).Append(Br);
            sb.Append(_zipCode.Replace("\n", "")).Append(Br);
            sb.Append(_city.Replace("\n", "")).Append(Br);
            sb.Append(_country).Append(Br);
            return sb.ToString();
        }

    }
}

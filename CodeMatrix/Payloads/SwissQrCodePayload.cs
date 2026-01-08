using System;
using System.Globalization;
using System.Text;
using CodeMatrix.Internal;

namespace CodeMatrix.Payloads;

/// <summary>
/// Swiss QR bill payload.
/// </summary>
public sealed class SwissQrCodePayload {
    private const string Br = "\n";
    private readonly Iban _iban;
    private readonly Contact _creditor;
    private readonly Contact? _ultimateCreditor;
    private readonly AdditionalInformation _additionalInformation;
    private readonly decimal? _amount;
    private readonly QrSwissCurrency _currency;
    private readonly Contact? _debitor;
    private readonly Reference _reference;
    private readonly string? _alternativeProcedure1;
    private readonly string? _alternativeProcedure2;

    /// <summary>
    /// Creates a Swiss QR bill payload.
    /// </summary>
    public SwissQrCodePayload(
        Iban iban,
        QrSwissCurrency currency,
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
        _ultimateCreditor = ultimateCreditor;
        _additionalInformation = additionalInformation ?? new AdditionalInformation();
        if (amount.HasValue && amount.Value.ToString(CultureInfo.InvariantCulture).Length > 12) {
            throw new ArgumentException("Amount (including decimals) must be shorter than 13 places.", nameof(amount));
        }
        _amount = amount;
        _currency = currency;
        _debitor = debitor;
        _reference = reference ?? throw new ArgumentNullException(nameof(reference));
        if (_iban.IsQrIban && _reference.RefType != Reference.ReferenceType.QRR) {
            throw new ArgumentException("If QR-IBAN is used, you have to choose \"QRR\" as reference type.", nameof(reference));
        }
        if (!_iban.IsQrIban && _reference.RefType == Reference.ReferenceType.QRR) {
            throw new ArgumentException("If non QR-IBAN is used, you have to choose either \"SCOR\" or \"NON\" as reference type.", nameof(reference));
        }
        if (alternativeProcedure1 is not null && alternativeProcedure1.Length > 100) {
            throw new ArgumentException("Alternative procedure information block 1 must be shorter than 101 chars.", nameof(alternativeProcedure1));
        }
        if (alternativeProcedure2 is not null && alternativeProcedure2.Length > 100) {
            throw new ArgumentException("Alternative procedure information block 2 must be shorter than 101 chars.", nameof(alternativeProcedure2));
        }
        _alternativeProcedure1 = alternativeProcedure1;
        _alternativeProcedure2 = alternativeProcedure2;
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
        sb.Append(_amount.HasValue ? $"{_amount:0.00}".Replace(",", ".") : string.Empty).Append(Br);
        sb.Append(_currency).Append(Br);
        sb.Append(_debitor is null ? RepeatBr(7) : _debitor.ToString());
        sb.Append(_reference.RefType).Append(Br);
        var referenceText = _reference.ReferenceText;
        sb.Append(!string.IsNullOrEmpty(referenceText) ? referenceText : string.Empty).Append(Br);
        var unstructured = _additionalInformation.UnstructuredMessage;
        sb.Append(!string.IsNullOrEmpty(unstructured) ? unstructured : string.Empty).Append(Br);
        sb.Append(_additionalInformation.Trailer).Append(Br);
        var billInfo = _additionalInformation.BillInformation;
        if (!string.IsNullOrEmpty(billInfo)) {
            sb.Append(billInfo).Append(Br);
        }
        var alt1 = _alternativeProcedure1;
        if (!string.IsNullOrEmpty(alt1)) {
            sb.Append(alt1!.Replace("\n", "")).Append(Br);
        }
        var alt2 = _alternativeProcedure2;
        if (!string.IsNullOrEmpty(alt2)) {
            sb.Append(alt2!.Replace("\n", "")).Append(Br);
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
            _unstructuredMessage = unstructuredMessage;
            _billInformation = billInformation;
        }
    }

    /// <summary>
    /// Reference section.
    /// </summary>
    public sealed class Reference {
        /// <summary>
        /// Reference type.
        /// </summary>
        public enum ReferenceType {
            /// <summary>
            /// QR reference (QRR).
            /// </summary>
            QRR,
            /// <summary>
            /// Creditor reference (SCOR).
            /// </summary>
            SCOR,
            /// <summary>
            /// No reference (NON).
            /// </summary>
            NON
        }

        /// <summary>
        /// Reference text type.
        /// </summary>
        public enum ReferenceTextType {
            /// <summary>
            /// QR reference text.
            /// </summary>
            QrReference,
            /// <summary>
            /// Creditor reference ISO 11649.
            /// </summary>
            CreditorReferenceIso11649
        }

        private readonly string? _reference;
        private readonly ReferenceTextType? _referenceTextType;

        /// <summary>
        /// Reference type.
        /// </summary>
        public ReferenceType RefType { get; }

        /// <summary>
        /// Reference text (single line).
        /// </summary>
        public string? ReferenceText => string.IsNullOrEmpty(_reference) ? null : _reference!.Replace("\n", "");

        /// <summary>
        /// Creates a reference section.
        /// </summary>
        public Reference(ReferenceType referenceType, string? reference = null, ReferenceTextType? referenceTextType = null) {
            RefType = referenceType;
            _referenceTextType = referenceTextType;
            if (referenceType == ReferenceType.NON && reference != null) {
                throw new ArgumentException("Reference is only allowed when referenceType not equals \"NON\".");
            }
            if (referenceType != ReferenceType.NON && reference != null && !referenceTextType.HasValue) {
                throw new ArgumentException("ReferenceTextType must be set when using the reference text.");
            }
            if (referenceTextType == ReferenceTextType.QrReference && reference != null) {
                if (reference.Length > 27) throw new ArgumentException("QR-references have to be shorter than 28 chars.");
                if (!RegexCache.DigitsRequired().IsMatch(reference)) throw new ArgumentException("QR-reference must exist out of digits only.");
                if (!QrPayloadValidation.ChecksumMod10(reference)) throw new ArgumentException("QR-reference is invalid. Checksum error.");
            }
            if (referenceTextType == ReferenceTextType.CreditorReferenceIso11649 && reference != null && reference.Length > 25) {
                throw new ArgumentException("Creditor references (ISO 11649) have to be shorter than 26 chars.");
            }
            _reference = reference;
        }
    }

    /// <summary>
    /// IBAN section.
    /// </summary>
    public sealed class Iban {
        /// <summary>
        /// IBAN type.
        /// </summary>
        public enum IbanType {
            /// <summary>
            /// Standard IBAN.
            /// </summary>
            Iban,
            /// <summary>
            /// QR-IBAN.
            /// </summary>
            QrIban
        }

        private readonly string _iban;
        private readonly IbanType _ibanType;

        /// <summary>
        /// True when the IBAN is a QR-IBAN.
        /// </summary>
        public bool IsQrIban => _ibanType == IbanType.QrIban;

        /// <summary>
        /// Creates an IBAN instance.
        /// </summary>
        public Iban(string iban, IbanType ibanType) {
            if (ibanType == IbanType.Iban && !QrPayloadValidation.IsValidIban(iban)) {
                throw new ArgumentException("The IBAN entered isn't valid.", nameof(iban));
            }
            if (ibanType == IbanType.QrIban && !QrPayloadValidation.IsValidQrIban(iban)) {
                throw new ArgumentException("The QR-IBAN entered isn't valid.", nameof(iban));
            }
            if (!iban.StartsWith("CH", StringComparison.Ordinal) && !iban.StartsWith("LI", StringComparison.Ordinal)) {
                throw new ArgumentException("The IBAN must start with \"CH\" or \"LI\".", nameof(iban));
            }
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
    public sealed class Contact {
        /// <summary>
        /// Contact address type.
        /// </summary>
        public enum AddressType {
            /// <summary>
            /// Structured address.
            /// </summary>
            StructuredAddress,
            /// <summary>
            /// Combined address.
            /// </summary>
            CombinedAddress
        }

        private readonly string _name;
        private readonly string _zipCode;
        private readonly string _city;
        private readonly string _country;
        private readonly string? _streetOrAddressLine1;
        private readonly string? _houseNumberOrAddressLine2;
        private readonly AddressType _addressType;

        /// <summary>
        /// Creates a contact with a structured address.
        /// </summary>
        public static Contact CreateStructured(string name, string street, string houseNumber, string zipCode, string city, string country) {
            return new Contact(name, zipCode, city, country, street, houseNumber, AddressType.StructuredAddress);
        }

        /// <summary>
        /// Creates a contact with a combined address.
        /// </summary>
        public static Contact CreateCombined(string name, string addressLine1, string addressLine2, string country) {
            return new Contact(name, null, null, country, addressLine1, addressLine2, AddressType.CombinedAddress);
        }

        private Contact(string name, string? zipCode, string? city, string country, string? streetOrAddressLine1, string? houseNumberOrAddressLine2, AddressType addressType) {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name must not be empty.", nameof(name));
            if (name.Length > 70) throw new ArgumentException("Name must be shorter than 71 chars.", nameof(name));
            if (!IsTwoLetterCode(country)) throw new ArgumentException("Country must be a valid two-letter code.", nameof(country));

            _name = name;
            _country = country.ToUpperInvariant();
            _addressType = addressType;
            _streetOrAddressLine1 = streetOrAddressLine1;
            _houseNumberOrAddressLine2 = houseNumberOrAddressLine2;

            if (_addressType == AddressType.StructuredAddress) {
                if (string.IsNullOrEmpty(zipCode)) throw new ArgumentException("Zip code must not be empty.", nameof(zipCode));
                if (string.IsNullOrEmpty(city)) throw new ArgumentException("City must not be empty.", nameof(city));
                _zipCode = zipCode!;
                _city = city!;
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
            sb.Append(_addressType == AddressType.StructuredAddress ? "S" : "K").Append(Br);
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

        private static bool IsTwoLetterCode(string code) {
            return RegexCache.CountryCode().IsMatch(code ?? string.Empty);
        }
    }
}

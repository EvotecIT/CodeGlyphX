using System;
using CodeGlyphX.DataBar;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>Builds and reads GS1 AI 8110 coupons using the existing GS1 validator and DataBar Expanded carrier.</summary>
public static class NorthAmericanCoupon {
    /// <summary>Builds the mandatory coupon fields and validates any additional optional fields with the GS1 rules.</summary>
    /// <param name="companyPrefix">Six through twelve company-prefix digits.</param>
    /// <param name="offerCode">Six offer-code digits.</param>
    /// <param name="saveValue">One through five digits in the coupon's prescribed value units.</param>
    /// <param name="purchaseRequirement">One through five purchase-requirement digits.</param>
    /// <param name="purchaseRequirementCode">The primary purchase-requirement code, 0..4 or 9.</param>
    /// <param name="familyCode">Three primary purchase-family digits.</param>
    /// <param name="additionalFields">Optional AI 8110 field groups, including their field identifiers and length indicators.</param>
    public static string Prepare(string companyPrefix, string offerCode, string saveValue, string purchaseRequirement,
        char purchaseRequirementCode, string familyCode, string additionalFields = "") {
        RequireDigits(companyPrefix, 6, 12, nameof(companyPrefix));
        RequireDigits(offerCode, 6, 6, nameof(offerCode));
        RequireDigits(saveValue, 1, 5, nameof(saveValue));
        RequireDigits(purchaseRequirement, 1, 5, nameof(purchaseRequirement));
        RequireDigits(familyCode, 3, 3, nameof(familyCode));
        if (additionalFields is null) throw new ArgumentNullException(nameof(additionalFields));
        var value = "8110" + (char)('0' + companyPrefix.Length - 6) + companyPrefix + offerCode
            + (char)('0' + saveValue.Length) + saveValue + (char)('0' + purchaseRequirement.Length) + purchaseRequirement
            + purchaseRequirementCode + familyCode + additionalFields;
        if (!TryParse(value, out _)) throw new ArgumentException("Invalid GS1 North American coupon fields.");
        return value;
    }

    /// <summary>Encodes a complete AI 8110 element string as a DataBar Expanded Stacked symbol.</summary>
    public static MatrixSymbol Encode(string value, int columns = 2) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (!TryParse(value, out var coupon)) throw new ArgumentException("A coupon requires one valid AI 8110 element.", nameof(value));
        return new MatrixSymbol(SymbolFormat.Gs1DataBarExpandedStacked, DataBarExpandedEncoder.EncodeExpandedStacked(coupon.ElementString, columns));
    }

    /// <summary>Renders the checked coupon carrier with its company-prefix and offer-code caption.</summary>
    public static RenderedOutput Render(string value, OutputFormat format, int columns = 2,
        MatrixOptions? options = null, OutputOptions? outputOptions = null) {
        if (!TryParse(value, out var coupon)) throw new ArgumentException("A coupon requires one valid AI 8110 element.", nameof(value));
        var render = options?.Clone() ?? new MatrixOptions();
        render.LabelText ??= coupon.HumanReadable;
        return Encode(value, columns).Render(format, render, outputOptions);
    }

    /// <summary>Decodes a DataBar Expanded Stacked symbol and validates its coupon fields.</summary>
    public static bool TryDecode(BitMatrix modules, out NorthAmericanCouponData coupon) {
        coupon = null!;
        return modules != null && DataBarExpandedDecoder.TryDecodeExpandedStacked(modules, out var text) && TryParse(text, out coupon);
    }

    /// <summary>Parses a raw or parenthesized AI 8110 message; unrelated or additional AIs are rejected.</summary>
    public static bool TryParse(string value, out NorthAmericanCouponData coupon) {
        coupon = null!;
        if (value is null || !Gs1.TryValidate(value, out var result) || result.Elements.Count != 1 || result.Elements[0].Ai != "8110") return false;
        var data = result.Elements[0].Data;
        var prefixLength = data[0] - '0' + 6;
        var prefix = data.Substring(1, prefixLength);
        var offer = data.Substring(1 + prefixLength, 6);
        coupon = new NorthAmericanCouponData(data, prefix, offer);
        return true;
    }

    private static void RequireDigits(string value, int min, int max, string name) {
        if (value is null) throw new ArgumentNullException(name);
        if (value.Length < min || value.Length > max) throw new ArgumentException($"The field requires {min} through {max} digits.", name);
        IdentifierChecks.RequireDigits(value);
    }
}

/// <summary>A validated AI 8110 coupon and its conventional company-prefix/offer label.</summary>
public sealed class NorthAmericanCouponData {
    /// <summary>Gets the complete AI 8110 data field.</summary>
    public string Data { get; }
    /// <summary>Gets the company prefix.</summary>
    public string CompanyPrefix { get; }
    /// <summary>Gets the six-digit offer code.</summary>
    public string OfferCode { get; }
    /// <summary>Gets the machine-readable AI 8110 element string.</summary>
    public string ElementString => "8110" + Data;
    /// <summary>Gets the conventional company-prefix and offer label.</summary>
    public string HumanReadable => CompanyPrefix + "-" + OfferCode;
    internal NorthAmericanCouponData(string data, string prefix, string offer) { Data = data; CompanyPrefix = prefix; OfferCode = offer; }
}

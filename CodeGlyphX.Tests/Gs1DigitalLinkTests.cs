using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Gs1DigitalLinkTests {
    // Normative and informative conformance examples from GS1 Digital Link URI Syntax 1.6.0,
    // ratified March 2025, sections 4.12 and 5.
    public static IEnumerable<object[]> OfficialUriExamples() {
        yield return Example(
            "https://id.gs1.org/01/09520123456788",
            "https://id.gs1.org/01/09520123456788",
            "0109520123456788");
        yield return Example(
            "https://id.gs1.org/01/09520123456788/10/ABC123",
            "https://id.gs1.org/01/09520123456788/10/ABC123",
            "010952012345678810ABC123");
        yield return Example(
            "https://id.gs1.org/01/09520123456788/10/ABC1/21/12345?17=180426",
            "https://id.gs1.org/01/09520123456788/10/ABC1/21/12345?17=180426",
            "010952012345678810ABC1" + Gs1.GroupSeparator + "2112345" + Gs1.GroupSeparator + "17180426");
        yield return Example(
            "https://id.gs1.org/414/9520123456788/254/32a%2Fb",
            "https://id.gs1.org/414/9520123456788/254/32a%2Fb",
            "414952012345678825432a/b");
        yield return Example(
            "https://example.com/01/09520123456788?3103=000195&3922=0299&17=201225",
            "https://id.gs1.org/01/09520123456788?17=201225&3103=000195&3922=0299",
            "0109520123456788310300019539220299" + Gs1.GroupSeparator + "17201225");
        yield return Example(
            "https://id.gs1.org/00/195201234567891232",
            "https://id.gs1.org/00/195201234567891232",
            "00195201234567891232");
    }

    [Theory]
    [MemberData(nameof(OfficialUriExamples))]
    public void Parse_MatchesOfficialUriSyntaxExamples(string uri, string canonical, string elementString) {
        var result = Gs1DigitalLink.Parse(uri);

        Assert.Equal("1.6.0", Gs1DigitalLink.SyntaxVersion);
        Assert.Equal(canonical, result.CanonicalUri);
        Assert.Equal(elementString, result.ToElementString());
        Assert.Equal(uri == canonical, result.IsCanonical);
        Assert.True(result.PrimaryIdentifier.Definition!.IsDigitalLinkPrimaryKey);
    }

    [Fact]
    public void Parse_CustomStemExtensionsAndFragment_PreservesWebDataAndCanonicalizesGs1Data() {
        const string uri = "http://brand.example/links/414/9520123456788/254/32a%2fb?linkType=gs1%3Atraceability&label=caf%C3%A9#details";

        var result = Gs1.ParseDigitalLink(uri);

        Assert.Equal("http://brand.example/links", result.UriStem);
        Assert.Equal("414", result.PrimaryIdentifier.Ai);
        Assert.Equal("32a/b", Assert.Single(result.KeyQualifiers).Data);
        Assert.Empty(result.DataAttributes);
        Assert.Equal("gs1:traceability", result.ExtensionParameters["linkType"]);
        Assert.Equal("café", result.ExtensionParameters["label"]);
        Assert.Equal("details", result.Fragment);
        Assert.False(result.IsCanonical);
        Assert.Equal("https://id.gs1.org/414/9520123456788/254/32a%2Fb", result.CanonicalUri);
    }

    [Fact]
    public void Parse_UsesOriginalUriSpellingWhenDeterminingCanonicality() {
        const string input = "HTTPS://ID.GS1.ORG/01/09520123456788";

        var result = Gs1DigitalLink.Parse(input);

        Assert.Equal(input, result.OriginalUri);
        Assert.Equal(input, result.ToString());
        Assert.Equal("https://id.gs1.org/01/09520123456788", result.CanonicalUri);
        Assert.False(result.IsCanonical);
    }

    [Fact]
    public void BuildCanonical_PlacesQualifiersInPathAndSortsAttributesLexically() {
        var result = Gs1DigitalLink.BuildCanonical(new[] {
            Gs1Element.Create("3922", "0299"),
            Gs1Element.Create("01", "09520123456788"),
            Gs1Element.Create("3103", "000195"),
            Gs1Element.Create("17", "201225")
        });

        Assert.Equal(
            "https://id.gs1.org/01/09520123456788?17=201225&3103=000195&3922=0299",
            result.Uri.AbsoluteUri);
        Assert.True(result.IsCanonical);
        Assert.Equal(new[] { "17", "3103", "3922" }, result.DataAttributes.Select(element => element.Ai));
    }

    [Fact]
    public void Build_CustomStemEscapingExtensionsAndFragment_RoundTrips() {
        var result = Gs1DigitalLink.Build(
            "https://brand.example/root/",
            new[] {
                Gs1Element.Create("414", "9520123456788"),
                Gs1Element.Create("254", "32a/b")
            },
            extensionParameters: new Dictionary<string, string> {
                ["linkType"] = "gs1:traceability"
            },
            fragment: "section 1");

        Assert.Equal(
            "https://brand.example/root/414/9520123456788/254/32a%2Fb?linkType=gs1%3Atraceability#section%201",
            result.Uri.AbsoluteUri);
        Assert.Equal("https://brand.example/root", result.UriStem);
        Assert.Equal("https://id.gs1.org/414/9520123456788/254/32a%2Fb", result.CanonicalUri);
    }

    [Fact]
    public void Build_MultiplePrimaryKeys_UsesTheExplicitAuthorityAndQueriesTheOtherKey() {
        var result = Gs1DigitalLink.BuildCanonical(
            new[] {
                Gs1Element.Create("01", "09520123456788"),
                Gs1Element.Create("8004", "9520614141234567")
            },
            primaryIdentifierAi: "8004");

        Assert.Equal(
            "https://id.gs1.org/8004/9520614141234567?01=09520123456788",
            result.Uri.AbsoluteUri);
        Assert.Equal("8004", result.PrimaryIdentifier.Ai);
        Assert.Equal("01", Assert.Single(result.DataAttributes).Ai);
    }

    [Fact]
    public void Build_RequiredPayToReference_PlacesQualifierInPath() {
        var result = Gs1DigitalLink.BuildCanonical(new[] {
            Gs1Element.Create("415", "9520123456788"),
            Gs1Element.Create("8020", "REF-123")
        });

        Assert.Equal(
            "https://id.gs1.org/415/9520123456788/8020/REF-123",
            result.Uri.AbsoluteUri);
        Assert.Equal("8020", Assert.Single(result.KeyQualifiers).Ai);
    }

    [Theory]
    [InlineData(".", "%2E")]
    [InlineData("..", "%2E%2E")]
    public void Build_DotOnlyPathValuesRemainGs1Data(string value, string escapedValue) {
        var result = Gs1DigitalLink.BuildCanonical(new[] {
            Gs1Element.Create("01", "09520123456788"),
            Gs1Element.Create("10", value)
        });

        var expected = "https://id.gs1.org/01/09520123456788/10/" + escapedValue;
        Assert.Equal(expected, result.CanonicalUri);
        Assert.Equal(expected, result.OriginalUri);
        Assert.Equal(value, Assert.Single(result.KeyQualifiers).Data);
        Assert.Equal("010952012345678810" + value, result.ToElementString());
        Assert.True(result.IsCanonical);
    }

    [Fact]
    public void Parse_QuerySupportsBothStandardDelimitersWithoutFormUrlDecoding() {
        var result = Gs1DigitalLink.Parse(
            "https://id.gs1.org/01/09520123456788/10/A%2BB?17=201225;3103=000195");

        Assert.Equal("A+B", Assert.Single(result.KeyQualifiers).Data);
        Assert.Equal("201225", result.DataAttributes.Single(element => element.Ai == "17").Data);
        Assert.Equal("000195", result.DataAttributes.Single(element => element.Ai == "3103").Data);
        Assert.Equal(
            "https://id.gs1.org/01/09520123456788/10/A%2BB?17=201225&3103=000195",
            result.CanonicalUri);
    }

    [Theory]
    [InlineData("https://id.gs1.org/gtin/09520123456788", Gs1DigitalLinkIssueCode.MissingPrimaryIdentifier)]
    [InlineData("https://id.gs1.org/01/0952012345678", Gs1DigitalLinkIssueCode.InvalidApplicationIdentifier)]
    [InlineData("https://id.gs1.org/01/09520123456788/21/123/10/LOT", Gs1DigitalLinkIssueCode.InvalidPath)]
    [InlineData("https://id.gs1.org/01/09520123456788?10=LOT", Gs1DigitalLinkIssueCode.InvalidQuery)]
    [InlineData("https://id.gs1.org/01/09520123456788?236=12098", Gs1DigitalLinkIssueCode.UnknownApplicationIdentifier)]
    [InlineData("https://id.gs1.org/01/09520123456788?12345=12098", Gs1DigitalLinkIssueCode.InvalidExtensionParameter)]
    [InlineData("https://id.gs1.org/01/09520123456788?17=201225&17=201226", Gs1DigitalLinkIssueCode.DuplicateApplicationIdentifier)]
    [InlineData("https://id.gs1.org/01/09520123456788%ZZ", Gs1DigitalLinkIssueCode.InvalidPercentEncoding)]
    [InlineData("https://id.gs1.org/01/09520123456788/", Gs1DigitalLinkIssueCode.InvalidPath)]
    [InlineData("https://id.gs1.org/01/09520123456788?", Gs1DigitalLinkIssueCode.InvalidQuery)]
    [InlineData("https://id.gs1.org/01/09520123456788?linkType=café", Gs1DigitalLinkIssueCode.MalformedUri)]
    public void Validate_RejectsNonConformantUriStructures(string uri, Gs1DigitalLinkIssueCode expected) {
        var result = Gs1.ValidateDigitalLink(uri);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == expected);
        Assert.False(Gs1DigitalLink.TryParse(uri, out _));
    }

    [Fact]
    public void Build_RejectsDuplicateAlternativeQualifiersAndNumericExtensions() {
        Assert.Throws<FormatException>(() => Gs1DigitalLink.BuildCanonical(new[] {
            Gs1Element.Create("01", "09520123456788"),
            Gs1Element.Create("21", "SERIAL"),
            Gs1Element.Create("235", "TPX")
        }));

        Assert.Throws<FormatException>(() => Gs1DigitalLink.Build(
            "https://example.com",
            new[] { Gs1Element.Create("01", "09520123456788") },
            extensionParameters: new Dictionary<string, string> { ["12345"] = "value" }));

        Assert.Throws<ArgumentException>(() => Gs1DigitalLink.Build(
            "https://example.com?",
            new[] { Gs1Element.Create("01", "09520123456788") }));

        Assert.Throws<ArgumentException>(() => Gs1DigitalLink.Build(
            "https://example.com/café",
            new[] { Gs1Element.Create("01", "09520123456788") }));
    }

    [Fact]
    public void Validation_ExposesUnderlyingGs1SemanticFailures() {
        var result = Gs1DigitalLink.Validate("https://id.gs1.org/01/09520123456789");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ElementValidation);
        Assert.False(result.ElementValidation!.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == Gs1DigitalLinkIssueCode.InvalidApplicationIdentifier);
        Assert.Throws<FormatException>(() => Gs1DigitalLink.Parse("https://id.gs1.org/01/09520123456789"));
    }

    private static object[] Example(string uri, string canonical, string elementString) {
        return new object[] { uri, canonical, elementString };
    }
}

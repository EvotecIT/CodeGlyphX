using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Gs1Data;
using Xunit;

namespace CodeGlyphX.Tests;

public class Gs1CatalogTests {
    [Fact]
    public void Catalog_ContainsEveryExpandedIdentifierFromOfficialRelease() {
        Assert.Equal("2026-01-27", Gs1ApplicationIdentifierCatalog.Release);
        Assert.Equal(541, Gs1ApplicationIdentifierCatalog.All.Count);
        Assert.Equal(541, Gs1ApplicationIdentifierCatalog.All.Select(x => x.Ai).Distinct().Count());

        var lot = Gs1ApplicationIdentifierCatalog.Get("10");
        Assert.Equal("BATCH/LOT", lot.Title);
        Assert.Equal(1, lot.MinimumDataLength);
        Assert.Equal(20, lot.MaximumDataLength);
        Assert.True(lot.RequiresFnc1Separator);

        var weight = Gs1ApplicationIdentifierCatalog.Get("3103");
        Assert.Equal("N6", weight.Format);
        Assert.True(weight.HasPredefinedLength);
        Assert.True(weight.IsDigitalLinkDataAttribute);

        var gdti = Gs1ApplicationIdentifierCatalog.Get("253");
        Assert.Equal(13, gdti.MinimumDataLength);
        Assert.Equal(30, gdti.MaximumDataLength);
        Assert.Equal(2, gdti.Components.Count);
        Assert.True(gdti.Components[1].IsOptional);
        Assert.True(gdti.IsDigitalLinkPrimaryKey);
    }

    [Fact]
    public void Catalog_ExposesAssociationAndDigitalLinkMetadata() {
        var gtin = Gs1ApplicationIdentifierCatalog.Get("01");
        Assert.Contains("255", gtin.ExcludedAssociations);
        Assert.Contains("37", gtin.ExcludedAssociations);
        Assert.Equal("22,10,21|235", gtin.DigitalLinkPrimaryKeyQualifiers);

        var lot = Gs1ApplicationIdentifierCatalog.Get("10");
        Assert.Contains("01,02,03,8006,8026", lot.RequiredAssociations);
        Assert.True(lot.IsDigitalLinkDataAttribute);
    }

    [Fact]
    public void Catalog_EveryReferencedSemanticRuleHasAManagedImplementation() {
        var ruleNames = Gs1ApplicationIdentifierCatalog.All
            .SelectMany(definition => definition.Components)
            .SelectMany(component => component.Linters)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(rule => rule, StringComparer.Ordinal)
            .ToArray();
        var definition = Gs1ApplicationIdentifierCatalog.Get("90");

        Assert.Equal(34, ruleNames.Length);
        foreach (var rule in ruleNames) {
            Assert.True(
                Gs1Validator.TryApplySemanticRule(rule, string.Empty, definition, 0, new List<Gs1ValidationIssue>()),
                $"GS1 semantic rule '{rule}' is not implemented.");
        }
    }

    [Fact]
    public void Validate_ParsesBracketedAndRawRepresentations() {
        const string bracketed = "(01)09506000134352(10)ABC123(17)240101";
        var result = Gs1.Validate(bracketed);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues));
        Assert.True(result.HasCompleteSemanticValidation);
        Assert.Equal(3, result.Elements.Count);
        Assert.Equal("BATCH/LOT", result.Elements[1].Definition!.Title);

        var raw = Gs1Validator.ToElementString(bracketed);
        Assert.Equal("010950600013435210ABC123" + Gs1.GroupSeparator + "17240101", raw);

        var decoded = Gs1.Validate(raw);
        Assert.True(decoded.IsValid, string.Join(Environment.NewLine, decoded.Issues));
        Assert.Equal(result.Elements.Select(x => (x.Ai, x.Data)), decoded.Elements.Select(x => (x.Ai, x.Data)));
    }

    [Theory]
    [InlineData("(01)09506000134353", Gs1ValidationIssueCode.InvalidData)]
    [InlineData("(01)09506000134352(17)241332", Gs1ValidationIssueCode.InvalidData)]
    [InlineData("(01)09506000134352(10)ABC DEF", Gs1ValidationIssueCode.InvalidCharacter)]
    [InlineData("(1234)ABC", Gs1ValidationIssueCode.UnknownApplicationIdentifier)]
    [InlineData("(10)ABC", Gs1ValidationIssueCode.MissingRequiredApplicationIdentifier)]
    public void Validate_ReportsActionableFailures(string input, Gs1ValidationIssueCode expected) {
        var result = Gs1.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == expected);
    }

    [Fact]
    public void Validate_EnforcesMutuallyExclusiveAiRules() {
        var result = Gs1.Validate("(01)09506000134352(37)1");

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == Gs1ValidationIssueCode.MutuallyExclusiveApplicationIdentifiers);
    }

    [Fact]
    public void Validate_DetectsUnnecessaryAndMissingSeparators() {
        var unexpected = Gs1.Validate("0109506000134352" + Gs1.GroupSeparator + "17240101");
        Assert.Contains(unexpected.Issues, issue => issue.Code == Gs1ValidationIssueCode.UnexpectedFnc1Separator);

        var missing = Gs1.Validate("10" + new string('A', 20) + "17240101");
        Assert.Contains(missing.Issues, issue => issue.Code == Gs1ValidationIssueCode.DataTooLong);
    }

    [Theory]
    [InlineData("10LOT\u001D")]
    [InlineData("(10)LOT\u001D")]
    [InlineData("0109506000134352\u001D")]
    [InlineData("(01)09506000134352\u001D")]
    public void Validate_RejectsTrailingFnc1Separators(string input) {
        var result = Gs1.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Code == Gs1ValidationIssueCode.MalformedInput
            && issue.Position == input.Length - 1);
    }

    [Fact]
    public void Validate_AppliesOfficialCodeListsAndIbanChecksum() {
        var valid = Gs1.Validate("(415)1234567890128(8020)REF42(8007)PL10105000997603123456789123");
        Assert.True(valid.IsValid, string.Join(Environment.NewLine, valid.Issues));

        var invalid = Gs1.Validate("(415)1234567890128(8020)REF42(8007)PL10105000997603123456789124");
        Assert.Contains(invalid.Issues, issue => issue.Ai == "8007" && issue.Code == Gs1ValidationIssueCode.InvalidData);
    }

    [Theory]
    [InlineData("(00)123456789012345675(7041)BX")]
    [InlineData("(8110)012345612345611110123")]
    [InlineData("(8112)001234561234560123456")]
    public void Validate_AppliesGeneratedAndSpecializedSemanticRules(string input) {
        var result = Gs1.Validate(input);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues));
        Assert.True(result.HasCompleteSemanticValidation);
    }

    [Fact]
    public void ElementString_PreservesLegacyPermissiveBuilderContract() {
        Assert.Equal("240", Gs1.ElementString("(240)"));
        Assert.Equal("91Hello World!", Gs1.ElementString("(91)Hello World!"));
        Assert.Equal("010950600013435210LOT", Gs1.ElementString("(01)09506000134352|(10)LOT"));
        Assert.Equal("CUSTOM" + Gs1.GroupSeparator + "RAW", Gs1.ElementString("CUSTOM|RAW"));

        var unknown = Gs1.ElementString("(1234)ABC(10)LOT");
        Assert.Equal("1234ABC" + Gs1.GroupSeparator + "10LOT", unknown);
    }

    [Fact]
    public void ExplicitElements_CanUseCatalogOrExpertLengthSemantics() {
        var catalogElement = Gs1Element.Create("10", "LOT42");
        var fixedElement = Gs1Element.Fixed("99", "INTERNAL");
        var value = Gs1.ElementString(catalogElement, fixedElement);

        Assert.Equal("10LOT42" + Gs1.GroupSeparator + "99INTERNAL", value);
        Assert.NotNull(catalogElement.Definition);
        Assert.Null(fixedElement.Definition);
    }
}

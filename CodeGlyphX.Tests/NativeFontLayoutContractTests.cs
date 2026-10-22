using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Internal.Fonts;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class NativeFontLayoutContractTests {
    [Theory]
    [InlineData("ss01", 1, "A", "a")]
    [InlineData("ss01", 0, "A", "A")]
    [InlineData("salt", 1, "B", "a")]
    [InlineData("salt", 2, "B", "f")]
    [InlineData("salt", 99, "B", "f")]
    [InlineData("ccmp", 1, "a", "fi")]
    [InlineData("calt", 1, "AaB", "AfB")]
    [InlineData("calt", 1, "BaB", "BaB")]
    [InlineData("rvrn", 1, "AaB", "AiB")]
    [InlineData("ss02", 1, "B", "A")]
    [InlineData("ss03", 1, "Aa", "Af")]
    public void Substitutions_ApplyAuthoredFeaturesAndKeepSourceClusters(string tag, int value, string text, string expected) {
        var data = BarcodeFontCollectionTests.Bytes("TestLayoutSubstitution.ttf");
        var reader = ManagedOpenTypeReader.TryCreate(data)!;
        var substitution = ManagedOpenTypeSubstitution.TryCreate(data)!;
        var settings = new ManagedTextFeatureSettings().With(tag, value);
        Assert.True(substitution.CanApply(settings));
        var tokens = text.Select((c, i) => new ManagedOpenTypeSubstitution.GlyphToken(reader.MapGlyph(c), c.ToString(), i, c)).ToList();
        substitution.Apply(tokens, settings, default);
        Assert.Equal(expected.Select(c => reader.MapGlyph(c)), tokens.Select(t => t.GlyphId));
        Assert.Equal(text, string.Concat(tokens.Select(t => t.UnicodeText)));
        Assert.All(tokens, token => Assert.InRange(token.ClusterStart, 0, text.Length - 1));
        if (tag == "ccmp") {
            Assert.True(tokens[1].IsUnicodeContinuation);
            Assert.Equal(tokens[0].ClusterStart, tokens[1].ClusterStart);
        }
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => substitution.CanApply(settings, cancellation.Token));
    }

    [Theory]
    [InlineData("TestKerningPairs.ttf", -80)]
    [InlineData("TestKerningClasses.ttf", -70)]
    [InlineData("TestKerningExtension.ttf", -60)]
    [InlineData("TestKerningLegacy.ttf", -5)]
    public void Kerning_PairClassExtensionAndLegacyRespectGposPrecedence(string fixture, int adjustment) {
        var data = BarcodeFontCollectionTests.Bytes(fixture);
        var reader = ManagedOpenTypeReader.TryCreate(data)!;
        var font = ManagedTrueTypeFont.TryLoad(data)!;
        var kerning = ManagedOpenTypeKerning.FromReader(reader);
        var a = reader.MapGlyph('A');
        var b = reader.MapGlyph('B');
        Assert.Equal(adjustment, kerning.Adjustment(a, b));
        Assert.Equal((1250 + adjustment) / 100.0, font.Measure("AB", 10), 6);
        Assert.Equal(12.75, font.Measure("BB", 10), 6); // Pair absent from GPOS falls back to kern.
        Assert.Equal(0, kerning.Adjustment(a, a));
        Assert.Equal(0, kerning.Adjustment(-1, b));
        var positioned = kerning.PositionRun(new[] { a, b }, new[] { 65, 66 });
        Assert.Equal(adjustment, positioned.Sum(p => p.XAdvance));
        Assert.Empty(kerning.PositionRun(Array.Empty<int>(), Array.Empty<int>()));
        Assert.Throws<ArgumentException>(() => kerning.PositionRun(new[] { a }, Array.Empty<int>()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => kerning.PositionRun(new[] { a, b }, new[] { 65, 66 }, cancellation.Token));
        if (fixture == "TestKerningPairs.ttf") {
            var pair = kerning.PositionRun(new[] { b, a }, new[] { 66, 65 });
            Assert.Equal(10, pair[0].XPlacement);
            Assert.Equal(-40, pair[0].XAdvance);
            Assert.Equal(20, pair[1].XPlacement);
            Assert.Equal(-10, pair[1].XAdvance);
            Assert.Equal(12, font.Measure("BA", 10), 6);
        }
    }

    [Fact]
    public void FeatureSelections_AreImmutableOrderedIdentitiesAndRejectInvalidTags() {
        var values = new Dictionary<string, int> { ["liga"] = 1, ["kern"] = 0 };
        var settings = new ManagedTextFeatureSettings(values);
        values["liga"] = 0;
        var equal = new ManagedTextFeatureSettings().With("kern", 0).With("liga", 1);
        Assert.Equal(settings, equal);
        Assert.Equal(settings.GetHashCode(), equal.GetHashCode());
        Assert.True(settings.TryGetValue("liga", out var enabled));
        Assert.Equal(1, enabled);
        Assert.NotEqual(settings, settings.With("liga", 0));
        Assert.False(settings.Equals((object?)null));
        Assert.Throws<ArgumentException>(() => settings.With("bad", 1));
        Assert.Throws<ArgumentException>(() => settings.With("a\nbc", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.With("liga", -1));
    }

    [Fact]
    public void VariationSelectors_ResolveDefaultAlternateAndSupplementarySequencesWithoutStandaloneInk() {
        var data = BarcodeFontCollectionTests.Bytes("TestTrackingAndSelectors.ttf");
        var font = ManagedTrueTypeFont.TryLoad(data)!;
        Assert.Equal(7.8, font.Measure("A\ufe0f", 12), 6); // Nondefault UVS selects B; authored tracking is zero at size 12.
        Assert.Equal(6, font.Measure("A\U000e0100", 12), 6); // Supplementary selector selects a.
        Assert.Equal(6, font.Measure("a\ufe0f", 12), 6); // Default UVS retains nominal a.
        Assert.False(font.HasGlyphs("A\ufe0e")); // Unregistered sequences are outside this font's coverage.
        Assert.Equal(0, font.Measure("\ufe0f", 12), 6);
        Assert.True(font.HasGlyphs("A\ufe0fa\ufe0f"));
        Assert.NotEmpty(font.GetTextContours("A\ufe0f", 0, 0, 10));
    }

    [Fact]
    public void Tracking_InterpolatesAuthoredSizesAndAppliesOncePerGrapheme() {
        var data = BarcodeFontCollectionTests.Bytes("TestTrackingAndSelectors.ttf");
        var reader = ManagedOpenTypeReader.TryCreate(data)!;
        Assert.True(reader.TryGetTable("trak", out var offset, out var length));
        var tracking = ManagedOpenTypeTracking.Parse(data, offset, length)!;
        Assert.Equal(0, tracking.GetAdjustment(12));
        Assert.Equal(10, tracking.GetAdjustment(18));
        Assert.Equal(20, tracking.GetAdjustment(24));
        Assert.Equal(0, tracking.GetAdjustment(double.NaN));
        var font = ManagedTrueTypeFont.TryLoad(data)!;
        Assert.Equal(21.96, font.Measure("AA", 18), 6);
        Assert.Equal(29.76, font.Measure("AA", 24), 6);
        Assert.Equal(new[] { false, true, true }, ManagedOpenTypeTracking.GetBoundaries("A\u0301B", new[] { 0, 1, 2 }));
        Assert.Equal(new[] { true, false, true }, ManagedOpenTypeTracking.GetBoundaries("A\u0301B", new[] { 0, 1, 2 }, negative: true));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Internal.Fonts;
using CodeGlyphX.Printing;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class NativeFontVariationContractTests {
    // Independently instantiated and measured by fontTools (test-layout-metadata.json).
    [Theory]
    [InlineData(100, 12, 540, 500, 740)]
    [InlineData(400, 12, 600, 560, 800)]
    [InlineData(650, 12, 675, 635, 875)]
    [InlineData(900, 12, 700, 660, 900)]
    [InlineData(900, 72, 760, 720, 960)]
    public void VariableTrueType_MatchesIndependentAdvancesOutlinesAndLineMetrics(int weight, int optical, int advance, int right, int ascender) {
        foreach (var fixture in new[] { "TestVariable.ttf", "TestVariableGvarMetrics.ttf" }) {
            var data = BarcodeFontCollectionTests.Bytes(fixture);
            var model = Model(data, weight, optical);
            var font = ManagedTrueTypeFont.TryLoad(data, model, out var error);
            Assert.Null(error);
            Assert.NotNull(font);
            Assert.Equal(advance / 100.0, font!.Measure("A", 10), 6);
            Assert.Equal(ascender / 100.0, font.BaselineOffset(10), 6);
            var points = font.GetTextContours("A", 0, 0, 10).SelectMany(c => c).ToArray();
            Assert.Equal(.2, points.Min(p => p.X), 6);
            Assert.Equal(right / 100.0, points.Max(p => p.X), 6);
            Assert.Equal(7, points.Max(p => p.Y) - points.Min(p => p.Y), 6);
            Assert.Equal(2 * advance / 100.0, font.Measure("AA", 10), 6);
        }
    }

    [Fact]
    public void AxisSelection_ClampsInputsMapsAvarAndSeparatesOpticalSizeIdentity() {
        var data = BarcodeFontCollectionTests.Bytes("TestVariable.ttf");
        var reader = ManagedOpenTypeReader.TryCreate(data)!;
        var middle = Model(data, 650, 12);
        Assert.Equal(.75, middle.NormalizedCoordinates[0], 6);
        Assert.Equal(0, middle.NormalizedCoordinates[1]);
        Assert.Equal(Model(data, 900, 12).Identity, Model(data, 5000, 12).Identity);
        Assert.Equal(Model(data, 100, 12).Identity, Model(data, -1000, 12).Identity);
        var automatic = ManagedFontVariationModel.Create(reader, null);
        Assert.True(automatic.TryGetOpticalSize(100, out var optical));
        Assert.Equal(72, optical);
        var selected = automatic.ForOpticalSize(reader, 42);
        Assert.Equal(42, selected.DesignCoordinates["opsz"]);
        Assert.NotEqual(automatic.Identity, selected.Identity);
        Assert.False(middle.TryGetOpticalSize(42, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => automatic.TryGetOpticalSize(double.NaN, out _));
        Assert.Throws<ArgumentException>(() => ManagedFontVariationModel.Create(reader, new Dictionary<string, float> { ["nope"] = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ManagedFontVariationModel.Create(reader, new Dictionary<string, float> { ["wght"] = float.PositiveInfinity }));
        var staticReader = ManagedOpenTypeReader.TryCreate(BarcodeFontCollectionTests.Bytes("TestOutlines.ttf"))!;
        Assert.Throws<ArgumentException>(() => ManagedFontVariationModel.Create(staticReader, new Dictionary<string, float> { ["wght"] = 400 }));
        Assert.Equal(6, BarcodeFont.FromBytes(data).Shape("A", 10).AdvanceWidth, 6);
    }

    private static ManagedFontVariationModel Model(byte[] data, float weight, float optical) =>
        ManagedFontVariationModel.Create(ManagedOpenTypeReader.TryCreate(data)!, new Dictionary<string, float> { ["wght"] = weight, ["opsz"] = optical });
}

using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Internal.Fonts;
using CodeGlyphX.Printing;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class NativeFontCffContractTests {
    [Theory]
    [InlineData("TestCffCid.otf")]
    [InlineData("TestCffCidFormat0.otf")]
    [InlineData("TestCff2Dictionaries.otf")]
    public void CidFontDictionaries_UseTheSelectedGlyphsLocalSubroutines(string fixture) {
        var font = BarcodeFont.FromBytes(BarcodeFontCollectionTests.Bytes(fixture));
        var first = font.Shape("A", 10);
        var second = font.Shape("B", 10);
        Assert.Equal(6, first.AdvanceWidth, 6);
        Assert.Equal(6, second.AdvanceWidth, 6);
        Assert.Equal(3, first.InkBounds.Width, 6);
        Assert.Equal(5, second.InkBounds.Width, 6);
        Assert.Equal(.2, first.InkBounds.X, 6);
        Assert.Equal(.2, second.InkBounds.X, 6);
        Assert.Equal(9.5, first.InkBounds.Height, 6); // Outer glyph continues after both local and global calls.
        Assert.Equal(9.5, second.InkBounds.Height, 6);
        Assert.Single(first.Contours);
        Assert.Single(second.Contours);
        // Reuse the first dictionary after reading the other one.
        Assert.Equal(first.InkBounds.Width, font.Shape("A", 10).InkBounds.Width);
    }

    [Fact]
    public void Cff1Subroutines_StillRequireExplicitReturn() {
        var font = BarcodeFont.FromBytes(BarcodeFontCollectionTests.Bytes("TestCffCidUnterminated.otf"));
        Assert.Throws<System.IO.InvalidDataException>(() => font.Shape("A", 10));
    }

    [Fact]
    public void ComposedCffAccent_RetainsBaseHoleAccentTranslationAndMetrics() {
        var font = BarcodeFont.FromBytes(BarcodeFontCollectionTests.Bytes("TestCffSeac.otf"));
        var shape = font.Shape("é", 10);
        Assert.Equal(5, shape.AdvanceWidth, 6);
        Assert.Equal(.2, shape.InkBounds.X, 6);
        Assert.Equal(-1, shape.InkBounds.Y, 6);
        Assert.Equal(5.4, shape.InkBounds.Width, 6);
        Assert.Equal(9, shape.InkBounds.Height, 6);
        Assert.Equal(3, shape.Contours.Count); // Two base contours preserve the hole; the accent is separate.
        Assert.Equal(6, font.Shape("A", 10).AdvanceWidth, 6);
    }

    [Theory]
    [InlineData(400, 600, 560)]
    [InlineData(650, 650, 610)]
    [InlineData(900, 700, 660)]
    public void VariableCff2_BlendOutlinesAndAdvancesMatchIndependentInstances(int weight, int advance, int right) {
        var data = BarcodeFontCollectionTests.Bytes("TestVariableCff2.otf");
        var axes = new System.Collections.Generic.Dictionary<string, float> { ["wght"] = weight };
        var font = ManagedOpenTypeCffFont.TryLoad(data, axes, out var error);
        Assert.Null(error);
        Assert.NotNull(font);
        Assert.Equal(advance / 100.0, font!.Measure("A", 10), 6);
        var points = font.GetTextContoursBounded("A", 0, 0, 10, 1000, default).SelectMany(contour => contour).ToArray();
        Assert.Equal(.2, points.Min(point => point.X), 6);
        Assert.Equal(right / 100.0, points.Max(point => point.X), 6);
        Assert.Equal(7, points.Max(point => point.Y) - points.Min(point => point.Y), 6);
        Assert.Equal(6, BarcodeFont.FromBytes(data).Shape("A", 10).AdvanceWidth, 6); // Public loader selects the default instance.
    }

    // Independent BoundsPen measurements; arithmetic/subroutine cases draw an authored 540x700 box.
    [Theory]
    [InlineData(57344, 20, 0, 560, 700)]
    [InlineData(57345, 20, 0, 560, 700)]
    [InlineData(57346, 20, 0, 560, 700)]
    [InlineData(57347, 20, 0, 420, 365.956481006)]
    [InlineData(57348, 20, 0, 470, 365.956481006)]
    [InlineData(57349, 20, 0, 470, 385.956481006)]
    [InlineData(57350, 20, 0, 240, 500)]
    [InlineData(57351, 20, 0, 420, 320)]
    [InlineData(57352, 20, 0, 360, 400)]
    [InlineData(57353, 20, 0, 320, 440)]
    [InlineData(57354, 20, 0, 420, 100)]
    [InlineData(57355, 20, 0, 420, 102.629802941)]
    [InlineData(57356, 20, 0, 420, 120)]
    [InlineData(57357, 20, 0, 420, 102.629802941)]
    [InlineData(57358, 20, 0, 560, 700)]
    [InlineData(57359, 20, 0, 560, 700)]
    [InlineData(57360, 20, 0, 560, 700)]
    [InlineData(57361, 20, 0, 560, 700)]
    [InlineData(57362, 20, 0, 560, 700)]
    [InlineData(57363, 20, 0, 560, 700)]
    [InlineData(57364, 20, 0, 560, 700)]
    public void CffCurveHintSubroutineAndArithmeticPrograms_KeepMeasuredInk(int scalar, double left, double bottom, double right, double top) {
        var data = BarcodeFontCollectionTests.Bytes("TestCffPrograms.otf");
        var font = BarcodeFont.FromBytes(data);
        var text = char.ConvertFromUtf32(scalar);
        var shape = font.Shape(text, 10);
        Assert.Equal(6, shape.AdvanceWidth, 6);
        Assert.Equal(left / 100, shape.InkBounds.X, 3);
        Assert.InRange(Math.Abs((800 - top) / 100 - shape.InkBounds.Y), 0, .01);
        Assert.InRange(Math.Abs((right - left) / 100 - shape.InkBounds.Width), 0, .01);
        Assert.InRange(Math.Abs((top - bottom) / 100 - shape.InkBounds.Height), 0, .01);
        Assert.All(shape.Contours.SelectMany(contour => contour), point => {
            Assert.False(double.IsNaN(point.X) || double.IsInfinity(point.X));
            Assert.False(double.IsNaN(point.Y) || double.IsInfinity(point.Y));
        });
        var loaded = ManagedOpenTypeCffFont.TryLoad(data, null, out var error)!;
        Assert.Null(error);
        Assert.Throws<InvalidOperationException>(() => loaded.GetTextContoursBounded(text, 0, 0, 10, 2, default));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => loaded.GetTextContoursBounded(text, 0, 0, 10, 1000, cancellation.Token));
    }
}

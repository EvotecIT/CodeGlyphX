using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Internal.Fonts;
using CodeGlyphX.Printing;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class NativeFontCffContractTests {
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

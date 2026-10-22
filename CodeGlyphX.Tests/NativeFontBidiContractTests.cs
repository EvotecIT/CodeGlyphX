using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal.Fonts;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class NativeFontBidiContractTests {
    [Fact]
    public void Mirroring_MatchesEveryPublishedUnicode17PairAndPreservesOtherScalars() {
        using var input = typeof(NativeFontBidiContractTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Fonts.BidiMirroring-17.0.0.txt");
        Assert.NotNull(input);
        using var reader = new StreamReader(input!);
        var source = new StringBuilder();
        var expected = new StringBuilder();
        while (reader.ReadLine() is { } line) {
            var record = line.Split('#')[0].Trim();
            if (record.Length == 0) continue;
            var fields = record.Split(';');
            source.Append(char.ConvertFromUtf32(int.Parse(fields[0].Trim(), NumberStyles.HexNumber)));
            expected.Append(char.ConvertFromUtf32(int.Parse(fields[1].Trim(), NumberStyles.HexNumber)));
        }
        Assert.NotEmpty(source.ToString());
        Assert.Equal(expected.ToString(), ManagedBidiTextResolver.MirrorText(source.ToString()));
        Assert.Equal("A\U0001f642", ManagedBidiTextResolver.MirrorText("A\U0001f642"));
        Assert.Equal(source.ToString(), ManagedBidiTextResolver.MirrorText(expected.ToString()));
    }

    [Theory]
    [InlineData("\u202eabc\u202c", "cba")]
    [InlineData("abc \u2067אב\u2069 def", "abc בא def")]
    [InlineData("\u2068אב\u2069 X", "בא X")]
    [InlineData("12 אב", "בא 12")]
    [InlineData("abc\u2029אב", "abc\u2029בא")]
    [InlineData("\u200eA\u200fאב\u061c", "Aבא")]
    [InlineData("\u202aאב\u202c", "בא")]
    [InlineData("\u202dאב\u202c", "אב")]
    [InlineData("\u2066AB\u2069", "AB")]
    [InlineData("אב (AB)", "(AB) בא")]
    public void DirectionalRuns_RespectControlsNumbersMirrorsAndParagraphs(string logical, string visual) {
        Assert.Equal(visual, ManagedBidiTextResolver.ToVisualOrder(logical));
        var runs = ManagedBidiTextResolver.ResolveRuns(logical);
        Assert.Equal(Enumerable.Range(0, runs.Count), runs.Select(r => r.LogicalOrder));
        Assert.DoesNotContain(string.Concat(runs.Select(r => r.Text)), c => c is '\u202c' or '\u2069');
    }

    [Fact]
    public void BoundedEmbeddingOverflow_RecoversOuterDirectionAndHonoursCancellation() {
        var embedding = new string('\u202e', 200) + "ab" + new string('\u202c', 200) + " tail";
        var isolate = new string('\u2067', 200) + "אב" + new string('\u2069', 200) + " tail";
        Assert.Equal("ba tail", ManagedBidiTextResolver.ToVisualOrder(embedding));
        Assert.Equal("בא tail", ManagedBidiTextResolver.ToVisualOrder(isolate));
        Assert.Empty(ManagedBidiTextResolver.ResolveRuns(null));
        Assert.Equal(string.Empty, ManagedBidiTextResolver.ToVisualOrder(null));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => ManagedBidiTextResolver.ToVisualOrder(embedding, ManagedTextDirection.Auto, cancellation.Token));
        var fragments = new[] { "א", "ב" };
        Assert.Equal(new[] { "ב", "א" }, ManagedBidiTextResolver.ToVisualOrder("אב", fragments, ManagedTextDirection.Auto, default));
        Assert.Empty(ManagedBidiTextResolver.ToVisualOrder("אב", new[] { "א" }, ManagedTextDirection.Auto, default));
        IReadOnlyList<IReadOnlyList<string>> lines = new IReadOnlyList<string>[] { new[] { "א" }, new[] { "ב" } };
        var ordered = ManagedBidiTextResolver.ToVisualLineOrder("אב", lines, ManagedTextDirection.Auto, default);
        Assert.Equal(new[] { "א", "ב" }, ordered.SelectMany(line => line));
    }

    [Fact]
    public void ShapingBaseDirection_IgnoresIsolatedTextAndKeepsLogicalGlyphOwnership() {
        var data = BarcodeFontCollectionTests.Bytes("TestOutlines.ttf");
        var request = new ManagedTextShapingRequest("\u2068אב\u2069 A", "Test", data, false, 1000, ManagedTextDirection.Auto, null);
        var shaped = ManagedTextShapingProvider.Instance.ShapeText(request);
        Assert.NotNull(shaped);
        Assert.Equal(ManagedTextDirection.LeftToRight, shaped!.Direction);
        Assert.Equal(new[] { 2, 1, 4, 5 }, shaped.Glyphs.Select(glyph => glyph.TextIndex));
        Assert.Equal(new[] { "ב", "א", " ", "A" }, shaped.Glyphs.Select(glyph => glyph.UnicodeText));
    }
}

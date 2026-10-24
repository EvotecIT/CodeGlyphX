using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrLogoBudgetTests {
    public static IEnumerable<object[]> VersionsAndLevels() {
        for (var version = 1; version <= 40; version++)
            foreach (QrErrorCorrectionLevel ecc in Enum.GetValues(typeof(QrErrorCorrectionLevel)))
                yield return new object[] { version, ecc };
    }

    [Theory]
    [MemberData(nameof(VersionsAndLevels))]
    public void AcceptedKnockouts_SurviveEveryCoveredBitBeingFlipped(int version, QrErrorCorrectionLevel ecc) {
        var qr = QrCodeEncoder.EncodeText("BRAND", ecc, version, version, version % 8);
        var result = QrArt.FitLogo(qr);
        Assert.Equal(QrTables.GetNumBlocks(version, ecc), result.CoveredCodewordsPerBlock.Count);
        Assert.All(result.CoveredCodewordsPerBlock, covered =>
            Assert.InRange(covered, 0, Math.Max(0, result.CorrectableErrorsPerBlock - result.ReservedErrorsPerBlock)));
        if (!result.CanPlace) {
            Assert.Equal(0, result.LogoSideModules);
            Assert.All(result.CoveredCodewordsPerBlock, covered => Assert.Equal(0, covered));
            return;
        }
        var damaged = qr.Modules.Clone();
        var functions = QrStructureAnalysis.BuildFunctionMask(version, qr.Size);
        for (var y = result.OffsetModules; y < result.OffsetModules + result.KnockoutSideModules; y++) {
            for (var x = result.OffsetModules; x < result.OffsetModules + result.KnockoutSideModules; x++) {
                Assert.False(functions[x, y]);
                damaged[x, y] = !damaged[x, y];
            }
        }
        Assert.True(QrDecoder.TryDecode(damaged, out var decoded), $"Failed v{version}/{ecc} after knockout damage.");
        Assert.Equal("BRAND", decoded.Text);
        Assert.Equal(version, decoded.Version);
        Assert.Equal(ecc, decoded.ErrorCorrectionLevel);
    }

    [Theory]
    [MemberData(nameof(VersionsAndLevels))]
    public void Layout_AccountsForEveryBitAndBothShortAndLongBlocks(int version, QrErrorCorrectionLevel ecc) {
        var qr = QrCodeEncoder.EncodeText("BRAND", ecc, version, version);
        var layout = new QrCodewordLayout(qr);
        var words = QrTables.GetNumRawDataModules(version) / 8;
        var counts = new int[words];
        foreach (var word in layout.ModuleCodewords) if (word >= 0) counts[word]++;
        Assert.All(counts, bits => Assert.Equal(8, bits));
        var shortBlocks = layout.BlockCount - words % layout.BlockCount;
        var shortLength = words / layout.BlockCount;
        for (var block = 0; block < layout.BlockCount; block++)
            Assert.Equal(shortLength + (block >= shortBlocks ? 1 : 0), layout.CodewordBlocks.Count(b => b == block));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(21)]
    [InlineData(35)]
    public void CenteredAlignment_ReturnsNoFit_InsteadOfSpendingParityOnFunctionalDamage(int version) {
        var qr = QrCodeEncoder.EncodeText("BRAND", QrErrorCorrectionLevel.H, version, version);
        var result = QrArt.FitLogo(qr, paddingModules: 0, reservedErrorsPerBlock: 0);
        Assert.False(result.CanPlace);
        Assert.Equal(0, result.KnockoutSideModules);
    }

    [Fact]
    public void PaddingIsIncludedInTheKnockout_AndTheReserveCanExhaustItsBudget() {
        var qr = QrCodeEncoder.EncodeText("BRAND", QrErrorCorrectionLevel.H, 5, 5);
        var result = QrArt.FitLogo(qr, maximumSideModules: 8, paddingModules: 1);
        Assert.True(result.CanPlace);
        Assert.InRange(result.KnockoutSideModules, 3, 7);
        Assert.Equal(1, result.KnockoutSideModules % 2);
        Assert.Equal(result.LogoSideModules + 2, result.KnockoutSideModules);
        Assert.Equal(qr.Size, result.OffsetModules * 2 + result.KnockoutSideModules);
        Assert.False(QrArt.FitLogo(qr, reservedErrorsPerBlock: result.CorrectableErrorsPerBlock).CanPlace);
        Assert.False(QrArt.FitLogo(qr, maximumSideModules: 2, paddingModules: 1).CanPlace);
    }

    [Fact]
    public void BudgetedLogo_RendersAtItsMeasuredModuleBounds_AndRecoversThePayload() {
        var qr = QrCodeEncoder.EncodeText("https://example.com/brand", QrErrorCorrectionLevel.H, 4, 4);
        var result = QrArt.FitLogo(qr);
        Assert.True(result.CanPlace);
        const int module = 16;
        var logo = new QrRasterLogoOptions(new byte[] { 20, 60, 110, 255 }, 1, 1) {
            Scale = result.LogoSideModules / (double)qr.Size,
            PaddingPx = result.PaddingModules * module,
            DrawBackground = true
        };
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = module, Logo = logo });
        Assert.True(QrImageDecoder.TryDecodeImage(png, out var decoded));
        Assert.Equal("https://example.com/brand", decoded.Text);
        Assert.True(ImageReader.TryDecodeRgba32(png, out var pixels, out var width, out _));
        var center = width / 2;
        Assert.Equal(20, pixels[(center * width + center) * 4]);
    }

    [Fact]
    public void InvalidFitArguments_AreRejectedBeforeLayoutWork() {
        var qr = QR.Encode("BRAND");
        Assert.Throws<ArgumentNullException>(() => QrArt.FitLogo(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.FitLogo(qr, maximumSideModules: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.FitLogo(qr, paddingModules: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.FitLogo(qr, paddingModules: qr.Size + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.FitLogo(qr, reservedErrorsPerBlock: -1));
    }
}

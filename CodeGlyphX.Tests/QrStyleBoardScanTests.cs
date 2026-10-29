using CodeGlyphX.Examples;
using CodeGlyphX.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

/// <summary>Every website style-board look is rendered through the generator's own code path and
/// must decode its exact payload with strict ZXing and the native decoder.</summary>
[Collection("ImageScannerSerial")]
public sealed class QrStyleBoardScanTests {
    private readonly ITestOutputHelper _output;

    public QrStyleBoardScanTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> LookCases() => QrStyleBoardLooks.All.Select(look => new object[] { look.Name });

    [Fact]
    public void Board_HasACuratedSet_WithUniqueNamesAndFiles() {
        var looks = QrStyleBoardLooks.All;
        Assert.InRange(looks.Count, 18, 22);
        Assert.Equal(looks.Count, looks.Select(l => l.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(looks.Count, looks.Select(l => l.Slug).Distinct(StringComparer.Ordinal).Count());
        Assert.All(looks, look => Assert.InRange(look.Name.Length, 1, 14));
    }

    [Theory]
    [MemberData(nameof(LookCases))]
    [Trait("Category", "CorpusTiming")]
    public void BoardLook_DecodesExactPayload_AtDeliverySize_AndAfterResizingRotationAndBlur(string name) {
        var look = QrStyleBoardLooks.All.Single(l => l.Name == name);
        var png = QrStyleBoardLooks.RenderPng(look);
        Assert.True(ImageReader.TryDecodeRgba32(png, out _, out var width, out _));

        var results = QrArtWave1ScanTests.ScanImage(png, QrStyleBoardLooks.Payload).ToList();
        // The delivery size is the PNG the website ships, sampled at its own width.
        results.AddRange(QrArtWave1ScanTests.ScanImageAt(png, QrStyleBoardLooks.Payload, width));
        foreach (var result in results) _output.WriteLine(result.ToString());

        Assert.Equal(12, results.Count);
        var failures = results.Where(r => !r.StrictPassed || !r.LibraryPassed).ToArray();
        Assert.True(failures.Length == 0, $"{name}: " + string.Join(Environment.NewLine, failures.Select(r => r.ToString())));
    }
}

using System;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.PosiCode;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SpecialistImageResourceTests {
    [Theory]
    [InlineData(PosiCodeVariant.A)]
    [InlineData(PosiCodeVariant.B)]
    public void PosiCode_MaximumLatin1PayloadRetainsLinkedImageDecoding(PosiCodeVariant variant) {
        var text = new string('\u00ff', 500);
        var matrix = PosiCodeEncoder.EncodeStacked(text, 2, variant, rowHeight: 1).Modules;
        Assert.True(PosiCodeDecoder.TryDecode(matrix, out var decoded));
        Assert.Equal(text, decoded.Text);
        var png = MatrixPngRenderer.Render(matrix, new MatrixPngRenderOptions { ModuleSize = 2, QuietZone = 2 });
        var result = SymbolScanner.Scan(png, new ScanOptions {
            Formats = new[] { SymbolFormat.PosiCode }, TimeoutMilliseconds = 10000, EnableTileScan = false, MaxSymbols = 1
        });
        Assert.Equal(text, Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void PosiCode_OversizedTransitionRowsRespectTheExistingCodewordLimit() {
        var modules = new bool[262144];
        for (var x = 0; x < modules.Length; x++) modules[x] = (x & 1) == 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        var decoded = PosiCode.PosiCodeDecoder.TryDecode(modules, out _);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.False(decoded);
        Assert.True(allocated < 256 * 1024, $"Oversized PosiCode row allocated {allocated} bytes");
    }

    [Theory]
    [InlineData(SymbolFormat.CodablockF, 1)]
    [InlineData(SymbolFormat.CodablockF, 2)]
    [InlineData(SymbolFormat.Gs1Composite, 1)]
    [InlineData(SymbolFormat.Gs1Composite, 2)]
    [InlineData(SymbolFormat.PosiCode, 1)]
    [InlineData(SymbolFormat.PosiCode, 2)]
    public void WideAlternatingRows_DoNotAmplifyPitchWorkspace(SymbolFormat format, int height) {
        const int width = 262144;
        var mask = new byte[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++) mask[y * width + x] = (byte)(x & 1);
        var context = new SpecialistImageDecoder.ImageContext(mask, width, height);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var decoded = SpecialistImageDecoder.TryDecode(context, format, null, CancellationToken.None,
            out _, out _, out _, out _);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.False(decoded);
        // The mask is already prepared. Allow generous room for the linear-size projections,
        // but prevent seven transition lists per view from amplifying a shallow source frame.
        Assert.True(allocated < 16L * 1024 * 1024, $"{format}: {allocated} bytes for {width}x{height}");
    }
}

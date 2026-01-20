using System.IO;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AsyncDecodeTests {
    [Fact]
    public async Task TryDecodePngAsync_Stream_Returns_Result() {
        var qr = QrCodeEncoder.EncodeText("ASYNC", QrErrorCorrectionLevel.M);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
        });

        await using var stream = new MemoryStream(png);
        var decoded = await CodeGlyph.TryDecodePngAsync(stream);

        Assert.NotNull(decoded);
        Assert.Equal(CodeGlyphKind.Qr, decoded!.Kind);
    }
}

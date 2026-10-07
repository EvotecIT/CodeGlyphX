using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

internal static partial class QrRenderer {
    internal static byte[] RenderPixels(QrCode qr, out int widthPx, out int heightPx, out int stride, QrRenderOptions? options) {
        var opts = options is null ? new QrRenderOptions() : CloneOptions(options);
        ValidateOptions(opts);
        return QrPngRenderer.RenderPixels(qr.Modules, BuildPngOptions(opts, qr), out widthPx, out heightPx, out stride);
    }
}

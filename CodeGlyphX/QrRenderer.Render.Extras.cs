using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

internal static partial class QrRenderer {
    internal static byte[] RenderPixels(QrCode qr, out int widthPx, out int heightPx, out int stride, QrRenderOptions? options) {
        var opts = options is null ? new QrRenderOptions() : CloneOptions(options);
        ValidateOptions(opts);
        var render = BuildPngOptions(opts, qr);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
        if (opts.ArtGuardrailsEnabled && HasArtHints(render, opts))
            VerifyRenderedArt(qr, pixels, widthPx, heightPx, stride);
        return pixels;
    }
}

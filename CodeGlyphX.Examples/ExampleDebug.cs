using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class ExampleDebug {
    public static void WriteQrDebugImages(byte[] rgba, int width, int height, int stride, string debugDir, string baseName) {
        var opts = new QrPixelDebugOptions();
        QrPixelDebug.RenderToFile(rgba, width, height, stride, PixelFormat.Rgba32, QrPixelDebugMode.Binarized, debugDir, $"{baseName}-bin.png", opts);

        var adaptive = new QrPixelDebugOptions {
            AdaptiveThreshold = true,
            AdaptiveWindowSize = 15,
            AdaptiveOffset = 8
        };
        QrPixelDebug.RenderToFile(rgba, width, height, stride, PixelFormat.Rgba32, QrPixelDebugMode.Binarized, debugDir, $"{baseName}-bin-adaptive.png", adaptive);
        QrPixelDebug.RenderToFile(rgba, width, height, stride, PixelFormat.Rgba32, QrPixelDebugMode.Heatmap, debugDir, $"{baseName}-heatmap.png", opts);
    }
}

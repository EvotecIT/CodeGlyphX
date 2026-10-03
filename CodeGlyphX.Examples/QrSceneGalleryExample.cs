using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrSceneGalleryExample {
    internal static void Run(string outputDir) {
        const string payload = "https://example.com/scenes";
        const int tile = 400, gutter = 16;
        var sheetSize = tile * 3 + gutter * 4;
        var sheetHeight = tile * 2 + gutter * 3;
        var sheet = new byte[sheetSize * sheetHeight * 4];
        for (var p = 0; p < sheet.Length; p += 4) { sheet[p] = 236; sheet[p + 1] = 237; sheet[p + 2] = 244; sheet[p + 3] = 255; }
        var index = 0;
        foreach (QrSceneStyle style in Enum.GetValues(typeof(QrSceneStyle))) {
            var options = QrScenePresets.Create(style);
            var result = QrArt.ComposeScene(payload, options);
            var name = "scene-" + style.ToString().ToLowerInvariant();
            result.SavePng(Path.Combine(outputDir, name + ".png"));
            result.ToRecipe().Save(Path.Combine(outputDir, name + ".cgxart"));
            // Render the actual editable design at gallery tile resolution.
            options.Size = tile;
            var small = QrArt.ComposeScene(payload, options);
            small.SavePng(Path.Combine(outputDir, name + "-thumb.png"));
            var preview = small.Image.GetPixels();
            var left = gutter + index % 3 * (tile + gutter); var top = gutter + index / 3 * (tile + gutter);
            for (var y = 0; y < tile; y++) Buffer.BlockCopy(preview, y * tile * 4, sheet, ((top + y) * sheetSize + left) * 4, tile * 4);
            index++;
        }
        File.WriteAllBytes(Path.Combine(outputDir, "scene-gallery.png"), PngImageEncoder.EncodeRgba32(sheet, sheetSize, sheetHeight));
    }
}

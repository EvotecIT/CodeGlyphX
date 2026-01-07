using System.Text;
using CodeMatrix;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Examples;

internal static class ExampleHelpers {
    public static void WriteBinary(string outputDir, string fileName, byte[] data) {
        if (data.Length == 0) return;
        var path = Path.Combine(outputDir, fileName);
        File.WriteAllBytes(path, data);
    }

    public static void WriteText(string outputDir, string fileName, string text) {
        var path = Path.Combine(outputDir, fileName);
        File.WriteAllText(path, text ?? string.Empty, Encoding.UTF8);
    }

    public static string WrapHtml(string title, string innerHtml) {
        return "<!doctype html>" +
               "<html lang=\"en\">" +
               "<head><meta charset=\"utf-8\"/>" +
               "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>" +
               $"<title>{title}</title></head>" +
               "<body style=\"background:#f5f7fb;font-family:Segoe UI,Arial,sans-serif;\">" +
               "<div style=\"padding:24px;\">" + innerHtml + "</div></body></html>";
    }

    public static byte[] RenderQrPixels(BitMatrix modules, QrPngRenderOptions opts, out int width, out int height, out int stride) {
        return RenderQrPixels(modules, opts.ModuleSize, opts.QuietZone, opts.Foreground, opts.Background, out width, out height, out stride);
    }

    public static byte[] RenderQrPixels(BitMatrix modules, int moduleSize, int quietZone, Rgba32 dark, Rgba32 light, out int width, out int height, out int stride) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (moduleSize <= 0) throw new ArgumentOutOfRangeException(nameof(moduleSize));
        if (quietZone < 0) throw new ArgumentOutOfRangeException(nameof(quietZone));

        var outModules = modules.Width + quietZone * 2;
        width = outModules * moduleSize;
        height = width;
        stride = width * 4;

        var pixels = new byte[height * stride];
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            var my = y / moduleSize - quietZone;
            for (var x = 0; x < width; x++) {
                var mx = x / moduleSize - quietZone;
                var darkModule = (uint)mx < (uint)modules.Width && (uint)my < (uint)modules.Height && modules[mx, my];
                var color = darkModule ? dark : light;
                var p = row + x * 4;
                pixels[p + 0] = color.R;
                pixels[p + 1] = color.G;
                pixels[p + 2] = color.B;
                pixels[p + 3] = color.A;
            }
        }

        return pixels;
    }

    public static byte[] CreateLogoRgba(int size, Rgba32 color, Rgba32 accent, out int width, out int height) {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        width = size;
        height = size;
        var rgba = new byte[size * size * 4];
        var cx = (size - 1) / 2.0;
        var radius = size * 0.45;
        var accentRadius = size * 0.25;
        var r2 = radius * radius;
        var a2 = accentRadius * accentRadius;

        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                var dx = x - cx;
                var dy = y - cx;
                var dist2 = dx * dx + dy * dy;
                if (dist2 > r2) continue;

                var c = dist2 <= a2 ? accent : color;
                var p = (y * size + x) * 4;
                rgba[p + 0] = c.R;
                rgba[p + 1] = c.G;
                rgba[p + 2] = c.B;
                rgba[p + 3] = c.A;
            }
        }

        return rgba;
    }

    public static bool TryReadRepoFile(string relativePath, out byte[] data, out string? fullPath) {
        data = Array.Empty<byte>();
        fullPath = null;

        if (string.IsNullOrWhiteSpace(relativePath)) return false;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) {
                data = File.ReadAllBytes(candidate);
                fullPath = candidate;
                return true;
            }
            dir = dir.Parent;
        }

        return false;
    }
}

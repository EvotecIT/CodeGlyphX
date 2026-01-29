using System;
using System.IO;

namespace CodeGlyphX.Internal;

internal readonly struct SaveByExtensionHandlers {
    internal Func<string> Default { get; init; }
    internal Func<string> Png { get; init; }
    internal Func<string> Webp { get; init; }
    internal Func<string> Svg { get; init; }
    internal Func<string> Svgz { get; init; }
    internal Func<string> Html { get; init; }
    internal Func<string> Jpeg { get; init; }
    internal Func<string> Bmp { get; init; }
    internal Func<string> Ppm { get; init; }
    internal Func<string> Pbm { get; init; }
    internal Func<string> Pgm { get; init; }
    internal Func<string> Pam { get; init; }
    internal Func<string> Xbm { get; init; }
    internal Func<string> Xpm { get; init; }
    internal Func<string> Tga { get; init; }
    internal Func<string> Ico { get; init; }
    internal Func<string> Pdf { get; init; }
    internal Func<string> Eps { get; init; }
}

internal delegate string SaveByExtensionSpanHandler(ReadOnlySpan<byte> data);

internal readonly struct SaveByExtensionSpanHandlers {
    internal SaveByExtensionSpanHandler Default { get; init; }
    internal SaveByExtensionSpanHandler Png { get; init; }
    internal SaveByExtensionSpanHandler Webp { get; init; }
    internal SaveByExtensionSpanHandler Svg { get; init; }
    internal SaveByExtensionSpanHandler Svgz { get; init; }
    internal SaveByExtensionSpanHandler Html { get; init; }
    internal SaveByExtensionSpanHandler Jpeg { get; init; }
    internal SaveByExtensionSpanHandler Bmp { get; init; }
    internal SaveByExtensionSpanHandler Ppm { get; init; }
    internal SaveByExtensionSpanHandler Pbm { get; init; }
    internal SaveByExtensionSpanHandler Pgm { get; init; }
    internal SaveByExtensionSpanHandler Pam { get; init; }
    internal SaveByExtensionSpanHandler Xbm { get; init; }
    internal SaveByExtensionSpanHandler Xpm { get; init; }
    internal SaveByExtensionSpanHandler Tga { get; init; }
    internal SaveByExtensionSpanHandler Ico { get; init; }
    internal SaveByExtensionSpanHandler Pdf { get; init; }
    internal SaveByExtensionSpanHandler Eps { get; init; }
}

internal static class SaveByExtensionHelper {
    internal static string Save(string path, in SaveByExtensionHandlers handlers) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return handlers.Svgz();
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return handlers.Default();

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return handlers.Png();
            case ".webp":
                return handlers.Webp();
            case ".svg":
                return handlers.Svg();
            case ".html":
            case ".htm":
                return handlers.Html();
            case ".jpg":
            case ".jpeg":
                return handlers.Jpeg();
            case ".bmp":
                return handlers.Bmp();
            case ".ppm":
                return handlers.Ppm();
            case ".pbm":
                return handlers.Pbm();
            case ".pgm":
                return handlers.Pgm();
            case ".pam":
                return handlers.Pam();
            case ".xbm":
                return handlers.Xbm();
            case ".xpm":
                return handlers.Xpm();
            case ".tga":
                return handlers.Tga();
            case ".ico":
                return handlers.Ico();
            case ".svgz":
                return handlers.Svgz();
            case ".pdf":
                return handlers.Pdf();
            case ".eps":
            case ".ps":
                return handlers.Eps();
            default:
                return handlers.Default();
        }
    }

    internal static string Save(ReadOnlySpan<byte> data, string path, in SaveByExtensionSpanHandlers handlers) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return handlers.Svgz(data);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return handlers.Default(data);

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return handlers.Png(data);
            case ".webp":
                return handlers.Webp(data);
            case ".svg":
                return handlers.Svg(data);
            case ".html":
            case ".htm":
                return handlers.Html(data);
            case ".jpg":
            case ".jpeg":
                return handlers.Jpeg(data);
            case ".bmp":
                return handlers.Bmp(data);
            case ".ppm":
                return handlers.Ppm(data);
            case ".pbm":
                return handlers.Pbm(data);
            case ".pgm":
                return handlers.Pgm(data);
            case ".pam":
                return handlers.Pam(data);
            case ".xbm":
                return handlers.Xbm(data);
            case ".xpm":
                return handlers.Xpm(data);
            case ".tga":
                return handlers.Tga(data);
            case ".ico":
                return handlers.Ico(data);
            case ".svgz":
                return handlers.Svgz(data);
            case ".pdf":
                return handlers.Pdf(data);
            case ".eps":
            case ".ps":
                return handlers.Eps(data);
            default:
                return handlers.Default(data);
        }
    }
}

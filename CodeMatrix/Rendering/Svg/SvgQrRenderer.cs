using System;
using System.Text;

namespace CodeMatrix.Rendering.Svg;

public static class SvgQrRenderer {
    public static string Render(BitMatrix modules, QrSvgRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var size = modules.Width;
        var outSize = size + opts.QuietZone * 2;
        var px = outSize * opts.ModuleSize;

        var sb = new StringBuilder(outSize * outSize * 2);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(px).Append("\" height=\"").Append(px)
            .Append("\" viewBox=\"0 0 ").Append(outSize).Append(' ').Append(outSize)
            .Append("\" shape-rendering=\"crispEdges\">");

        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(opts.LightColor).Append("\"/>");

        // Compact dark modules as horizontal runs into a single path.
        sb.Append("<path fill=\"").Append(opts.DarkColor).Append("\" d=\"");
        for (var y = 0; y < size; y++) {
            var outY = y + opts.QuietZone;
            var runStart = -1;
            for (var x = 0; x < size; x++) {
                var dark = modules[x, y];
                if (dark && runStart < 0) runStart = x;
                if ((!dark || x == size - 1) && runStart >= 0) {
                    var runEnd = dark && x == size - 1 ? x + 1 : x;
                    sb.Append('M').Append(runStart + opts.QuietZone).Append(' ').Append(outY)
                        .Append('h').Append(runEnd - runStart).Append("v1h-").Append(runEnd - runStart).Append('z');
                    runStart = -1;
                }
            }
        }
        sb.Append("\"/>");

        sb.Append("</svg>");
        return sb.ToString();
    }
}


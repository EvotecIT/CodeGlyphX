using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CodeGlyphX.Rendering.Svgz;

/// <summary>
/// Writes SVGZ (gzip-compressed SVG).
/// </summary>
public static class SvgzWriter {
    /// <summary>
    /// Writes SVGZ bytes from an SVG string.
    /// </summary>
    public static byte[] WriteSvg(string svg) {
        if (svg is null) throw new ArgumentNullException(nameof(svg));
        using var ms = new MemoryStream();
        WriteSvg(ms, svg);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes SVGZ bytes to a stream from an SVG string.
    /// </summary>
    public static void WriteSvg(Stream stream, string svg) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (svg is null) throw new ArgumentNullException(nameof(svg));
        using var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
        var bytes = Encoding.UTF8.GetBytes(svg);
        gzip.Write(bytes, 0, bytes.Length);
    }
}

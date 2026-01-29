using System;
using System.IO;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Helpers for output format detection and metadata.
/// </summary>
public static class OutputFormatInfo {
    /// <summary>
    /// Returns the output kind for a given format.
    /// </summary>
    public static OutputKind GetKind(OutputFormat format) {
        return format switch {
            OutputFormat.Svg => OutputKind.Text,
            OutputFormat.Html => OutputKind.Text,
            OutputFormat.Xbm => OutputKind.Text,
            OutputFormat.Xpm => OutputKind.Text,
            OutputFormat.Eps => OutputKind.Text,
            OutputFormat.Ascii => OutputKind.Text,
            _ => OutputKind.Binary
        };
    }

    /// <summary>
    /// Returns true when the format is textual.
    /// </summary>
    public static bool IsText(OutputFormat format) => GetKind(format) == OutputKind.Text;

    /// <summary>
    /// Returns true when the format is binary.
    /// </summary>
    public static bool IsBinary(OutputFormat format) => GetKind(format) == OutputKind.Binary;

    /// <summary>
    /// Returns the default file extension (without a leading dot) for a format.
    /// </summary>
    public static string GetDefaultExtension(OutputFormat format) {
        return format switch {
            OutputFormat.Png => "png",
            OutputFormat.Svg => "svg",
            OutputFormat.Svgz => "svgz",
            OutputFormat.Html => "html",
            OutputFormat.Jpeg => "jpg",
            OutputFormat.Webp => "webp",
            OutputFormat.Bmp => "bmp",
            OutputFormat.Ppm => "ppm",
            OutputFormat.Pbm => "pbm",
            OutputFormat.Pgm => "pgm",
            OutputFormat.Pam => "pam",
            OutputFormat.Xbm => "xbm",
            OutputFormat.Xpm => "xpm",
            OutputFormat.Tga => "tga",
            OutputFormat.Ico => "ico",
            OutputFormat.Pdf => "pdf",
            OutputFormat.Eps => "eps",
            OutputFormat.Ascii => "txt",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Returns a MIME type for the format when known.
    /// </summary>
    public static string GetMimeType(OutputFormat format) {
        return format switch {
            OutputFormat.Png => "image/png",
            OutputFormat.Svg => "image/svg+xml",
            OutputFormat.Svgz => "image/svg+xml",
            OutputFormat.Html => "text/html",
            OutputFormat.Jpeg => "image/jpeg",
            OutputFormat.Webp => "image/webp",
            OutputFormat.Bmp => "image/bmp",
            OutputFormat.Ppm => "image/x-portable-pixmap",
            OutputFormat.Pbm => "image/x-portable-bitmap",
            OutputFormat.Pgm => "image/x-portable-graymap",
            OutputFormat.Pam => "image/x-portable-anymap",
            OutputFormat.Xbm => "image/x-xbitmap",
            OutputFormat.Xpm => "image/x-xpixmap",
            OutputFormat.Tga => "image/x-tga",
            OutputFormat.Ico => "image/x-icon",
            OutputFormat.Pdf => "application/pdf",
            OutputFormat.Eps => "application/postscript",
            OutputFormat.Ascii => "text/plain",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Detects the output format from a file path.
    /// </summary>
    public static OutputFormat FromPath(string path) {
        if (string.IsNullOrWhiteSpace(path)) return OutputFormat.Unknown;
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return OutputFormat.Svgz;
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) return OutputFormat.Unknown;
        return FromExtension(ext);
    }

    /// <summary>
    /// Detects the output format from a file extension (with or without a leading dot).
    /// </summary>
    public static OutputFormat FromExtension(string extension) {
        if (string.IsNullOrWhiteSpace(extension)) return OutputFormat.Unknown;
        var ext = extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
        switch (ext.ToLowerInvariant()) {
            case ".png":
                return OutputFormat.Png;
            case ".webp":
                return OutputFormat.Webp;
            case ".svg":
                return OutputFormat.Svg;
            case ".svgz":
                return OutputFormat.Svgz;
            case ".html":
            case ".htm":
                return OutputFormat.Html;
            case ".jpg":
            case ".jpeg":
                return OutputFormat.Jpeg;
            case ".bmp":
                return OutputFormat.Bmp;
            case ".ppm":
                return OutputFormat.Ppm;
            case ".pbm":
                return OutputFormat.Pbm;
            case ".pgm":
                return OutputFormat.Pgm;
            case ".pam":
                return OutputFormat.Pam;
            case ".xbm":
                return OutputFormat.Xbm;
            case ".xpm":
                return OutputFormat.Xpm;
            case ".tga":
                return OutputFormat.Tga;
            case ".ico":
                return OutputFormat.Ico;
            case ".pdf":
                return OutputFormat.Pdf;
            case ".eps":
            case ".ps":
                return OutputFormat.Eps;
            case ".txt":
                return OutputFormat.Ascii;
            default:
                return OutputFormat.Unknown;
        }
    }

    /// <summary>
    /// Detects the format from a path and falls back to a default if unknown.
    /// </summary>
    public static OutputFormat Resolve(string path, OutputFormat fallback) {
        var format = FromPath(path);
        return format == OutputFormat.Unknown ? fallback : format;
    }
}

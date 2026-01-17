using System;
using System.Text;
using System.IO;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Convenience extension methods for rendered outputs.
/// </summary>
public static class RenderExtensions {
    /// <summary>
    /// Writes binary data to a file.
    /// </summary>
    public static string WriteBinary(this byte[] data, string path) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return RenderIO.WriteBinary(path, data);
    }

    /// <summary>
    /// Writes binary data to a file under the specified directory.
    /// </summary>
    public static string WriteBinary(this byte[] data, string directory, string fileName) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return RenderIO.WriteBinary(directory, fileName, data);
    }

    /// <summary>
    /// Writes binary data to a stream.
    /// </summary>
    public static void WriteBinary(this byte[] data, Stream stream) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        RenderIO.WriteBinary(stream, data);
    }

    /// <summary>
    /// Writes text to a file.
    /// </summary>
    public static string WriteText(this string text, string path, Encoding? encoding = null) {
        return RenderIO.WriteText(path, text, encoding);
    }

    /// <summary>
    /// Writes text to a file under the specified directory.
    /// </summary>
    public static string WriteText(this string text, string directory, string fileName, Encoding? encoding = null) {
        return RenderIO.WriteText(directory, fileName, text, encoding);
    }

    /// <summary>
    /// Writes text to a stream.
    /// </summary>
    public static void WriteText(this string text, Stream stream, Encoding? encoding = null) {
        RenderIO.WriteText(stream, text, encoding);
    }

    /// <summary>
    /// Reads binary data from a file path.
    /// </summary>
    public static byte[] ReadBinary(this string path) {
        return RenderIO.ReadBinary(path);
    }

    /// <summary>
    /// Reads text from a file path.
    /// </summary>
    public static string ReadText(this string path, Encoding? encoding = null) {
        return RenderIO.ReadText(path, encoding);
    }

    /// <summary>
    /// Reads binary data from a stream.
    /// </summary>
    public static byte[] ReadBinary(this Stream stream) {
        return RenderIO.ReadBinary(stream);
    }

    /// <summary>
    /// Reads text from a stream.
    /// </summary>
    public static string ReadText(this Stream stream, Encoding? encoding = null) {
        return RenderIO.ReadText(stream, encoding);
    }

    /// <summary>
    /// Attempts to read binary data from a file path.
    /// </summary>
    public static bool TryReadBinary(this string path, out byte[] data) {
        return RenderIO.TryReadBinary(path, out data);
    }

    /// <summary>
    /// Attempts to read text from a file path.
    /// </summary>
    public static bool TryReadText(this string path, out string text, Encoding? encoding = null) {
        return RenderIO.TryReadText(path, out text, encoding);
    }

    /// <summary>
    /// Wraps HTML content in a minimal document shell.
    /// </summary>
    public static string WrapHtml(this string innerHtml, string? title) {
        title ??= string.Empty;
        return "<!doctype html>" +
               "<html lang=\"en\">" +
               "<head><meta charset=\"utf-8\"/>" +
               "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>" +
               $"<title>{title}</title></head>" +
               "<body style=\"background:#f5f7fb;font-family:Segoe UI,Arial,sans-serif;\">" +
               "<div style=\"padding:24px;\">" + innerHtml + "</div></body></html>";
    }

    /// <summary>
    /// Encodes binary data as Base64.
    /// </summary>
    public static string ToBase64(this byte[] data) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return Convert.ToBase64String(data);
    }

    /// <summary>
    /// Encodes text as Base64 (UTF-8 by default).
    /// </summary>
    public static string ToBase64(this string text, Encoding? encoding = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var enc = encoding ?? Encoding.UTF8;
        return Convert.ToBase64String(enc.GetBytes(text));
    }

    /// <summary>
    /// Encodes binary data as a Base64 data URI.
    /// </summary>
    public static string ToDataUri(this byte[] data, string mimeType) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(mimeType)) throw new ArgumentException("Mime type cannot be null, empty, or whitespace.", nameof(mimeType));
        return "data:" + mimeType + ";base64," + Convert.ToBase64String(data);
    }

    /// <summary>
    /// Encodes PNG bytes as a Base64 data URI.
    /// </summary>
    public static string ToPngDataUri(this byte[] data) => data.ToDataUri("image/png");

    /// <summary>
    /// Encodes JPEG bytes as a Base64 data URI.
    /// </summary>
    public static string ToJpegDataUri(this byte[] data) => data.ToDataUri("image/jpeg");

    /// <summary>
    /// Encodes BMP bytes as a Base64 data URI.
    /// </summary>
    public static string ToBmpDataUri(this byte[] data) => data.ToDataUri("image/bmp");

    /// <summary>
    /// Encodes SVG content as a Base64 data URI.
    /// </summary>
    public static string ToSvgDataUri(this string svg, Encoding? encoding = null) {
        if (svg is null) throw new ArgumentNullException(nameof(svg));
        var enc = encoding ?? Encoding.UTF8;
        return "data:image/svg+xml;base64," + Convert.ToBase64String(enc.GetBytes(svg));
    }
}

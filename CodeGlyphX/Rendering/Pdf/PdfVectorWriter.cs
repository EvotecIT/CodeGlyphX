using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace CodeGlyphX.Rendering.Pdf;

/// <summary>
/// Minimal PDF writer for vector content streams.
/// </summary>
internal static class PdfVectorWriter {
    internal static byte[] Write(int width, int height, string content) {
        using var ms = new MemoryStream();
        Write(ms, width, height, content);
        return ms.ToArray();
    }

    /// <summary>Writes a vector page using explicit PDF-point dimensions, including fractional physical sizes.</summary>
    internal static byte[] Write(double width, double height, string content) {
        using var ms = new MemoryStream();
        WriteCore(ms, width.ToString("0.########", CultureInfo.InvariantCulture), height.ToString("0.########", CultureInfo.InvariantCulture), width, height, content);
        return ms.ToArray();
    }

    internal static void Write(Stream stream, int width, int height, string content) {
        WriteCore(stream, width.ToString(CultureInfo.InvariantCulture), height.ToString(CultureInfo.InvariantCulture), width, height, content);
    }

    private static void WriteCore(Stream stream, string widthText, string heightText, double width, double height, string content) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (content is null) throw new ArgumentNullException(nameof(content));

        var offsets = new long[5];
        using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true);

        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        offsets[1] = stream.Position;
        writer.WriteLine("1 0 obj");
        writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
        writer.WriteLine("endobj");
        writer.Flush();

        offsets[2] = stream.Position;
        writer.WriteLine("2 0 obj");
        writer.WriteLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        writer.WriteLine("endobj");
        writer.Flush();

        offsets[3] = stream.Position;
        writer.WriteLine("3 0 obj");
        writer.WriteLine($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {widthText} {heightText}]");
        writer.WriteLine("   /Resources << >>");
        writer.WriteLine("   /Contents 4 0 R >>");
        writer.WriteLine("endobj");
        writer.Flush();

        var contentBytes = Encoding.ASCII.GetBytes(content);
        offsets[4] = stream.Position;
        writer.WriteLine("4 0 obj");
        writer.WriteLine($"<< /Length {contentBytes.Length} >>");
        writer.WriteLine("stream");
        writer.Flush();
        stream.Write(contentBytes, 0, contentBytes.Length);
        writer.WriteLine();
        writer.WriteLine("endstream");
        writer.WriteLine("endobj");
        writer.Flush();

        var xrefStart = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine("0 5");
        writer.WriteLine("0000000000 65535 f ");
        for (var i = 1; i <= 4; i++) {
            writer.WriteLine($"{offsets[i]:0000000000} 00000 n ");
        }
        writer.WriteLine("trailer");
        writer.WriteLine("<< /Size 5 /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefStart);
        writer.WriteLine("%%EOF");
        writer.Flush();
    }
}

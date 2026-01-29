using System;
using System.IO;
using System.Text;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Writes rendered outputs to files and streams.
/// </summary>
public static class OutputWriter {
    /// <summary>
    /// Writes a rendered output to a file and returns the path.
    /// </summary>
    public static string Write(string path, RenderedOutput output, Encoding? encoding = null) {
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (output.IsText) {
            return RenderIO.WriteText(path, output.GetText(encoding), encoding);
        }
        return RenderIO.WriteBinary(path, output.Data);
    }

    /// <summary>
    /// Writes a rendered output to a stream.
    /// </summary>
    public static void Write(Stream stream, RenderedOutput output, Encoding? encoding = null) {
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (output.IsText) {
            RenderIO.WriteText(stream, output.GetText(encoding), encoding);
            return;
        }
        RenderIO.WriteBinary(stream, output.Data);
    }
}

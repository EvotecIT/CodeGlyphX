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
            if (encoding is null) {
                return RenderIO.WriteBinary(path, output.OwnedBytes);
            }
            return RenderIO.WriteText(path, output.GetText(), encoding);
        }
        return RenderIO.WriteBinary(path, output.OwnedBytes);
    }

    /// <summary>
    /// Writes a rendered output to a stream.
    /// </summary>
    public static void Write(Stream stream, RenderedOutput output, Encoding? encoding = null) {
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (output.IsText) {
            if (encoding is null) {
                RenderIO.WriteBinary(stream, output.OwnedBytes);
                return;
            }
            RenderIO.WriteText(stream, output.GetText(), encoding);
            return;
        }
        RenderIO.WriteBinary(stream, output.OwnedBytes);
    }
}

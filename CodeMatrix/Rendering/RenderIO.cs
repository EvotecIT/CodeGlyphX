using System;
using System.IO;
using System.Text;

namespace CodeMatrix.Rendering;

/// <summary>
/// Simple file and stream helpers for rendered assets.
/// </summary>
public static class RenderIO {
    /// <summary>
    /// Writes binary data to a file and returns the full path.
    /// </summary>
    /// <param name="path">Output file path.</param>
    /// <param name="data">Binary data to write.</param>
    /// <returns>The output file path.</returns>
    public static string WriteBinary(string path, byte[] data) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (data is null) throw new ArgumentNullException(nameof(data));
        EnsureDirectory(path);
        File.WriteAllBytes(path, data);
        return path;
    }

    /// <summary>
    /// Writes binary data to a file under the specified directory.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="data">Binary data to write.</param>
    /// <returns>The output file path.</returns>
    public static string WriteBinary(string directory, string fileName, byte[] data) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        return WriteBinary(Path.Combine(directory, fileName), data);
    }

    /// <summary>
    /// Writes binary data to a stream.
    /// </summary>
    /// <param name="stream">Target stream.</param>
    /// <param name="data">Binary data to write.</param>
    public static void WriteBinary(Stream stream, byte[] data) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (data is null) throw new ArgumentNullException(nameof(data));
        stream.Write(data, 0, data.Length);
    }

    /// <summary>
    /// Reads binary data from a file.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <returns>Binary file contents.</returns>
    public static byte[] ReadBinary(string path) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Reads binary data from a stream.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <returns>Binary data.</returns>
    public static byte[] ReadBinary(Stream stream) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory) return memory.ToArray();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Attempts to read binary data from a file.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="data">Binary file contents.</param>
    /// <returns>True when the file exists and was read.</returns>
    public static bool TryReadBinary(string path, out byte[] data) {
        data = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        data = File.ReadAllBytes(path);
        return true;
    }

    /// <summary>
    /// Reads text from a file.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <returns>Text content.</returns>
    public static string ReadText(string path, Encoding? encoding = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        return File.ReadAllText(path, encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// Reads text from a stream.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <returns>Text content.</returns>
    public static string ReadText(Stream stream, Encoding? encoding = null) {
        var bytes = ReadBinary(stream);
        return (encoding ?? Encoding.UTF8).GetString(bytes);
    }

    /// <summary>
    /// Attempts to read text from a file.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <returns>True when the file exists and was read.</returns>
    public static bool TryReadText(string path, out string text, Encoding? encoding = null) {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        text = File.ReadAllText(path, encoding ?? Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// Writes text to a file and returns the full path.
    /// </summary>
    /// <param name="path">Output file path.</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <returns>The output file path.</returns>
    public static string WriteText(string path, string? text, Encoding? encoding = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        EnsureDirectory(path);
        File.WriteAllText(path, text ?? string.Empty, encoding ?? Encoding.UTF8);
        return path;
    }

    /// <summary>
    /// Writes text to a stream.
    /// </summary>
    /// <param name="stream">Target stream.</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    public static void WriteText(Stream stream, string? text, Encoding? encoding = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(text ?? string.Empty);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes text to a file under the specified directory.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <returns>The output file path.</returns>
    public static string WriteText(string directory, string fileName, string? text, Encoding? encoding = null) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        return WriteText(Path.Combine(directory, fileName), text, encoding);
    }

    private static void EnsureDirectory(string path) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) {
            Directory.CreateDirectory(dir);
        }
    }
}

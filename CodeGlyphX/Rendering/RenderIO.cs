using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Simple file and stream helpers for rendered assets.
/// </summary>
public static class RenderIO {
    private const string InputLimitMessage = "Input exceeds size limits.";
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
    /// Writes binary data to a file asynchronously and returns the full path.
    /// </summary>
    /// <param name="path">Output file path.</param>
    /// <param name="data">Binary data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output file path.</returns>
    public static async Task<string> WriteBinaryAsync(string path, byte[] data, CancellationToken cancellationToken = default) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (data is null) throw new ArgumentNullException(nameof(data));
        EnsureDirectory(path);
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous)) {
            await fs.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        }
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
    /// Writes binary data to a file under the specified directory with a safe file name.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name (no path separators).</param>
    /// <param name="data">Binary data to write.</param>
    /// <returns>The output file path.</returns>
    public static string WriteBinarySafe(string directory, string fileName, byte[] data) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        EnsureSafeFileName(fileName);
        return WriteBinary(Path.Combine(directory, fileName), data);
    }

    /// <summary>
    /// Writes binary data to a file under the specified directory asynchronously.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="data">Binary data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output file path.</returns>
    public static Task<string> WriteBinaryAsync(string directory, string fileName, byte[] data, CancellationToken cancellationToken = default) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        return WriteBinaryAsync(Path.Combine(directory, fileName), data, cancellationToken);
    }

    /// <summary>
    /// Writes binary data to a file under the specified directory asynchronously with a safe file name.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name (no path separators).</param>
    /// <param name="data">Binary data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output file path.</returns>
    public static Task<string> WriteBinarySafeAsync(string directory, string fileName, byte[] data, CancellationToken cancellationToken = default) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        EnsureSafeFileName(fileName);
        return WriteBinaryAsync(Path.Combine(directory, fileName), data, cancellationToken);
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
    /// Writes binary data to a stream asynchronously.
    /// </summary>
    /// <param name="stream">Target stream.</param>
    /// <param name="data">Binary data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task WriteBinaryAsync(Stream stream, byte[] data, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (data is null) throw new ArgumentNullException(nameof(data));
        return stream.WriteAsync(data, 0, data.Length, cancellationToken);
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
    /// Reads binary data from a file with a size limit.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <returns>Binary file contents.</returns>
    public static byte[] ReadBinary(string path, int maxBytes) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (maxBytes <= 0) return ReadBinary(path);
        var info = new FileInfo(path);
        if (info.Exists && info.Length > maxBytes) {
            throw new FormatException(GuardMessages.ForBytes(InputLimitMessage, info.Length, maxBytes));
        }
        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Reads binary data from a file asynchronously.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Binary file contents.</returns>
    public static async Task<byte[]> ReadBinaryAsync(string path, CancellationToken cancellationToken = default) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await ReadBinaryAsync(fs, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads binary data from a file asynchronously with a size limit.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Binary file contents.</returns>
    public static async Task<byte[]> ReadBinaryAsync(string path, int maxBytes, CancellationToken cancellationToken = default) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (maxBytes <= 0) return await ReadBinaryAsync(path, cancellationToken).ConfigureAwait(false);
        var info = new FileInfo(path);
        if (info.Exists && info.Length > maxBytes) {
            throw new FormatException(GuardMessages.ForBytes(InputLimitMessage, info.Length, maxBytes));
        }
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await ReadBinaryAsync(fs, maxBytes, cancellationToken).ConfigureAwait(false);
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
    /// Reads binary data from a stream with a size limit.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <returns>Binary data.</returns>
    public static byte[] ReadBinary(Stream stream, int maxBytes) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (maxBytes <= 0) return ReadBinary(stream);

        if (stream is MemoryStream memory) {
            if (memory.Length > maxBytes) {
                throw new FormatException(GuardMessages.ForBytes(InputLimitMessage, memory.Length, maxBytes));
            }
            return memory.ToArray();
        }

        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        while (true) {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0) break;
            total += read;
            if (total > maxBytes) throw new FormatException(GuardMessages.ForBytes(InputLimitMessage, total, maxBytes));
            ms.Write(buffer, 0, read);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Reads binary data from a stream asynchronously.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Binary data.</returns>
    public static async Task<byte[]> ReadBinaryAsync(Stream stream, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory) return memory.ToArray();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, 81920, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    /// <summary>
    /// Reads binary data from a stream asynchronously with a size limit.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Binary data.</returns>
    public static async Task<byte[]> ReadBinaryAsync(Stream stream, int maxBytes, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (maxBytes <= 0) return await ReadBinaryAsync(stream, cancellationToken).ConfigureAwait(false);

        if (stream is MemoryStream memory) {
            if (memory.Length > maxBytes) {
                throw new FormatException(GuardMessages.ForBytes(InputLimitMessage, memory.Length, maxBytes));
            }
            return memory.ToArray();
        }

        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        while (true) {
            var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            if (read <= 0) break;
            total += read;
            if (total > maxBytes) throw new FormatException(GuardMessages.ForBytes(InputLimitMessage, total, maxBytes));
            await ms.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Attempts to read binary data from a stream asynchronously with a size limit.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Binary data when successful; otherwise null.</returns>
    public static async Task<byte[]?> TryReadBinaryAsync(Stream stream, int maxBytes, CancellationToken cancellationToken = default) {
        try {
            return await ReadBinaryAsync(stream, maxBytes, cancellationToken).ConfigureAwait(false);
        } catch (FormatException) {
            return null;
        }
    }

    /// <summary>
    /// Attempts to read binary data from a file asynchronously with a size limit.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Binary data when successful; otherwise null.</returns>
    public static async Task<byte[]?> TryReadBinaryAsync(string path, int maxBytes, CancellationToken cancellationToken = default) {
        try {
            return await ReadBinaryAsync(path, maxBytes, cancellationToken).ConfigureAwait(false);
        } catch (FormatException) {
            return null;
        }
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
    /// Attempts to read binary data from a file with a size limit.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <param name="data">Binary file contents.</param>
    /// <returns>True when the file exists and was read.</returns>
    public static bool TryReadBinary(string path, int maxBytes, out byte[] data) {
        data = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        if (maxBytes <= 0) {
            data = File.ReadAllBytes(path);
            return true;
        }
        var info = new FileInfo(path);
        if (info.Exists && info.Length > maxBytes) return false;
        data = File.ReadAllBytes(path);
        return true;
    }

    /// <summary>
    /// Attempts to read binary data from a stream with a size limit.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="maxBytes">Maximum bytes to read (0 to disable).</param>
    /// <param name="data">Binary data.</param>
    /// <returns>True when the stream was read within the limit.</returns>
    public static bool TryReadBinary(Stream stream, int maxBytes, out byte[] data) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        try {
            data = ReadBinary(stream, maxBytes);
            return true;
        } catch (FormatException) {
            data = Array.Empty<byte>();
            return false;
        }
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
    /// Reads text from a file asynchronously.
    /// </summary>
    /// <param name="path">Input file path.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Text content.</returns>
    public static async Task<string> ReadTextAsync(string path, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        var bytes = await ReadBinaryAsync(path, cancellationToken).ConfigureAwait(false);
        return (encoding ?? Encoding.UTF8).GetString(bytes);
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
    /// Reads text from a stream asynchronously.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Text content.</returns>
    public static async Task<string> ReadTextAsync(Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        var bytes = await ReadBinaryAsync(stream, cancellationToken).ConfigureAwait(false);
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
    /// Writes text to a file asynchronously and returns the full path.
    /// </summary>
    /// <param name="path">Output file path.</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output file path.</returns>
    public static Task<string> WriteTextAsync(string path, string? text, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(text ?? string.Empty);
        return WriteBinaryAsync(path, bytes, cancellationToken);
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
    /// Writes text to a stream asynchronously.
    /// </summary>
    /// <param name="stream">Target stream.</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task WriteTextAsync(Stream stream, string? text, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var bytes = (encoding ?? Encoding.UTF8).GetBytes(text ?? string.Empty);
        return stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
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

    /// <summary>
    /// Writes text to a file under the specified directory with a safe file name.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name (no path separators).</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <returns>The output file path.</returns>
    public static string WriteTextSafe(string directory, string fileName, string? text, Encoding? encoding = null) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        EnsureSafeFileName(fileName);
        return WriteText(Path.Combine(directory, fileName), text, encoding);
    }

    /// <summary>
    /// Writes text to a file under the specified directory asynchronously.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output file path.</returns>
    public static Task<string> WriteTextAsync(string directory, string fileName, string? text, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        return WriteTextAsync(Path.Combine(directory, fileName), text, encoding, cancellationToken);
    }

    /// <summary>
    /// Writes text to a file under the specified directory asynchronously with a safe file name.
    /// </summary>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name (no path separators).</param>
    /// <param name="text">Text content.</param>
    /// <param name="encoding">Optional text encoding (defaults to UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output file path.</returns>
    public static Task<string> WriteTextSafeAsync(string directory, string fileName, string? text, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        if (directory is null) throw new ArgumentNullException(nameof(directory));
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        EnsureSafeFileName(fileName);
        return WriteTextAsync(Path.Combine(directory, fileName), text, encoding, cancellationToken);
    }

    private static void EnsureSafeFileName(string fileName) {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (fileName == "." || fileName == "..") throw new ArgumentException("File name cannot be a path segment.", nameof(fileName));
        if (Path.IsPathRooted(fileName)) throw new ArgumentException("File name must not be rooted.", nameof(fileName));
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)) {
            throw new ArgumentException("File name must not contain path separators.", nameof(fileName));
        }
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
            throw new ArgumentException("File name contains invalid characters.", nameof(fileName));
        }
    }

    private static void EnsureDirectory(string path) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) {
            Directory.CreateDirectory(dir);
        }
    }
}

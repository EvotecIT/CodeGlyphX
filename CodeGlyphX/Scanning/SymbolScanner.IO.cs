using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    /// <summary>Reads an encoded image from the current stream position and scans it.</summary>
    /// <remarks>The stream remains open. Its remaining contents are consumed. The total deadline includes reading.</remarks>
    public static ScanResult Scan(Stream stream, ScanOptions? options = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        options = SnapshotOptions(options);
        using var deadline = new ScanDeadline(options.CancellationToken, options.TimeoutMilliseconds);
        return ReadAndScan(stream, options, deadline);
    }

    /// <summary>Opens an encoded image file and scans it under one total deadline.</summary>
    public static ScanResult ScanFile(string path, ScanOptions? options = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        options = SnapshotOptions(options);
        using var deadline = new ScanDeadline(options.CancellationToken, options.TimeoutMilliseconds);
        if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
        using var stream = File.OpenRead(path);
        return ReadAndScan(stream, options, deadline);
    }

    /// <summary>Reads an encoded image asynchronously, then scans it under the same total deadline.</summary>
    /// <remarks>The stream remains open. Cancellation is represented in the returned structured result.</remarks>
    public static async Task<ScanResult> ScanAsync(Stream stream, ScanOptions? options = null, CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        options = SnapshotOptions(options);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken, cancellationToken);
        using var deadline = new ScanDeadline(caller.Token, options.TimeoutMilliseconds);
        return await ReadAndScanAsync(stream, options, deadline).ConfigureAwait(false);
    }

    /// <summary>Opens an encoded image file, reads it asynchronously, and scans it under one total deadline.</summary>
    /// <remarks>Cancellation is represented in the returned structured result, including before opening the file.</remarks>
    public static async Task<ScanResult> ScanFileAsync(string path, ScanOptions? options = null, CancellationToken cancellationToken = default) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        options = SnapshotOptions(options);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken, cancellationToken);
        using var deadline = new ScanDeadline(caller.Token, options.TimeoutMilliseconds);
        if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await ReadAndScanAsync(stream, options, deadline).ConfigureAwait(false);
    }

    private static ScanResult ReadAndScan(Stream stream, ScanOptions options, ScanDeadline deadline) {
        if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
        try {
            var bytes = RenderIO.ReadBinary(stream, options.Image?.MaxBytes ?? ImageReader.MaxImageBytes, deadline.Token);
            return ScanEncodedImage(bytes, options, deadline);
        } catch (OperationCanceledException) when (deadline.ShouldStop) {
            return Cancelled(deadline, new List<SymbolFormat>());
        } catch (FormatException ex) {
            return InvalidInput(deadline, ex.Message);
        }
    }

    private static async Task<ScanResult> ReadAndScanAsync(Stream stream, ScanOptions options, ScanDeadline deadline) {
        if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
        try {
            var bytes = await RenderIO.ReadBinaryAsync(stream, options.Image?.MaxBytes ?? ImageReader.MaxImageBytes, deadline.Token).ConfigureAwait(false);
            return ScanEncodedImage(bytes, options, deadline);
        } catch (OperationCanceledException) when (deadline.ShouldStop) {
            return Cancelled(deadline, new List<SymbolFormat>());
        } catch (FormatException ex) {
            return InvalidInput(deadline, ex.Message);
        }
    }

    private static ScanResult InvalidInput(ScanDeadline deadline, string message) {
        if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
        return Result(ScanStatus.InvalidImage, deadline, new List<DetectedSymbol>(), new List<SymbolFormat>(), message);
    }
}

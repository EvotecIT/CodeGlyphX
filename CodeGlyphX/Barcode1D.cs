using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// A simple 1D barcode represented as an alternating sequence of bar/space segments.
/// </summary>
public sealed class Barcode1D {
    private readonly IReadOnlyList<BarSegment> _segments;

    /// <summary>
    /// Gets a read-only snapshot of the barcode segments in order.
    /// </summary>
    public IReadOnlyList<BarSegment> Segments => _segments;

    /// <summary>
    /// Gets the total barcode width in modules (sum of <see cref="BarSegment.Modules"/> for all segments).
    /// </summary>
    public int TotalModules { get; }

    /// <summary>Gets the encoded physical format, or null for a manually constructed sequence of runs.</summary>
    public SymbolFormat? Format { get; }

    /// <summary>
    /// Creates a new <see cref="Barcode1D"/> from the provided segments.
    /// </summary>
    /// <param name="segments">Barcode segments in order (must contain at least one segment, each with a positive width).</param>
    public Barcode1D(IEnumerable<BarSegment> segments) : this(segments, null) { }

    internal Barcode1D(IEnumerable<BarSegment> segments, SymbolFormat? format) {
        if (segments is null) throw new ArgumentNullException(nameof(segments));
        var snapshot = segments.ToArray();
        if (snapshot.Length == 0) throw new ArgumentException("At least one segment is required.", nameof(segments));

        var total = 0;
        for (var i = 0; i < snapshot.Length; i++) {
            if (snapshot[i].Modules <= 0) throw new ArgumentException("Every segment must have a positive width.", nameof(segments));
            total = checked(total + snapshot[i].Modules);
        }
        _segments = Array.AsReadOnly(snapshot);
        TotalModules = total;
        Format = format;
    }

    /// <summary>Renders the encoded bars with layout and output settings.</summary>
    public RenderedOutput Render(OutputFormat format, BarcodeOptions? options = null, OutputOptions? extras = null) =>
        Barcode.Render(this, format, options, extras);

    /// <summary>Saves the barcode, selecting the output format from the file extension.</summary>
    public string Save(string path, BarcodeOptions? options = null, OutputOptions? extras = null) =>
        OutputWriter.Write(path, Render(OutputFormatInfo.Resolve(path, OutputFormat.Png), options, extras));

    /// <summary>Writes the barcode to a stream in the specified output format.</summary>
    public void Save(Stream stream, OutputFormat format, BarcodeOptions? options = null, OutputOptions? extras = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, Render(format, options, extras));
    }
}

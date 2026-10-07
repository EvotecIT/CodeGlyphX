using System;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// Creates a new <see cref="Barcode1D"/> from the provided segments.
    /// </summary>
    /// <param name="segments">Barcode segments in order (must contain at least one segment, each with a positive width).</param>
    public Barcode1D(IEnumerable<BarSegment> segments) {
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
    }
}

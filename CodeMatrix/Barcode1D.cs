using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMatrix;

/// <summary>
/// A simple 1D barcode represented as an alternating sequence of bar/space segments.
/// </summary>
public sealed class Barcode1D {
    private readonly BarSegment[] _segments;

    /// <summary>
    /// Gets the barcode segments in order.
    /// </summary>
    public IReadOnlyList<BarSegment> Segments => _segments;

    /// <summary>
    /// Gets the total barcode width in modules (sum of <see cref="BarSegment.Modules"/> for all segments).
    /// </summary>
    public int TotalModules { get; }

    /// <summary>
    /// Creates a new <see cref="Barcode1D"/> from the provided segments.
    /// </summary>
    /// <param name="segments">Barcode segments in order (must contain at least one segment).</param>
    public Barcode1D(IEnumerable<BarSegment> segments) {
        if (segments is null) throw new ArgumentNullException(nameof(segments));
        _segments = segments as BarSegment[] ?? segments.ToArray();
        if (_segments.Length == 0) throw new ArgumentException("At least one segment is required.", nameof(segments));

        var total = 0;
        for (var i = 0; i < _segments.Length; i++) total = checked(total + _segments[i].Modules);
        TotalModules = total;
    }
}

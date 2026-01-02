using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMatrix;

public sealed class Barcode1D {
    private readonly BarSegment[] _segments;

    public IReadOnlyList<BarSegment> Segments => _segments;
    public int TotalModules { get; }

    public Barcode1D(IEnumerable<BarSegment> segments) {
        if (segments is null) throw new ArgumentNullException(nameof(segments));
        _segments = segments as BarSegment[] ?? segments.ToArray();
        if (_segments.Length == 0) throw new ArgumentException("At least one segment is required.", nameof(segments));

        var total = 0;
        for (var i = 0; i < _segments.Length; i++) total = checked(total + _segments[i].Modules);
        TotalModules = total;
    }
}


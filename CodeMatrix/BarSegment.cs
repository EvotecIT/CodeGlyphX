using System;

namespace CodeMatrix;

/// <summary>
/// Represents a single run in a 1D barcode, expressed in module units.
/// </summary>
public readonly struct BarSegment {
    /// <summary>
    /// Gets whether the segment is a bar (dark) or a space (light).
    /// </summary>
    public bool IsBar { get; }

    /// <summary>
    /// Gets the segment width in modules.
    /// </summary>
    public int Modules { get; }

    /// <summary>
    /// Creates a new <see cref="BarSegment"/>.
    /// </summary>
    /// <param name="isBar">Whether the segment is a bar (dark) or a space (light).</param>
    /// <param name="modules">Width of the segment in modules (must be &gt; 0).</param>
    public BarSegment(bool isBar, int modules) {
        if (modules <= 0) throw new ArgumentOutOfRangeException(nameof(modules));
        IsBar = isBar;
        Modules = modules;
    }
}

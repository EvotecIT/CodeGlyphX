using System;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Options for per-module jitter offsets (organic placement).
/// </summary>
public sealed class QrPngModuleJitterOptions {
    /// <summary>
    /// Maximum jitter offset in pixels.
    /// </summary>
    public int MaxOffsetPx { get; set; } = 2;

    /// <summary>
    /// Random seed used to offset modules.
    /// </summary>
    public int Seed { get; set; } = 11021;

    /// <summary>
    /// When true, applies jitter to eye modules.
    /// </summary>
    public bool ApplyToEyes { get; set; }

    /// <summary>
    /// When true, functional patterns keep their original alignment.
    /// </summary>
    public bool ProtectFunctionalPatterns { get; set; } = true;

    /// <summary>
    /// When true, limits jitter to stay inside the module cell.
    /// </summary>
    public bool ClampToShape { get; set; } = true;

    internal void Validate() {
        if (MaxOffsetPx < 0) throw new ArgumentOutOfRangeException(nameof(MaxOffsetPx));
    }
}

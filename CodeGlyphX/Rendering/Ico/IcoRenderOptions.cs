using System;

namespace CodeGlyphX.Rendering.Ico;

/// <summary>
/// Options for ICO rendering (multi-size).
/// </summary>
public sealed partial class IcoRenderOptions {
    /// <summary>
    /// Output sizes in pixels (1..256). Defaults to common icon sizes.
    /// </summary>
    public int[] Sizes { get; set; } = { 16, 32, 48, 64, 128, 256 };

    /// <summary>
    /// Preserves aspect ratio and pads to square when true.
    /// </summary>
    public bool PreserveAspectRatio { get; set; } = true;

    internal int[] GetNormalizedSizes() {
        if (Sizes is null || Sizes.Length == 0) return new[] { 16, 32, 48, 64, 128, 256 };
        var list = new System.Collections.Generic.List<int>(Sizes.Length);
        for (var i = 0; i < Sizes.Length; i++) {
            var size = Sizes[i];
            if (size <= 0 || size > 256) continue;
            if (!list.Contains(size)) list.Add(size);
        }
        if (list.Count == 0) return new[] { 16, 32, 48, 64, 128, 256 };
        list.Sort();
        return list.ToArray();
    }
}

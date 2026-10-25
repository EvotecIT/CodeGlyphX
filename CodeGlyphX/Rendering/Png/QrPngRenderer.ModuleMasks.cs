using System;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    /// <summary>Samples the shared continuous geometry for binary image-composition silhouettes.</summary>
    internal static bool[] BuildModuleMask(int moduleSize, QrModuleShape shape, double scale, int cornerRadiusPx) {
        var mask = new bool[moduleSize * moduleSize];
        if (moduleSize <= 0) return mask;
        scale = Math.Max(0.1, Math.Min(1, scale));
        for (var y = 0; y < moduleSize; y++)
            for (var x = 0; x < moduleSize; x++)
                mask[y * moduleSize + x] = QrShapeGeometry.Contains(shape, (x + 0.5) / moduleSize,
                    (y + 0.5) / moduleSize, scale, cornerRadiusPx / (double)moduleSize);
        return mask;
    }
}

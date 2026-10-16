using System;

namespace CodeGlyphX;

/// <summary>Describes a known, cell-aligned grid occupying the entire scan region.</summary>
/// <remarks>
/// Use this calibration for raw marks whose white edge cells cannot be distinguished from a quiet zone,
/// such as Flattermarken. The scan region must exclude quiet zones and captions. Ordinary barcodes do not require it.
/// Dimensions describe the canonical grid before the declared clockwise print rotation. For example, a
/// 15-column, three-row grid printed at 90 degrees uses (15, 3, 90), although its image is three cells wide.
/// </remarks>
public sealed class ImageSamplingGrid {
    /// <summary>Gets the number of columns in the canonical grid before print rotation.</summary>
    public int Columns { get; }
    /// <summary>Gets the number of rows in the canonical grid before print rotation.</summary>
    public int Rows { get; }
    /// <summary>Gets the known clockwise rotation of the printed grid: zero, 90, 180 or 270 degrees.</summary>
    public int RotationDegrees { get; }

    /// <summary>Creates a grid calibration with positive dimensions.</summary>
    /// <param name="columns">Canonical columns before print rotation.</param>
    /// <param name="rows">Canonical rows before print rotation.</param>
    /// <param name="rotationDegrees">Clockwise print rotation: zero, 90, 180 or 270 degrees.</param>
    public ImageSamplingGrid(int columns, int rows, int rotationDegrees = 0) {
        if (columns <= 0 || columns > 4096) throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows <= 0 || rows > 4096 || (long)columns * rows > 1048576) throw new ArgumentOutOfRangeException(nameof(rows));
        Columns = columns;
        Rows = rows;
        if (rotationDegrees != 0 && rotationDegrees != 90 && rotationDegrees != 180 && rotationDegrees != 270)
            throw new ArgumentOutOfRangeException(nameof(rotationDegrees));
        RotationDegrees = rotationDegrees;
    }
}

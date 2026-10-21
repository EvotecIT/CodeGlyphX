using System;

namespace CodeGlyphX;

/// <summary>A bounded colour-cell grid for encoded or sampled Ultracode, without a quiet zone.</summary>
public sealed class UltracodeGrid {
    private readonly UltracodeColor[] _cells;
    /// <summary>Gets the width in cells.</summary>
    public int Width { get; }
    /// <summary>Gets the height in cells.</summary>
    public int Height { get; }
    /// <summary>Gets whether this snapshot rejects further changes.</summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>Creates a grid of unknown cells, at most 256 cells in each direction.</summary>
    public UltracodeGrid(int width, int height) {
        if (width < 1 || width > 256) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1 || height > 256) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width; Height = height; _cells = new UltracodeColor[width * height];
    }

    /// <summary>Gets or sets a logical colour. Unknown cells may be supplied as decoding erasures.</summary>
    public UltracodeColor this[int x, int y] {
        get => _cells[Index(x, y)];
        set {
            if (IsReadOnly) throw new InvalidOperationException("The Ultracode grid is read-only.");
            if ((byte)value > (byte)UltracodeColor.Green) throw new ArgumentOutOfRangeException(nameof(value));
            _cells[Index(x, y)] = value;
        }
    }

    /// <summary>Prevents further changes and returns this grid.</summary>
    public UltracodeGrid Freeze() { IsReadOnly = true; return this; }
    /// <summary>Creates an independent, editable copy.</summary>
    public UltracodeGrid Clone() {
        var copy = new UltracodeGrid(Width, Height);
        Array.Copy(_cells, copy._cells, _cells.Length);
        return copy;
    }
    private int Index(int x, int y) {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        return y * Width + x;
    }
}

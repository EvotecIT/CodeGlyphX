using System.Runtime.CompilerServices;

namespace CodeGlyphX.Internal;

/// <summary>Maps nonnegative image offsets to exact row and column coordinates.</summary>
internal readonly struct ImageCoordinateDivider {
    private readonly int _width;
    private readonly ulong _reciprocal;

    /// <summary>Prepares division for the positive width of a validated image.</summary>
    internal ImageCoordinateDivider(int width) {
        _width = width;
        _reciprocal = (1UL << 32) / (uint)width;
    }

    /// <summary>Uses a fixed reciprocal and one correction, preserving integer division exactly.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void GetCoordinates(int offset, out int x, out int y) {
        // Rounding the reciprocal down underestimates the quotient by at most
        // one for nonnegative Int32 offsets. Its product never exceeds offset,
        // so the remainder and the correction stay within Int32 bounds.
        y = (int)(((ulong)(uint)offset * _reciprocal) >> 32);
        x = offset - y * _width;
        if (x >= _width) {
            x -= _width;
            y++;
        }
    }
}

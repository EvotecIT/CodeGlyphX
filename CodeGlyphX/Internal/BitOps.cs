using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Numerics;
#endif

namespace CodeGlyphX.Internal;

internal static class BitOps {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(uint value) {
#if NET8_0_OR_GREATER
        return BitOperations.PopCount(value);
#else
        // Hamming weight (SWAR) for 32-bit values.
        value -= (value >> 1) & 0x55555555u;
        value = (value & 0x33333333u) + ((value >> 2) & 0x33333333u);
        value = (value + (value >> 4)) & 0x0F0F0F0Fu;
        value *= 0x01010101u;
        return (int)(value >> 24);
#endif
    }
}

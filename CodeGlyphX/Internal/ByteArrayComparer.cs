using System;
using System.Collections.Generic;

namespace CodeGlyphX.Internal;

internal sealed class ByteArrayComparer : IEqualityComparer<byte[]> {
    internal static readonly ByteArrayComparer Instance = new();

    public bool Equals(byte[]? x, byte[]? y) {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Length != y.Length) return false;
        return x.AsSpan().SequenceEqual(y);
    }

    public int GetHashCode(byte[] obj) {
        if (obj is null) return 0;
        unchecked {
            uint hash = 2166136261;
            for (var i = 0; i < obj.Length; i++) {
                hash ^= obj[i];
                hash *= 16777619;
            }
            return (int)hash;
        }
    }
}

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
            ulong hash = 14695981039346656037UL;
            for (var i = 0; i < obj.Length; i++) {
                hash ^= obj[i];
                hash *= 1099511628211UL;
            }
            hash ^= (ulong)obj.Length;
            hash *= 1099511628211UL;
            return (int)(hash ^ (hash >> 32));
        }
    }
}

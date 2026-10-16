namespace CodeGlyphX.Internal;

/// <summary>Shared assigned ISO 3166-1 alpha-2 country codes for checked application fields.</summary>
internal static class IsoCountryCodes {
    private static readonly ulong[] Alpha2Bits = {
        0x1e9afb77f7bdbb7bUL, 0xe4fc21a8012b0070UL, 0x003a900df9dfa800UL, 0xb160181ef00202c0UL,
        0x00b8d42f8281f2bfUL, 0x3fffeba4d2100080UL, 0x023cf1ca80000002UL, 0x008a8fbfe75cddf9UL,
        0x59820820eaa10200UL, 0x4002000000000800UL, 0x1020020080000000UL
    };

    /// <summary>Checks an ASCII country code against the shared assigned-code set.</summary>
    internal static bool ContainsAlpha2(string value, bool ignoreCase = false) {
        if (value.Length != 2) return false;
        var first = value[0];
        var second = value[1];
        if (ignoreCase) {
            if (first >= 'a' && first <= 'z') first = (char)(first - 'a' + 'A');
            if (second >= 'a' && second <= 'z') second = (char)(second - 'a' + 'A');
        }
        if (first < 'A' || first > 'Z' || second < 'A' || second > 'Z') return false;
        var bit = (first - 'A') * 26 + second - 'A';
        return (Alpha2Bits[bit / 64] & (1UL << (63 - bit % 64))) != 0;
    }
}

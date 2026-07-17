namespace CodeGlyphX.Gs1Data;

public static partial class Gs1Validator {
    private static readonly ulong[] Iso3166NumericBits = {
        0x08a888898888b888UL, 0x8aa80a2888888888UL, 0x0888888a22232889UL, 0x88188a2221e322a2UL,
        0x2a2a180088888888UL, 0x888a888888888888UL, 0x888288a2622a22a2UL, 0x222222228808b888UL,
        0x8888970808222222UL, 0x2de102888888a222UL, 0x320a1b222222a203UL, 0xe20808c8088888a8UL,
        0x8888889a89002021UL, 0xe080222a00082102UL, 0x0000000000000000UL, 0x0000000000000000UL
    };

    private static readonly ulong[] Iso3166Alpha2Bits = {
        0x1e9afb77f7bdbb7bUL, 0xe4fc21a8012b0070UL, 0x003a900df9dfa800UL, 0xb160181ef00202c0UL,
        0x00b8d42f8281f2bfUL, 0x3fffeba4d2100080UL, 0x023cf1ca80000002UL, 0x008a8fbfe75cddf9UL,
        0x59820820eaa10200UL, 0x4002000000000800UL, 0x1020020080000000UL
    };

    private static readonly ulong[] Iso4217NumericBits = {
        0x008800008808b808UL, 0x8880082080880808UL, 0x0880808800220008UL, 0x8010820202822000UL,
        0x0202000008000000UL, 0x8888088888888080UL, 0x088a88a262222002UL, 0x022200008800a080UL,
        0x88080c0008220200UL, 0x2022028880000020UL, 0x1202000000202002UL, 0xa200008000088888UL,
        0x0088880081002020UL, 0x2080002800002200UL, 0x0400000fe6adbfdfUL, 0xfdfdfce225000000UL
    };

    private static readonly byte[] MediaTypeBits = {
        0x7f, 0xe0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xf0
    };

    private static bool IsIso3166Numeric(string value) {
        return value.Length == 3 && ContainsOnlyDigits(value) && IsSet(Iso3166NumericBits, ParseDigits(value, 0, 3));
    }

    private static bool IsIso3166Alpha2(string value) {
        if (value.Length != 2 || value[0] < 'A' || value[0] > 'Z' || value[1] < 'A' || value[1] > 'Z') return false;
        return IsSet(Iso3166Alpha2Bits, (value[0] - 'A') * 26 + value[1] - 'A');
    }

    private static bool IsIso4217Numeric(string value) {
        return value.Length == 3 && ContainsOnlyDigits(value) && IsSet(Iso4217NumericBits, ParseDigits(value, 0, 3));
    }

    private static bool IsMediaType(string value) {
        if (value.Length != 2 || !ContainsOnlyDigits(value)) return false;
        var bit = ParseDigits(value, 0, 2);
        return bit < MediaTypeBits.Length * 8 && (MediaTypeBits[bit / 8] & (1 << (7 - bit % 8))) != 0;
    }

    private static bool IsSet(ulong[] field, int bit) {
        return bit >= 0 && bit < field.Length * 64 && (field[bit / 64] & (1UL << (63 - bit % 64))) != 0;
    }
}

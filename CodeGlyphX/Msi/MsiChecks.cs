using System;
using System.Globalization;

namespace CodeGlyphX.Msi;

/// <summary>Owns MSI check algorithms and their explicitly selected inverse interpretation.</summary>
internal static class MsiChecks {
    internal static string Append(string data, MsiChecksumType type) {
        switch (type) {
            case MsiChecksumType.None: return data;
            case MsiChecksumType.Mod10: return data + Mod10(data);
            case MsiChecksumType.Mod10Mod10:
                var first = data + Mod10(data);
                return first + Mod10(first);
            case MsiChecksumType.Mod11:
            case MsiChecksumType.NcrMod11:
            case MsiChecksumType.Mod11Mod10:
            case MsiChecksumType.NcrMod11Mod10:
                var ncr = type is MsiChecksumType.NcrMod11 or MsiChecksumType.NcrMod11Mod10;
                var checkedData = data + Mod11(data, ncr ? 9 : 7).ToString(CultureInfo.InvariantCulture);
                return type is MsiChecksumType.Mod11Mod10 or MsiChecksumType.NcrMod11Mod10
                    ? checkedData + Mod10(checkedData) : checkedData;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    internal static bool TryStrip(string raw, MsiChecksumType type, int? expectedDataLength, out string data) {
        data = raw;
        if (type == MsiChecksumType.None || !Enum.IsDefined(typeof(MsiChecksumType), type)) return false;
        if (expectedDataLength.HasValue) {
            var length = expectedDataLength.Value;
            if (length < 1 || length >= raw.Length) return false;
            var candidate = raw.Substring(0, length);
            if (Append(candidate, type) != raw) return false;
            data = candidate;
            return true;
        }
        string? found = null;
        // A Mod11 value of ten occupies two digits; its optional Mod10 adds one more.
        // Some strings admit both data lengths. Require a length hint instead of discarding data.
        for (var checks = 1; checks <= 3 && checks < raw.Length; checks++) {
            var candidate = raw.Substring(0, raw.Length - checks);
            if (Append(candidate, type) != raw) continue;
            if (found is not null) return false;
            found = candidate;
        }
        if (found is null) return false;
        data = found;
        return true;
    }

    internal static char Mod10(string data) {
        var sum = 0; var twice = true;
        for (var i = data.Length - 1; i >= 0; i--) {
            var value = data[i] - '0';
            if (twice) { value *= 2; value = value / 10 + value % 10; }
            sum = (sum + value) % 10;
            twice = !twice;
        }
        return (char)('0' + (10 - sum) % 10);
    }

    private static int Mod11(string data, int maximumWeight) {
        var sum = 0; var weight = 2;
        for (var i = data.Length - 1; i >= 0; i--) {
            sum = (sum + (data[i] - '0') * weight) % 11;
            if (++weight > maximumWeight) weight = 2;
        }
        return (11 - sum) % 11;
    }
}

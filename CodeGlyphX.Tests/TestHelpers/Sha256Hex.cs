using System.Security.Cryptography;

namespace CodeGlyphX.Tests.TestHelpers;

internal static class Sha256Hex {
    public static string HashHex(byte[] bytes) {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}


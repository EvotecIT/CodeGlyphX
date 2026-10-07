namespace CodeGlyphX.Tests;

internal static class SyntheticQrFixtureData {
    // Public RFC 4226/6238 test seed: ASCII "12345678901234567890" encoded as Base32.
    // These example.org enrollment payloads are synthetic and must never protect an account.
    private const string TestSeed = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
    public const string Clean = "otpauth://totp/CodeGlyphX-Synthetic:clean%40example.org?secret=" + TestSeed + "&issuer=CodeGlyphX-Synthetic&algorithm=SHA1&digits=6&period=30";
    public const string Noisy = "otpauth://totp/CodeGlyphX-Synthetic:noisy%40example.org?secret=" + TestSeed + "&issuer=CodeGlyphX-Synthetic&algorithm=SHA1&digits=6&period=30";
}

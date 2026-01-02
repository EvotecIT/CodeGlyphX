using System.Text;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class OtpTests {
    [Fact]
    public void Base32_RoundTrip_IsDeterministic_Uppercase_NoPadding() {
        var bytes = Encoding.ASCII.GetBytes("foo");
        var b32 = OtpAuthSecret.ToBase32(bytes);
        Assert.Equal("MZXW6", b32);

        var decoded = OtpAuthSecret.FromBase32("mzxw6===");
        Assert.Equal(bytes, decoded);

        var decoded2 = OtpAuthSecret.FromBase32(" mZxw6 - === ");
        Assert.Equal(bytes, decoded2);
    }

    [Fact]
    public void Otpauth_Totp_Escaping_AndOrdering_AreStable() {
        var secret = Encoding.ASCII.GetBytes("foo");
        var uri = OtpAuthTotp.Create("ACME Co", "john.doe+test@example.com", secret, OtpAlgorithm.Sha256, digits: 8, period: 60);
        Assert.Equal(
            "otpauth://totp/ACME%20Co:john.doe%2Btest%40example.com?secret=MZXW6&issuer=ACME%20Co&algorithm=SHA256&digits=8&period=60",
            uri);
    }
}


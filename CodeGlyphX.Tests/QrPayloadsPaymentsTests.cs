using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPayloadsPaymentsTests {
    [Fact]
    public void QrPayloads_Upi_BuildsExpectedUri() {
        var payload = QrPayloads.Upi("alice@upi", name: "Alice", amount: 12.5m).Text;
        Assert.Equal("upi://pay?pa=alice%40upi&pn=Alice&am=12.5&cu=INR", payload);
    }
}

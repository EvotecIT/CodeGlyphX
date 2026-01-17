using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPayloadsPaymentsTests {
    [Fact]
    public void QrPayloads_Upi_BuildsExpectedUri() {
        var payload = QrPayloads.Upi("alice@upi", name: "Alice", amount: 12.5m).Text;
        Assert.Equal("upi://pay?pa=alice%40upi&pn=Alice&am=12.5&cu=INR", payload);
    }

    [Fact]
    public void QrPayloads_BezahlCode_SinglePaymentSepa_BuildsExpectedUri() {
        var payload = QrPayloads.BezahlCodeSinglePaymentSepa(
            "Alice",
            "DE12500105170648489890",
            "BANKDEFF",
            12.5m,
            reason: "Invoice-1").Text;

        Assert.Equal(
            "bank://singlepaymentsepa?name=Alice&amount=12,50&reason=Invoice-1&currency=EUR&iban=DE12500105170648489890&bic=BANKDEFF",
            payload);
    }

    [Fact]
    public void QrPayloads_RussiaPaymentOrder_BuildsExpectedText() {
        var payload = QrPayloads.RussiaPaymentOrder(
            "ACME LLC",
            "40702810900000000001",
            "ACME BANK",
            "044525225",
            "30101810400000000225",
            "7700000000",
            "770001001",
            123.45m,
            "Invoice 123").Text;

        Assert.Equal(
            "ST00012|Name=ACME LLC|PersonalAcc=40702810900000000001|BankName=ACME BANK|BIC=044525225|CorrespAcc=30101810400000000225|PayeeINN=7700000000|KPP=770001001|Sum=12345|Purpose=Invoice 123",
            payload);
    }
}

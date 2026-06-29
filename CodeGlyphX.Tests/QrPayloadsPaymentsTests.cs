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

    [Fact]
    public void SwissQrCodePayload_UsesPublicSwissEnums() {
        var iban = new SwissQrCodePayload.Iban("CH9300762011623852957", SwissQrIbanType.Iban);
        var creditor = SwissQrCodePayload.Contact.CreateStructured("Evotec GmbH", "", "", "8000", "Zurich", "CH");
        var reference = new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, "RF18539007547034");

        var payload = new SwissQrCodePayload(
            iban,
            SwissQrCurrency.CHF,
            creditor,
            reference).ToString();

        Assert.Contains("\nCHF\n", payload);
        Assert.Contains("\nSCOR\nRF18539007547034\n", payload);
    }

    [Theory]
    [InlineData(SwissQrReferenceType.QRR)]
    [InlineData(SwissQrReferenceType.SCOR)]
    public void SwissQrCodePayload_RequiresReferenceForReferenceTypes(SwissQrReferenceType referenceType) {
        var exception = Assert.Throws<ArgumentException>(() => new SwissQrCodePayload.Reference(referenceType));

        Assert.Contains("Reference is required", exception.Message);
    }

    [Fact]
    public void SwissQrCodePayload_RejectsReferenceForNonReferenceType() {
        var exception = Assert.Throws<ArgumentException>(() => new SwissQrCodePayload.Reference(SwissQrReferenceType.NON, "RF18539007547034"));

        Assert.Contains("Reference is only allowed", exception.Message);
    }
}

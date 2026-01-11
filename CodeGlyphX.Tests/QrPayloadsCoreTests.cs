using System;
using CodeGlyphX.Payloads;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPayloadsCoreTests {
    [Fact]
    public void QrPayloads_CoreHelpers_ProduceExpectedPrefixes() {
        Assert.Equal("Hello", QrPayloads.Text("Hello").Text);
        Assert.StartsWith("https://", QrPayloads.Url("https://example.com").Text, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("WIFI:", QrPayloads.Wifi("Lab", "secret").Text, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("mailto:", QrPayloads.Email("hello@example.com", "Hi", "Body").Text, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("tel:", QrPayloads.Phone("+15551234567").Text, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("sms:", QrPayloads.Sms("+15551234567", "Hi").Text, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("geo:", QrPayloads.Geo("52.1", "21.0").Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QrPayloads_Contact_Calendar_And_Otp_AreNonEmpty() {
        var contact = QrPayloads.Contact(
            QrContactOutputType.VCard3,
            firstname: "Alice",
            lastname: "Example",
            email: "alice@example.com");
        Assert.Contains("BEGIN:VCARD", contact.Text, StringComparison.OrdinalIgnoreCase);

        var calendar = QrPayloads.CalendarEvent(
            subject: "Demo",
            description: "Test",
            location: "Remote",
            start: new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            end: new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            allDayEvent: false);
        Assert.Contains("BEGIN:VEVENT", calendar.Text, StringComparison.OrdinalIgnoreCase);

        var otp = QrPayloads.OneTimePassword(
            OtpAuthType.Totp,
            secretBase32: "JBSWY3DPEHPK3PXP",
            label: "alice@example.com",
            issuer: "CodeGlyphX");
        Assert.StartsWith("otpauth://", otp.Text, StringComparison.OrdinalIgnoreCase);
    }
}

using System.IO;
using System.Text;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrPayloadsExample {
    public static void Run(string outputDir) {
        var payloads = new (string Name, string Value)[] {
            ("text", QrPayload.Text("Hello from CodeGlyphX")),
            ("url", QrPayload.Url("https://example.com/products/qr")),
            ("appstore", QrPayload.AppStore("123456789")),
            ("googleplay", QrPayload.AppStoreGooglePlay("com.example.app")),
            ("wifi", QrPayload.Wifi("CodeGlyphX-Lab", "s3cret-pass", "WPA", hidden: false)),
            ("email", QrPayload.Email("hello@example.com", subject: "Hello", body: "Sent from CodeGlyphX")),
            ("phone", QrPayload.Phone("+14155550198")),
            ("sms", QrPayload.Sms("+14155550198", "Hello from CodeGlyphX")),
            ("upi", QrPayload.Upi("alice@upi", name: "Alice Doe", transactionNote: "Lunch", amount: 199.5m)),
            ("mecard", QrPayload.MeCard("Ava", "Stone", phone: "+14155550198", email: "ava@example.com", organization: "CodeGlyphX")),
            ("vcard", QrPayload.VCard("Ava", "Stone", phone: "+14155550198", email: "ava@example.com", organization: "CodeGlyphX")),
            ("vcard4", QrPayload.VCard4("Ava", "Stone", phones: new[] { "+14155550198" }, emails: new[] { "ava@example.com" }, organization: "CodeGlyphX", title: "Engineer", url: "https://example.com", address: "1 Main St;City;CA", note: "Example", birthday: "1990-01-01")),
            ("geo", QrPayload.Geo(37.7749, -122.4194)),
            ("location", QrPayload.Location(37.7749, -122.4194)),
            ("facebook", QrPayload.FacebookProfile("evotec")),
            ("twitter", QrPayload.TwitterProfile("evotec")),
            ("x", QrPayload.XProfile("evotec")),
            ("tiktok", QrPayload.TikTokProfile("evotec")),
            ("linkedin", QrPayload.LinkedInProfile("evotec")),
            ("event", QrPayload.CalendarEvent(
                "CodeGlyphX Demo",
                new DateTime(2026, 1, 20, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 20, 16, 0, 0, DateTimeKind.Utc),
                location: "Online",
                description: "Example calendar payload",
                organizer: "mailto:demo@example.com",
                uid: "codematrix-20260120",
                alarmMinutesBefore: 15)),
        };

        var sb = new StringBuilder();
        foreach (var item in payloads) {
            try {
                QR.SavePng(item.Value, Path.Combine(outputDir, $"qr-payload-{item.Name}.png"));
                sb.AppendLine($"[{item.Name}]");
                sb.AppendLine(item.Value);
                sb.AppendLine();
            } catch (Exception ex) {
                sb.AppendLine($"[{item.Name}]");
                sb.AppendLine("FAILED: " + ex.Message);
                sb.AppendLine(item.Value);
                sb.AppendLine();
            }
        }

        sb.ToString().WriteText(outputDir, "qr-payloads.txt");
    }
}

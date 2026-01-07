using System.Text;
using CodeMatrix;
using CodeMatrix.Payloads;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Examples;

internal static class QrPayloadsExample {
    public static void Run(string outputDir) {
        var payloads = new (string Name, string Value)[] {
            ("url", QrPayload.Url("https://example.com/products/qr")),
            ("wifi", QrPayload.Wifi("CodeMatrix-Lab", "s3cret-pass", "WPA", hidden: false)),
            ("email", QrPayload.Email("hello@example.com", subject: "Hello", body: "Sent from CodeMatrix")),
            ("phone", QrPayload.Phone("+14155550198")),
            ("sms", QrPayload.Sms("+14155550198", "Hello from CodeMatrix")),
            ("mecard", QrPayload.MeCard("Ava", "Stone", phone: "+14155550198", email: "ava@example.com", organization: "CodeMatrix")),
            ("vcard", QrPayload.VCard("Ava", "Stone", phone: "+14155550198", email: "ava@example.com", organization: "CodeMatrix")),
            ("vcard4", QrPayload.VCard4("Ava", "Stone", phones: new[] { "+14155550198" }, emails: new[] { "ava@example.com" }, organization: "CodeMatrix", title: "Engineer", url: "https://example.com", address: "1 Main St;City;CA", note: "Example", birthday: "1990-01-01")),
            ("geo", QrPayload.Geo(37.7749, -122.4194)),
            ("event", QrPayload.CalendarEvent(
                "CodeMatrix Demo",
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
                var qr = QrCodeEncoder.EncodeText(item.Value, QrErrorCorrectionLevel.M, 1, 40, null);
                var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
                    ModuleSize = 6,
                    QuietZone = 4,
                    Foreground = new Rgba32(24, 28, 38),
                    Background = new Rgba32(255, 255, 255),
                });

                ExampleHelpers.WriteBinary(outputDir, $"qr-payload-{item.Name}.png", png);
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

        ExampleHelpers.WriteText(outputDir, "qr-payloads.txt", sb.ToString());
    }
}

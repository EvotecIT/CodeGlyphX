using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Examples;

internal static class QrIllustratedExample {
    internal static void Run(string outputDir) {
        var directory = Path.Combine(outputDir, "qr-illustrated");
        Directory.CreateDirectory(directory);
        const string payload = "https://example.com/art";
        var qr = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        foreach (var style in Enum.GetValues<QrIllustratedStyle>()) {
            var subject = style == QrIllustratedStyle.EngravedPortrait ? "portrait" : style == QrIllustratedStyle.BotanicalBadge ? "flower" : "logo";
            var pixels = QrArtIllustrations.Draw(subject, 640);
            var options = QrIllustratedComposer.CreateOptions(style);
            var composition = QrIllustratedComposer.Render(qr, pixels, 640, 640, style, options);
            composition.Image.SavePng(Path.Combine(directory, style + ".png"));
            composition.SaveSvg(Path.Combine(directory, style + ".svg"));
            var report = QrArt.ValidateImage(composition.Image.ToPng(), payload, 3000);
            Console.WriteLine(style + ": " + string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}")));
        }
    }
}

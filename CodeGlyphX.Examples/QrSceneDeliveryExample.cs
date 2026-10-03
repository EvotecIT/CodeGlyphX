using System.Globalization;
using System.Text;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Examples;

internal static class QrSceneDeliveryExample {
    internal static void Run(string outputDir) {
        const string payload = "https://example.com/scenes";
        foreach (QrSceneStyle style in Enum.GetValues(typeof(QrSceneStyle))) {
            var scene = QrArt.ComposeScene(payload, QrScenePresets.Create(style));
            var export = scene.Export(new QrSceneExportOptions { WidthMillimeters = 100, Dpi = 300 });
            var prefix = Path.Combine(outputDir, "scene-print-" + style.ToString().ToLowerInvariant());
            var png = export.ToPng();
            File.WriteAllBytes(prefix + ".png", png);
            File.WriteAllText(prefix + ".svg", export.ToSvg());
            File.WriteAllBytes(prefix + ".pdf", export.ToPdf());
            export.Scene.ToRecipe().Save(prefix + ".cgxart");
            var report = QrArt.ValidateDelivery(png, payload, new QrImageDeliveryOptions {
                ScreenSize = 320, PrintMillimeters = export.WidthMillimeters, PrintDpi = export.Dpi
            });
            var text = new StringBuilder("CodeGlyphX scene delivery observations\n");
            text.AppendLine("Payload: " + payload).AppendLine("Width: " + export.WidthMillimeters.ToString(CultureInfo.InvariantCulture) + " mm; DPI: " + export.Dpi);
            foreach (var check in report.Checks) text.AppendLine($"{check.Name}: {(check.Passed ? "PASS" : "FAIL")} ({check.Width} x {check.Height} px)");
            text.AppendLine("Qualify actual target phones, viewers, paper and printers before distribution.");
            File.WriteAllText(prefix + "-checks.txt", text.ToString());
        }
    }
}

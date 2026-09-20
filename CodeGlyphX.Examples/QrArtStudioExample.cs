using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

/// <summary>Deterministic subject protection and print treatments, with measured scan results.</summary>
internal static class QrArtStudioExample {
    public static void Run(string outputDir) {
        const string payload = "https://example.com/art";
        var directory = Path.Combine(outputDir, "qr-art-studio");
        Directory.CreateDirectory(directory);
        var code = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        foreach (var scene in new[] { "portrait", "flower", "architecture", "logo" }) {
            var pixels = QrArtIllustrations.Draw(scene, 640);
            File.WriteAllBytes(Path.Combine(directory, scene + "-source.png"), PngImageEncoder.EncodeRgba32(pixels, 640, 640));
            foreach (var style in new[] { QrImageArtStyle.Engraving, QrImageArtStyle.Halftone, QrImageArtStyle.Contours, QrImageArtStyle.Mosaic, QrImageArtStyle.Botanical }) {
                foreach (var protect in new[] { false, true }) {
                    var options = new QrImageCompositionOptions {
                        ModuleSize = 16, Strength = 0.92,
                        Art = new QrImageArtOptions {
                            Style = style, FunctionalForeground = new Rgba32(27, 48, 47), FunctionalBackground = new Rgba32(246, 237, 216),
                            Subject = protect ? new QrImageSubjectOptions { X = .5, Y = scene == "flower" ? .38 : .5, Radius = .25 } : null
                        }
                    };
                    var result = QrImageComposer.Render(code, pixels, 640, 640, options);
                    var name = $"{scene}-{style}-{(protect ? "protected" : "baseline")}";
                    result.SavePng(Path.Combine(directory, name + ".png"));
                    var report = QrArt.ValidateImage(result.ToPng(), payload, 3000);
                    Console.WriteLine(name + ": " + string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}")));
                }
            }
        }
    }
}

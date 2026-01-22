using System;
using System.Collections.Generic;
using System.IO;

namespace CodeGlyphX.Examples;

internal static class QrDiagnosticsExample {
    public static void Run(string outputDir) {
        var samplePath = ResolveSample("Assets/DecodingSamples/qr-noisy-ui.png");
        var bytes = File.ReadAllBytes(samplePath);

        var maxMilliseconds = ReadIntEnv("CODEGLYPHX_DIAG_QR_MAXMS", 800);
        var aggressive = ReadBoolEnv("CODEGLYPHX_DIAG_QR_AGG", true);
        var disableTransforms = ReadBoolEnv("CODEGLYPHX_DIAG_QR_DISABLE_TRANSFORMS", false);
        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxMilliseconds = maxMilliseconds,
            MaxDimension = 1600,
            AggressiveSampling = aggressive,
            DisableTransforms = disableTransforms
        };

        var ok = QrImageDecoder.TryDecodeImage(bytes, out var decoded, out var info, options);
        var lines = new List<string> {
            $"Sample: {samplePath}",
            $"Result: {ok}",
            $"Info: {info}"
        };

        if (ok) {
            lines.Add($"Text: {decoded.Text}");
        }

        var outputPath = Path.Combine(outputDir, "qr-diagnostics.txt");
        File.WriteAllLines(outputPath, lines);
        Console.WriteLine(string.Join(Environment.NewLine, lines));
    }

    private static string ResolveSample(string relativePath) {
        if (string.IsNullOrWhiteSpace(relativePath)) {
            throw new ArgumentException("Path is required.", nameof(relativePath));
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate sample file '{relativePath}'.");
    }

    private static int ReadIntEnv(string name, int fallback) {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static bool ReadBoolEnv(string name, bool fallback) {
        var value = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }
}

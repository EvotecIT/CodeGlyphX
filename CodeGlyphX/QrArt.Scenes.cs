using System;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>Draws a deterministic illustrated scene with high error correction, protected functional patterns
    /// and a complete four-module quiet zone. Illustrations, captions and logos cannot cover the QR.
    /// Input settings are copied; no image service, model or network is used. Validate final delivery assets.</summary>
    public static QrSceneComposition ComposeScene(string payload, QrSceneOptions? options = null, CancellationToken cancellationToken = default) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        cancellationToken.ThrowIfCancellationRequested();
        var design = (options ?? QrScenePresets.Create(QrSceneStyle.TropicalGarden)).Clone();
        var code = QR.Encode(payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        return ComposeSceneCore(payload, code, design, cancellationToken);
    }

    private static QrSceneComposition ComposeSceneCore(string payload, QrCode code, QrSceneOptions design, CancellationToken token) {
        var moduleSize = GetSceneModuleSize(design, code);
        if (moduleSize < SceneMinimumModuleSize) throw new ArgumentException("Increase scene size or QR scale: at least two pixels per module are required.", nameof(design));
        var qrSize = moduleSize * (code.Size + 8);
        if (!TryGetScenePlacement(design, qrSize, out var x, out var y))
            throw new ArgumentException("The QR and its full quiet zone must fit inside the canvas.", nameof(design));
        var geometry = QrSceneIllustrations.Build(design);
        AddSceneCaption(geometry, design);
        var qrOptions = new QrPngRenderOptions { ModuleSize = moduleSize, QuietZone = 4, Foreground = design.Ink,
            Background = design.Paper, ModuleShape = design.ModuleShape, ModuleCornerRadiusPx = moduleSize / 3, ProtectFunctionalPatterns = true };
        var qrPixels = QrPngRenderer.RenderPixels(code.Modules, qrOptions, out _, out _, out _);
        var turns = (((int)Math.Round(design.Qr.RotationDegrees % 360 / 90)) % 4 + 4) % 4;
        var pixels = QrSceneRasterizer.Paint(design.Size, design, geometry, token);
        PaintSceneLogo(pixels, design, token);
        for (var yy = 0; yy < qrSize; yy++) {
            token.ThrowIfCancellationRequested();
            if (turns == 0) {
                Buffer.BlockCopy(qrPixels, yy * qrSize * 4, pixels, ((y + yy) * design.Size + x) * 4, qrSize * 4);
                continue;
            }
            for (var xx = 0; xx < qrSize; xx++) {
                var sx = turns == 1 ? yy : turns == 2 ? qrSize - 1 - xx : turns == 3 ? qrSize - 1 - yy : xx;
                var sy = turns == 1 ? qrSize - 1 - xx : turns == 2 ? qrSize - 1 - yy : turns == 3 ? xx : yy;
                Buffer.BlockCopy(qrPixels, (sy * qrSize + sx) * 4, pixels, ((y + yy) * design.Size + x + xx) * 4, 4);
            }
        }
        return new QrSceneComposition(payload, code, design, geometry, qrOptions, turns,
            new QrImageComposition(pixels, design.Size, x, y, qrSize));
    }

    private const int SceneMinimumModuleSize = 2;
    private static int GetSceneModuleSize(QrSceneOptions design, QrCode code) => (int)Math.Floor(design.Size * design.Qr.Scale / (code.Size + 8));
    private static bool TryGetScenePlacement(QrSceneOptions design, int qrSize, out int x, out int y) {
        x = (int)Math.Round(design.Qr.X * design.Size - qrSize / 2.0);
        y = (int)Math.Round(design.Qr.Y * design.Size - qrSize / 2.0);
        return x >= 0 && y >= 0 && x + qrSize <= design.Size && y + qrSize <= design.Size;
    }

    private static void AddSceneCaption(QrSceneGeometry geometry, QrSceneOptions design) {
        var text = design.Caption; if (!design.CaptionLayer.Visible || text.Length == 0) return;
        geometry.Layer(design.CaptionLayer);
        var unit = Math.Min(.009, .8 / (text.Length * 6 - 1));
        var origin = .5 - (text.Length * 6 - 1) * unit / 2;
        // The portable caption stays readable on both light and night-scene backdrops.
        geometry.Rect(design.Paper, origin - .018, .5 - 3.5 * unit - .014, (text.Length * 6 - 1) * unit + .036, 7 * unit + .028);
        for (var i = 0; i < text.Length; i++) {
            var glyph = BarcodeLabelFont.GetGlyph(text[i]);
            for (var y = 0; y < 7; y++) for (var x = 0; x < 5; x++) if ((glyph[y] & (1 << (4 - x))) != 0)
                geometry.Rect(design.Ink, origin + (i * 6 + x) * unit, .5 - 3.5 * unit + y * unit, unit, unit);
        }
    }

    private static void PaintSceneLogo(byte[] pixels, QrSceneOptions design, CancellationToken token) {
        if (design.LogoImage is null || !design.Logo.Visible) return;
        var source = ImageReader.DecodeRgba32(design.LogoImage, new ImageDecodeOptions { MaxBytes = 1024 * 1024, MaxPixels = 1_000_000, MaxDecodedBytes = 16_000_000 }, out var width, out var height);
        var logo = design.Logo; var extent = logo.Scale * design.Size;
        var drawWidth = extent * width / Math.Max(width, height); var drawHeight = extent * height / Math.Max(width, height);
        var side = (int)Math.Ceiling(extent * (Math.Abs(Math.Cos(logo.RotationDegrees % 360 * Math.PI / 180)) + Math.Abs(Math.Sin(logo.RotationDegrees % 360 * Math.PI / 180))));
        var centerX = logo.X * design.Size; var centerY = logo.Y * design.Size;
        var angle = -logo.RotationDegrees % 360 * Math.PI / 180; var cos = Math.Cos(angle); var sin = Math.Sin(angle);
        for (var y = Math.Max(0, (int)Math.Floor(centerY - side / 2.0)); y < Math.Min(design.Size, centerY + side / 2.0); y++) {
            token.ThrowIfCancellationRequested();
            for (var x = Math.Max(0, (int)Math.Floor(centerX - side / 2.0)); x < Math.Min(design.Size, centerX + side / 2.0); x++) {
                var dx = x + .5 - centerX; var dy = y + .5 - centerY;
                var sx = (dx * cos - dy * sin) / drawWidth + .5; var sy = (dx * sin + dy * cos) / drawHeight + .5;
                if (sx < 0 || sx >= 1 || sy < 0 || sy >= 1) continue;
                var p = (y * design.Size + x) * 4; var q = ((int)(sy * height) * width + (int)(sx * width)) * 4;
                var a = source[q + 3];
                for (var c = 0; c < 3; c++) pixels[p + c] = (byte)((source[q + c] * a + pixels[p + c] * (255 - a) + 127) / 255);
            }
        }
    }
}

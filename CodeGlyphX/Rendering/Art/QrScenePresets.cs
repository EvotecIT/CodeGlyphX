using CodeGlyphX.Rendering;
using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Fresh coordinated scene settings which callers may edit independently.</summary>
public static class QrScenePresets {
    /// <summary>Creates a scene palette, caption and QR treatment. Does not retain shared mutable settings.</summary>
    public static QrSceneOptions Create(QrSceneStyle style, int size = 1200) {
        if (!Enum.IsDefined(typeof(QrSceneStyle), style)) throw new ArgumentOutOfRangeException(nameof(style));
        var options = new QrSceneOptions { Style = style, Size = size };
        options.Colors = style switch {
            QrSceneStyle.TropicalGarden => Palette(0xEF5466, 0x1F8F68, 0xFFC45C, 0x125B4C),
            QrSceneStyle.ElectricCity => Palette(0xEC36AF, 0x39DCD1, 0xFACD51, 0x6150DB),
            QrSceneStyle.MusicFestival => Palette(0xF1535D, 0x17A9BA, 0xF7C64C, 0x824CC5),
            QrSceneStyle.OceanReef => Palette(0xEF718C, 0x35C5D0, 0xFFAA4C, 0x1F5C9E),
            QrSceneStyle.CosmicOrbit => Palette(0xDD4F97, 0x49C6D2, 0xFFCA67, 0x6D56BF),
            _ => Palette(0xED46AD, 0x33CDAA, 0xF4CD47, 0x6568D8)
        };
        // Each composition places the code in its own local backdrop; captions sit where the scene leaves room.
        // Scales of 0.46..0.54 leave room for the illustration and keep three pixels per module from about 320 pixels
        // (short URLs) or 450 pixels (130-character URLs); smaller renders throw instead of degrading the code.
        var (qrX, qrY, qrScale, captionY, captionScale) = style switch {
            QrSceneStyle.TropicalGarden => (0.5, 0.43, 0.46, 0.737, 0.52),
            QrSceneStyle.ElectricCity => (0.5, 0.37, 0.5, 0.958, 0.72),
            QrSceneStyle.MusicFestival => (0.5, 0.44, 0.5, 0.093, 0.8),
            QrSceneStyle.OceanReef => (0.5, 0.43, 0.5, 0.95, 0.66),
            QrSceneStyle.CosmicOrbit => (0.5, 0.44, 0.5, 0.95, 0.72),
            _ => (0.5, 0.4, 0.54, 0.79, 1.0)
        };
        options.Qr.X = qrX; options.Qr.Y = qrY; options.Qr.Scale = qrScale;
        options.CaptionLayer.Y = captionY; options.CaptionLayer.Scale = captionScale;
        options.Caption = style switch {
            QrSceneStyle.TropicalGarden => "GROW SOMETHING GOOD",
            QrSceneStyle.ElectricCity => "CONNECT THE CITY",
            QrSceneStyle.MusicFestival => "FEEL THE RHYTHM",
            QrSceneStyle.OceanReef => "DIVE INTO COLOR",
            QrSceneStyle.CosmicOrbit => "EXPLORE YOUR UNIVERSE",
            _ => "PRESS START"
        };
        // The code takes a deep tone and a module silhouette from its own scene, on a backdrop tinted to match,
        // so it reads as part of the illustration rather than a pasted-on label.
        (options.Ink, options.Paper, options.ModuleShape) = style switch {
            QrSceneStyle.TropicalGarden => (Color(0x10483A), Color(0xFFFAEE), QrModuleShape.Leaf),
            QrSceneStyle.ElectricCity => (Color(0x261E62), Color(0xF6F4FF), QrModuleShape.ConnectedSquircle),
            QrSceneStyle.MusicFestival => (Color(0x3A1A5C), Color(0xFFF7F0), QrModuleShape.Rounded),
            QrSceneStyle.OceanReef => (Color(0x0E345E), Color(0xF7FCFA), QrModuleShape.Circle),
            QrSceneStyle.CosmicOrbit => (Color(0x281E56), Color(0xFFFAEC), QrModuleShape.Circle),
            _ => (Color(0x1E1A46), Color(0xFFF9ED), QrModuleShape.Square)
        };
        options.Validate();
        return options;
    }
    private static Rgba32[] Palette(params int[] values) {
        var colors = new Rgba32[values.Length];
        for (var i = 0; i < colors.Length; i++) colors[i] = Color(values[i]);
        return colors;
    }
    private static Rgba32 Color(int value) => new((byte)(value >> 16), (byte)(value >> 8), (byte)value);
}

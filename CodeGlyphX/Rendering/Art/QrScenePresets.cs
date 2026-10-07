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
            QrSceneStyle.TropicalGarden => Palette(0xEF4770, 0x17A887, 0xFFC857, 0x196F74),
            QrSceneStyle.ElectricCity => Palette(0xEC36AF, 0x39DCD1, 0xFACD51, 0x6150DB),
            QrSceneStyle.MusicFestival => Palette(0xF1535D, 0x17A9BA, 0xF7C64C, 0x824CC5),
            QrSceneStyle.OceanReef => Palette(0xEF718C, 0x35C5D0, 0xFFBC55, 0x3A75AF),
            QrSceneStyle.CosmicOrbit => Palette(0xDD4F97, 0x49C6D2, 0xFFCA67, 0x6D56BF),
            _ => Palette(0xED46AD, 0x33CDAA, 0xF4CD47, 0x6568D8)
        };
        options.Caption = style switch {
            QrSceneStyle.TropicalGarden => "GROW SOMETHING GOOD",
            QrSceneStyle.ElectricCity => "CONNECT THE CITY",
            QrSceneStyle.MusicFestival => "FEEL THE RHYTHM",
            QrSceneStyle.OceanReef => "DIVE INTO COLOR",
            QrSceneStyle.CosmicOrbit => "EXPLORE YOUR UNIVERSE",
            _ => "PRESS START"
        };
        options.ModuleShape = style == QrSceneStyle.RetroArcade ? QrModuleShape.Square : QrModuleShape.ConnectedRounded;
        options.Validate();
        return options;
    }
    private static Rgba32[] Palette(params int[] values) {
        var colors = new Rgba32[values.Length];
        for (var i = 0; i < colors.Length; i++) colors[i] = new((byte)(values[i] >> 16), (byte)(values[i] >> 8), (byte)values[i]);
        return colors;
    }
}

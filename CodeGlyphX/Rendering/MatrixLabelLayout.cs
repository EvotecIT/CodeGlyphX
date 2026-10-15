using System;

namespace CodeGlyphX.Rendering;

/// <summary>Composes a matrix and its portable caption while preserving the symbol's quiet zone.</summary>
internal static class MatrixLabelLayout {
    internal static BitMatrix Compose(BitMatrix modules, MatrixOptions options) {
        if (options.ModuleSize < 1) throw new ArgumentOutOfRangeException(nameof(options.ModuleSize));
        if (options.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(options.QuietZone));
        if (options.LabelFontSize < 1) throw new ArgumentOutOfRangeException(nameof(options.LabelFontSize));
        if (options.LabelMargin < 0) throw new ArgumentOutOfRangeException(nameof(options.LabelMargin));
        var text = BarcodeLabelText.Normalize(options.LabelText);
        var scale = Math.Max(1, checked((int)(((long)options.LabelFontSize + 7L * options.ModuleSize - 1) / (7L * options.ModuleSize))));
        var gap = checked((int)(((long)options.LabelMargin + options.ModuleSize - 1) / options.ModuleSize));
        var textWidth = checked((text.Length * 6 - 1) * scale);
        var width = Math.Max(checked(modules.Width + options.QuietZone * 2), checked(textWidth + options.QuietZone * 2));
        var labelTop = checked(modules.Height + options.QuietZone * 2 + gap);
        var output = new BitMatrix(width, checked(labelTop + 7 * scale + options.QuietZone));
        var symbolLeft = (width - modules.Width) / 2;
        for (var y = 0; y < modules.Height; y++)
            for (var x = 0; x < modules.Width; x++) output[symbolLeft + x, options.QuietZone + y] = modules[x, y];
        var textLeft = (width - textWidth) / 2;
        for (var character = 0; character < text.Length; character++) {
            var glyph = BarcodeLabelFont.GetGlyph(text[character]);
            for (var y = 0; y < 7; y++) for (var x = 0; x < 5; x++) {
                if ((glyph[y] & (1 << (4 - x))) == 0) continue;
                for (var dy = 0; dy < scale; dy++) for (var dx = 0; dx < scale; dx++)
                    output[textLeft + character * 6 * scale + x * scale + dx, labelTop + y * scale + dy] = true;
            }
        }
        return output;
    }
}

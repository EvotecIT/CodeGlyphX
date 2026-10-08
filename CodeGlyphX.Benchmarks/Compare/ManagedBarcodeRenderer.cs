#if COMPARE_BARCODER
using System;
using Barcoder;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Benchmarks;

/// <summary>Adapts Barcoder's modules to the same managed PNG encoder used by the ZXing comparison.</summary>
internal sealed class ManagedBarcodeRenderer
{
    private readonly int _moduleSize;
    private readonly int _quietZone;
    private readonly int _barHeight;

    public ManagedBarcodeRenderer(int moduleSize, int quietZone, int barHeight)
    {
        if (moduleSize <= 0) throw new ArgumentOutOfRangeException(nameof(moduleSize));
        if (quietZone < 0) throw new ArgumentOutOfRangeException(nameof(quietZone));
        if (barHeight <= 0) throw new ArgumentOutOfRangeException(nameof(barHeight));
        _moduleSize = moduleSize;
        _quietZone = quietZone;
        _barHeight = barHeight;
    }

    public byte[] Render(IBarcode barcode)
    {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        var isLinear = barcode.Bounds.Y == 1;
        var width = checked((barcode.Bounds.X + _quietZone * 2) * _moduleSize);
        var height = checked((isLinear ? _barHeight : barcode.Bounds.Y + _quietZone * 2) * _moduleSize);
        var pixels = new byte[checked(width * height * 4)];
        Array.Fill(pixels, (byte)255);
        var top = isLinear ? 0 : _quietZone * _moduleSize;
        for (var y = 0; y < barcode.Bounds.Y; y++)
        {
            for (var x = 0; x < barcode.Bounds.X; x++)
            {
                if (!barcode.At(x, y)) continue;
                var left = (x + _quietZone) * _moduleSize;
                var firstRow = top + y * _moduleSize;
                var rows = isLinear ? height : _moduleSize;
                for (var row = firstRow; row < firstRow + rows; row++)
                {
                    for (var column = left; column < left + _moduleSize; column++)
                    {
                        var offset = (row * width + column) * 4;
                        pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 0;
                    }
                }
            }
        }
        return PngImageEncoder.EncodeRgba32(pixels, width, height);
    }
}
#endif

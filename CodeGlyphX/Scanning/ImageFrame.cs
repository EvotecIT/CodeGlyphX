using System;

namespace CodeGlyphX;

/// <summary>
/// Describes a non-owning raw image buffer supplied to the symbol scanner.
/// </summary>
/// <remarks>
/// The caller remains responsible for the lifetime and mutation of the underlying memory for the duration of a scan.
/// </remarks>
public sealed class ImageFrame {
    /// <summary>Gets the raw pixel memory.</summary>
    public ReadOnlyMemory<byte> Pixels { get; }
    /// <summary>Gets the image width in pixels.</summary>
    public int Width { get; }
    /// <summary>Gets the image height in pixels.</summary>
    public int Height { get; }
    /// <summary>Gets the number of bytes between adjacent rows.</summary>
    public int Stride { get; }
    /// <summary>Gets the raw pixel format.</summary>
    public PixelFormat PixelFormat { get; }
    /// <summary>Gets the row order in the source buffer.</summary>
    public ImageRowOrder RowOrder { get; }

    /// <summary>
    /// Creates a raw image frame.
    /// </summary>
    public ImageFrame(
        ReadOnlyMemory<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat pixelFormat,
        ImageRowOrder rowOrder = ImageRowOrder.TopDown) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (!Enum.IsDefined(typeof(PixelFormat), pixelFormat)) throw new ArgumentOutOfRangeException(nameof(pixelFormat));
        if (!Enum.IsDefined(typeof(ImageRowOrder), rowOrder)) throw new ArgumentOutOfRangeException(nameof(rowOrder));

        var rowBytes = GetMinimumRowBytes(width, pixelFormat);
        if (stride < rowBytes) throw new ArgumentOutOfRangeException(nameof(stride), $"Stride must be at least {rowBytes} bytes for {pixelFormat}.");
        var requiredLength = checked((height - 1) * stride + rowBytes);
        if (pixels.Length < requiredLength) {
            throw new ArgumentException($"Pixel memory contains {pixels.Length} bytes but at least {requiredLength} bytes are required.", nameof(pixels));
        }

        Pixels = pixels;
        Width = width;
        Height = height;
        Stride = stride;
        PixelFormat = pixelFormat;
        RowOrder = rowOrder;
    }

    /// <summary>
    /// Creates a raw image frame whose stride is the tightly packed row length for the selected format.
    /// </summary>
    public static ImageFrame Packed(
        ReadOnlyMemory<byte> pixels,
        int width,
        int height,
        PixelFormat pixelFormat,
        ImageRowOrder rowOrder = ImageRowOrder.TopDown) {
        return new ImageFrame(pixels, width, height, GetMinimumRowBytes(width, pixelFormat), pixelFormat, rowOrder);
    }

    /// <summary>
    /// Gets the minimum valid row length for a packed pixel format.
    /// </summary>
    public static int GetMinimumRowBytes(int width, PixelFormat pixelFormat) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        return checked(width * GetBytesPerPixel(pixelFormat));
    }

    /// <summary>
    /// Gets the byte width of one pixel for a supported packed format.
    /// </summary>
    public static int GetBytesPerPixel(PixelFormat pixelFormat) {
        switch (pixelFormat) {
            case PixelFormat.Gray8:
                return 1;
            case PixelFormat.Gray16LittleEndian:
            case PixelFormat.Rgb565LittleEndian:
                return 2;
            case PixelFormat.Rgb24:
            case PixelFormat.Bgr24:
                return 3;
            case PixelFormat.Bgra32:
            case PixelFormat.Rgba32:
            case PixelFormat.Argb32:
            case PixelFormat.Abgr32:
                return 4;
            default:
                throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, "Unsupported packed pixel format.");
        }
    }
}

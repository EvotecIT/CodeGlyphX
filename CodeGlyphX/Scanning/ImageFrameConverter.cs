using System;
using System.Runtime.InteropServices;

namespace CodeGlyphX;

internal static class ImageFrameConverter {
    internal static byte[] ToRgba32(ImageFrame frame, ImageRegion region, out int width, out int height) {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        width = region.Width;
        height = region.Height;

        if (CanReuseRgba32(frame, region, out var existing)) return existing;

        var destination = new byte[checked(width * height * 4)];
        var source = frame.Pixels.Span;
        var bytesPerPixel = ImageFrame.GetBytesPerPixel(frame.PixelFormat);

        for (var y = 0; y < height; y++) {
            var imageY = region.Y + y;
            var sourceY = frame.RowOrder == ImageRowOrder.TopDown ? imageY : frame.Height - 1 - imageY;
            var sourceOffset = checked(sourceY * frame.Stride + region.X * bytesPerPixel);
            var destinationOffset = y * width * 4;
            ConvertRow(source.Slice(sourceOffset), destination, destinationOffset, width, frame.PixelFormat);
        }

        return destination;
    }

    private static bool CanReuseRgba32(ImageFrame frame, ImageRegion region, out byte[] pixels) {
        pixels = Array.Empty<byte>();
        if (frame.PixelFormat != PixelFormat.Rgba32 || frame.RowOrder != ImageRowOrder.TopDown) return false;
        if (region.X != 0 || region.Y != 0 || region.Width != frame.Width || region.Height != frame.Height) return false;
        if (frame.Stride != frame.Width * 4) return false;
        if (!MemoryMarshal.TryGetArray(frame.Pixels, out ArraySegment<byte> segment)) return false;
        if (segment.Array is null || segment.Offset != 0 || segment.Count < frame.Stride * frame.Height) return false;
        pixels = segment.Array;
        return true;
    }

    private static void ConvertRow(ReadOnlySpan<byte> source, byte[] destination, int destinationOffset, int width, PixelFormat format) {
        for (var x = 0; x < width; x++) {
            var target = destinationOffset + x * 4;
            switch (format) {
                case PixelFormat.Rgba32: {
                    var offset = x * 4;
                    destination[target] = source[offset];
                    destination[target + 1] = source[offset + 1];
                    destination[target + 2] = source[offset + 2];
                    destination[target + 3] = source[offset + 3];
                    break;
                }
                case PixelFormat.Bgra32: {
                    var offset = x * 4;
                    destination[target] = source[offset + 2];
                    destination[target + 1] = source[offset + 1];
                    destination[target + 2] = source[offset];
                    destination[target + 3] = source[offset + 3];
                    break;
                }
                case PixelFormat.Argb32: {
                    var offset = x * 4;
                    destination[target] = source[offset + 1];
                    destination[target + 1] = source[offset + 2];
                    destination[target + 2] = source[offset + 3];
                    destination[target + 3] = source[offset];
                    break;
                }
                case PixelFormat.Abgr32: {
                    var offset = x * 4;
                    destination[target] = source[offset + 3];
                    destination[target + 1] = source[offset + 2];
                    destination[target + 2] = source[offset + 1];
                    destination[target + 3] = source[offset];
                    break;
                }
                case PixelFormat.Rgb24: {
                    var offset = x * 3;
                    destination[target] = source[offset];
                    destination[target + 1] = source[offset + 1];
                    destination[target + 2] = source[offset + 2];
                    destination[target + 3] = 255;
                    break;
                }
                case PixelFormat.Bgr24: {
                    var offset = x * 3;
                    destination[target] = source[offset + 2];
                    destination[target + 1] = source[offset + 1];
                    destination[target + 2] = source[offset];
                    destination[target + 3] = 255;
                    break;
                }
                case PixelFormat.Gray8: {
                    var value = source[x];
                    destination[target] = value;
                    destination[target + 1] = value;
                    destination[target + 2] = value;
                    destination[target + 3] = 255;
                    break;
                }
                case PixelFormat.Gray16LittleEndian: {
                    var offset = x * 2;
                    var value16 = source[offset] | (source[offset + 1] << 8);
                    var value = (byte)((value16 + 128) / 257);
                    destination[target] = value;
                    destination[target + 1] = value;
                    destination[target + 2] = value;
                    destination[target + 3] = 255;
                    break;
                }
                case PixelFormat.Rgb565LittleEndian: {
                    var offset = x * 2;
                    var packed = source[offset] | (source[offset + 1] << 8);
                    var red = (packed >> 11) & 0x1F;
                    var green = (packed >> 5) & 0x3F;
                    var blue = packed & 0x1F;
                    destination[target] = (byte)((red * 255 + 15) / 31);
                    destination[target + 1] = (byte)((green * 255 + 31) / 63);
                    destination[target + 2] = (byte)((blue * 255 + 15) / 31);
                    destination[target + 3] = 255;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported pixel format.");
            }
        }
    }
}

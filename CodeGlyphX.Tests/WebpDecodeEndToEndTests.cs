using System;
using System.Runtime.InteropServices;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

public sealed class WebpDecodeEndToEndTests {
    private readonly ITestOutputHelper _output;

    public WebpDecodeEndToEndTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Webp_EndToEnd_QrDecode_LosslessNative() {
        const string payload = "WEBP-END-TO-END";

        var modules = QrCodeEncoder.EncodeText(payload).Modules;
        const int moduleSize = 8;
        const int quietZone = 4;

        var (rgba, width, height, stride) = RenderQrToRgba(modules, moduleSize, quietZone);
        if (!TryEncodeWebpLossless(rgba, width, height, stride, out var webp, out var reason)) {
            _output.WriteLine(reason);
            return;
        }

        Assert.True(ImageReader.TryDetectFormat(webp, out var format));
        Assert.Equal(ImageFormat.Webp, format);

        Assert.True(ImageReader.TryReadInfo(webp, out var info));
        Assert.Equal(width, info.Width);
        Assert.Equal(height, info.Height);

        Assert.True(ImageReader.TryDecodeRgba32(webp, out var decodedRgba, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(width * height * 4, decodedRgba.Length);

        Assert.True(QrDecoder.TryDecode(decodedRgba, decodedWidth, decodedHeight, decodedWidth * 4, PixelFormat.Rgba32, out var decoded, new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true,
            MaxMilliseconds = 2000
        }));
        Assert.Equal(payload, decoded.Text);
    }

    private static (byte[] rgba, int width, int height, int stride) RenderQrToRgba(BitMatrix modules, int moduleSize, int quietZone) {
        var modulesWidth = modules.Width + (quietZone * 2);
        var modulesHeight = modules.Height + (quietZone * 2);
        var width = checked(modulesWidth * moduleSize);
        var height = checked(modulesHeight * moduleSize);
        var stride = checked(width * 4);
        var rgba = new byte[checked(height * stride)];

        Fill(rgba, stride, width, height, 255, 255, 255, 255);

        for (var y = 0; y < modules.Height; y++) {
            for (var x = 0; x < modules.Width; x++) {
                if (!modules[x, y]) continue;
                var px = (x + quietZone) * moduleSize;
                var py = (y + quietZone) * moduleSize;
                FillBlock(rgba, stride, px, py, moduleSize, moduleSize, 0, 0, 0, 255);
            }
        }

        return (rgba, width, height, stride);
    }

    private static void Fill(byte[] rgba, int stride, int width, int height, byte r, byte g, byte b, byte a) {
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var i = row + (x * 4);
                rgba[i] = r;
                rgba[i + 1] = g;
                rgba[i + 2] = b;
                rgba[i + 3] = a;
            }
        }
    }

    private static void FillBlock(byte[] rgba, int stride, int x, int y, int blockWidth, int blockHeight, byte r, byte g, byte b, byte a) {
        for (var yy = 0; yy < blockHeight; yy++) {
            var row = (y + yy) * stride;
            for (var xx = 0; xx < blockWidth; xx++) {
                var i = row + ((x + xx) * 4);
                rgba[i] = r;
                rgba[i + 1] = g;
                rgba[i + 2] = b;
                rgba[i + 3] = a;
            }
        }
    }

    private static bool TryEncodeWebpLossless(byte[] rgba, int width, int height, int stride, out byte[] webp, out string reason) {
        webp = Array.Empty<byte>();
        reason = string.Empty;
        try {
            webp = EncodeWebpLossless(rgba, width, height, stride);
            reason = "libwebp lossless encode succeeded.";
            return true;
        } catch (DllNotFoundException ex) {
            reason = $"libwebp not found: {ex.Message}";
        } catch (EntryPointNotFoundException ex) {
            reason = $"libwebp encode API not found: {ex.Message}";
        } catch (BadImageFormatException ex) {
            reason = $"libwebp incompatible: {ex.Message}";
        }
        return false;
    }

    private static byte[] EncodeWebpLossless(byte[] rgba, int width, int height, int stride) {
        if (rgba.Length == 0) throw new InvalidOperationException("RGBA buffer is empty.");
        if (width <= 0 || height <= 0) throw new InvalidOperationException("Invalid image dimensions.");

        var size = TryEncodeLossless(rgba, width, height, stride, out var output);
        if (size == UIntPtr.Zero || output == IntPtr.Zero) {
            throw new InvalidOperationException("libwebp lossless encode failed.");
        }

        try {
            var length = checked((int)size);
            var webp = new byte[length];
            Marshal.Copy(output, webp, 0, length);
            return webp;
        } finally {
            TryFree(output);
        }
    }

    private static UIntPtr TryEncodeLossless(byte[] rgba, int width, int height, int stride, out IntPtr output) {
        output = IntPtr.Zero;

        try {
            var size = WebPEncodeLosslessRGBA_libwebp(rgba, width, height, stride, out output);
            if (size != UIntPtr.Zero && output != IntPtr.Zero) return size;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            var size = WebPEncodeLosslessRGBA_so7(rgba, width, height, stride, out output);
            if (size != UIntPtr.Zero && output != IntPtr.Zero) return size;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            var size = WebPEncodeLosslessRGBA_so(rgba, width, height, stride, out output);
            if (size != UIntPtr.Zero && output != IntPtr.Zero) return size;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            var size = WebPEncodeLosslessRGBA_dll(rgba, width, height, stride, out output);
            if (size != UIntPtr.Zero && output != IntPtr.Zero) return size;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            var size = WebPEncodeLosslessRGBA_dylib(rgba, width, height, stride, out output);
            if (size != UIntPtr.Zero && output != IntPtr.Zero) return size;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        output = IntPtr.Zero;
        return UIntPtr.Zero;
    }

    private static void TryFree(IntPtr ptr) {
        if (ptr == IntPtr.Zero) return;

        try {
            WebPFree_libwebp(ptr);
            return;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            WebPFree_so7(ptr);
            return;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            WebPFree_so(ptr);
            return;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            WebPFree_dll(ptr);
            return;
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }

        try {
            WebPFree_dylib(ptr);
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) {
        } catch (BadImageFormatException) {
        }
    }

    [DllImport("libwebp", EntryPoint = "WebPEncodeLosslessRGBA")]
    private static extern UIntPtr WebPEncodeLosslessRGBA_libwebp(byte[] rgba, int width, int height, int stride, out IntPtr output);

    [DllImport("libwebp.so.7", EntryPoint = "WebPEncodeLosslessRGBA")]
    private static extern UIntPtr WebPEncodeLosslessRGBA_so7(byte[] rgba, int width, int height, int stride, out IntPtr output);

    [DllImport("libwebp.so", EntryPoint = "WebPEncodeLosslessRGBA")]
    private static extern UIntPtr WebPEncodeLosslessRGBA_so(byte[] rgba, int width, int height, int stride, out IntPtr output);

    [DllImport("libwebp.dll", EntryPoint = "WebPEncodeLosslessRGBA")]
    private static extern UIntPtr WebPEncodeLosslessRGBA_dll(byte[] rgba, int width, int height, int stride, out IntPtr output);

    [DllImport("libwebp.dylib", EntryPoint = "WebPEncodeLosslessRGBA")]
    private static extern UIntPtr WebPEncodeLosslessRGBA_dylib(byte[] rgba, int width, int height, int stride, out IntPtr output);

    [DllImport("libwebp", EntryPoint = "WebPFree")]
    private static extern void WebPFree_libwebp(IntPtr pointer);

    [DllImport("libwebp.so.7", EntryPoint = "WebPFree")]
    private static extern void WebPFree_so7(IntPtr pointer);

    [DllImport("libwebp.so", EntryPoint = "WebPFree")]
    private static extern void WebPFree_so(IntPtr pointer);

    [DllImport("libwebp.dll", EntryPoint = "WebPFree")]
    private static extern void WebPFree_dll(IntPtr pointer);

    [DllImport("libwebp.dylib", EntryPoint = "WebPFree")]
    private static extern void WebPFree_dylib(IntPtr pointer);
}

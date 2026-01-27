using System;
using System.Runtime.InteropServices;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Minimal WebP container parsing plus native decode via libwebp when available.
/// </summary>
/// <remarks>
/// This native decode path is a temporary fallback and should be replaced by a
/// fully managed decoder when available.
/// </remarks>
public static class WebpReader {
    private const uint FourCcRiff = 0x46464952; // "RIFF"
    private const uint FourCcWebp = 0x50424557; // "WEBP"
    private const uint FourCcVp8X = 0x58385056; // "VP8X"
    private const uint FourCcVp8L = 0x4C385056; // "VP8L"
    private const uint FourCcVp8 = 0x20385056;  // "VP8 "

    /// <summary>
    /// Checks whether the buffer looks like a WebP RIFF container.
    /// </summary>
    public static bool IsWebp(ReadOnlySpan<byte> data) {
        if (data.Length < 12) return false;
        return ReadU32LE(data, 0) == FourCcRiff && ReadU32LE(data, 8) == FourCcWebp;
    }

    /// <summary>
    /// Attempts to read WebP dimensions from the RIFF container without decoding pixels.
    /// </summary>
    public static bool TryReadDimensions(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!IsWebp(data)) return false;

        if (data.Length < 12) return false;
        var riffSize = ReadU32LE(data, 4);
        var riffLimit = data.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = (int)declaredLimit;
        }
        if (riffLimit < 12) return false;

        var offset = 12;
        while (offset + 8 <= riffLimit) {
            var fourCc = ReadU32LE(data, offset);
            var chunkSize = ReadU32LE(data, offset + 4);
            var dataOffset = offset + 8;

            if (chunkSize > int.MaxValue) return false;
            var chunkLength = (int)chunkSize;
            if (dataOffset < 0 || dataOffset > riffLimit) return false;
            if (dataOffset + chunkLength > riffLimit) return false;

            var chunk = data.Slice(dataOffset, chunkLength);
            if (fourCc == FourCcVp8X && TryReadVp8XSize(chunk, out width, out height)) return true;
            if (fourCc == FourCcVp8L && TryReadVp8LSize(chunk, out width, out height)) return true;
            if (fourCc == FourCcVp8 && TryReadVp8Size(chunk, out width, out height)) return true;

            var padded = chunkLength + (chunkLength & 1);
            var nextOffset = (long)dataOffset + padded;
            if (nextOffset < 0 || nextOffset > riffLimit || nextOffset > int.MaxValue) return false;
            offset = (int)nextOffset;
        }

        return false;
    }

    /// <summary>
    /// Decodes a WebP image to RGBA32 using native libwebp when available.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (!IsWebp(data)) throw new FormatException("Invalid WebP container.");
        if (WebpManagedDecoder.TryDecodeRgba32(data, out var managedRgba, out width, out height)) {
            return managedRgba;
        }
        if (!IsNativeAvailable) {
            throw new FormatException("WebP decode requires native libwebp (temporary fallback until managed decode is implemented).");
        }

        var buffer = data.ToArray();
        if (!WebpNative.TryDecodeRgba32(buffer, out var rgba, out width, out height)) {
            throw new FormatException("WebP decode failed using native libwebp (temporary fallback until managed decode is implemented).");
        }
        return rgba;
    }

    /// <summary>
    /// Gets whether native libwebp appears to be available for decode on this machine.
    /// </summary>
    public static bool IsNativeAvailable => WebpNative.IsAvailable;

    private static bool TryReadVp8XSize(ReadOnlySpan<byte> chunk, out int width, out int height) {
        width = 0;
        height = 0;
        if (chunk.Length < 10) return false;
        var widthMinus1 = ReadU24LE(chunk, 4);
        var heightMinus1 = ReadU24LE(chunk, 7);
        width = widthMinus1 + 1;
        height = heightMinus1 + 1;
        return width > 0 && height > 0;
    }

    private static bool TryReadVp8LSize(ReadOnlySpan<byte> chunk, out int width, out int height) {
        width = 0;
        height = 0;
        if (chunk.Length < 5) return false;
        if (chunk[0] != 0x2F) return false;
        var bits = ReadU32LE(chunk, 1);
        width = (int)(bits & 0x3FFF) + 1;
        height = (int)((bits >> 14) & 0x3FFF) + 1;
        return width > 0 && height > 0;
    }

    private static bool TryReadVp8Size(ReadOnlySpan<byte> chunk, out int width, out int height) {
        width = 0;
        height = 0;
        if (chunk.Length < 10) return false;
        if (chunk[3] != 0x9D || chunk[4] != 0x01 || chunk[5] != 0x2A) return false;
        width = ReadU16LE(chunk, 6) & 0x3FFF;
        height = ReadU16LE(chunk, 8) & 0x3FFF;
        return width > 0 && height > 0;
    }

    private static int ReadU16LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 2 > data.Length) return 0;
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadU24LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 3 > data.Length) return 0;
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
    }

    private static uint ReadU32LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }

    private static class WebpNative {
        private static readonly Lazy<bool> _isAvailable = new Lazy<bool>(ProbeNativeAvailability);

        public static bool IsAvailable => _isAvailable.Value;

        public static bool TryDecodeRgba32(byte[] data, out byte[] rgba, out int width, out int height) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            if (data.Length == 0) return false;

            var decoded = TryDecode(data, out var w, out var h);
            if (decoded == IntPtr.Zero || w <= 0 || h <= 0) return false;

            try {
                var pixelBytes = checked(w * h * 4);
                rgba = new byte[pixelBytes];
                Marshal.Copy(decoded, rgba, 0, rgba.Length);
                width = w;
                height = h;
                return true;
            } finally {
                TryFree(decoded);
            }
        }

        private static IntPtr TryDecode(byte[] data, out int width, out int height) {
            width = 0;
            height = 0;
            try {
                var ptr = WebPDecodeRGBA_libwebp(data, (UIntPtr)(uint)data.Length, out width, out height);
                if (ptr != IntPtr.Zero && width > 0 && height > 0) return ptr;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var ptr = WebPDecodeRGBA_so7(data, (UIntPtr)(uint)data.Length, out width, out height);
                if (ptr != IntPtr.Zero && width > 0 && height > 0) return ptr;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var ptr = WebPDecodeRGBA_so(data, (UIntPtr)(uint)data.Length, out width, out height);
                if (ptr != IntPtr.Zero && width > 0 && height > 0) return ptr;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var ptr = WebPDecodeRGBA_dll(data, (UIntPtr)(uint)data.Length, out width, out height);
                if (ptr != IntPtr.Zero && width > 0 && height > 0) return ptr;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var ptr = WebPDecodeRGBA_dylib(data, (UIntPtr)(uint)data.Length, out width, out height);
                if (ptr != IntPtr.Zero && width > 0 && height > 0) return ptr;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            width = 0;
            height = 0;
            return IntPtr.Zero;
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

        private static bool ProbeNativeAvailability() {
            try {
                var version = WebPGetDecoderVersion_libwebp();
                if (version != 0) return true;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var version = WebPGetDecoderVersion_so7();
                if (version != 0) return true;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var version = WebPGetDecoderVersion_so();
                if (version != 0) return true;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var version = WebPGetDecoderVersion_dll();
                if (version != 0) return true;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            try {
                var version = WebPGetDecoderVersion_dylib();
                if (version != 0) return true;
            } catch (DllNotFoundException) {
            } catch (EntryPointNotFoundException) {
            } catch (BadImageFormatException) {
            }

            return false;
        }

        [DllImport("libwebp", EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_libwebp();

        [DllImport("libwebp.so.7", EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_so7();

        [DllImport("libwebp.so", EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_so();

        [DllImport("libwebp.dll", EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_dll();

        [DllImport("libwebp.dylib", EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_dylib();

        [DllImport("libwebp", EntryPoint = "WebPDecodeRGBA")]
        private static extern IntPtr WebPDecodeRGBA_libwebp(byte[] data, UIntPtr dataSize, out int width, out int height);

        [DllImport("libwebp.so.7", EntryPoint = "WebPDecodeRGBA")]
        private static extern IntPtr WebPDecodeRGBA_so7(byte[] data, UIntPtr dataSize, out int width, out int height);

        [DllImport("libwebp.so", EntryPoint = "WebPDecodeRGBA")]
        private static extern IntPtr WebPDecodeRGBA_so(byte[] data, UIntPtr dataSize, out int width, out int height);

        [DllImport("libwebp.dll", EntryPoint = "WebPDecodeRGBA")]
        private static extern IntPtr WebPDecodeRGBA_dll(byte[] data, UIntPtr dataSize, out int width, out int height);

        [DllImport("libwebp.dylib", EntryPoint = "WebPDecodeRGBA")]
        private static extern IntPtr WebPDecodeRGBA_dylib(byte[] data, UIntPtr dataSize, out int width, out int height);

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
}

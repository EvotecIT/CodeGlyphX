using System;
using System.Runtime.InteropServices;

namespace CodeMatrix.ScreenScan.Wpf;

internal static class ScreenCapture {
    private const int SRCCOPY = 0x00CC0020;
    private const int CAPTUREBLT = 0x40000000;

    public sealed class Session : IDisposable {
        private readonly int _width;
        private readonly int _height;
        private readonly int _stride;
        private readonly byte[] _buffer;

        private IntPtr _hdcScreen;
        private IntPtr _hdcMem;
        private IntPtr _hBmp;
        private IntPtr _bitsPtr;
        private IntPtr _oldObj;

        public int Width => _width;
        public int Height => _height;
        public int Stride => _stride;
        public byte[] Buffer => _buffer;

        public Session(int width, int height) {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _width = width;
            _height = height;
            _stride = checked(width * 4);
            _buffer = new byte[_stride * _height];

            try {
                _hdcScreen = GetDC(IntPtr.Zero);
                if (_hdcScreen == IntPtr.Zero) throw new InvalidOperationException("GetDC failed.");

                _hdcMem = CreateCompatibleDC(_hdcScreen);
                if (_hdcMem == IntPtr.Zero) throw new InvalidOperationException("CreateCompatibleDC failed.");

                var bmi = new BITMAPINFO();
                bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
                bmi.bmiHeader.biWidth = width;
                bmi.bmiHeader.biHeight = -height; // top-down
                bmi.bmiHeader.biPlanes = 1;
                bmi.bmiHeader.biBitCount = 32;
                bmi.bmiHeader.biCompression = 0; // BI_RGB
                bmi.bmiHeader.biSizeImage = (uint)(_stride * height);

                _hBmp = CreateDIBSection(_hdcScreen, ref bmi, 0, out _bitsPtr, IntPtr.Zero, 0);
                if (_hBmp == IntPtr.Zero || _bitsPtr == IntPtr.Zero) throw new InvalidOperationException("CreateDIBSection failed.");

                _oldObj = SelectObject(_hdcMem, _hBmp);
                if (_oldObj == IntPtr.Zero) throw new InvalidOperationException("SelectObject failed.");
            } catch {
                Dispose();
                throw;
            }
        }

        public void Capture(int x, int y) {
            if (_hdcMem == IntPtr.Zero || _hdcScreen == IntPtr.Zero || _bitsPtr == IntPtr.Zero) throw new ObjectDisposedException(nameof(Session));

            if (!BitBlt(_hdcMem, 0, 0, _width, _height, _hdcScreen, x, y, SRCCOPY | CAPTUREBLT)) {
                throw new InvalidOperationException("BitBlt failed.");
            }

            Marshal.Copy(_bitsPtr, _buffer, 0, _buffer.Length);
        }

        public void Dispose() {
            // Safe to call multiple times.
            if (_hdcMem != IntPtr.Zero) {
                if (_oldObj != IntPtr.Zero) {
                    SelectObject(_hdcMem, _oldObj);
                    _oldObj = IntPtr.Zero;
                }

                DeleteDC(_hdcMem);
                _hdcMem = IntPtr.Zero;
            }

            if (_hBmp != IntPtr.Zero) {
                DeleteObject(_hBmp);
                _hBmp = IntPtr.Zero;
            }

            if (_hdcScreen != IntPtr.Zero) {
                ReleaseDC(IntPtr.Zero, _hdcScreen);
                _hdcScreen = IntPtr.Zero;
            }

            _bitsPtr = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO {
        public BITMAPINFOHEADER bmiHeader;
        public uint bmiColors; // unused for 32bpp BI_RGB
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h, IntPtr hdcSource, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
}

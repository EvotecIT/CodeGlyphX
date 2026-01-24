using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Qr;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RoundTripTests {
    [Fact]
    public void FinderDetector_FindsFinderPatterns_OnGeneratedQr() {
        var text = "finder-test";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);
        Assert.True(QrGrayImage.TryCreate(rgba, width, height, stride, PixelFormat.Rgba32, scale: 1, out var grayBase));
        var gray = grayBase.WithThreshold(128);

        var candidates = QrFinderPatternDetector.FindCandidates(gray, invert: false);
        Assert.True(candidates.Count >= 3, $"candidates={candidates.Count} t={gray.Threshold} min={gray.Min} max={gray.Max}");
    }

    [Fact]
    public void PerspectiveTransform_MapsCorners() {
        var t = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            10, 0,
            10, 10,
            0, 10,
            100, 100,
            200, 120,
            180, 220,
            90, 210);

        AssertClose(t, 0, 0, 100, 100);
        AssertClose(t, 10, 0, 200, 120);
        AssertClose(t, 10, 10, 180, 220);
        AssertClose(t, 0, 10, 90, 210);

        static void AssertClose(QrPerspectiveTransform transform, double x, double y, double ex, double ey) {
            transform.Transform(x, y, out var ox, out var oy);
            Assert.InRange(Math.Abs(ox - ex), 0, 1e-6);
            Assert.InRange(Math.Abs(oy - ey), 0, 1e-6);
        }
    }

    [Fact]
    public void Encode_RenderPng_DecodePixels_RoundTrip() {
        var text = "Round-trip test";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);
        Assert.True(QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_FinderBased_CanIgnoreNoiseOutsideQr() {
        var text = "Noise outside QR";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var padLeft = 37;
        var padTop = 19;
        var outW = width + padLeft + 11;
        var outH = height + padTop + 23;
        var outStride = outW * 4;
        var canvas = new byte[outStride * outH];

        // Fill white.
        for (var i = 0; i < canvas.Length; i += 4) {
            canvas[i + 0] = 255;
            canvas[i + 1] = 255;
            canvas[i + 2] = 255;
            canvas[i + 3] = 255;
        }

        // Copy QR image into the canvas at an offset.
        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            var dstRow = (y + padTop) * outStride + padLeft * 4;
            rgba.AsSpan(srcRow, width * 4).CopyTo(canvas.AsSpan(dstRow, width * 4));
        }

        // Add one stray black pixel outside the QR so simple bounding-box approaches fail.
        canvas[0] = 0;
        canvas[1] = 0;
        canvas[2] = 0;
        canvas[3] = 255;

        var ok = QrDecoder.TryDecode(canvas, outW, outH, outStride, PixelFormat.Rgba32, out var decoded, out var diag);
        var manual = TryManualDecodeFinderSample(canvas, outW, outH, outStride, PixelFormat.Rgba32, expectedDimension: qr.Modules.Width, out var manualDiag);

        Assert.True(ok, $"auto:   {diag}\nmanual: {manualDiag} (ok={manual})");
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_CanDecodeUiScaledBilinearQr() {
        var text = "UI-scaled QR test";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        // Simulate UI scaling that introduces non-integer module sizes and anti-aliasing.
        var dstW = Math.Max(32, width - 23);
        var dstH = Math.Max(32, height - 23);
        var scaled = ResizeBilinearRgba32(rgba, width, height, stride, dstW, dstH, out var dstStride);

        Assert.True(QrDecoder.TryDecode(scaled, dstW, dstH, dstStride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_CanDecodeDotStyleAntiAliasedQr() {
        var text = "Dot QR AA";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            ModuleShape = QrPngModuleShape.Circle,
            ModuleScale = 0.65
        });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        // Simulate UI scaling + anti-aliasing on dot-style modules.
        var dstW = Math.Max(32, width - 17);
        var dstH = Math.Max(32, height - 17);
        var scaled = ResizeBilinearRgba32(rgba, width, height, stride, dstW, dstH, out var dstStride);

        var options = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust, AggressiveSampling = true };
        Assert.True(QrDecoder.TryDecode(scaled, dstW, dstH, dstStride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_CanDecodeDotStyleBilinearScaled() {
        var text = "Dot QR scaled";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 9,
            QuietZone = 4,
            ModuleShape = QrPngModuleShape.Circle,
            ModuleScale = 0.7
        });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        // Bilinear scale down to introduce anti-aliasing.
        var dstW = (int)Math.Max(32, width * 0.78);
        var dstH = (int)Math.Max(32, height * 0.78);
        var scaled = ResizeBilinearRgba32(rgba, width, height, stride, dstW, dstH, out var dstStride);

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true
        };
        Assert.True(QrDecoder.TryDecode(scaled, dstW, dstH, dstStride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_CanDecodeLogoQr_Robust() {
        var text = "Logo QR";
        var logo = LogoBuilder.CreateCirclePng(
            size: 96,
            color: new Rgba32(24, 24, 24, 255),
            accent: new Rgba32(240, 240, 240, 255),
            out _,
            out _);
        var opts = QrPresets.Logo(logo);
        var png = QrEasy.RenderPng(text, opts);

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);
        var options = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust, AggressiveSampling = true };
        Assert.True(QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_CanDecodeFancyQr_Robust() {
        var text = "Fancy QR";
        var opts = new QrEasyOptions { Style = QrRenderStyle.Fancy, ModuleSize = 8, QuietZone = 4 };
        var png = QrEasy.RenderPng(text, opts);

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);
        var options = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust, AggressiveSampling = true };
        Assert.True(QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_CanDecodeNoQuietZone_Robust() {
        var text = "No quiet zone";
        var opts = new QrEasyOptions { QuietZone = 0, ErrorCorrectionLevel = QrErrorCorrectionLevel.H, ModuleSize = 8 };
        var png = QrEasy.RenderPng(text, opts);

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);
        var options = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust, AggressiveSampling = true };
        Assert.True(QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_CanDecodeOddModuleSizeQr() {
        var text = "Odd module size QR";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 7, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);
        Assert.True(QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    private static byte[] ResizeBilinearRgba32(byte[] src, int srcW, int srcH, int srcStride, int dstW, int dstH, out int dstStride) {
        dstStride = dstW * 4;
        var dst = new byte[dstStride * dstH];

        for (var y = 0; y < dstH; y++) {
            var sy = (y + 0.5) * srcH / dstH - 0.5;
            var y0 = (int)Math.Floor(sy);
            var y1 = y0 + 1;
            var ty = sy - y0;
            if (y0 < 0) { y0 = 0; ty = 0; }
            if (y1 >= srcH) y1 = srcH - 1;

            var row0 = y0 * srcStride;
            var row1 = y1 * srcStride;
            var outRow = y * dstStride;

            for (var x = 0; x < dstW; x++) {
                var sx = (x + 0.5) * srcW / dstW - 0.5;
                var x0 = (int)Math.Floor(sx);
                var x1 = x0 + 1;
                var tx = sx - x0;
                if (x0 < 0) { x0 = 0; tx = 0; }
                if (x1 >= srcW) x1 = srcW - 1;

                var p00 = row0 + x0 * 4;
                var p10 = row0 + x1 * 4;
                var p01 = row1 + x0 * 4;
                var p11 = row1 + x1 * 4;

                for (var c = 0; c < 4; c++) {
                    var v00 = src[p00 + c];
                    var v10 = src[p10 + c];
                    var v01 = src[p01 + c];
                    var v11 = src[p11 + c];

                    var v0 = v00 + (v10 - v00) * tx;
                    var v1 = v01 + (v11 - v01) * tx;
                    var v = v0 + (v1 - v0) * ty;

                    dst[outRow + x * 4 + c] = (byte)Math.Clamp((int)Math.Round(v), 0, 255);
                }
            }
        }

        return dst;
    }

    private static bool TryManualDecodeFinderSample(byte[] rgba, int width, int height, int stride, PixelFormat fmt, int expectedDimension, out string diagnostics) {
        diagnostics = string.Empty;

        if (!QrGrayImage.TryCreate(rgba, width, height, stride, fmt, scale: 1, out var baseImage)) {
            diagnostics = "gray-create-failed";
            return false;
        }

        // Use a stable threshold for this crisp synthetic image (avoid Otsu extremes).
        var image = baseImage.WithThreshold((byte)((baseImage.Min + baseImage.Max) / 2));

        var candidates = QrFinderPatternDetector.FindCandidates(image, invert: false);
        if (candidates.Count < 3) {
            diagnostics = $"candidates={candidates.Count} t={image.Threshold}";
            return false;
        }

        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var a0 = candidates[0];
        var a1 = candidates[1];
        var a2 = candidates[2];
        OrderAsTlTrBl(a0, a1, a2, out var tl, out var tr, out var bl);

        var dimension = expectedDimension;
        var modulesBetweenCenters = dimension - 7;
        var vxX0 = (tr.X - tl.X) / modulesBetweenCenters;
        var vxY0 = (tr.Y - tl.Y) / modulesBetweenCenters;
        var vyX0 = (bl.X - tl.X) / modulesBetweenCenters;
        var vyY0 = (bl.Y - tl.Y) / modulesBetweenCenters;

        var baselineOk = TrySample(image, tl, tr, bl, vxX0, vxY0, vyX0, vyY0, phaseX: 0, phaseY: 0, dimension, useAlignment: false, out var baselineDiag);
        if (baselineOk) {
            diagnostics = $"baseline {baselineDiag}";
            return true;
        }

        QrPixelSampling.RefineTransform(image, invert: false, tl.X, tl.Y, vxX0, vxY0, vyX0, vyY0, dimension, out var vxX, out var vxY, out var vyX, out var vyY, out var phaseX, out var phaseY);
        var refinedOk = TrySample(image, tl, tr, bl, vxX, vxY, vyX, vyY, phaseX, phaseY, dimension, useAlignment: true, out var refinedDiag);

        diagnostics = $"baseline {baselineDiag}; refined {refinedDiag} (ok={refinedOk})";
        return refinedOk;

        static void OrderAsTlTrBl(QrFinderPatternDetector.FinderPattern p0, QrFinderPatternDetector.FinderPattern p1, QrFinderPatternDetector.FinderPattern p2, out QrFinderPatternDetector.FinderPattern tl, out QrFinderPatternDetector.FinderPattern tr, out QrFinderPatternDetector.FinderPattern bl) {
            var d01 = Dist2(p0, p1);
            var d02 = Dist2(p0, p2);
            var d12 = Dist2(p1, p2);

            QrFinderPatternDetector.FinderPattern a, b, c;
            if (d01 >= d02 && d01 >= d12) {
                a = p2;
                b = p0;
                c = p1;
            } else if (d02 >= d01 && d02 >= d12) {
                a = p1;
                b = p0;
                c = p2;
            } else {
                a = p0;
                b = p1;
                c = p2;
            }

            // a is TL, b/c are TR/BL depending on orientation.
            var cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            if (cross < 0) (b, c) = (c, b);

            tl = a;
            tr = b;
            bl = c;

            static double Dist2(QrFinderPatternDetector.FinderPattern u, QrFinderPatternDetector.FinderPattern v) {
                var dx = u.X - v.X;
                var dy = u.Y - v.Y;
                return dx * dx + dy * dy;
            }
        }

        static bool TrySample(
            QrGrayImage image,
            QrFinderPatternDetector.FinderPattern tl,
            QrFinderPatternDetector.FinderPattern tr,
            QrFinderPatternDetector.FinderPattern bl,
            double vxX,
            double vxY,
            double vyX,
            double vyY,
            double phaseX,
            double phaseY,
            int dimension,
            bool useAlignment,
            out string diag) {
            var moduleSize = (Math.Sqrt(vxX * vxX + vxY * vxY) + Math.Sqrt(vyX * vyX + vyY * vyY)) / 2.0;
            var lenX = Math.Sqrt(vxX * vxX + vxY * vxY);
            var lenY = Math.Sqrt(vyX * vyX + vyY * vyY);

            var brX = tr.X - tl.X + bl.X;
            var brY = tr.Y - tl.Y + bl.Y;
            var brModule = dimension - 3.5;

            var version = (dimension - 17) / 4;
            if (useAlignment && version >= 2) {
                var align = QrTables.GetAlignmentPatternPositions(version);
                var aPos = align[align.Length - 1];
                var dxA = aPos - 3 + phaseX;
                var dyA = aPos - 3 + phaseY;
                var predX = tl.X + vxX * dxA + vyX * dyA;
                var predY = tl.Y + vxY * dxA + vyY * dyA;

                if (QrAlignmentPatternFinder.TryFind(image, invert: false, predX, predY, vxX, vxY, vyX, vyY, moduleSize, out var ax, out var ay)) {
                    brX = ax;
                    brY = ay;
                    brModule = dimension - 6.5;
                }
            }

            var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
                3.5 + phaseX, 3.5 + phaseY,
                (dimension - 3.5) + phaseX, 3.5 + phaseY,
                brModule + phaseX, brModule + phaseY,
                3.5 + phaseX, (dimension - 3.5) + phaseY,
                tl.X, tl.Y,
                tr.X, tr.Y,
                brX, brY,
                bl.X, bl.Y);

            var bm = new BitMatrix(dimension, dimension);
            for (var my = 0; my < dimension; my++) {
                for (var mx = 0; mx < dimension; mx++) {
                    var mxc = mx + 0.5 + phaseX;
                    var myc = my + 0.5 + phaseY;
                    transform.Transform(mxc, myc, out var sx, out var sy);
                    bm[mx, my] = QrPixelSampling.SampleModule9Px(image, sx, sy, invert: false);
                }
            }

            if (QrDecoder.TryDecode(bm, out _, out var moduleDiag)) {
                diag = $"ok phase({phaseX:F2},{phaseY:F2}) len({lenX:F2},{lenY:F2}) ms{moduleSize:F2} {moduleDiag}";
                return true;
            }

            diag = $"fail phase({phaseX:F2},{phaseY:F2}) len({lenX:F2},{lenY:F2}) ms{moduleSize:F2} {moduleDiag}";
            return false;
        }
    }
}

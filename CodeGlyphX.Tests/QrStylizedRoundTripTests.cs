using System;
using System.IO;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrStylizedRoundTripTests {
    [Fact]
    public void QrDecode_StylizedLargeImages_RoundTripLikePlayground() {
        const int targetSize = 2400;
        const int maxDimension = 2400;
        const int budgetMs = 12000;

        RoundTripLargeStylized("https://example.com/neon-dot", CreateNeonDotOptions(targetSize), maxDimension, budgetMs);
        RoundTripLargeStylized("https://example.com/candy-checker", CreateCandyCheckerOptions(targetSize), maxDimension, budgetMs);
    }

    [Fact]
    public void QrDecode_NewArtPresets_RoundTrip() {
        const int targetSize = 1600;
        const int maxDimension = 1600;
        const int budgetMs = 9000;

        RoundTripLargeStylized("https://example.com/connected-melt", CreateConnectedMeltOptions(targetSize), maxDimension, budgetMs);
        RoundTripLargeStylized("https://example.com/neon-glow", CreateNeonGlowOptions(targetSize), maxDimension, budgetMs);
    }

    private static void RoundTripLargeStylized(string payload, QrEasyOptions options, int maxDimension, int budgetMs) {
        var png = RenderPng(payload, options);
        Assert.True(ImageReader.TryDecodeRgba32(png, out var rgba, out var width, out var height));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(budgetMs));
        var decodeOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            AggressiveSampling = true,
            StylizedSampling = true,
            BudgetMilliseconds = budgetMs,
            MaxDimension = maxDimension,
            AutoCrop = true
        };

        Assert.True(
            QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decodedAll, out var info, decodeOptions, cts.Token),
            info.ToString());
        Assert.Contains(decodedAll, decoded => decoded.Text == payload);
    }

    private static byte[] RenderPng(string payload, QrEasyOptions options) {
        using var stream = new MemoryStream();
        QR.SavePng(payload, stream, options);
        return stream.ToArray();
    }

    private static QrEasyOptions CreateNeonDotOptions(int targetSizePx) {
        return new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            TargetSizePx = targetSizePx,
            TargetSizeIncludesQuietZone = true,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = R(0, 255, 213),
            Background = R(255, 255, 255),
            BackgroundSupersample = 2,
            ModuleShape = QrPngModuleShape.Dot,
            ModuleScale = 0.9,
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Random,
                Seed = 14001,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] { R(0, 255, 213), R(255, 59, 255), R(255, 214, 0) }
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Target,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = R(0, 255, 213),
                InnerColor = R(255, 59, 255),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 24,
                CornerRadiusPx = 26,
                Background = R(18, 18, 28),
                BackgroundGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.DiagonalDown,
                    StartColor = R(18, 18, 28),
                    EndColor = R(48, 23, 72)
                }
            }
        };
    }

    private static QrEasyOptions CreateCandyCheckerOptions(int targetSizePx) {
        return new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            TargetSizePx = targetSizePx,
            TargetSizeIncludesQuietZone = true,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = R(255, 107, 107),
            Background = R(255, 255, 255),
            BackgroundSupersample = 2,
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.9,
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Checker,
                Seed = 0,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] { R(255, 107, 107), R(255, 217, 61) }
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Badge,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = R(255, 107, 107),
                InnerColor = R(255, 217, 61),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 24,
                CornerRadiusPx = 26,
                Background = R(255, 248, 240),
                Pattern = new QrPngBackgroundPatternOptions {
                    Type = QrPngBackgroundPatternType.Dots,
                    Color = R(255, 107, 107, 28),
                    SizePx = 14,
                    ThicknessPx = 1,
                    SnapToModuleSize = true,
                    ModuleStep = 2
                }
            }
        };
    }

    private static QrEasyOptions CreateConnectedMeltOptions(int targetSizePx) {
        return new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            TargetSizePx = targetSizePx,
            TargetSizeIncludesQuietZone = true,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = R(88, 120, 255),
            Background = R(255, 255, 255),
            BackgroundSupersample = 2,
            ModuleShape = QrPngModuleShape.ConnectedRounded,
            ModuleScale = 0.9,
            ModuleScaleMap = new QrPngModuleScaleMapOptions {
                Mode = QrPngModuleScaleMode.Radial,
                MinScale = 0.86,
                MaxScale = 1.0,
                RingSize = 2,
            },
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Cycle,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] { R(88, 120, 255), R(120, 96, 255), R(88, 210, 255) }
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Target,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = R(88, 120, 255),
                InnerColor = R(88, 210, 255),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4,
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 24,
                CornerRadiusPx = 26,
                BackgroundGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.DiagonalDown,
                    StartColor = R(14, 18, 42),
                    EndColor = R(28, 20, 76),
                }
            }
        };
    }

    private static QrEasyOptions CreateNeonGlowOptions(int targetSizePx) {
        return new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            TargetSizePx = targetSizePx,
            TargetSizeIncludesQuietZone = true,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = R(0, 255, 240),
            Background = R(255, 255, 255),
            BackgroundSupersample = 2,
            ModuleShape = QrPngModuleShape.Dot,
            ModuleScale = 0.9,
            ModuleScaleMap = new QrPngModuleScaleMapOptions {
                Mode = QrPngModuleScaleMode.Radial,
                MinScale = 0.82,
                MaxScale = 1.0,
                RingSize = 2,
            },
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Cycle,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] { R(0, 255, 240), R(0, 170, 255), R(255, 92, 255) }
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Glow,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = R(0, 255, 240),
                InnerColor = R(255, 92, 255),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4,
                GlowRadiusPx = 30,
                GlowAlpha = 130,
                GlowColor = R(0, 200, 255, 200),
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 24,
                CornerRadiusPx = 26,
                BackgroundGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.DiagonalDown,
                    StartColor = R(8, 10, 28),
                    EndColor = R(28, 18, 64),
                }
            }
        };
    }

    private static Rgba32 R(byte r, byte g, byte b, byte a = 255) => new(r, g, b, a);

    private static byte[] ResizeNearest(byte[] rgba, int width, int height, int maxDimension, out int newWidth, out int newHeight) {
        if (maxDimension <= 0 || (width <= maxDimension && height <= maxDimension)) {
            newWidth = width;
            newHeight = height;
            return rgba;
        }

        var scale = Math.Min(1.0, maxDimension / (double)Math.Max(width, height));
        newWidth = Math.Max(1, (int)Math.Round(width * scale));
        newHeight = Math.Max(1, (int)Math.Round(height * scale));

        var dst = new byte[newWidth * newHeight * 4];
        var xRatio = width / (double)newWidth;
        var yRatio = height / (double)newHeight;
        var srcStride = width * 4;

        for (var y = 0; y < newHeight; y++) {
            var srcY = Math.Min(height - 1, (int)(y * yRatio));
            var srcRow = srcY * srcStride;
            var dstRow = y * newWidth * 4;
            for (var x = 0; x < newWidth; x++) {
                var srcX = Math.Min(width - 1, (int)(x * xRatio));
                var srcIndex = srcRow + srcX * 4;
                var dstIndex = dstRow + x * 4;
                dst[dstIndex + 0] = rgba[srcIndex + 0];
                dst[dstIndex + 1] = rgba[srcIndex + 1];
                dst[dstIndex + 2] = rgba[srcIndex + 2];
                dst[dstIndex + 3] = rgba[srcIndex + 3];
            }
        }

        return dst;
    }
}

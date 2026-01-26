using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class Playground {
    internal string GetBarcodeContentLabel()
    {
        return SelectedBarcodeType switch
        {
            "EAN" => "Digits (8 or 13)",
            "UPCA" => "Digits (12)",
            "UPCE" => "Digits (6-8)",
            "ITF14" => "Digits (14)",
            "MSI" => "Digits",
            "Code11" => "Digits and '-'",
            "Plessey" => "Hex digits (0-9, A-F)",
            _ => "Content"
        };
    }

    internal string GetBarcodeContentPlaceholder()
    {
        return SelectedBarcodeType switch
        {
            "Code39" => "HELLO-123",
            "Code93" => "HELLO-123",
            "Code11" => "123-45",
            "Codabar" => "A123456B",
            "MSI" => "1234567",
            "Plessey" => "A1B2C3",
            "EAN" => "5901234123457",
            "UPCA" => "036000291452",
            "UPCE" => "042526",
            "ITF14" => "10012345678902",
            "GS1128" => "(01)09506000134352(10)ABC123",
            _ => "PRODUCT-12345"
        };
    }

    internal void GenerateCode()
    {
        if (SelectedMode != "Generate")
        {
            return;
        }

        ResetOutputs();

        try
        {
            byte[] pngBytes = Array.Empty<byte>();
            string? svg = null;

            if (SelectedCategory == "QR")
            {
                if (string.IsNullOrWhiteSpace(Content))
                    return;

                var qrEcc = ErrorCorrection switch
                {
                    "L" => QrErrorCorrectionLevel.L,
                    "Q" => QrErrorCorrectionLevel.Q,
                    "H" => QrErrorCorrectionLevel.H,
                    _ => QrErrorCorrectionLevel.M
                };

                var options = new QrEasyOptions
                {
                    ErrorCorrectionLevel = qrEcc,
                    Foreground = ParseColor(ForegroundColor),
                    Background = ParseColor(BackgroundColor),
                    ModuleShape = ParseModuleShape(ModuleShape),
                    ModuleScale = ModuleScale,
                    ModuleCornerRadiusPx = ModuleShape == "Rounded" ? CornerRadius : 0,
                    TargetSizePx = TargetSizePx,
                    TargetSizeIncludesQuietZone = TargetSizeIncludesQuietZone
                };

                if (UseForegroundGradient)
                {
                    options.ForegroundGradient = new QrPngGradientOptions
                    {
                        Type = ParseGradientType(ForegroundGradientType),
                        StartColor = ParseColor(ForegroundGradientStart),
                        EndColor = ParseColor(ForegroundGradientEnd)
                    };
                }

                if (UseBackgroundGradient)
                {
                    options.BackgroundGradient = new QrPngGradientOptions
                    {
                        Type = ParseGradientType(BackgroundGradientType),
                        StartColor = ParseColor(BackgroundGradientStart),
                        EndColor = ParseColor(BackgroundGradientEnd)
                    };
                }

                options.BackgroundSupersample = BackgroundSupersample;

                if (EnableQrBackgroundPattern)
                {
                    options.BackgroundPattern = new QrPngBackgroundPatternOptions
                    {
                        Type = ParsePatternType(QrBackgroundPatternType),
                        Color = ApplyAlpha(ParseColor(QrBackgroundPatternColor), QrBackgroundPatternAlpha),
                        SizePx = QrBackgroundPatternSizePx,
                        ThicknessPx = QrBackgroundPatternThicknessPx,
                        SnapToModuleSize = QrBackgroundPatternSnapToModules,
                        ModuleStep = QrBackgroundPatternModuleStep
                    };
                }

                if (EnablePalette)
                {
                    options.ForegroundPalette = new QrPngPaletteOptions
                    {
                        Mode = ParsePaletteMode(PaletteMode),
                        Seed = PaletteSeed,
                        RingSize = PaletteRingSize,
                        ApplyToEyes = false,
                        Colors = new[]
                        {
                            ParseColor(PaletteColor1),
                            ParseColor(PaletteColor2),
                            ParseColor(PaletteColor3)
                        }
                    };
                }

                if (EnableZonePalettes)
                {
                    options.ForegroundPaletteZones = new QrPngPaletteZoneOptions
                    {
                        CenterPalette = new QrPngPaletteOptions
                        {
                            Mode = ParsePaletteMode(CenterPaletteMode),
                            Seed = CenterPaletteSeed,
                            RingSize = 2,
                            ApplyToEyes = false,
                            Colors = new[]
                            {
                                ParseColor(CenterPaletteColor1),
                                ParseColor(CenterPaletteColor2),
                                ParseColor(CenterPaletteColor3)
                            }
                        },
                        CenterSize = CenterZoneSize,
                        CornerPalette = new QrPngPaletteOptions
                        {
                            Mode = ParsePaletteMode(CornerPaletteMode),
                            Seed = CornerPaletteSeed,
                            RingSize = 2,
                            ApplyToEyes = false,
                            Colors = new[]
                            {
                                ParseColor(CornerPaletteColor1),
                                ParseColor(CornerPaletteColor2)
                            }
                        },
                        CornerSize = CornerZoneSize
                    };
                }

                if (EnableScaleMap)
                {
                    options.ModuleScaleMap = new QrPngModuleScaleMapOptions
                    {
                        Mode = ParseScaleMapMode(ScaleMapMode),
                        MinScale = ScaleMapMin,
                        MaxScale = ScaleMapMax,
                        RingSize = ScaleMapRingSize,
                        Seed = ScaleMapSeed,
                        ApplyToEyes = ScaleMapApplyToEyes
                    };
                }

                if (CustomEyes)
                {
                    options.Eyes = new QrPngEyeOptions
                    {
                        UseFrame = true,
                        FrameStyle = ParseEyeFrameStyle(EyeFrameStyle),
                        OuterShape = ParseModuleShape(EyeOuterShape),
                        InnerShape = ParseModuleShape(EyeInnerShape),
                        OuterColor = ParseColor(EyeOuterColor),
                        InnerColor = ParseColor(EyeInnerColor)
                    };
                }

                if (EnableCanvas)
                {
                    options.Canvas = new QrPngCanvasOptions
                    {
                        PaddingPx = CanvasPaddingPx,
                        CornerRadiusPx = CanvasCornerRadiusPx,
                        Background = ParseColor(CanvasBackgroundColor),
                        BackgroundGradient = CanvasUseGradient
                            ? new QrPngGradientOptions
                            {
                                Type = ParseGradientType(CanvasGradientType),
                                StartColor = ParseColor(CanvasGradientStart),
                                EndColor = ParseColor(CanvasGradientEnd)
                            }
                            : null,
                        Pattern = CanvasUsePattern
                            ? new QrPngBackgroundPatternOptions
                            {
                                Type = ParsePatternType(CanvasPatternType),
                                Color = ApplyAlpha(ParseColor(CanvasPatternColor), CanvasPatternAlpha),
                                SizePx = CanvasPatternSizePx,
                                ThicknessPx = CanvasPatternThicknessPx
                            }
                            : null,
                        BorderPx = CanvasBorderPx,
                        BorderColor = ParseColor(CanvasBorderColor),
                        ShadowOffsetX = CanvasShadowOffsetX,
                        ShadowOffsetY = CanvasShadowOffsetY,
                        ShadowColor = ApplyAlpha(ParseColor(CanvasShadowColor), CanvasShadowAlpha)
                    };
                }

                if (EnableDebugOverlay)
                {
                    options.Debug = new QrPngDebugOptions
                    {
                        ShowQuietZone = DebugShowQuietZone,
                        ShowQrBounds = DebugShowQrBounds,
                        ShowEyeBounds = DebugShowEyeBounds,
                        ShowLogoBounds = DebugShowLogoBounds,
                        StrokePx = DebugStrokePx
                    };
                }

                SafetyReport = QrEasy.EvaluateSafety(Content, options);
                pngBytes = QrEasy.RenderPng(Content, options);
                svg = QrEasy.RenderSvg(Content, options);
            }
            else if (SelectedCategory == "SpecialQR")
            {
                var payloadData = GetSpecialPayloadData();
                if (payloadData == null || string.IsNullOrWhiteSpace(payloadData.Text))
                    return;

                pngBytes = QrEasy.RenderPng(payloadData);
                svg = QrEasy.RenderSvg(payloadData);
            }
            else if (SelectedCategory == "Barcode")
            {
                if (string.IsNullOrWhiteSpace(BarcodeContent))
                    return;

                var barcodeType = SelectedBarcodeType switch
                {
                    "Code128" => BarcodeType.Code128,
                    "GS1128" => BarcodeType.GS1_128,
                    "Code39" => BarcodeType.Code39,
                    "Code93" => BarcodeType.Code93,
                    "Code11" => BarcodeType.Code11,
                    "Codabar" => BarcodeType.Codabar,
                    "MSI" => BarcodeType.MSI,
                    "Plessey" => BarcodeType.Plessey,
                    "EAN" => BarcodeType.EAN,
                    "UPCA" => BarcodeType.UPCA,
                    "UPCE" => BarcodeType.UPCE,
                    "ITF14" => BarcodeType.ITF14,
                    _ => BarcodeType.Code128
                };

                string content = BarcodeContent;

                // Special handling for different barcode types
                if (SelectedBarcodeType == "Code39")
                {
                    content = content.ToUpperInvariant();
                    if (!IsValidCode39Content(content))
                    {
                        ErrorMessage = "Code 39 supports uppercase A-Z, digits 0-9, and -. $/+% space";
                        return;
                    }
                }
                else if (SelectedBarcodeType == "Code93")
                {
                    content = content.ToUpperInvariant();
                }
                else if (SelectedBarcodeType == "Code11")
                {
                    content = content.ToUpperInvariant();
                    if (!IsValidCode11Content(content))
                    {
                        ErrorMessage = "Code 11 supports digits and '-' only.";
                        return;
                    }
                }
                else if (SelectedBarcodeType == "Plessey")
                {
                    content = content.ToUpperInvariant();
                    if (!IsValidHexContent(content))
                    {
                        ErrorMessage = "Plessey supports hex digits only (0-9, A-F).";
                        return;
                    }
                }
                else if (SelectedBarcodeType == "EAN")
                {
                    content = NormalizeDigits(content);
                    if (content.Length <= 8) content = content.PadLeft(8, '0');
                    else if (content.Length < 13) content = content.PadLeft(13, '0');
                }
                else if (SelectedBarcodeType == "UPCA")
                {
                    content = NormalizeDigits(content).PadLeft(12, '0');
                }
                else if (SelectedBarcodeType == "UPCE")
                {
                    content = NormalizeDigits(content);
                    if (content.Length < 6) content = content.PadLeft(6, '0');
                }
                else if (SelectedBarcodeType == "ITF14")
                {
                    content = NormalizeDigits(content).PadLeft(14, '0');
                }
                else if (SelectedBarcodeType == "Codabar")
                {
                    content = content.ToUpperInvariant();
                }
                else if (SelectedBarcodeType == "MSI")
                {
                    content = NormalizeDigits(content);
                }

                pngBytes = BarcodeEasy.RenderPng(barcodeType, content);
                svg = BarcodeEasy.RenderSvg(barcodeType, content);
            }
            else if (SelectedCategory == "Matrix")
            {
                if (string.IsNullOrWhiteSpace(MatrixContent))
                    return;

                switch (SelectedMatrixType)
                {
                    case "Pdf417":
                        var pdf417Options = new Pdf417EncodeOptions
                        {
                            ErrorCorrectionLevel = Pdf417EccLevel,
                            Compact = Pdf417Compact
                        };
                        pngBytes = Pdf417Code.Png(MatrixContent, pdf417Options);
                        svg = Pdf417Code.Svg(MatrixContent, pdf417Options);
                        break;
                    case "Aztec":
                        var aztecOptions = new AztecEncodeOptions
                        {
                            ErrorCorrectionPercent = AztecAutoEcc ? null : AztecEccPercent,
                            Layers = AztecLayers > 0 ? AztecLayers : null
                        };
                        pngBytes = AztecCode.Png(MatrixContent, aztecOptions);
                        svg = AztecCode.Svg(MatrixContent, aztecOptions);
                        break;
                    default:
                        var dmMode = ParseDataMatrixMode(SelectedDataMatrixMode);
                        pngBytes = DataMatrixCode.Png(MatrixContent, dmMode);
                        svg = DataMatrixCode.Svg(MatrixContent, dmMode);
                        break;
                }
            }

            ImageDataUri = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
            if (svg != null)
            {
                SvgDataUri = $"data:image/svg+xml;base64,{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg))}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        _exampleKey++;
        StateHasChanged();
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PdfDecodeTests {
    [Fact]
    public void Decode_Pdf_Flate_Image() {
        var rgb = new byte[] { 255, 0, 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, rgb, "/DeviceRGB", bitsPerComponent: 8, "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(4, rgba.Length);
        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[1]);
        Assert.Equal(0, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    [Fact]
    public void Decode_Pdf_Flate_16bit_Image() {
        var rgb16 = new byte[] { 0x12, 0x34, 0xAB, 0xCD, 0xFF, 0xFF };
        var pdf = BuildPdfWithFlateImage(1, 1, rgb16, "/DeviceRGB", bitsPerComponent: 16, "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x12, 0xAB, 0xFF, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_16bit_PngPredictor() {
        var predicted = new byte[] { 0x00, 0x12, 0x34, 0xAB, 0xCD, 0xFF, 0xFF };
        var pdf = BuildPdfWithFlateImage(1, 1, predicted, "/DeviceRGB", bitsPerComponent: 16, "/Filter /FlateDecode /DecodeParms << /Predictor 12 /Colors 3 /Columns 1 >> ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x12, 0xAB, 0xFF, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_16bit_TiffPredictor() {
        var predicted = new byte[] { 0x01, 0x01, 0x01, 0x01 };
        var pdf = BuildPdfWithFlateImage(2, 1, predicted, "/DeviceGray", bitsPerComponent: 16, "/Filter /FlateDecode /Predictor 2 /Colors 1 ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x01, 0x01, 0x01, 255, 0x02, 0x02, 0x02, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_Filter_Array() {
        var rgb = new byte[] { 0, 255, 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, rgb, "/DeviceRGB", bitsPerComponent: 8, "/Filter [/FlateDecode] ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0, 255, 0, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_Cmyk_Image() {
        var cmyk = new byte[] { 0, 255, 0, 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, cmyk, "/DeviceCMYK", bitsPerComponent: 8, "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 255, 0, 255, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_CalRgb_Image() {
        var rgb = new byte[] { 0x10, 0x20, 0x30 };
        var pdf = BuildPdfWithFlateImage(1, 1, rgb, "[/CalRGB << /WhitePoint [1 1 1] >>]", bitsPerComponent: 8, "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_IccBased_Alternate_Image() {
        var rgb = new byte[] { 0x2A, 0x3B, 0x4C };
        var pdf = BuildPdfWithFlateImage(1, 1, rgb, "[/ICCBased << /N 3 /Alternate /DeviceRGB >>]", bitsPerComponent: 8, "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x2A, 0x3B, 0x4C, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_IccBased_Indirect_Image() {
        var rgb = new byte[] { 0x11, 0x22, 0x33 };
        var pdf = BuildPdfWithFlateImageAndIccObject(1, 1, rgb);

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Separation_Alternate_Image() {
        var gray = new byte[] { 0x5A };
        var pdf = BuildPdfWithFlateImage(1, 1, gray, "[/Separation /Spot /DeviceGray << /FunctionType 2 >>]", bitsPerComponent: 8, "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x5A, 0x5A, 0x5A, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_SoftMask_Applies_Alpha() {
        var rgb = new byte[] { 0x10, 0x20, 0x30 };
        var mask = new byte[] { 0x80 };
        var pdf = BuildPdfWithFlateImageAndSoftMask(1, 1, rgb, mask);

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 0x80 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_DecodeArray_Inverts_Gray() {
        var gray = new byte[] { 0 };
        var pdf = BuildPdfWithFlateImage(1, 1, gray, "/DeviceGray", bitsPerComponent: 8, "/Filter /FlateDecode /Decode [1 0] ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Ascii85_Flate_Image() {
        var rgb = new byte[] { 0, 0, 255 };
        var pdf = BuildPdfWithAscii85FlateImage(1, 1, rgb);

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_AsciiHex_Image() {
        var rgb = new byte[] { 0x10, 0x20, 0x30 };
        var pdf = BuildPdfWithAsciiHexImage(1, 1, rgb);

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Lzw_Image() {
        var rgb = new byte[] { 0x12, 0x34, 0x56 };
        var pdf = BuildPdfWithLzwImage(1, 1, rgb);

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Lzw_EarlyChange_Zero() {
        var rgb = new byte[] { 0xAA, 0xBB, 0xCC };
        var pdf = BuildPdfWithLzwImage(1, 1, rgb, earlyChange: 0, dictExtras: "/DecodeParms << /EarlyChange 0 >> ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_Predictor_From_DecodeParms() {
        var predicted = new byte[] { 0, 0x11, 0x22, 0x33 };
        var pdf = BuildPdfWithFlateImage(1, 1, predicted, "/DeviceRGB", bitsPerComponent: 8, "/Filter /FlateDecode /DecodeParms << /Predictor 12 /Colors 3 /Columns 1 >> ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_Predictor_From_DecodeParms_Array() {
        var predicted = new byte[] { 0, 0x9A, 0xBC, 0xDE };
        var pdf = BuildPdfWithAscii85FlateImageWithDict(1, 1, predicted, "/Filter [/ASCII85Decode /FlateDecode] /DecodeParms [null << /Predictor 12 /Colors 3 /Columns 1 >>] ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x9A, 0xBC, 0xDE, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Inline_Image_Raw() {
        var gray = new byte[] { 0x7F };
        var pdf = BuildPdfWithInlineImageRaw(1, 1, gray, "/CS /DeviceGray /BPC 8");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x7F, 0x7F, 0x7F, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Inline_Image_CalGray_Raw() {
        var gray = new byte[] { 0x44 };
        var pdf = BuildPdfWithInlineImageRaw(1, 1, gray, "/CS [/CalGray << /WhitePoint [1 1 1] >>] /BPC 8");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x44, 0x44, 0x44, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Inline_Image_IccBased_NOnly() {
        var gray = new byte[] { 0x21 };
        var pdf = BuildPdfWithInlineImageRaw(1, 1, gray, "/CS [/ICCBased << /N 1 >>] /BPC 8");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x21, 0x21, 0x21, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Inline_Image_DeviceN_Alternate() {
        var rgb = new byte[] { 0x10, 0x20, 0x30 };
        var pdf = BuildPdfWithInlineImageRaw(1, 1, rgb, "/CS [/DeviceN [/Cyan /Magenta /Yellow] /DeviceRGB << /FunctionType 2 >>] /BPC 8");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Inline_Image_Ascii85_Flate() {
        var rgb = new byte[] { 0x22, 0x33, 0x44 };
        var pdf = BuildPdfWithInlineImageAscii85Flate(1, 1, rgb, "/CS /DeviceRGB /BPC 8 /F [/ASCII85Decode /FlateDecode]");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x22, 0x33, 0x44, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Inline_Image_DecodeParms_Predictor() {
        var predicted = new byte[] { 0, 0x66, 0x77, 0x88 };
        var pdf = BuildPdfWithInlineImageFlate(1, 1, predicted, "/CS /DeviceRGB /BPC 8 /F /FlateDecode /DP << /Predictor 12 /Colors 3 /Columns 1 >>");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0x66, 0x77, 0x88, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Flate_Indexed_Image() {
        var indexData = new byte[] { 0x80 };
        var lookup = "<00FF00FF0000>";
        var pdf = BuildPdfWithFlateImage(1, 1, indexData, $"[/Indexed /DeviceRGB 1 {lookup}]", bitsPerComponent: 1, "/Filter /FlateDecode ");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, rgba);
    }

    [Fact]
    public void Decode_Pdf_Inline_Image_Indexed_Raw() {
        var indexData = new byte[] { 0x00 };
        var lookup = "<0000FF00FF00>";
        var pdf = BuildPdfWithInlineImageRaw(1, 1, indexData, $"/CS [/Indexed /DeviceRGB 1 {lookup}] /BPC 1");

        var rgba = ImageReader.DecodeRgba32(pdf, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, rgba);
    }

    private static byte[] BuildPdfWithFlateImage(int width, int height, byte[] payload, string colorSpace, int bitsPerComponent, string filterClause) {
        byte[] compressed;
        using (var ms = new MemoryStream()) {
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
                deflate.Write(payload, 0, payload.Length);
            }
            compressed = ms.ToArray();
        }

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace ").Append(colorSpace).Append(' ');
        sb.Append("/BitsPerComponent ").Append(bitsPerComponent).Append(' ');
        sb.Append(filterClause);
        sb.Append("/Length ").Append(compressed.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + compressed.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(compressed, 0, output, header.Length, compressed.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + compressed.Length, footer.Length);
        return output;
    }

    private static byte[] BuildPdfWithAscii85FlateImage(int width, int height, byte[] rgb) {
        var compressed = Deflate(rgb);
        var encoded = Ascii85Encode(compressed);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace /DeviceRGB ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append("/Filter [/ASCII85Decode /FlateDecode] ");
        sb.Append("/Length ").Append(encoded.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + encoded.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(encoded, 0, output, header.Length, encoded.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + encoded.Length, footer.Length);
        return output;
    }

    private static byte[] BuildPdfWithAscii85FlateImageWithDict(int width, int height, byte[] payload, string dictExtras) {
        var compressed = Deflate(payload);
        var encoded = Ascii85Encode(compressed);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace /DeviceRGB ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append(dictExtras);
        sb.Append("/Length ").Append(encoded.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + encoded.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(encoded, 0, output, header.Length, encoded.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + encoded.Length, footer.Length);
        return output;
    }

    private static byte[] BuildPdfWithFlateImageAndIccObject(int width, int height, byte[] payload) {
        byte[] compressed;
        using (var ms = new MemoryStream()) {
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
                deflate.Write(payload, 0, payload.Length);
            }
            compressed = ms.ToArray();
        }

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace [/ICCBased 2 0 R] ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append("/Filter /FlateDecode ");
        sb.Append("/Length ").Append(compressed.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
        var obj2 = Encoding.ASCII.GetBytes("2 0 obj\n<< /N 3 /Alternate /DeviceRGB >>\nendobj\n%%EOF\n");

        var output = new byte[header.Length + compressed.Length + footer.Length + obj2.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(compressed, 0, output, header.Length, compressed.Length);
        var offset = header.Length + compressed.Length;
        Buffer.BlockCopy(footer, 0, output, offset, footer.Length);
        offset += footer.Length;
        Buffer.BlockCopy(obj2, 0, output, offset, obj2.Length);
        return output;
    }

    private static byte[] BuildPdfWithFlateImageAndSoftMask(int width, int height, byte[] rgb, byte[] mask) {
        byte[] rgbCompressed;
        using (var ms = new MemoryStream()) {
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
                deflate.Write(rgb, 0, rgb.Length);
            }
            rgbCompressed = ms.ToArray();
        }

        byte[] maskCompressed;
        using (var ms = new MemoryStream()) {
            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
                deflate.Write(mask, 0, mask.Length);
            }
            maskCompressed = ms.ToArray();
        }

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace /DeviceRGB ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append("/Filter /FlateDecode ");
        sb.Append("/SMask 2 0 R ");
        sb.Append("/Length ").Append(rgbCompressed.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
        var maskHeader = Encoding.ASCII.GetBytes("2 0 obj\n<< /Type /XObject /Subtype /Image /Width " + width + " /Height " + height +
            " /ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /FlateDecode /Length " + maskCompressed.Length + " >>\nstream\n");
        var maskFooter = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + rgbCompressed.Length + footer.Length + maskHeader.Length + maskCompressed.Length + maskFooter.Length];
        var offset = 0;
        Buffer.BlockCopy(header, 0, output, offset, header.Length);
        offset += header.Length;
        Buffer.BlockCopy(rgbCompressed, 0, output, offset, rgbCompressed.Length);
        offset += rgbCompressed.Length;
        Buffer.BlockCopy(footer, 0, output, offset, footer.Length);
        offset += footer.Length;
        Buffer.BlockCopy(maskHeader, 0, output, offset, maskHeader.Length);
        offset += maskHeader.Length;
        Buffer.BlockCopy(maskCompressed, 0, output, offset, maskCompressed.Length);
        offset += maskCompressed.Length;
        Buffer.BlockCopy(maskFooter, 0, output, offset, maskFooter.Length);
        return output;
    }

    private static byte[] BuildPdfWithAsciiHexImage(int width, int height, byte[] rgb) {
        var encoded = AsciiHexEncode(rgb);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace /DeviceRGB ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append("/Filter /ASCIIHexDecode ");
        sb.Append("/Length ").Append(encoded.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + encoded.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(encoded, 0, output, header.Length, encoded.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + encoded.Length, footer.Length);
        return output;
    }

    private static byte[] BuildPdfWithLzwImage(int width, int height, byte[] rgb) {
        return BuildPdfWithLzwImage(width, height, rgb, earlyChange: 1, dictExtras: string.Empty);
    }

    private static byte[] BuildPdfWithLzwImage(int width, int height, byte[] rgb, int earlyChange, string dictExtras) {
        var encoded = LzwEncode(rgb, earlyChange);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n");
        sb.Append("<< /Type /XObject /Subtype /Image ");
        sb.Append("/Width ").Append(width).Append(' ');
        sb.Append("/Height ").Append(height).Append(' ');
        sb.Append("/ColorSpace /DeviceRGB ");
        sb.Append("/BitsPerComponent 8 ");
        sb.Append("/Filter /LZWDecode ");
        sb.Append(dictExtras);
        sb.Append("/Length ").Append(encoded.Length).Append(" >>\n");
        sb.Append("stream\n");

        var header = Encoding.ASCII.GetBytes(sb.ToString());
        var footer = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");

        var output = new byte[header.Length + encoded.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(encoded, 0, output, header.Length, encoded.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + encoded.Length, footer.Length);
        return output;
    }

    private static byte[] BuildPdfWithInlineImageRaw(int width, int height, byte[] data, string dictExtras) {
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\nBI /W " + width + " /H " + height + " " + dictExtras + " ID\n");
        var footer = Encoding.ASCII.GetBytes("\nEI\n%%EOF\n");
        var output = new byte[header.Length + data.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(data, 0, output, header.Length, data.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + data.Length, footer.Length);
        return output;
    }

    private static byte[] BuildPdfWithInlineImageAscii85Flate(int width, int height, byte[] rgb, string dictExtras) {
        var compressed = Deflate(rgb);
        var encoded = Ascii85Encode(compressed);
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\nBI /W " + width + " /H " + height + " " + dictExtras + " ID\n");
        var footer = Encoding.ASCII.GetBytes("\nEI\n%%EOF\n");
        var output = new byte[header.Length + encoded.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(encoded, 0, output, header.Length, encoded.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + encoded.Length, footer.Length);
        return output;
    }

    private static byte[] BuildPdfWithInlineImageFlate(int width, int height, byte[] payload, string dictExtras) {
        var compressed = Deflate(payload);
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\nBI /W " + width + " /H " + height + " " + dictExtras + " ID\n");
        var footer = Encoding.ASCII.GetBytes("\nEI\n%%EOF\n");
        var output = new byte[header.Length + compressed.Length + footer.Length];
        Buffer.BlockCopy(header, 0, output, 0, header.Length);
        Buffer.BlockCopy(compressed, 0, output, header.Length, compressed.Length);
        Buffer.BlockCopy(footer, 0, output, header.Length + compressed.Length, footer.Length);
        return output;
    }

    private static byte[] Deflate(byte[] data) {
        using var ms = new MemoryStream();
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true)) {
            deflate.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    private static byte[] Ascii85Encode(byte[] data) {
        using var ms = new MemoryStream();
        var index = 0;
        while (index < data.Length) {
            var remaining = Math.Min(4, data.Length - index);
            uint tuple = 0;
            for (var i = 0; i < 4; i++) {
                tuple <<= 8;
                if (i < remaining) tuple |= data[index + i];
            }

            if (remaining == 4 && tuple == 0) {
                ms.WriteByte((byte)'z');
                index += 4;
                continue;
            }

            var encoded = new byte[5];
            for (var i = 4; i >= 0; i--) {
                encoded[i] = (byte)((tuple % 85) + (byte)'!');
                tuple /= 85;
            }

            var count = remaining + 1;
            ms.Write(encoded, 0, count);
            index += remaining;
        }

        ms.WriteByte((byte)'~');
        ms.WriteByte((byte)'>');
        return ms.ToArray();
    }

    private static byte[] AsciiHexEncode(byte[] data) {
        var sb = new StringBuilder(data.Length * 2 + 1);
        for (var i = 0; i < data.Length; i++) {
            sb.Append(data[i].ToString("X2"));
        }
        sb.Append('>');
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static byte[] LzwEncode(ReadOnlySpan<byte> data) {
        return LzwEncode(data, earlyChange: 1);
    }

    private static byte[] LzwEncode(ReadOnlySpan<byte> data, int earlyChange) {
        const int clear = 256;
        const int eoi = 257;
        const int maxCode = 4096;

        var writer = new LzwBitWriter(data.Length);
        var dict = new System.Collections.Generic.Dictionary<int, int>(4096);
        var codeSize = 9;
        var nextCode = 258;

        writer.Write(clear, codeSize);
        if (data.IsEmpty) {
            writer.Write(eoi, codeSize);
            return writer.ToArray();
        }

        var prefix = (int)data[0];
        for (var i = 1; i < data.Length; i++) {
            var b = data[i];
            var key = (prefix << 8) | b;
            if (dict.TryGetValue(key, out var code)) {
                prefix = code;
                continue;
            }

            writer.Write(prefix, codeSize);
            if (nextCode < maxCode) {
                dict[key] = nextCode++;
                if (nextCode == (1 << codeSize) - earlyChange && codeSize < 12) {
                    codeSize++;
                }
            } else {
                writer.Write(clear, codeSize);
                dict.Clear();
                codeSize = 9;
                nextCode = 258;
            }
            prefix = b;
        }

        writer.Write(prefix, codeSize);
        writer.Write(eoi, codeSize);
        return writer.ToArray();
    }

    private sealed class LzwBitWriter {
        private readonly System.Collections.Generic.List<byte> _output;
        private uint _buffer;
        private int _bitCount;

        public LzwBitWriter(int dataLength) {
            _output = new System.Collections.Generic.List<byte>(Math.Max(16, dataLength));
        }

        public void Write(int code, int codeSize) {
            _buffer = (_buffer << codeSize) | (uint)code;
            _bitCount += codeSize;
            while (_bitCount >= 8) {
                var shift = _bitCount - 8;
                _output.Add((byte)(_buffer >> shift));
                _bitCount -= 8;
                if (_bitCount == 0) {
                    _buffer = 0;
                } else {
                    _buffer &= (1u << _bitCount) - 1u;
                }
            }
        }

        public byte[] ToArray() {
            if (_bitCount > 0) {
                _output.Add((byte)(_buffer << (8 - _bitCount)));
            }
            return _output.ToArray();
        }
    }
}

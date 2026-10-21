using System;
using System.IO;
using System.Text;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>Encodes and decodes complete Swiss QR symbols through the existing QR and payment-payload owners.</summary>
public static class SwissQrCode {
    private static readonly UTF8Encoding Utf8 = new(false, true);

    /// <summary>Encodes a validated payment payload in UTF-8 byte mode with error correction M and the smallest fitting QR version.</summary>
    /// <remarks>The rendered symbol includes the recognition cross. It supplies the QR symbol, not a complete payment part and receipt.</remarks>
    public static SwissQrSymbol Encode(SwissQrCodePayload payload, bool allowLegacyCombinedAddresses = false) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var value = payload.ToString();
        if (!SwissQrCodePayload.TryParse(value, out _, allowLegacyCombinedAddresses))
            throw new ArgumentException("The payload does not satisfy the selected Swiss QR address and field rules.", nameof(payload));
        var qr = QrCodeEncoder.EncodeBytes(Utf8.GetBytes(value), QrErrorCorrectionLevel.M, maxVersion: 25);
        return new SwissQrSymbol(qr, payload);
    }

    /// <summary>Decodes and validates a Swiss payment from sampled QR modules.</summary>
    public static bool TryDecode(BitMatrix modules, out SwissQrCodePayload payload, bool allowLegacyCombinedAddresses = false) {
        payload = null!;
        return modules != null && QrDecoder.TryDecode(modules, out var qr) && TryRead(qr, out payload, allowLegacyCombinedAddresses);
    }

    /// <summary>Decodes and validates a rendered Swiss QR symbol from pixels, including the recognition overlay.</summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format,
        out SwissQrCodePayload payload, bool allowLegacyCombinedAddresses = false) {
        payload = null!;
        return pixels != null && QrImageDecoder.TryDecode(pixels, width, height, stride, format, out var qr) && TryRead(qr, out payload, allowLegacyCombinedAddresses);
    }

    private static bool TryRead(QrDecoded qr, out SwissQrCodePayload payload, bool legacy) {
        payload = null!;
        if (qr.ErrorCorrectionLevel != QrErrorCorrectionLevel.M || qr.Version > 25 || qr.StructuredAppend != null || qr.Fnc1Mode != QrFnc1Mode.None) return false;
        if (!SwissQrCodePayload.TryParseBytes(qr.Bytes, out var parsed, out var text, legacy)
            || (qr.HasEci && !string.Equals(qr.Text, text, StringComparison.Ordinal))) return false;
        payload = parsed;
        return true;
    }
}

/// <summary>An encoded Swiss QR payment with its standard recognition overlay.</summary>
public sealed class SwissQrSymbol {
    /// <summary>Gets the unmodified standard QR symbol and its encoding metadata.</summary>
    public QrCode Qr { get; }
    /// <summary>Gets the validated payment data.</summary>
    public SwissQrCodePayload Payload { get; }
    /// <summary>Gets the logical QR modules before applying the recognition overlay.</summary>
    public BitMatrix Modules => Qr.Modules;
    internal SwissQrSymbol(QrCode qr, SwissQrCodePayload payload) { Qr = qr; Payload = payload; }

    /// <summary>Renders a black-and-white Swiss QR with a centered cross proportional to 7 mm within the 46 mm QR area.</summary>
    /// <remarks>For print, scale the data-module area, excluding the quiet zone, to 46 by 46 mm. Output includes four quiet-zone modules on each side, so its complete width is 46 * (Modules.Width + 8) / Modules.Width mm. Vector output is recommended for physical scaling.</remarks>
    public RenderedOutput Render(OutputFormat format, int moduleSize = 8, OutputOptions? outputOptions = null) => Qr.Render(format, BuildOptions(moduleSize), outputOptions);

    /// <summary>Renders raw RGBA pixels with the Swiss cross.</summary>
    public byte[] RenderPixels(out int width, out int height, out int stride, int moduleSize = 8) => Qr.RenderPixels(out width, out height, out stride, BuildOptions(moduleSize));

    /// <summary>Saves the complete symbol using the file extension to select its format.</summary>
    public string Save(string path, int moduleSize = 8, OutputOptions? outputOptions = null) =>
        OutputWriter.Write(path, Render(OutputFormatInfo.Resolve(path, OutputFormat.Png), moduleSize, outputOptions));

    /// <summary>Writes the complete symbol to a caller-owned stream.</summary>
    public void Save(Stream stream, OutputFormat format, int moduleSize = 8, OutputOptions? outputOptions = null) =>
        OutputWriter.Write(stream, Render(format, moduleSize, outputOptions));

    private static QrRenderOptions BuildOptions(int moduleSize) => new() {
        ModuleSize = moduleSize, QuietZone = 4, LogoPng = LogoBuilder.CreateSwissCrossPng(),
        LogoScale = 7.0 / 46, LogoPaddingPx = 0, LogoDrawBackground = false, LogoCornerRadiusPx = 0
    };
}

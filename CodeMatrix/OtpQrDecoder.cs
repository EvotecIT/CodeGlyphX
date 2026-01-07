using System;
using CodeMatrix.Qr;

namespace CodeMatrix;

/// <summary>
/// Convenience decoder for OTP QR payloads.
/// </summary>
public static class OtpQrDecoder {
    /// <summary>
    /// Attempts to decode an OTP payload from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out OtpAuthPayload payload) {
        if (!QrDecoder.TryDecode(modules, out var decoded)) {
            payload = null!;
            return false;
        }

        return OtpAuthParser.TryParse(decoded.Text, out payload);
    }

    /// <summary>
    /// Attempts to decode an OTP payload from a module matrix and returns warnings.
    /// </summary>
    public static bool TryDecodeDetailed(BitMatrix modules, out OtpAuthParseResult result, out string error) {
        if (!QrDecoder.TryDecode(modules, out var decoded)) {
            result = null!;
            error = "QR decode failed.";
            return false;
        }

        return OtpAuthParser.TryParseDetailed(decoded.Text, out result, out error);
    }

    /// <summary>
    /// Attempts to decode and strictly validate an OTP payload from a module matrix.
    /// </summary>
    public static bool TryDecodeStrict(BitMatrix modules, out OtpAuthPayload payload, out string error) {
        return TryDecodeStrict(modules, OtpAuthValidationOptions.Strict, out payload, out error);
    }

    /// <summary>
    /// Attempts to decode and validate an OTP payload from a module matrix.
    /// </summary>
    public static bool TryDecodeStrict(BitMatrix modules, OtpAuthValidationOptions options, out OtpAuthPayload payload, out string error) {
        if (!TryDecodeDetailed(modules, out var result, out error)) {
            payload = null!;
            return false;
        }

        if (!OtpAuthValidator.TryValidate(result, options, out error)) {
            payload = null!;
            return false;
        }

        payload = result.Payload;
        return true;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode an OTP payload from raw pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out OtpAuthPayload payload) {
        if (!QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, IsOtpPayload, out var decoded, out _)) {
            payload = null!;
            return false;
        }

        return OtpAuthParser.TryParse(decoded.Text, out payload);
    }

    /// <summary>
    /// Attempts to decode an OTP payload from raw pixels and returns warnings.
    /// </summary>
    public static bool TryDecodeDetailed(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out OtpAuthParseResult result, out string error) {
        if (!QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, IsOtpPayload, out var decoded, out _)) {
            result = null!;
            error = "QR decode failed.";
            return false;
        }

        return OtpAuthParser.TryParseDetailed(decoded.Text, out result, out error);
    }

    /// <summary>
    /// Attempts to decode an OTP payload from raw pixels, returning QR diagnostics text and OTP warnings.
    /// </summary>
    public static bool TryDecodeDetailed(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out OtpAuthParseResult result, out string qrDiagnostics, out string error) {
        if (!QrPixelDecoder.TryDecode(pixels, width, height, stride, fmt, IsOtpPayload, out var decoded, out var diag)) {
            result = null!;
            qrDiagnostics = "qr-decode-failed";
            error = "QR decode failed.";
            return false;
        }

        qrDiagnostics = diag.ToString();
        return OtpAuthParser.TryParseDetailed(decoded.Text, out result, out error);
    }

    /// <summary>
    /// Attempts to decode and strictly validate an OTP payload from raw pixels.
    /// </summary>
    public static bool TryDecodeStrict(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out OtpAuthPayload payload, out string error) {
        return TryDecodeStrict(pixels, width, height, stride, fmt, OtpAuthValidationOptions.Strict, out payload, out error);
    }

    /// <summary>
    /// Attempts to decode and validate an OTP payload from raw pixels.
    /// </summary>
    public static bool TryDecodeStrict(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, OtpAuthValidationOptions options, out OtpAuthPayload payload, out string error) {
        if (!TryDecodeDetailed(pixels, width, height, stride, fmt, out var result, out error)) {
            payload = null!;
            return false;
        }

        if (!OtpAuthValidator.TryValidate(result, options, out error)) {
            payload = null!;
            return false;
        }

        payload = result.Payload;
        return true;
    }
#endif

    private static bool IsOtpPayload(QrDecoded decoded) {
        return OtpAuthParser.TryParse(decoded.Text, out _);
    }
}

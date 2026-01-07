using System;

namespace CodeMatrix;

/// <summary>
/// Opinionated QR presets for OTP URIs.
/// </summary>
public static class OtpQrPreset {
    /// <summary>
    /// Creates PNG render options optimized for OTP QR readability.
    /// </summary>
    public static Rendering.Png.QrPngRenderOptions CreatePngRenderOptions(
        int moduleSize = 6,
        int quietZone = 4,
        Rendering.Png.Rgba32? foreground = null,
        Rendering.Png.Rgba32? background = null) {
        if (moduleSize <= 0) throw new ArgumentOutOfRangeException(nameof(moduleSize));
        if (quietZone < 0) throw new ArgumentOutOfRangeException(nameof(quietZone));

        return new Rendering.Png.QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = foreground ?? Rendering.Png.Rgba32.Black,
            Background = background ?? Rendering.Png.Rgba32.White,
        };
    }

    /// <summary>
    /// Builds a TOTP <c>otpauth://</c> URI and encodes it as a QR code.
    /// </summary>
    public static QrCode EncodeTotp(
        string issuer,
        string account,
        byte[] secret,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6,
        int period = 30,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.H,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        var uri = OtpAuthTotp.Create(issuer, account, secret, alg, digits, period);
        return EncodeUri(uri, ecc, minVersion, maxVersion, forceMask);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Builds a TOTP <c>otpauth://</c> URI and encodes it as a QR code.
    /// </summary>
    public static QrCode EncodeTotp(
        string issuer,
        string account,
        ReadOnlySpan<byte> secret,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6,
        int period = 30,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.H,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        var uri = OtpAuthTotp.Create(issuer, account, secret, alg, digits, period);
        return EncodeUri(uri, ecc, minVersion, maxVersion, forceMask);
    }
#endif

    /// <summary>
    /// Builds a HOTP <c>otpauth://</c> URI and encodes it as a QR code.
    /// </summary>
    public static QrCode EncodeHotp(
        string issuer,
        string account,
        byte[] secret,
        long counter,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.H,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        var uri = OtpAuthHotp.Create(issuer, account, secret, counter, alg, digits);
        return EncodeUri(uri, ecc, minVersion, maxVersion, forceMask);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Builds a HOTP <c>otpauth://</c> URI and encodes it as a QR code.
    /// </summary>
    public static QrCode EncodeHotp(
        string issuer,
        string account,
        ReadOnlySpan<byte> secret,
        long counter,
        OtpAlgorithm alg = OtpAlgorithm.Sha1,
        int digits = 6,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.H,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        var uri = OtpAuthHotp.Create(issuer, account, secret, counter, alg, digits);
        return EncodeUri(uri, ecc, minVersion, maxVersion, forceMask);
    }
#endif

    /// <summary>
    /// Encodes a pre-built <c>otpauth://</c> URI as a QR code.
    /// </summary>
    public static QrCode EncodeUri(
        string otpauthUri,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.H,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        if (otpauthUri is null) throw new ArgumentNullException(nameof(otpauthUri));
        return QrCodeEncoder.EncodeText(otpauthUri, ecc, minVersion, maxVersion, forceMask);
    }
}

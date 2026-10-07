using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Creates standards-based OTP URIs and QR codes.
/// </summary>
public static class Otp {
    /// <summary>
    /// Builds a TOTP URI from a Base32 secret.
    /// </summary>
    public static string TotpUri(string issuer, string account, string secretBase32, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var secret = OtpAuthSecret.FromBase32(secretBase32);
        return OtpAuthTotp.Create(issuer, account, secret, alg, digits, period);
    }

    /// <summary>
    /// Builds a TOTP URI from raw secret bytes.
    /// </summary>
    public static string TotpUri(string issuer, string account, byte[] secret, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        return OtpAuthTotp.Create(issuer, account, secret, alg, digits, period);
    }

    /// <summary>
    /// Builds a HOTP URI from a Base32 secret.
    /// </summary>
    public static string HotpUri(string issuer, string account, string secretBase32, long counter, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var secret = OtpAuthSecret.FromBase32(secretBase32);
        return OtpAuthHotp.Create(issuer, account, secret, counter, alg, digits);
    }

    /// <summary>
    /// Builds a HOTP URI from raw secret bytes.
    /// </summary>
    public static string HotpUri(string issuer, string account, byte[] secret, long counter, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        return OtpAuthHotp.Create(issuer, account, secret, counter, alg, digits);
    }

    /// <summary>
    /// Starts a fluent TOTP builder.
    /// </summary>
    public static TotpBuilder Totp(string issuer, string account, string secretBase32, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) {
        return new TotpBuilder(issuer, account, OtpAuthSecret.FromBase32(secretBase32), renderOptions, encodingOptions);
    }

    /// <summary>
    /// Starts a fluent HOTP builder.
    /// </summary>
    public static HotpBuilder Hotp(string issuer, string account, string secretBase32, long counter, QrRenderOptions? renderOptions = null, QrEncodingOptions? encodingOptions = null) {
        return new HotpBuilder(issuer, account, OtpAuthSecret.FromBase32(secretBase32), counter, renderOptions, encodingOptions);
    }

    private static RenderedOutput RenderUri(string uri, OutputFormat format, QrRenderOptions renderOptions, QrEncodingOptions encodingOptions, OutputOptions? outputOptions) {
        return QR.Render(uri, format, renderOptions, encodingOptions, outputOptions);
    }

    private static string SaveUri(string uri, string path, QrRenderOptions renderOptions, QrEncodingOptions encodingOptions, OutputOptions? outputOptions) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        return OutputWriter.Write(path, RenderUri(uri, format, renderOptions, encodingOptions, outputOptions));
    }

    private static void SaveUri(string uri, Stream stream, OutputFormat format, QrRenderOptions renderOptions, QrEncodingOptions encodingOptions, OutputOptions? outputOptions) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, RenderUri(uri, format, renderOptions, encodingOptions, outputOptions));
    }

    /// <summary>
    /// Fluent TOTP builder.
    /// </summary>
    public sealed class TotpBuilder {
        private readonly string _issuer;
        private readonly string _account;
        private readonly byte[] _secret;

        /// <summary>
        /// Rendering options used by this builder.
        /// </summary>
        public QrRenderOptions Rendering { get; }

        /// <summary>Mutable encoding settings owned by this builder.</summary>
        public QrEncodingOptions Encoding { get; }

        /// <summary>
        /// OTP algorithm.
        /// </summary>
        public OtpAlgorithm Algorithm { get; set; } = OtpAlgorithm.Sha1;

        /// <summary>
        /// OTP digits.
        /// </summary>
        public int Digits { get; set; } = 6;

        /// <summary>
        /// TOTP period in seconds.
        /// </summary>
        public int Period { get; set; } = 30;

        internal TotpBuilder(string issuer, string account, byte[] secret, QrRenderOptions? renderOptions, QrEncodingOptions? encodingOptions) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _secret = secret ?? throw new ArgumentNullException(nameof(secret));
            Rendering = renderOptions is null ? new QrRenderOptions() : QrRenderer.CloneOptions(renderOptions);
            Encoding = encodingOptions?.Clone() ?? QrPresets.Otp();
        }

        /// <summary>
        /// Updates rendering options.
        /// </summary>
        public TotpBuilder WithRendering(Action<QrRenderOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Rendering);
            return this;
        }

        /// <summary>Updates the encoding settings owned by this builder.</summary>
        public TotpBuilder WithEncoding(Action<QrEncodingOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Encoding);
            return this;
        }

        /// <summary>
        /// Sets algorithm, digits, and period.
        /// </summary>
        public TotpBuilder WithParameters(OtpAlgorithm algorithm, int digits, int period) {
            Algorithm = algorithm;
            Digits = digits;
            Period = period;
            return this;
        }

        /// <summary>
        /// Builds the otpauth URI.
        /// </summary>
        public string Uri() => OtpAuthTotp.Create(_issuer, _account, _secret, Algorithm, Digits, Period);

        /// <summary>
        /// Encodes the QR code.
        /// </summary>
        public QrCode Encode() => QR.Encode(Uri(), Encoding);

        /// <summary>
        /// Renders the configured TOTP QR code to the requested output format.
        /// </summary>
        public RenderedOutput Render(OutputFormat format, OutputOptions? outputOptions = null) =>
            RenderUri(Uri(), format, Rendering, Encoding, outputOptions);

        /// <summary>
        /// Saves the configured TOTP QR code, selecting the output format from the file extension.
        /// </summary>
        public string Save(string path, OutputOptions? outputOptions = null) =>
            SaveUri(Uri(), path, Rendering, Encoding, outputOptions);

        /// <summary>
        /// Writes the configured TOTP QR code to a stream in the requested output format.
        /// </summary>
        public void Save(Stream stream, OutputFormat format, OutputOptions? outputOptions = null) =>
            SaveUri(Uri(), stream, format, Rendering, Encoding, outputOptions);
    }

    /// <summary>
    /// Fluent HOTP builder.
    /// </summary>
    public sealed class HotpBuilder {
        private readonly string _issuer;
        private readonly string _account;
        private readonly byte[] _secret;

        /// <summary>
        /// Rendering options used by this builder.
        /// </summary>
        public QrRenderOptions Rendering { get; }

        /// <summary>Mutable encoding settings owned by this builder.</summary>
        public QrEncodingOptions Encoding { get; }

        /// <summary>
        /// OTP algorithm.
        /// </summary>
        public OtpAlgorithm Algorithm { get; set; } = OtpAlgorithm.Sha1;

        /// <summary>
        /// OTP digits.
        /// </summary>
        public int Digits { get; set; } = 6;

        /// <summary>
        /// HOTP counter.
        /// </summary>
        public long Counter { get; set; }

        internal HotpBuilder(string issuer, string account, byte[] secret, long counter, QrRenderOptions? renderOptions, QrEncodingOptions? encodingOptions) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _secret = secret ?? throw new ArgumentNullException(nameof(secret));
            Counter = counter;
            Rendering = renderOptions is null ? new QrRenderOptions() : QrRenderer.CloneOptions(renderOptions);
            Encoding = encodingOptions?.Clone() ?? QrPresets.Otp();
        }

        /// <summary>
        /// Updates rendering options.
        /// </summary>
        public HotpBuilder WithRendering(Action<QrRenderOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Rendering);
            return this;
        }

        /// <summary>Updates the encoding settings owned by this builder.</summary>
        public HotpBuilder WithEncoding(Action<QrEncodingOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Encoding);
            return this;
        }

        /// <summary>
        /// Sets algorithm and digits.
        /// </summary>
        public HotpBuilder WithParameters(OtpAlgorithm algorithm, int digits) {
            Algorithm = algorithm;
            Digits = digits;
            return this;
        }

        /// <summary>
        /// Builds the otpauth URI.
        /// </summary>
        public string Uri() => OtpAuthHotp.Create(_issuer, _account, _secret, Counter, Algorithm, Digits);

        /// <summary>
        /// Encodes the QR code.
        /// </summary>
        public QrCode Encode() => QR.Encode(Uri(), Encoding);

        /// <summary>
        /// Renders the configured HOTP QR code to the requested output format.
        /// </summary>
        public RenderedOutput Render(OutputFormat format, OutputOptions? outputOptions = null) =>
            RenderUri(Uri(), format, Rendering, Encoding, outputOptions);

        /// <summary>
        /// Saves the configured HOTP QR code, selecting the output format from the file extension.
        /// </summary>
        public string Save(string path, OutputOptions? outputOptions = null) =>
            SaveUri(Uri(), path, Rendering, Encoding, outputOptions);

        /// <summary>
        /// Writes the configured HOTP QR code to a stream in the requested output format.
        /// </summary>
        public void Save(Stream stream, OutputFormat format, OutputOptions? outputOptions = null) =>
            SaveUri(Uri(), stream, format, Rendering, Encoding, outputOptions);
    }
}

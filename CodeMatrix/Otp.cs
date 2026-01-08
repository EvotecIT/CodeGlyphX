using System;
using System.IO;
using CodeMatrix.Rendering;

namespace CodeMatrix;

/// <summary>
/// Simple OTP helpers with fluent and static APIs.
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
    /// Renders a TOTP QR as PNG from a Base32 secret.
    /// </summary>
    public static byte[] TotpPng(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return QrEasy.RenderPng(uri, options);
    }

    /// <summary>
    /// Renders a TOTP QR as SVG from a Base32 secret.
    /// </summary>
    public static string TotpSvg(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return QrEasy.RenderSvg(uri, options);
    }

    /// <summary>
    /// Renders a TOTP QR as HTML from a Base32 secret.
    /// </summary>
    public static string TotpHtml(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return QrEasy.RenderHtml(uri, options);
    }

    /// <summary>
    /// Renders a TOTP QR as JPEG from a Base32 secret.
    /// </summary>
    public static byte[] TotpJpeg(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return QrEasy.RenderJpeg(uri, options);
    }

    /// <summary>
    /// Renders a HOTP QR as PNG from a Base32 secret.
    /// </summary>
    public static byte[] HotpPng(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return QrEasy.RenderPng(uri, options);
    }

    /// <summary>
    /// Renders a HOTP QR as SVG from a Base32 secret.
    /// </summary>
    public static string HotpSvg(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return QrEasy.RenderSvg(uri, options);
    }

    /// <summary>
    /// Renders a HOTP QR as HTML from a Base32 secret.
    /// </summary>
    public static string HotpHtml(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return QrEasy.RenderHtml(uri, options);
    }

    /// <summary>
    /// Renders a HOTP QR as JPEG from a Base32 secret.
    /// </summary>
    public static byte[] HotpJpeg(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return QrEasy.RenderJpeg(uri, options);
    }

    /// <summary>
    /// Saves a TOTP PNG to a file.
    /// </summary>
    public static string SaveTotpPng(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        return TotpPng(issuer, account, secretBase32, options, alg, digits, period).WriteBinary(path);
    }

    /// <summary>
    /// Saves a TOTP PNG to a stream.
    /// </summary>
    public static void SaveTotpPng(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        QR.SavePng(TotpUri(issuer, account, secretBase32, alg, digits, period), stream, options);
    }

    /// <summary>
    /// Saves a TOTP SVG to a file.
    /// </summary>
    public static string SaveTotpSvg(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        return TotpSvg(issuer, account, secretBase32, options, alg, digits, period).WriteText(path);
    }

    /// <summary>
    /// Saves a TOTP SVG to a stream.
    /// </summary>
    public static void SaveTotpSvg(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        TotpSvg(issuer, account, secretBase32, options, alg, digits, period).WriteText(stream);
    }

    /// <summary>
    /// Saves a TOTP HTML to a file.
    /// </summary>
    public static string SaveTotpHtml(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30, string? title = null) {
        var html = TotpHtml(issuer, account, secretBase32, options, alg, digits, period);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves a TOTP HTML to a stream.
    /// </summary>
    public static void SaveTotpHtml(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30, string? title = null) {
        var html = TotpHtml(issuer, account, secretBase32, options, alg, digits, period);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves a TOTP JPEG to a file.
    /// </summary>
    public static string SaveTotpJpeg(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        return TotpJpeg(issuer, account, secretBase32, options, alg, digits, period).WriteBinary(path);
    }

    /// <summary>
    /// Saves a TOTP JPEG to a stream.
    /// </summary>
    public static void SaveTotpJpeg(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        QR.SaveJpeg(TotpUri(issuer, account, secretBase32, alg, digits, period), stream, options);
    }

    /// <summary>
    /// Saves a HOTP PNG to a file.
    /// </summary>
    public static string SaveHotpPng(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        return HotpPng(issuer, account, secretBase32, counter, options, alg, digits).WriteBinary(path);
    }

    /// <summary>
    /// Saves a HOTP PNG to a stream.
    /// </summary>
    public static void SaveHotpPng(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        QR.SavePng(HotpUri(issuer, account, secretBase32, counter, alg, digits), stream, options);
    }

    /// <summary>
    /// Saves a HOTP SVG to a file.
    /// </summary>
    public static string SaveHotpSvg(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        return HotpSvg(issuer, account, secretBase32, counter, options, alg, digits).WriteText(path);
    }

    /// <summary>
    /// Saves a HOTP SVG to a stream.
    /// </summary>
    public static void SaveHotpSvg(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        HotpSvg(issuer, account, secretBase32, counter, options, alg, digits).WriteText(stream);
    }

    /// <summary>
    /// Saves a HOTP HTML to a file.
    /// </summary>
    public static string SaveHotpHtml(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, string? title = null) {
        var html = HotpHtml(issuer, account, secretBase32, counter, options, alg, digits);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves a HOTP HTML to a stream.
    /// </summary>
    public static void SaveHotpHtml(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, string? title = null) {
        var html = HotpHtml(issuer, account, secretBase32, counter, options, alg, digits);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves a HOTP JPEG to a file.
    /// </summary>
    public static string SaveHotpJpeg(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        return HotpJpeg(issuer, account, secretBase32, counter, options, alg, digits).WriteBinary(path);
    }

    /// <summary>
    /// Saves a HOTP JPEG to a stream.
    /// </summary>
    public static void SaveHotpJpeg(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        QR.SaveJpeg(HotpUri(issuer, account, secretBase32, counter, alg, digits), stream, options);
    }

    /// <summary>
    /// Starts a fluent TOTP builder.
    /// </summary>
    public static TotpBuilder Totp(string issuer, string account, string secretBase32, QrEasyOptions? options = null) {
        return new TotpBuilder(issuer, account, OtpAuthSecret.FromBase32(secretBase32), options);
    }

    /// <summary>
    /// Starts a fluent HOTP builder.
    /// </summary>
    public static HotpBuilder Hotp(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null) {
        return new HotpBuilder(issuer, account, OtpAuthSecret.FromBase32(secretBase32), counter, options);
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
        public QrEasyOptions Options { get; }

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

        internal TotpBuilder(string issuer, string account, byte[] secret, QrEasyOptions? options) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _secret = secret ?? throw new ArgumentNullException(nameof(secret));
            Options = options ?? new QrEasyOptions();
        }

        /// <summary>
        /// Updates rendering options.
        /// </summary>
        public TotpBuilder WithOptions(Action<QrEasyOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Options);
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
        public QrCode Encode() => QrEasy.Encode(Uri(), Options);

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => QrEasy.RenderPng(Uri(), Options);

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => QrEasy.RenderSvg(Uri(), Options);

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => QrEasy.RenderHtml(Uri(), Options);

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => QrEasy.RenderJpeg(Uri(), Options);

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => Png().WriteBinary(path);

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => QR.SavePng(Uri(), stream, Options);

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => Svg().WriteText(path);

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => Svg().WriteText(stream);

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            var html = Html();
            if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
            return html.WriteText(path);
        }

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) {
            var html = Html();
            if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
            html.WriteText(stream);
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => Jpeg().WriteBinary(path);

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => QR.SaveJpeg(Uri(), stream, Options);
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
        public QrEasyOptions Options { get; }

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

        internal HotpBuilder(string issuer, string account, byte[] secret, long counter, QrEasyOptions? options) {
            _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _secret = secret ?? throw new ArgumentNullException(nameof(secret));
            Counter = counter;
            Options = options ?? new QrEasyOptions();
        }

        /// <summary>
        /// Updates rendering options.
        /// </summary>
        public HotpBuilder WithOptions(Action<QrEasyOptions> configure) {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(Options);
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
        public QrCode Encode() => QrEasy.Encode(Uri(), Options);

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => QrEasy.RenderPng(Uri(), Options);

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => QrEasy.RenderSvg(Uri(), Options);

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => QrEasy.RenderHtml(Uri(), Options);

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => QrEasy.RenderJpeg(Uri(), Options);

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => Png().WriteBinary(path);

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => QR.SavePng(Uri(), stream, Options);

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => Svg().WriteText(path);

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => Svg().WriteText(stream);

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            var html = Html();
            if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
            return html.WriteText(path);
        }

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) {
            var html = Html();
            if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
            html.WriteText(stream);
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => Jpeg().WriteBinary(path);

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => QR.SaveJpeg(Uri(), stream, Options);
    }
}

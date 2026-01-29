using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

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

    private static RenderedOutput RenderUri(string uri, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        return QrCode.Render(uri, format, options, extras);
    }

    /// <summary>
    /// Renders a TOTP QR as PNG from a Base32 secret.
    /// </summary>
    public static byte[] TotpPng(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return RenderUri(uri, OutputFormat.Png, options).Data;
    }

    /// <summary>
    /// Renders a TOTP QR as SVG from a Base32 secret.
    /// </summary>
    public static string TotpSvg(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return RenderUri(uri, OutputFormat.Svg, options).GetText();
    }

    /// <summary>
    /// Renders a TOTP QR as HTML from a Base32 secret.
    /// </summary>
    public static string TotpHtml(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return RenderUri(uri, OutputFormat.Html, options).GetText();
    }

    /// <summary>
    /// Renders a TOTP QR as JPEG from a Base32 secret.
    /// </summary>
    public static byte[] TotpJpeg(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return RenderUri(uri, OutputFormat.Jpeg, options).Data;
    }

    /// <summary>
    /// Renders a TOTP QR as WebP from a Base32 secret.
    /// </summary>
    public static byte[] TotpWebp(string issuer, string account, string secretBase32, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return RenderUri(uri, OutputFormat.Webp, options).Data;
    }

    /// <summary>
    /// Renders a HOTP QR as PNG from a Base32 secret.
    /// </summary>
    public static byte[] HotpPng(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return RenderUri(uri, OutputFormat.Png, options).Data;
    }

    /// <summary>
    /// Renders a HOTP QR as SVG from a Base32 secret.
    /// </summary>
    public static string HotpSvg(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return RenderUri(uri, OutputFormat.Svg, options).GetText();
    }

    /// <summary>
    /// Renders a HOTP QR as HTML from a Base32 secret.
    /// </summary>
    public static string HotpHtml(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return RenderUri(uri, OutputFormat.Html, options).GetText();
    }

    /// <summary>
    /// Renders a HOTP QR as JPEG from a Base32 secret.
    /// </summary>
    public static byte[] HotpJpeg(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return RenderUri(uri, OutputFormat.Jpeg, options).Data;
    }

    /// <summary>
    /// Renders a HOTP QR as WebP from a Base32 secret.
    /// </summary>
    public static byte[] HotpWebp(string issuer, string account, string secretBase32, long counter, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return RenderUri(uri, OutputFormat.Webp, options).Data;
    }

    /// <summary>
    /// Saves a TOTP PNG to a file.
    /// </summary>
    public static string SaveTotpPng(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves a TOTP PNG to a stream.
    /// </summary>
    public static void SaveTotpPng(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves a TOTP SVG to a file.
    /// </summary>
    public static string SaveTotpSvg(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves a TOTP SVG to a stream.
    /// </summary>
    public static void SaveTotpSvg(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves a TOTP HTML to a file.
    /// </summary>
    public static string SaveTotpHtml(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30, string? title = null) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves a TOTP HTML to a stream.
    /// </summary>
    public static void SaveTotpHtml(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30, string? title = null) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves a TOTP JPEG to a file.
    /// </summary>
    public static string SaveTotpJpeg(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a TOTP JPEG to a stream.
    /// </summary>
    public static void SaveTotpJpeg(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a TOTP WebP to a file.
    /// </summary>
    public static string SaveTotpWebp(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a TOTP WebP to a stream.
    /// </summary>
    public static void SaveTotpWebp(string issuer, string account, string secretBase32, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a HOTP PNG to a file.
    /// </summary>
    public static string SaveHotpPng(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves a HOTP PNG to a stream.
    /// </summary>
    public static void SaveHotpPng(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Png, options));
    }

    /// <summary>
    /// Saves a HOTP SVG to a file.
    /// </summary>
    public static string SaveHotpSvg(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves a HOTP SVG to a stream.
    /// </summary>
    public static void SaveHotpSvg(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Svg, options));
    }

    /// <summary>
    /// Saves a HOTP HTML to a file.
    /// </summary>
    public static string SaveHotpHtml(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, string? title = null) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves a HOTP HTML to a stream.
    /// </summary>
    public static void SaveHotpHtml(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, string? title = null) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Html, options, extras));
    }

    /// <summary>
    /// Saves a HOTP JPEG to a file.
    /// </summary>
    public static string SaveHotpJpeg(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a HOTP JPEG to a stream.
    /// </summary>
    public static void SaveHotpJpeg(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Jpeg, options));
    }

    /// <summary>
    /// Saves a HOTP WebP to a file.
    /// </summary>
    public static string SaveHotpWebp(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        return OutputWriter.Write(path, RenderUri(uri, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a HOTP WebP to a stream.
    /// </summary>
    public static void SaveHotpWebp(string issuer, string account, string secretBase32, long counter, Stream stream, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        OutputWriter.Write(stream, RenderUri(uri, OutputFormat.Webp, options));
    }

    /// <summary>
    /// Saves a TOTP QR based on file extension (.png/.webp/.svg/.html/.jpg/.bmp/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveTotp(string issuer, string account, string secretBase32, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, int period = 30, string? title = null) {
        var uri = TotpUri(issuer, account, secretBase32, alg, digits, period);
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        return OutputWriter.Write(path, RenderUri(uri, format, options, extras));
    }

    /// <summary>
    /// Saves a HOTP QR based on file extension (.png/.webp/.svg/.html/.jpg/.bmp/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveHotp(string issuer, string account, string secretBase32, long counter, string path, QrEasyOptions? options = null, OtpAlgorithm alg = OtpAlgorithm.Sha1, int digits = 6, string? title = null) {
        var uri = HotpUri(issuer, account, secretBase32, counter, alg, digits);
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        return OutputWriter.Write(path, RenderUri(uri, format, options, extras));
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

        private RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return RenderUri(Uri(), format, Options, extras);
        }

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => Render(OutputFormat.Png).Data;

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => Render(OutputFormat.Svg).GetText();

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => Render(OutputFormat.Html).GetText();

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => Render(OutputFormat.Jpeg).Data;

        /// <summary>
        /// Renders WebP bytes.
        /// </summary>
        public byte[] Webp() => Render(OutputFormat.Webp).Data;

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => OutputWriter.Write(path, Render(OutputFormat.Png));

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Png));

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => OutputWriter.Write(path, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            return OutputWriter.Write(path, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            OutputWriter.Write(stream, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => OutputWriter.Write(path, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves WebP to a file.
        /// </summary>
        public string SaveWebp(string path) => OutputWriter.Write(path, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves WebP to a stream.
        /// </summary>
        public void SaveWebp(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves based on file extension (.png/.webp/.svg/.html/.jpg). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
            return OutputWriter.Write(path, Render(format, extras));
        }
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

        private RenderedOutput Render(OutputFormat format, RenderExtras? extras = null) {
            return RenderUri(Uri(), format, Options, extras);
        }

        /// <summary>
        /// Renders PNG bytes.
        /// </summary>
        public byte[] Png() => Render(OutputFormat.Png).Data;

        /// <summary>
        /// Renders SVG text.
        /// </summary>
        public string Svg() => Render(OutputFormat.Svg).GetText();

        /// <summary>
        /// Renders HTML text.
        /// </summary>
        public string Html() => Render(OutputFormat.Html).GetText();

        /// <summary>
        /// Renders JPEG bytes.
        /// </summary>
        public byte[] Jpeg() => Render(OutputFormat.Jpeg).Data;

        /// <summary>
        /// Renders WebP bytes.
        /// </summary>
        public byte[] Webp() => Render(OutputFormat.Webp).Data;

        /// <summary>
        /// Saves PNG to a file.
        /// </summary>
        public string SavePng(string path) => OutputWriter.Write(path, Render(OutputFormat.Png));

        /// <summary>
        /// Saves PNG to a stream.
        /// </summary>
        public void SavePng(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Png));

        /// <summary>
        /// Saves SVG to a file.
        /// </summary>
        public string SaveSvg(string path) => OutputWriter.Write(path, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves SVG to a stream.
        /// </summary>
        public void SaveSvg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Svg));

        /// <summary>
        /// Saves HTML to a file.
        /// </summary>
        public string SaveHtml(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            return OutputWriter.Write(path, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves HTML to a stream.
        /// </summary>
        public void SaveHtml(Stream stream, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            OutputWriter.Write(stream, Render(OutputFormat.Html, extras));
        }

        /// <summary>
        /// Saves JPEG to a file.
        /// </summary>
        public string SaveJpeg(string path) => OutputWriter.Write(path, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves JPEG to a stream.
        /// </summary>
        public void SaveJpeg(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Jpeg));

        /// <summary>
        /// Saves WebP to a file.
        /// </summary>
        public string SaveWebp(string path) => OutputWriter.Write(path, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves WebP to a stream.
        /// </summary>
        public void SaveWebp(Stream stream) => OutputWriter.Write(stream, Render(OutputFormat.Webp));

        /// <summary>
        /// Saves based on file extension (.png/.webp/.svg/.html/.jpg). Defaults to PNG when no extension is provided.
        /// </summary>
        public string Save(string path, string? title = null) {
            var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
            var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
            return OutputWriter.Write(path, Render(format, extras));
        }
    }
}

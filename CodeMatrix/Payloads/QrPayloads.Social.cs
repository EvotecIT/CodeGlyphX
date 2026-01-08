using System;
namespace CodeMatrix.Payloads;

public static partial class QrPayloads {
    /// <summary>
    /// Builds an App Store payload for the given platform.
    /// </summary>
    public static QrPayloadData AppStore(string appIdOrUrl, QrAppStorePlatform platform = QrAppStorePlatform.Apple) {
        var payload = BuildAppStoreUrl(appIdOrUrl, platform);
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a Facebook profile payload.
    /// </summary>
    public static QrPayloadData Facebook(string profileOrUrl) {
        var payload = BuildProfileUrl(profileOrUrl, "https://www.facebook.com/", ensureAtPrefix: false);
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a Twitter/X profile payload.
    /// </summary>
    public static QrPayloadData Twitter(string handleOrUrl) {
        var payload = BuildProfileUrl(handleOrUrl, "https://x.com/", ensureAtPrefix: false);
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a TikTok profile payload.
    /// </summary>
    public static QrPayloadData TikTok(string handleOrUrl) {
        var payload = BuildProfileUrl(handleOrUrl, "https://www.tiktok.com/", ensureAtPrefix: true);
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a LinkedIn profile payload.
    /// </summary>
    public static QrPayloadData LinkedIn(string handleOrUrl) {
        var payload = BuildProfileUrl(handleOrUrl, "https://www.linkedin.com/in/", ensureAtPrefix: false);
        return new QrPayloadData(payload);
    }

    private static string BuildAppStoreUrl(string appIdOrUrl, QrAppStorePlatform platform) {
        if (appIdOrUrl is null) return string.Empty;
        var value = appIdOrUrl.Trim();
        if (value.Length == 0) return string.Empty;

        if (value.Contains("://", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("apps.apple.com", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("play.google.com", StringComparison.OrdinalIgnoreCase)) {
            return NormalizeUrl(value);
        }

        return platform == QrAppStorePlatform.GooglePlay
            ? "https://play.google.com/store/apps/details?id=" + value
            : "https://apps.apple.com/app/id" + StripAppleIdPrefix(value);
    }

    private static string BuildProfileUrl(string handleOrUrl, string baseUrl, bool ensureAtPrefix) {
        if (handleOrUrl is null) return string.Empty;
        var value = handleOrUrl.Trim();
        if (value.Length == 0) return string.Empty;
        if (value.Contains("://", StringComparison.OrdinalIgnoreCase)) return value;
        if (value.Contains(baseUrl, StringComparison.OrdinalIgnoreCase)) return NormalizeUrl(value);

        var handle = value.StartsWith("@", StringComparison.Ordinal) ? value[1..] : value;
        if (ensureAtPrefix) handle = "@" + handle;
        return baseUrl + handle;
    }

    private static string StripAppleIdPrefix(string value) {
        if (value.StartsWith("id", StringComparison.OrdinalIgnoreCase)) {
            return value.Substring(2);
        }
        return value;
    }
}

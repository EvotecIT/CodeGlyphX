using CodeMatrix.Payloads;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class QrPayloadSocialTests {
    [Fact]
    public void AppStore_Apple_BuildsDefaultUrl() {
        var payload = QrPayloads.AppStore("123456789").Text;
        Assert.Equal("https://apps.apple.com/app/id123456789", payload);
    }

    [Fact]
    public void AppStore_GooglePlay_BuildsDefaultUrl() {
        var payload = QrPayloads.AppStore("com.example.app", QrAppStorePlatform.GooglePlay).Text;
        Assert.Equal("https://play.google.com/store/apps/details?id=com.example.app", payload);
    }

    [Fact]
    public void Social_Profiles_BuildDefaultUrls() {
        Assert.Equal("https://www.facebook.com/openai", QrPayloads.Facebook("openai").Text);
        Assert.Equal("https://x.com/openai", QrPayloads.Twitter("@openai").Text);
        Assert.Equal("https://www.tiktok.com/@openai", QrPayloads.TikTok("openai").Text);
        Assert.Equal("https://www.linkedin.com/in/openai", QrPayloads.LinkedIn("openai").Text);
    }
}

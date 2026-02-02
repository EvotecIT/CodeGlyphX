using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RenderSanitizerTests {
    [Fact]
    public void SafeCssColor_Allows_KnownFormats() {
        Assert.Equal("#fff", RenderSanitizer.SafeCssColor("#fff", "black"));
        Assert.Equal("rgb(0, 0, 0)", RenderSanitizer.SafeCssColor("rgb(0, 0, 0)", "black"));
        Assert.Equal("rebeccapurple", RenderSanitizer.SafeCssColor("rebeccapurple", "black"));
    }

    [Fact]
    public void SafeCssColor_Rejects_Injection() {
        var fallback = "black";
        var value = "red; background:url(javascript:alert(1))";

        Assert.Equal(fallback, RenderSanitizer.SafeCssColor(value, fallback));
    }

    [Fact]
    public void SafeFontFamily_Strips_DisallowedChars() {
        var fallback = "sans-serif";
        var value = "Roboto, 'Comic Sans'; url(x)";

        Assert.Equal("Roboto, Comic Sans urlx", RenderSanitizer.SafeFontFamily(value, fallback));
    }

    [Fact]
    public void SafeFontFamily_FallsBack_OnEmptyResult() {
        var fallback = "sans-serif";

        Assert.Equal(fallback, RenderSanitizer.SafeFontFamily("'" , fallback));
    }
}

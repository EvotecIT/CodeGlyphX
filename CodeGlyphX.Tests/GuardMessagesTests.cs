using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GuardMessagesTests {
    [Fact]
    public void ForBytes_Includes_Actual_And_Max() {
        var message = GuardMessages.ForBytes("Input exceeds size limits.", 2048, 1024);

        Assert.Contains("got 2 KB", message);
        Assert.Contains("max 1 KB", message);
    }

    [Fact]
    public void ForBytes_Returns_Message_When_MaxDisabled() {
        var message = GuardMessages.ForBytes("Limit.", 100, 0);

        Assert.Equal("Limit.", message);
    }

    [Fact]
    public void ForBytes_Returns_Message_When_ActualInvalid() {
        var message = GuardMessages.ForBytes("Limit.", -1, 100);

        Assert.Equal("Limit.", message);
    }

    [Fact]
    public void ForPixels_Includes_Dimensions_And_Max() {
        var message = GuardMessages.ForPixels("Image dimensions exceed limits.", 10, 20, 200, 100);

        Assert.Contains("got 10x20 = 200 px", message);
        Assert.Contains("max 100 px", message);
    }

    [Fact]
    public void ForPixels_Uses_Total_When_Dimensions_Missing() {
        var message = GuardMessages.ForPixels("Image dimensions exceed limits.", 0, 0, 500, 100);

        Assert.Contains("got 500 = 500 px", message);
    }
}

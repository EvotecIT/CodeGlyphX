#if NET10_0
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering.Art;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed partial class PlaygroundLivePreviewTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ADelayedRecipeCannotReplaceNewerDataOrDesign(bool usePreset) {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "pattern", "scene");
        var file = new SceneBrowserFile(new QrSceneRecipe("OBSOLETE-RECIPE", QrScenePresets.Create(QrSceneStyle.ElectricCity)).ToXml(), deferred: true);
        var upload = renderer.Upload("Open scene recipe", file);
        await file.Started;
        if (usePreset) await renderer.Click("WiFi");
        else {
            await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Styled");
            await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "NEWER-DATA");
        }
        file.Release();
        await upload;
        Assert.True(QrImageDecoder.TryDecodeImage(renderer.Png(), out var decoded));
        Assert.Equal(usePreset ? "WIFI:T:WPA;S:Office-WiFi;P:Password123;;" : "NEWER-DATA", decoded.Text);
        Assert.True(file.ReadCancellation.IsCancellationRequested);
        Assert.Empty(renderer.Errors);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task AnUnrenderableRecipeRetainsCurrentDataDesignAndExports(bool oversizedPayload, bool hiddenLogo) {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "CURRENT-DATA");
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "pattern", "scene");
        var before = renderer.PreviewUri();
        var design = QrScenePresets.Create(QrSceneStyle.ElectricCity);
        if (!oversizedPayload) { design.LogoImage = new byte[] { 1, 2, 3 }; design.Logo.Visible = !hiddenLogo; }
        var recipe = new QrSceneRecipe(oversizedPayload ? new string('x', 4096) : "INVALID-LOGO-DATA", design);
        await renderer.Upload("Open scene recipe", new SceneBrowserFile(recipe.ToXml()));
        Assert.Equal(before, renderer.PreviewUri());
        Assert.Contains(renderer.Elements(), e => e.Tag == "textarea" && e.Attribute("value") == "CURRENT-DATA");
        Assert.Contains(renderer.Elements(), e => e.Tag == "p" && e.Attribute("role") == "alert");
        Assert.Equal(before, renderer.Elements().Single(e => e.Attribute("download") == "qrcode.png").Attribute("href"));
        Assert.Empty(renderer.Errors);
    }

    // A delayed browser stream models the real asynchronous InputFile boundary without a clock or product hook.
    private sealed class SceneBrowserFile : IBrowserFile {
        private readonly byte[] _bytes;
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public SceneBrowserFile(string xml, bool deferred = false) { _bytes = Encoding.UTF8.GetBytes(xml); if (!deferred) _release.SetResult(); }
        public string Name => "scene.cgxart";
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size => _bytes.Length;
        public string ContentType => "application/xml";
        public Task Started => _started.Task;
        public CancellationToken ReadCancellation { get; private set; }
        public void Release() => _release.TrySetResult();
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) {
            if (Size > maxAllowedSize) throw new IOException("Browser file exceeds its stream limit.");
            ReadCancellation = cancellationToken;
            return new SceneReadStream(_bytes, _started, _release.Task, cancellationToken);
        }
    }
    private sealed class SceneReadStream(byte[] bytes, TaskCompletionSource started, Task release, CancellationToken uploadToken) : MemoryStream(bytes) {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            started.TrySetResult();
            await release.WaitAsync(uploadToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
}
#endif

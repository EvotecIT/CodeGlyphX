namespace CodeGlyphX.Rendering.Ico;

public sealed partial class IcoRenderOptions {
    /// <summary>
    /// Sets the output icon sizes in pixels.
    /// </summary>
    public IcoRenderOptions WithSizes(int[] sizes) {
        Sizes = sizes;
        return this;
    }

    /// <summary>
    /// Sets whether to preserve aspect ratio when rendering icons.
    /// </summary>
    public IcoRenderOptions WithPreserveAspectRatio(bool preserveAspectRatio = true) {
        PreserveAspectRatio = preserveAspectRatio;
        return this;
    }
}

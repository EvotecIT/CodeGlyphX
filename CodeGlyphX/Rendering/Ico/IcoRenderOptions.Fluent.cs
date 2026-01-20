namespace CodeGlyphX.Rendering.Ico;

public sealed partial class IcoRenderOptions {
    /// <summary>Sets the icon sizes to generate.</summary>
    public IcoRenderOptions WithSizes(int[] sizes) {
        Sizes = sizes;
        return this;
    }

    /// <summary>Sets whether to preserve the source aspect ratio.</summary>
    public IcoRenderOptions WithPreserveAspectRatio(bool preserveAspectRatio = true) {
        PreserveAspectRatio = preserveAspectRatio;
        return this;
    }
}

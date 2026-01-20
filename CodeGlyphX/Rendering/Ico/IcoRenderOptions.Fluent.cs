namespace CodeGlyphX.Rendering.Ico;

public sealed partial class IcoRenderOptions {
    public IcoRenderOptions WithSizes(int[] sizes) {
        Sizes = sizes;
        return this;
    }

    public IcoRenderOptions WithPreserveAspectRatio(bool preserveAspectRatio = true) {
        PreserveAspectRatio = preserveAspectRatio;
        return this;
    }
}

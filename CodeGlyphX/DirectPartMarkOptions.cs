namespace CodeGlyphX;

/// <summary>Controls opt-in ISO/IEC 29158-style direct-part-mark image recovery.</summary>
public sealed class DirectPartMarkOptions {
    /// <summary>Gets or sets the material/marking profile.</summary>
    public DirectPartMarkProfile Profile { get; set; } = DirectPartMarkProfile.Auto;
    /// <summary>Gets or sets the local adaptive-threshold radius. Zero derives it from image size.</summary>
    public int AdaptiveWindowRadius { get; set; }
    /// <summary>Gets or sets the local contrast bias from 0 through 64.</summary>
    public int ThresholdBias { get; set; } = 4;
    /// <summary>Gets or sets the morphology radius from 0 through 3.</summary>
    public int MorphologyRadius { get; set; } = 1;
    /// <summary>Gets or sets the maximum number of preprocessing attempts from 1 through 8.</summary>
    public int MaxAttempts { get; set; } = 6;

    /// <summary>Creates a balanced direct-part-mark profile.</summary>
    public static DirectPartMarkOptions Auto() => new();
    /// <summary>Creates a dot-peen profile that reconnects sparse module marks.</summary>
    public static DirectPartMarkOptions DotPeen() => new() { Profile = DirectPartMarkProfile.DotPeen, ThresholdBias = 2, MorphologyRadius = 1 };
    /// <summary>Creates a low-contrast laser-etch profile.</summary>
    public static DirectPartMarkOptions LaserEtch() => new() { Profile = DirectPartMarkProfile.LaserEtch, ThresholdBias = 3, MorphologyRadius = 0 };

    internal DirectPartMarkOptions Clone() => new() {
        Profile = Profile, AdaptiveWindowRadius = AdaptiveWindowRadius, ThresholdBias = ThresholdBias,
        MorphologyRadius = MorphologyRadius, MaxAttempts = MaxAttempts
    };
}

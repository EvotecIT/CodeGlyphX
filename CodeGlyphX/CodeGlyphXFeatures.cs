namespace CodeGlyphX;

/// <summary>
/// Runtime feature flags for CodeGlyphX capabilities.
/// </summary>
public static class CodeGlyphXFeatures {
    /// <summary>
    /// True when the high-performance QR pixel pipeline is available (net8+).
    /// </summary>
    public static bool SupportsQrPixelDecode {
        get {
#if NET8_0_OR_GREATER
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// True when QR pixel debug rendering is available (net8+).
    /// </summary>
    public static bool SupportsQrPixelDebug {
        get {
#if NET8_0_OR_GREATER
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// True when span-based pixel APIs and fast pipeline paths are available (net8+).
    /// </summary>
    public static bool SupportsSpanPixelPipeline {
        get {
#if NET8_0_OR_GREATER
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// The target framework for this build (compile-time constant).
    /// </summary>
    public static string TargetFramework {
        get {
#if NET10_0_OR_GREATER
            return "net10.0";
#elif NET8_0_OR_GREATER
            return "net8.0";
#elif NET472
            return "net472";
#elif NETSTANDARD2_0
            return "netstandard2.0";
#else
            return "unknown";
#endif
        }
    }
}

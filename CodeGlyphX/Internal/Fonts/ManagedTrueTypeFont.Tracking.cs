// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;

namespace CodeGlyphX.Internal.Fonts;

internal sealed partial class ManagedTrueTypeFont {
    private double _trackingEvaluationScale = 1D;

    // A raster resolution change scales geometry, while tracking retains the authored text size.
    internal ManagedTrueTypeFont ForOutputScale(double scale) {
        if (_tracking == null || scale == _trackingEvaluationScale) return this;
        if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
        var instance = (ManagedTrueTypeFont)MemberwiseClone();
        instance._opticalSizeFonts = null;
        instance._trackingEvaluationScale = scale;
        return instance;
    }

    private double HorizontalTracking(double fontSize) =>
        (_tracking?.GetAdjustment(fontSize / _trackingEvaluationScale) ?? 0D) * ScaleFor(fontSize);

    private bool[]? TrackingBoundaries(string text, IReadOnlyList<int> indexes, bool negative = false) {
        if (_tracking == null || indexes.Count == 0) return null;
        return ManagedOpenTypeTracking.GetBoundaries(text, indexes, negative);
    }
}

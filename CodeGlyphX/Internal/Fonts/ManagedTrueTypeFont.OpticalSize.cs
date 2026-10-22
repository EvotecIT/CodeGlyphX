// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.IO;

namespace CodeGlyphX.Internal.Fonts;

internal sealed partial class ManagedTrueTypeFont {
    private readonly object _opticalSizeSync = new();
    private Dictionary<float, ManagedTrueTypeFont>? _opticalSizeFonts;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "S1244:Floating point numbers should not be tested for equality", Justification = "Exact authored optical-size identity controls instance caching; adjacent sizes must remain distinct.")]
    internal ManagedTrueTypeFont ForOpticalSize(double authoredSize) {
        if (!_variationModel.TryGetOpticalSize(authoredSize, out float size) ||
            _variationModel.DesignCoordinates["opsz"] == size) return this;
        lock (_opticalSizeSync) {
            var cache = _opticalSizeFonts ??= new Dictionary<float, ManagedTrueTypeFont>();
            if (cache.TryGetValue(size, out ManagedTrueTypeFont? instance)) return instance;
            ManagedOpenTypeReader reader = ManagedOpenTypeReader.TryCreate(_data)
                ?? throw new InvalidDataException("The variable font directory is invalid.");
            ManagedFontVariationModel model = _variationModel.ForOpticalSize(reader, authoredSize);
            instance = TryLoad(_data, model, out string? error)
                ?? throw new InvalidDataException(error ?? "The optical font instance could not be loaded.");
            instance._trackingEvaluationScale = _trackingEvaluationScale;
            if (cache.Count >= 16) cache.Clear();
            cache.Add(size, instance);
            return instance;
        }
    }
}

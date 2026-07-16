using System;

namespace CodeGlyphX;

/// <summary>Controls GS1 Composite encoding.</summary>
public sealed class Gs1CompositeEncodingOptions {
    private Gs1CompositeComponent _component = Gs1CompositeComponent.Auto;

    /// <summary>Gets or sets the requested two-dimensional component.</summary>
    public Gs1CompositeComponent Component {
        get => _component;
        set {
            if (value is < Gs1CompositeComponent.Auto or > Gs1CompositeComponent.CcC) {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _component = value;
        }
    }
}

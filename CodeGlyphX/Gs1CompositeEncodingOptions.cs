using System;

namespace CodeGlyphX;

/// <summary>Controls GS1 Composite encoding.</summary>
public sealed class Gs1CompositeEncodingOptions {
    private Gs1CompositeComponent _component = Gs1CompositeComponent.Auto;
    private Gs1CompositeCarrier _carrier;
    private int _expandedColumns = 2;

    /// <summary>Gets or sets the linear carrier. CC-C is available only with GS1-128.</summary>
    public Gs1CompositeCarrier Carrier {
        get => _carrier;
        set {
            if (value < Gs1CompositeCarrier.Gs1_128 || value > Gs1CompositeCarrier.DataBarExpandedStacked)
                throw new ArgumentOutOfRangeException(nameof(value));
            _carrier = value;
        }
    }

    /// <summary>Gets or sets the finder/data-character blocks per Expanded Stacked row, from 2 through 10.</summary>
    /// <remarks>A Composite component requires at least two blocks in the first row.</remarks>
    public int DataBarExpandedColumns {
        get => _expandedColumns;
        set {
            if (value < 2 || value > 10) throw new ArgumentOutOfRangeException(nameof(value));
            _expandedColumns = value;
        }
    }

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

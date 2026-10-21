// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System.Collections.Generic;

namespace CodeGlyphX.Internal.Fonts;

/// <summary>Shared structural validation for TrueType composite glyph records.</summary>
internal static class ManagedOpenTypeCompositeGlyph {
    private const ushort HasScale = 0x0008;
    private const ushort HasXAndYScale = 0x0040;
    private const ushort HasTwoByTwo = 0x0080;
    private const ushort ScaledComponentOffset = 0x0800;
    private const ushort UnscaledComponentOffset = 0x1000;

    internal static bool HasConflictingTransformFlags(ushort flags) {
        int transformFlags = flags & (HasScale | HasXAndYScale | HasTwoByTwo);
        return transformFlags != 0 && (transformFlags & (transformFlags - 1)) != 0;
    }

    internal static bool HasConflictingOffsetFlags(ushort flags) =>
        (flags & ScaledComponentOffset) != 0 && (flags & UnscaledComponentOffset) != 0;

    internal static ManagedPoint ResolveXyOffset(
        ushort flags,
        double xx,
        double xy,
        double yx,
        double yy,
        double x,
        double y) {
        if (HasConflictingOffsetFlags(flags)) {
            throw new System.ArgumentException("Composite glyph offset flags conflict.", nameof(flags));
        }
        return (flags & ScaledComponentOffset) != 0
            ? new ManagedPoint((xx * x) + (xy * y), (yx * x) + (yy * y))
            : new ManagedPoint(x, y);
    }

    internal static bool TryResolvePointAttachment(
        IReadOnlyList<ManagedPoint> parentPoints,
        int parentPointIndex,
        IReadOnlyList<ManagedPoint> componentPoints,
        int componentPointIndex,
        out ManagedPoint translation) {
        if ((uint)parentPointIndex >= (uint)parentPoints.Count ||
            (uint)componentPointIndex >= (uint)componentPoints.Count) {
            translation = default;
            return false;
        }

        ManagedPoint parent = parentPoints[parentPointIndex];
        ManagedPoint component = componentPoints[componentPointIndex];
        translation = new ManagedPoint(parent.X - component.X, parent.Y - component.Y);
        return true;
    }

    internal static ManagedPoint ApplyComponentVariation(ManagedPoint attachmentTranslation, ManagedPoint variationTranslation) =>
        new ManagedPoint(
            attachmentTranslation.X + variationTranslation.X,
            attachmentTranslation.Y + variationTranslation.Y);
}

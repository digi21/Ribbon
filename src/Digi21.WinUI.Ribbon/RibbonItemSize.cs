namespace Digi21.WinUI.Ribbon;

/// <summary>The shape an item takes in its group.</summary>
/// <remarks>
/// The members are ordered from narrowest to widest, and the layout relies on that: squeezing a
/// group means walking every item down this list as far as the item allows.
/// </remarks>
public enum RibbonItemSize
{
    /// <summary>The icon on its own, with no label.</summary>
    Small,

    /// <summary>The icon and the label side by side, on one row.</summary>
    Normal,

    /// <summary>The icon above the label, over the whole height of the group.</summary>
    Large,
}

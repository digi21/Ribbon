namespace Digi21.WinUI.Ribbon;

/// <summary>The shapes an item is willing to take, which is what the layout is free to choose from.</summary>
/// <remarks>
/// An item that accepts nothing small enough for the width left keeps the smallest shape it does
/// accept: a hosted <c>NumberBox</c> declaring <see cref="Normal"/> alone stays that size however
/// hard the ribbon is squeezed, rather than being drawn as something it cannot be.
/// </remarks>
[Flags]
public enum RibbonItemSizes
{
    /// <summary>Nothing declared. The item is laid out as <see cref="RibbonItemSize.Normal"/>.</summary>
    None = 0,

    /// <summary>The item can be drawn as its icon alone.</summary>
    Small = 1,

    /// <summary>The item can be drawn as an icon and a label side by side.</summary>
    Normal = 2,

    /// <summary>The item can be drawn as an icon above its label, filling the height of the group.</summary>
    Large = 4,

    /// <summary>Every shape, which is what an ordinary button declares.</summary>
    All = Small | Normal | Large,
}

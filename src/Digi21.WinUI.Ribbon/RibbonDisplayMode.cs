namespace Digi21.WinUI.Ribbon;

/// <summary>How much of itself the ribbon draws.</summary>
/// <remarks>
/// This is not the same question as <see cref="Ribbon.IsMinimized"/> and the two are set
/// independently: a minimised ribbon is one the user has put away and can open a tab from, and a
/// simplified one is a ribbon that is there all the time and takes one row to be there in.
/// </remarks>
public enum RibbonDisplayMode
{
    /// <summary>Three rows to a group, as in Office. The shape everything else in this library assumes.</summary>
    Full,

    /// <summary>One row, with the group names off and every item drawn beside its label or as its icon alone.</summary>
    /// <remarks>
    /// <para>
    /// Office's simplified ribbon. What does not fit is not taken away: the group with the lowest
    /// priority folds into its button and opens from there, which is the same thing that happens to
    /// a full ribbon squeezed hard, and the only overflow this library has.
    /// </para>
    /// <para>
    /// A group holding something that cannot be drawn in one row - a stack of labelled controls, or
    /// an item that accepts no shape smaller than <see cref="RibbonItemSize.Large"/> - is drawn as
    /// its button whatever the width. Inside the flyout it has all the room it wants and is laid out
    /// the way a full ribbon would lay it out, so nothing is lost by asking for one row: the row
    /// stays a row.
    /// </para>
    /// </remarks>
    Simplified,
}

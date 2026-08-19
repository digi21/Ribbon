namespace Digi21.WinUI.Ribbon.Layout;

// One item, as the layout sees it: the shapes it accepts and how wide it is in each of them.
//
// The widths are measured by the control, because only a live element knows how wide its label
// renders at the current scale and font. Everything from here on is arithmetic over those numbers,
// which is the whole reason this type exists: it is what lets the layout be decided - and tested -
// without a window.
//
// A separator has no shapes and no label. It is given a column of its own and its NormalWidth.
internal readonly record struct RibbonItemMetrics(
    RibbonItemSizes AllowedSizes,
    double SmallWidth,
    double NormalWidth,
    double LargeWidth,
    bool IsSeparator = false,
    int Rows = 1)
{
    // A separator, and an item drawn Large, take the whole height of the group and therefore a
    // column to themselves. Everything else takes the rows it needs - one for a button or a single
    // control, more for something an application built out of several of them - which is what makes
    // six small buttons a grid rather than a run, and a stack of three boxes a column rather than a
    // ribbon three times too tall.
    internal int RowsAt(RibbonItemSize size, int maxRows) =>
        IsSeparator || size == RibbonItemSize.Large ? maxRows : Math.Clamp(Rows, 1, maxRows);

    internal double WidthAt(RibbonItemSize size) => size switch
    {
        RibbonItemSize.Small => SmallWidth,
        RibbonItemSize.Normal => NormalWidth,
        _ => LargeWidth,
    };

    // The largest shape this item accepts that is no bigger than the cap.
    //
    // An item that accepts nothing that small keeps the smallest it does accept. That is not a
    // rounding error: it is what stops the layout from believing it has recovered width that it has
    // not, which would otherwise leave the group overflowing with no step left to take.
    internal RibbonItemSize SizeUnder(RibbonItemSize cap)
    {
        if (IsSeparator || AllowedSizes == RibbonItemSizes.None)
        {
            return RibbonItemSize.Normal;
        }

        for (int level = (int)cap; level >= (int)RibbonItemSize.Small; level--)
        {
            if (Allows((RibbonItemSize)level))
            {
                return (RibbonItemSize)level;
            }
        }

        for (int level = (int)RibbonItemSize.Small; level <= (int)RibbonItemSize.Large; level++)
        {
            if (Allows((RibbonItemSize)level))
            {
                return (RibbonItemSize)level;
            }
        }

        return RibbonItemSize.Normal;
    }

    private bool Allows(RibbonItemSize size) => (AllowedSizes & Flag(size)) != 0;

    private static RibbonItemSizes Flag(RibbonItemSize size) => size switch
    {
        RibbonItemSize.Small => RibbonItemSizes.Small,
        RibbonItemSize.Normal => RibbonItemSizes.Normal,
        _ => RibbonItemSizes.Large,
    };
}

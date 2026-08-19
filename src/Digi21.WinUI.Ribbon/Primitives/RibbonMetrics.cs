namespace Digi21.WinUI.Ribbon.Primitives;

// The sizes the layout imposes from code rather than through a template, because the panels have to
// agree with each other about them to the pixel: the solver decides what will fit from these
// numbers and the panels then place elements by them, and a disagreement would have a group adopt an
// arrangement the solver did not choose.
//
// They will move into the theme dictionary as keys once there is a theme dictionary; the read will
// go through a helper with these as its fallbacks, so that an application can space a ribbon out
// without retemplating it.
internal static class RibbonMetrics
{
    // Three rows to a column, as in Office. The row height is what makes a Large item exactly as
    // tall as three Small ones stacked, which is the whole reason the two mix in one group.
    internal const int MaxRows = 3;

    internal const double RowHeight = 24;

    internal const double SmallIconSize = 16;

    internal const double LargeIconSize = 32;

    internal const double IconLabelGap = 6;

    internal const double ItemPadding = 4;

    internal const double GroupPadding = 6;

    // The strip under a group holding its name.
    internal const double GroupLabelHeight = 16;

    internal const double SeparatorWidth = 9;

    // The mark on an item that opens something.
    internal const double ChevronSize = 12;

    // The button in the corner of a group that opens the rest of what it does.
    internal const double LauncherSize = 16;
}

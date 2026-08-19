namespace Digi21.WinUI.Ribbon.Layout;

// What the layout decided for one group.
//
// ItemSizes are the shapes the items take where they are actually shown: inline while the group is
// expanded, and inside the flyout once it has collapsed - which is why a collapsed group reports
// the shapes it would have at its widest. The flyout has all the room it wants, so there is nothing
// there to squeeze.
//
// ShowsCollapsedLabel only means anything when IsCollapsed is true. It is false in the one state
// left when every group has collapsed and the strip is still too narrow: the buttons keep their
// icons and drop their text. There is no state after that in which a group is taken out of the
// strip - the ribbon clips instead, and Overflows on the layout says so.
internal readonly record struct RibbonGroupArrangement(
    bool IsCollapsed,
    bool ShowsCollapsedLabel,
    IReadOnlyList<RibbonItemSize> ItemSizes,
    double Width);

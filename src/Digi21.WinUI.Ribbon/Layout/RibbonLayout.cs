namespace Digi21.WinUI.Ribbon.Layout;

// What the layout decided for a whole strip of groups, in the order they were given.
//
// Overflows says the arrangement is wider than the room it was given even after the last resort,
// which is the honest answer when a window is narrower than the icons of every group put together.
// The control clips; it does not drop a group, because a command the user cannot reach is worse
// than one drawn off the edge.
internal readonly record struct RibbonLayout(
    IReadOnlyList<RibbonGroupArrangement> Groups,
    double Width,
    bool Overflows);

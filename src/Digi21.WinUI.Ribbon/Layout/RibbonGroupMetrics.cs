namespace Digi21.WinUI.Ribbon.Layout;

// One group, as the layout sees it.
//
// Priority decides which group gives way first, and the lowest one goes first: a group nobody
// declared a priority for is a group the application is happy to lose room in. Ties are broken from
// the right, as in Office.
//
// LabelWidth is a floor, not an addition: a group is never narrower than its own name, as in Office,
// where the name under a group is always readable however hard the ribbon is squeezed. Leaving it
// out did not merely look worse - it made the layout wrong, because the group already rendered that
// wide and the layout was predicting something narrower.
//
// The two collapsed widths are the button the group becomes when it no longer fits - with its name
// and, as a last resort, without it. Like the label width they are measured rather than guessed,
// because the text is the application's, in the application's language.
internal readonly record struct RibbonGroupMetrics(
    int Priority,
    double ChromeWidth,
    double LabelWidth,
    double CollapsedWidth,
    double CollapsedIconWidth,
    IReadOnlyList<RibbonItemMetrics> Items);

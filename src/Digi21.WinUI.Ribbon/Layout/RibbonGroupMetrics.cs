namespace Digi21.WinUI.Ribbon.Layout;

// One group, as the layout sees it.
//
// Priority decides which group gives way first, and the lowest one goes first: a group nobody
// declared a priority for is a group the application is happy to lose room in. Ties are broken from
// the right, as in Office.
//
// The two collapsed widths are the button the group becomes when it no longer fits - with its label
// and, as a last resort, without it. They are measured rather than guessed because the label is the
// application's text, in the application's language.
internal readonly record struct RibbonGroupMetrics(
    int Priority,
    double ChromeWidth,
    double CollapsedWidth,
    double CollapsedIconWidth,
    IReadOnlyList<RibbonItemMetrics> Items);

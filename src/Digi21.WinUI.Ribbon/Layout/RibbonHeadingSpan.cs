namespace Digi21.WinUI.Ribbon.Layout;

// Which tabs a contextual heading is drawn over.
//
// The heading is the coloured band above a set of contextual tabs, and what it has to line up with
// is the tabs of its group that are on the strip right now - a set that changes every time one of
// them is switched on or off, and that is empty most of the time, because a group whose tabs have
// all gone is a heading with nothing under it.
//
// Here rather than in the panel, and beside the other rules in this folder, because it is arithmetic
// over a list and it decides where something is drawn. The panel does the pixels.
//
// One rule: the band runs from the first tab of the group to the last, counted among the tabs that
// are on the strip. Two tabs of one group side by side are one band over both. A tab that does not
// belong to the group, declared between two that do, is drawn under the band as well - which is the
// honest picture of what the application declared, and Office does not offer the arrangement at all.
internal static class RibbonHeadingSpan
{
    // The first tab of the group and how many tabs the band covers, or nothing at all when the group
    // has no tab on the strip.
    internal static (int First, int Count) Of(IReadOnlyList<object?> groups, object group)
    {
        int first = -1;
        int last = -1;

        for (int i = 0; i < groups.Count; i++)
        {
            if (!ReferenceEquals(groups[i], group))
            {
                continue;
            }

            if (first < 0)
            {
                first = i;
            }

            last = i;
        }

        return first < 0 ? (0, 0) : (first, last - first + 1);
    }
}

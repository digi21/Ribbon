namespace Digi21.WinUI.Ribbon.Layout;

// Decides what a strip of groups looks like at a given width: which shape every item takes, which
// groups have collapsed into a button, and whether even that fits.
//
// It is a pure function of the width it is handed, and that is the point rather than a convenience.
// A layout that decided from the room it had *left over* would feed its own output back into its
// input - collapsing a group frees width, the freed width invites the group back, and the ribbon
// oscillates between two arrangements at the width where one turns into the other. Solving once,
// against a width nobody has touched, makes that impossible by construction rather than by tuning a
// threshold.
//
// The search is a walk along one fixed sequence of states. Which group gives way next is decided
// from the state alone - the caps, what has collapsed, the priorities - and never from the width
// available, so the sequence is the same at every width; the width only chooses how far along it to
// stop, at the first state that fits.
//
// Everything the ribbon promises falls out of those two sentences. Narrowing a window and widening
// it back lands exactly where it started, because the stopping point is a function of the width and
// of nothing else, and there is no width at which two arrangements are both admissible, so there is
// nothing to flicker between. And nothing grows as the window narrows: a narrower window stops at
// the same state or a later one, and every state it walked past was, by the test that made it walk
// past, wider than the width it had - so the state it lands on is narrower than the one a wider
// window stopped at. That holds however the sequence itself is shaped, which is what makes it worth
// leaning on. Keep the decisions above blind to `available` and it keeps holding.
internal static class RibbonLayoutSolver
{
    // Office stacks small items three to a column. Anything taller reads as a list rather than a
    // ribbon, and anything shorter wastes the height the tab strip already cost.
    internal const int MaxRows = 3;

    internal const double ColumnSpacing = 4;

    internal const double GroupSpacing = 8;

    internal static RibbonLayout Solve(
        IReadOnlyList<RibbonGroupMetrics> groups,
        double available,
        int maxRows = MaxRows,
        double columnSpacing = ColumnSpacing,
        double groupSpacing = GroupSpacing)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (groups.Count == 0)
        {
            return new RibbonLayout([], 0, false);
        }

        // Every group starts as wide as it wants to be, and the search only ever takes width away.
        var caps = new RibbonItemSize[groups.Count];
        Array.Fill(caps, RibbonItemSize.Large);

        var collapsed = new bool[groups.Count];
        bool showsCollapsedLabel = true;

        while (Total() > available)
        {
            // The lowest priority gives way first, and among equals the rightmost does - which is
            // where the eye expects a ribbon to lose detail.
            int index = LowestPriority(i => !collapsed[i] && caps[i] > RibbonItemSize.Small);
            if (index >= 0)
            {
                caps[index]--;
                continue;
            }

            // Nothing can be made any smaller, so a group has to fold into its button. Only one
            // that is actually narrower folded is worth folding: a group holding a single small
            // button is wider as a button with a label than it is as itself, and collapsing it
            // would make the overflow worse while hiding a command.
            index = LowestPriority(i => !collapsed[i] && groups[i].CollapsedWidth < Expanded(i));
            if (index >= 0)
            {
                collapsed[index] = true;
                continue;
            }

            // The last resort. After this there is no step left: the strip keeps every group, in
            // its button, and the ribbon clips.
            if (showsCollapsedLabel && Array.IndexOf(collapsed, true) >= 0)
            {
                showsCollapsedLabel = false;
                continue;
            }

            break;
        }

        var arrangements = new RibbonGroupArrangement[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            // A collapsed group is shown in a flyout, which has all the width it wants, so its
            // items go back to the shapes they would have had before anything was taken away.
            RibbonItemSize cap = collapsed[i] ? RibbonItemSize.Large : caps[i];

            arrangements[i] = new RibbonGroupArrangement(
                collapsed[i],
                showsCollapsedLabel,
                SizesUnder(groups[i], cap),
                Width(i));
        }

        double width = Total();
        return new RibbonLayout(arrangements, width, width > available);

        double Expanded(int index) => Measure(groups[index], caps[index], maxRows, columnSpacing);

        double Width(int index) => collapsed[index]
            ? showsCollapsedLabel ? groups[index].CollapsedWidth : groups[index].CollapsedIconWidth
            : Expanded(index);

        double Total()
        {
            double total = (groups.Count - 1) * groupSpacing;
            for (int i = 0; i < groups.Count; i++)
            {
                total += Width(i);
            }

            return total;
        }

        int LowestPriority(Func<int, bool> candidate)
        {
            int best = -1;
            for (int i = 0; i < groups.Count; i++)
            {
                // Walking left to right and taking ties as they come is what makes the rightmost of
                // several equals the one that gives way.
                if (candidate(i) && (best < 0 || groups[i].Priority <= groups[best].Priority))
                {
                    best = i;
                }
            }

            return best;
        }
    }

    // The shape each item of a group takes under a cap, in the order the items were given.
    internal static RibbonItemSize[] SizesUnder(in RibbonGroupMetrics group, RibbonItemSize cap)
    {
        var sizes = new RibbonItemSize[group.Items.Count];
        for (int i = 0; i < group.Items.Count; i++)
        {
            sizes[i] = group.Items[i].SizeUnder(cap);
        }

        return sizes;
    }

    // How wide a group is with every item at the shape the cap leaves it, packed into columns of at
    // most maxRows rows. A column is as wide as the widest item in it, which is why a single long
    // label in a column of three widens all three.
    internal static double Measure(in RibbonGroupMetrics group, RibbonItemSize cap, int maxRows = MaxRows, double columnSpacing = ColumnSpacing)
    {
        double items = 0;
        double column = 0;
        int rows = 0;
        int columns = 0;

        foreach (RibbonItemMetrics item in group.Items)
        {
            RibbonItemSize size = item.SizeUnder(cap);
            int itemRows = item.RowsAt(size, maxRows);

            if (rows > 0 && rows + itemRows > maxRows)
            {
                items += column;
                columns++;
                column = 0;
                rows = 0;
            }

            column = Math.Max(column, item.WidthAt(size));
            rows += itemRows;
        }

        if (rows > 0)
        {
            items += column;
            columns++;
        }

        return group.ChromeWidth + items + (columns > 1 ? (columns - 1) * columnSpacing : 0);
    }
}

namespace Digi21.WinUI.Ribbon.Layout;

// Decides what a strip of groups looks like at a given width: which shape every item takes, which
// groups have folded into a button, and whether even that fits.
//
// It is written as two halves that cannot see each other, because the property the whole ribbon
// rests on is a property of that separation. States walks the arrangements the groups can take, in
// order, and is not given the width available - it could not consult it if it wanted to. Solve
// takes the first arrangement that fits. A layout that decided from the room it had *left over*
// would feed its own output back into its input - folding a group frees width, the freed width
// invites the group back, and the ribbon oscillates between two arrangements at the width where one
// turns into the other - and this shape makes writing that impossible rather than merely wrong.
//
// What follows from the separation:
//
//   Every step only ever degrades. Lowering a cap cannot raise any item's shape, and folding a
//   group takes its items off the strip altogether, so the sequence is monotone per item by its
//   index in the sequence - even though the total width need not be.
//
//   Solve returns the first state that fits. For W' < W, every state that did not fit in W' did not
//   fit in W either, so the index returned for W' is at least the one returned for W. A later index
//   plus monotonicity per item means nothing grows as the window narrows. The total width never
//   enters the argument.
//
//   The stopping point is a function of the width and of nothing else, so narrowing a window and
//   widening it back lands exactly where it started, and no width admits two arrangements, so there
//   is nothing to flicker between.
//
// Neither half assumes anything about the numbers it is handed. A group only folds if folding is
// narrower than leaving it alone, and the collapsed buttons only drop their labels if that is
// narrower than keeping them - a group of two icons is wider as a button carrying the group's name
// than it is as itself, and folding it would both widen the strip and hide two commands.
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
        double groupSpacing = GroupSpacing,
        RibbonItemSize largest = RibbonItemSize.Large)
    {
        ArgumentNullException.ThrowIfNull(groups);

        RibbonGroupArrangement[] terminal = [];

        foreach (RibbonGroupArrangement[] state in States(groups, maxRows, columnSpacing, largest))
        {
            terminal = state;

            double fitted = Width(state, groupSpacing);
            if (fitted <= available)
            {
                return new RibbonLayout(state, fitted, false);
            }
        }

        // The terminal state: every group folded, the buttons down to their icons, and still too
        // much. There is nothing after it. The strip keeps every group and the ribbon clips, because
        // a command drawn off the edge can be reached by widening the window and one that has been
        // taken out of the strip cannot be reached at all.
        return new RibbonLayout(terminal, Width(terminal, groupSpacing), true);
    }

    // The arrangements a strip can take, widest first, each one a degradation of the one before.
    //
    // The width available is deliberately not a parameter. Which group gives way next is read from
    // the state alone - the caps, what has folded, the priorities - so the sequence is the same
    // whatever room there is, and Solve's choice of where to stop is the only thing a width decides.
    internal static IEnumerable<RibbonGroupArrangement[]> States(
        IReadOnlyList<RibbonGroupMetrics> groups,
        int maxRows = MaxRows,
        double columnSpacing = ColumnSpacing,
        RibbonItemSize largest = RibbonItemSize.Large)
    {
        // Every group starts as wide as it wants to be, and the walk only ever takes width away. The
        // widest shape is a parameter because a ribbon of one row has no room for the tallest of
        // them: there, the walk starts one shape down rather than spending its first states taking
        // away something that was never on offer.
        var caps = new RibbonItemSize[groups.Count];
        Array.Fill(caps, largest);

        // A group that cannot be drawn in the rows there are starts folded and stays folded. It is
        // the one thing here that is not a degradation: no width brings it back, because no width
        // was ever the reason.
        var collapsed = new bool[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            collapsed[i] = groups[i].MustCollapse;
        }

        var labelled = new bool[groups.Count];
        Array.Fill(labelled, true);

        yield return Snapshot();

        while (true)
        {
            // The lowest priority gives way first, and among equals the rightmost does - which is
            // where the eye expects a ribbon to lose detail.
            int index = LowestPriority(i => !collapsed[i] && caps[i] > RibbonItemSize.Small);
            if (index >= 0)
            {
                caps[index]--;
                yield return Snapshot();
                continue;
            }

            // Nothing can be made any smaller, so a group has to fold into its button - but only one
            // that is actually narrower folded is worth folding.
            index = LowestPriority(i => !collapsed[i] && groups[i].CollapsedWidth < GroupWidth(i));
            if (index >= 0)
            {
                collapsed[index] = true;
                yield return Snapshot();
                continue;
            }

            // The last resort: a folded button drops its label. One button at a time and in the
            // same priority order as everything else, so a strip only loses the names it has to,
            // and only where losing one buys room - a button whose icon is no narrower than the
            // button carrying the group's name keeps the name, which is more use for the width.
            index = LowestPriority(i => collapsed[i] && labelled[i] && groups[i].CollapsedIconWidth < groups[i].CollapsedWidth);
            if (index >= 0)
            {
                labelled[index] = false;
                yield return Snapshot();
                continue;
            }

            yield break;
        }

        RibbonGroupArrangement[] Snapshot()
        {
            var state = new RibbonGroupArrangement[groups.Count];
            for (int i = 0; i < groups.Count; i++)
            {
                // A folded group is shown in a flyout, which has all the width it wants, so its
                // items go back to the shapes they would have had before anything was taken away.
                RibbonItemSize cap = collapsed[i] ? RibbonItemSize.Large : caps[i];

                state[i] = new RibbonGroupArrangement(
                    collapsed[i],
                    labelled[i],
                    SizesUnder(groups[i], cap),
                    GroupWidth(i));
            }

            return state;
        }

        double GroupWidth(int index) => collapsed[index]
            ? labelled[index] ? groups[index].CollapsedWidth : groups[index].CollapsedIconWidth
            : Measure(groups[index], caps[index], maxRows, columnSpacing);

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

    // What one arrangement of the whole strip takes across.
    internal static double Width(IReadOnlyList<RibbonGroupArrangement> groups, double groupSpacing = GroupSpacing)
    {
        if (groups.Count == 0)
        {
            return 0;
        }

        double total = (groups.Count - 1) * groupSpacing;
        for (int i = 0; i < groups.Count; i++)
        {
            total += groups[i].Width;
        }

        return total;
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
    // label in a column of three widens all three, and no group is ever narrower than its own name.
    internal static double Measure(in RibbonGroupMetrics group, RibbonItemSize cap, int maxRows = MaxRows, double columnSpacing = ColumnSpacing)
    {
        var rows = new int[group.Items.Count];
        var widths = new double[group.Items.Count];

        for (int i = 0; i < group.Items.Count; i++)
        {
            RibbonItemSize size = group.Items[i].SizeUnder(cap);
            rows[i] = group.Items[i].RowsAt(size, maxRows);
            widths[i] = group.Items[i].WidthAt(size);
        }

        int[] placement = RibbonColumnPacker.Pack(rows, maxRows, out int columns);

        var columnWidths = new double[columns];
        for (int i = 0; i < placement.Length; i++)
        {
            columnWidths[placement[i]] = Math.Max(columnWidths[placement[i]], widths[i]);
        }

        double total = 0;
        foreach (double width in columnWidths)
        {
            total += width;
        }

        total += columns > 1 ? (columns - 1) * columnSpacing : 0;

        // The floor. It is the same at every cap, so squeezing a group still never widens it.
        return group.ChromeWidth + Math.Max(total, group.LabelWidth);
    }
}

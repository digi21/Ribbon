namespace Digi21.WinUI.Ribbon.Layout;

// Packs a group's items into columns of at most maxRows rows, which is what makes six small buttons
// a grid rather than a run.
//
// One implementation, used twice: the solver measures a group with it while deciding what will fit,
// and the panel places the elements with it while arranging them. Two packings would be two answers
// to the same question, and the layout would predict an arrangement the group does not adopt.
internal static class RibbonColumnPacker
{
    // The column each item lands in, in the order the items were given. An item asking for the full
    // height of the group - one drawn Large, or a separator - takes a column to itself, and the
    // column that was being filled is closed behind it.
    internal static int[] Pack(IReadOnlyList<int> rows, int maxRows, out int columns)
    {
        var placement = new int[rows.Count];

        columns = 0;
        int used = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            if (used > 0 && used + rows[i] > maxRows)
            {
                columns++;
                used = 0;
            }

            placement[i] = columns;
            used += rows[i];
        }

        if (used > 0)
        {
            columns++;
        }

        return placement;
    }
}

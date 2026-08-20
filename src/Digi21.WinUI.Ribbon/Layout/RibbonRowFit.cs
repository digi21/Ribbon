namespace Digi21.WinUI.Ribbon.Layout;

// How tall a row is, and how many of them each item needs.
//
// One implementation used twice, for the same reason the column packing is: the layout decides what
// fits from these numbers and the panel places the elements by them, and two answers to one question
// is how a group comes to adopt an arrangement the layout did not choose.
//
// The rule it exists to get right: an item taller than a row takes several rows. The first version of
// this library said instead that a row is as tall as the tallest item in one, which is true while
// every item is a button or a single control and wrong the moment an application puts a stack of
// three labelled boxes in a group - one item, a hundred pixels tall, which made all three rows a
// hundred pixels tall and the ribbon three times the height it should be.
//
// The second version took the rows away from that item without giving it the height back: a stack of
// three combo boxes spanned three rows of twenty-four and was drawn in seventy-two pixels, so the
// group cut the top off the first box and the bottom off the last. The rows an item spans have to be
// tall enough between them to hold it, which is the second half of the same rule and the reason
// RowHeight looks at the tall items too - not to let one of them set the height of a row, but to
// stop the rows it spans from adding up to less than it is.
internal static class RibbonRowFit
{
    // Above this, an item is not one row of a ribbon. A standard WinUI control is thirty-two pixels
    // and one with a name beside it thirty-three, so the bound sits just above those - which is the
    // distinction being drawn, and the only place a number can draw it.
    //
    // It sat at forty-four to begin with, and forty-four lets a ToggleSwitch through: forty pixels
    // tall, one of them anywhere in a tab made every row of every group forty, and the gallery's View
    // tab was a hundred and forty-eight pixels of ribbon for one switch with eighty of nothing under
    // it. A control that does not fit a row is not a tall row; it is two rows, which is what Office
    // does with one and what this bound now says.
    internal const double SingleRowCeiling = 36;

    // The height of a row, given what has to sit in one and how many rows there are to spread it
    // over. The floor is the ribbon's own item height; a group holding taller controls gets taller
    // rows; and a group holding something taller than a row gets rows that between them are as tall
    // as that item - a third of a hundred-pixel stack per row rather than the whole of it.
    //
    // A ribbon of one row is asked the same question and gives a bigger answer, which is why a group
    // holding something that does not fit a row is folded in that mode rather than laid out here.
    internal static double RowHeight(IReadOnlyList<double> heights, int maxRows)
    {
        double row = Primitives.RibbonMetrics.RowHeight;

        foreach (double height in heights)
        {
            if (height <= SingleRowCeiling)
            {
                row = Math.Max(row, height);
            }
        }

        // Which rows a spanning item takes is settled against the height the single-row items asked
        // for, and only then is the row raised to fit it. Reading the running total instead would
        // have the answer depend on the order the items came in, and the group would be a different
        // height for the same items listed the other way round.
        double single = row;

        foreach (double height in heights)
        {
            if (height > SingleRowCeiling)
            {
                row = Math.Max(row, height / Rows(height, single, maxRows));
            }
        }

        return row;
    }

    // How many rows one item needs, never more than a group has.
    internal static int Rows(double height, double rowHeight, int maxRows) =>
        height <= rowHeight
            ? 1
            : Math.Min(maxRows, (int)Math.Ceiling(height / rowHeight));
}

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
internal static class RibbonRowFit
{
    // Above this, an item is not one row of a ribbon. A standard WinUI control is thirty-two pixels
    // and one with a name beside it thirty-three, so the bound sits above those and well below two of
    // them stacked - which is the distinction being drawn, and the only place a number can draw it.
    internal const double SingleRowCeiling = 44;

    // The height of a row, given what has to sit in one. The floor is the ribbon's own item height;
    // a group holding taller controls gets taller rows, and a group holding something taller than a
    // row is not consulted, because that item will be spanning rows rather than setting their height.
    internal static double RowHeight(IReadOnlyList<double> heights)
    {
        double row = Primitives.RibbonMetrics.RowHeight;

        foreach (double height in heights)
        {
            if (height <= SingleRowCeiling)
            {
                row = Math.Max(row, height);
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

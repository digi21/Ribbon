using Digi21.WinUI.Ribbon.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>The items of one group, packed into columns of at most three rows.</summary>
/// <remarks>
/// <para>
/// It places the elements with <c>RibbonColumnPacker</c>, which is the same packing the layout used
/// to decide what would fit. Two packings would be two answers to the same question, and the group
/// would adopt an arrangement the layout did not choose.
/// </para>
/// <para>
/// This panel, with the items in it, is the thing that moves when a group folds: it is taken out of
/// the group and put into the flyout of the group's button, and taken back when there is room again.
/// Moved, never rebuilt - which is what makes a reference an application kept to a control it put
/// here go on working.
/// </para>
/// </remarks>
public sealed partial class RibbonItemsPanel : Panel
{
    private int[] placement = [];
    private int[] rows = [];
    private double[] columnWidths = [];
    private double rowHeight = RibbonMetrics.RowHeight;
    private int maxRows = RibbonMetrics.MaxRows;

    /// <summary>Gets or sets how many rows a column of this group has. Written by the group.</summary>
    /// <remarks>
    /// Three in a full ribbon and one in a simplified one - and three again while the panel is
    /// inside a folded group's flyout, which has all the room it wants and lays the group out the
    /// way a full ribbon would.
    /// </remarks>
    internal int Rows
    {
        get => maxRows;

        set
        {
            if (maxRows == value)
            {
                return;
            }

            maxRows = value;
            InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        int count = Children.Count;

        if (count == 0)
        {
            placement = [];
            rows = [];
            columnWidths = [];
            rowHeight = RibbonMetrics.RowHeight;
            return new Size(0, maxRows * RibbonMetrics.RowHeight);
        }

        rows = new int[count];
        var widths = new double[count];

        // A row is as tall as the tallest thing that has to sit in one; an item taller than a row
        // takes several rows rather than making every row that tall; and the rows it takes are
        // between them as tall as it is, so that nothing is drawn shorter than it asked to be. All
        // three are decided by RibbonRowFit, which is the same code the layout used to work out what
        // would fit.
        var heights = new double[count];

        for (int i = 0; i < count; i++)
        {
            UIElement child = Children[i];

            // Measured against no ceiling at all, in height as well as in width. Offering the three
            // rows the group has would have a taller item report exactly those three rows back -
            // WinUI never says an element wants more than it was offered - and the group would then
            // be built to the height of the answer rather than the height of the item, which is how
            // a stack of three combo boxes came to be drawn in seventy-two pixels with its first and
            // last box cut. What the rows are worth is settled below, once the true heights are in.
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            widths[i] = child.DesiredSize.Width;
            heights[i] = child.DesiredSize.Height;
        }

        rowHeight = RibbonRowFit.RowHeight(heights, maxRows);

        for (int i = 0; i < count; i++)
        {
            rows[i] = Spans(Children[i])
                ? maxRows
                : RibbonRowFit.Rows(heights[i], rowHeight, maxRows);
        }

        double height = maxRows * rowHeight;

        placement = RibbonColumnPacker.Pack(rows, maxRows, out int columns);

        columnWidths = new double[columns];
        for (int i = 0; i < count; i++)
        {
            columnWidths[placement[i]] = Math.Max(columnWidths[placement[i]], widths[i]);
        }

        double total = 0;
        foreach (double width in columnWidths)
        {
            total += width;
        }

        return new Size(total + ((columns - 1) * RibbonLayoutSolver.ColumnSpacing), height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
        {
            return finalSize;
        }

        var lefts = new double[columnWidths.Length];
        double x = 0;
        for (int column = 0; column < columnWidths.Length; column++)
        {
            lefts[column] = x;
            x += columnWidths[column] + RibbonLayoutSolver.ColumnSpacing;
        }

        var used = new int[columnWidths.Length];

        // Stretched to the height the strip settled on, so that the rows of every group line up
        // however tall the tallest item of any one of them turned out to be.
        double row = Math.Max(rowHeight, finalSize.Height / maxRows);

        for (int i = 0; i < Children.Count; i++)
        {
            int column = placement[i];

            Children[i].Arrange(new Rect(lefts[column], used[column] * row, columnWidths[column], rows[i] * row));
            used[column] += rows[i];
        }

        return finalSize;
    }

    // A separator, and an item drawn Large, take the whole height of the group whatever they measure.
    private static bool Spans(UIElement child) =>
        child is RibbonSeparator || Ribbon.GetSize(child) == RibbonItemSize.Large;
}

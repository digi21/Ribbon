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

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = RibbonMetrics.MaxRows * RibbonMetrics.RowHeight;
        int count = Children.Count;

        if (count == 0)
        {
            placement = [];
            rows = [];
            columnWidths = [];
            return new Size(0, height);
        }

        rows = new int[count];
        var widths = new double[count];

        for (int i = 0; i < count; i++)
        {
            UIElement child = Children[i];
            child.Measure(new Size(double.PositiveInfinity, height));
            rows[i] = RowsOf(child);
            widths[i] = child.DesiredSize.Width;
        }

        placement = RibbonColumnPacker.Pack(rows, RibbonMetrics.MaxRows, out int columns);

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

        for (int i = 0; i < Children.Count; i++)
        {
            int column = placement[i];
            double top = used[column] * RibbonMetrics.RowHeight;
            double itemHeight = rows[i] * RibbonMetrics.RowHeight;

            Children[i].Arrange(new Rect(lefts[column], top, columnWidths[column], itemHeight));
            used[column] += rows[i];
        }

        return finalSize;
    }

    // A separator, and an item drawn Large, take the whole height of the group and so a column of
    // their own. Everything else takes one row.
    private static int RowsOf(UIElement child) =>
        child is RibbonSeparator || Ribbon.GetSize(child) == RibbonItemSize.Large
            ? RibbonMetrics.MaxRows
            : 1;
}

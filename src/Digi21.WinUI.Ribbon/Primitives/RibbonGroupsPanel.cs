using Digi21.WinUI.Ribbon.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>The groups of one tab, laid out across the width there is.</summary>
/// <remarks>
/// This is where the measuring meets the deciding. It asks each group how wide its items would be in
/// each shape, hands those numbers and the width available to the layout, and applies what comes
/// back. It reads the width once, at the top of the pass, and never looks at what is left over
/// afterwards - a layout that did would invite back the group whose folding had just freed the room,
/// and the ribbon would flicker between two arrangements at the width where one becomes the other.
/// </remarks>
public sealed partial class RibbonGroupsPanel : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        List<RibbonGroup> groups = [.. Children.OfType<RibbonGroup>()];

        if (groups.Count == 0)
        {
            return new Size(0, 0);
        }

        var metrics = new RibbonGroupMetrics[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            metrics[i] = groups[i].CollectMetrics();
        }

        // Every group of a tab is laid out in the same number of rows, so the first of them speaks
        // for the strip. One row cannot hold an item drawn with its icon above its label, so the
        // walk starts a shape lower rather than spending its first states offering one.
        int rows = groups[0].Rows;
        RibbonItemSize largest = rows > 1 ? RibbonItemSize.Large : RibbonItemSize.Normal;

        RibbonLayout layout = RibbonLayoutSolver.Solve(
            metrics,
            availableSize.Width,
            rows,
            largest: largest);

        double width = (groups.Count - 1) * RibbonLayoutSolver.GroupSpacing;
        double height = 0;

        for (int i = 0; i < groups.Count; i++)
        {
            groups[i].Apply(layout.Groups[i]);
            groups[i].Measure(new Size(double.PositiveInfinity, availableSize.Height));

            width += groups[i].DesiredSize.Width;
            height = Math.Max(height, groups[i].DesiredSize.Height);
        }

        return new Size(width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;

        foreach (UIElement child in Children)
        {
            double width = child.DesiredSize.Width;
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width + RibbonLayoutSolver.GroupSpacing;
        }

        return finalSize;
    }
}

using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// How wide one group is: the packing of its items into columns, and the shape each item takes under
// a cap. The strip-wide decisions are in RibbonLayoutSolverTests.
public class RibbonGroupPackingTests
{
    private const double Spacing = RibbonLayoutSolver.ColumnSpacing;

    [Fact]
    public void ThreeSmallItems_ShareOneColumn()
    {
        double width = Measure(RibbonItemSize.Small, Small(40), Small(40), Small(40));

        Assert.Equal(40, width);
    }

    [Fact]
    public void AFourthSmallItem_StartsANewColumn()
    {
        double width = Measure(RibbonItemSize.Small, Small(40), Small(40), Small(40), Small(40));

        Assert.Equal(40 + Spacing + 40, width);
    }

    [Fact]
    public void AColumnIsAsWideAsItsWidestItem()
    {
        // One long label in a column of three widens all three, which is why Office keeps the long
        // ones out of shared columns.
        double width = Measure(RibbonItemSize.Small, Small(40), Small(96), Small(40));

        Assert.Equal(96, width);
    }

    [Fact]
    public void ALargeItem_TakesAColumnOfItsOwn()
    {
        double width = Measure(RibbonItemSize.Large, Item(RibbonItemSizes.All, 40, 90, 72), Small(40), Small(40));

        // The Large item fills its column, so the two small ones start the next.
        Assert.Equal(72 + Spacing + 40, width);
    }

    [Fact]
    public void ASeparator_TakesAColumnOfItsOwn()
    {
        double width = Measure(RibbonItemSize.Small, Small(40), Separator(9), Small(40));

        Assert.Equal(40 + Spacing + 9 + Spacing + 40, width);
    }

    [Fact]
    public void ChromeIsAddedOnceAndSpacingOnlyBetweenColumns()
    {
        var group = new RibbonGroupMetrics(0, ChromeWidth: 18, 0, 0, [Small(40), Small(40), Small(40)]);

        Assert.Equal(18 + 40, RibbonLayoutSolver.Measure(group, RibbonItemSize.Small));
    }

    [Fact]
    public void AGroupWithNoItems_IsJustItsChrome()
    {
        var group = new RibbonGroupMetrics(0, ChromeWidth: 18, 0, 0, []);

        Assert.Equal(18, RibbonLayoutSolver.Measure(group, RibbonItemSize.Large));
    }

    [Fact]
    public void AnItemTakesTheLargestShapeItAcceptsUnderTheCap()
    {
        RibbonItemMetrics item = Item(RibbonItemSizes.All, 40, 90, 72);

        Assert.Equal(RibbonItemSize.Large, item.SizeUnder(RibbonItemSize.Large));
        Assert.Equal(RibbonItemSize.Normal, item.SizeUnder(RibbonItemSize.Normal));
        Assert.Equal(RibbonItemSize.Small, item.SizeUnder(RibbonItemSize.Small));
    }

    [Fact]
    public void AnItemSkipsTheShapesItDoesNotAccept()
    {
        // Large and Small only: under a Normal cap it has to be the Small one.
        RibbonItemMetrics item = Item(RibbonItemSizes.Large | RibbonItemSizes.Small, 40, 90, 72);

        Assert.Equal(RibbonItemSize.Small, item.SizeUnder(RibbonItemSize.Normal));
    }

    [Fact]
    public void AnItemAcceptingNothingSmallEnough_KeepsTheSmallestItDoesAccept()
    {
        // A hosted NumberBox: it is a text field, so it is Normal or it is nothing.
        RibbonItemMetrics box = Item(RibbonItemSizes.Normal, 0, 96, 0);

        Assert.Equal(RibbonItemSize.Normal, box.SizeUnder(RibbonItemSize.Small));
        Assert.Equal(96, Measure(RibbonItemSize.Small, box));
    }

    [Fact]
    public void AnItemDeclaringNothing_IsLaidOutAsNormal()
    {
        // What a bare WinUI control dropped into a group gets, having said nothing at all.
        RibbonItemMetrics bare = Item(RibbonItemSizes.None, 40, 96, 72);

        Assert.Equal(RibbonItemSize.Normal, bare.SizeUnder(RibbonItemSize.Large));
        Assert.Equal(RibbonItemSize.Normal, bare.SizeUnder(RibbonItemSize.Small));
    }

    [Fact]
    public void NarrowingTheCap_NeverWidensTheGroup()
    {
        var group = new RibbonGroupMetrics(0, 18, 0, 0,
        [
            Item(RibbonItemSizes.All, 40, 90, 72),
            Item(RibbonItemSizes.All, 40, 88, 70),
            Item(RibbonItemSizes.Normal, 0, 96, 0),
            Small(40),
        ]);

        double large = RibbonLayoutSolver.Measure(group, RibbonItemSize.Large);
        double normal = RibbonLayoutSolver.Measure(group, RibbonItemSize.Normal);
        double small = RibbonLayoutSolver.Measure(group, RibbonItemSize.Small);

        Assert.True(normal <= large, $"Normal ({normal}) is wider than Large ({large})");
        Assert.True(small <= normal, $"Small ({small}) is wider than Normal ({normal})");
    }

    private static double Measure(RibbonItemSize cap, params RibbonItemMetrics[] items) =>
        RibbonLayoutSolver.Measure(new RibbonGroupMetrics(0, 0, 0, 0, items), cap);

    private static RibbonItemMetrics Item(RibbonItemSizes allowed, double small, double normal, double large) =>
        new(allowed, small, normal, large);

    private static RibbonItemMetrics Small(double width) =>
        new(RibbonItemSizes.Small, width, width, width);

    private static RibbonItemMetrics Separator(double width) =>
        new(RibbonItemSizes.None, width, width, width, IsSeparator: true);
}

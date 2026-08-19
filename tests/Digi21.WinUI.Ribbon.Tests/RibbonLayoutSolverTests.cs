using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// The strip-wide decisions: which group gives way, in what order, when one folds into its button,
// and what happens when there is no room left for anything.
//
// Every test below is built on the same three groups of five buttons, whose widths were chosen so
// that the sequence of states the solver walks through is easy to read:
//
//   940  everything Large
//   792  the first group Normal          712  the first group Small
//   564  the second group Normal         484  the second group Small
//   336  the third group Normal          256  the third group Small
//   246  the first group collapsed       236  and the second       226  and the third
//   124  the collapsed buttons lose their labels
//
// A width picks how far down that column the solver stops, and nothing else does.
public class RibbonLayoutSolverTests
{
    private const double Chrome = 12;
    private const double CollapsedWidth = 70;
    private const double CollapsedIconWidth = 36;

    [Fact]
    public void WithRoomToSpare_EveryItemIsAtItsLargest()
    {
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), double.PositiveInfinity);

        Assert.Equal(940, layout.Width);
        Assert.False(layout.Overflows);
        Assert.All(layout.Groups, group =>
        {
            Assert.False(group.IsCollapsed);
            Assert.All(group.ItemSizes, size => Assert.Equal(RibbonItemSize.Large, size));
        });
    }

    [Fact]
    public void TheLowestPriorityGroupGivesWayFirst()
    {
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), 900);

        Assert.Equal(792, layout.Width);
        Assert.Equal(RibbonItemSize.Normal, layout.Groups[0].ItemSizes[0]);
        Assert.Equal(RibbonItemSize.Large, layout.Groups[1].ItemSizes[0]);
        Assert.Equal(RibbonItemSize.Large, layout.Groups[2].ItemSizes[0]);
    }

    [Fact]
    public void GroupsOfEqualPriority_GiveWayFromTheRight()
    {
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(0, 0, 0), 900);

        Assert.Equal(RibbonItemSize.Large, layout.Groups[0].ItemSizes[0]);
        Assert.Equal(RibbonItemSize.Large, layout.Groups[1].ItemSizes[0]);
        Assert.Equal(RibbonItemSize.Normal, layout.Groups[2].ItemSizes[0]);
    }

    [Fact]
    public void NothingCollapses_WhileAnyGroupCanStillBeMadeSmaller()
    {
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), 260);

        Assert.Equal(256, layout.Width);
        Assert.All(layout.Groups, group =>
        {
            Assert.False(group.IsCollapsed);
            Assert.All(group.ItemSizes, size => Assert.Equal(RibbonItemSize.Small, size));
        });
    }

    [Fact]
    public void AGroupThatNoLongerFits_FoldsIntoItsButton()
    {
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), 250);

        Assert.Equal(246, layout.Width);
        Assert.True(layout.Groups[0].IsCollapsed);
        Assert.Equal(CollapsedWidth, layout.Groups[0].Width);
        Assert.False(layout.Groups[1].IsCollapsed);
        Assert.False(layout.Groups[2].IsCollapsed);
    }

    [Fact]
    public void ACollapsedGroup_ShowsItsItemsAtTheirLargest()
    {
        // Its items are in a flyout now, and a flyout has all the width it wants. Reporting the
        // shapes the group had while it was being squeezed would draw a Small toolbar inside a
        // pop-up with room for anything.
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), 250);

        Assert.True(layout.Groups[0].IsCollapsed);
        Assert.All(layout.Groups[0].ItemSizes, size => Assert.Equal(RibbonItemSize.Large, size));
    }

    [Fact]
    public void WhenEveryGroupHasCollapsed_TheButtonsDropTheirLabels()
    {
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), 200);

        Assert.Equal(124, layout.Width);
        Assert.All(layout.Groups, group =>
        {
            Assert.True(group.IsCollapsed);
            Assert.False(group.ShowsCollapsedLabel);
            Assert.Equal(CollapsedIconWidth, group.Width);
        });
    }

    [Fact]
    public void WhenEvenTheIconsDoNotFit_TheRibbonSaysSoAndKeepsEveryGroup()
    {
        // There is no state after this one. A command drawn off the edge can still be reached by
        // widening the window; a command taken out of the strip cannot be reached at all.
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), 100);

        Assert.True(layout.Overflows);
        Assert.Equal(124, layout.Width);
        Assert.Equal(3, layout.Groups.Count);
        Assert.All(layout.Groups, group => Assert.True(group.Width > 0));
    }

    [Fact]
    public void AGroupWiderAsAButtonThanAsItself_IsLeftAlone()
    {
        // Two icons and a bit of chrome are narrower than a button carrying the group's name, so
        // folding this one would both widen the strip and hide two commands.
        var group = new RibbonGroupMetrics(0, Chrome, CollapsedWidth, CollapsedIconWidth, [Button(), Button()]);

        RibbonLayout layout = RibbonLayoutSolver.Solve([group], 0);

        Assert.False(layout.Groups[0].IsCollapsed);
        Assert.True(layout.Overflows);
    }

    [Fact]
    public void NothingGrowsAsTheWindowNarrows()
    {
        RibbonLayout? previous = null;

        for (double available = 1000; available >= 0; available -= 5)
        {
            RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), available);

            if (previous is { } before)
            {
                Assert.True(layout.Width <= before.Width, $"the strip grew at {available}");

                for (int i = 0; i < layout.Groups.Count; i++)
                {
                    RibbonGroupArrangement was = before.Groups[i];
                    RibbonGroupArrangement now = layout.Groups[i];

                    Assert.True(now.Width <= was.Width, $"group {i} grew at {available}");
                    Assert.True(!was.IsCollapsed || now.IsCollapsed, $"group {i} came back out at {available}");

                    // A collapsed group's items are reported at the size its flyout shows them, so
                    // only a group that is still on the strip has an on-screen size to compare.
                    if (was.IsCollapsed || now.IsCollapsed)
                    {
                        continue;
                    }

                    for (int j = 0; j < now.ItemSizes.Count; j++)
                    {
                        Assert.True(now.ItemSizes[j] <= was.ItemSizes[j], $"item {i}.{j} grew at {available}");
                    }
                }
            }

            previous = layout;
        }
    }

    [Fact]
    public void NarrowingAndWideningBack_LandsExactlyWhereItStarted()
    {
        var onTheWayDown = new Dictionary<double, double>();

        for (double available = 1000; available >= 0; available -= 5)
        {
            onTheWayDown[available] = RibbonLayoutSolver.Solve(Strip(), available).Width;
        }

        for (double available = 0; available <= 1000; available += 5)
        {
            RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), available);

            Assert.Equal(onTheWayDown[available], layout.Width);
        }
    }

    [Fact]
    public void NoGroupEverLeavesTheStrip()
    {
        for (double available = 1000; available >= 0; available -= 5)
        {
            RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), available);

            Assert.Equal(3, layout.Groups.Count);
            Assert.All(layout.Groups, group => Assert.True(group.Width > 0, $"a group vanished at {available}"));
        }
    }

    [Fact]
    public void AStripWithNoGroups_IsAnEmptyLayout()
    {
        RibbonLayout layout = RibbonLayoutSolver.Solve([], 0);

        Assert.Empty(layout.Groups);
        Assert.Equal(0, layout.Width);
        Assert.False(layout.Overflows);
    }

    // Three groups of five buttons each, in the order they would be declared, with the priorities
    // given. The default set is the usual one: the leftmost group is the most expendable.
    private static RibbonGroupMetrics[] Strip(int first = 0, int second = 10, int third = 20) =>
    [
        Group(first),
        Group(second),
        Group(third),
    ];

    private static RibbonGroupMetrics Group(int priority) =>
        new(priority, Chrome, CollapsedWidth, CollapsedIconWidth,
            [Button(), Button(), Button(), Button(), Button()]);

    private static RibbonItemMetrics Button() => new(RibbonItemSizes.All, 32, 72, 56);
}

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
    public void WhenFoldingIsNotEnough_TheButtonsDropTheirLabelsOneAtATime()
    {
        // A strip only loses the names it has to, in the same priority order as everything else.
        RibbonLayout layout = RibbonLayoutSolver.Solve(Strip(), 200);

        Assert.Equal(192, layout.Width);
        Assert.All(layout.Groups, group => Assert.True(group.IsCollapsed));

        Assert.False(layout.Groups[0].ShowsCollapsedLabel);
        Assert.Equal(CollapsedIconWidth, layout.Groups[0].Width);

        Assert.True(layout.Groups[1].ShowsCollapsedLabel);
        Assert.True(layout.Groups[2].ShowsCollapsedLabel);
        Assert.Equal(CollapsedWidth, layout.Groups[2].Width);
    }

    [Fact]
    public void AButtonWhoseIconIsNoNarrowerThanItsName_KeepsTheName()
    {
        // Nothing is assumed about the numbers the control hands over. A button that gains no width
        // by dropping its label keeps the label, which is more use for the same room.
        var group = new RibbonGroupMetrics(0, Chrome, CollapsedWidth, CollapsedIconWidth: CollapsedWidth,
            [Button(), Button(), Button(), Button(), Button()]);

        RibbonLayout layout = RibbonLayoutSolver.Solve([group], 0);

        Assert.True(layout.Groups[0].IsCollapsed);
        Assert.True(layout.Groups[0].ShowsCollapsedLabel);
        Assert.Equal(CollapsedWidth, layout.Groups[0].Width);
        Assert.True(layout.Overflows);
    }

    [Fact]
    public void NoButtonDropsItsName_UntilThereIsNothingElseLeftToTry()
    {
        // A folded button without its name is the least identifiable state there is: an icon on its
        // own, with nothing to say which group it belongs to. It has to be the last thing tried for
        // the whole strip, not another rung among the others.
        //
        // The third group here is two buttons wide, so it is narrower than a button carrying its
        // name and never folds. That is what makes this test worth running: the labels come off
        // while it is still on the strip, so the assertions below have something to look at.
        RibbonGroupMetrics[] strip =
        [
            Group(0),
            Group(10),
            new RibbonGroupMetrics(20, Chrome, CollapsedWidth, CollapsedIconWidth, [Button(), Button()]),
        ];

        // Asserted step by step rather than at the index the first name went: a property of every
        // transition holds however long the sequence gets, and it fails naming the step instead of
        // a state number that moves whenever the algorithm is touched.
        RibbonGroupArrangement[]? previous = null;
        int drops = 0;
        int inspected = 0;

        foreach (RibbonGroupArrangement[] state in RibbonLayoutSolver.States(strip))
        {
            if (previous is { } before)
            {
                int dropped = -1;
                for (int i = 0; i < state.Length; i++)
                {
                    if (before[i].ShowsCollapsedLabel && !state[i].ShowsCollapsedLabel)
                    {
                        dropped = i;
                    }
                }

                if (dropped >= 0)
                {
                    drops++;

                    for (int i = 0; i < before.Length; i++)
                    {
                        if (before[i].IsCollapsed)
                        {
                            continue;
                        }

                        inspected++;

                        Assert.True(
                            before[i].ItemSizes.SequenceEqual(RibbonLayoutSolver.SizesUnder(strip[i], RibbonItemSize.Small)),
                            $"group {dropped} lost its name while group {i} could still have shrunk");

                        Assert.False(
                            strip[i].CollapsedWidth < before[i].Width,
                            $"group {dropped} lost its name while group {i} could still have folded");
                    }
                }
            }

            previous = state;
        }

        Assert.True(drops > 0, "no step in the sequence dropped a name");
        Assert.True(inspected > 0, "no name was dropped while a group was still on the strip");
    }

    [Fact]
    public void EveryStateOnlyDegradesTheOneBeforeIt()
    {
        // The invariant everything else rests on, asserted on the sequence itself instead of being
        // inferred from wherever a sweep of widths happened to stop.
        //
        // Note what is deliberately not asserted: that each state is narrower overall. A cap step
        // may widen a group whose Normal label runs longer on one row than its Large one does
        // wrapped, and that is allowed, because Solve never returns a state without first measuring
        // it against the width it was given. What may not happen is a step that degrades a group and
        // charges more for it.
        RibbonGroupArrangement[]? previous = null;
        int index = 0;

        foreach (RibbonGroupArrangement[] state in RibbonLayoutSolver.States(Strip()))
        {
            if (previous is { } before)
            {
                for (int i = 0; i < state.Length; i++)
                {
                    Assert.True(!before[i].IsCollapsed || state[i].IsCollapsed, $"group {i} came back out at state {index}");
                    Assert.True(before[i].ShowsCollapsedLabel || !state[i].ShowsCollapsedLabel, $"group {i} got its label back at state {index}");

                    if (!before[i].IsCollapsed && state[i].IsCollapsed)
                    {
                        Assert.True(state[i].Width < before[i].Width, $"folding group {i} made it wider at state {index}");
                    }

                    if (before[i].ShowsCollapsedLabel && !state[i].ShowsCollapsedLabel)
                    {
                        Assert.True(state[i].Width < before[i].Width, $"dropping group {i}'s label made it wider at state {index}");
                    }

                    // A folded group's items are reported at the size its flyout shows them, so only
                    // a group that is on the strip in both states has an on-screen shape to compare.
                    if (before[i].IsCollapsed || state[i].IsCollapsed)
                    {
                        continue;
                    }

                    for (int j = 0; j < state[i].ItemSizes.Count; j++)
                    {
                        Assert.True(state[i].ItemSizes[j] <= before[i].ItemSizes[j], $"item {i}.{j} grew at state {index}");
                    }
                }
            }

            previous = state;
            index++;
        }

        // Six shrinks, three folds, three labels and the state they all started from.
        Assert.Equal(13, index);
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
        Assert.All(layout.Groups, group =>
        {
            Assert.True(group.IsCollapsed);
            Assert.False(group.ShowsCollapsedLabel);
            Assert.Equal(CollapsedIconWidth, group.Width);
        });
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

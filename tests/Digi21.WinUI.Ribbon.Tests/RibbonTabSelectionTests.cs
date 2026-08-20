using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// Which tab the ribbon shows once tabs are allowed to come and go.
//
// The rule is four lines long and it is the one thing about contextual tabs a user notices when it
// is wrong: the ground moves under them, and where they land is this. It is here rather than in the
// probe because it needs no window - it is arithmetic over which tabs are on the strip - and a rule
// that can be asked without a window should be.
public class RibbonTabSelectionTests
{
    [Fact]
    public void ATabOnTheStripIsTheTabShown()
    {
        Assert.Equal(2, RibbonTabSelection.Legalize([true, true, true], wanted: 2, fallback: 0));
    }

    [Fact]
    public void AContextualTabThatIsNotOnTheStripLeavesTheRibbonWhereItWas()
    {
        // The application asked for a tab it has not switched on. Nothing moves: the set of tabs on
        // the strip changes while the application runs, so this is a race and not a mistake, and
        // taking the user somewhere would be a worse answer than staying put.
        Assert.Equal(1, RibbonTabSelection.Legalize([true, true, false], wanted: 2, fallback: 1));
    }

    [Fact]
    public void ATabSwitchedOffUnderTheUserFallsBackToWhereItCameFrom()
    {
        // Tab 2 was showing and has gone; tab 0 is where it was chosen from.
        Assert.Equal(0, RibbonTabSelection.Legalize([true, true, false], wanted: 0, fallback: 2));
    }

    [Fact]
    public void AndToTheFirstTabThereIsWhenWhereItCameFromHasGoneToo()
    {
        // Both contextual tabs off at once, which is what cancelling the state that lit them up
        // does. The ribbon is not left showing nothing while a fixed tab is sitting there.
        Assert.Equal(1, RibbonTabSelection.Legalize([false, true, false], wanted: 0, fallback: 2));
    }

    [Fact]
    public void TheFirstTabThereIsMeansTheFirstOnTheStripAndNotTheFirstDeclared()
    {
        Assert.Equal(2, RibbonTabSelection.Legalize([false, false, true, true], wanted: -1, fallback: -1));
    }

    [Fact]
    public void ARibbonWithNoTabOnTheStripShowsNoTab()
    {
        // An application whose every tab is contextual is in this state until the first of them
        // lights up. The answer is that there is no tab, not a tab nobody offered.
        Assert.Equal(-1, RibbonTabSelection.Legalize([false, false], wanted: 0, fallback: 1));
    }

    [Fact]
    public void ARibbonWithNoTabsAtAllShowsNoTab()
    {
        Assert.Equal(-1, RibbonTabSelection.Legalize([], wanted: 0, fallback: 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void AnIndexOffTheEndIsNoWorseThanATabThatIsNotOnTheStrip(int wanted)
    {
        // Reached by an application that removed a tab and set the index in the other order, which
        // is not a state worth throwing over: the fallback is still standing.
        Assert.Equal(1, RibbonTabSelection.Legalize([true, true, true], wanted, fallback: 1));
    }

    [Fact]
    public void AFallbackOffTheEndIsIgnoredRatherThanReturned()
    {
        Assert.Equal(0, RibbonTabSelection.Legalize([true, true], wanted: 5, fallback: 9));
    }

    [Fact]
    public void TheAnswerIsAlwaysATabOnTheStripOrNothing()
    {
        // The invariant the other tests are examples of, over every combination of three tabs and
        // every pair of indices near them. A ribbon showing a tab that is not on the strip has a
        // header that is not there and a body that is, which is the one state nothing downstream is
        // written to survive.
        for (int mask = 0; mask < 8; mask++)
        {
            bool[] active = [(mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0];

            for (int wanted = -2; wanted <= 4; wanted++)
            {
                for (int fallback = -2; fallback <= 4; fallback++)
                {
                    int chosen = RibbonTabSelection.Legalize(active, wanted, fallback);

                    Assert.True(
                        chosen == -1 || (chosen >= 0 && chosen < active.Length && active[chosen]),
                        $"[{string.Join(",", active)}] wanted {wanted} fallback {fallback} gave {chosen}");

                    // And it only gives up when there is nothing to give.
                    Assert.True(
                        chosen != -1 || Array.TrueForAll(active, on => !on),
                        $"[{string.Join(",", active)}] wanted {wanted} fallback {fallback} gave no tab where one was on the strip");
                }
            }
        }
    }
}

using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// Which tab an arrow key moves to.
//
// Here rather than in the probe because a keystroke cannot be faked from inside the process, so a
// window would not make this any more real - and because it is arithmetic over which tabs are on
// the strip, which is a question worth being able to ask without one. What the probe can still see
// is the shape around it: one tab stop for the whole strip, and it is the tab on show.
public class RibbonKeyboardTests
{
    [Fact]
    public void AnArrowStepsToTheTabBesideIt()
    {
        Assert.Equal(2, RibbonKeyboard.Step([true, true, true], from: 1, forward: true));
        Assert.Equal(0, RibbonKeyboard.Step([true, true, true], from: 1, forward: false));
    }

    [Fact]
    public void AndOverAContextualTabThatIsSwitchedOff()
    {
        // A tab that is off the strip is not a place to stand, and it is not a stop on the way to
        // one either: the arrow goes past it as if it had never been declared.
        Assert.Equal(3, RibbonKeyboard.Step([true, true, false, true], from: 1, forward: true));
        Assert.Equal(0, RibbonKeyboard.Step([true, false, false, true], from: 3, forward: false));
    }

    [Fact]
    public void ItWrapsRoundTheEnd()
    {
        // As Office does, and as every WinUI control holding a strip of headers does.
        Assert.Equal(0, RibbonKeyboard.Step([true, true, true], from: 2, forward: true));
        Assert.Equal(2, RibbonKeyboard.Step([true, true, true], from: 0, forward: false));
    }

    [Fact]
    public void AndWrapsOntoTheFirstTabThereIsRatherThanTheFirstDeclared()
    {
        Assert.Equal(1, RibbonKeyboard.Step([false, true, true], from: 2, forward: true));
        Assert.Equal(1, RibbonKeyboard.Step([true, true, false], from: 0, forward: false));
    }

    [Fact]
    public void AStripOfOneTabAnswersWithThatTab()
    {
        // Not with nothing. The key belongs to the strip either way, and the ribbon consumes it
        // rather than letting it fall through and hand the focus sideways into the body.
        Assert.Equal(1, RibbonKeyboard.Step([false, true, false], from: 1, forward: true));
        Assert.Equal(1, RibbonKeyboard.Step([false, true, false], from: 1, forward: false));
    }

    [Fact]
    public void AStripWithNoTabOnItAnswersWithNothing()
    {
        // An application whose every tab is contextual is in this state until the first of them
        // lights up.
        Assert.Equal(-1, RibbonKeyboard.Step([false, false], from: 0, forward: true));
        Assert.Equal(-1, RibbonKeyboard.Step([], from: 0, forward: true));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void AnArrowFromNowhereGoesToTheFirstTabThatWay(int from)
    {
        // Standing nowhere is where a user is left when the contextual tab they were on has just
        // been switched off. The arrow still means what it always meant: the first tab that way.
        Assert.Equal(1, RibbonKeyboard.Step([false, true, true], from, forward: true));
        Assert.Equal(2, RibbonKeyboard.Step([false, true, true], from, forward: false));
    }

    [Fact]
    public void HomeAndEndAreTheFirstAndLastTabOnTheStrip()
    {
        Assert.Equal(0, RibbonKeyboard.Edge([true, true, true], first: true));
        Assert.Equal(2, RibbonKeyboard.Edge([true, true, true], first: false));
    }

    [Fact]
    public void AndNotTheFirstAndLastDeclared()
    {
        // The two ends of a strip are the two ends of what is drawn on it. A contextual tab that is
        // off is not the end of anything.
        Assert.Equal(1, RibbonKeyboard.Edge([false, true, true, false], first: true));
        Assert.Equal(2, RibbonKeyboard.Edge([false, true, true, false], first: false));
        Assert.Equal(-1, RibbonKeyboard.Edge([false, false], first: true));
    }

    [Fact]
    public void EveryArrowLandsOnATabOnTheStripOrOnNothing()
    {
        // The invariant the examples above are examples of, over every combination of four tabs and
        // every index near them. A strip that puts the keyboard on a tab which is not there has a
        // header that cannot be seen holding a focus that cannot be found.
        for (int mask = 0; mask < 16; mask++)
        {
            bool[] active = [(mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0];
            bool any = Array.Exists(active, on => on);

            for (int from = -2; from <= 5; from++)
            {
                foreach (bool forward in new[] { true, false })
                {
                    int landed = RibbonKeyboard.Step(active, from, forward);

                    Assert.True(
                        landed == -1 || (landed >= 0 && landed < active.Length && active[landed]),
                        $"[{string.Join(",", active)}] from {from} {(forward ? "forwards" : "backwards")} landed on {landed}");

                    // And it only gives up when there is nothing to give.
                    Assert.True(
                        landed != -1 || !any,
                        $"[{string.Join(",", active)}] from {from} landed nowhere with a tab on the strip");
                }
            }

            Assert.Equal(any, RibbonKeyboard.Edge(active, first: true) != -1);
            Assert.Equal(any, RibbonKeyboard.Edge(active, first: false) != -1);
        }
    }
}

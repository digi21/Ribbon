using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// How much wider the tabs of a contextual group have to be for the name on their band to fit.
//
// The rule exists because of what a band is: it is drawn from the left edge of the first tab of its
// group to the right edge of the last, so with one narrow tab it has room for nothing the tab does
// not already say - which is exactly the information it was added to carry.
public class RibbonHeadingFitTests
{
    [Fact]
    public void TabsThatAlreadyFitTheirBandAreLeftAlone()
    {
        Assert.Equal(0, RibbonHeadingFit.Extra(covered: 200, needed: 84, tabs: 2));
        Assert.Equal(0, RibbonHeadingFit.Extra(covered: 84, needed: 84, tabs: 1));
    }

    [Fact]
    public void AGroupOfOneNarrowTabTakesTheWholeOfWhatIsMissing()
    {
        // lop's case: one contextual tab seventy pixels wide under a band whose name asks for a
        // hundred and twenty.
        Assert.Equal(50, RibbonHeadingFit.Extra(covered: 70, needed: 120, tabs: 1));
    }

    [Fact]
    public void AndAPairSharesItEqually()
    {
        // Equally rather than one of them taking it, so that two tabs of a pair stay the size of
        // each other: a band over two tabs of visibly different widths reads as two things.
        Assert.Equal(15, RibbonHeadingFit.Extra(covered: 100, needed: 130, tabs: 2));
    }

    [Fact]
    public void AGroupWithNoTabOnTheStripIsWidenedByNothing()
    {
        // The ordinary state of a contextual group, and the one place this could divide by nothing.
        Assert.Equal(0, RibbonHeadingFit.Extra(covered: 0, needed: 120, tabs: 0));
    }

    [Theory]
    [InlineData(70, 120, 1)]
    [InlineData(100, 130, 2)]
    [InlineData(60, 61, 3)]
    [InlineData(0, 84, 1)]
    public void WhatIsAddedIsExactlyWhatWasMissing(double covered, double needed, int tabs)
    {
        // The invariant: after widening, the tabs of the group cover the name and not a pixel more.
        // More would be a band with room to spare and tabs bigger than they need to be; less would
        // be the trimmed name this rule exists to prevent.
        double extra = RibbonHeadingFit.Extra(covered, needed, tabs);

        Assert.Equal(needed, covered + (extra * tabs), 6);
    }
}

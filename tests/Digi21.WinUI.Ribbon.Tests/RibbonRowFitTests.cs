using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// How tall a row is and how many of them an item needs. Both halves used to be one assumption - that
// everything which is not Large takes exactly one row, and that a row is as tall as the tallest of
// them - and the assumption held until an application put a stack of three labelled boxes in a group
// as a single element. One item, a hundred pixels tall, made all three rows a hundred pixels tall.
public class RibbonRowFitTests
{
    private const int MaxRows = 3;

    [Fact]
    public void AGroupOfButtons_KeepsTheRibbonsOwnRowHeight()
    {
        Assert.Equal(24, RibbonRowFit.RowHeight([24, 24, 24]));
    }

    [Fact]
    public void AGroupHoldingATallerControl_GetsTallerRows()
    {
        // A WinUI control is thirty-two, and one with a name beside it thirty-three.
        Assert.Equal(33, RibbonRowFit.RowHeight([24, 33, 24]));
    }

    [Fact]
    public void SomethingTallerThanARow_DoesNotDecideHowTallARowIs()
    {
        // The whole point. The hundred-pixel stack is going to span rows, so it has no business
        // setting their height on the way past.
        Assert.Equal(33, RibbonRowFit.RowHeight([33, 100]));
    }

    [Fact]
    public void AGroupOfNothingButTallThings_StillHasOrdinaryRows()
    {
        Assert.Equal(24, RibbonRowFit.RowHeight([100, 120]));
    }

    [Fact]
    public void AnItemThatFitsARow_TakesOne()
    {
        Assert.Equal(1, RibbonRowFit.Rows(33, rowHeight: 33, MaxRows));
        Assert.Equal(1, RibbonRowFit.Rows(20, rowHeight: 33, MaxRows));
    }

    [Fact]
    public void AnItemTallerThanARow_TakesTheRowsItNeeds()
    {
        Assert.Equal(2, RibbonRowFit.Rows(50, rowHeight: 33, MaxRows));
        Assert.Equal(3, RibbonRowFit.Rows(99, rowHeight: 33, MaxRows));
    }

    [Fact]
    public void AnItemTallerThanTheGroup_TakesTheGroup()
    {
        // There is nowhere further to put it, and a count above the group's would pack it into a
        // column that does not exist.
        Assert.Equal(MaxRows, RibbonRowFit.Rows(400, rowHeight: 33, MaxRows));
    }

    [Fact]
    public void TheBoundaryIsAboveAControlAndBelowTwoOfThem()
    {
        // The one number in here that draws a distinction rather than measuring one, so it is worth
        // pinning where it sits: a standard control counts as a row, two of them stacked do not.
        Assert.Equal(34, RibbonRowFit.RowHeight([34]));
        Assert.Equal(24, RibbonRowFit.RowHeight([68]));
    }
}

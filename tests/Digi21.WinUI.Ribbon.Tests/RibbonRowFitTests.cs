using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// How tall a row is and how many of them an item needs. Both halves used to be one assumption - that
// everything which is not Large takes exactly one row, and that a row is as tall as the tallest of
// them - and the assumption held until an application put a stack of three labelled boxes in a group
// as a single element. One item, a hundred pixels tall, made all three rows a hundred pixels tall.
//
// Taking the rows away from that item was only half the answer. The other half is here: the rows it
// spans have to be as tall between them as the item is, or the group draws it in less room than it
// asked for and cuts the top off the first box and the bottom off the last.
public class RibbonRowFitTests
{
    private const int MaxRows = 3;

    [Fact]
    public void AGroupOfButtons_KeepsTheRibbonsOwnRowHeight()
    {
        Assert.Equal(24, RibbonRowFit.RowHeight([24, 24, 24], MaxRows));
    }

    [Fact]
    public void AGroupHoldingATallerControl_GetsTallerRows()
    {
        // A WinUI control is thirty-two, and one with a name beside it thirty-three.
        Assert.Equal(33, RibbonRowFit.RowHeight([24, 33, 24], MaxRows));
    }

    [Fact]
    public void SomethingTallerThanARow_DoesNotSetTheHeightOfOne()
    {
        // The whole point. The hundred-pixel stack is going to span rows, so it has no business
        // making each of them a hundred pixels tall on the way past. It does get a third of itself,
        // because three rows of thirty-three would leave a pixel of it outside the group.
        Assert.Equal(100d / 3, RibbonRowFit.RowHeight([33, 100], MaxRows), 6);
    }

    [Fact]
    public void SomethingThatFitsTheRowsItSpans_LeavesThemAlone()
    {
        // Three rows of thirty-three hold a ninety-pixel stack with room over, so there is nothing
        // to raise: the tall item is paid for out of the height the controls beside it already need.
        Assert.Equal(33, RibbonRowFit.RowHeight([33, 90], MaxRows));
    }

    [Fact]
    public void AGroupOfNothingButTallThings_HasRowsThatHoldTheTallestOfThem()
    {
        // No control here says how tall a row is, so the rows are worth whatever it takes for three
        // of them to hold the tallest item - and the group is the height of that item, once, rather
        // than three times it and rather than two thirds of it.
        Assert.Equal(40, RibbonRowFit.RowHeight([100, 120], MaxRows));
    }

    [Fact]
    public void TheRowsAnItemSpans_AlwaysHoldIt()
    {
        // The invariant the two halves of the rule exist to keep. Whatever a group is made of, no
        // item is asked to draw itself in less room than it said it needed.
        double[][] groups =
        [
            [24, 24, 24],
            [33, 100],
            [24, 33, 68],
            [100, 120],
            [33, 400],
            [45],
        ];

        foreach (double[] heights in groups)
        {
            double row = RibbonRowFit.RowHeight(heights, MaxRows);

            foreach (double height in heights)
            {
                int rows = RibbonRowFit.Rows(height, row, MaxRows);

                Assert.True(rows * row >= height, $"{height} in {rows} rows of {row}");
            }
        }
    }

    [Fact]
    public void ARibbonOfOneRow_IsAskedTheSameQuestion()
    {
        // And gives a bigger answer, because one row asked to hold a hundred-pixel stack is a
        // hundred pixels tall. That is why a group holding one is folded in a simplified ribbon
        // rather than laid out inline - the rule is not wrong here, it is being asked something a
        // single row cannot do anything sensible with.
        Assert.Equal(100, RibbonRowFit.RowHeight([24, 100], maxRows: 1));

        // What does fit a row still decides how tall the row is, which is what a simplified ribbon
        // is laid out from.
        Assert.Equal(33, RibbonRowFit.RowHeight([24, 33], maxRows: 1));
    }

    [Fact]
    public void TheOrderTheItemsCameIn_DoesNotChangeTheAnswer()
    {
        // Which rows a spanning item takes is settled against the single-row items rather than
        // against the running total, so that a group is the same height however it is listed.
        Assert.Equal(RibbonRowFit.RowHeight([33, 100, 120], MaxRows), RibbonRowFit.RowHeight([120, 100, 33], MaxRows), 6);
        Assert.Equal(RibbonRowFit.RowHeight([100, 33], MaxRows), RibbonRowFit.RowHeight([33, 100], MaxRows), 6);
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
    public void ASwitchIsNotARow()
    {
        // A WinUI ToggleSwitch is forty pixels tall, and while the bound sat above that, one of them
        // in a tab made every row of every group forty - a hundred and forty-eight pixels of ribbon
        // for one switch, with eighty of nothing underneath it. It spans two ordinary rows instead.
        Assert.Equal(24, RibbonRowFit.RowHeight([40], MaxRows));
        Assert.Equal(2, RibbonRowFit.Rows(40, rowHeight: 24, MaxRows));

        // And beside controls that do set the height of a row, it still does not raise them.
        Assert.Equal(33, RibbonRowFit.RowHeight([33, 40], MaxRows));
    }

    [Fact]
    public void TheBoundaryIsAboveAControlAndBelowTwoOfThem()
    {
        // The one number in here that draws a distinction rather than measuring one, so it is worth
        // pinning where it sits: a standard control counts as a row, two of them stacked do not.
        Assert.Equal(34, RibbonRowFit.RowHeight([34], MaxRows));

        // Two of them stacked are not a row: they take rows of their own, and three ordinary rows
        // already hold them, so the row height does not move.
        Assert.Equal(24, RibbonRowFit.RowHeight([68], MaxRows));
    }
}

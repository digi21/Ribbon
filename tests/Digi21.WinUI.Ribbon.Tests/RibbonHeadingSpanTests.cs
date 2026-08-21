using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// Which tabs the coloured band over a set of contextual tabs is drawn across.
//
// The set changes every time one of those tabs is switched on or off, and it is empty most of the
// time, so the band is drawn over a different run of tabs from one second to the next. It is
// arithmetic over which tabs are on the strip, which is why it is here rather than in the harness.
public class RibbonHeadingSpanTests
{
    private static readonly object Tools = new();
    private static readonly object Table = new();

    [Fact]
    public void OneTabOfTheGroupIsABandOverThatTab()
    {
        Assert.Equal((2, 1), RibbonHeadingSpan.Of([null, null, Tools], Tools));
    }

    [Fact]
    public void TwoSideBySideAreOneBandOverBoth()
    {
        // The whole point of a heading: it says that these tabs are one thing, which two bands
        // saying the same word twice would not.
        Assert.Equal((1, 2), RibbonHeadingSpan.Of([null, Tools, Tools], Tools));
    }

    [Fact]
    public void AGroupWithNoTabOnTheStripIsNoBandAtAll()
    {
        // The ordinary state of a contextual group: nothing is selected, so none of its tabs is on
        // the strip. The band is still there to be measured - that is what keeps the strip the
        // height it was - and it is drawn over nothing.
        Assert.Equal((0, 0), RibbonHeadingSpan.Of([null, null], Tools));
        Assert.Equal((0, 0), RibbonHeadingSpan.Of([], Tools));
    }

    [Fact]
    public void EachGroupIsAskedAboutSeparately()
    {
        Assert.Equal((0, 2), RibbonHeadingSpan.Of([Tools, Tools, Table], Tools));
        Assert.Equal((2, 1), RibbonHeadingSpan.Of([Tools, Tools, Table], Table));
    }

    [Fact]
    public void ATabBetweenTwoOfAGroupIsCoveredByTheBand()
    {
        // Not a case Office offers, and not one this library reorders tabs to avoid: the strip draws
        // them where they were declared. An application that declares a fixed tab between two tabs
        // of one group gets a band over all three, which is the honest picture of what it asked for
        // and is visibly wrong rather than quietly wrong.
        Assert.Equal((0, 3), RibbonHeadingSpan.Of([Tools, null, Tools], Tools));
    }

    [Fact]
    public void TheBandIsAlwaysOverTabsOfItsOwnGroupAtBothEnds()
    {
        // The invariant the examples are examples of, over every arrangement of four tabs. Whatever
        // else the band covers, the tab it starts on and the tab it ends on are its own - a band
        // that began one tab early would be a band over somebody else's name.
        for (int mask = 0; mask < 16; mask++)
        {
            object?[] groups =
            [
                (mask & 1) != 0 ? Tools : null,
                (mask & 2) != 0 ? Tools : null,
                (mask & 4) != 0 ? Tools : null,
                (mask & 8) != 0 ? Tools : null,
            ];

            (int first, int count) = RibbonHeadingSpan.Of(groups, Tools);

            if (mask == 0)
            {
                Assert.Equal((0, 0), (first, count));
                continue;
            }

            Assert.True(count > 0, $"{mask:X}: a group with a tab on the strip is drawn over nothing");
            Assert.Same(Tools, groups[first]);
            Assert.Same(Tools, groups[first + count - 1]);

            // And it covers every tab of the group: a band that stopped short would leave one of
            // its own tabs outside the thing that says what it is for.
            for (int i = 0; i < groups.Length; i++)
            {
                Assert.True(
                    !ReferenceEquals(groups[i], Tools) || (i >= first && i < first + count),
                    $"{mask:X}: the tab at {i} belongs to the group and is outside the band");
            }
        }
    }
}

using Digi21.WinUI.Ribbon.Layout;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// Whether a change of tab is drawn, and which way the tab arriving comes from.
//
// The animation itself needs a window and belongs to the probe, which watches a tab come to rest.
// What is here is the decision in front of it, and every case in it is a case where the answer is to
// draw nothing: the ones that were once animations running where nobody had asked for one.
public class RibbonTabMotionTests
{
    [Fact]
    public void TheTabArrivingComesFromTheSideTheUserMovedTowards()
    {
        // Rightwards, so it comes in from the right: the ribbon moves the way the hand did.
        Assert.Equal(RibbonTabMotion.Distance, RibbonTabMotion.Entry(RibbonTabTransition.Slide, animations: true, opening: false, from: 0, to: 2));
    }

    [Fact]
    public void AndFromTheOtherSideGoingBack()
    {
        Assert.Equal(-RibbonTabMotion.Distance, RibbonTabMotion.Entry(RibbonTabTransition.Slide, animations: true, opening: false, from: 2, to: 0));
    }

    [Fact]
    public void AFadeStartsTheTabWhereItWillEnd()
    {
        Assert.Equal(0, RibbonTabMotion.Entry(RibbonTabTransition.Fade, animations: true, opening: false, from: 0, to: 2));
    }

    [Fact]
    public void NothingIsDrawnWhenTheApplicationAskedForNothing()
    {
        Assert.Null(RibbonTabMotion.Entry(RibbonTabTransition.None, animations: true, opening: false, from: 0, to: 2));
    }

    [Fact]
    public void NorWhenWindowsIsShowingNoAnimations()
    {
        // The user switched them off everywhere, and a ribbon is not the place to argue.
        Assert.Null(RibbonTabMotion.Entry(RibbonTabTransition.Slide, animations: false, opening: false, from: 0, to: 2));
    }

    [Fact]
    public void NorWhenTheTabIsArrivingInAPopupThatIsItselfOpening()
    {
        // A minimised ribbon opening a tab over the content. The popup animates itself, and two
        // arrivals for one click is one too many.
        Assert.Null(RibbonTabMotion.Entry(RibbonTabTransition.Slide, animations: true, opening: true, from: 0, to: 2));
    }

    [Fact]
    public void TheRibbonShowingItsFirstTabIsNotAChangeOfTab()
    {
        // Nothing was there to move away from. A window opening is not a gesture.
        Assert.Null(RibbonTabMotion.Entry(RibbonTabTransition.Slide, animations: true, opening: false, from: -1, to: 0));
    }

    [Fact]
    public void NorIsTheSameTabShownAgain()
    {
        // What every rebuild of the strip asks for: a tab added, a tab renamed, a contextual tab
        // switched on somewhere else. The tab on show has not moved and is not made to.
        Assert.Null(RibbonTabMotion.Entry(RibbonTabTransition.Slide, animations: true, opening: false, from: 1, to: 1));
    }

    [Fact]
    public void AndThereIsNothingToDrawWhenThereIsNoTabToShow()
    {
        // Every tab contextual and none of them switched on, which is a legitimate state and not a
        // fault.
        Assert.Null(RibbonTabMotion.Entry(RibbonTabTransition.Slide, animations: true, opening: false, from: 0, to: -1));
    }
}

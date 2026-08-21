namespace Digi21.WinUI.Ribbon.Layout;

// How much wider the tabs of a contextual group have to be for the name on their band to fit over
// them.
//
// The band is drawn from the left edge of the first tab of its group to the right edge of the last,
// which is what says those tabs are one thing - so its name has only that much room, and a group of
// one narrow tab is a band that can say nothing the tab does not already say. "Selection tools" over
// a seventy pixel tab is "Selectio...", and the part that was cut is the part the band was added
// for.
//
// The answer is not to let the band be wider than its tabs, which would be a band starting or ending
// over somebody else's name. It is for the tabs to make room: they are laid out in a row that
// nothing competes for width in, so a floor under them costs the strip nothing but its own width.
//
// Shared out equally rather than given to one of them, so that two tabs of a pair stay the size of
// each other. A group of one takes the whole of it.
internal static class RibbonHeadingFit
{
    // What to add to each tab of the group, given the room they take between them now - gaps
    // included, because the band covers those too - and the room the name needs.
    internal static double Extra(double covered, double needed, int tabs) =>
        tabs <= 0 || needed <= covered ? 0 : (needed - covered) / tabs;
}

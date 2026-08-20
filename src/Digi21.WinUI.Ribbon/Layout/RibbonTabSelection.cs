namespace Digi21.WinUI.Ribbon.Layout;

// Which tab the ribbon shows, given which of them are on the strip at all.
//
// Plain arithmetic over a list of booleans, and in this folder rather than inside the control for
// the reason the rest of this folder is here: it is a rule that decides where a user is standing
// when the ground moves under them, and a rule like that is worth being able to ask without a
// window. Every version of it that lived inside an event handler answered a slightly different
// question depending on which handler had run last.
//
// One rule, three uses, and the order of its answers is the whole of it:
//
// - Asked for a tab that is on the strip, it gives that tab. Nothing else here applies.
// - Asked for a tab that is not - an application that sets SelectedIndex to a contextual tab it has
//   not switched on - it leaves the ribbon where it was. A tab that cannot be shown is not a reason
//   to move somebody somewhere they did not ask to go, and it is not a reason to throw either: the
//   set of tabs on the strip changes under an application's feet by design.
// - Asked for a tab that is not, from a ribbon whose own tab has gone too - the contextual tab
//   somebody was standing on has just been switched off, and it did not say where it came from -
//   it falls to the first tab there is. A ribbon showing no tab is a window with no commands in it.
internal static class RibbonTabSelection
{
    // The tab to show: the one wanted, or the one already showing, or the first there is.
    internal static int Legalize(IReadOnlyList<bool> active, int wanted, int fallback)
    {
        if (Selectable(active, wanted))
        {
            return wanted;
        }

        if (Selectable(active, fallback))
        {
            return fallback;
        }

        for (int i = 0; i < active.Count; i++)
        {
            if (active[i])
            {
                return i;
            }
        }

        // Every tab off at once. Legitimate rather than a fault - an application whose tabs are all
        // contextual is in this state until the first of them lights up - and the answer is that
        // there is no tab to show, not a tab nobody offered.
        return -1;
    }

    private static bool Selectable(IReadOnlyList<bool> active, int index) =>
        index >= 0 && index < active.Count && active[index];
}

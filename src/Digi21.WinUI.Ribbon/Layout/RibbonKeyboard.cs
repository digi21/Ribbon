namespace Digi21.WinUI.Ribbon.Layout;

// Which tab an arrow key moves to, given which tabs are on the strip at all.
//
// Beside RibbonTabSelection, and here for the same reason: it is arithmetic over a list of
// booleans, it decides where somebody ends up, and a rule like that is worth being able to ask
// without a window. It matters more here than there. A keystroke cannot be faked from inside the
// process - the probe says so, and says it having tried - so the part of the keyboard that can be
// asked without one is the part that gets tested at all, and it is worth it being this part: which
// tab an arrow moves to is the whole of what the strip does with the keyboard.
//
// Two rules, and the whole of both is that a tab which is off the strip is not a place to stand:
//
// - An arrow steps to the next tab that is on the strip, over any number of contextual tabs that
//   are switched off, and wraps round the end - as Office does, and as every WinUI control holding
//   a strip of headers does. A strip of one tab answers with that tab rather than with nothing, so
//   that the key is still the strip's and does not fall through to something else.
// - Home and End go to the first and the last tab there is, which is not the first and the last
//   declared: a contextual tab that is off is not the end of anything.
internal static class RibbonKeyboard
{
    // The tab an arrow key moves to, or the one it started from when there is nowhere else to go.
    internal static int Step(IReadOnlyList<bool> active, int from, bool forward)
    {
        if (active.Count == 0)
        {
            return -1;
        }

        // Standing nowhere - a ribbon whose tab has just been switched off under the user - is not
        // a reason to refuse to move. The arrow means the same thing it always did: the first tab
        // that way.
        if (from < 0 || from >= active.Count)
        {
            return Edge(active, first: forward);
        }

        int by = forward ? 1 : -1;

        // Every other index in turn, ending back at the one it started from: a strip with one tab
        // on it answers with that tab, and a strip with none answers with nothing.
        for (int i = 1; i <= active.Count; i++)
        {
            int index = (((from + (by * i)) % active.Count) + active.Count) % active.Count;

            if (active[index])
            {
                return index;
            }
        }

        return -1;
    }

    // The first or the last tab on the strip.
    internal static int Edge(IReadOnlyList<bool> active, bool first)
    {
        for (int i = 0; i < active.Count; i++)
        {
            int index = first ? i : active.Count - 1 - i;

            if (active[index])
            {
                return index;
            }
        }

        return -1;
    }
}

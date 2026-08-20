namespace Digi21.WinUI.Ribbon.Layout;

// Whether a change of tab is drawn at all, and where the tab arriving comes from.
//
// In this folder for the reason the rest of it is here: it is a rule, and a rule is worth being able
// to ask without a window. What it decides is small and every part of it is a case somebody hit -
// the ribbon opening on its first tab, a tab redrawn without changing, a system with animations
// switched off - and each of those was once an animation running where nobody had asked for one.
//
// The order of the answers is the whole of it:
//
// - Nothing is drawn when the application asked for nothing, when Windows says no animations, or
//   when the tab is arriving inside a popup that is opening: the popup animates itself.
// - Nothing is drawn when there is no change to draw. The ribbon showing its first tab is not a
//   change - nothing was there to move away - and neither is the same tab shown again, which is what
//   every rebuild of the strip asks for.
// - Otherwise the tab arriving starts beside its place, on the side the user moved towards, and a
//   fade starts it where it will end.
internal static class RibbonTabMotion
{
    // Far enough to be a direction and not far enough to be a journey. It is a render transform, so
    // it costs the layout nothing however large it is; what it costs is the wait, and a ribbon is
    // the thing somebody is trying to get through rather than the thing they came to look at.
    internal const double Distance = 12;

    internal static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(160);

    // Where the tab arriving starts, in pixels beside its place - zero for a fade, nothing at all
    // for a change that is not drawn.
    internal static double? Entry(RibbonTabTransition transition, bool animations, bool opening, int from, int to)
    {
        if (transition == RibbonTabTransition.None || !animations || opening)
        {
            return null;
        }

        if (to < 0 || from < 0 || from == to)
        {
            return null;
        }

        if (transition == RibbonTabTransition.Fade)
        {
            return 0;
        }

        return from < to ? Distance : -Distance;
    }
}

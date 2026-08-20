namespace Digi21.WinUI.Ribbon;

/// <summary>How the ribbon draws a change of tab.</summary>
/// <remarks>
/// <para>
/// A tab is a whole strip of commands replaced at once. Replaced between two frames, it leaves the
/// eye to work out on its own that everything under the tab strip is now something else; a short
/// movement in the direction the user asked to go says it instead, and says which way they went.
/// That is the whole of what this does. It is chrome over a change that has already happened - the
/// tab is chosen, laid out and hit-testable before the first frame of it is drawn.
/// </para>
/// <para>
/// Whatever this says, a system told to show no animations is obeyed and the ribbon cuts. So does a
/// minimised ribbon opening a tab over the content: that is a popup arriving with an animation of
/// its own, and two arrivals for one click is one too many.
/// </para>
/// </remarks>
public enum RibbonTabTransition
{
    /// <summary>The tab arriving fades in from the side the user moved towards. The default.</summary>
    /// <remarks>
    /// Towards, not away from: choosing a tab to the right brings the new one in from the right, so
    /// that the ribbon moves the way the hand did. A render transform and an opacity, neither of
    /// which the layout can see, so nothing is measured twice for it.
    /// </remarks>
    Slide,

    /// <summary>The tab arriving fades in where it stands.</summary>
    /// <remarks>The same length and the same reason, without the movement, for an application that wants the change marked and not pointed at.</remarks>
    Fade,

    /// <summary>Nothing: the new tab is simply there, in the next frame.</summary>
    None,
}

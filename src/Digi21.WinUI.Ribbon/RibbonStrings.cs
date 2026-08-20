namespace Digi21.WinUI.Ribbon;

/// <summary>Every sentence the ribbon puts in front of a user on its own account, in one place, so it can be translated.</summary>
/// <remarks>
/// <para>
/// The ribbon does not translate what an application puts in it: the name of a tab, of a group or of
/// an item arrives already in the user's language, because only the application knows what it is
/// saying. What is here is the handful of things the ribbon says by itself - what a screen reader is
/// told the launcher of a group does, what the button a folded group becomes is called - and that is
/// the ribbon's to translate.
/// </para>
/// <para>
/// Set these once, early, from wherever the application keeps its translations. They are static
/// because the text is built where there is no control to ask, and shared because an application
/// showing two ribbons in two languages at once is not a thing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// RibbonStrings.GroupLauncherNameFormat = "Opciones de {0}";
/// RibbonStrings.CollapsedGroupNameFormat = "{0}, grupo plegado";
/// </code>
/// </example>
public static class RibbonStrings
{
    /// <summary>Gets or sets what the launcher of a group announces itself as. Takes the group's name.</summary>
    /// <remarks>
    /// A screen reader reading "button" is being told nothing: every group's launcher looks the same
    /// and does something different, and the group's name is the only thing that tells them apart.
    /// </remarks>
    public static string GroupLauncherNameFormat { get; set; } = "{0} options";

    /// <summary>Gets or sets what the chevron announces itself as when pressing it drops the ribbon to one row.</summary>
    /// <remarks>What the chevron does depends on <see cref="RibbonCollapseBehavior"/>, so it has a sentence for each: a button announcing that it minimises a ribbon it is about to simplify is worse than one that says nothing.</remarks>
    public static string SimplifyRibbonName { get; set; } = "Simplify the ribbon";

    /// <summary>Gets or sets what the same chevron announces itself as once the ribbon is down to one row.</summary>
    public static string FullRibbonName { get; set; } = "Show the full ribbon";

    /// <summary>Gets or sets what the chevron that puts the ribbon away announces itself as.</summary>
    public static string MinimizeRibbonName { get; set; } = "Minimise the ribbon";

    /// <summary>Gets or sets what the same chevron announces itself as once the ribbon is put away.</summary>
    /// <remarks>Two sentences rather than one, because a button that does the opposite of what it says is a button nobody can use without looking.</remarks>
    public static string ExpandRibbonName { get; set; } = "Expand the ribbon";

    /// <summary>Gets or sets what a contextual tab announces itself as. Takes the tab's name.</summary>
    /// <remarks>
    /// The accent line above a contextual tab says that it is one, and says it only to somebody
    /// looking at the strip. What makes a contextual tab worth having is that it was not there a
    /// moment ago, and a screen reader that reads out its name and nothing else has left out the
    /// whole of that.
    /// </remarks>
    public static string ContextualTabNameFormat { get; set; } = "{0}, contextual tab";

    /// <summary>Gets or sets what the button a folded group becomes announces itself as. Takes the group's name.</summary>
    /// <remarks>
    /// Worth saying that it is folded rather than only naming it: the commands it holds are one press
    /// away rather than on the strip, and somebody looking for one of them needs to know that.
    /// </remarks>
    public static string CollapsedGroupNameFormat { get; set; } = "{0}, collapsed group";
}

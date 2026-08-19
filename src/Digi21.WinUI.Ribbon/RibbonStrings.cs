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

    /// <summary>Gets or sets what the button a folded group becomes announces itself as. Takes the group's name.</summary>
    /// <remarks>
    /// Worth saying that it is folded rather than only naming it: the commands it holds are one press
    /// away rather than on the strip, and somebody looking for one of them needs to know that.
    /// </remarks>
    public static string CollapsedGroupNameFormat { get; set; } = "{0}, collapsed group";
}

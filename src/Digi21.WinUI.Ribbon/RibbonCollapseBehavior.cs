namespace Digi21.WinUI.Ribbon;

/// <summary>What the ribbon does when the user asks for less of it.</summary>
/// <remarks>
/// <para>
/// There is one gesture for asking - the chevron in the corner, a double-click on a tab, and
/// <c>Ctrl+F1</c> - and this is what it means. One gesture with one meaning: a chevron that
/// simplified while the keyboard shortcut minimised would be two behaviours wearing one name.
/// </para>
/// <para>
/// It decides only what the gesture does. <see cref="Ribbon.DisplayMode"/> and
/// <see cref="Ribbon.IsMinimized"/> are ordinary properties whatever this says, so an application
/// that wants a menu item for the state the gesture does not reach can still write it.
/// </para>
/// </remarks>
public enum RibbonCollapseBehavior
{
    /// <summary>The ribbon drops to one row, and the gesture again brings it back. The default.</summary>
    /// <remarks>
    /// The commands are still there afterwards, which is the reason this is the default rather than
    /// the Office behaviour it replaces: a small chevron in a corner is easy to press by accident,
    /// and pressing it should not leave somebody looking at a window with no commands in it and no
    /// idea what they did.
    /// </remarks>
    Simplify,

    /// <summary>The ribbon is put away, leaving only its tabs, as in Office.</summary>
    Minimize,

    /// <summary>Nothing. There is no chevron, and the double-click and the shortcut do nothing.</summary>
    /// <remarks>For an application whose ribbon is the whole of its command surface and is meant to stay put.</remarks>
    None,
}

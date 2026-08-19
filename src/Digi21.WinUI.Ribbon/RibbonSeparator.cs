using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Ribbon;

/// <summary>A rule between two columns of a group.</summary>
/// <remarks>
/// It takes a column to itself, over the whole height of the group, which is what makes it read as a
/// division rather than as one more item in a stack. It has no label, no icon and no shapes to
/// choose between, and it is not focusable or reachable by an automated test, because there is
/// nothing there to do.
/// </remarks>
public partial class RibbonSeparator : Control
{
    /// <summary>Initializes a new instance of the <see cref="RibbonSeparator"/> class.</summary>
    public RibbonSeparator()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonSeparator);
        IsTabStop = false;
    }
}

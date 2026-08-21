using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>The coloured band drawn above a set of contextual tabs, carrying the name of what they are for.</summary>
/// <remarks>
/// <para>
/// One of these per <see cref="RibbonContextualGroup"/> a tab has been pointed at. The ribbon makes
/// them, the strip lays them out over the tabs of their group, and neither the application nor the
/// tabs hold one.
/// </para>
/// <para>
/// Out of the automation tree rather than in it. What it says is already said, better, by every tab
/// under it: a contextual tab in a group announces its own name and its group's, so a band read out
/// separately would be the same news twice with no way to tell that it was the same news.
/// </para>
/// </remarks>
public partial class RibbonContextualHeading : Control
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonContextualHeading), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Accent"/> dependency property.</summary>
    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(nameof(Accent), typeof(Brush), typeof(RibbonContextualHeading), new PropertyMetadata(null));

    /// <summary>Initializes a new instance of the <see cref="RibbonContextualHeading"/> class.</summary>
    public RibbonContextualHeading()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonContextualHeading);
        IsTabStop = false;

        // Kept out of the automation tree. Every tab under this band already announces the name on
        // it as part of its own, so a band read out separately is the same news twice - and twice is
        // worse than once for somebody who cannot see that the two are the same thing.
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
    }

    /// <summary>Gets or sets the name on the band.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Gets or sets the colour of the band.</summary>
    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    // Which group this band belongs to, so that the strip can find the tabs to draw it over.
    internal RibbonContextualGroup? Group { get; set; }

    // How much room this band's name asks for, measured against nothing. Kept because it is what the
    // tabs of the group are widened to fit, and because by the time they have been the band has been
    // measured again at the width they gave it - so this is the only place the number survives.
    internal double Natural { get; set; }
}

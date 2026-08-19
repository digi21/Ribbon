using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Ribbon;

/// <summary>An item in a group that opens a menu of its own.</summary>
/// <remarks>
/// A <see cref="DropDownButton"/>, so its flyout, its chevron, Esc closing it and the
/// <c>ExpandCollapsePattern</c> that says whether it is open come from WinUI.
/// </remarks>
public partial class RibbonDropDownButton : DropDownButton, IRibbonItem
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonDropDownButton), new PropertyMetadata(string.Empty, OnChromeChanged));

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(Microsoft.UI.Xaml.Controls.IconSource), typeof(RibbonDropDownButton), new PropertyMetadata(null, OnChromeChanged));

    private readonly RibbonItemChrome chrome;

    /// <summary>Initializes a new instance of the <see cref="RibbonDropDownButton"/> class.</summary>
    public RibbonDropDownButton()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonDropDownButton);

        // Without this the control gets no template at all, and a control with no template measures
        // nothing and is arranged into a column of zero width: present according to every count, and
        // invisible.
        //
        // DropDownButton is one of WinUI's own newer controls, and those look their default style up
        // in the dictionary DefaultStyleResourceUri names rather than in the application's resources.
        // Inherited unchanged, that points at WinUI's dictionary, where a type of ours does not
        // exist. Button and ToggleButton are older and resolve the classic way, which is why the
        // other items work without this line and this one did not.
        DefaultStyleResourceUri = new Uri("ms-appx:///Digi21.WinUI.Ribbon/Themes/Generic.xaml");
        chrome = new RibbonItemChrome(this, () => Label, () => IconSource);
        Ribbon.SetAllowedSizes(this, RibbonItemSizes.All);
    }

    /// <inheritdoc/>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <inheritdoc/>
    public IconSource? IconSource
    {
        get => (IconSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        chrome.Attach(GetTemplateChild("PART_Content") as RibbonItemContent);
    }

    private static void OnChromeChanged(DependencyObject item, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonDropDownButton)item).chrome.Update();
    }
}

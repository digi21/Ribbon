using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Digi21.WinUI.Ribbon;

/// <summary>A setting in a group that is either on or off.</summary>
/// <remarks>
/// A <see cref="ToggleButton"/>, so <c>IsChecked</c>, <c>Checked</c>, <c>Unchecked</c> and the
/// <c>TogglePattern</c> that tells an automated test which of the two it is in come from WinUI. That
/// is the reason these types have no base class of their own: a two-state item announced as a plain
/// button is one an application on top cannot test without going to screen coordinates.
/// </remarks>
public partial class RibbonToggleButton : ToggleButton, IRibbonItem
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonToggleButton), new PropertyMetadata(string.Empty, OnChromeChanged));

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(Microsoft.UI.Xaml.Controls.IconSource), typeof(RibbonToggleButton), new PropertyMetadata(null, OnChromeChanged));

    private readonly RibbonItemChrome chrome;

    /// <summary>Initializes a new instance of the <see cref="RibbonToggleButton"/> class.</summary>
    public RibbonToggleButton()
    {
        DefaultStyleKey = typeof(RibbonToggleButton);
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
        ((RibbonToggleButton)item).chrome.Update();
    }
}

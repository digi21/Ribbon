using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Ribbon;

/// <summary>A command in a group: click it and something happens.</summary>
/// <remarks>
/// A <see cref="Button"/>, so <c>Click</c>, <c>Command</c>, activation with Space and Enter and the
/// <c>InvokePattern</c> an automated test drives it by all come from WinUI rather than from here.
/// </remarks>
public partial class RibbonButton : Button, IRibbonItem
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonButton), new PropertyMetadata(string.Empty, OnChromeChanged));

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(Microsoft.UI.Xaml.Controls.IconSource), typeof(RibbonButton), new PropertyMetadata(null, OnChromeChanged));

    private readonly RibbonItemChrome chrome;

    /// <summary>Initializes a new instance of the <see cref="RibbonButton"/> class.</summary>
    public RibbonButton()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonButton);
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
        ((RibbonButton)item).chrome.Update();
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>The name of a tab in the strip along the top of the ribbon.</summary>
/// <remarks>
/// A <see cref="ButtonBase"/> for now, so that it is invokable and reachable from the keyboard
/// without any of that being written here. It should announce itself as one of a set of tabs rather
/// than as a button, which is a <c>SelectionItemPattern</c> and an automation peer of its own; that
/// is on the list for the keyboard work, not forgotten.
/// </remarks>
public partial class RibbonTabHeader : ButtonBase
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonTabHeader), new PropertyMetadata(string.Empty, OnLabelChanged));

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(RibbonTabHeader), new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>Initializes a new instance of the <see cref="RibbonTabHeader"/> class.</summary>
    public RibbonTabHeader()
    {
        DefaultStyleKey = typeof(RibbonTabHeader);
    }

    /// <summary>Gets or sets the name shown.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether this is the tab on show.</summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    internal RibbonTab? Tab { get; set; }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateSelectionState();
    }

    private static void OnLabelChanged(DependencyObject header, DependencyPropertyChangedEventArgs arguments)
    {
        AutomationProperties.SetName(header, (string)arguments.NewValue);
    }

    private static void OnIsSelectedChanged(DependencyObject header, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonTabHeader)header).UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        VisualStateManager.GoToState(this, IsSelected ? "Selected" : "Unselected", useTransitions: false);
    }
}

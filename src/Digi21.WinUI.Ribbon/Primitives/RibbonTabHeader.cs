using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>The name of a tab in the strip along the top of the ribbon.</summary>
/// <remarks>
/// <para>
/// The tab on show is marked by an accent line under its name, which grows when the pointer is on it
/// and rests shorter when it is not; a tab that is not on show darkens instead. That is the same
/// marking the docking library gives its tabs, and the same one WinUI's own navigation controls use,
/// so a window holding both looks like one window.
/// </para>
/// <para>
/// A <see cref="ButtonBase"/> for now, so that it is invokable and reachable from the keyboard
/// without any of that being written here. It should announce itself as one of a set of tabs rather
/// than as a button, which is a <c>SelectionItemPattern</c> and an automation peer of its own; that
/// is on the list for the keyboard work, not forgotten.
/// </para>
/// </remarks>
public partial class RibbonTabHeader : ButtonBase
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonTabHeader), new PropertyMetadata(string.Empty, OnLabelChanged));

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(RibbonTabHeader), new PropertyMetadata(false, OnIsSelectedChanged));

    private bool pointerOver;
    private bool pressed;

    /// <summary>Initializes a new instance of the <see cref="RibbonTabHeader"/> class.</summary>
    public RibbonTabHeader()
    {
        DefaultStyleKey = typeof(RibbonTabHeader);
        RibbonThemeResources.Ensure();
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

        // Without transitions the first time, so that a ribbon does not open with its indicator
        // sliding out from nothing.
        UpdateSelectionState(useTransitions: false);
        UpdatePointerState(useTransitions: false);
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        pointerOver = true;
        UpdatePointerState(useTransitions: true);
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        pointerOver = false;
        pressed = false;
        UpdatePointerState(useTransitions: true);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        pressed = true;
        UpdatePointerState(useTransitions: true);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        pressed = false;
        UpdatePointerState(useTransitions: true);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        pointerOver = false;
        pressed = false;
        UpdatePointerState(useTransitions: true);
    }

    private static void OnLabelChanged(DependencyObject header, DependencyPropertyChangedEventArgs arguments)
    {
        AutomationProperties.SetName(header, (string)arguments.NewValue);
    }

    private static void OnIsSelectedChanged(DependencyObject header, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonTabHeader)header).UpdateSelectionState(useTransitions: true);
    }

    // Two groups rather than four combined states, which is what lets them stay independent: the
    // pointer decides how long the line is and whether the tab darkens, and the selection decides
    // whether the line is there at all. Hovering a tab that is not on show therefore stretches a
    // line nobody can see, which costs nothing and keeps each group answerable to one question.
    private void UpdatePointerState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, pressed ? "Pressed" : pointerOver ? "PointerOver" : "Normal", useTransitions);
    }

    private void UpdateSelectionState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, IsSelected ? "Selected" : "Unselected", useTransitions);
    }
}

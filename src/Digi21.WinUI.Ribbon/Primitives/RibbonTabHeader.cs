using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
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
/// A contextual tab carries a second line, above its name and across the whole of it, in the accent
/// colour. Above rather than below because below is taken, and across the whole width rather than
/// inset because two contextual tabs side by side then draw one unbroken line - which is where the
/// coloured heading over a set of them goes, the day there is one.
/// </para>
/// <para>
/// A <see cref="ButtonBase"/>, so that pressing it and reaching it from the keyboard are WinUI's
/// rather than written here. It announces itself as a tab rather than as a button, through
/// <see cref="RibbonTabHeaderAutomationPeer"/> - which had to be written, because unlike
/// <c>Button</c> a <see cref="ButtonBase"/> brings no peer of its own and every tab of the ribbon
/// therefore answered to no pattern at all until the probe said so.
/// </para>
/// </remarks>
public partial class RibbonTabHeader : ButtonBase
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonTabHeader), new PropertyMetadata(string.Empty, OnNameChanged));

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(RibbonTabHeader), new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>Identifies the <see cref="IsContextual"/> dependency property.</summary>
    public static readonly DependencyProperty IsContextualProperty =
        DependencyProperty.Register(nameof(IsContextual), typeof(bool), typeof(RibbonTabHeader), new PropertyMetadata(false, OnIsContextualChanged));

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

    /// <summary>Gets or sets a value indicating whether this names a tab that comes and goes.</summary>
    public bool IsContextual
    {
        get => (bool)GetValue(IsContextualProperty);
        set => SetValue(IsContextualProperty, value);
    }

    internal RibbonTab? Tab { get; set; }

    // The ribbon this header belongs to, so that its peer can name the set it is one of. A tab item
    // whose selection container is nothing is a tab item with no set, which is most of what a driver
    // wants to know about a tab.
    internal Ribbon? Owner { get; set; }

    /// <summary>Occurs when this tab is chosen, by a click, by the keyboard or by an automated test.</summary>
    internal event EventHandler? Chosen;

    /// <summary>Chooses this tab, exactly as clicking it would.</summary>
    /// <remarks>The one door in, so that a driver goes through the same code a finger does rather than through a shortcut nobody else takes.</remarks>
    internal void Choose()
    {
        Chosen?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabHeaderAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Without transitions the first time, so that a ribbon does not open with its indicator
        // sliding out from nothing.
        UpdateSelectionState(useTransitions: false);
        UpdatePointerState(useTransitions: false);
        UpdateContextualState(useTransitions: false);
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

    private static void OnNameChanged(DependencyObject header, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonTabHeader)header).UpdateAutomationName();
    }

    private static void OnIsSelectedChanged(DependencyObject header, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonTabHeader)header).UpdateSelectionState(useTransitions: true);
    }

    private static void OnIsContextualChanged(DependencyObject header, DependencyPropertyChangedEventArgs arguments)
    {
        var self = (RibbonTabHeader)header;

        self.UpdateContextualState(useTransitions: false);
        self.UpdateAutomationName();
    }

    // Three groups rather than eight combined states, which is what lets them stay independent: the
    // pointer decides how long the line under the name is and whether the tab darkens, the selection
    // decides whether that line is there at all, and being contextual decides the line above the
    // name. Hovering a tab that is not on show therefore stretches a line nobody can see, which costs
    // nothing and keeps each group answerable to one question.
    private void UpdatePointerState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, pressed ? "Pressed" : pointerOver ? "PointerOver" : "Normal", useTransitions);
    }

    private void UpdateSelectionState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, IsSelected ? "Selected" : "Unselected", useTransitions);

        // Said out loud, because a contextual tab arriving and taking the strip is a thing that
        // happened to the user without the user doing it, and a driver that had to poll the tree to
        // notice would be back to asking screen coordinates what the ribbon is doing. Only when
        // somebody is listening: a peer built for nobody is a peer built for nothing.
        if (IsSelected && AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected))
        {
            FrameworkElementAutomationPeer.FromElement(this)?.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
        }
    }

    // No transitions, ever. A contextual tab is drawn as one from the moment it is on the strip, and
    // an accent line fading in over a tab that has only just appeared is two arrivals for one event.
    private void UpdateContextualState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, IsContextual ? "Contextual" : "Fixed", useTransitions);
    }

    // What a screen reader is told this tab is called. A contextual tab says so, for the same reason
    // a folded group does: what is worth knowing about it is not only its name but that it is here
    // now and was not a moment ago, and that is invisible to somebody who cannot see the strip change
    // length.
    private void UpdateAutomationName()
    {
        AutomationProperties.SetName(
            this,
            IsContextual
                ? string.Format(CultureInfo.CurrentCulture, RibbonStrings.ContextualTabNameFormat, Label)
                : Label);
    }
}

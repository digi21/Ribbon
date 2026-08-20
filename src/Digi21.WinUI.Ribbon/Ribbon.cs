using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Windows.System;

namespace Digi21.WinUI.Ribbon;

/// <summary>An Office-style ribbon: tabs of groups that give way as the window narrows.</summary>
/// <remarks>
/// <para>
/// Every tab is realized once, when it is added, and stays realized: the tab that is not showing is
/// collapsed, which the layout system skips, but its elements are the same ones it will show again.
/// That costs the memory of every tab from the start and buys the promise the whole library is
/// built on - that an application which keeps a reference to a control it put in a group can go on
/// using it, through a change of tab, through a group folding and through every relayout in between.
/// </para>
/// </remarks>
[ContentProperty(Name = nameof(Tabs))]
[TemplatePart(Name = TabStripPart, Type = typeof(Panel))]
[TemplatePart(Name = BodyPart, Type = typeof(Panel))]
public partial class Ribbon : Control
{
    /// <summary>Identifies the shapes an item accepts, attached so that any element can carry it.</summary>
    public static readonly DependencyProperty AllowedSizesProperty =
        DependencyProperty.RegisterAttached(
            "AllowedSizes",
            typeof(RibbonItemSizes),
            typeof(Ribbon),
            new PropertyMetadata(RibbonItemSizes.None, OnAllowedSizesChanged));

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(Ribbon),
            new PropertyMetadata(0, OnSelectedIndexChanged));

    // Written by the layout and read by the item, which is why the property itself is not public
    // while GetSize is: an application - or a probe - has every reason to ask an item what shape it
    // ended up in, and none to tell it.
    internal static readonly DependencyProperty SizeProperty =
        DependencyProperty.RegisterAttached(
            "Size",
            typeof(RibbonItemSize),
            typeof(Ribbon),
            new PropertyMetadata(RibbonItemSize.Normal));

    /// <summary>Identifies the <see cref="DisplayMode"/> dependency property.</summary>
    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register(
            nameof(DisplayMode),
            typeof(RibbonDisplayMode),
            typeof(Ribbon),
            new PropertyMetadata(RibbonDisplayMode.Full, OnDisplayModeChanged));

    /// <summary>Identifies the <see cref="CollapseBehavior"/> dependency property.</summary>
    public static readonly DependencyProperty CollapseBehaviorProperty =
        DependencyProperty.Register(
            nameof(CollapseBehavior),
            typeof(RibbonCollapseBehavior),
            typeof(Ribbon),
            new PropertyMetadata(RibbonCollapseBehavior.Simplify, OnCollapseBehaviorChanged));

    /// <summary>Identifies the <see cref="IsMinimized"/> dependency property.</summary>
    public static readonly DependencyProperty IsMinimizedProperty =
        DependencyProperty.Register(
            nameof(IsMinimized),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false, OnIsMinimizedChanged));

    private const string TabStripPart = "PART_TabStrip";
    private const string BodyPart = "PART_Body";
    private const string StripPart = "PART_Strip";
    private const string BodyHostPart = "PART_BodyHost";
    private const string MinimizePart = "PART_Minimize";
    private const string MinimizeGlyphPart = "PART_MinimizeGlyph";
    private const string OverlayPart = "PART_Overlay";
    private const string OverlayHostPart = "PART_OverlayHost";
    private const string ExpandPart = "PART_Expand";

    private readonly ObservableCollection<RibbonTab> tabs = [];
    private readonly List<RibbonTabHeader> headers = [];
    private readonly List<ButtonBase> invocations = [];

    private Panel? tabStrip;
    private Panel? body;
    private FrameworkElement? strip;
    private Border? bodyHost;
    private Button? minimize;
    private Button? expand;
    private FontIcon? minimizeGlyph;
    private Popup? overlay;
    private Border? overlayHost;

    /// <summary>Initializes a new instance of the <see cref="Ribbon"/> class.</summary>
    public Ribbon()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(Ribbon);
        tabs.CollectionChanged += OnTabsChanged;

        // Office's shortcut, and the one anybody who has used a ribbon will try.
        var shortcut = new KeyboardAccelerator { Key = VirtualKey.F1, Modifiers = VirtualKeyModifiers.Control };
        shortcut.Invoked += (_, arguments) =>
        {
            Collapse();
            arguments.Handled = true;
        };

        KeyboardAccelerators.Add(shortcut);
    }

    /// <summary>Occurs when the tab on show changes.</summary>
    public event TypedEventHandler<Ribbon, object>? SelectionChanged;

    /// <summary>Gets the tabs, in the order they are shown.</summary>
    public IList<RibbonTab> Tabs => tabs;

    /// <summary>Gets or sets the index of the tab on show.</summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Gets or sets how much of itself the ribbon draws: three rows to a group, or one.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="RibbonDisplayMode.Simplified"/> is Office's simplified ribbon: one row, no group
    /// names, and every item beside its label or down to its icon. What does not fit folds into the
    /// group's button exactly as it does in a full ribbon squeezed hard, and a group holding
    /// something that cannot be drawn in one row is its button at every width - with everything it
    /// holds laid out in the flyout the way a full ribbon would lay it out.
    /// </para>
    /// <para>
    /// Independent of <see cref="IsMinimized"/>, which is about whether the ribbon is on show at
    /// all. Both are ordinary two-way properties, so an application can save what the user chose
    /// and put it back on the next run.
    /// </para>
    /// </remarks>
    public RibbonDisplayMode DisplayMode
    {
        get => (RibbonDisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    /// <summary>Gets or sets what the chevron, a double-click on a tab and <c>Ctrl+F1</c> do. Simplifying, out of the box.</summary>
    /// <remarks>
    /// <para>
    /// One gesture with one meaning. Out of the box it drops the ribbon to
    /// <see cref="RibbonDisplayMode.Simplified"/> and brings it back, which leaves the commands on
    /// screen: a chevron in a corner is easy to press by accident, and pressing one should not leave
    /// somebody in front of a window with no commands in it.
    /// </para>
    /// <para>
    /// <see cref="RibbonCollapseBehavior.Minimize"/> is the Office behaviour - the ribbon goes away
    /// and leaves its tabs - and <see cref="RibbonCollapseBehavior.None"/> takes the chevron off
    /// altogether. Either way <see cref="DisplayMode"/> and <see cref="IsMinimized"/> stay writable,
    /// so an application can offer the state the gesture does not reach from a menu of its own.
    /// </para>
    /// </remarks>
    public RibbonCollapseBehavior CollapseBehavior
    {
        get => (RibbonCollapseBehavior)GetValue(CollapseBehaviorProperty);
        set => SetValue(CollapseBehaviorProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the ribbon is put away, leaving only its tabs.</summary>
    /// <remarks>
    /// <para>
    /// An ordinary two-way property so that an application can save it with the rest of its settings
    /// and put it back on the next run. A user who puts the ribbon away and finds it open again
    /// every morning will stop putting it away.
    /// </para>
    /// <para>
    /// Clicking a tab while the ribbon is minimised opens it <em>over</em> the content rather than
    /// pushing the content down, and that opening is transient: it goes away on a command, on a
    /// click elsewhere and on Esc, and it does not change this property. What the user asked for was
    /// to see a tab, not to bring the ribbon back.
    /// </para>
    /// </remarks>
    public bool IsMinimized
    {
        get => (bool)GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    /// <summary>Gets the tab on show, or <see langword="null"/> when there are no tabs.</summary>
    public RibbonTab? SelectedTab =>
        SelectedIndex >= 0 && SelectedIndex < tabs.Count ? tabs[SelectedIndex] : null;

    /// <summary>Reads the shapes an item accepts.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The shapes it accepts, or <see cref="RibbonItemSizes.None"/> when it has not said.</returns>
    public static RibbonItemSizes GetAllowedSizes(UIElement item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (RibbonItemSizes)item.GetValue(AllowedSizesProperty);
    }

    /// <summary>Declares the shapes an item accepts.</summary>
    /// <param name="item">The item.</param>
    /// <param name="value">The shapes it accepts.</param>
    public static void SetAllowedSizes(UIElement item, RibbonItemSizes value)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.SetValue(AllowedSizesProperty, value);
    }

    /// <summary>Reads the shape the layout gave an item.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The shape it is being drawn in.</returns>
    /// <remarks>Set by the ribbon as it lays out; an application reads it, and a test harness reads it to check it.</remarks>
    public static RibbonItemSize GetSize(UIElement item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (RibbonItemSize)item.GetValue(SizeProperty);
    }

    internal static void SetSize(UIElement item, RibbonItemSize value)
    {
        item.SetValue(SizeProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        tabStrip = GetTemplateChild(TabStripPart) as Panel;
        body = GetTemplateChild(BodyPart) as Panel;
        strip = GetTemplateChild(StripPart) as FrameworkElement;
        bodyHost = GetTemplateChild(BodyHostPart) as Border;
        minimize = GetTemplateChild(MinimizePart) as Button;
        minimizeGlyph = GetTemplateChild(MinimizeGlyphPart) as FontIcon;
        overlay = GetTemplateChild(OverlayPart) as Popup;
        overlayHost = GetTemplateChild(OverlayHostPart) as Border;

        if (minimize is not null)
        {
            minimize.Click += (_, _) => Collapse();
        }

        if (GetTemplateChild(ExpandPart) is Button found)
        {
            expand = found;

            // Only ever one thing, so it needs no glyph swapping and no second sentence.
            expand.Click += (_, _) => IsMinimized = false;
            AutomationProperties.SetName(expand, RibbonStrings.ExpandRibbonName);
            ToolTipService.SetToolTip(expand, RibbonStrings.ExpandRibbonName);
        }

        if (overlay is not null)
        {
            overlay.Closed += OnOverlayClosed;
        }

        Rebuild();
        UpdateMinimizedState();
    }

    private static void OnAllowedSizesChanged(DependencyObject item, DependencyPropertyChangedEventArgs arguments)
    {
        // The set of shapes an item accepts is an input to the layout, so changing it has to make
        // the strip decide again.
        if (item is FrameworkElement element)
        {
            element.InvalidateMeasure();
        }
    }

    private static void OnSelectedIndexChanged(DependencyObject ribbon, DependencyPropertyChangedEventArgs arguments)
    {
        ((Ribbon)ribbon).ShowSelectedTab();
    }

    private void OnTabsChanged(object? sender, NotifyCollectionChangedEventArgs arguments)
    {
        Rebuild();
    }

    // Rebuilds the strip of headers and the body from the tabs.
    //
    // The tabs themselves are moved rather than recreated - they are the elements the application
    // put its controls into - so a tab added to the collection later joins the ones already there
    // without disturbing them.
    private void Rebuild()
    {
        if (tabStrip is null || body is null)
        {
            return;
        }

        tabStrip.Children.Clear();
        headers.Clear();

        foreach (RibbonTab tab in tabs)
        {
            var header = new RibbonTabHeader { Label = tab.Label, Tab = tab };
            // A click goes through the same door a driver does, rather than each having its own.
            header.Click += (sender, _) => ((RibbonTabHeader)sender).Choose();
            header.Chosen += OnHeaderChosen;
            header.DoubleTapped += OnHeaderDoubleTapped;

            headers.Add(header);
            tabStrip.Children.Add(header);

            if (!body.Children.Contains(tab))
            {
                body.Children.Add(tab);
            }
        }

        for (int i = body.Children.Count - 1; i >= 0; i--)
        {
            if (body.Children[i] is RibbonTab existing && !tabs.Contains(existing))
            {
                body.Children.RemoveAt(i);
            }
        }

        UpdateDisplayMode();
        ShowSelectedTab();
    }

    private static void OnIsMinimizedChanged(DependencyObject ribbon, DependencyPropertyChangedEventArgs arguments)
    {
        ((Ribbon)ribbon).UpdateMinimizedState();
    }

    private static void OnDisplayModeChanged(DependencyObject ribbon, DependencyPropertyChangedEventArgs arguments)
    {
        ((Ribbon)ribbon).UpdateDisplayMode();
    }

    // Hands the row count down to the tabs, which hand it on to their groups. Nothing is rebuilt on
    // the way: the mode changes what the strip is laid out in, and an application that put a control
    // in a group is holding the same control afterwards.
    private void UpdateDisplayMode()
    {
        int rows = DisplayMode == RibbonDisplayMode.Simplified ? 1 : RibbonMetrics.MaxRows;

        foreach (RibbonTab tab in tabs)
        {
            tab.Rows = rows;
        }

        UpdateChevron();
    }

    private void OnHeaderChosen(object? sender, EventArgs arguments)
    {
        if (sender is not RibbonTabHeader { Tab: { } tab })
        {
            return;
        }

        bool already = ReferenceEquals(tab, SelectedTab);
        SelectedIndex = tabs.IndexOf(tab);

        if (!IsMinimized)
        {
            return;
        }

        // Clicking the tab that is already showing over the content puts it away again, which is the
        // only gesture that would otherwise do nothing at all.
        if (already && overlay is { IsOpen: true })
        {
            overlay.IsOpen = false;
        }
        else
        {
            ShowOverlay();
        }
    }

    private void OnHeaderDoubleTapped(object sender, RoutedEventArgs arguments)
    {
        Collapse();
    }

    // The one gesture, and the only place that decides what it means. Three ways in - the chevron,
    // a double-click on a tab, Ctrl+F1 - and one thing out, because a chevron that simplified while
    // the shortcut minimised would be two behaviours wearing one name.
    private void Collapse()
    {
        switch (CollapseBehavior)
        {
            case RibbonCollapseBehavior.Simplify:
                DisplayMode = DisplayMode == RibbonDisplayMode.Simplified
                    ? RibbonDisplayMode.Full
                    : RibbonDisplayMode.Simplified;
                break;

            case RibbonCollapseBehavior.Minimize:
                IsMinimized = !IsMinimized;
                break;

            default:
                break;
        }
    }

    private static void OnCollapseBehaviorChanged(DependencyObject ribbon, DependencyPropertyChangedEventArgs arguments)
    {
        ((Ribbon)ribbon).UpdateChevron();
    }

    // The chevron says what the gesture will do, which depends on what the gesture means and on
    // where the ribbon is now. Four sentences and two glyphs for what looks like one button, because
    // a button that does the opposite of what it says is a button nobody can use without looking.
    private void UpdateChevron()
    {
        if (minimize is null)
        {
            return;
        }

        if (CollapseBehavior == RibbonCollapseBehavior.None)
        {
            minimize.Visibility = Visibility.Collapsed;
            return;
        }

        minimize.Visibility = Visibility.Visible;

        bool collapsed = CollapseBehavior == RibbonCollapseBehavior.Minimize
            ? IsMinimized
            : DisplayMode == RibbonDisplayMode.Simplified;

        if (minimizeGlyph is not null)
        {
            // Pointing the way it will move: down to bring the ribbon back, up to take it away.
            minimizeGlyph.Glyph = collapsed ? "" : "";
        }

        string name = CollapseBehavior == RibbonCollapseBehavior.Minimize
            ? collapsed ? RibbonStrings.ExpandRibbonName : RibbonStrings.MinimizeRibbonName
            : collapsed ? RibbonStrings.FullRibbonName : RibbonStrings.SimplifyRibbonName;

        AutomationProperties.SetName(minimize, name);
        ToolTipService.SetToolTip(minimize, name);
    }

    private void UpdateMinimizedState()
    {
        UpdateChevron();

        if (overlay is { IsOpen: true })
        {
            overlay.IsOpen = false;
        }

        if (bodyHost is not null)
        {
            bodyHost.Visibility = IsMinimized ? Visibility.Collapsed : Visibility.Visible;
        }

        // The chevron that puts the ribbon away lives in the body and goes with it, so a ribbon that
        // is away would otherwise offer no way back that anybody can see: the double-click on a tab
        // and Ctrl+F1 are both things you have to already know. This is the one that can be found by
        // looking.
        if (expand is not null)
        {
            expand.Visibility = IsMinimized ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // Moves the body over the content. The same element, moved rather than rebuilt, for the reason
    // every move in this library is a move: what an application put in a group has to be the same
    // object afterwards.
    private void ShowOverlay()
    {
        if (overlay is null || overlayHost is null || bodyHost is null || body is null)
        {
            return;
        }

        // Opened at the ribbon's own width, so that the layout decides with the number it would have
        // had inline. There is one measuring path and this is not a second one.
        overlayHost.Width = ActualWidth;

        if (!ReferenceEquals(overlayHost.Child, body))
        {
            bodyHost.Child = null;
            overlayHost.Child = body;
        }

        overlay.XamlRoot = XamlRoot;
        overlay.VerticalOffset = strip?.ActualHeight ?? 0;
        overlay.IsOpen = true;

        // The third way out. Light dismissal covers a click elsewhere and Esc; a command invoked
        // inside the ribbon is the one the popup cannot see.
        foreach (RibbonGroup group in SelectedTab?.Groups ?? [])
        {
            foreach (UIElement item in group.Items)
            {
                if (item is ButtonBase button)
                {
                    button.Click += OnItemInvoked;
                    invocations.Add(button);
                }
            }
        }
    }

    private void OnItemInvoked(object sender, RoutedEventArgs arguments)
    {
        if (overlay is not null)
        {
            overlay.IsOpen = false;
        }
    }

    private void OnOverlayClosed(object? sender, object arguments)
    {
        foreach (ButtonBase button in invocations)
        {
            button.Click -= OnItemInvoked;
        }

        invocations.Clear();

        // Home again, and collapsed if the ribbon is still minimised. Left in the popup it would be
        // a tab nobody can reach and a set of controls the application still holds references to.
        if (overlayHost is not null && bodyHost is not null && body is not null && ReferenceEquals(overlayHost.Child, body))
        {
            overlayHost.Child = null;
            bodyHost.Child = body;
        }
    }

    private void ShowSelectedTab()
    {
        RibbonTab? selected = SelectedTab;

        foreach (RibbonTab tab in tabs)
        {
            // Collapsed rather than removed: the layout system skips it, and the controls inside it
            // are still the ones the application is holding on to.
            tab.Visibility = ReferenceEquals(tab, selected) ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (RibbonTabHeader header in headers)
        {
            header.IsSelected = ReferenceEquals(header.Tab, selected);
        }

        SelectionChanged?.Invoke(this, selected!);
    }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Digi21.WinUI.Ribbon.Layout;
using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;

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
/// <para>
/// A contextual tab is realized on the same terms and kept on the same terms. What
/// <see cref="RibbonTab.IsActive"/> takes away is its header, not the tab: the groups, the items and
/// the application's references to them all outlive it being off the strip, so a tab that comes and
/// goes twenty times a minute costs one build and no more.
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

    /// <summary>Identifies the <see cref="TabTransition"/> dependency property.</summary>
    public static readonly DependencyProperty TabTransitionProperty =
        DependencyProperty.Register(
            nameof(TabTransition),
            typeof(RibbonTabTransition),
            typeof(Ribbon),
            new PropertyMetadata(RibbonTabTransition.Slide));

    private const string TabStripPart = "PART_TabStrip";
    private const string BodyPart = "PART_Body";
    private const string StripPart = "PART_Strip";
    private const string BodyHostPart = "PART_BodyHost";
    private const string MinimizePart = "PART_Minimize";
    private const string MinimizeGlyphPart = "PART_MinimizeGlyph";
    private const string OverlayPart = "PART_Overlay";
    private const string OverlayHostPart = "PART_OverlayHost";
    private const string ExpandPart = "PART_Expand";

    // The way to ask Windows about animations. The object is expensive to make and cheap to ask, so
    // there is one of it for the whole application and the question is put to it every time.
    private static UISettings? settings;

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

    // The transition being drawn, held so that a change of tab arriving on top of one still being
    // drawn can stop it. Stopped rather than left to finish, because what it animates then goes back
    // to what the tab was born with - in its place and opaque - which is where a tab that is not
    // moving belongs.
    private Storyboard? motion;

    // The tab currently on show, held as the tab and not as its index. An index into a collection
    // that changes under it is a different tab tomorrow, and the whole of the contextual machinery
    // is about a collection that changes under it.
    private RibbonTab? showing;

    // Set while the ribbon is putting SelectedIndex back to a legal value, so that the write does not
    // start a second pass through the same code. One place decides which tab is shown, and it is
    // ShowSelectedTab; this is what stops it from being re-entered halfway down.
    private bool selecting;

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

        // The rest of the keyboard, which is about the focus rather than about one shortcut: where
        // Tab comes in, what the arrows do once it is in, and where Esc sends it back to.
        ConfigureKeyboard();
    }

    /// <summary>Occurs when the tab on show changes.</summary>
    public event TypedEventHandler<Ribbon, object>? SelectionChanged;

    /// <summary>Occurs when a tab arrives on the strip, after the ribbon has decided which tab is showing.</summary>
    /// <remarks>
    /// <para>
    /// Raised after the strip has been rebuilt and after any move to the new tab, so a handler asking
    /// <see cref="SelectedTab"/> is told where the ribbon ended up rather than where it was. The
    /// ribbon also says so in UI Automation, which is the same news for a driver that is out of
    /// process and has no event of this kind to hang off.
    /// </para>
    /// </remarks>
    public event TypedEventHandler<Ribbon, RibbonTab>? TabActivated;

    /// <summary>Occurs when a tab leaves the strip, after the ribbon has decided which tab is showing instead.</summary>
    public event TypedEventHandler<Ribbon, RibbonTab>? TabDeactivated;

    /// <summary>Gets the tabs, in the order they are shown.</summary>
    /// <remarks>
    /// <para>
    /// The order here is the order in the strip, contextual tabs included: a tab that comes and goes
    /// appears in the gap it left rather than being moved to the end, so that the tab an application
    /// declared third is the third one drawn whenever it is drawn at all. Declare a contextual tab
    /// last if it should sit on the right, as Office puts them.
    /// </para>
    /// <para>
    /// <see cref="SelectedIndex"/> indexes this collection, and it indexes all of it. A tab switched
    /// off does not shift the ones after it.
    /// </para>
    /// </remarks>
    public IList<RibbonTab> Tabs => tabs;

    /// <summary>Gets or sets the index into <see cref="Tabs"/> of the tab on show.</summary>
    /// <remarks>
    /// <para>
    /// Only a tab that is on the strip can be shown. Setting this to a tab whose
    /// <see cref="RibbonTab.IsActive"/> is <see langword="false"/> leaves the ribbon where it was and
    /// puts the property back, rather than throwing or showing nothing: the set of tabs on the strip
    /// changes while an application runs, so asking for one that has just gone is a race and not a
    /// mistake.
    /// </para>
    /// <para>
    /// It reads back as <c>-1</c> only when there is no tab to show at all - a ribbon with no tabs,
    /// or one whose every tab is contextual and none of them switched on.
    /// </para>
    /// </remarks>
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

    /// <summary>Gets or sets how the change from one tab to the next is drawn. Sliding, out of the box.</summary>
    /// <remarks>
    /// <para>
    /// Chrome over a change that has already happened: the tab is chosen, laid out and hit-testable
    /// before the first frame of the transition is drawn, and clicking a command while one is running
    /// invokes it. It is a render transform and an opacity, so the layout neither sees it nor is run
    /// again for it.
    /// </para>
    /// <para>
    /// Whatever this says, the ribbon cuts when Windows has been told to show no animations. It also
    /// cuts when a minimised ribbon opens a tab over the content, because the popup that carries it
    /// arrives with an animation of its own and two arrivals for one click is one too many.
    /// </para>
    /// </remarks>
    public RibbonTabTransition TabTransition
    {
        get => (RibbonTabTransition)GetValue(TabTransitionProperty);
        set => SetValue(TabTransitionProperty, value);
    }

    /// <summary>Gets the tab on show, or <see langword="null"/> when there is no tab to show.</summary>
    public RibbonTab? SelectedTab =>
        SelectedIndex >= 0 && SelectedIndex < tabs.Count ? tabs[SelectedIndex] : null;

    // The header of the tab on show, for the peer that has to hand it back as the selection.
    internal RibbonTabHeader? SelectedHeader => headers.FirstOrDefault(header => header.IsSelected);

    // Whether Windows is showing animations at all. Asked of the system rather than guessed at from
    // the theme, and asked every time rather than cached, because a user who switches them off
    // switches them off while the application is running and does not expect to have to restart it.
    private static bool Animations
    {
        get
        {
            try
            {
                settings ??= new UISettings();

                return settings.AnimationsEnabled;
            }
            catch (Exception)
            {
                // A decoration is not worth taking a window down over. Whatever went wrong here, the
                // ribbon draws its transition and the user sees a ribbon rather than a crash.
                return true;
            }
        }
    }

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
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonAutomationPeer(this);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // Before the template is measured, so that the tab on show is measured with the height the
        // whole ribbon has already settled on rather than with its own.
        LevelTabs();

        return base.MeasureOverride(availableSize);
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

        if (body is Panel host)
        {
            // Esc, listened for on the body rather than on the ribbon: a minimised ribbon keeps the
            // body in a popup, and a key pressed in there routes up as far as the body and no
            // further.
            host.KeyDown += OnBodyKeyDown;

            // A tab arriving comes from beside its place, and beside its place is outside the ribbon:
            // without this, the first frames of it are drawn past the ribbon's own edge and over
            // whatever the window has put there. Kept up to date rather than set once, because the
            // body is resized by every drag of the window border and is moved into the popup whole
            // when the ribbon is minimised.
            host.SizeChanged += (_, arguments) => host.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, arguments.NewSize.Width, arguments.NewSize.Height),
            };
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
        var self = (Ribbon)ribbon;

        // The write that put the property back is not a request to change tab; it is the tail of the
        // request that is already being served.
        if (!self.selecting)
        {
            self.ShowSelectedTab();
        }
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
            // Taken here and given up below, which is what lets a tab tell the ribbon that it has
            // been switched on or renamed. A tab nobody has added to a ribbon has nobody to tell.
            tab.Owner = this;

            var header = new RibbonTabHeader
            {
                Label = tab.Label,
                IsContextual = tab.IsContextual,
                Tab = tab,
                Owner = this,
            };

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
                // Both ends of the reference, so that a tab an application takes back out of the
                // ribbon and holds on to cannot go on driving a strip it is no longer part of.
                existing.Owner = null;
                existing.Restore = null;
                body.Children.RemoveAt(i);
            }
        }

        UpdateActivation();
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
            CloseOverlay();
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

    // One height for the whole ribbon, and every tab laid out at it.
    //
    // A ribbon is a strip with the whole window under it, so a strip that is four pixels taller for
    // one tab than for another moves everything below it when a tab is chosen - and thirty pixels
    // taller when one tab holds a stack of controls and another holds buttons. Every tab is asked
    // what it needs, including the ones not showing, and every tab is given the largest answer.
    //
    // Asked rather than measured, because a tab that is not showing is collapsed and a collapsed
    // element measures as nothing however directly it is asked. What a group needs is arithmetic
    // over the heights of its items, and those it can measure whether it is on show or not.
    //
    // What a group needs is also read with every group open, so the height does not move when a
    // group folds either: a ribbon that changed height as the window narrowed would be the same
    // fault wearing a different hat.
    private void LevelTabs()
    {
        double height = 0;

        foreach (RibbonTab tab in tabs)
        {
            height = Math.Max(height, tab.RequiredHeight);
        }

        foreach (RibbonTab tab in tabs)
        {
            tab.MinHeight = height;
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

        CloseOverlay();

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
        CloseOverlay();
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

    // A tab has been switched on or off. The one place that decides what that means.
    //
    // Everything here happens before the event is raised, so that a handler asking the ribbon what is
    // showing is told where it ended up rather than where it was on the way.
    internal void OnTabActivationChanged(RibbonTab tab)
    {
        // First, because everything below reads the strip back and a header that is still on it is a
        // tab the ribbon would happily go on showing.
        UpdateActivation();

        if (tab.IsActive)
        {
            // Stepping forward is what makes a contextual tab worth having over a row of greyed-out
            // buttons: it arrives at the moment its commands start working, and it says so by being
            // the tab in front of you. A fixed tab shown again stays where it is put.
            Select(tab.IsContextual && tab.SelectsWhenActivated ? tabs.IndexOf(tab) : SelectedIndex);

            TabActivated?.Invoke(this, tab);
        }
        else
        {
            bool wasShowing = ReferenceEquals(tab, showing);

            // Read before the move and cleared after it, so that the tab holds no reference to a tab
            // it is no longer coming back from.
            RibbonTab? back = tab.Restore;
            tab.Restore = null;

            // The overlay was opened over the application's content to show one tab, and that tab has
            // gone. It closes with it rather than swapping in a tab nobody asked to see.
            if (wasShowing && overlay is { IsOpen: true })
            {
                CloseOverlay();
            }

            Select(wasShowing
                ? RibbonTabSelection.Legalize(Activity(), back is null ? -1 : tabs.IndexOf(back), -1)
                : SelectedIndex);

            TabDeactivated?.Invoke(this, tab);
        }

        // Said in UI Automation as well as in CLR, because a driver out of process has no event of
        // the kind above to hang off, and the alternative is polling the tree for a tab that may
        // never come.
        AnnounceStrip();
    }

    // A tab has been renamed, or has changed from fixed to contextual. Neither touches the layout;
    // both change what a header draws and what it is called.
    internal void OnTabChromeChanged(RibbonTab tab)
    {
        foreach (RibbonTabHeader header in headers)
        {
            if (ReferenceEquals(header.Tab, tab))
            {
                header.Label = tab.Label;
                header.IsContextual = tab.IsContextual;
            }
        }
    }

    // Which tabs are on the strip, in the order they are declared in, which is the order they are
    // drawn in: a contextual tab reappears in the gap it left rather than at the end.
    private bool[] Activity()
    {
        var active = new bool[tabs.Count];

        for (int i = 0; i < tabs.Count; i++)
        {
            active[i] = tabs[i].IsActive;
        }

        return active;
    }

    // Puts the headers of the tabs that are not on the strip out of sight.
    //
    // Collapsed rather than hidden, and for more than the layout: a collapsed element is not in the
    // UI Automation tree either, so a tab that is not on the strip is a tab a driver cannot find,
    // rather than one it can find and cannot press. The header itself is kept - it is the same object
    // when the tab comes back, as everything else in this library is.
    private void UpdateActivation()
    {
        foreach (RibbonTabHeader header in headers)
        {
            header.Visibility = header.Tab is { IsActive: true } ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // Asks for a tab, and makes sure the asking is followed through even when the answer is the tab
    // that is already showing: setting a dependency property to the value it already has raises no
    // callback, and the strip still has to be redrawn around whatever has just changed under it.
    private void Select(int index)
    {
        if (SelectedIndex == index)
        {
            ShowSelectedTab();
            return;
        }

        SelectedIndex = index;
    }

    private void AnnounceStrip()
    {
        if (!AutomationPeer.ListenerExists(AutomationEvents.StructureChanged))
        {
            return;
        }

        (FrameworkElementAutomationPeer.FromElement(this) ?? FrameworkElementAutomationPeer.CreatePeerForElement(this))
            ?.RaiseAutomationEvent(AutomationEvents.StructureChanged);
    }

    private void ShowSelectedTab()
    {
        // The one gate every request for a tab goes through, whoever made it: a click, a driver, an
        // application setting the index, or a tab going off the strip from under the user. What comes
        // out is a tab that is actually on the strip, or nothing when there is no such tab.
        int wanted = SelectedIndex;

        // Where the ribbon is standing, read before anything moves. It is two things at once: what
        // the ribbon falls back to when the tab asked for cannot be shown, and - further down - which
        // side the tab arriving comes from.
        int from = showing is null ? -1 : tabs.IndexOf(showing);
        int allowed = RibbonTabSelection.Legalize(Activity(), wanted, from);

        if (allowed != wanted)
        {
            // Written back rather than only acted on, so that SelectedIndex never reads as a tab the
            // ribbon is not showing. An application reads this property.
            selecting = true;
            SelectedIndex = allowed;
            selecting = false;
        }

        RibbonTab? selected = allowed >= 0 && allowed < tabs.Count ? tabs[allowed] : null;

        // Where this tab was chosen from, remembered on the tab itself so that a second contextual
        // tab arriving over the first goes back to the first rather than past it.
        if (selected is not null && !ReferenceEquals(selected, showing))
        {
            selected.Restore = showing;
        }

        showing = selected;

        foreach (RibbonTab tab in tabs)
        {
            // Collapsed rather than removed: the layout system skips it, and the controls inside it
            // are still the ones the application is holding on to.
            tab.Visibility = ReferenceEquals(tab, selected) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Which header wears the mark of the tab on show, and which one the keyboard stands on -
        // one question, because the answer is the same header and the strip has only ever one of
        // each. It also carries the focus across when the strip is where the focus already was.
        MarkHeaders(selected);

        // Last, and after the visibilities on purpose: what is drawn moving is a tab that is already
        // there, already laid out and already answering to a click. Nothing waits for it.
        Animate(selected, from, allowed);

        SelectionChanged?.Invoke(this, selected!);
    }

    // Draws the change of tab, when there is a change and anybody wants it drawn.
    //
    // A render transform and an opacity, neither of which the layout system can see: a width or a
    // margin animated here would re-measure the strip sixty times a second, and the strip is where
    // the whole ribbon decides what fits. The tab arriving is the only one that moves - the one
    // leaving is collapsed in the same pass, as it always was - because a tab fading out is a tab
    // still on screen, and a transition interrupted halfway would leave it there.
    private void Animate(RibbonTab? tab, int from, int to)
    {
        // Whatever was being drawn belongs to a change the user has already moved past. Stopping it
        // puts what it animates back to what the tab was born with, which is where a tab that is not
        // moving belongs - including the tab this one is replacing, which would otherwise be left
        // standing wherever it had got to.
        motion?.Stop();
        motion = null;

        if (tab is null)
        {
            return;
        }

        // The popup a minimised ribbon opens a tab in animates itself, and this would be the second
        // arrival for one click. A tab chosen while that popup is already open is an ordinary change
        // of tab and is drawn like one.
        bool opening = IsMinimized && overlay is not { IsOpen: true };

        if (RibbonTabMotion.Entry(TabTransition, Animations, opening, from, to) is not double entry)
        {
            return;
        }

        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = RibbonTabMotion.Duration,
        };

        Storyboard.SetTarget(fade, tab);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        if (entry != 0)
        {
            // A new transform each time rather than one reused: it is what the storyboard is stopped
            // back to, so it has to start where a tab that is not moving stands.
            var move = new TranslateTransform();
            tab.RenderTransform = move;

            var slide = new DoubleAnimation
            {
                From = entry,
                To = 0,
                Duration = RibbonTabMotion.Duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };

            Storyboard.SetTarget(slide, move);
            Storyboard.SetTargetProperty(slide, "X");
            storyboard.Children.Add(slide);
        }

        motion = storyboard;
        storyboard.Begin();
    }
}

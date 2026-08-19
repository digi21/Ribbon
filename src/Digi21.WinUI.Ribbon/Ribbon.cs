using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

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

    private const string TabStripPart = "PART_TabStrip";
    private const string BodyPart = "PART_Body";

    private readonly ObservableCollection<RibbonTab> tabs = [];
    private readonly List<RibbonTabHeader> headers = [];

    private Panel? tabStrip;
    private Panel? body;

    /// <summary>Initializes a new instance of the <see cref="Ribbon"/> class.</summary>
    public Ribbon()
    {
        DefaultStyleKey = typeof(Ribbon);
        tabs.CollectionChanged += OnTabsChanged;
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

        Rebuild();
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
            header.Click += OnHeaderClick;

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

        ShowSelectedTab();
    }

    private void OnHeaderClick(object sender, RoutedEventArgs arguments)
    {
        if (sender is RibbonTabHeader { Tab: { } tab })
        {
            SelectedIndex = tabs.IndexOf(tab);
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

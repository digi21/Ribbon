using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Digi21.WinUI.Ribbon.Layout;
using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Digi21.WinUI.Ribbon;

/// <summary>A named block of related items inside a tab.</summary>
/// <remarks>
/// <para>
/// When the width runs out, a group folds into a button carrying its icon and its name, and the
/// whole group opens from that button. It never leaves the strip: a command drawn off the edge can
/// be reached by widening the window, and one that has been taken away cannot be reached at all.
/// </para>
/// <para>
/// Folding moves the panel holding the items into the button's flyout and back again. Nothing is
/// rebuilt, so the item an application put here is the same object before, during and after.
/// </para>
/// </remarks>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = ItemsHostPart, Type = typeof(Border))]
[TemplatePart(Name = ItemsPart, Type = typeof(RibbonItemsPanel))]
[TemplatePart(Name = LabelPart, Type = typeof(TextBlock))]
[TemplatePart(Name = CollapsedPart, Type = typeof(Button))]
[TemplatePart(Name = CollapsedContentPart, Type = typeof(RibbonItemContent))]
[TemplatePart(Name = LauncherPart, Type = typeof(Button))]
public partial class RibbonGroup : Control
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonGroup), new PropertyMetadata(string.Empty, OnChromeChanged));

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(Microsoft.UI.Xaml.Controls.IconSource), typeof(RibbonGroup), new PropertyMetadata(null, OnChromeChanged));

    /// <summary>Identifies the <see cref="Priority"/> dependency property.</summary>
    public static readonly DependencyProperty PriorityProperty =
        DependencyProperty.Register(nameof(Priority), typeof(int), typeof(RibbonGroup), new PropertyMetadata(0, OnLayoutChanged));

    /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property. Written by the ribbon.</summary>
    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasLauncher"/> dependency property.</summary>
    public static readonly DependencyProperty HasLauncherProperty =
        DependencyProperty.Register(nameof(HasLauncher), typeof(bool), typeof(RibbonGroup), new PropertyMetadata(false, OnChromeChanged));

    /// <summary>Identifies the <see cref="LauncherFlyout"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherFlyoutProperty =
        DependencyProperty.Register(nameof(LauncherFlyout), typeof(FlyoutBase), typeof(RibbonGroup), new PropertyMetadata(null, OnChromeChanged));

    private const string LabelRowPart = "PART_LabelRow";
    private const string ItemsHostPart = "PART_ItemsHost";
    private const string ItemsPart = "PART_Items";
    private const string LabelPart = "PART_Label";
    private const string CollapsedPart = "PART_Collapsed";
    private const string CollapsedContentPart = "PART_CollapsedContent";
    private const string LauncherPart = "PART_Launcher";

    private readonly ObservableCollection<UIElement> items = [];
    private readonly Border flyoutHost = new();

    private RowDefinition? labelRow;
    private Border? itemsHost;
    private RibbonItemsPanel? itemsPanel;
    private TextBlock? labelText;
    private Button? collapsedButton;
    private RibbonItemContent? collapsedContent;
    private Button? launcher;

    // What the items measured the last time this group was on the strip. A folded group's items sit
    // in a closed flyout, where they have no template applied and measure as nothing; without this
    // the layout would be told the group costs zero and would never bring it back.
    private RibbonItemMetrics[]? measured;

    // The last thing the layout asked for, so that a template arriving afterwards is folded the way
    // the strip already decided rather than opening the group back up.
    private bool showsCollapsedLabel = true;

    // How wide the group's name is, remembered from when it was last on show. A folded group hides
    // its name, and a hidden TextBlock measures nothing: read straight off, a group would report a
    // floor of zero the moment it folded and the strip would be laid out from a number that depends
    // on what it last did rather than on what the group says. It is the trap the item metrics fell
    // into, one control along - and it showed as a folded button drawn with its name in a window
    // dragged narrow and without it in a window opened narrow, at the same width.
    private double nameWidth;

    // How many rows this group is laid out in: three in a full ribbon, one in a simplified one.
    // Written by the tab, which is told by the ribbon.
    private int rows = RibbonMetrics.MaxRows;

    // How tall a row of this group has to be, from the last time its items were measured. Kept
    // because the ribbon asks every tab how tall it needs to be - including the tabs that are not
    // showing, which cannot be measured at all: a collapsed element measures as nothing however
    // directly it is asked.
    private double rowHeight = RibbonMetrics.RowHeight;

    // Whether the items, as they measured, hold something that cannot be drawn in a single row.
    // Cached alongside them and for the same reason: a folded group's items are in a closed flyout
    // and measure as nothing, so a group asked afresh would say it fits a row and unfold into one.
    private bool needsRoom;

    /// <summary>Occurs when the launcher is pressed, for a group that opens a dialog rather than a flyout.</summary>
    public event TypedEventHandler<RibbonGroup, RoutedEventArgs>? LauncherClick;

    /// <summary>Initializes a new instance of the <see cref="RibbonGroup"/> class.</summary>
    public RibbonGroup()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonGroup);
        items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>Gets or sets the group's name, already in the user's language.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Gets or sets the icon the group's button wears once it has folded.</summary>
    public IconSource? IconSource
    {
        get => (IconSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    /// <summary>Gets or sets which group gives way first. The lowest gives way first.</summary>
    /// <remarks>Groups of equal priority give way from the right, as in Office. Defaults to zero, so a ribbon that says nothing loses room from the right.</remarks>
    public int Priority
    {
        get => (int)GetValue(PriorityProperty);
        set => SetValue(PriorityProperty, value);
    }

    /// <summary>Gets a value indicating whether the group has folded into its button.</summary>
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        private set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the group offers a launcher. Off out of the box.</summary>
    /// <remarks>
    /// The small button in the corner of a group that opens everything the group does not have room
    /// for. Off by default because most groups have nothing more to offer, and a button that opens
    /// nothing is worse than no button.
    /// </remarks>
    public bool HasLauncher
    {
        get => (bool)GetValue(HasLauncherProperty);
        set => SetValue(HasLauncherProperty, value);
    }

    /// <summary>Gets or sets what the launcher opens.</summary>
    /// <remarks>Leave it unset and handle <see cref="LauncherClick"/> instead when what should open is a dialog rather than a flyout.</remarks>
    public FlyoutBase? LauncherFlyout
    {
        get => (FlyoutBase?)GetValue(LauncherFlyoutProperty);
        set => SetValue(LauncherFlyoutProperty, value);
    }

    // The rows this group has to lay itself out in. Setting it moves the chrome with it: a group in a
    // one-row ribbon has no name under it - there is no room for one and Office does not draw one -
    // and the button it folds into is drawn beside its icon rather than under it.
    internal int Rows
    {
        get => rows;

        set
        {
            if (rows == value)
            {
                return;
            }

            rows = value;
            measured = null;

            UpdateChrome();
            Fold(IsCollapsed, showsCollapsedLabel);
            InvalidateMeasure();
        }
    }

    // Whether the group draws its own name under it, which is also the question of whether it has
    // the height to. One row has no room for a name and Office's simplified ribbon draws none.
    private bool ShowsName => rows > 1;

    /// <summary>Gets the items of this group, left to right and then top to bottom within a column.</summary>
    /// <remarks>Any element at all: the ribbon's own item types, or a control of your own, which is laid out as <see cref="RibbonItemSize.Normal"/> and keeps its focus.</remarks>
    public IList<UIElement> Items => items;

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        labelRow = GetTemplateChild(LabelRowPart) as RowDefinition;
        itemsHost = GetTemplateChild(ItemsHostPart) as Border;
        itemsPanel = GetTemplateChild(ItemsPart) as RibbonItemsPanel;
        labelText = GetTemplateChild(LabelPart) as TextBlock;
        collapsedButton = GetTemplateChild(CollapsedPart) as Button;
        collapsedContent = GetTemplateChild(CollapsedContentPart) as RibbonItemContent;

        if (collapsedButton is not null)
        {
            collapsedButton.Flyout = new Flyout { Content = flyoutHost };
        }

        if (GetTemplateChild(LauncherPart) is Button found)
        {
            launcher = found;
            launcher.Click += (sender, arguments) => LauncherClick?.Invoke(this, arguments);
        }

        Sync();
        UpdateChrome();

        // The strip may already have decided this group is folded - the layout runs before a control
        // that has never been measured has a template - and the parts that do the folding have only
        // just appeared. Without this the group would draw itself open in a strip laid out on the
        // understanding that it was shut, and overflow the width by its whole expanded self.
        Fold(IsCollapsed, showsCollapsedLabel);
    }

    // How tall this group needs to be, whatever the width and whether or not it is on show.
    //
    // Not what it currently measures: what it needs. A group that has folded draws a button and
    // needs less, and a group in a tab nobody has chosen yet measures nothing at all, and neither is
    // a reason for the ribbon to be a different height. It is the rows it has at the height its
    // items need them, with its name and its padding on top.
    internal double RequiredHeight
    {
        get
        {
            ApplyTemplate();
            measured ??= MeasureItems();

            // A group that cannot be drawn in the rows there are is drawn as its button at every
            // width, and a button is one row whatever the group holds. Asking what its items need
            // would have a stack of three combo boxes decide the height of a ribbon that is not
            // going to draw them: it is the flyout that draws them, and a flyout costs no strip.
            double needed = needsRoom && rows == 1
                ? RibbonMetrics.RowHeight
                : rows * rowHeight;

            return needed
                + (ShowsName ? RibbonMetrics.GroupLabelHeight : 0)
                + (2 * RibbonMetrics.GroupPadding);
        }
    }

    // Everything the layout needs to know about this group, measured now.
    internal RibbonGroupMetrics CollectMetrics()
    {
        // Before anything is read off it. A control WinUI has not measured yet has no template, and
        // a group with no template has no name to put a floor under its width and no parts to fold
        // with - so the first pass would decide the strip from numbers belonging to a group that
        // cannot draw itself, and the first pass is the whole layout for a window that opens at the
        // width it stays at.
        ApplyTemplate();

        if (!IsCollapsed || measured is null)
        {
            measured = MeasureItems();
        }

        if (labelText is { Visibility: Visibility.Visible })
        {
            labelText.Measure(new Size(double.PositiveInfinity, RibbonMetrics.GroupLabelHeight));
            nameWidth = labelText.DesiredSize.Width;
        }

        // A one-row ribbon draws no name under a group, so the name is no floor under its width
        // either. Reading it anyway would have every group in a simplified strip as wide as its own
        // name for nothing.
        double labelWidth = ShowsName ? nameWidth : 0;

        // The launcher sits beside the name, so it widens the floor the name puts under the group
        // rather than being paid for somewhere else. Leaving it out of the sum would have the layout
        // predict a group narrower than the group draws - which is the fault the name floor was
        // introduced to fix, and it would be back the moment a group switched its launcher on.
        if (HasLauncher && ShowsName)
        {
            labelWidth += RibbonMetrics.LauncherSize;
        }

        const double chrome = 2 * RibbonMetrics.GroupPadding;

        // The folded button draws exactly like an item, so its width comes out of the same numbers
        // rather than a guess that would drift away from what it actually renders - Large in a full
        // ribbon, with the name under the icon, and Normal in a one-row one, with the name beside it.
        double icon = ShowsName ? RibbonMetrics.LargeIconSize : RibbonMetrics.SmallIconSize;
        double name = nameWidth;

        double iconOnly = icon + (2 * RibbonMetrics.ItemPadding) + chrome;
        double withLabel = ShowsName
            ? Math.Max(icon, name) + (2 * RibbonMetrics.ItemPadding) + chrome
            : icon + RibbonMetrics.IconLabelGap + name + (2 * RibbonMetrics.ItemPadding) + chrome;

        return new RibbonGroupMetrics(Priority, chrome, labelWidth, withLabel, iconOnly, measured, needsRoom && rows == 1);
    }

    // Applies what the layout decided: the shape of every item, and whether the group is folded.
    internal void Apply(RibbonGroupArrangement arrangement)
    {
        for (int i = 0; i < items.Count && i < arrangement.ItemSizes.Count; i++)
        {
            Ribbon.SetSize(items[i], arrangement.ItemSizes[i]);
        }

        Fold(arrangement.IsCollapsed, arrangement.ShowsCollapsedLabel);
    }

    private static void OnChromeChanged(DependencyObject group, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonGroup)group).UpdateChrome();
    }

    private static void OnLayoutChanged(DependencyObject group, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonGroup)group).InvalidateMeasure();
    }

    // Asks each item how wide it would be in each of the three shapes, which is what the layout
    // chooses between. Three measures per item per pass, with nothing cached: whether that is too
    // many is a question for the probe's PERF run, not for a guess made here.
    private RibbonItemMetrics[] MeasureItems()
    {
        var widths = new (double Small, double Normal, double Large)[items.Count];
        var heights = new double[items.Count];

        for (int i = 0; i < items.Count; i++)
        {
            UIElement item = items[i];
            RibbonItemSize restore = Ribbon.GetSize(item);

            widths[i].Small = MeasureAt(item, RibbonItemSize.Small);
            widths[i].Normal = MeasureAt(item, RibbonItemSize.Normal);

            // The height that decides how many rows it needs is the one it has on a row, which is
            // the Normal one - a Large item spans the group whatever it measures.
            heights[i] = item.DesiredSize.Height;

            widths[i].Large = MeasureAt(item, RibbonItemSize.Large);

            Ribbon.SetSize(item, restore);
        }

        // The same rule the panel places by, so that what the layout decides and what the group draws
        // cannot come apart. Kept, because the ribbon reads it back to give every tab one height.
        rowHeight = RibbonRowFit.RowHeight(heights, rows);

        var metrics = new RibbonItemMetrics[items.Count];
        needsRoom = false;

        for (int i = 0; i < items.Count; i++)
        {
            metrics[i] = new RibbonItemMetrics(
                Ribbon.GetAllowedSizes(items[i]),
                widths[i].Small,
                widths[i].Normal,
                widths[i].Large,
                items[i] is RibbonSeparator,
                RibbonRowFit.Rows(heights[i], rowHeight, rows));

            // Against the same bound that decides who sets the height of a row rather than against
            // the row itself, which in a ribbon of one row would be as tall as whatever it was
            // asked to hold and would therefore say that everything fits.
            //
            // Two ways of not fitting a row: being taller than one, and accepting no shape but
            // Large, which is an icon above a label and three rows of anybody's ribbon. Either is
            // what makes a group draw itself as its button in a one-row ribbon.
            needsRoom |= heights[i] > RibbonRowFit.SingleRowCeiling
                || metrics[i].SizeUnder(RibbonItemSize.Normal) == RibbonItemSize.Large;
        }

        return metrics;
    }

    private static double MeasureAt(UIElement item, RibbonItemSize size)
    {
        Ribbon.SetSize(item, size);
        item.InvalidateMeasure();
        // No ceiling, for the reason the panel gives: an item measured against the group's three rows
        // never asks for more than those three rows, and the height read back here is the one the
        // rows have to be tall enough to hold.
        item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        return item.DesiredSize.Width;
    }

    // Moves the panel between the group and the button's flyout. Detached before attached, because
    // an element with two parents is not a thing.
    private void Fold(bool collapsed, bool showsLabel)
    {
        IsCollapsed = collapsed;
        showsCollapsedLabel = showsLabel;

        if (collapsedContent is not null)
        {
            collapsedContent.Label = showsLabel ? Label : string.Empty;
        }

        if (itemsHost is null || itemsPanel is null || collapsedButton is null)
        {
            return;
        }

        itemsHost.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        collapsedButton.Visibility = collapsed ? Visibility.Visible : Visibility.Collapsed;

        // The strip under the group, with its name in it, and the row that holds it. A ribbon of one
        // row has neither: no name, no launcher, and no sixteen pixels held for them under every
        // group on the strip.
        if (labelRow is not null)
        {
            labelRow.Height = new GridLength(ShowsName ? RibbonMetrics.GroupLabelHeight : 0);
        }

        if (labelText is not null)
        {
            labelText.Visibility = collapsed || !ShowsName ? Visibility.Collapsed : Visibility.Visible;
        }

        // The launcher goes with the name it stands beside. It sits in the same panel, which is
        // drawn over the button the group has become, so a group that folded with its launcher on
        // wore it on top of its own button - a second thing to press, opening the dialog of a group
        // whose commands are all behind the flyout underneath it.
        if (launcher is not null)
        {
            launcher.Visibility = !collapsed && HasLauncher && ShowsName ? Visibility.Visible : Visibility.Collapsed;
        }

        // Inside the flyout the group has all the room it wants, so it is laid out there the way a
        // full ribbon would lay it out however few rows the strip itself has.
        itemsPanel.Rows = collapsed ? RibbonMetrics.MaxRows : rows;

        if (collapsed && !ReferenceEquals(flyoutHost.Child, itemsPanel))
        {
            itemsHost.Child = null;
            flyoutHost.Child = itemsPanel;
        }
        else if (!collapsed && !ReferenceEquals(itemsHost.Child, itemsPanel))
        {
            flyoutHost.Child = null;
            itemsHost.Child = itemsPanel;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs arguments)
    {
        measured = null;
        Sync();
    }

    private void Sync()
    {
        if (itemsPanel is null)
        {
            return;
        }

        for (int i = itemsPanel.Children.Count - 1; i >= 0; i--)
        {
            if (!items.Contains(itemsPanel.Children[i]))
            {
                itemsPanel.Children.RemoveAt(i);
            }
        }

        foreach (UIElement item in items)
        {
            if (!itemsPanel.Children.Contains(item))
            {
                itemsPanel.Children.Add(item);
            }
        }

        InvalidateMeasure();
    }

    private void UpdateChrome()
    {
        measured = null;

        // Only while the name is on show, because that is the only state it can be measured in. A
        // group renamed while it is folded keeps the width of the name it had until it opens again,
        // which is one pass of one width out rather than a floor of nothing.
        if (labelText is { Visibility: Visibility.Visible })
        {
            nameWidth = 0;
        }

        if (labelText is not null)
        {
            labelText.Text = Label;
        }

        if (itemsPanel is not null)
        {
            itemsPanel.Rows = IsCollapsed ? RibbonMetrics.MaxRows : rows;
        }

        if (collapsedContent is not null)
        {
            collapsedContent.Label = Label;
            collapsedContent.IconSource = IconSource;
            collapsedContent.ItemSize = ShowsName ? RibbonItemSize.Large : RibbonItemSize.Normal;
        }

        if (collapsedButton is not null)
        {
            AutomationProperties.SetName(collapsedButton, string.Format(CultureInfo.CurrentCulture, RibbonStrings.CollapsedGroupNameFormat, Label));
        }

        if (launcher is not null)
        {
            launcher.Visibility = HasLauncher && !IsCollapsed ? Visibility.Visible : Visibility.Collapsed;
            launcher.Flyout = LauncherFlyout;

            // Every launcher looks the same and does something different, so the group's name is the
            // only thing that tells one from another to anybody not looking at the screen.
            AutomationProperties.SetName(launcher, string.Format(CultureInfo.CurrentCulture, RibbonStrings.GroupLauncherNameFormat, Label));
            ToolTipService.SetToolTip(launcher, AutomationProperties.GetName(launcher));
        }

        InvalidateMeasure();
    }
}

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

    private const string ItemsHostPart = "PART_ItemsHost";
    private const string ItemsPart = "PART_Items";
    private const string LabelPart = "PART_Label";
    private const string CollapsedPart = "PART_Collapsed";
    private const string CollapsedContentPart = "PART_CollapsedContent";
    private const string LauncherPart = "PART_Launcher";

    private readonly ObservableCollection<UIElement> items = [];
    private readonly Border flyoutHost = new();

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

    /// <summary>Gets the items of this group, left to right and then top to bottom within a column.</summary>
    /// <remarks>Any element at all: the ribbon's own item types, or a control of your own, which is laid out as <see cref="RibbonItemSize.Normal"/> and keeps its focus.</remarks>
    public IList<UIElement> Items => items;

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

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
    }

    // Everything the layout needs to know about this group, measured now.
    internal RibbonGroupMetrics CollectMetrics()
    {
        if (!IsCollapsed || measured is null)
        {
            measured = MeasureItems();
        }

        double labelWidth = 0;
        if (labelText is not null)
        {
            labelText.Measure(new Size(double.PositiveInfinity, RibbonMetrics.GroupLabelHeight));
            labelWidth = labelText.DesiredSize.Width;
        }

        // The launcher sits beside the name, so it widens the floor the name puts under the group
        // rather than being paid for somewhere else. Leaving it out of the sum would have the layout
        // predict a group narrower than the group draws - which is the fault the name floor was
        // introduced to fix, and it would be back the moment a group switched its launcher on.
        if (HasLauncher)
        {
            labelWidth += RibbonMetrics.LauncherSize;
        }

        const double chrome = 2 * RibbonMetrics.GroupPadding;

        // The folded button draws exactly like a Large item, so its width comes out of the same
        // numbers rather than a guess that would drift away from what it actually renders.
        double iconOnly = RibbonMetrics.LargeIconSize + (2 * RibbonMetrics.ItemPadding) + chrome;
        double withLabel = Math.Max(RibbonMetrics.LargeIconSize, labelWidth) + (2 * RibbonMetrics.ItemPadding) + chrome;

        return new RibbonGroupMetrics(Priority, chrome, labelWidth, withLabel, iconOnly, measured);
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
        var metrics = new RibbonItemMetrics[items.Count];

        for (int i = 0; i < items.Count; i++)
        {
            UIElement item = items[i];
            RibbonItemSize restore = Ribbon.GetSize(item);

            metrics[i] = new RibbonItemMetrics(
                Ribbon.GetAllowedSizes(item),
                WidthAt(item, RibbonItemSize.Small),
                WidthAt(item, RibbonItemSize.Normal),
                WidthAt(item, RibbonItemSize.Large),
                item is RibbonSeparator);

            Ribbon.SetSize(item, restore);
        }

        return metrics;
    }

    private static double WidthAt(UIElement item, RibbonItemSize size)
    {
        Ribbon.SetSize(item, size);
        item.InvalidateMeasure();
        item.Measure(new Size(double.PositiveInfinity, RibbonMetrics.MaxRows * RibbonMetrics.RowHeight));

        return item.DesiredSize.Width;
    }

    // Moves the panel between the group and the button's flyout. Detached before attached, because
    // an element with two parents is not a thing.
    private void Fold(bool collapsed, bool showsLabel)
    {
        IsCollapsed = collapsed;

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

        if (labelText is not null)
        {
            labelText.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        }

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

        if (labelText is not null)
        {
            labelText.Text = Label;
        }

        if (collapsedContent is not null)
        {
            collapsedContent.Label = Label;
            collapsedContent.IconSource = IconSource;
            collapsedContent.ItemSize = RibbonItemSize.Large;
        }

        if (collapsedButton is not null)
        {
            AutomationProperties.SetName(collapsedButton, string.Format(CultureInfo.CurrentCulture, RibbonStrings.CollapsedGroupNameFormat, Label));
        }

        if (launcher is not null)
        {
            launcher.Visibility = HasLauncher ? Visibility.Visible : Visibility.Collapsed;
            launcher.Flyout = LauncherFlyout;

            // Every launcher looks the same and does something different, so the group's name is the
            // only thing that tells one from another to anybody not looking at the screen.
            AutomationProperties.SetName(launcher, string.Format(CultureInfo.CurrentCulture, RibbonStrings.GroupLauncherNameFormat, Label));
            ToolTipService.SetToolTip(launcher, AutomationProperties.GetName(launcher));
        }

        InvalidateMeasure();
    }
}

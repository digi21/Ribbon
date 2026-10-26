using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>The inside of a ribbon item: its icon, its label, and the three ways of putting them together.</summary>
/// <remarks>
/// <para>
/// A panel that measures and arranges its two children itself, rather than three visual states over
/// a fixed tree, because the layout has to ask the same item how wide it would be in each shape
/// before it can choose one. Asking means setting the shape and measuring immediately, and a visual
/// state has not necessarily been applied by then - a storyboard certainly has not. Deciding in
/// <c>MeasureOverride</c> is what makes the answer true at the moment it is read.
/// </para>
/// <para>
/// The icon goes in a <see cref="Viewbox"/> so that one <see cref="IconSource"/> serves every size:
/// a font icon is sized by its glyph size, a bitmap by its bounds, and scaling the built element
/// treats all of them the same way.
/// </para>
/// </remarks>
public sealed partial class RibbonItemContent : Panel
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonItemContent), new PropertyMetadata(string.Empty, OnContentChanged));

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(RibbonItemContent), new PropertyMetadata(null, OnContentChanged));

    /// <summary>Identifies the <see cref="ItemSize"/> dependency property.</summary>
    public static readonly DependencyProperty ItemSizeProperty =
        DependencyProperty.Register(nameof(ItemSize), typeof(RibbonItemSize), typeof(RibbonItemContent), new PropertyMetadata(RibbonItemSize.Normal, OnContentChanged));

    /// <summary>Identifies the <see cref="ShowsChevron"/> dependency property.</summary>
    public static readonly DependencyProperty ShowsChevronProperty =
        DependencyProperty.Register(nameof(ShowsChevron), typeof(bool), typeof(RibbonItemContent), new PropertyMetadata(false, OnContentChanged));

    private readonly Viewbox iconBox = new() { Stretch = Stretch.Uniform };
    private readonly IconSourceElement icon = new();
    private readonly FontIcon chevron = new()
    {
        FontSize = 8,
        Glyph = "",
        Visibility = Visibility.Collapsed,
    };

    private readonly TextBlock label = new()
    {
        TextTrimming = TextTrimming.CharacterEllipsis,
        TextWrapping = TextWrapping.NoWrap,
        VerticalAlignment = VerticalAlignment.Center,
    };

    // Which half of a split button is out of action. A split button draws its two halves with one
    // content - the shapes are this panel's to decide, and two panels would decide them twice - so
    // the half that greys out is greyed out in here, part by part.
    private bool primaryDimmed;
    private bool secondaryDimmed;

    /// <summary>Initializes a new instance of the <see cref="RibbonItemContent"/> class.</summary>
    public RibbonItemContent()
    {
        iconBox.Child = icon;
        Children.Add(iconBox);
        Children.Add(label);
        Children.Add(chevron);
    }

    /// <summary>Gets or sets a value indicating whether the item shows the mark of something that opens.</summary>
    /// <remarks>
    /// Here rather than in the item's template, because where it goes depends on the shape: beside
    /// the label when there is one on the same line, and under it when the icon is above the text.
    /// A template cannot know which of those it is looking at; this panel decides the shape.
    /// </remarks>
    public bool ShowsChevron
    {
        get => (bool)GetValue(ShowsChevronProperty);
        set => SetValue(ShowsChevronProperty, value);
    }

    /// <summary>Gets or sets the text beside or below the icon.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Gets or sets the recipe the icon is built from.</summary>
    public IconSource? IconSource
    {
        get => (IconSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    /// <summary>Gets or sets the shape to draw.</summary>
    public RibbonItemSize ItemSize
    {
        get => (RibbonItemSize)GetValue(ItemSizeProperty);
        set => SetValue(ItemSizeProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        icon.IconSource = IconSource;
        label.Text = Label;

        double side = ItemSize == RibbonItemSize.Large ? RibbonMetrics.LargeIconSize : RibbonMetrics.SmallIconSize;
        iconBox.Width = side;
        iconBox.Height = side;
        iconBox.Visibility = IconSource is null ? Visibility.Collapsed : Visibility.Visible;
        iconBox.Measure(new Size(side, side));

        bool showsLabel = ItemSize != RibbonItemSize.Small && !string.IsNullOrEmpty(Label);
        label.Visibility = showsLabel ? Visibility.Visible : Visibility.Collapsed;
        label.Measure(showsLabel ? new Size(double.PositiveInfinity, double.PositiveInfinity) : new Size(0, 0));

        chevron.Visibility = ShowsChevron ? Visibility.Visible : Visibility.Collapsed;
        chevron.Measure(ShowsChevron ? new Size(RibbonMetrics.ChevronSize, RibbonMetrics.ChevronSize) : new Size(0, 0));

        double iconWidth = IconSource is null ? 0 : side;
        double labelWidth = showsLabel ? label.DesiredSize.Width : 0;
        double mark = ShowsChevron ? RibbonMetrics.ChevronSize : 0;

        return ItemSize switch
        {
            // The mark goes under the text when the icon is above it, so it widens nothing.
            RibbonItemSize.Large => new Size(
                Math.Max(iconWidth, labelWidth) + (2 * RibbonMetrics.ItemPadding),
                RibbonMetrics.MaxRows * RibbonMetrics.RowHeight),

            RibbonItemSize.Normal => new Size(
                iconWidth + (showsLabel && iconWidth > 0 ? RibbonMetrics.IconLabelGap : 0) + labelWidth + mark + (2 * RibbonMetrics.ItemPadding),
                RibbonMetrics.RowHeight),

            _ => new Size(iconWidth + mark + (2 * RibbonMetrics.ItemPadding), RibbonMetrics.RowHeight),
        };
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        double side = ItemSize == RibbonItemSize.Large ? RibbonMetrics.LargeIconSize : RibbonMetrics.SmallIconSize;
        bool showsIcon = IconSource is not null;
        bool showsLabel = label.Visibility == Visibility.Visible;

        double mark = ShowsChevron ? RibbonMetrics.ChevronSize : 0;

        if (ItemSize == RibbonItemSize.Large)
        {
            (double iconTop, double top) = Column(finalSize.Height);

            if (showsIcon)
            {
                iconBox.Arrange(new Rect((finalSize.Width - side) / 2, iconTop, side, side));
            }

            if (showsLabel)
            {
                label.Arrange(new Rect(
                    RibbonMetrics.ItemPadding,
                    top,
                    Math.Max(0, finalSize.Width - (2 * RibbonMetrics.ItemPadding)),
                    label.DesiredSize.Height));
            }

            if (ShowsChevron)
            {
                // Under the name, centred: an item drawn with its icon above its text says that it
                // opens by pointing downwards at the end of the reading, not off to one side.
                chevron.Arrange(new Rect(
                    (finalSize.Width - mark) / 2,
                    top + (showsLabel ? label.DesiredSize.Height : 0),
                    mark,
                    mark));
            }

            ApplyDimming(large: true);

            return finalSize;
        }

        double x = RibbonMetrics.ItemPadding;
        if (showsIcon)
        {
            iconBox.Arrange(new Rect(x, (finalSize.Height - side) / 2, side, side));
            x += side + (showsLabel ? RibbonMetrics.IconLabelGap : 0);
        }

        if (showsLabel)
        {
            double room = Math.Max(0, finalSize.Width - x - mark - RibbonMetrics.ItemPadding);
            label.Arrange(new Rect(x, 0, room, finalSize.Height));
            x += room;
        }

        if (ShowsChevron)
        {
            chevron.Arrange(new Rect(x, (finalSize.Height - mark) / 2, mark, mark));
        }

        ApplyDimming(large: false);

        return finalSize;
    }

    // Greys out the half of a split button that cannot be used: its command cannot run, or the whole
    // item is disabled and both of them are.
    internal void Dim(bool primary, bool secondary)
    {
        primaryDimmed = primary;
        secondaryDimmed = secondary;
        InvalidateArrange();
    }

    // How much of the far end of an item of this size belongs to the half of a split button that
    // opens: the chevron and the room around it beside a label, and the label and the chevron under
    // an icon. Asked by the split button before it arranges its halves, so it is worked out from what
    // the measure pass left rather than from an arrange that has not happened yet.
    internal double SecondaryExtent(Size size)
    {
        if (ItemSize != RibbonItemSize.Large)
        {
            // From where the chevron starts to the end. Beside a label the chevron is at the end of
            // the item; on its own it follows the icon with no gap, and an item stretched across a
            // column wider than itself leaves the rest of the column to the half that opens rather
            // than to a command whose icon is nowhere near it.
            bool showsLabel = label.Visibility == Visibility.Visible;
            double chevronLeft = size.Width - RibbonMetrics.ItemPadding - (ShowsChevron ? RibbonMetrics.ChevronSize : 0);

            if (!showsLabel)
            {
                chevronLeft = RibbonMetrics.ItemPadding + (IconSource is null ? 0 : RibbonMetrics.SmallIconSize);
            }

            return size.Width - chevronLeft;
        }

        // Office's large split button: the icon is the command, and the name with the chevron under
        // it is what opens. Halfway into the gap between them, so neither half is drawn touching the
        // other's edge.
        return IconSource is null
            ? size.Height / 2
            : size.Height - Column(size.Height).LabelTop + (RibbonMetrics.ItemPadding / 2);
    }

    // Where the icon and the label start down an item drawn Large.
    //
    // Centred down the column rather than started at the top of it. An item drawn Large takes the
    // whole height of the group whatever it measures, and a group holding a stack of controls beside
    // its buttons is taller than three rows of the ribbon's own - which used to leave a Large button's
    // icon and name pressed against the top of a highlight running forty pixels past the bottom of
    // them.
    private (double IconTop, double LabelTop) Column(double height)
    {
        bool showsIcon = IconSource is not null;
        bool showsLabel = label.Visibility == Visibility.Visible;

        double content = (showsIcon ? RibbonMetrics.LargeIconSize + RibbonMetrics.ItemPadding : 0)
            + (showsLabel ? label.DesiredSize.Height : 0)
            + (ShowsChevron ? RibbonMetrics.ChevronSize : 0);

        double iconTop = Math.Max(RibbonMetrics.ItemPadding, (height - content) / 2);

        return (iconTop, iconTop + (showsIcon ? RibbonMetrics.LargeIconSize + RibbonMetrics.ItemPadding : 0));
    }

    private void ApplyDimming(bool large)
    {
        const double Dimmed = 0.4;

        // The label is part of what opens when it is under the icon, and part of the command when it
        // is beside it - the same line SecondaryExtent draws between the halves.
        iconBox.Opacity = primaryDimmed ? Dimmed : 1;
        label.Opacity = (large ? secondaryDimmed : primaryDimmed) ? Dimmed : 1;
        chevron.Opacity = secondaryDimmed ? Dimmed : 1;
    }

    private static void OnContentChanged(DependencyObject content, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonItemContent)content).InvalidateMeasure();
    }
}

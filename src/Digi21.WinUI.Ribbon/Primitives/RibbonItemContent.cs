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
            double iconTop = RibbonMetrics.ItemPadding;
            if (showsIcon)
            {
                iconBox.Arrange(new Rect((finalSize.Width - side) / 2, iconTop, side, side));
            }

            double top = iconTop + (showsIcon ? side + RibbonMetrics.ItemPadding : 0);

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

        return finalSize;
    }

    private static void OnContentChanged(DependencyObject content, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonItemContent)content).InvalidateMeasure();
    }
}
